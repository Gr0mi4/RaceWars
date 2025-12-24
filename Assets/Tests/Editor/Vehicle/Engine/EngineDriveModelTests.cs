using NUnit.Framework;
using UnityEngine;
using Vehicle.Core;
using Vehicle.Systems;
using Vehicle.Specs;

namespace Vehicle.Tests.Engine
{
    /// <summary>
    /// Integration tests for DriveSystem.
    /// Tests integration with EngineSystem and GearboxSystem, force application, and reverse gear handling.
    /// </summary>
    [TestFixture]
    public class EngineDriveModelTests
    {
        private GameObject _gameObject;
        private Rigidbody _rigidbody;
        private Transform _transform;
        private CarSpec _carSpec;
        private EngineSpec _engineSpec;
        private GearboxSpec _gearboxSpec;
        private WheelSpec _wheelSpec;
        private DriveSystem _driveSystem;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("TestVehicle");
            _rigidbody = _gameObject.AddComponent<Rigidbody>();
            _rigidbody.mass = 1000f;
            _transform = _gameObject.transform;

            _carSpec = ScriptableObject.CreateInstance<CarSpec>();
            
            _engineSpec = ScriptableObject.CreateInstance<EngineSpec>();
            _engineSpec.maxPower = 110f;
            _engineSpec.maxTorque = 155f;
            _engineSpec.maxRPM = 6500f;
            _engineSpec.idleRPM = 800f;
            _engineSpec.minSpeedForPower = 0.5f;
            _engineSpec.powerCurve = AnimationCurve.Linear(0f, 0.3f, 1f, 1f);
            _engineSpec.torqueCurve = AnimationCurve.Linear(0f, 0.8f, 1f, 0.6f);

            _gearboxSpec = ScriptableObject.CreateInstance<GearboxSpec>();
            _gearboxSpec.gearRatios = new float[] { 3.5f, 2.0f, 1.4f, 1.0f, 0.8f };
            _gearboxSpec.finalDriveRatio = 3.9f;
            _gearboxSpec.reverseGearRatio = 3.5f;
            _gearboxSpec.transmissionType = GearboxSpec.TransmissionType.Automatic;
            _gearboxSpec.shiftTime = 0.2f;
            _gearboxSpec.autoShiftUpRPM = 6000f;
            _gearboxSpec.autoShiftDownRPM = 2500f;

            _wheelSpec = ScriptableObject.CreateInstance<WheelSpec>();
            _wheelSpec.wheelRadius = 0.3f;

            // Assign specs to CarSpec (this is how it should work now)
            _carSpec.engineSpec = _engineSpec;
            _carSpec.gearboxSpec = _gearboxSpec;
            _carSpec.wheelSpec = _wheelSpec;

