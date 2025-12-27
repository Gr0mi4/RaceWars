using UnityEngine;

namespace Vehicle.Specs
{
    /// <summary>
    /// ScriptableObject containing steering configuration parameters.
    /// Defines steering geometry, tire grip, and yaw control characteristics.
    /// </summary>
    [CreateAssetMenu(menuName = "Vehicle/Steering Spec", fileName = "SteeringSpec")]
    public sealed class SteeringSpec : ScriptableObject
    {
        [Header("Bicycle Model")]
        /// <summary>
        /// Distance between front and rear axles (wheelbase) in meters.
        /// Typical sedan: 2.7-3.0m
        /// </summary>
        [Range(2.0f, 5.0f)]
        [Tooltip("Distance between front and rear axles (wheelbase) in meters. Typical sedan: 2.7-3.0m")]
        public float wheelbase = 2.8f;

        /// <summary>
        /// Maximum steering angle in degrees.
        /// Typical: 30-35° for realistic feel
        /// </summary>
        [Range(10f, 50f)]
        [Tooltip("Maximum steering angle in degrees. Typical: 28-32° for realistic feel")]
        public float maxSteerAngle = 32f;

        [Header("Speed-Based Steer")]
        /// <summary>
        /// Steer multiplier at high speed (0..1). 1 = no reduction.
        /// </summary>
        [Range(0.1f, 1f)]
        [Tooltip("Steer multiplier at high speed. 1 = no reduction.")]
        public float highSpeedSteerMultiplier = 0.5f;

        /// <summary>
        /// Speed (m/s) at which steer starts to reduce.
        /// </summary>
        [Min(0.1f)]
        [Tooltip("Speed (m/s) where steer reduction begins.")]
        public float steerReductionStartSpeed = 10f; // ~36 km/h

        /// <summary>
        /// Speed (m/s) where steer reduction reaches highSpeedSteerMultiplier.
        /// </summary>
        [Min(0.1f)]
        [Tooltip("Speed (m/s) where steer reduction reaches highSpeedSteerMultiplier.")]
        public float steerReductionEndSpeed = 30f; // ~108 km/h

        [Header("Low-Speed Yaw Scaling")]
        /// <summary>
        /// Steer/yaw multiplier at very low speed (0..1). Lower = less agile at crawl.
        /// </summary>
        [Range(0.1f, 1f)]
        [Tooltip("Steer/yaw multiplier at low speed. Lower = less agile when slow.")]
        public float lowSpeedYawMultiplier = 0.6f;

        /// <summary>
        /// Speed (m/s) below which low-speed multiplier is applied fully.
        /// </summary>
        [Min(0.01f)]
        [Tooltip("Speed (m/s) where low-speed reduction is strongest.")]
        public float lowSpeedYawStart = 0.5f; // 1.8 km/h

        /// <summary>
        /// Speed (m/s) where low-speed reduction fades out to 1.0.
        /// </summary>
        [Min(0.01f)]
        [Tooltip("Speed (m/s) where low-speed reduction ends.")]
        public float lowSpeedYawEnd = 8f; // ~29 km/h

        [Header("Tire Grip Coupling")]
        /// <summary>
        /// How strongly longitudinal usage (brake/throttle) reduces lateral grip.
        /// Driver-style: ~0.9-1.0 for strong coupling
        /// </summary>
        [Range(0.5f, 1.0f)]
        [Tooltip("How strongly brake/throttle reduces lateral grip. Driver-style: ~0.9-1.0 for strong coupling")]
        public float frictionCircleStrength = 0.95f;

        /// <summary>
        /// How much throttle affects friction circle (0.0-1.0).
        /// Lower = throttle affects lateral grip less.
        /// Driver-style: ~0.5-0.7 allows steering while accelerating
        /// </summary>
        [Range(0.01f, 0.3f)]
        [Tooltip("How much throttle affects friction circle. Lower = throttle affects lateral grip less")]
        public float throttleFrictionEffect = 0.1f;

        /// <summary>
        /// Grip reduction multiplier when handbrake is active.
        /// Driver-style: ~0.2 for controlled slides
        /// </summary>
        [Range(0.05f, 0.5f)]
        [Tooltip("Grip reduction multiplier when handbrake is active. Driver-style: ~0.2 for controlled slides")]
        public float handbrakeGripMultiplier = 0.2f;

        [Header("Yaw Rate Control")]
        /// <summary>
        /// How quickly yaw rate responds to steering input (response time in seconds).
        /// Driver-style: ~0.1-0.12s for realistic lag
        /// </summary>
        [Range(0.05f, 0.3f)]
        [Tooltip("How quickly yaw rate responds to steering input (response time in seconds). Driver-style: ~0.1-0.12s")]
        public float yawResponseTime = 0.11f;

        /// <summary>
        /// Maximum yaw acceleration in rad/s².
        /// Driver-style: ~10-12 for realistic limits
        /// </summary>
        [Range(5f, 25f)]
        [Tooltip("Maximum yaw acceleration in rad/s². Driver-style: ~10-12 for realistic limits")]
        public float maxYawAccel = 11f;

        [Header("Edge Cases")]
        /// <summary>
        /// Minimum forward speed (m/s) for steering calculations.
        /// Prevents division by zero and unrealistic steering at very low speeds
        /// </summary>
        [Range(0.05f, 1.0f)]
        [Tooltip("Minimum forward speed (m/s) for steering calculations. Prevents division by zero")]
        public float minForwardSpeed = 0.2f;
    }
}

