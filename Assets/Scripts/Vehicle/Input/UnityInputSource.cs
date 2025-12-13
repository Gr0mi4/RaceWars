using UnityEngine;
using Vehicle.Core;

namespace Vehicle.Input
{
    public sealed class UnityInputSource : IInputSource
    {
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
