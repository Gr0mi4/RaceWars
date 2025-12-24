using UnityEngine;
using Vehicle.Core;

namespace Vehicle.Systems
{
    /// <summary>
    /// Suspension system that applies forces based on suspension characteristics.
    /// This is a placeholder for future suspension system implementation.
    /// </summary>
    public sealed class SuspensionSystem : IVehicleModule
    {
        /// <summary>
        /// Initializes a new instance of the SuspensionSystem.
        /// </summary>
        public SuspensionSystem()
        {
        }

        /// <summary>
        /// Updates the suspension system.
        /// Currently empty - placeholder for future implementation.
        /// </summary>
        /// <param name="input">Current vehicle input.</param>
        /// <param name="state">Current vehicle state.</param>
        /// <param name="ctx">Vehicle context containing rigidbody, transform, and specifications.</param>
        public void Tick(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
        {
            // TODO: Implement suspension system
            // - Calculate suspension forces based on wheel contact
            // - Apply spring and damping forces
            // - Handle weight transfer
        }
    }
}

