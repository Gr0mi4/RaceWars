using UnityEngine;

namespace Vehicle.Specs
{
    /// <summary>
    /// ScriptableObject containing all vehicle configuration parameters.
    /// Create instances of this asset to define different vehicle types or tuning presets.
    /// </summary>
    [CreateAssetMenu(menuName = "Vehicle/Car Spec", fileName = "CarSpec")]
    public sealed class CarSpec : ScriptableObject
    {
        [Header("Rigidbody Defaults")]
        /// <summary>
        /// Minimum mass of the vehicle in kilograms. The rigidbody mass will be set to at least this value.
        /// </summary>
        [Min(1f)] public float minMass = 800f;
        
        /// <summary>
        /// Linear damping (drag) applied to the rigidbody. Higher values reduce velocity faster.
        /// </summary>
        [Min(0f)] public float linearDamping = 0.05f;
        
        /// <summary>
        /// Angular damping applied to the rigidbody. Higher values reduce rotation faster.
        /// </summary>
        [Min(0f)] public float angularDamping = 0.5f;
        
        /// <summary>
        /// Center of mass offset from the transform origin. Lower Y values make the vehicle more stable.
        /// </summary>
        public Vector3 centerOfMass = new Vector3(0f, -0.4f, 0f);

        [Header("Tuning")]
        /// <summary>
        /// Base motor force in Newtons. Used by ForceDriveModel to calculate driving force.
        /// </summary>
        [Min(0f)] public float motorForce = 12000f;
        
        /// <summary>
        /// Base steering strength. Used by legacy steering systems (deprecated in favor of physics-based steering).
        /// </summary>
        [Min(0f)] public float steerStrength = 120f;
        
        /// <summary>
        /// Maximum speed limit in meters per second. Used by SpeedLimiterModule.
        /// </summary>
        [Min(0f)] public float maxSpeed = 25f;

        [Header("Aerodynamics")]
        /// <summary>
        /// Frontal area of the vehicle in square meters. Used for aerodynamic drag calculations.
        /// </summary>
        [Range(0.1f, 10f)] public float frontArea = 2.0f;
        
        /// <summary>
        /// Drag coefficient (Cd). Lower values mean less aerodynamic drag. Typical range: 0.2-0.5 for cars.
        /// </summary>
        [Range(0.1f, 2.0f)] public float dragCoefficient = 0.35f;
    }
}
