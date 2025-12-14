using UnityEngine;
using Vehicle.Core;

namespace Vehicle.Modules
{
    public sealed class SpeedLimiterModule : IVehicleModule
    {
        private readonly bool _flatOnly;
        private readonly float _maxSpeedMultiplier;

        public SpeedLimiterModule(bool flatOnly, float maxSpeedMultiplier)
        {
            _flatOnly = flatOnly;
            _maxSpeedMultiplier = Mathf.Max(0.01f, maxSpeedMultiplier);
        }

        public void Tick(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
        {
            float maxSpeed = (ctx.spec != null ? ctx.spec.maxSpeed : 0f) * _maxSpeedMultiplier;
            if (maxSpeed <= 0f) return;

            Vector3 v = RigidbodyCompat.GetVelocity(ctx.rb);
            Vector3 velocityToCheck = _flatOnly ? new Vector3(v.x, 0f, v.z) : v;
            float speed = velocityToCheck.magnitude;

            if (speed <= maxSpeed) return;

            // Apply braking force instead of directly limiting velocity
            // This works with both Velocity and Force drive modes
            // Force is proportional to speed excess: F = -k * (speed - maxSpeed) * mass * direction
            float speedExcess = speed - maxSpeed;
            float mass = ctx.rb.mass;
            
            // Brake force coefficient: higher value = stronger braking
            // Adjusted to quickly limit speed without being too aggressive
            const float brakeCoefficient = 50f;
            float brakeForce = brakeCoefficient * speedExcess * mass;
            
            // Apply force opposite to velocity direction
            Vector3 brakeDirection = -velocityToCheck.normalized;
            Vector3 brakeForceVector = brakeDirection * brakeForce;
            
            ctx.rb.AddForce(brakeForceVector, ForceMode.Force);
        }
    }
}
