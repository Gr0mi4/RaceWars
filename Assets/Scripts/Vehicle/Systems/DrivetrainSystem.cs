using UnityEngine;
using Vehicle.Core;
using Vehicle.Specs;

namespace Vehicle.Systems
{
    /// <summary>
    /// DrivetrainSystem:
    /// - Updates gearbox (manual/auto)
    /// - Computes mechanicalRPM from driven wheels (wheelOmega) with low-speed stabilization
    /// - Computes shaftTorque (engine drive + engine drag) with HARD CUT limiter
    /// - Writes per-wheel driveTorque (does NOT apply forces)
    ///
    /// Wheel index convention (ВАЖНО):
    /// Front wheels: 0 and 2
    /// Rear wheels : 1 and 3
    /// </summary>
    public sealed class DrivetrainSystem : IVehicleModule
    {
        private const float MinWheelRadius = 0.01f;
        private const float MinGearRatioCombined = 1e-4f;
        private const float ShiftInputThreshold = 0.5f;
        private const float Epsilon = 1e-6f;

        // Stabilization: don’t trust wheelOmega near standstill
        private const float LowSpeedOmegaTrustMS = 0.6f; // m/s

        private readonly EngineSystem _engine = new EngineSystem();

        private GearboxSystem _gearbox;
        private GearboxSpec _cachedGearboxSpec;

        // UI/telemetry RPM (smoothed)
        private float _engineRpmSmoothed = 800f;

        // simple clutch: disengage while shifting
        private bool _clutchEngaged = true;

