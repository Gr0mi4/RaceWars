namespace Vehicle.Core
{
    /// <summary>
    /// Interface for vehicle modules that process vehicle behavior each physics frame.
    /// All vehicle systems (drive, steering, aerodynamics, etc.) implement this interface.
    /// </summary>
    public interface IVehicleModule
    {
        /// <summary>
        /// Called every physics frame to update the module. Modules can read input, modify state, and apply forces.
        /// </summary>
        /// <param name="input">Current vehicle input.</param>
        /// <param name="state">Current vehicle state (can be modified).</param>
        /// <param name="ctx">Vehicle context containing rigidbody, transform, and specifications.</param>
        void Tick(in VehicleInput input, ref VehicleState state, in VehicleContext ctx);
    }

    /// <summary>
    /// Optional interface for modules that need to respond to collision events.
    /// Implement this interface in addition to IVehicleModule to receive collision notifications.
    /// </summary>
    public interface IVehicleCollisionListener
    {
        /// <summary>
        /// Called when the vehicle collides with another object.
        /// </summary>
        /// <param name="collision">Collision information from Unity.</param>
        /// <param name="ctx">Vehicle context at the time of collision.</param>
        /// <param name="state">Current vehicle state (can be modified).</param>
        void OnCollisionEnter(UnityEngine.Collision collision, in VehicleContext ctx, ref VehicleState state);
    }
}
