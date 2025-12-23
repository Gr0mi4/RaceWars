using UnityEngine;
using Vehicle.Core;
using Vehicle.Specs;

namespace Vehicle.Modules
{
    /// <summary>
    /// Module that stores wheel parameters in the vehicle state.
    /// Currently stores wheel radius, but can be extended in the future for tire pressure, width, grip, etc.
    /// This module does not apply forces, it only updates state information.
    /// </summary>
    public sealed class WheelModule : IVehicleModule
    {
        private readonly float _wheelRadius;

        /// <summary>
        /// Initializes a new instance of the WheelModule.
        /// </summary>
        /// <param name="wheelRadius">Radius of the wheel in meters. Must be greater than 0.01.</param>
        public WheelModule(float wheelRadius)
        {
            _wheelRadius = Mathf.Max(0.01f, wheelRadius);
        }

        /// <summary>
        /// Updates the vehicle state with wheel radius information.
        /// This allows other modules (like EngineDriveModel) to access wheel parameters.
        /// </summary>
        /// <param name="input">Current vehicle input (not used in this module).</param>
        /// <param name="state">Current vehicle state (modified to store wheel radius).</param>
        /// <param name="ctx">Vehicle context containing rigidbody and specifications (not used in this module).</param>
        public void Tick(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
        {
            // Store wheel radius in state for use by other modules
            state.wheelRadius = _wheelRadius;
        }
    }
}

