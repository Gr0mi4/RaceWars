using UnityEngine;

namespace Vehicle.Specs
{
    /// <summary>
    /// ScriptableObject containing wheel and tire configuration parameters.
    /// Defines wheel dimensions and tire characteristics (friction, stiffness, pressure).
    /// </summary>
    [CreateAssetMenu(menuName = "Vehicle/Wheel Spec", fileName = "WheelSpec")]
    public sealed class WheelSpec : ScriptableObject
    {
        [Header("Wheel Dimensions")]
        /// <summary>
        /// Radius of the wheel in meters. Used for calculating RPM from vehicle speed.
        /// Typical values: 0.3-0.4m for passenger cars, 0.25-0.35m for sports cars.
        /// </summary>
        [Range(0.1f, 1.0f)]
        [Tooltip("Wheel radius in meters. Typical: 0.3-0.4m for cars")]
        public float wheelRadius = 0.3f;

        /// <summary>
        /// Wheel mount points relative to the vehicle origin (local space).
        /// Order: 0 = Front-Left, 1 = Front-Right, 2 = Rear-Left, 3 = Rear-Right.
        /// These are physics points, not tied to mesh vertices.
        /// </summary>
        [Tooltip("Wheel mount points (local). Order: FL, FR, RL, RR.")]
        public Vector3[] wheelOffsets = new Vector3[4]
        {
            new Vector3(-0.77f, -0.20f,  1.29f), // FL
            new Vector3( 0.77f, -0.20f,  1.29f), // FR
            new Vector3(-0.76f, -0.20f, -1.29f), // RL
            new Vector3( 0.76f, -0.20f, -1.29f)  // RR
        };

        [Header("Tire Properties")]
        /// <summary>
        /// Tire friction coefficient on dry surface (μ).
        /// Typical values: 0.7-0.9 for road tires, 0.9-1.2 for performance tires.
        /// </summary>
        [Range(0.1f, 2.0f)]
        [Tooltip("Tire friction coefficient on dry surface (μ). Typical: 0.7-0.9 for road tires")]
        public float dryFrictionCoefficient = 0.8f;

        /// <summary>
        /// Tire friction coefficient on wet surface (μ).
        /// Typically 60-80% of dry friction.
        /// </summary>
        [Range(0.1f, 1.5f)]
        [Tooltip("Tire friction coefficient on wet surface (μ). Typically 60-80% of dry friction")]
        public float wetFrictionCoefficient = 0.5f;

        /// <summary>
        /// Base friction coefficient used across systems (single source of truth for μ).
        /// Defaults to dry tire friction.
        /// </summary>
        [Range(0.1f, 2.0f)]
        [Tooltip("Base friction coefficient μ (used by steering/drive).")]
        public float friction = 0.9f;

        /// <summary>
        /// Rolling resistance coefficient Crr. Typical asphalt: 0.01-0.02.
        /// Used to apply a small resisting torque/force at each wheel.
        /// </summary>
        [Range(0.001f, 0.05f)]
        [Tooltip("Rolling resistance coefficient Crr. Typical asphalt: 0.01-0.02.")]
        public float rollingResistance = 0.015f;

        /// <summary>
        /// Tire sidewall stiffness. Affects lateral grip and response.
        /// Higher values = stiffer tire, more responsive but less grip at limits.
        /// </summary>
        [Range(0.1f, 10.0f)]
        [Tooltip("Tire sidewall stiffness. Higher = stiffer, more responsive but less grip at limits")]
        public float sidewallStiffness = 1.0f;

        /// <summary>
        /// Tire pressure in bar (atmospheric pressure = 1.0 bar).
        /// Typical values: 2.0-2.5 bar for passenger cars.
        /// </summary>
        [Range(1.0f, 5.0f)]
        [Tooltip("Tire pressure in bar. Typical: 2.0-2.5 bar for passenger cars")]
        public float tirePressure = 2.2f;

        [Header("Tire Dimensions")]
        /// <summary>
        /// Tire width in meters.
        /// Typical values: 0.15-0.25m for passenger cars.
        /// </summary>
        [Range(0.1f, 0.5f)]
        [Tooltip("Tire width in meters. Typical: 0.15-0.25m for passenger cars")]
        public float tireWidth = 0.2f;

        /// <summary>
        /// Tire aspect ratio (sidewall height / width * 100).
        /// Typical values: 40-65 for passenger cars.
        /// </summary>
        [Range(30f, 80f)]
        [Tooltip("Tire aspect ratio (sidewall height / width * 100). Typical: 40-65 for passenger cars")]
        public float aspectRatio = 55f;

        [Header("Lateral Grip")]
        /// <summary>
        /// Lateral grip coefficient. Higher values = stronger lateral grip (less side slip).
        /// This controls how quickly lateral velocity is damped.
        /// Typical values: 4-8 for realistic feel.
        /// </summary>
        [Range(0f, 10f)]
        [Tooltip("Lateral grip coefficient. Higher = stronger lateral grip. Typical: 4-8")]
        public float sideGrip = 6f;

        /// <summary>
        /// Grip reduction multiplier when handbrake is active.
        /// Lower values = more sliding when handbrake is pressed.
        /// Typical values: 0.1-0.3 for controlled slides.
        /// </summary>
        [Range(0f, 1f)]
        [Tooltip("Grip reduction multiplier when handbrake is active. Typical: 0.1-0.3 for controlled slides")]
        public float handbrakeGripMultiplier = 0.2f;
    }
}