        public void Tick(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
        {
            var engineSpec = ctx.engineSpec ?? ctx.spec?.engineSpec;
            var gearboxSpec = ctx.gearboxSpec ?? ctx.spec?.gearboxSpec;
            var wheelSpec = ctx.wheelSpec ?? ctx.spec?.wheelSpec;

            if (ctx.rb == null || engineSpec == null || gearboxSpec == null || wheelSpec == null)
                return;

            if (state.wheels == null || state.wheels.Length == 0)
                return;

            // Init gearbox if needed
            if (_gearbox == null || _cachedGearboxSpec != gearboxSpec)
            {
                _gearbox = new GearboxSystem(gearboxSpec);
                _cachedGearboxSpec = gearboxSpec;
                _engineRpmSmoothed = Mathf.Max(engineSpec.idleRPM, _engineRpmSmoothed);
            }

            // Clear driveTorque each tick so it never sticks
            for (int i = 0; i < state.wheels.Length; i++)
                state.wheels[i].driveTorque = 0f;

            // Manual shifting
            if (gearboxSpec.transmissionType == GearboxSpec.TransmissionType.Manual)
            {
                if (input.shiftUp > ShiftInputThreshold) _gearbox.ShiftUp();
                else if (input.shiftDown > ShiftInputThreshold) _gearbox.ShiftDown();
            }

            float throttle01 = Mathf.Clamp01(Mathf.Abs(input.throttle));

            float vForward = Vector3.Dot(ctx.rb.linearVelocity, ctx.Forward);

            // Gearbox update (auto timers, auto shift)
            _gearbox.Update(_engineRpmSmoothed, Mathf.Abs(vForward), throttle01, ctx.dt);

            float ratioCombined = _gearbox.GetCurrentGearRatio(); // signed, includes final drive
            bool gearEngaged = !_gearbox.IsShifting && Mathf.Abs(ratioCombined) > MinGearRatioCombined;

            _clutchEngaged = !_gearbox.IsShifting;

            float wheelRadius = state.wheelRadius > MinWheelRadius ? state.wheelRadius : wheelSpec.wheelRadius;
            wheelRadius = Mathf.Max(MinWheelRadius, wheelRadius);

            // Driven wheels
            int[] driven = GetDrivenWheelIndices(ctx, state.wheels.Length);

            // Average wheelOmega of grounded driven wheels
            float avgOmega = 0f;
            int omegaCount = 0;
            for (int k = 0; k < driven.Length; k++)
            {
                int idx = driven[k];
                if (idx < 0 || idx >= state.wheels.Length) continue;

                ref WheelRuntime w = ref state.wheels[idx];
                if (!w.isGrounded) continue;

                avgOmega += w.wheelOmega;
                omegaCount++;
            }
            if (omegaCount > 0) avgOmega /= omegaCount;

            // Low-speed stabilization: use omegaFromSpeed when nearly stopped
            float omegaFromSpeed = vForward / wheelRadius;
            if (omegaCount == 0)
            {
                avgOmega = omegaFromSpeed;
            }
            else
            {
                if (Mathf.Abs(vForward) < LowSpeedOmegaTrustMS)
                    avgOmega = omegaFromSpeed; // do not trust noisy wheelOmega at near-zero speed
                else
                    avgOmega = Mathf.Lerp(omegaFromSpeed, avgOmega, 0.85f); // blend at speed
            }

            float finalDrive = Mathf.Max(Epsilon, gearboxSpec.finalDriveRatio);
            float baseGear = (gearEngaged ? (Mathf.Abs(ratioCombined) / finalDrive) : 0f);

            // Mechanical RPM from wheels (this can be > redline, that’s OK)
            float mechanicalRpm;
            if (gearEngaged && baseGear > Epsilon && _clutchEngaged)
            {
                mechanicalRpm = _engine.CalculateEngineRPMFromWheel(avgOmega, baseGear, finalDrive);
            }
            else
            {
                mechanicalRpm = _engineRpmSmoothed;
            }

            // Smoothed RPM for UI
            float rpmLerp = Mathf.Clamp01(engineSpec.rpmInertia * ctx.dt);
            if (gearEngaged && _clutchEngaged)
                _engineRpmSmoothed = Mathf.Lerp(_engineRpmSmoothed, mechanicalRpm, rpmLerp);
            else
            {
                float desired = engineSpec.idleRPM + (engineSpec.maxRPM - engineSpec.idleRPM) * throttle01;
                _engineRpmSmoothed = Mathf.Lerp(_engineRpmSmoothed, desired, rpmLerp);
            }
            _engineRpmSmoothed = Mathf.Clamp(_engineRpmSmoothed, engineSpec.idleRPM, engineSpec.maxRPM);

            // If not coupled to wheels -> no wheel torque (but state is updated)
            state.engineRPM = _engineRpmSmoothed;
            state.currentGear = _gearbox.CurrentGear;

            if (!gearEngaged || !_clutchEngaged)
                return;

            // Compute shaft torque on engine side:
            // sign of engine omega: use avgOmega and ratio sign (important for drag direction)
            float engineOmegaSign = avgOmega * Mathf.Sign(ratioCombined);

            float shaftTorqueEngineNm = _engine.GetShaftTorqueNm(mechanicalRpm, throttle01, engineOmegaSign, engineSpec);

            // Convert to wheel side torque (combined ratio already includes final drive)
            float totalWheelTorque = shaftTorqueEngineNm * Mathf.Abs(ratioCombined);

            // Distribute to grounded driven wheels only
            int groundedDriven = 0;
            for (int k = 0; k < driven.Length; k++)
            {
                int idx = driven[k];
                if (idx < 0 || idx >= state.wheels.Length) continue;
                if (state.wheels[idx].isGrounded) groundedDriven++;
            }

            if (groundedDriven == 0)
                return;

            // Direction: ratio sign maps engine shaft torque direction to wheel direction
            float wheelDir = Mathf.Sign(ratioCombined);
            float perWheel = (totalWheelTorque / groundedDriven) * wheelDir;

            for (int k = 0; k < driven.Length; k++)
            {
                int idx = driven[k];
                if (idx < 0 || idx >= state.wheels.Length) continue;

                ref WheelRuntime w = ref state.wheels[idx];
                if (!w.isGrounded) continue;

                w.driveTorque = perWheel;
            }
        }

        private static int[] GetDrivenWheelIndices(in VehicleContext ctx, int wheelCount)
        {
            var driveType = ctx.drivetrainSpec != null ? ctx.drivetrainSpec.driveType : DrivetrainSpec.DriveType.FWD;

            if (wheelCount < 4)
                return new[] { 0 };

            // Your rule:
            // Front: 0,2
            // Rear : 1,3
            return driveType switch
            {
                DrivetrainSpec.DriveType.RWD => new[] { 1, 3 },
                DrivetrainSpec.DriveType.AWD => new[] { 0, 1, 2, 3 },
                _ => new[] { 0, 2 }, // FWD
            };
        }
    }
}
