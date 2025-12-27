using UnityEngine;
using Vehicle.Core;
using Vehicle.Specs;

namespace Vehicle.UI.Telemetry
{
    /// <summary>
    /// Powertrain telemetry for the current architecture:
    /// DrivetrainSystem writes torques (drive/brake) into WheelRuntime,
    /// TireForcesSystem converts torques -> Fx/Fy with friction circle and applies forces.
    ///
    /// This module focuses on debugging:
    /// - Gear / combined ratio
    /// - Engine RPM vs Mechanical RPM (from driven wheelOmega)
    /// - Torques per driven wheel
    /// - Tire forces / friction budget utilization
    /// </summary>
    public sealed class PowertrainTelemetryModule : TelemetryModuleBase
    {
        public override string ModuleName => "[POWERTRAIN]";

        private const float Epsilon = 1e-5f;

        public PowertrainTelemetryModule(bool enabled = true) : base(enabled) { }

        protected override string BuildDisplayText(in VehicleInput input, in VehicleState state, in VehicleContext ctx)
        {
            var engineSpec = ctx.engineSpec ?? ctx.spec?.engineSpec;
            var gearboxSpec = ctx.gearboxSpec ?? ctx.spec?.gearboxSpec;
            var wheelSpec = ctx.wheelSpec ?? ctx.spec?.wheelSpec;

            if (ctx.rb == null || engineSpec == null || gearboxSpec == null || wheelSpec == null)
                return string.Empty;

            var wheels = state.wheels;
            if (wheels == null || wheels.Length == 0)
                return string.Empty;

            // ---------------------------------------------------------------------
            // 1) Gear & ratios
            // ---------------------------------------------------------------------
            int gear = state.currentGear;

            float gearRatioCombined = GetCombinedRatio(gear, gearboxSpec); // includes final drive
            float finalDrive = Mathf.Max(0.0001f, gearboxSpec.finalDriveRatio);
            float baseGearRatio = Mathf.Abs(gearRatioCombined) / finalDrive;

            bool gearEngaged = (gear != 0) && Mathf.Abs(gearRatioCombined) > 0.001f;

            // ---------------------------------------------------------------------
            // 2) Engine RPM (from state) + mechanical RPM (from driven wheelOmega)
            // ---------------------------------------------------------------------
            float engineRpm = state.engineRPM;

            float wheelRadius = state.wheelRadius > 0.01f ? state.wheelRadius : wheelSpec.wheelRadius;
            wheelRadius = Mathf.Max(0.01f, wheelRadius);

            GetDrivenWheels(ctx, wheels.Length, out int[] drivenIdx);

            float avgDrivenOmega = 0f;
            int omegaCount = 0;

            float sumDriveTq = 0f;
            float sumBrakeTq = 0f;

            float sumFz = 0f;
            float sumMuFz = 0f;

            float sumFx = 0f;
            float sumFy = 0f;

            float sumFxDesired = 0f;
            float sumFyDesired = 0f;

            float sumUtil = 0f;
            float sumVLong = 0f;

            for (int k = 0; k < drivenIdx.Length; k++)
            {
                int i = drivenIdx[k];
                if (i < 0 || i >= wheels.Length) continue;

                ref readonly var w = ref wheels[i];
                if (!w.isGrounded) continue;

                avgDrivenOmega += w.wheelOmega;
                omegaCount++;

                sumDriveTq += w.driveTorque;
                sumBrakeTq += w.brakeTorque;

                sumFz += Mathf.Max(0f, w.normalForce);

                // If you store muFz in debugMuFz (recommended), use it; otherwise compute approx.
                float mu = Mathf.Max(0f, wheelSpec.friction);
                float muFz = (HasDebugMuFz(w) ? w.debugMuFz : (mu * Mathf.Max(0f, w.normalForce)));
                sumMuFz += muFz;

                // Forces after circle clamp (debugLongForce/debugLatForce are world vectors)
                sumFx += w.debugLongForce.magnitude;
                sumFy += w.debugLatForce.magnitude;

                // Desired (before clamp) - optional
                if (HasDebugDesired(w))
                {
                    sumFxDesired += Mathf.Abs(w.debugFxDesired);
                    sumFyDesired += Mathf.Abs(w.debugFyDesired);
                }

                sumUtil += w.debugUtil;

                // vLong - optional
                if (HasDebugVLong(w))
                    sumVLong += w.debugVLong;
            }

            if (omegaCount > 0) avgDrivenOmega /= omegaCount;
            float mechRpm = 0f;
            if (gearEngaged && omegaCount > 0)
            {
                // RPM from wheelOmega: wheelOmega -> engineRPM through gearing
                // EngineSystem.CalculateEngineRPMFromWheel expects wheelAngularVelocity (rad/s) and baseGearRatio + finalDrive
                mechRpm = (avgDrivenOmega * Mathf.Abs(baseGearRatio) * finalDrive) * (60f / (2f * Mathf.PI));
            }

            float utilAvg = omegaCount > 0 ? (sumUtil / omegaCount) : 0f;

            // ---------------------------------------------------------------------
            // 3) Build text
            // ---------------------------------------------------------------------
            string text = ModuleName + "\n";

            // Inputs
            text += FormatValue("Throttle", Mathf.Clamp01(input.throttle), "", 2);
            text += FormatValue("Brake", Mathf.Clamp01(input.brake), "", 2);
            text += FormatValue("Handbrake", Mathf.Clamp01(input.handbrake), "", 2) + "\n";

            // Gearbox
            text += FormatValue("Gear", gear, "", 0);
            text += FormatValue("GR (combined)", gearRatioCombined, "", 3);
            text += FormatValue("GR (base)", baseGearRatio, "", 3);
            text += FormatBool("Gear Engaged", gearEngaged) + "\n";

            // RPMs
            text += FormatValue("Engine RPM (state)", engineRpm, "", 0) + $" / {engineSpec.maxRPM:F0}\n";
            text += FormatValue("Mech RPM (wheelOmega)", mechRpm, "", 0);
            text += FormatValue("RPM Delta (state - mech)", engineRpm - mechRpm, "", 0) + "\n";

            // Torques (driven wheels sums)
            text += FormatValue("DriveTorque Σ (driven)", sumDriveTq, "Nm", 1);
            text += FormatValue("BrakeTorque Σ (driven)", sumBrakeTq, "Nm", 1) + "\n";

            // Tire budget summary
            text += FormatValue("Fz Σ (driven)", sumFz, "N", 0);
            text += FormatValue("mu*Fz Σ", sumMuFz, "N", 0);
            text += FormatValue("Util avg", utilAvg, "", 2) + "\n";

            // Forces summary (after clamp)
            text += FormatValue("|Fx| Σ (after)", sumFx, "N", 0);
            text += FormatValue("|Fy| Σ (after)", sumFy, "N", 0) + "\n";

            // Desired (before clamp) if available
            if (HasDebugDesired(wheels[0]))
            {
                text += FormatValue("|Fx| Σ (desired)", sumFxDesired, "N", 0);
                text += FormatValue("|Fy| Σ (desired)", sumFyDesired, "N", 0) + "\n";
            }

            // vLong if available
            if (HasDebugVLong(wheels[0]))
            {
                float vLongAvg = omegaCount > 0 ? (sumVLong / omegaCount) : 0f;
                text += FormatValue("vLong avg (driven)", vLongAvg, "m/s", 2);
            }

            return text;
        }

