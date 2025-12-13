using System.Collections.Generic;

namespace Vehicle.Core
{
    public sealed class VehiclePipeline
    {
        private readonly List<IVehicleModule> _modules;

        public VehiclePipeline(List<IVehicleModule> modules)
        {
            _modules = modules ?? new List<IVehicleModule>();
        }

        public void Tick(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
        {
            for (int i = 0; i < _modules.Count; i++)
                _modules[i].Tick(input, ref state, ctx);
        }

        public void NotifyCollision(UnityEngine.Collision collision, in VehicleContext ctx, ref VehicleState state)
        {
            for (int i = 0; i < _modules.Count; i++)
            {
                if (_modules[i] is IVehicleCollisionListener listener)
                    listener.OnCollisionEnter(collision, ctx, ref state);
            }
        }
    }
}
