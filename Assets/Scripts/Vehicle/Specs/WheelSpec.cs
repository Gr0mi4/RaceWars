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


