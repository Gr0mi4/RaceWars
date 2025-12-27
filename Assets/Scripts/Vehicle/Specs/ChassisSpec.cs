using UnityEngine;

namespace Vehicle.Specs
{
    /// <summary>
    /// ScriptableObject containing chassis (body) configuration parameters.
    /// Defines vehicle mass, center of mass, aerodynamics, and mass distribution.
    /// </summary>
    [CreateAssetMenu(menuName = "Vehicle/Chassis Spec", fileName = "ChassisSpec")]
    public sealed class ChassisSpec : ScriptableObject
    {
        [Header("Mass and Center of Mass")]
        /// <summary>
        /// Vehicle mass in kilograms. The rigidbody mass will be set to this value.
        /// </summary>
        [Min(1f)]
        [Tooltip("Vehicle mass in kilograms")]
        public float mass = 800f;

        /// <summary>
        /// Center of mass offset from the transform origin. Lower Y values make the vehicle more stable.
        /// </summary>
        [Tooltip("Center of mass offset from transform origin. Lower Y = more stable")]
        public Vector3 centerOfMass = new Vector3(0f, -0.4f, 0f);

        [Header("Mass Distribution")]
        /// <summary>
        /// Front/rear mass distribution (0.0 = all rear, 1.0 = all front, 0.5 = balanced).
        /// Typical values: 0.45-0.55 for most cars.
        /// </summary>
        [Range(0.0f, 1.0f)]
        [Tooltip("Front/rear mass distribution (0.0=rear, 1.0=front, 0.5=balanced). Typical: 0.45-0.55")]
        public float frontRearDistribution = 0.5f;

        /// <summary>
        /// Left/right mass distribution (0.0 = all right, 1.0 = all left, 0.5 = balanced).
        /// Typically 0.5 for symmetric vehicles.
        /// </summary>
        [Range(0.0f, 1.0f)]
        [Tooltip("Left/right mass distribution (0.0=right, 1.0=left, 0.5=balanced). Typically 0.5")]
        public float leftRightDistribution = 0.5f;

        [Header("Inertia")]
        /// <summary>
        /// Moment of inertia around X axis (roll). Higher values = more resistance to rolling.
        /// </summary>
        [Min(0.1f)]
        [Tooltip("Moment of inertia around X axis (roll)")]
        public float inertiaX = 500f;

        /// <summary>
        /// Moment of inertia around Y axis (yaw). Higher values = more resistance to yaw rotation.
        /// </summary>
        [Min(0.1f)]
        [Tooltip("Moment of inertia around Y axis (yaw)")]
        public float inertiaY = 1500f;

        /// <summary>
        /// Moment of inertia around Z axis (pitch). Higher values = more resistance to pitching.
        /// </summary>
        [Min(0.1f)]
        [Tooltip("Moment of inertia around Z axis (pitch)")]
        public float inertiaZ = 500f;

        [Header("Dimensions")]
        /// <summary>
        /// Overall chassis length (m). Used for sanity checks against wheel offsets.
        /// </summary>
        [Min(0.1f)]
        [Tooltip("Chassis length in meters.")]
        public float length = 4.21f; // Golf V approx

        /// <summary>
        /// Overall chassis width (m).
        /// </summary>
        [Min(0.1f)]
        [Tooltip("Chassis width in meters.")]
        public float width = 1.78f; // Golf V approx

        /// <summary>
        /// Overall chassis height (m).
        /// </summary>
        [Min(0.1f)]
        [Tooltip("Chassis height in meters.")]
        public float height = 1.48f; // Golf V approx

        [Header("Aerodynamics")]
        /// <summary>
        /// Frontal area of the vehicle in square meters. Used for aerodynamic drag calculations.
        /// </summary>
        [Range(0.1f, 10f)]
        [Tooltip("Frontal area in square meters. Used for drag calculations")]
        public float frontArea = 2.0f;

        /// <summary>
        /// Drag coefficient (Cd). Lower values mean less aerodynamic drag. Typical range: 0.2-0.5 for cars.
        /// </summary>
        [Range(0.1f, 2.0f)]
        [Tooltip("Drag coefficient (Cd). Lower = less drag. Typical: 0.2-0.5 for cars")]
        public float dragCoefficient = 0.35f;

        /// <summary>
        /// Downforce coefficient. Positive values create downforce (negative lift).
        /// Typical values: 0.0-2.0 for road cars, 2.0-5.0+ for race cars.
        /// </summary>
        [Range(-1.0f, 10.0f)]
        [Tooltip("Downforce coefficient. Positive = downforce. Typical: 0.0-2.0 for road cars")]
        public float downforceCoefficient = 0.0f;

        /// <summary>
        /// Center of pressure offset from center of mass. Affects aerodynamic balance.
        /// </summary>
        [Tooltip("Center of pressure offset from center of mass")]
        public Vector3 centerOfPressure = Vector3.zero;
    }
}