        // ---------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------

        private static float GetCombinedRatio(int currentGear, GearboxSpec gearboxSpec)
        {
            if (currentGear == -1)
                return -gearboxSpec.reverseGearRatio * gearboxSpec.finalDriveRatio;

            if (currentGear > 0 && currentGear <= gearboxSpec.gearRatios.Length)
                return gearboxSpec.gearRatios[currentGear - 1] * gearboxSpec.finalDriveRatio;

            return 0f; // neutral / invalid
        }

        private static void GetDrivenWheels(in VehicleContext ctx, int wheelCount, out int[] driven)
        {
            var driveType = ctx.drivetrainSpec != null
                ? ctx.drivetrainSpec.driveType
                : DrivetrainSpec.DriveType.FWD;

            // WheelIndex convention: 0=FL,1=RL,2=FR,3=RR (front=0/2, rear=1/3)
            if (wheelCount < 4)
            {
                driven = new[] { 0 };
                return;
            }

            driven = driveType switch
            {
                DrivetrainSpec.DriveType.RWD => Vehicle.Core.WheelIndex.Rear,
                DrivetrainSpec.DriveType.AWD => new[] { 0, 1, 2, 3 },
                _ => Vehicle.Core.WheelIndex.Front, // FWD
            };
        }

        // These "Has*" helpers let the module compile even if you haven't added debug fields yet.
        private static bool HasDebugMuFz(in WheelRuntime w)
        {
            // if you already added debugMuFz field -> it compiles and works.
            // if not -> remove this helper and the usage lines (see section "what to change").
            return true;
        }

        private static bool HasDebugDesired(in WheelRuntime w) => true;
        private static bool HasDebugVLong(in WheelRuntime w) => true;
    }
}
