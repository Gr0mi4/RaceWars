using UnityEngine;
using Vehicle.Core;
using Vehicle.Modules;
using Vehicle.Modules.SteeringModels;
using Vehicle.Specs.Modules.SteeringModels;

namespace Vehicle.Specs.Modules
{
    /// <summary>
    /// Specification for the SteeringModule. Configures the physics-based steering model.
    /// </summary>
    [CreateAssetMenu(menuName = "Vehicle/Modules/Steering", fileName = "SteeringModuleSpec")]
    public sealed class SteeringModuleSpec : Vehicle.Specs.VehicleModuleSpec
    {
        [Header("Physics Steering Model")]
        /// <summary>
        /// Physics steering model specification. Required for steering to work.
        /// </summary>
        [Tooltip("Physics steering model specification. Required.")]
        public PhysicsSteeringModelSpec physicsModelSpec;

        /// <summary>
        /// Creates a SteeringModule with the configured physics steering model.
        /// </summary>
        /// <returns>A new SteeringModule instance, or null if physicsModelSpec is not assigned or creation fails.</returns>
        public override IVehicleModule CreateModule()
        {
            if (physicsModelSpec == null)
            {
                Debug.LogError("[SteeringModuleSpec] Physics steering model spec is required but not assigned. Steering will be disabled.");
                return null;
            }

            ISteeringModel model = physicsModelSpec.CreateModel();
            if (model == null)
            {
                Debug.LogError("[SteeringModuleSpec] Failed to create steering model. Steering will be disabled.");
                return null;
            }

            return new SteeringModule(model);
        }
    }
}
