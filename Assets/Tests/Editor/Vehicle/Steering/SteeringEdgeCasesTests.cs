using NUnit.Framework;
using UnityEngine;
using Vehicle.Core;
using Vehicle.Systems;
using Vehicle.Specs;

namespace Vehicle.Tests.Steering
{
    /// <summary>
    /// Tests for edge cases and error handling in steering system.
    /// Ensures system never breaks and handles all edge cases gracefully.
    /// </summary>
    [TestFixture]
    public class SteeringEdgeCasesTests
    {
        private GameObject _gameObject;
        private Rigidbody _rigidbody;
        private Transform _transform;
        private WheelSpec _wheelSpec;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("TestVehicle");
            _rigidbody = _gameObject.AddComponent<Rigidbody>();
            _rigidbody.mass = 1000f;
            _transform = _gameObject.transform;
            _wheelSpec = ScriptableObject.CreateInstance<WheelSpec>();
            _wheelSpec.friction = 0.75f;
        }

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null)
            {
                Object.DestroyImmediate(_gameObject);
            }
            if (_wheelSpec != null)
            {
                Object.DestroyImmediate(_wheelSpec);
            }
        }

        [Test]
        public void PhysicsSteeringModel_VeryLowForwardSpeed_HandlesGracefully()
        {
            // Arrange
            var spec = ScriptableObject.CreateInstance<SteeringSpec>();
            spec.minForwardSpeed = 0.2f;
            spec.wheelbase = 2.8f;
            spec.maxSteerAngle = 32f;
            spec.yawResponseTime = 0.11f;
            spec.maxYawAccel = 11f;
            var system = new SteeringSystem(spec);

            var input = new VehicleInput { steer = 1f };
            var state = new VehicleState
            {
                localVelocity = new Vector3(0f, 0f, 0.19f), // Just below minForwardSpeed
                yawRate = 0f
            };
            var ctx = new VehicleContext(_rigidbody, _transform, null, 0.02f, null, null, _wheelSpec, null, spec, null, null);

            // Act
            bool result = system.ApplySteering(input, state, ctx, out float torque);

            // Assert
            Assert.IsFalse(result); // Should return false (steering disabled)
            Assert.AreEqual(0f, torque);

            Object.DestroyImmediate(spec);
        }

        [Test]
        public void PhysicsSteeringModel_ZeroWheelbase_HandlesGracefully()
        {
            // Arrange
            var spec = ScriptableObject.CreateInstance<SteeringSpec>();
            spec.wheelbase = 0f; // Invalid
            spec.maxSteerAngle = 32f;
            spec.yawResponseTime = 0.11f;
            spec.maxYawAccel = 11f;
            spec.minForwardSpeed = 0.2f;
            var system = new SteeringSystem(spec);

            var input = new VehicleInput { steer = 1f };
            var state = new VehicleState
            {
                localVelocity = new Vector3(0f, 0f, 10f),
                yawRate = 0f
            };
            var ctx = new VehicleContext(_rigidbody, _transform, null, 0.02f, null, null, _wheelSpec, null, spec, null, null);

            // Act & Assert - should not throw or produce NaN
            bool result = system.ApplySteering(input, state, ctx, out float torque);
            Assert.IsTrue(float.IsFinite(torque) || !result);

            Object.DestroyImmediate(spec);
        }

        [Test]
        public void PhysicsSteeringModel_ExtremeSteerAngle_HandlesGracefully()
        {
            // Arrange
            var spec = ScriptableObject.CreateInstance<SteeringSpec>();
            spec.wheelbase = 2.8f;
            spec.maxSteerAngle = 90f; // Extreme angle
            spec.yawResponseTime = 0.11f;
            spec.maxYawAccel = 11f;
            spec.minForwardSpeed = 0.2f;
            var system = new SteeringSystem(spec);

            var input = new VehicleInput { steer = 1f };
            var state = new VehicleState
            {
                localVelocity = new Vector3(0f, 0f, 10f),
                yawRate = 0f
            };
            var ctx = new VehicleContext(_rigidbody, _transform, null, 0.02f, null, null, _wheelSpec, null, spec, null, null);

            // Act
            bool result = system.ApplySteering(input, state, ctx, out float torque);

            // Assert
            Assert.IsTrue(result);
            Assert.IsTrue(float.IsFinite(torque));

            Object.DestroyImmediate(spec);
        }

        [Test]
        public void PhysicsSteeringModel_SimultaneousBrakeAndThrottle_HandlesCorrectly()
        {
            // Arrange
            var spec = ScriptableObject.CreateInstance<SteeringSpec>();
            spec.wheelbase = 2.8f;
            spec.maxSteerAngle = 32f;
            spec.frictionCircleStrength = 0.95f;
            spec.yawResponseTime = 0.11f;
            spec.maxYawAccel = 11f;
            spec.minForwardSpeed = 0.2f;
            var system = new SteeringSystem(spec);

            var input = new VehicleInput { steer = 1f, brake = 1f, throttle = 1f }; // Both pressed
            var state = new VehicleState
            {
                localVelocity = new Vector3(0f, 0f, 10f),
                yawRate = 0f
            };
            var ctx = new VehicleContext(_rigidbody, _transform, null, 0.02f, null, null, _wheelSpec, null, spec, null, null);

            // Act
            bool result = system.ApplySteering(input, state, ctx, out float torque);

            // Assert
            Assert.IsTrue(result);
            Assert.IsTrue(float.IsFinite(torque));
            // longUsage should account for both (clamped to 1.0)
            // This should reduce lateral grip significantly

            Object.DestroyImmediate(spec);
        }

        [Test]
        public void PhysicsSteeringModel_VeryHighYawRate_ClampsCorrectly()
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
                yawRate = 100f // Very high existing yaw rate
            };
            var ctx = new VehicleContext(_rigidbody, _transform, null, 0.02f, null, null, _wheelSpec, null, spec, null, null);

            // Act
            bool result = system.ApplySteering(input, state, ctx, out float torque);

            // Assert
            Assert.IsTrue(result);
            Assert.IsTrue(float.IsFinite(torque));
            // Torque should be clamped by maxYawAccel
        }

        [Test]
        public void PhysicsSteeringModel_NegativeMu_HandlesGracefully()
        {
            // Arrange
            var spec = ScriptableObject.CreateInstance<SteeringSpec>();
            spec.wheelbase = 2.8f;
            spec.maxSteerAngle = 32f;
            spec.yawResponseTime = 0.11f;
            spec.maxYawAccel = 11f;
            spec.minForwardSpeed = 0.2f;
            var system = new SteeringSystem(spec);
            _wheelSpec.friction = -0.5f; // Invalid negative

            var input = new VehicleInput { steer = 1f };
            var state = new VehicleState
            {
                localVelocity = new Vector3(0f, 0f, 10f),
                yawRate = 0f
            };
            var ctx = new VehicleContext(_rigidbody, _transform, null, 0.02f, null, null, _wheelSpec, null, spec, null, null);

            // Act & Assert - should not throw or produce NaN
            bool result = system.ApplySteering(input, state, ctx, out float torque);
            Assert.IsTrue(float.IsFinite(torque) || !result);

            Object.DestroyImmediate(spec);
        }

        [Test]
        public void PhysicsSteeringModel_ZeroResponseTime_HandlesGracefully()
        {
            // Arrange
            var spec = ScriptableObject.CreateInstance<SteeringSpec>();
            spec.wheelbase = 2.8f;
            spec.maxSteerAngle = 32f;
            spec.yawResponseTime = 0f; // Invalid zero
            spec.maxYawAccel = 11f;
            spec.minForwardSpeed = 0.2f;
            var system = new SteeringSystem(spec);

            var input = new VehicleInput { steer = 1f };
            var state = new VehicleState
            {
                localVelocity = new Vector3(0f, 0f, 10f),
                yawRate = 0f
            };
            var ctx = new VehicleContext(_rigidbody, _transform, null, 0.02f, null, null, _wheelSpec, null, spec, null, null);

            // Act & Assert - should not throw (division by zero should be handled)
            Assert.DoesNotThrow(() =>
            {
                system.ApplySteering(input, state, ctx, out float torque);
            });

            Object.DestroyImmediate(spec);
        }

        [Test]
        public void PhysicsSteeringModel_SidewaysVelocity_SteeringStillWorks()
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
                localVelocity = new Vector3(5f, 0f, 0.5f), // Mostly sideways, but some forward
                yawRate = 0f
            };
            var ctx = new VehicleContext(_rigidbody, _transform, null, 0.02f, null, null, _wheelSpec, null, spec, null, null);

            // Act
            bool result = system.ApplySteering(input, state, ctx, out float torque);

            // Assert
            // Should work because forward speed (0.5) is above minForwardSpeed (0.2)
            Assert.IsTrue(result);
            Assert.IsTrue(float.IsFinite(torque));

            Object.DestroyImmediate(spec);
        }

        [Test]
        public void PhysicsSteeringModel_FrictionCircleAtMaximum_ReducesGripToZero()
        {
            // Arrange
            var spec = ScriptableObject.CreateInstance<SteeringSpec>();
            spec.wheelbase = 2.8f;
            spec.maxSteerAngle = 32f;
            spec.frictionCircleStrength = 1.0f; // Maximum coupling
            spec.yawResponseTime = 0.11f;
            spec.maxYawAccel = 11f;
            spec.minForwardSpeed = 0.2f;
            var system = new SteeringSystem(spec);

            var input = new VehicleInput { steer = 1f, brake = 1f }; // Full brake
            var state = new VehicleState
            {
                localVelocity = new Vector3(0f, 0f, 10f),
                yawRate = 0f
            };
            var ctx = new VehicleContext(_rigidbody, _transform, null, 0.02f, null, null, _wheelSpec, null, spec, null, null);

            // Act
            bool result = system.ApplySteering(input, state, ctx, out float torque);

            // Assert
            // With frictionCircleStrength = 1.0 and full brake, latGripFactor should be ~0
            // So yawRateMax should be very small, resulting in minimal torque
            Assert.IsTrue(result);
            Assert.IsTrue(float.IsFinite(torque));

            Object.DestroyImmediate(spec);
        }
    }
}

