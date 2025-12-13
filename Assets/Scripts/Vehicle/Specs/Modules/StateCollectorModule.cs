using UnityEngine;
using Vehicle.Core;

namespace Vehicle.Modules
{
    public sealed class StateCollectorModule : IVehicleModule
    {
        public void Tick(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
        {
            Vector3 v = RigidbodyCompat.GetVelocity(ctx.rb);

            state.worldVelocity = v;
            state.speed = v.magnitude;
            state.localVelocity = ctx.tr.InverseTransformDirection(v);

            // yaw rate (approx) from rigidbody angular velocity
            state.yawRate = ctx.rb.angularVelocity.y;
        }
    }
}
