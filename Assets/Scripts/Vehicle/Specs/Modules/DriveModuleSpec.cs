using UnityEngine;
using Vehicle.Core;
using Vehicle.Modules;

namespace Vehicle.Specs.Modules
{
    [CreateAssetMenu(menuName = "Vehicle/Modules/State Collector", fileName = "StateCollectorModuleSpec")]
    public sealed class StateCollectorModuleSpec : Vehicle.Specs.VehicleModuleSpec
    {
        public override IVehicleModule CreateModule() => new StateCollectorModule();
    }
}
