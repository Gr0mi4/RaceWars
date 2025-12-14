using UnityEngine;
using Vehicle.Core;

namespace Vehicle.Modules
{
    public sealed class SpeedometerModule : IVehicleModule
    {
        // This module collects speed data and makes it available for UI
        // The actual data is already collected by StateCollectorModule
        // This module can be used for additional processing if needed
        
        public void Tick(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
        {
            // Speed data is already in state, collected by StateCollectorModule
            // This module can be extended for additional speed calculations if needed
        }
    }
}

