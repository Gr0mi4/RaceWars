using UnityEngine;
using Vehicle.Core;
using Vehicle.Modules;

namespace Vehicle.Specs.Modules
{
    [CreateAssetMenu(menuName = "Vehicle/Modules/Telemetry", fileName = "TelemetryModuleSpec")]
    public sealed class TelemetryModuleSpec : Vehicle.Specs.VehicleModuleSpec
    {
        public bool enabled = false;
        [Min(0.05f)] public float intervalSeconds = 0.25f;
        public bool logCollisions = true;

        public override IVehicleModule CreateModule()
            => new TelemetryModule(enabled, intervalSeconds, logCollisions);
    }
}
