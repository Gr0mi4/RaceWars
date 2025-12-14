using UnityEngine;
using Vehicle.Core;

namespace Vehicle.Modules
{
    public sealed class TelemetryModule : IVehicleModule, IVehicleCollisionListener
    {
        private readonly bool _enabled;
        private readonly float _intervalSeconds;
        private readonly bool _logCollisions;

        private float _nextTime;
        private float _prevSpeed;
        private float _prevTime;

        public TelemetryModule(bool enabled, float intervalSeconds, bool logCollisions)
        {
            _enabled = enabled;
            _intervalSeconds = Mathf.Max(0.05f, intervalSeconds);
            _logCollisions = logCollisions;
            _prevSpeed = 0f;
            _prevTime = 0f;
        }

        public void Tick(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
        {
            if (!_enabled) return;
            if (Time.time < _nextTime) return;

            _nextTime = Time.time + _intervalSeconds;

            // Get current velocity
            Vector3 velocity = RigidbodyCompat.GetVelocity(ctx.rb);
            float speed = velocity.magnitude;
            float forwardSpeed = Vector3.Dot(velocity, ctx.tr.forward);

            // Calculate acceleration (change in speed over time)
            float acceleration = 0f;
            if (_prevTime > 0f && ctx.dt > 0f)
            {
                acceleration = (speed - _prevSpeed) / ctx.dt;
            }
            _prevSpeed = speed;
            _prevTime = Time.time;

            // Calculate forces
            float motorForce = 0f;
            float dragForce = 0f;
            float dampingForce = 0f;
            float netForce = 0f;

            if (ctx.spec != null)
            {
                // Motor Force (assuming Force mode - throttle * motorForce)
                motorForce = input.throttle * ctx.spec.motorForce;

                // Aerodynamic Drag Force: F = 0.5 * ρ * Cd * A * v²
                // Using standard air density 1.225 kg/m³ (can be adjusted if needed)
                const float airDensity = 1.225f;
                if (speed > 0.1f && ctx.spec.frontArea > 0f && ctx.spec.dragCoefficient > 0f)
                {
                    dragForce = 0.5f * airDensity * ctx.spec.dragCoefficient * ctx.spec.frontArea * speed * speed;
                }

                // Damping Force: F = -damping * velocity * mass
                // Unity's linearDamping applies: F = -drag * velocity
                float linearDamping = RigidbodyCompat.GetLinearDamping(ctx.rb);
                if (speed > 0.001f)
                {
                    dampingForce = linearDamping * speed * ctx.rb.mass;
                }
            }

            // Net Force (motor - drag - damping)
            netForce = motorForce - dragForce - dampingForce;

            // Get side speed (lateral velocity - left/right movement)
            float sideSpeed = state.localVelocity.x;

            // Output all parameters in a table format, grouped by category
            Debug.Log(
                $"<color=black>=== VEHICLE TELEMETRY ===\n" +
                $"[INPUT]\n" +
                $"  Throttle: {input.throttle:F2}\n" +
                $"  Brake: {input.brake:F2}\n" +
                $"  Steer: {input.steer:F2}\n" +
                $"[MOTION]\n" +
                $"  Speed: {speed:F2} m/s\n" +
                $"  Side Speed: {sideSpeed:F2} m/s\n" +
                $"  Yaw Rate: {state.yawRate:F2} rad/s\n" +
                $"  Acceleration: {acceleration:F2} m/s²\n" +
                $"[FORCES]\n" +
                $"  Motor Force: {motorForce:F0} N\n" +
                $"  Drag Force: {dragForce:F0} N\n" +
                $"  Damping Force: {dampingForce:F0} N\n" +
                $"  Net Force: {netForce:F0} N</color>"
            );
        }

        public void OnCollisionEnter(Collision collision, in VehicleContext ctx, ref VehicleState state)
        {
            if (!_enabled || !_logCollisions) return;

            float rel = collision.relativeVelocity.magnitude;
            Debug.Log($"[Vehicle] Collision with '{collision.gameObject.name}', relativeSpeed={rel:0.00}");
        }
    }
}
