using UnityEngine;
using Vehicle.Core;

namespace Vehicle.Modules.DriveModels
{
    public sealed class VelocityDriveModel : IDriveModel
    {
        private const float BlendFactor = 0.25f;

        public void ApplyDrive(in VehicleInput input, in VehicleState state, in VehicleContext ctx)
        {
            float maxSpeed = ctx.spec != null ? ctx.spec.maxSpeed : 25f;
            Vector3 desired = ctx.Forward * (input.throttle * maxSpeed);
            
            Vector3 currentVel = RigidbodyCompat.GetVelocity(ctx.rb);
            desired.y = currentVel.y; // keep vertical motion (gravity/jumps)

            Vector3 blended = Vector3.Lerp(currentVel, desired, BlendFactor);
            RigidbodyCompat.SetVelocity(ctx.rb, blended);
        }
    }
}

