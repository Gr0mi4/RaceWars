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
        // TODO: Add suspension parameters when implementing suspension system
    }
}

