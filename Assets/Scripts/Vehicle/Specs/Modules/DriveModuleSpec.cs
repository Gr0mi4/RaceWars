using UnityEngine;
using Vehicle.Core;
using Vehicle.Modules;
using Vehicle.Modules.DriveModels;

namespace Vehicle.Specs.Modules
{
    [CreateAssetMenu(menuName = "Vehicle/Modules/Drive", fileName = "DriveModuleSpec")]
    public sealed class DriveModuleSpec : Vehicle.Specs.VehicleModuleSpec
    {
        public enum DriveMode
        {
            Velocity,
            Force
        }

        public DriveMode driveMode = DriveMode.Force;
        [Range(0.1f, 3f)] public float motorForceMultiplier = 1f;

        public override IVehicleModule CreateModule()
        {
            IDriveModel model = driveMode switch
            {
                DriveMode.Force => new ForceDriveModel(motorForceMultiplier),
                DriveMode.Velocity => new VelocityDriveModel(),
                _ => new ForceDriveModel(motorForceMultiplier)
            };
            return new DriveModule(model);
        }
    }
}
