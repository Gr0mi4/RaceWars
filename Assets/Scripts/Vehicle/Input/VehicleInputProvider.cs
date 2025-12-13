using UnityEngine;
using Vehicle.Core;

namespace Vehicle.Input
{
    [DisallowMultipleComponent]
    public sealed class VehicleInputProvider : MonoBehaviour
    {
        public VehicleInput CurrentInput { get; private set; }

        private IInputSource _source;

        private void Awake()
        {
            _source = new UnityInputSource();
        }

        private void Update()
        {
            CurrentInput = _source != null ? _source.ReadInput() : VehicleInput.Zero;
        }
    }
}
