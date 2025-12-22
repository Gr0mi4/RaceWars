using UnityEngine;
using Vehicle.Core;

namespace Vehicle.Input
{
    /// <summary>
    /// Component that provides vehicle input to the VehicleController.
    /// Reads input from an IInputSource and exposes it via CurrentInput property.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VehicleInputProvider : MonoBehaviour
    {
        /// <summary>
        /// Gets the current vehicle input. Updated every frame in Update().
        /// </summary>
        public VehicleInput CurrentInput { get; private set; }

        private IInputSource _source;

        /// <summary>
        /// Initializes the input source. Currently uses UnityInputSource by default.
        /// </summary>
        private void Awake()
        {
            _source = new UnityInputSource();
        }

        /// <summary>
        /// Updates the current input every frame by reading from the input source.
        /// </summary>
        private void Update()
        {
            CurrentInput = _source != null ? _source.ReadInput() : VehicleInput.Zero;
        }
    }
}
