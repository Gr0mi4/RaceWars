using UnityEngine;
using Vehicle.Core;

namespace Vehicle.Specs
{
    /// <summary>
    /// Base class for all vehicle module specifications.
    /// Module specs are ScriptableObjects that define configuration parameters and create module instances.
    /// </summary>
    public abstract class VehicleModuleSpec : ScriptableObject
    {
        /// <summary>
        /// Creates a new instance of the vehicle module with the configuration from this spec.
        /// </summary>
        /// <returns>A new IVehicleModule instance, or null if creation fails.</returns>
        public abstract IVehicleModule CreateModule();
    }
}
