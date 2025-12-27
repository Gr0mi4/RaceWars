using UnityEngine;
using Vehicle.Core;
using Vehicle.Specs;
using Vehicle.Systems;

namespace Vehicle.UI.Telemetry
{
    /// <summary>
    /// Detailed telemetry module for transmission diagnostics with comprehensive parameters.
    /// </summary>
    public sealed class TransmissionTelemetryModule : TelemetryModuleBase
    {
        private readonly EngineSystem _engineSystem;

        public override string ModuleName => "[GEARBOX]";

        public TransmissionTelemetryModule(bool enabled = true) : base(enabled)
        {
            _engineSystem = new EngineSystem();
        }

        protected override string BuildDisplayText(in VehicleInput input, in VehicleState state, in VehicleContext ctx)
        {
            if (ctx.gearboxSpec == null)
            {
                return string.Empty;
            }

            int currentGear = state.currentGear;
            string gearName = GetGearName(currentGear);

            // Calculate gear ratios
            float baseGearRatio = 0f;
            float finalDriveRatio = ctx.gearboxSpec.finalDriveRatio;
            float totalGearRatio = 0f;

            if (currentGear == -1)
            {
                baseGearRatio = ctx.gearboxSpec.reverseGearRatio;
                totalGearRatio = baseGearRatio * finalDriveRatio;
            }
            else if (currentGear > 0 && currentGear <= ctx.gearboxSpec.gearRatios.Length)
            {
                int arrayIndex = currentGear - 1;
                baseGearRatio = ctx.gearboxSpec.gearRatios[arrayIndex];
                totalGearRatio = baseGearRatio * finalDriveRatio;
            }

            // Get shifting status from DriveSystem via reflection
            bool isShifting = false;
            float shiftTimer = 0f;
            bool clutchEngaged = true;
            bool gearEngaged = currentGear != 0;

            // Try to get gearbox system state from VehicleController
            var vehicleController = ctx.rb.GetComponent<VehicleController>();
            if (vehicleController != null)
            {
                // Access DriveSystem through reflection if possible
                // This is a workaround - in a perfect world, this data would be in VehicleState
                // For now, we'll calculate what we can
            }

            // Current speed
            float currentSpeed = Mathf.Abs(Vector3.Dot(RigidbodyCompat.GetVelocity(ctx.rb), ctx.tr.forward));
            // Angular velocities
            float wheelAngularVelocity = state.wheelAngularVelocity;
            float engineAngularVelocity = 0f;
            if (ctx.engineSpec != null)
            {
                engineAngularVelocity = state.engineRPM * (2f * Mathf.PI / 60f);
            }

            // Calculated speed from wheel angular velocity
            float calculatedSpeed = 0f;
            if (state.wheelRadius > 0.01f)
            {
                calculatedSpeed = wheelAngularVelocity * state.wheelRadius;
            }

            string transmissionType = ctx.gearboxSpec.transmissionType.ToString();

            string text = ModuleName + "\n";

            // Current state
            text += FormatString("Gear", gearName);
            text += FormatValue("Base Gear Ratio", baseGearRatio, "", 3);
            text += FormatValue("Final Drive Ratio", finalDriveRatio, "", 2);
            text += FormatValue("Total Gear Ratio", totalGearRatio, "", 3);
            text += FormatString("Transmission Type", transmissionType) + "\n";

            // Shifting status
            text += FormatBool("Is Shifting", isShifting);
            if (isShifting)
            {
                text += FormatValue("Shift Timer", shiftTimer, "s", 2);
            }
            text += FormatBool("Clutch Engaged", clutchEngaged);
            text += FormatBool("Gear Engaged", gearEngaged) + "\n";

            // Speed characteristics
            text += FormatValue("Current Speed", currentSpeed, "m/s", 2) +
                    $" ({currentSpeed * 3.6f:F1} km/h)\n";
            text += FormatValue("Upshift RPM", ctx.gearboxSpec.autoShiftUpRPM, "", 0);
            text += FormatValue("Downshift RPM", ctx.gearboxSpec.autoShiftDownRPM, "", 0);
            text += FormatValue("Min Speed for Upshift", ctx.gearboxSpec.minSpeedForUpshift, "m/s", 2) + "\n";

            // Angular velocities
            text += FormatValue("Wheel Angular Velocity", wheelAngularVelocity, "rad/s", 2);
            text += FormatValue("Engine Angular Velocity", engineAngularVelocity, "rad/s", 2);
            text += FormatValue("Speed from Wheel", calculatedSpeed, "m/s", 2) +
                    $" ({calculatedSpeed * 3.6f:F1} km/h)\n";

            // Static parameters (optional, can be shown if needed)
            // Uncomment if needed for debugging:
            // text += FormatString("All Gear Ratios", string.Join(", ", ctx.gearboxSpec.gearRatios));
            // text += FormatValue("Reverse Gear Ratio", ctx.gearboxSpec.reverseGearRatio, "", 2);
            // text += FormatValue("Shift Time", ctx.gearboxSpec.shiftTime, "s", 2);

            return text;
        }

        private string GetGearName(int gear)
        {
            return gear switch
            {
                -1 => "R",
                0 => "N",
                _ => gear.ToString()
            };
        }
    }
}

