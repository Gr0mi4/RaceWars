using System.Text;
using Vehicle.Core;

namespace Vehicle.UI.Telemetry
{
    /// <summary>
    /// Telemetry module for suspension / wheel contact data.
    /// </summary>
    public sealed class SuspensionTelemetryModule : TelemetryModuleBase
    {
        public override string ModuleName => "[SUSPENSION]";

        public SuspensionTelemetryModule(bool enabled = true) : base(enabled)
        {
        }

        protected override string BuildDisplayText(in VehicleInput input, in VehicleState state, in VehicleContext ctx)
        {
            if (state.wheels == null || state.wheels.Length == 0)
            {
                return ModuleName + "\n(no wheel data)\n";
            }

            var sb = new StringBuilder();
            sb.AppendLine(ModuleName);

            for (int i = 0; i < state.wheels.Length; i++)
            {
                var w = state.wheels[i];
                sb.Append($"W{i} ");
                sb.Append(w.isGrounded ? "G " : "A "); // G = grounded, A = airborne
                sb.Append($"N:{w.normalForce:F0}N ");
                sb.Append($"C:{w.compression:F3}m ");
                sb.Append($"Ang:{w.angularVelocity:F2}rad/s ");
                sb.Append($"Y:{w.contactPoint.y:F3} ");
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}

