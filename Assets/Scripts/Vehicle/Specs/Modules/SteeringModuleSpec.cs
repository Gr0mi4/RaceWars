using UnityEngine;
using Vehicle.Core;
using Vehicle.Modules;
using Vehicle.Modules.SteeringModels;
using Vehicle.Specs.Modules.SteeringModels;

namespace Vehicle.Specs.Modules
{
    [CreateAssetMenu(menuName = "Vehicle/Modules/Steering", fileName = "SteeringModuleSpec")]
    public sealed class SteeringModuleSpec : Vehicle.Specs.VehicleModuleSpec
    {
        public enum SteeringMode
        {
            Legacy,     // Old direct rotation method (deprecated)
            Physics     // New physics-based steering model
        }

        [Header("Steering Mode")]
        [Tooltip("Legacy: Old direct rotation (deprecated). Physics: New physics-based model with grip limits.")]
        public SteeringMode steeringMode = SteeringMode.Physics;

        [Header("Legacy Mode (Deprecated)")]
        [Tooltip("Legacy mode only. Minimum speed to enable steering.")]
        [Min(0f)] public float minSpeedToSteer = 0.5f;
        
        [Tooltip("Legacy mode only. Strength multiplier for steering.")]
        [Range(0.1f, 3f)] public float strengthMultiplier = 1f;

        [Header("Physics Mode")]
        [Tooltip("Physics steering model specification. Required for Physics mode.")]
        public PhysicsSteeringModelSpec physicsModelSpec;

        public override IVehicleModule CreateModule()
        {
            switch (steeringMode)
            {
                case SteeringMode.Legacy:
                    Debug.LogWarning("[SteeringModuleSpec] Using legacy steering mode. Consider migrating to Physics mode for better control.");
                    return new SteeringModule(minSpeedToSteer, strengthMultiplier);

                case SteeringMode.Physics:
                    if (physicsModelSpec == null)
                    {
                        Debug.LogError("[SteeringModuleSpec] Physics mode requires physicsModelSpec. Falling back to legacy mode.");
                        return new SteeringModule(minSpeedToSteer, strengthMultiplier);
                    }
                    ISteeringModel model = physicsModelSpec.CreateModel();
                    return new SteeringModule(model);

                default:
                    Debug.LogError($"[SteeringModuleSpec] Unknown steering mode: {steeringMode}. Using legacy mode.");
                    return new SteeringModule(minSpeedToSteer, strengthMultiplier);
            }
        }
    }
}
