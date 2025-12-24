using UnityEngine;

namespace Vehicle.Specs
{
    /// <summary>
    /// ScriptableObject containing all vehicle configuration parameters.
    /// References to all component specifications (engine, gearbox, chassis, steering, etc.).
    /// Create instances of this asset to define different vehicle types or tuning presets.
    /// </summary>
    [CreateAssetMenu(menuName = "Vehicle/Car Spec", fileName = "CarSpec")]
    public sealed class CarSpec : ScriptableObject
    {
        [Header("Rigidbody Settings")]
        /// <summary>
        /// Linear damping (drag) applied to the rigidbody. Higher values reduce velocity faster.
        /// </summary>
        [Min(0f)]
        [Tooltip("Linear damping applied to rigidbody. Higher = faster velocity reduction")]
        public float linearDamping = 0.05f;
        
        /// <summary>
        /// Angular damping applied to the rigidbody. Higher values reduce rotation faster.
        /// </summary>
        [Min(0f)]
        [Tooltip("Angular damping applied to rigidbody. Higher = faster rotation reduction")]
        public float angularDamping = 0.5f;

        [Header("Component Specifications")]
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
        /// Wheel and tire specification containing wheel dimensions and tire properties.
        /// </summary>
        [Tooltip("Wheel and tire specification.")]
        public WheelSpec wheelSpec;

        /// <summary>
        /// Chassis specification containing mass, center of mass, aerodynamics, and mass distribution.
        /// </summary>
        [Tooltip("Chassis specification containing mass, center of mass, and aerodynamics.")]
        public ChassisSpec chassisSpec;

        /// <summary>
        /// Steering specification containing steering geometry, tire grip, and yaw control.
        /// </summary>
        [Tooltip("Steering specification containing steering geometry and tire grip.")]
        public SteeringSpec steeringSpec;

        /// <summary>
        /// Suspension specification containing spring, damping, and suspension geometry parameters.
        /// Optional - placeholder for future suspension system implementation.
        /// </summary>
        [Tooltip("Suspension specification. Optional - placeholder for future implementation.")]
        public SuspensionSpec suspensionSpec;

        /// <summary>
        /// Drivetrain specification containing drive type, torque distribution, and differential settings.
        /// Optional - placeholder for future drivetrain system implementation.
        /// </summary>
        [Tooltip("Drivetrain specification. Optional - placeholder for future implementation.")]
        public DrivetrainSpec drivetrainSpec;
    }
}
