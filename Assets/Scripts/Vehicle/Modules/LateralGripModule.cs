using UnityEngine;
using Vehicle.Core;

namespace Vehicle.Modules
{
    public sealed class LateralGripModule : IVehicleModule
    {
        private readonly float _sideGrip;
        private readonly float _handbrakeGripMultiplier;

        public LateralGripModule(float sideGrip, float handbrakeGripMultiplier)
        {
            _sideGrip = Mathf.Max(0f, sideGrip);
            _handbrakeGripMultiplier = Mathf.Clamp01(handbrakeGripMultiplier);
        }

        public void Tick(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
        {
            // Get current velocity in world space
            Vector3 vWorld = RigidbodyCompat.GetVelocity(ctx.rb);
            
            // Convert to local space
            Vector3 vLocal = ctx.tr.InverseTransformDirection(vWorld);
            
            // Calculate effective grip (reduced when handbrake is pressed)
            float grip = _sideGrip;
            if (input.handbrake > 0.5f)
            {
                grip *= _handbrakeGripMultiplier;
            }
            
            // Apply exponential decay to lateral component (X)
            // vLocal.x *= exp(-grip * dt)
            float decayFactor = Mathf.Exp(-grip * ctx.dt);
            vLocal.x *= decayFactor;
            
            // Y (vertical) and Z (forward/backward) remain unchanged
            
            // Convert back to world space
            Vector3 vWorldNew = ctx.tr.TransformDirection(vLocal);
            RigidbodyCompat.SetVelocity(ctx.rb, vWorldNew);
        }
    }
}


