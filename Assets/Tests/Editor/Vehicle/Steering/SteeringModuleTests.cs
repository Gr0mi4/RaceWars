using NUnit.Framework;
using UnityEngine;
using Vehicle.Core;
using Vehicle.Systems;
using Vehicle.Specs;

namespace Vehicle.Tests.Steering
{
    /// <summary>
    /// Unit tests for SteeringSystem.
    /// Tests physics-based steering mode.
    /// </summary>
    [TestFixture]
    public class SteeringModuleTests
    {
        private GameObject _gameObject;
        private Rigidbody _rigidbody;
        private Transform _transform;
        private CarSpec _carSpec;
        private WheelSpec _wheelSpec;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("TestVehicle");
            _rigidbody = _gameObject.AddComponent<Rigidbody>();
            _rigidbody.mass = 1000f;
            _transform = _gameObject.transform;

            _carSpec = ScriptableObject.CreateInstance<CarSpec>();
            _wheelSpec = ScriptableObject.CreateInstance<WheelSpec>();
            _wheelSpec.friction = 0.9f;
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
            if (_wheelSpec != null)
            {
                Object.DestroyImmediate(_wheelSpec);
            }
        }

        [Test]
        public void SteeringModule_PhysicsMode_AppliesTorque()
        {
            // Arrange
            var spec = ScriptableObject.CreateInstance<SteeringSpec>();
            spec.wheelbase = 2.8f;
            spec.maxSteerAngle = 32f;
            spec.yawResponseTime = 0.11f;
            spec.maxYawAccel = 11f;
            spec.minForwardSpeed = 0.2f;
            var system = new SteeringSystem(spec);

            var input = new VehicleInput { steer = 1f };
            var state = new VehicleState
            {
                localVelocity = new Vector3(0f, 0f, 10f),
                yawRate = 0f,
                speed = 10f
            };
            var ctx = new VehicleContext(_rigidbody, _transform, _carSpec, 0.02f, null, null, _wheelSpec, null, spec, null, null);

            Vector3 initialAngularVelocity = _rigidbody.angularVelocity;

            // Act
            system.Tick(input, ref state, ctx);

            // Assert
            Vector3 finalAngularVelocity = _rigidbody.angularVelocity;
            // Angular velocity should have changed (torque was applied)
            Assert.AreNotEqual(initialAngularVelocity.y, finalAngularVelocity.y);

            Object.DestroyImmediate(spec);
        }

        [Test]
        public void SteeringModule_PhysicsMode_ZeroSteer_DoesNotApplyTorque()
        {
            // Arrange
            var spec = ScriptableObject.CreateInstance<SteeringSpec>();
            spec.wheelbase = 2.8f;
            spec.maxSteerAngle = 32f;
            spec.yawResponseTime = 0.11f;
            spec.maxYawAccel = 11f;
            spec.minForwardSpeed = 0.2f;
            var system = new SteeringSystem(spec);

            var input = VehicleInput.Zero; // No steering
            var state = new VehicleState
            {
                localVelocity = new Vector3(0f, 0f, 10f),
                yawRate = 0f,
                speed = 10f
            };
            var ctx = new VehicleContext(_rigidbody, _transform, _carSpec, 0.02f, null, null, _wheelSpec, null, spec, null, null);

            Vector3 initialAngularVelocity = _rigidbody.angularVelocity;

            // Act
            system.Tick(input, ref state, ctx);

            // Assert
            Vector3 finalAngularVelocity = _rigidbody.angularVelocity;
            // Angular velocity should not change
            Assert.AreEqual(initialAngularVelocity.y, finalAngularVelocity.y, 0.001f);

            Object.DestroyImmediate(spec);
        }

        [Test]
        public void SteeringModule_NullModel_HandlesGracefully()
        {
            // Arrange
            var system = new SteeringSystem(null);
            var input = new VehicleInput { steer = 1f };
            var state = new VehicleState
            {
                localVelocity = new Vector3(0f, 0f, 10f),
                yawRate = 0f,
                speed = 10f
            };
            var ctx = new VehicleContext(_rigidbody, _transform, _carSpec, 0.02f);

            // Act & Assert - should not throw
            Assert.DoesNotThrow(() =>
            {
                system.Tick(input, ref state, ctx);
            });
        }

        [Test]
        public void SteeringModule_NullRigidbody_HandlesGracefully()
        {
            // Arrange
            var spec = ScriptableObject.CreateInstance<SteeringSpec>();
            spec.wheelbase = 2.8f;
            spec.maxSteerAngle = 32f;
            spec.yawResponseTime = 0.11f;
            spec.maxYawAccel = 11f;
            spec.minForwardSpeed = 0.2f;
            var system = new SteeringSystem(spec);

            var input = new VehicleInput { steer = 1f };
            var state = new VehicleState
            {
                localVelocity = new Vector3(0f, 0f, 10f),
                yawRate = 0f,
                speed = 10f
            };

            // Create context with null rigidbody
            var ctx = new VehicleContext(null, _transform, _carSpec, 0.02f, null, null, _wheelSpec, null, spec, null, null);

            // Act & Assert - should not throw (system should check for null)
            Assert.DoesNotThrow(() =>
            {
                system.Tick(input, ref state, ctx);
            });

            Object.DestroyImmediate(spec);
        }
    }
}

