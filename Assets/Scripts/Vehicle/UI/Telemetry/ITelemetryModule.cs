using UnityEngine;
using Vehicle.Core;
using Vehicle.Specs;

namespace Vehicle.UI.Telemetry
{
    /// <summary>
    /// Interface for telemetry modules that display vehicle information.
    /// Each module can be enabled/disabled independently.
    /// </summary>
    public interface ITelemetryModule
    {
        /// <summary>
        /// Gets the display name of the module (e.g., "[ENGINE]", "[GEARBOX]").
        /// </summary>
        string ModuleName
        {
            get;
        }

        /// <summary>
        /// Gets whether this module is currently enabled.
        /// </summary>
        bool IsEnabled
        {
            get;
        }

        /// <summary>
        /// Updates the module with current vehicle data and returns formatted text to display.
        /// </summary>
        /// <param name="input">Current vehicle input.</param>
        /// <param name="state">Current vehicle state.</param>
        /// <param name="ctx">Vehicle context with specifications.</param>
        /// <returns>Formatted text string to display, or empty string if module is disabled.</returns>
        string GetDisplayText(in VehicleInput input, in VehicleState state, in VehicleContext ctx);
    }
}

