using UnityEngine;
using Vehicle.Core;

namespace Vehicle.Input
{
    /// <summary>
    /// Input source implementation using Unity's legacy Input system.
    /// Reads input from keyboard and gamepad using Input.GetAxisRaw and Input.GetKey.
    /// </summary>
    public sealed class UnityInputSource : IInputSource
    {
        /// <summary>
        /// Reads input from Unity's Input system.
        /// Vertical axis: positive = throttle, negative = brake/reverse.
        /// Horizontal axis: left = -1, right = 1.
        /// Space key: handbrake.
        /// </summary>
        /// <returns>Current vehicle input values.</returns>
        public VehicleInput ReadInput()
        {
            float v = Mathf.Clamp(UnityEngine.Input.GetAxisRaw("Vertical"), -1f, 1f);
            float h = Mathf.Clamp(UnityEngine.Input.GetAxisRaw("Horizontal"), -1f, 1f);

            // Keep the same semantics as before: negative "vertical" is reverse.
            float throttle = Mathf.Max(0f, v);
            float brakeOrReverse = Mathf.Max(0f, -v);

            return new VehicleInput
            {
                throttle = throttle,
                brake = brakeOrReverse,
                steer = h,
                handbrake = UnityEngine.Input.GetKey(KeyCode.Space) ? 1f : 0f
            };
        }
    }
}
