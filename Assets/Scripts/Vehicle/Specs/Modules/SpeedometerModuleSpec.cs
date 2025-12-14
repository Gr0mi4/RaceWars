using UnityEngine;
using Vehicle.Core;
using Vehicle.Modules;

namespace Vehicle.Specs.Modules
{
    [CreateAssetMenu(menuName = "Vehicle/Modules/Speedometer", fileName = "SpeedometerModuleSpec")]
    public sealed class SpeedometerModuleSpec : Vehicle.Specs.VehicleModuleSpec
    {
        public override IVehicleModule CreateModule()
            => new SpeedometerModule();
    }
}

