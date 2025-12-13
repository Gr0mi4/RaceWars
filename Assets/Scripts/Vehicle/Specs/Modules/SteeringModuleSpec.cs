using UnityEngine;
using Vehicle.Core;
using Vehicle.Modules;

namespace Vehicle.Specs.Modules
{
    [CreateAssetMenu(menuName = "Vehicle/Modules/Steering", fileName = "SteeringModuleSpec")]
    public sealed class SteeringModuleSpec : Vehicle.Specs.VehicleModuleSpec
    {
        [Min(0f)] public float minSpeedToSteer = 0.5f;
        [Range(0.1f, 3f)] public float strengthMultiplier = 1f;

        public override IVehicleModule CreateModule()
            => new SteeringModule(minSpeedToSteer, strengthMultiplier);
    }
}
