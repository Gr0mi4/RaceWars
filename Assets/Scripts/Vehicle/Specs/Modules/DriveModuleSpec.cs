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
        
        [Header("Force Mode")]
        [Range(0.1f, 3f)] public float motorForceMultiplier = 1f;
        
        [Header("Velocity Mode")]
        [Range(0.01f, 0.5f)] public float poweredBlend = 0.25f;
        [Range(0.001f, 0.2f)] public float coastingBlend = 0.02f;

        public override IVehicleModule CreateModule()
        {
            IDriveModel model = driveMode switch
            {
                DriveMode.Force => new ForceDriveModel(motorForceMultiplier),
                DriveMode.Velocity => new VelocityDriveModel(poweredBlend, coastingBlend),
                _ => new ForceDriveModel(motorForceMultiplier)
            };
            return new DriveModule(model);
        }
    }
}
