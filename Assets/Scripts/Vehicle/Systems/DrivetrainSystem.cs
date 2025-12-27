// DrivetrainSystem.cs
using UnityEngine;
using Vehicle.Core;
using Vehicle.Specs;

namespace Vehicle.Systems
{
    /// <summary>
    /// DrivetrainSystem (powertrain orchestrator):
    /// - Updates gearbox state (manual/auto)
    /// - Computes mechanical engine RPM from driven wheels (uses wheelOmega when grounded)
    /// - Computes engine torque via EngineSystem (with HARD-CUT limiter)
    /// - Writes per-wheel driveTorque into WheelRuntime
    ///
    /// Does NOT apply forces. TireForcesSystem turns (driveTorque/brakeTorque) into Fx/Fy and applies friction circle.
    ///
    /// Wheel order convention:
    /// 0=FL, 1=FR, 2=RL, 3=RR
    /// FWD => 0,1 ; RWD => 2,3 ; AWD => 0,1,2,3
    /// </summary>
    public sealed class DrivetrainSystem : IVehicleModule
    {
        private const float MinWheelRadius = 0.01f;
        private const float MinGearRatioCombined = 0.001f;
        private const float ShiftInputThreshold = 0.5f;
        private const float Epsilon = 1e-6f;

        private readonly EngineSystem _engine = new EngineSystem();

        private GearboxSystem _gearbox;
        private GearboxSpec _cachedGearboxSpec;

        // Smoothed for UI/telemetry. Real physics uses mechanicalRpm.
        private float _engineRpmSmoothed;

        // Simple clutch model: disengage while shifting
        private bool _clutchEngaged = true;

