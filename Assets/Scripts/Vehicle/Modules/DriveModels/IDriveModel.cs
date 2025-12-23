using Vehicle.Core;

namespace Vehicle.Modules.DriveModels
{
    /// <summary>
    /// Interface for drive models that determine how driving forces are applied to the vehicle.
    /// Different models (Force, Velocity) provide different driving behaviors.
    /// </summary>
    public interface IDriveModel
    {
        /// <summary>
        /// Applies driving forces to the vehicle based on throttle input.
        /// </summary>
        /// <param name="input">Current vehicle input containing throttle value.</param>
        /// <param name="state">Current vehicle state (can be modified by the drive model).</param>
        /// <param name="ctx">Vehicle context containing rigidbody and specifications.</param>
        void ApplyDrive(in VehicleInput input, ref VehicleState state, in VehicleContext ctx);
    }
}
