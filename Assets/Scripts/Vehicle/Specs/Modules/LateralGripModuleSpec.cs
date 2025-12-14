using UnityEngine;
using Vehicle.Core;
using Vehicle.Modules;

namespace Vehicle.Specs.Modules
{
    [CreateAssetMenu(menuName = "Vehicle/Modules/Lateral Grip", fileName = "LateralGripModuleSpec")]
    public sealed class LateralGripModuleSpec : Vehicle.Specs.VehicleModuleSpec
    {
        [Range(0f, 10f)] public float sideGrip = 6f;
        [Range(0f, 1f)] public float handbrakeGripMultiplier = 0.2f;

        public override IVehicleModule CreateModule()
            => new LateralGripModule(sideGrip, handbrakeGripMultiplier);
    }
}


