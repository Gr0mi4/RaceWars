using UnityEngine;
using Vehicle.Core;
using Vehicle.Modules;

namespace Vehicle.Specs.Modules
{
    /// <summary>
    /// Specification for the WheelModule. Configures wheel radius parameter.
    /// </summary>
    [CreateAssetMenu(menuName = "Vehicle/Modules/Wheel", fileName = "WheelModuleSpec")]
    public sealed class WheelModuleSpec : Vehicle.Specs.VehicleModuleSpec
    {
        [Header("Wheel Configuration")]
        /// <summary>
        /// Radius of the wheel in meters. Used for calculating engine RPM from vehicle speed.
        /// Typical values: 0.3-0.4m for passenger cars.
        /// </summary>
        [Range(0.1f, 1.0f)]
        [Tooltip("Wheel radius in meters. Typical: 0.3-0.4m for cars")]
        public float wheelRadius = 0.3f;

        /// <summary>
        /// Creates a WheelModule with the configured wheel radius.
        /// </summary>
        /// <returns>A new WheelModule instance.</returns>
        public override IVehicleModule CreateModule()
        {
            return new WheelModule(wheelRadius);
        }
    }
}

