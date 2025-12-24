using UnityEngine;
using Vehicle.Core;
using Vehicle.Specs;

namespace Vehicle.UI.Telemetry
{
    /// <summary>
    /// Base class for telemetry modules providing common functionality.
    /// </summary>
    public abstract class TelemetryModuleBase : ITelemetryModule
    {
        protected bool _enabled;

        /// <summary>
        /// Gets the display name of the module.
        /// </summary>
        public abstract string ModuleName { get; }

        /// <summary>
        /// Gets whether this module is currently enabled.
        /// </summary>
        public bool IsEnabled => _enabled;

        /// <summary>
        /// Initializes a new instance of the telemetry module.
        /// </summary>
        /// <param name="enabled">Whether the module is enabled by default.</param>
        protected TelemetryModuleBase(bool enabled = true)
        {
            _enabled = enabled;
        }

        /// <summary>
        /// Sets whether this module is enabled.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
        }

        /// <summary>
        /// Updates the module with current vehicle data and returns formatted text to display.
        /// </summary>
        public string GetDisplayText(in VehicleInput input, in VehicleState state, in VehicleContext ctx)
        {
            if (!_enabled)
            {
                return string.Empty;
            }

            return BuildDisplayText(input, state, ctx);
        }

        /// <summary>
        /// Builds the display text for this module. Override in derived classes.
        /// </summary>
        protected abstract string BuildDisplayText(in VehicleInput input, in VehicleState state, in VehicleContext ctx);

        /// <summary>
        /// Formats a value with units for display.
        /// </summary>
        protected string FormatValue(string label, float value, string unit, int decimals = 2)
        {
            string format = "F" + decimals.ToString();
            return $"  {label}: {value.ToString(format)} {unit}\n";
        }

        /// <summary>
        /// Formats a value without units for display.
        /// </summary>
        protected string FormatValue(string label, float value, int decimals = 2)
        {
            string format = "F" + decimals.ToString();
            return $"  {label}: {value.ToString(format)}\n";
        }

        /// <summary>
        /// Formats a boolean value for display.
        /// </summary>
        protected string FormatBool(string label, bool value)
        {
            return $"  {label}: {(value ? "Yes" : "No")}\n";
        }

        /// <summary>
        /// Formats a string value for display.
        /// </summary>
        protected string FormatString(string label, string value)
        {
            return $"  {label}: {value}\n";
        }
    }
}

