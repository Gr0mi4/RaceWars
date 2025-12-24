using Vehicle.Core;
using Vehicle.Specs;

namespace Vehicle.UI.Telemetry
{
    /// <summary>
    /// Telemetry module for displaying vehicle input data (throttle, brake, steer).
    /// </summary>
    public sealed class InputTelemetryModule : TelemetryModuleBase
    {
        public override string ModuleName => "[INPUT]";

        public InputTelemetryModule(bool enabled = true) : base(enabled)
        {
        }

        protected override string BuildDisplayText(in VehicleInput input, in VehicleState state, in VehicleContext ctx)
        {
            string text = ModuleName + "\n";
            text += FormatValue("Throttle", input.throttle);
            text += FormatValue("Brake", input.brake);
            text += FormatValue("Steer", input.steer);
            return text;
        }
    }
}

