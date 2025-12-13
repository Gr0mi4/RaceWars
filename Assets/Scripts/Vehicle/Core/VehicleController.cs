using UnityEngine;
using Vehicle.Input;
using Vehicle.Specs;

namespace Vehicle.Core
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(VehicleInputProvider))]
    public sealed class VehicleController : MonoBehaviour
    {
        [SerializeField] private VehiclePipelineSpec pipelineSpec;
        [SerializeField] private CarSpec carSpec;

        private VehiclePipeline _pipeline;
        private VehicleInputProvider _inputProvider;
        private Rigidbody _rb;
        private VehicleState _state;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _inputProvider = GetComponent<VehicleInputProvider>();

            if (carSpec != null)
            {
                ApplyRigidbodyDefaults();
            }

            if (pipelineSpec != null)
            {
                _pipeline = pipelineSpec.CreatePipeline();
            }
        }

        private void ApplyRigidbodyDefaults()
        {
            _rb.mass = Mathf.Max(_rb.mass, carSpec.minMass);
            RigidbodyCompat.SetLinearDamping(_rb, carSpec.linearDamping);
            RigidbodyCompat.SetAngularDamping(_rb, carSpec.angularDamping);
            _rb.useGravity = true;
            _rb.centerOfMass = carSpec.centerOfMass;
        }

        private void FixedUpdate()
        {
            if (_pipeline == null || _inputProvider == null || carSpec == null)
                return;

            UpdateState();
            var ctx = new VehicleContext(_rb, transform, carSpec, Time.fixedDeltaTime);
            var input = _inputProvider.CurrentInput;

            _pipeline.Tick(input, ref _state, ctx);
        }

        private void UpdateState()
        {
            _state.worldVelocity = RigidbodyCompat.GetVelocity(_rb);
            _state.localVelocity = transform.InverseTransformDirection(_state.worldVelocity);
            _state.speed = _state.localVelocity.magnitude;
            _state.yawRate = _rb.angularVelocity.y;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_pipeline == null || carSpec == null)
                return;

            var ctx = new VehicleContext(_rb, transform, carSpec, Time.fixedDeltaTime);
            _pipeline.NotifyCollision(collision, ctx, ref _state);
        }
    }
}
