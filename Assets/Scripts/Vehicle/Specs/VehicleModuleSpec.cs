using UnityEngine;
using Vehicle.Core;

namespace Vehicle.Specs
{
    public abstract class VehicleModuleSpec : ScriptableObject
    {
        public abstract IVehicleModule CreateModule();
    }
}
