using UnityEngine;
using Vehicle.Modules.SteeringModels;

namespace Vehicle.Specs.Modules.SteeringModels
{
    [CreateAssetMenu(menuName = "Vehicle/Modules/Steering Models/Physics Steering", fileName = "PhysicsSteeringModelSpec")]
    public sealed class PhysicsSteeringModelSpec : ScriptableObject
    {
        [Header("Bicycle Model")]
        [Tooltip("Distance between front and rear axles (wheelbase) in meters. Typical sedan: 2.7-3.0m")]
        [Range(2.0f, 5.0f)] public float wheelbase = 2.8f;
        
        [Tooltip("Maximum steering angle in degrees. Typical: 30-35° for realistic feel")]
        [Range(20f, 50f)] public float maxSteerAngle = 32f;

        [Header("Tire Grip")]
        [Tooltip("Base friction coefficient (μ). Driver-style: ~0.7-0.8 for realistic tire limits")]
        [Range(0.5f, 1.5f)] public float baseMu = 0.75f;
        
        [Tooltip("How strongly longitudinal usage (brake/throttle) reduces lateral grip. Driver-style: ~0.9-1.0 for strong coupling")]
        [Range(0.5f, 1.0f)] public float frictionCircleStrength = 0.95f;
        
        [Tooltip("How much throttle affects friction circle (0.0-1.0). Lower = throttle affects lateral grip less. Driver-style: ~0.5-0.7 allows steering while accelerating")]
        [Range(0.3f, 1.0f)] public float throttleFrictionEffect = 0.6f;
        
        [Tooltip("Grip reduction multiplier when handbrake is active. Driver-style: ~0.2 for controlled slides")]
        [Range(0.05f, 0.5f)] public float handbrakeGripMultiplier = 0.2f;

        [Header("Yaw Rate Control")]
        [Tooltip("How quickly yaw rate responds to steering input (response time in seconds). Driver-style: ~0.1-0.12s for realistic lag")]
        [Range(0.05f, 0.3f)] public float yawResponseTime = 0.11f;
        
        [Tooltip("Maximum yaw acceleration in rad/s². Driver-style: ~10-12 for realistic limits")]
        [Range(5f, 25f)] public float maxYawAccel = 11f;

        [Header("Edge Cases")]
        [Tooltip("Minimum forward speed (m/s) for steering calculations. Prevents division by zero and unrealistic steering at very low speeds")]
        [Range(0.05f, 1.0f)] public float minForwardSpeed = 0.2f;

        [Header("Debug")]
        [Tooltip("Enable debug logging for tuning (only in editor)")]
        public bool enableDebugLogs = false;

        /// <summary>
        /// Validates parameters and creates a PhysicsSteeringModel instance.
        /// </summary>
        public ISteeringModel CreateModel()
        {
            // Validate and clamp parameters
            wheelbase = Mathf.Max(2.0f, wheelbase);
            maxSteerAngle = Mathf.Clamp(maxSteerAngle, 20f, 50f);
            baseMu = Mathf.Clamp(baseMu, 0.5f, 1.5f);
            frictionCircleStrength = Mathf.Clamp(frictionCircleStrength, 0.5f, 1.0f);
            throttleFrictionEffect = Mathf.Clamp(throttleFrictionEffect, 0.3f, 1.0f);
            handbrakeGripMultiplier = Mathf.Clamp(handbrakeGripMultiplier, 0.05f, 0.5f);
            yawResponseTime = Mathf.Clamp(yawResponseTime, 0.05f, 0.3f);
            maxYawAccel = Mathf.Clamp(maxYawAccel, 5f, 25f);
            minForwardSpeed = Mathf.Clamp(minForwardSpeed, 0.05f, 1.0f);

            return new PhysicsSteeringModel(this);
        }
    }
}

