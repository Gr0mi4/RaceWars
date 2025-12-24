using UnityEngine;
using Vehicle.Core;
using Vehicle.Specs;

namespace Vehicle.Systems
{
    /// <summary>
    /// Steering system that applies yaw control to the vehicle using physics-based steering.
    /// Implements bicycle model, tire grip limits, and friction circle.
    /// </summary>
    public sealed class SteeringSystem : IVehicleModule
    {
        private readonly SteeringSpec _spec;
        private const float Epsilon = 0.001f;
        private const float GravityMagnitude = 9.81f;

        /// <summary>
        /// Initializes a new instance of the SteeringSystem.
        /// </summary>
        /// <param name="spec">Steering specification containing all steering parameters.</param>
        public SteeringSystem(SteeringSpec spec)
        {
            _spec = spec;
            
            if (_spec == null)
            {
                UnityEngine.Debug.LogError("[SteeringSystem] SteeringSpec is null! Steering will be disabled.");
            }
            else
            {
                // Validate spec parameters
                if (_spec.wheelbase < Epsilon)
                {
                    UnityEngine.Debug.LogWarning($"[SteeringSystem] Invalid wheelbase ({_spec.wheelbase}), using default 2.8m");
                }
                if (_spec.baseMu < Epsilon)
                {
                    UnityEngine.Debug.LogWarning($"[SteeringSystem] Invalid baseMu ({_spec.baseMu}), using default 0.75");
                }
            }
        }

        /// <summary>
        /// Updates the steering system, applying yaw torque based on steering input.
        /// </summary>
        /// <param name="input">Current vehicle input containing steering value (-1 to 1).</param>
        /// <param name="state">Current vehicle state (modified by reference).</param>
        /// <param name="ctx">Vehicle context containing rigidbody, transform, and specifications.</param>
        public void Tick(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
        {
            if (_spec == null || ctx.rb == null)
            {
                return;
            }

            if (ApplySteering(input, state, ctx, out float yawTorque))
            {
                Vector3 torque = new Vector3(0f, yawTorque, 0f);
                ctx.rb.AddTorque(torque, ForceMode.Force);
            }
        }

        /// <summary>
        /// Applies physics-based steering using bicycle model, tire grip limits, and friction circle.
        /// Public for testing purposes.
        /// </summary>
        /// <param name="input">Vehicle input containing steering, throttle, brake, and handbrake values.</param>
        /// <param name="state">Current vehicle state containing local velocity and yaw rate.</param>
        /// <param name="ctx">Vehicle context containing rigidbody.</param>
        /// <param name="yawTorque">Output parameter for calculated yaw torque.</param>
        /// <returns>True if steering was applied, false otherwise.</returns>
        public bool ApplySteering(in VehicleInput input, in VehicleState state, in VehicleContext ctx, out float yawTorque)
        {
            yawTorque = 0f;

            // Early exit if no steering input
            if (Mathf.Abs(input.steer) < Epsilon)
            {
                return false;
            }

            // Get forward speed (local Z velocity)
            float vx = state.localVelocity.z;
            float absVx = Mathf.Abs(vx);

            // If forward speed is too low, disable steering to prevent unrealistic rotation
            if (absVx < _spec.minForwardSpeed)
            {
                return false;
            }

            // 1. Bicycle model: desired yaw rate from steer angle
            float steerAngleDeg = input.steer * _spec.maxSteerAngle;
            float steerAngleRad = steerAngleDeg * Mathf.Deg2Rad;
            
            // Use absolute forward speed for calculations (works for both forward and reverse)
            float effectiveVx = Mathf.Max(absVx, _spec.minForwardSpeed);
            float yawRateDesired = (effectiveVx / _spec.wheelbase) * Mathf.Tan(steerAngleRad);

            // 2. Base grip limit (friction-based)
            float ayMax = _spec.baseMu * GravityMagnitude;
            float yawRateMaxBase = ayMax / effectiveVx;

            // 3. Friction circle: reduce lateral grip during braking/acceleration
            float brakeUsage = Mathf.Abs(input.brake);
            float throttleUsage = Mathf.Abs(input.throttle) * _spec.throttleFrictionEffect;
            float longUsage = Mathf.Clamp01(brakeUsage + throttleUsage);
            float frictionCircleFactor = longUsage * _spec.frictionCircleStrength;
            float latGripFactor = Mathf.Sqrt(Mathf.Max(0f, 1f - frictionCircleFactor * frictionCircleFactor));
            float yawRateMax = yawRateMaxBase * latGripFactor;

            // 4. Handbrake reduces grip
            if (input.handbrake > 0.5f)
            {
                yawRateMax *= _spec.handbrakeGripMultiplier;
            }

            // 5. Clamp desired yaw rate to maximum allowed
            float yawRateTarget = Mathf.Clamp(yawRateDesired, -yawRateMax, yawRateMax);

            // 6. Yaw rate control with response time
            float yawRateError = yawRateTarget - state.yawRate;
            float yawAccel = Mathf.Clamp(yawRateError / _spec.yawResponseTime, -_spec.maxYawAccel, _spec.maxYawAccel);

            // 7. Convert yaw acceleration to torque
            // Approximate moment of inertia: I ≈ m * L² (simplified for a rod rotating about center)
            float momentOfInertia = ctx.rb.mass * _spec.wheelbase * _spec.wheelbase;
            yawTorque = yawAccel * momentOfInertia;

            // Validate output (prevent NaN/Infinity)
            if (!float.IsFinite(yawTorque))
            {
                UnityEngine.Debug.LogWarning($"[SteeringSystem] Invalid yawTorque calculated: {yawTorque}. Clamping to zero.");
                yawTorque = 0f;
                return false;
            }

            // Clamp torque to reasonable limits (safety check)
            float maxTorque = ctx.rb.mass * 1000f; // Arbitrary but reasonable limit
            yawTorque = Mathf.Clamp(yawTorque, -maxTorque, maxTorque);

            return true;
        }
    }
}

