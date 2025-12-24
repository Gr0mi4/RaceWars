using System.Collections.Generic;
using UnityEngine;
using Vehicle.Input;
using Vehicle.Specs;
using Vehicle.Systems;
using Vehicle.Debug;

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
        /// Car specification containing vehicle parameters (mass, forces, aerodynamics, etc.).
        /// </summary>
        [SerializeField] private CarSpec carSpec;
        
        /// <summary>
        /// Input provider that supplies vehicle input from various sources (keyboard, gamepad, AI, etc.).
        /// </summary>
        [SerializeField] private VehicleInputProvider inputProvider;

        /// <summary>
        /// Air density for aerodynamic calculations (kg/m³). Default: 1.225 at sea level.
        /// </summary>
        [SerializeField] private float airDensity = 1.225f;

        /// <summary>
        /// Minimum speed (m/s) below which aerodynamic drag is not applied.
        /// </summary>
        [SerializeField] private float minSpeedForDrag = 0.1f;

        /// <summary>
        /// Enable telemetry logging for debugging.
        /// </summary>
        [SerializeField] private bool enableTelemetry = false;

        /// <summary>
        /// Telemetry logging interval in seconds.
        /// </summary>
        [SerializeField] private float telemetryInterval = 0.5f;

        /// <summary>
        /// Enable collision logging in telemetry.
        /// </summary>
        [SerializeField] private bool logCollisions = false;

        private VehiclePipeline _pipeline;
        private Rigidbody _rb;
        private VehicleState _state;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            
            // Initialize vehicle state with default values
            _state = new VehicleState
            {
                worldVelocity = Vector3.zero,
                localVelocity = Vector3.zero,
                speed = 0f,
                yawRate = 0f,
                engineRPM = 0f,
                wheelAngularVelocity = 0f,
                currentGear = 1, // Start in 1st gear (matches GearboxSystem default)
                wheelRadius = 0f, // Will be set by WheelSystem or use default from WheelSpec
                wheels = System.Array.Empty<WheelRuntime>()
            };
            
            // Use serialized field if assigned, otherwise try to get component
            if (inputProvider == null)
            {
                inputProvider = GetComponent<VehicleInputProvider>();
            }

            if (carSpec != null)
            {
                ApplyRigidbodyDefaults();
                InitializeWheelState();
                _pipeline = CreatePipeline();
            }
        }

        /// <summary>
        /// Initializes per-wheel runtime state array based on WheelSpec offsets.
        /// </summary>
        private void InitializeWheelState()
        {
            var offsets = carSpec?.wheelSpec?.wheelOffsets;
            int count = (offsets != null && offsets.Length > 0) ? offsets.Length : 0;
            _state.wheels = count > 0 ? new WheelRuntime[count] : System.Array.Empty<WheelRuntime>();
        }

        /// <summary>
        /// Applies default rigidbody settings from the car specification.
        /// </summary>
        private void ApplyRigidbodyDefaults()
        {
            // Mass and center of mass from ChassisSpec
            if (carSpec?.chassisSpec != null)
            {
                _rb.mass = Mathf.Max(_rb.mass, carSpec.chassisSpec.mass);
                _rb.centerOfMass = carSpec.chassisSpec.centerOfMass;
            }
            
            // Damping from CarSpec
            RigidbodyCompat.SetLinearDamping(_rb, carSpec.linearDamping);
            RigidbodyCompat.SetAngularDamping(_rb, carSpec.angularDamping);
            _rb.useGravity = true;
        }

        /// <summary>
        /// Called every physics update. Updates vehicle state and executes the pipeline.
        /// </summary>
        private void FixedUpdate()
        {
            if (_pipeline == null || inputProvider == null || carSpec == null)
                return;

            UpdateState();
            // Create context with all specs from CarSpec
            var ctx = new VehicleContext(
                _rb, 
                transform, 
                carSpec, 
                Time.fixedDeltaTime,
                carSpec?.engineSpec,
                carSpec?.gearboxSpec,
                carSpec?.wheelSpec,
                carSpec?.chassisSpec,
                carSpec?.steeringSpec,
                carSpec?.suspensionSpec,
                carSpec?.drivetrainSpec
            );
            var input = inputProvider.CurrentInput;

            _pipeline.Tick(input, ref _state, ctx);

            // Consume shift flags after processing - this ensures one-shot behavior
            // Each key press triggers only one shift action, even if FixedUpdate is called multiple times
            inputProvider.ConsumeShiftFlags();
        }

        /// <summary>
        /// Creates the fixed vehicle pipeline with all systems in the correct order.
        /// This defines the sequence of forces applied to all vehicles.
        /// </summary>
        /// <returns>A new VehiclePipeline with all systems configured.</returns>
        private VehiclePipeline CreatePipeline()
        {
            var modules = new List<IVehicleModule>();

            // 1. Wheel System - stores wheel radius and applies lateral grip
            if (carSpec?.wheelSpec != null)
            {
                modules.Add(new WheelSystem(
                    carSpec.wheelSpec.wheelRadius,
                    carSpec.wheelSpec.sideGrip,
                    carSpec.wheelSpec.handbrakeGripMultiplier
                ));
            }

            // 2. Drive System - applies engine/gearbox forces
            if (carSpec?.engineSpec != null && carSpec?.gearboxSpec != null)
            {
                modules.Add(new DriveSystem(forceMultiplier: 1.0f));
            }

            // 3. Steering System - applies yaw torque
            if (carSpec?.steeringSpec != null)
            {
                modules.Add(new SteeringSystem(carSpec.steeringSpec));
            }

            // 4. Aerodynamic Drag System - applies drag force
            if (carSpec?.chassisSpec != null)
            {
                modules.Add(new AerodragSystem(airDensity, minSpeedForDrag));
            }

            // 5. Suspension System - placeholder for future implementation
            if (carSpec?.suspensionSpec != null)
            {
                modules.Add(new SuspensionSystem());
            }

            // 6. Rolling Resistance System - apply per-wheel rolling drag
            if (carSpec?.wheelSpec != null)
            {
                modules.Add(new RollingResistanceSystem());
            }

            // 7. Drivetrain System - placeholder for future implementation
            if (carSpec?.drivetrainSpec != null)
            {
                modules.Add(new DrivetrainSystem());
            }

            // 8. Telemetry System - debug/utility (optional)
            if (enableTelemetry)
            {
                modules.Add(new TelemetrySystem(enableTelemetry, telemetryInterval, logCollisions));
            }

            return new VehiclePipeline(modules);
        }

        /// <summary>
        /// Updates the vehicle state from the current rigidbody properties.
        /// This replaces the old StateCollectorModule functionality.
        /// </summary>
        private void UpdateState()
        {
            Vector3 v = RigidbodyCompat.GetVelocity(_rb);
            _state.worldVelocity = v;
            _state.speed = v.magnitude;
            _state.localVelocity = transform.InverseTransformDirection(v);
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

            var ctx = new VehicleContext(
                _rb, 
                transform, 
                carSpec, 
                Time.fixedDeltaTime,
                carSpec?.engineSpec,
                carSpec?.gearboxSpec,
                carSpec?.wheelSpec,
                carSpec?.chassisSpec,
                carSpec?.steeringSpec,
                carSpec?.suspensionSpec,
                carSpec?.drivetrainSpec
            );
            _pipeline.NotifyCollision(collision, ctx, ref _state);
        }
    }
}
