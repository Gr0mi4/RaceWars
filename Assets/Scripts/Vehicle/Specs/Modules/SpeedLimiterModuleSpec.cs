using UnityEngine;
using Vehicle.Core;
using Vehicle.Modules;

namespace Vehicle.Specs.Modules
{
    [CreateAssetMenu(menuName = "Vehicle/Modules/Speed Limiter", fileName = "SpeedLimiterModuleSpec")]
    public sealed class SpeedLimiterModuleSpec : Vehicle.Specs.VehicleModuleSpec
    {
        public bool limitFlatOnly = true;
        [Range(0.1f, 3f)] public float maxSpeedMultiplier = 1f;

        public override IVehicleModule CreateModule()
            => new SpeedLimiterModule(limitFlatOnly, maxSpeedMultiplier);
    }
}
