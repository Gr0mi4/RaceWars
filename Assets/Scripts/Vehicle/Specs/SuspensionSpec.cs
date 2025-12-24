using UnityEngine;

namespace Vehicle.Specs
{
    /// <summary>
    /// ScriptableObject containing suspension configuration parameters.
    /// This is a placeholder for future suspension system implementation.
    /// Future parameters will include:
    /// - Spring stiffness (front/rear)
    /// - Damping coefficients (front/rear)
    /// - Suspension travel (compression/rebound)
    /// - Anti-dive and anti-squat coefficients
    /// - Camber, toe, caster angles
    /// </summary>
    [CreateAssetMenu(menuName = "Vehicle/Suspension Spec", fileName = "SuspensionSpec")]
    public sealed class SuspensionSpec : ScriptableObject
    {
        [Header("Spring/Damper (per wheel, shared)")]
        /// <summary>
        /// Spring rate (N/m). Golf V ballpark: 25k-35k.
        /// </summary>
        [Min(0f)]
        [Tooltip("Spring rate (N/m). Golf V: ~28k N/m.")]
        public float spring = 70000f;

        /// <summary>
        /// Damping coefficient (N*s/m). Street car: ~0.2 of spring magnitude.
        /// </summary>
        [Min(0f)]
        [Tooltip("Damper coefficient (N*s/m). Golf V: ~6000 N*s/m.")]
        public float damper = 12000f;

        [Header("Travel")]
        /// <summary>
        /// Resting length of the suspension (m).
        /// </summary>
        [Min(0f)]
        [Tooltip("Resting length (m). Golf V: ~0.32 m.")]
        public float restLength = 0.25f;

        /// <summary>
        /// Maximum compression travel (m).
        /// </summary>
        [Min(0f)]
        [Tooltip("Max compression (m). Golf V: ~0.14 m.")]
        public float maxCompression = 0.25f;

        /// <summary>
        /// Maximum droop (extension) travel (m).
        /// </summary>
        [Min(0f)]
        [Tooltip("Max droop (m). Golf V: ~0.12 m.")]
        public float maxDroop = 0.15f;

        [Header("Force Limits")]
        /// <summary>
        /// Maximum damper force magnitude (N) to avoid spikes.
        /// </summary>
        [Min(0f)]
        [Tooltip("Max damper force (N) to clamp spikes.")]
        public float maxDamperForce = 20000f;

        /// <summary>
        /// Maximum total suspension force per wheel (N).
        /// </summary>
        [Min(0f)]
        [Tooltip("Max total suspension force per wheel (N).")]
        public float maxForce = 40000f;
    }
}

