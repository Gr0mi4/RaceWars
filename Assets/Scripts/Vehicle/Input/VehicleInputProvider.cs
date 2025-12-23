using UnityEngine;
using Vehicle.Core;

namespace Vehicle.Input
{
    /// <summary>
    /// Component that provides vehicle input to the VehicleController.
    /// Reads input from an IInputSource and exposes it via CurrentInput property.
    /// Supports configurable input mappings via InputMapping ScriptableObject.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VehicleInputProvider : MonoBehaviour
    {
        /// <summary>
        /// Input mapping configuration. If null, uses default key bindings.
        /// Create an InputMapping asset to customize key bindings.
        /// </summary>
        [Tooltip("Input mapping configuration. If null, uses default key bindings.")]
        [SerializeField] private InputMapping inputMapping;

        /// <summary>
        /// Gets the current vehicle input. Updated every frame in Update().
        /// </summary>
        public VehicleInput CurrentInput { get; private set; }

        private IInputSource _source;

        /// <summary>
        /// Initializes the input source. Uses InputMapping if provided, otherwise uses default mappings.
        /// </summary>
        private void Awake()
        {
            _source = new UnityInputSource(inputMapping);
        }

        /// <summary>
        /// Updates key state tracking. Called every frame in Update() to reliably detect key presses.
        /// </summary>
        private void Update()
        {
            // Update key state tracking (for shift buttons) - must be called from Update()
            // This detects key presses and sets flags that persist until consumed
            if (_source is UnityInputSource unitySource)
            {
                unitySource.UpdateKeyState();
            }

            // Read current input (axis values, etc.)
            // Note: shift flags are NOT reset here - they persist until consumed in FixedUpdate
            CurrentInput = _source != null ? _source.ReadInput() : VehicleInput.Zero;
        }

        /// <summary>
        /// Consumes shift button flags after they have been processed.
        /// Should be called after FixedUpdate has processed the input.
        /// </summary>
        public void ConsumeShiftFlags()
        {
            if (_source is UnityInputSource unitySource)
            {
                unitySource.ConsumeShiftFlags();
            }
        }
    }
}
