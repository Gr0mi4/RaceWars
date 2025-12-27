using Vehicle.Core;
using Vehicle.Specs;

namespace Vehicle.UI.Telemetry
{
    /// <summary>
    /// Telemetry module for displaying wheel data.
    /// </summary>
    public sealed class WheelsTelemetryModule : TelemetryModuleBase
    {
        public override string ModuleName => "[WHEELS]";

        public WheelsTelemetryModule(bool enabled = true) : base(enabled)
        {
        }

        protected override string BuildDisplayText(in VehicleInput input, in VehicleState state, in VehicleContext ctx)
        {
            if (state.wheelRadius <= 0.01f)
            {
                return string.Empty;
            }

            string text = ModuleName + "\n";
            text += FormatValue("Wheel Radius", state.wheelRadius, "m", 3);
            text += FormatValue("Wheel Angular Velocity", state.wheelAngularVelocity, "rad/s", 2);

            // Calculate speed from wheel angular velocity
            float speedFromWheel = state.wheelAngularVelocity * state.wheelRadius;
            text += FormatValue("Speed from Wheel", speedFromWheel, "m/s", 2) + $" ({speedFromWheel * 3.6f:F1} km/h)\n";

            return text;
        }
    }
}

