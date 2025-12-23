using Vehicle.Core;
using Vehicle.Modules.DriveModels;

namespace Vehicle.Modules
{
    /// <summary>
    /// Module that applies driving forces to the vehicle based on throttle input.
    /// Uses EngineDriveModel to apply realistic engine-based forces.
    /// </summary>
    public sealed class DriveModule : IVehicleModule
    {
        private readonly IDriveModel _driveModel;

        /// <summary>
        /// Initializes a new instance of the DriveModule with the specified drive model.
        /// </summary>
        /// <param name="driveModel">The drive model to use for applying forces (e.g., EngineDriveModel).</param>
        public DriveModule(IDriveModel driveModel)
        {
            _driveModel = driveModel;
        }

        /// <summary>
        /// Updates the drive module, applying driving forces based on throttle input.
        /// </summary>
        /// <param name="input">Current vehicle input containing throttle value.</param>
        /// <param name="state">Current vehicle state (modified by reference).</param>
        /// <param name="ctx">Vehicle context containing rigidbody, transform, and specifications.</param>
        public void Tick(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
        {
            _driveModel?.ApplyDrive(input, ref state, ctx);
        }
    }
}

