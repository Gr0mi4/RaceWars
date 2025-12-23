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


        [Header("Engine System")]
        /// <summary>
        /// Engine specification containing power/torque curves and RPM characteristics.
        /// Required for engine-based drive system.
        /// </summary>
        [Tooltip("Engine specification. Required for engine-based drive system.")]
        public EngineSpec engineSpec;

        /// <summary>
        /// Gearbox specification containing gear ratios and transmission settings.
        /// Required for engine-based drive system.
        /// </summary>
        [Tooltip("Gearbox specification. Required for engine-based drive system.")]
        public GearboxSpec gearboxSpec;

        /// <summary>
        /// Wheel specification containing wheel parameters.
        /// Optional, can be used by WheelModule or EngineDriveModel.
        /// </summary>
        [Tooltip("Wheel specification. Optional, used by WheelModule or EngineDriveModel.")]
        public WheelSpec wheelSpec;

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
