using UnityEngine;

namespace Vehicle.Specs
{
    /// <summary>
    /// ScriptableObject containing transmission (gearbox) configuration parameters.
    /// Defines gear ratios, transmission type, shift behavior, and reverse gear.
    /// Create instances of this asset to define different transmission types for vehicles.
    /// </summary>
    [CreateAssetMenu(menuName = "Vehicle/Gearbox Spec", fileName = "GearboxSpec")]
    public sealed class GearboxSpec : ScriptableObject
    {
        /// <summary>
        /// Transmission type: Automatic or Manual.
        /// </summary>
        public enum TransmissionType
        {
            /// <summary>
            /// Automatic transmission that shifts gears automatically based on RPM and speed.
            /// </summary>
            Automatic,

            /// <summary>
            /// Manual transmission that requires manual gear shifting (for future implementation).
            /// Currently behaves like Automatic but can be extended for manual control.
            /// </summary>
            Manual
        }

        [Header("Gear Ratios")]
        /// <summary>
        /// Array of gear ratios for forward gears. Index 0 = 1st gear, index 1 = 2nd gear, etc.
        /// Higher values = lower gears (more torque, less speed).
        /// Typical values: [3.5, 2.0, 1.4, 1.0, 0.8] for a 5-speed transmission.
        /// </summary>
        [Tooltip("Gear ratios for forward gears. Index 0 = 1st gear. Typical: [3.5, 2.0, 1.4, 1.0, 0.8]")]
        public float[] gearRatios = { 3.5f, 2.0f, 1.4f, 1.0f, 0.8f };

        /// <summary>
        /// Final drive ratio (differential ratio). This is multiplied with gear ratio to get total ratio.
        /// Typical values: 3.0-4.5 for passenger cars.
        /// </summary>
        [Range(1.0f, 10.0f)]
        [Tooltip("Final drive ratio (differential). Typical: 3.0-4.5")]
        public float finalDriveRatio = 3.9f;

        /// <summary>
        /// Reverse gear ratio. Typically similar to 1st gear but negative or separate value.
        /// This is used when the vehicle is in reverse.
        /// </summary>
        [Range(1.0f, 10.0f)]
        [Tooltip("Reverse gear ratio. Typically similar to 1st gear. Typical: 3.0-4.0")]
        public float reverseGearRatio = 3.5f;

        [Header("Transmission Type")]
        /// <summary>
        /// Type of transmission: Automatic or Manual.
        /// Automatic shifts gears automatically based on RPM thresholds.
        /// Manual requires manual gear shifting (for future implementation).
        /// </summary>
        [Tooltip("Transmission type: Automatic or Manual")]
        public TransmissionType transmissionType = TransmissionType.Automatic;

        [Header("Shift Timing")]
        /// <summary>
        /// Time in seconds required to complete a gear shift.
        /// During this time, no power is transmitted to the wheels (clutch disengaged).
        /// Typical values: 0.1-0.5 seconds.
        /// </summary>
        [Range(0.01f, 2.0f)]
        [Tooltip("Time in seconds to complete a gear shift. Typical: 0.1-0.5s")]
        public float shiftTime = 0.2f;

        [Header("Automatic Shift Points (RPM)")]
        /// <summary>
        /// RPM threshold for automatic upshift (shifting to a higher gear).
        /// When RPM exceeds this value, the transmission will shift up.
        /// Typical values: 5000-7000 RPM.
        /// </summary>
        [Range(1000f, 10000f)]
        [Tooltip("RPM threshold for automatic upshift. Typical: 5000-7000 RPM")]
        public float autoShiftUpRPM = 6000f;

        /// <summary>
        /// RPM threshold for automatic downshift (shifting to a lower gear).
        /// When RPM falls below this value, the transmission will shift down.
        /// Typical values: 2000-4000 RPM.
        /// </summary>
        [Range(500f, 8000f)]
        [Tooltip("RPM threshold for automatic downshift. Typical: 2000-4000 RPM")]
        public float autoShiftDownRPM = 2500f;

        /// <summary>
        /// Minimum speed in m/s required before automatic upshift can occur.
        /// Prevents shifting too early when starting from a stop.
        /// </summary>
        [Range(0.1f, 5.0f)]
        [Tooltip("Minimum speed (m/s) before upshift can occur. Prevents early shifts.")]
        public float minSpeedForUpshift = 2.0f;

        /// <summary>
        /// Validates the gearbox specification parameters.
        /// Called automatically by Unity when values change in the Inspector.
        /// </summary>
        private void OnValidate()
        {
            // Ensure gear ratios array is valid
            if (gearRatios == null || gearRatios.Length == 0)
            {
                gearRatios = new float[] { 3.5f, 2.0f, 1.4f, 1.0f, 0.8f };
            }

            // Ensure all gear ratios are positive
            for (int i = 0; i < gearRatios.Length; i++)
            {
                if (gearRatios[i] <= 0f)
                {
                    gearRatios[i] = 1.0f;
                }
            }

            // Ensure autoShiftUpRPM is greater than autoShiftDownRPM
            if (autoShiftUpRPM <= autoShiftDownRPM)
            {
                autoShiftUpRPM = autoShiftDownRPM + 500f;
            }

            // Ensure shiftTime is positive
            if (shiftTime <= 0f)
            {
                shiftTime = 0.2f;
            }
        }
    }
}

