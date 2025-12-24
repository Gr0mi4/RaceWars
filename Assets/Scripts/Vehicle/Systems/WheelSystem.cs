using UnityEngine;
using Vehicle.Core;
using Vehicle.Specs;

namespace Vehicle.Systems
{
    /// <summary>
    /// Wheel system that manages wheel parameters and applies lateral grip forces.
    /// Updates wheel radius in vehicle state and applies lateral grip damping to reduce side slip.
    /// </summary>
    public sealed class WheelSystem : IVehicleModule
    {
        private readonly float _wheelRadius;
        private readonly float _sideGrip;
        private readonly float _handbrakeGripMultiplier;

        /// <summary>
        /// Initializes a new instance of the WheelSystem.
        /// </summary>
        /// <param name="wheelRadius">Radius of the wheel in meters. Must be greater than 0.01.</param>
        /// <param name="sideGrip">Lateral grip coefficient. Higher values = stronger lateral grip.</param>
        /// <param name="handbrakeGripMultiplier">Grip reduction multiplier when handbrake is active.</param>
        public WheelSystem(float wheelRadius, float sideGrip, float handbrakeGripMultiplier)
        {
            _wheelRadius = Mathf.Max(0.01f, wheelRadius);
            _sideGrip = Mathf.Max(0f, sideGrip);
            _handbrakeGripMultiplier = Mathf.Clamp01(handbrakeGripMultiplier);
        }

        /// <summary>
        /// Updates the wheel system: stores wheel radius in state and applies lateral grip forces.
        /// </summary>
        /// <param name="input">Current vehicle input containing handbrake value.</param>
        /// <param name="state">Current vehicle state (modified to store wheel radius).</param>
        /// <param name="ctx">Vehicle context containing rigidbody and transform.</param>
        public void Tick(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
        {
            // Store wheel radius in state for use by other systems
            state.wheelRadius = _wheelRadius;

            // Apply lateral grip force to reduce side slip
            ApplyLateralGrip(input, ctx);
        }

        /// <summary>
        /// Applies lateral grip force to reduce side slip (lateral velocity).
        /// Uses damping force: F = -mass * grip * velocity
        /// </summary>
        private void ApplyLateralGrip(in VehicleInput input, in VehicleContext ctx)
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
            
            // Apply lateral grip as a force to reduce side slip
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

