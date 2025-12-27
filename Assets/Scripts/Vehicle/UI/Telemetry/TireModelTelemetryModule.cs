using UnityEngine;
using Vehicle.Core;

namespace Vehicle.UI.Telemetry
{
    /// <summary>
    /// Telemetry module for displaying tire model v2 parameters (slip angle, slip ratio, forces, velocities).
    /// Essential for debugging steering and handling issues.
    /// </summary>
    public sealed class TireModelTelemetryModule : TelemetryModuleBase
    {
        public override string ModuleName => "[TIRE MODEL]";

        public TireModelTelemetryModule(bool enabled = true) : base(enabled)
        {
        }

        protected override string BuildDisplayText(in VehicleInput input, in VehicleState state, in VehicleContext ctx)
        {
            if (state.wheels == null || state.wheels.Length == 0)
            {
                return string.Empty;
            }

            string text = ModuleName + "\n";
            text += "Wheel-by-wheel tire model data:\n\n";

            // Wheel names: if 4 wheels -> WheelIndex convention, else fallback
            string[] wheelNames = { "W0", "W1", "W2", "W3" };
            if (state.wheels.Length >= 4)
                wheelNames = new[] { "FL", "RL", "FR", "RR" };
            else if (state.wheels.Length == 2)
            {
                wheelNames = new[] { "L", "R" };
            }

            for (int i = 0; i < state.wheels.Length && i < wheelNames.Length; i++)
            {
                var w = state.wheels[i];
                text += $"[{wheelNames[i]}]\n";

                // Velocities
                text += $"  vLong: {w.debugVLong:F2} m/s\n";
                text += $"  vLat:  {w.debugVLat:F2} m/s\n";

                // Slip parameters
                float alphaDeg = w.debugSlipAngleRad * Mathf.Rad2Deg;
                text += $"  α: {alphaDeg:F2}° (slip angle)\n";
                text += $"  κ: {w.debugSlipRatio:F3} (slip ratio)\n";

                // Raw forces (before combined slip + build-up)
                text += $"  FxRaw: {w.debugFxRaw:F0} N\n";
                text += $"  FyRaw: {w.debugFyRaw:F0} N\n";

                // Final forces (after combined slip + build-up)
                text += $"  FxFinal: {w.debugFxFinal:F0} N\n";
                text += $"  FyFinal: {w.debugFyFinal:F0} N\n";

                // Build-up state
                text += $"  FxPrev: {w.fxPrev:F0} N\n";
                text += $"  FyPrev: {w.fyPrev:F0} N\n";

                // Zero-lateral flag
                if (w.debugShouldZeroLateral)
                {
                    text += "  ⚠ ZERO LATERAL ACTIVE\n";
                }

                // Utilization
                text += $"  Util: {w.debugUtil:F2} ({w.debugUtil * 100f:F0}%)\n";

                text += "\n";
            }

            return text;
        }
    }
}

