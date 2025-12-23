using UnityEngine;

namespace Vehicle.Specs
{
    /// <summary>
    /// ScriptableObject containing wheel configuration parameters.
    /// Create instances of this asset to define different wheel specifications for vehicles.
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
    }
}

