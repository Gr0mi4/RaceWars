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
            
            // Apply lateral grip as a force instead of directly setting velocity
            // This works with both Velocity and Force drive modes
            if (Mathf.Abs(vLocal.x) > 0.001f && grip > 0f)
            {
                // Calculate lateral velocity in world space
                Vector3 lateralVelocity = ctx.Right * vLocal.x;
                
                // Apply damping force: F = -mass * grip * velocity
                // This creates exponential decay: v(t) = v0 * exp(-grip * t)
                float mass = ctx.rb.mass;
                Vector3 lateralForce = -lateralVelocity * (mass * grip);
                
                ctx.rb.AddForce(lateralForce, ForceMode.Force);
            }
        }
    }
}


