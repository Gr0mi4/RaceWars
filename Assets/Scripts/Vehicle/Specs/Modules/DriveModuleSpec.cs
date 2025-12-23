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
        [Header("Engine Configuration")]
        /// <summary>
        /// Optional multiplier for the calculated engine force. Default is 1.0.
        /// Note: EngineSpec and GearboxSpec are automatically taken from CarSpec (no need to specify here).
        /// </summary>
        [Range(0.1f, 3f)]
        [Tooltip("Multiplier for engine force. Engine/Gearbox specs come from CarSpec.")]
        public float engineForceMultiplier = 1f;

        /// <summary>
        /// Creates a DriveModule with EngineDriveModel.
        /// Engine and gearbox specs are obtained from CarSpec at runtime.
        /// </summary>
        /// <returns>A new DriveModule instance.</returns>
        public override IVehicleModule CreateModule()
        {
            // EngineSpec and GearboxSpec are taken from CarSpec via VehicleContext
            // No need to specify them here - this keeps configuration centralized in CarSpec
            IDriveModel model = new EngineDriveModel(engineForceMultiplier);
            return new DriveModule(model);
        }
    }
}
