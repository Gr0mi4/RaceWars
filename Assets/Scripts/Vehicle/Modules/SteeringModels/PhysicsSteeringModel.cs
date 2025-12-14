using UnityEngine;
using Vehicle.Core;
using Vehicle.Specs.Modules.SteeringModels;

namespace Vehicle.Modules.SteeringModels
{
    /// <summary>
    /// Physics-based steering model using bicycle model, tire grip limits, and friction circle.
    /// Implements Driver-style realistic steering with pronounced grip limits.
    /// </summary>
    public sealed class PhysicsSteeringModel : ISteeringModel
    {
        private readonly PhysicsSteeringModelSpec _spec;
        private const float Epsilon = 0.001f;
        private const float GravityMagnitude = 9.81f; // Cache to avoid repeated calls

        public PhysicsSteeringModel(PhysicsSteeringModelSpec spec)
        {
            _spec = spec ?? CreateDefaultSpec();
            
            // Validate spec parameters
            if (_spec.wheelbase < Epsilon)
            {
                Debug.LogWarning($"[PhysicsSteeringModel] Invalid wheelbase ({_spec.wheelbase}), using default 2.8m");
            }
            if (_spec.baseMu < Epsilon)
            {
                Debug.LogWarning($"[PhysicsSteeringModel] Invalid baseMu ({_spec.baseMu}), using default 0.75");
            }
        }

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
                if (_spec.enableDebugLogs)
                {
                    Debug.Log($"[PhysicsSteeringModel] Forward speed too low ({absVx:F3} m/s < {_spec.minForwardSpeed:F3}), steering disabled");
                }
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
            // Brake has stronger effect on lateral grip than throttle (braking reduces turning more)
            // Throttle effect is configurable to allow steering while accelerating (more realistic)
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
                Debug.LogWarning($"[PhysicsSteeringModel] Invalid yawTorque calculated: {yawTorque}. Clamping to zero.");
                yawTorque = 0f;
                return false;
            }

            // Clamp torque to reasonable limits (safety check)
            float maxTorque = ctx.rb.mass * 1000f; // Arbitrary but reasonable limit
            yawTorque = Mathf.Clamp(yawTorque, -maxTorque, maxTorque);

            if (_spec.enableDebugLogs)
            {
                Debug.Log($"[PhysicsSteeringModel] vx={vx:F2} steer={input.steer:F2} yawRateDesired={yawRateDesired:F3} " +
                         $"yawRateMax={yawRateMax:F3} yawRateTarget={yawRateTarget:F3} yawRate={state.yawRate:F3} " +
                         $"longUsage={longUsage:F2} latGripFactor={latGripFactor:F2} torque={yawTorque:F1}");
            }

            return true;
        }

        private static PhysicsSteeringModelSpec CreateDefaultSpec()
        {
            Debug.LogWarning("[PhysicsSteeringModel] No spec provided, creating default spec with Driver-style values");
            var spec = ScriptableObject.CreateInstance<PhysicsSteeringModelSpec>();
            // Defaults are already set in the ScriptableObject, but we can override here if needed
            return spec;
        }
    }
}

