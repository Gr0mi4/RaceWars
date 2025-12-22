using UnityEngine;
using Vehicle.Core;
using Vehicle.Modules;
using Vehicle.Modules.DriveModels;

namespace Vehicle.Specs.Modules
{
    /// <summary>
    /// Specification for the DriveModule. Configures which drive model to use and its parameters.
    /// </summary>
    [CreateAssetMenu(menuName = "Vehicle/Modules/Drive", fileName = "DriveModuleSpec")]
    public sealed class DriveModuleSpec : Vehicle.Specs.VehicleModuleSpec
    {
        /// <summary>
        /// Drive mode determines how forces are applied to the vehicle.
        /// </summary>
        public enum DriveMode
        {
            /// <summary>
            /// Velocity-based driving with direct velocity interpolation (arcade-style).
            /// </summary>
            Velocity,
            /// <summary>
            /// Force-based driving using AddForce (realistic physics).
            /// </summary>
            Force
        }

        /// <summary>
        /// The drive mode to use. Force provides realistic physics, Velocity provides arcade-style control.
        /// </summary>
        public DriveMode driveMode = DriveMode.Force;
        
        [Header("Force Mode")]
        /// <summary>
        /// Multiplier for the base motor force from CarSpec. Only used in Force mode.
        /// </summary>
        [Range(0.1f, 3f)] public float motorForceMultiplier = 1f;
        
        [Header("Velocity Mode")]
        /// <summary>
        /// Blend rate when throttle or brake is applied. Higher = faster response. Only used in Velocity mode.
        /// </summary>
        [Range(0.01f, 0.5f)] public float poweredBlend = 0.25f;
        
        /// <summary>
        /// Blend rate when coasting (no input). Lower = more inertia. Only used in Velocity mode.
        /// </summary>
        [Range(0.001f, 0.2f)] public float coastingBlend = 0.02f;

        /// <summary>
        /// Creates a DriveModule with the configured drive model.
        /// </summary>
        /// <returns>A new DriveModule instance.</returns>
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
