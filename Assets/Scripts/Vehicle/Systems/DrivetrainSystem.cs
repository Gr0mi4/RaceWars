using UnityEngine;
using Vehicle.Core;

namespace Vehicle.Systems
{
    /// <summary>
    /// Drivetrain system that manages torque distribution between wheels.
    /// This is a placeholder for future drivetrain system implementation.
    /// </summary>
    public sealed class DrivetrainSystem : IVehicleModule
    {
        /// <summary>
        /// Initializes a new instance of the DrivetrainSystem.
        /// </summary>
        public DrivetrainSystem()
        {
        }

        /// <summary>
        /// Updates the drivetrain system.
        /// Currently empty - placeholder for future implementation.
        /// </summary>
        /// <param name="input">Current vehicle input.</param>
        /// <param name="state">Current vehicle state.</param>
        /// <param name="ctx">Vehicle context containing rigidbody, transform, and specifications.</param>
        public void Tick(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
        {
            // TODO: Implement drivetrain system
            // - Distribute torque between front/rear wheels (AWD)
            // - Handle differential behavior (open/LSD/locked)
            // - Apply torque to individual wheels
        }
    }
}

