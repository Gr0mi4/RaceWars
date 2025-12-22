using UnityEngine;
using Vehicle.Input;
using Vehicle.Specs;

namespace Vehicle.Core
{
    /// <summary>
    /// Main controller component for vehicles. Manages the vehicle pipeline, state updates, and module execution.
    /// Attach this component to a GameObject with a Rigidbody to create a controllable vehicle.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class VehicleController : MonoBehaviour
    {
        /// <summary>
        /// Pipeline specification that defines which modules to use and their order.
        /// </summary>
        [SerializeField] private VehiclePipelineSpec pipelineSpec;
        
        /// <summary>
        /// Car specification containing vehicle parameters (mass, forces, aerodynamics, etc.).
        /// </summary>
        [SerializeField] private CarSpec carSpec;
        
        /// <summary>
        /// Input provider that supplies vehicle input from various sources (keyboard, gamepad, AI, etc.).
        /// </summary>
        [SerializeField] private VehicleInputProvider inputProvider;

        private VehiclePipeline _pipeline;
        private Rigidbody _rb;
        private VehicleState _state;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            
            // Use serialized field if assigned, otherwise try to get component
            if (inputProvider == null)
            {
                inputProvider = GetComponent<VehicleInputProvider>();
            }

            if (carSpec != null)
            {
                ApplyRigidbodyDefaults();
            }

            if (pipelineSpec != null)
            {
                _pipeline = pipelineSpec.CreatePipeline();
            }
        }

        /// <summary>
        /// Applies default rigidbody settings from the car specification.
        /// </summary>
        private void ApplyRigidbodyDefaults()
        {
            _rb.mass = Mathf.Max(_rb.mass, carSpec.minMass);
            RigidbodyCompat.SetLinearDamping(_rb, carSpec.linearDamping);
            RigidbodyCompat.SetAngularDamping(_rb, carSpec.angularDamping);
            _rb.useGravity = true;
            _rb.centerOfMass = carSpec.centerOfMass;
        }

        /// <summary>
        /// Called every physics update. Updates vehicle state and executes the pipeline.
        /// </summary>
        private void FixedUpdate()
        {
            if (_pipeline == null || inputProvider == null || carSpec == null)
                return;

            UpdateState();
            var ctx = new VehicleContext(_rb, transform, carSpec, Time.fixedDeltaTime);
            var input = inputProvider.CurrentInput;

            _pipeline.Tick(input, ref _state, ctx);
        }

        /// <summary>
        /// Updates the vehicle state from the current rigidbody properties.
        /// </summary>
        private void UpdateState()
        {
            _state.worldVelocity = RigidbodyCompat.GetVelocity(_rb);
            _state.localVelocity = transform.InverseTransformDirection(_state.worldVelocity);
            _state.speed = _state.localVelocity.magnitude;
            _state.yawRate = _rb.angularVelocity.y;
        }

        /// <summary>
        /// Called when the vehicle collides with another object. Notifies collision listeners in the pipeline.
        /// </summary>
        /// <param name="collision">Collision information.</param>
        private void OnCollisionEnter(Collision collision)
        {
            if (_pipeline == null || carSpec == null)
                return;

            var ctx = new VehicleContext(_rb, transform, carSpec, Time.fixedDeltaTime);
            _pipeline.NotifyCollision(collision, ctx, ref _state);
        }
    }
}