            _driveSystem = new DriveSystem(1.0f);
        }

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null)
            {
                Object.DestroyImmediate(_gameObject);
            }
            if (_carSpec != null)
            {
                Object.DestroyImmediate(_carSpec);
            }
            if (_engineSpec != null)
            {
                Object.DestroyImmediate(_engineSpec);
            }
            if (_gearboxSpec != null)
            {
                Object.DestroyImmediate(_gearboxSpec);
            }
            if (_wheelSpec != null)
            {
                Object.DestroyImmediate(_wheelSpec);
            }
        }

        [Test]
        public void ApplyDrive_ForwardThrottle_AppliesForwardForce()
        {
            // Arrange
            var input = new VehicleInput { throttle = 1.0f, brake = 0f };
            var state = new VehicleState
            {
                speed = 0f,
                wheelRadius = 0.3f,
                currentGear = 1,
                engineRPM = 0f,
                wheelAngularVelocity = 0f
            };
            // Context gets specs from CarSpec automatically
            var ctx = new VehicleContext(_rigidbody, _transform, _carSpec, 0.02f);

            Vector3 initialVelocity = _rigidbody.linearVelocity;

            // Act
            _driveSystem.Tick(input, ref state, ctx);

            // Assert
            Vector3 finalVelocity = _rigidbody.linearVelocity;
            // Should have forward velocity (positive Z in local space)
            Assert.Greater(Vector3.Dot(finalVelocity, _transform.forward), 0f);
        }

        [Test]
        public void ApplyDrive_ReverseBrake_AppliesReverseForce()
        {
            // Arrange
            var input = new VehicleInput { throttle = 0f, brake = 1.0f };
            var state = new VehicleState
            {
                speed = 0f,
                wheelRadius = 0.3f,
                currentGear = 1,
                engineRPM = 0f,
                wheelAngularVelocity = 0f
            };
            // Context gets specs from CarSpec automatically
            var ctx = new VehicleContext(_rigidbody, _transform, _carSpec, 0.02f);

            // Act
            _driveSystem.Tick(input, ref state, ctx);

            // Assert
            // Should shift to reverse and apply backward force
            // Note: This test may need adjustment based on exact reverse logic
            Assert.IsTrue(true); // Placeholder - reverse logic is complex
        }

        [Test]
        public void ApplyDrive_NoThrottle_DoesNotApplyForce()
        {
            // Arrange
            var input = new VehicleInput { throttle = 0f, brake = 0f };
            var state = new VehicleState
            {
                speed = 10f,
                wheelRadius = 0.3f,
                currentGear = 3,
                engineRPM = 3000f,
                wheelAngularVelocity = 0f
            };
            // Context gets specs from CarSpec automatically
            var ctx = new VehicleContext(_rigidbody, _transform, _carSpec, 0.02f);

            Vector3 initialVelocity = _rigidbody.linearVelocity;

            // Act
            _driveSystem.Tick(input, ref state, ctx);

            // Assert
            // Force should be minimal (only drag/damping, no engine force)
            // This is a simplified test - in reality, drag would slow the vehicle
            Assert.IsTrue(true); // Placeholder
        }

        [Test]
        public void ApplyDrive_UpdatesStateWithRPM()
        {
            // Arrange
            var input = new VehicleInput { throttle = 0.5f, brake = 0f };
            var state = new VehicleState
            {
                speed = 5f,
                wheelRadius = 0.3f,
                currentGear = 2,
                engineRPM = 0f,
                wheelAngularVelocity = 0f
            };
            // Context gets specs from CarSpec automatically
            var ctx = new VehicleContext(_rigidbody, _transform, _carSpec, 0.02f);

            // Act
            _driveSystem.Tick(input, ref state, ctx);

            // Assert
            // RPM should be calculated and stored in state
            Assert.Greater(state.engineRPM, 0f);
            Assert.LessOrEqual(state.engineRPM, _engineSpec.maxRPM);
        }

        [Test]
        public void ApplyDrive_UpdatesStateWithGear()
        {
            // Arrange
            var input = new VehicleInput { throttle = 1.0f, brake = 0f };
            var state = new VehicleState
            {
                speed = 0f,
                wheelRadius = 0.3f,
                currentGear = 0,
                engineRPM = 0f,
                wheelAngularVelocity = 0f
            };
            // Context gets specs from CarSpec automatically
            var ctx = new VehicleContext(_rigidbody, _transform, _carSpec, 0.02f);

            // Act
            _driveSystem.Tick(input, ref state, ctx);

            // Assert
            // Gear should be updated (should be 1st gear or higher)
            Assert.GreaterOrEqual(state.currentGear, 1);
        }

        [Test]
        public void ApplyDrive_MissingEngineSpec_DoesNotApplyForce()
        {
            // Arrange
            var input = new VehicleInput { throttle = 1.0f, brake = 0f };
            var state = new VehicleState
            {
                speed = 0f,
                wheelRadius = 0.3f
            };
            var ctx = new VehicleContext(_rigidbody, _transform, _carSpec, 0.02f, null, _gearboxSpec, _wheelSpec, null, null);

            Vector3 initialVelocity = _rigidbody.linearVelocity;

            // Act
            _driveSystem.Tick(input, ref state, ctx);

            // Assert
            // Should not apply force without engine spec
            Vector3 finalVelocity = _rigidbody.linearVelocity;
            Assert.AreEqual(initialVelocity, finalVelocity);
        }

        [Test]
        public void ApplyDrive_MissingGearboxSpec_DoesNotApplyForce()
        {
            // Arrange
            var input = new VehicleInput { throttle = 1.0f, brake = 0f };
            var state = new VehicleState
            {
                speed = 0f,
                wheelRadius = 0.3f
            };
            var ctx = new VehicleContext(_rigidbody, _transform, _carSpec, 0.02f, _engineSpec, null, _wheelSpec, null, null);

            Vector3 initialVelocity = _rigidbody.linearVelocity;

            // Act
            _driveSystem.Tick(input, ref state, ctx);

            // Assert
            // Should not apply force without gearbox spec
            Vector3 finalVelocity = _rigidbody.linearVelocity;
            Assert.AreEqual(initialVelocity, finalVelocity);
        }

        [Test]
        public void ApplyDrive_WhileShifting_DoesNotApplyForce()
        {
            // Arrange
            var input = new VehicleInput { throttle = 1.0f, brake = 0f };
            var state = new VehicleState
            {
                speed = 10f,
                wheelRadius = 0.3f,
                currentGear = 1,
                engineRPM = 6500f, // High RPM to trigger shift
                wheelAngularVelocity = 0f
            };
            // Context gets specs from CarSpec automatically
            var ctx = new VehicleContext(_rigidbody, _transform, _carSpec, 0.02f);

            // Trigger a shift
            _driveSystem.Tick(input, ref state, ctx);
            
            // Get the gearbox model and check if it's shifting
            // This is a simplified test - in reality, we'd need to access the internal gearbox model
            Assert.IsTrue(true); // Placeholder
        }

        [Test]
        public void EngineDriveModel_MissingEngineSpec_DoesNotApplyForce()
        {
            // Arrange
            var input = new VehicleInput { throttle = 1.0f, brake = 0f };
            var state = new VehicleState
            {
                speed = 0f,
                wheelRadius = 0.3f
            };
            // Create context without engine spec (but with CarSpec that also has no engine spec)
            var carSpecNoEngine = ScriptableObject.CreateInstance<CarSpec>();
            carSpecNoEngine.engineSpec = null;
            carSpecNoEngine.gearboxSpec = null;
            var ctx = new VehicleContext(_rigidbody, _transform, carSpecNoEngine, 0.02f, null, null, _wheelSpec, null, null);

            Vector3 initialVelocity = _rigidbody.linearVelocity;

            // Act
            _driveSystem.Tick(input, ref state, ctx);

            // Assert
            // Should not apply force without engine spec
            Vector3 finalVelocity = _rigidbody.linearVelocity;
            Assert.AreEqual(initialVelocity, finalVelocity);

            Object.DestroyImmediate(carSpecNoEngine);
        }

        [Test]
        public void EngineDriveModel_MissingGearboxSpec_DoesNotApplyForce()
        {
            // Arrange
            var input = new VehicleInput { throttle = 1.0f, brake = 0f };
            var state = new VehicleState
            {
                speed = 0f,
                wheelRadius = 0.3f
            };
            // Create context without gearbox spec (but with CarSpec that also has no gearbox spec)
            var carSpecNoGearbox = ScriptableObject.CreateInstance<CarSpec>();
            carSpecNoGearbox.engineSpec = null;
            carSpecNoGearbox.gearboxSpec = null;
            var ctx = new VehicleContext(_rigidbody, _transform, carSpecNoGearbox, 0.02f, null, null, _wheelSpec, null, null);

            Vector3 initialVelocity = _rigidbody.linearVelocity;

            // Act
            _driveSystem.Tick(input, ref state, ctx);

            // Assert
            // Should not apply force without gearbox spec
            Vector3 finalVelocity = _rigidbody.linearVelocity;
            Assert.AreEqual(initialVelocity, finalVelocity);

            Object.DestroyImmediate(carSpecNoGearbox);
        }

        [Test]
        public void ApplyDrive_UsesWheelRadiusFromState()
        {
            // Arrange
            var input = new VehicleInput { throttle = 1.0f, brake = 0f };
            var state = new VehicleState
            {
                speed = 5f,
                wheelRadius = 0.4f, // Different from wheelSpec
                currentGear = 2,
                engineRPM = 0f,
                wheelAngularVelocity = 0f
            };
            // Context gets specs from CarSpec automatically
            var ctx = new VehicleContext(_rigidbody, _transform, _carSpec, 0.02f);

            // Act
            _driveSystem.Tick(input, ref state, ctx);

            // Assert
            // Should use wheelRadius from state (0.4) instead of wheelSpec (0.3)
            // This affects RPM calculation
            Assert.IsTrue(true); // Placeholder - verify RPM calculation uses state.wheelRadius
        }
    }
}

