using UnityEngine;

namespace Vehicle.Input
{
    /// <summary>
    /// ScriptableObject containing key mappings for vehicle input.
    /// Allows easy customization of input keys without code changes.
    /// Create instances of this asset to define different input configurations.
    /// </summary>
    [CreateAssetMenu(menuName = "Vehicle/Input Mapping", fileName = "InputMapping")]
    public sealed class InputMapping : ScriptableObject
    {
        [Header("Movement Controls")]
        /// <summary>
        /// Unity Input axis name for throttle/brake. Positive = throttle, negative = brake.
        /// Default: "Vertical"
        /// </summary>
        [Tooltip("Unity Input axis name for throttle/brake. Positive = throttle, negative = brake.")]
        public string throttleAxis = "Vertical";

        /// <summary>
        /// Unity Input axis name for steering. Negative = left, positive = right.
        /// Default: "Horizontal"
        /// </summary>
        [Tooltip("Unity Input axis name for steering. Negative = left, positive = right.")]
        public string steerAxis = "Horizontal";

        [Header("Button Controls")]
        /// <summary>
        /// Key code for handbrake button.
        /// Default: Space
        /// </summary>
        [Tooltip("Key code for handbrake button.")]
        public KeyCode handbrakeKey = KeyCode.Space;

        [Header("Gear Shifting Controls")]
        /// <summary>
        /// Key code for shifting up (to higher gear).
        /// Default: A
        /// </summary>
        [Tooltip("Key code for shifting up (to higher gear).")]
        public KeyCode shiftUpKey = KeyCode.A;

        /// <summary>
        /// Key code for shifting down (to lower gear).
        /// Default: Z
        /// </summary>
        [Tooltip("Key code for shifting down (to lower gear).")]
        public KeyCode shiftDownKey = KeyCode.Z;
    }
}
