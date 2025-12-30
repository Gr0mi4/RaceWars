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
        /// Mass of a single wheel+tire assembly in kg.
        /// Used for calculating wheel moment of inertia (I = k * m * R²).
        /// Typical: 15-20kg for passenger cars, 20-30kg for sports/performance cars.
        /// </summary>
        [Range(5f, 50f)]
        [Tooltip("Wheel+tire mass in kg. Typical: 15-25kg")]
        public float wheelMass = 18f;

        /// <summary>
        /// Inertia shape coefficient for wheel moment of inertia calculation.
        /// 0.5 = solid disc (mass uniformly distributed), 1.0 = thin ring (all mass at rim).
        /// Real wheels are typically 0.7-0.9 because most mass is in the tire and rim.
        /// </summary>
        [Range(0.5f, 1.0f)]
        [Tooltip("Inertia coefficient: 0.5=disc, 1.0=ring. Typical wheel: 0.8")]
        public float wheelInertiaCoefficient = 0.8f;

        /// <summary>
        /// Wheel mount points relative to the vehicle origin (local space).
        /// Order (IMPORTANT - matches WheelIndex):
        /// 0 = Front-Left, 1 = Rear-Left, 2 = Front-Right, 3 = Rear-Right.
        /// These are physics points, not tied to mesh vertices.
        /// </summary>
        [Tooltip("Wheel mount points (local). Order: FL, RL, FR, RR (WheelIndex convention).")]
        public Vector3[] wheelOffsets = new Vector3[4]
        {
            new Vector3(-0.77f, -0.20f,  1.29f), // FL
            new Vector3(-0.76f, -0.20f, -1.29f), // RL
            new Vector3( 0.77f, -0.20f,  1.29f), // FR
            new Vector3( 0.76f, -0.20f, -1.29f)  // RR
        };

        [Header("Tire Properties (currently used)")]
        /// <summary>
        /// Base friction coefficient used across systems (single source of truth for μ).
        /// </summary>
        [Range(0.1f, 2.0f)]
        [Tooltip("Base friction coefficient μ (used by steering/drive).")]
        public float friction = 0.9f;
    }
}


