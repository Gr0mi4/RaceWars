using UnityEngine;
using Vehicle.Core;

namespace Vehicle.Input
{
    /// <summary>
    /// Input source implementation using Unity's legacy Input system.
    /// Reads input from keyboard and gamepad using Input.GetAxisRaw and Input.GetKey.
    /// Uses InputMapping for configurable key bindings.
    /// Tracks key state changes to handle GetKeyDown properly even when called from FixedUpdate.
    /// </summary>
    public sealed class UnityInputSource : IInputSource
    {
        private readonly InputMapping _mapping;
        private bool _shiftUpPressed;
        private bool _shiftDownPressed;
        private bool _shiftUpWasPressed;
        private bool _shiftDownWasPressed;

        /// <summary>
        /// Initializes a new instance of UnityInputSource with default key mappings.
        /// </summary>
        public UnityInputSource()
        {
            // Create default mapping if none provided
            _mapping = ScriptableObject.CreateInstance<InputMapping>();
        }

        /// <summary>
        /// Initializes a new instance of UnityInputSource with the specified input mapping.
        /// </summary>
        /// <param name="mapping">Input mapping configuration. If null, uses default mappings.</param>
        public UnityInputSource(InputMapping mapping)
        {
            _mapping = mapping ?? ScriptableObject.CreateInstance<InputMapping>();
        }

        /// <summary>
        /// Updates key state. Should be called from Update() to track key presses reliably.
        /// This ensures GetKeyDown-like behavior works even when ReadInput() is called from FixedUpdate().
        /// </summary>
        public void UpdateKeyState()
        {
            // Track current key state
            bool shiftUpCurrent = UnityEngine.Input.GetKey(_mapping.shiftUpKey);
            bool shiftDownCurrent = UnityEngine.Input.GetKey(_mapping.shiftDownKey);

            // Detect key press (transition from not pressed to pressed)
            // Set flag if key was just pressed (not if it's being held)
            if (shiftUpCurrent && !_shiftUpWasPressed)
            {
                _shiftUpPressed = true;
            }
            if (shiftDownCurrent && !_shiftDownWasPressed)
            {
                _shiftDownPressed = true;
            }

            // Store current state for next frame
            _shiftUpWasPressed = shiftUpCurrent;
            _shiftDownWasPressed = shiftDownCurrent;
        }

        /// <summary>
        /// Reads input from Unity's Input system using the configured key mappings.
        /// Note: For reliable key press detection, call UpdateKeyState() from Update() before calling this.
        /// </summary>
        /// <returns>Current vehicle input values.</returns>
        public VehicleInput ReadInput()
        {
            // Read axis inputs
            float v = Mathf.Clamp(UnityEngine.Input.GetAxisRaw(_mapping.throttleAxis), -1f, 1f);
            float h = Mathf.Clamp(UnityEngine.Input.GetAxisRaw(_mapping.steerAxis), -1f, 1f);

            // Keep the same semantics as before: negative "vertical" is reverse.
            float throttle = Mathf.Max(0f, v);
            float brakeOrReverse = Mathf.Max(0f, -v);

            // Read button inputs
            float handbrake = UnityEngine.Input.GetKey(_mapping.handbrakeKey) ? 1f : 0f;

            // Use tracked key state for shift buttons (more reliable than GetKeyDown)
            float shiftUp = _shiftUpPressed ? 1f : 0f;
            float shiftDown = _shiftDownPressed ? 1f : 0f;

            // DO NOT reset flags here - they will be reset after being consumed in FixedUpdate
            // This ensures the flags persist until they are actually used

            return new VehicleInput
            {
                throttle = throttle,
                brake = brakeOrReverse,
                steer = h,
                handbrake = handbrake,
                shiftUp = shiftUp,
                shiftDown = shiftDown
            };
        }

        /// <summary>
        /// Consumes the shift button flags. Should be called after the input has been processed in FixedUpdate.
        /// This ensures one-shot behavior - each key press triggers only one shift action.
        /// </summary>
        public void ConsumeShiftFlags()
        {
            _shiftUpPressed = false;
            _shiftDownPressed = false;
        }
    }
}