        public void Tick(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
        {
            // ------------------------------------------------------------
            // 0) Validate
            // ------------------------------------------------------------
            var engineSpec = ctx.engineSpec ?? ctx.spec?.engineSpec;
            var gearboxSpec = ctx.gearboxSpec ?? ctx.spec?.gearboxSpec;
            var wheelSpec = ctx.wheelSpec ?? ctx.spec?.wheelSpec;

            if (ctx.rb == null || engineSpec == null || gearboxSpec == null || wheelSpec == null)
                return;

            if (state.wheels == null || state.wheels.Length == 0)
                return;

            // ------------------------------------------------------------
            // 1) Init / re-init gearbox if spec changed
            // ------------------------------------------------------------
            if (_gearbox == null || _cachedGearboxSpec != gearboxSpec)
            {
                _gearbox = new GearboxSystem(gearboxSpec);
                _cachedGearboxSpec = gearboxSpec;
                _engineRpmSmoothed = Mathf.Max(engineSpec.idleRPM, _engineRpmSmoothed);
            }

            // Clear per-wheel drive torque so it never "sticks"
            for (int i = 0; i < state.wheels.Length; i++)
                state.wheels[i].driveTorque = 0f;

            // ------------------------------------------------------------
            // 2) Manual shift input (if manual gearbox)
            // ------------------------------------------------------------
            if (gearboxSpec.transmissionType == GearboxSpec.TransmissionType.Manual)
            {
                if (input.shiftUp > ShiftInputThreshold) _gearbox.ShiftUp();
                else if (input.shiftDown > ShiftInputThreshold) _gearbox.ShiftDown();
            }

            // Throttle in 0..1 (sign/Reverse is handled by gear ratio sign)
            float throttle01 = Mathf.Clamp01(Mathf.Abs(input.throttle));

            // Body forward speed (m/s)
            float vForward = Vector3.Dot(ctx.rb.linearVelocity, ctx.Forward);

            // ------------------------------------------------------------
            // 3) Gearbox update (auto shifting, shift timers)
            // ------------------------------------------------------------
            _gearbox.Update(_engineRpmSmoothed, Mathf.Abs(vForward), throttle01, ctx.dt);

            float gearRatioCombined = _gearbox.GetCurrentGearRatio(); // includes final drive in your GearboxSystem
            bool gearEngaged = !_gearbox.IsShifting && Mathf.Abs(gearRatioCombined) > MinGearRatioCombined;

            _clutchEngaged = !_gearbox.IsShifting;

            // ------------------------------------------------------------
            // 4) Wheel radius source of truth
            // ------------------------------------------------------------
            float wheelRadius = state.wheelRadius > MinWheelRadius ? state.wheelRadius : wheelSpec.wheelRadius;
            wheelRadius = Mathf.Max(MinWheelRadius, wheelRadius);

            // ------------------------------------------------------------
            // 5) Mechanical RPM from driven wheels (uses wheelOmega when possible)
            // ------------------------------------------------------------
            int[] driven = GetDrivenWheelIndices(ctx, state.wheels.Length);

            float avgDrivenOmega = 0f; // rad/s
            int omegaCount = 0;

            for (int k = 0; k < driven.Length; k++)
            {
                int idx = driven[k];
                if (idx < 0 || idx >= state.wheels.Length) continue;

                ref WheelRuntime w = ref state.wheels[idx];
                if (!w.isGrounded) continue;

                // wheelOmega is updated by TireForcesSystem (wheelspin/lockup)
                avgDrivenOmega += w.wheelOmega;
                omegaCount++;
            }

            if (omegaCount > 0)
                avgDrivenOmega /= omegaCount;
            else
                avgDrivenOmega = vForward / wheelRadius; // fallback when airborne etc.

            // Extract base gear ratio (without final drive) because EngineSystem expects them separately.
            float finalDrive = Mathf.Max(Epsilon, gearboxSpec.finalDriveRatio);

            // Your GearboxSystem.GetCurrentGearRatio() already multiplies by finalDriveRatio.
            // So baseGear = |combined| / finalDrive.
            float baseGear = Mathf.Abs(gearRatioCombined) / finalDrive;

            float mechanicalRpm = 0f;
            if (gearEngaged)
            {
                mechanicalRpm = _engine.CalculateEngineRPMFromWheel(avgDrivenOmega, baseGear, finalDrive);
            }
            else
            {
                // Neutral: engine RPM not tied to wheels
                mechanicalRpm = _engineRpmSmoothed;
            }

            // ------------------------------------------------------------
            // 6) Smoothed RPM for telemetry / UI (optional)
            // ------------------------------------------------------------
            float rpmLerp = Mathf.Clamp01(engineSpec.rpmInertia * ctx.dt);
            if (gearEngaged && _clutchEngaged)
            {
                _engineRpmSmoothed = Mathf.Lerp(_engineRpmSmoothed, mechanicalRpm, rpmLerp);
            }
            else
            {
                float desired = engineSpec.idleRPM + (engineSpec.maxRPM - engineSpec.idleRPM) * throttle01;
                _engineRpmSmoothed = Mathf.Lerp(_engineRpmSmoothed, desired, rpmLerp);
            }
            _engineRpmSmoothed = Mathf.Clamp(_engineRpmSmoothed, engineSpec.idleRPM, engineSpec.maxRPM);

            // ------------------------------------------------------------
            // 7) Engine torque with HARD-CUT limiter (based on mechanical RPM!)
            // ------------------------------------------------------------
            float engineTorque = 0f;
            if (gearEngaged && _clutchEngaged)
            {
                engineTorque = _engine.GetTorqueHardCutLimitedNm(mechanicalRpm, throttle01, engineSpec, gearEngaged: true);
            }
            else
            {
                // Neutral / clutch open: still allow engine to rev (no wheel torque)
                engineTorque = _engine.GetTorqueNm(_engineRpmSmoothed, throttle01, engineSpec, gearEngaged: true);
            }

            // ------------------------------------------------------------
            // 8) Convert engine torque -> wheel torque (total), distribute to grounded driven wheels
            // ------------------------------------------------------------
            float wheelTorqueTotal = 0f;
            if (_clutchEngaged && gearEngaged)
            {
                // combined gear ratio already includes final drive in your GearboxSystem,
                // so multiply by |combined| (no extra final drive multiply).
                wheelTorqueTotal = engineTorque * Mathf.Abs(gearRatioCombined);
            }

            int groundedDriven = 0;
            for (int k = 0; k < driven.Length; k++)
            {
                int idx = driven[k];
                if (idx < 0 || idx >= state.wheels.Length) continue;
                if (state.wheels[idx].isGrounded) groundedDriven++;
            }

            if (groundedDriven > 0 && wheelTorqueTotal != 0f)
            {
                float sign = Mathf.Sign(gearRatioCombined); // forward/reverse direction
                float perWheelTorque = (wheelTorqueTotal / groundedDriven) * sign;

                for (int k = 0; k < driven.Length; k++)
                {
                    int idx = driven[k];
                    if (idx < 0 || idx >= state.wheels.Length) continue;
                    if (!state.wheels[idx].isGrounded) continue;

                    state.wheels[idx].driveTorque = perWheelTorque;
                }
            }

            // ------------------------------------------------------------
            // 9) Telemetry outputs
            // ------------------------------------------------------------
            state.engineRPM = _engineRpmSmoothed;      // UI-friendly RPM
            state.currentGear = _gearbox.CurrentGear; // -1,0,1...
        }

        private static int[] GetDrivenWheelIndices(in VehicleContext ctx, int wheelCount)
        {
            var driveType = ctx.drivetrainSpec != null
                ? ctx.drivetrainSpec.driveType
                : DrivetrainSpec.DriveType.FWD;

            // If you ever support <4 wheels, you can expand this later.
            if (wheelCount < 4)
                return new[] { 0 };

            return driveType switch
            {
                DrivetrainSpec.DriveType.RWD => new[] { 1, 3 },
                DrivetrainSpec.DriveType.AWD => new[] { 0, 1, 2, 3 },
                _ => new[] { 0, 2 }, // FWD
            };
        }
    }
}
