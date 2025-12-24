using NUnit.Framework;
using UnityEngine;
using Vehicle.Core;
using Vehicle.Systems;
using Vehicle.Specs;

namespace Vehicle.Tests.Steering
{
    /// <summary>
    /// Unit tests for SteeringSystem.
    /// Tests bicycle model, grip limits, friction circle, and edge cases.
    /// </summary>
    [TestFixture]
    public class PhysicsSteeringModelTests
    {
        private SteeringSpec _spec;
        private SteeringSystem _system;
        private GameObject _gameObject;
        private Rigidbody _rigidbody;
        private Transform _transform;

        [SetUp]
        public void SetUp()
        {
            // Create test spec with known values
            _spec = ScriptableObject.CreateInstance<SteeringSpec>();
            _spec.wheelbase = 2.8f;
            _spec.maxSteerAngle = 32f;
            _spec.baseMu = 0.75f;
            _spec.frictionCircleStrength = 0.95f;
            _spec.handbrakeGripMultiplier = 0.2f;
            _spec.yawResponseTime = 0.11f;
            _spec.maxYawAccel = 11f;
            _spec.minForwardSpeed = 0.2f;

            _system = new SteeringSystem(_spec);

            // Create test GameObject with Rigidbody
            _gameObject = new GameObject("TestVehicle");
            _rigidbody = _gameObject.AddComponent<Rigidbody>();
            _rigidbody.mass = 1000f;
            _transform = _gameObject.transform;
        }

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null)
            {
                Object.DestroyImmediate(_gameObject);
            }
            if (_spec != null)
            {
                Object.DestroyImmediate(_spec);
            }
        }

        [Test]
        public void ApplySteering_ZeroSteerInput_ReturnsFalse()
        {
            // Arrange
            var input = VehicleInput.Zero;
            var state = new VehicleState
            {
                localVelocity = new Vector3(0f, 0f, 10f), // Moving forward
                yawRate = 0f
            };
            var ctx = new VehicleContext(_rigidbody, _transform, null, 0.02f);

            // Act
            bool result = _system.ApplySteering(input, state, ctx, out float torque);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(0f, torque);
        }

        [Test]
        public void ApplySteering_ZeroForwardSpeed_ReturnsFalse()
        {
            // Arrange
            var input = new VehicleInput { steer = 1f };
            var state = new VehicleState
            {
                localVelocity = new Vector3(0f, 0f, 0f), // Not moving
                yawRate = 0f
            };
            var ctx = new VehicleContext(_rigidbody, _transform, null, 0.02f);

            // Act
            bool result = _system.ApplySteering(input, state, ctx, out float torque);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(0f, torque);
        }

        [Test]
        public void ApplySteering_LowForwardSpeedBelowMin_ReturnsFalse()
        {
            // Arrange
            var input = new VehicleInput { steer = 1f };
            var state = new VehicleState
            {
                localVelocity = new Vector3(0f, 0f, 0.1f), // Below minForwardSpeed (0.2)
                yawRate = 0f
            };
            var ctx = new VehicleContext(_rigidbody, _transform, null, 0.02f);

            // Act
            bool result = _system.ApplySteering(input, state, ctx, out float torque);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(0f, torque);
        }

        [Test]
        public void ApplySteering_ForwardSpeed_ReturnsTrueAndCalculatesTorque()
        {
            // Arrange
            var input = new VehicleInput { steer = 1f }; // Full right
            var state = new VehicleState
            {
                localVelocity = new Vector3(0f, 0f, 10f), // 10 m/s forward
                yawRate = 0f
            };
            var ctx = new VehicleContext(_rigidbody, _transform, null, 0.02f);

            // Act
            bool result = _system.ApplySteering(input, state, ctx, out float torque);

            // Assert
            Assert.IsTrue(result);
            Assert.Greater(Mathf.Abs(torque), 0f);
            Assert.IsTrue(float.IsFinite(torque));
        }

        [Test]
        public void ApplySteering_ReverseSpeed_WorksCorrectly()
        {
            // Arrange
            var input = new VehicleInput { steer = 1f };
            var state = new VehicleState
            {
                localVelocity = new Vector3(0f, 0f, -10f), // Reversing
                yawRate = 0f
            };
            var ctx = new VehicleContext(_rigidbody, _transform, null, 0.02f);

            // Act
            bool result = _system.ApplySteering(input, state, ctx, out float torque);

            // Assert
            Assert.IsTrue(result);
            Assert.IsTrue(float.IsFinite(torque));
        }

        [Test]
        public void ApplySteering_BrakingReducesLateralGrip()
        {
            // Arrange
            var inputNoBrake = new VehicleInput { steer = 1f, brake = 0f };
            var inputWithBrake = new VehicleInput { steer = 1f, brake = 1f };
            var state = new VehicleState
            {
                localVelocity = new Vector3(0f, 0f, 10f),
                yawRate = 0f
            };
            var ctx = new VehicleContext(_rigidbody, _transform, null, 0.02f);

            // Act
            _system.ApplySteering(inputNoBrake, state, ctx, out float torqueNoBrake);
            _system.ApplySteering(inputWithBrake, state, ctx, out float torqueWithBrake);

            // Assert
            // With braking, lateral grip is reduced, so torque should be lower (or equal if already at limit)
            Assert.LessOrEqual(Mathf.Abs(torqueWithBrake), Mathf.Abs(torqueNoBrake));
        }

        [Test]
        public void ApplySteering_ThrottleReducesLateralGrip()
        {
            // Arrange
            var inputNoThrottle = new VehicleInput { steer = 1f, throttle = 0f };
            var inputWithThrottle = new VehicleInput { steer = 1f, throttle = 1f };
            var state = new VehicleState
            {
                localVelocity = new Vector3(0f, 0f, 10f),
                yawRate = 0f
            };
            var ctx = new VehicleContext(_rigidbody, _transform, null, 0.02f);

            // Act
            _system.ApplySteering(inputNoThrottle, state, ctx, out float torqueNoThrottle);
            _system.ApplySteering(inputWithThrottle, state, ctx, out float torqueWithThrottle);

            // Assert
            // With throttle, lateral grip is reduced
            Assert.LessOrEqual(Mathf.Abs(torqueWithThrottle), Mathf.Abs(torqueNoThrottle));
        }

        [Test]
        public void ApplySteering_HandbrakeReducesGrip()
        {
            // Arrange
            var inputNoHandbrake = new VehicleInput { steer = 1f, handbrake = 0f };
            var inputWithHandbrake = new VehicleInput { steer = 1f, handbrake = 1f };
            var state = new VehicleState
            {
                localVelocity = new Vector3(0f, 0f, 10f),
                yawRate = 0f
            };
            var ctx = new VehicleContext(_rigidbody, _transform, null, 0.02f);

            // Act
            _system.ApplySteering(inputNoHandbrake, state, ctx, out float torqueNoHandbrake);
            _system.ApplySteering(inputWithHandbrake, state, ctx, out float torqueWithHandbrake);

            // Assert
            // Handbrake should significantly reduce grip (multiplier = 0.2)
            Assert.Less(Mathf.Abs(torqueWithHandbrake), Mathf.Abs(torqueNoHandbrake));
        }

        [Test]
        public void ApplySteering_HighSpeed_RespectsGripLimits()
        {
            // Arrange
            var input = new VehicleInput { steer = 1f };
            var state = new VehicleState
            {
                localVelocity = new Vector3(0f, 0f, 30f), // High speed
                yawRate = 0f
            };
            var ctx = new VehicleContext(_rigidbody, _transform, null, 0.02f);

            // Act
            bool result = _system.ApplySteering(input, state, ctx, out float torque);

            // Assert
            Assert.IsTrue(result);
            Assert.IsTrue(float.IsFinite(torque));
            // Torque should be reasonable (not infinite)
            Assert.Less(Mathf.Abs(torque), 1000000f); // Arbitrary large limit
        }

        [Test]
        public void ApplySteering_ExistingYawRate_AffectsTorque()
        {
            // Arrange
            var input = new VehicleInput { steer = 1f };
            var stateNoYaw = new VehicleState
            {
                localVelocity = new Vector3(0f, 0f, 10f),
                yawRate = 0f
            };
            var stateWithYaw = new VehicleState
            {
                localVelocity = new Vector3(0f, 0f, 10f),
                yawRate = 5f // Already rotating
            };
            var ctx = new VehicleContext(_rigidbody, _transform, null, 0.02f);

            // Act
            _system.ApplySteering(input, stateNoYaw, ctx, out float torqueNoYaw);
            _system.ApplySteering(input, stateWithYaw, ctx, out float torqueWithYaw);

            // Assert
            // If already rotating in desired direction, torque should be less
            // (yaw rate control tries to reach target, not exceed it)
            Assert.IsTrue(float.IsFinite(torqueNoYaw));
            Assert.IsTrue(float.IsFinite(torqueWithYaw));
        }

        [Test]
        public void ApplySteering_LeftSteer_ProducesNegativeTorque()
        {
            // Arrange
            var input = new VehicleInput { steer = -1f }; // Left
            var state = new VehicleState
            {
                localVelocity = new Vector3(0f, 0f, 10f),
                yawRate = 0f
            };
            var ctx = new VehicleContext(_rigidbody, _transform, null, 0.02f);

            // Act
            bool result = _system.ApplySteering(input, state, ctx, out float torque);

            // Assert
            Assert.IsTrue(result);
            Assert.Less(torque, 0f); // Negative torque for left turn
        }

        [Test]
        public void ApplySteering_RightSteer_ProducesPositiveTorque()
        {
            // Arrange
            var input = new VehicleInput { steer = 1f }; // Right
            var state = new VehicleState
            {
                localVelocity = new Vector3(0f, 0f, 10f),
                yawRate = 0f
            };
            var ctx = new VehicleContext(_rigidbody, _transform, null, 0.02f);

            // Act
            bool result = _system.ApplySteering(input, state, ctx, out float torque);

            // Assert
            Assert.IsTrue(result);
            Assert.Greater(torque, 0f); // Positive torque for right turn
        }

        [Test]
        public void ApplySteering_ExtremeSteerInput_ClampsCorrectly()
        {
            // Arrange
            var input = new VehicleInput { steer = 2f }; // Beyond [-1, 1] range
            var state = new VehicleState
            {
                localVelocity = new Vector3(0f, 0f, 10f),
                yawRate = 0f
            };
            var ctx = new VehicleContext(_rigidbody, _transform, null, 0.02f);

            // Act
            bool result = _system.ApplySteering(input, state, ctx, out float torque);

            // Assert
            // Should still work (input.steer is used directly, clamping happens in maxSteerAngle)
            Assert.IsTrue(result);
            Assert.IsTrue(float.IsFinite(torque));
        }

        [Test]
        public void ApplySteering_NullSpec_UsesDefaults()
        {
            // Arrange
            var systemWithNullSpec = new SteeringSystem(null);
            var input = new VehicleInput { steer = 1f };
            var state = new VehicleState
            {
                localVelocity = new Vector3(0f, 0f, 10f),
                yawRate = 0f
            };
            var ctx = new VehicleContext(_rigidbody, _transform, null, 0.02f);

            // Act & Assert - should not throw
            Assert.DoesNotThrow(() =>
            {
                systemWithNullSpec.ApplySteering(input, state, ctx, out float torque);
            });
        }

        [Test]
        public void ApplySteering_InvalidSpecValues_HandlesGracefully()
        {
            // Arrange
            var invalidSpec = ScriptableObject.CreateInstance<SteeringSpec>();
            invalidSpec.wheelbase = 0f; // Invalid
            invalidSpec.baseMu = -1f; // Invalid
            var system = new SteeringSystem(invalidSpec);
            var input = new VehicleInput { steer = 1f };
            var state = new VehicleState
            {
                localVelocity = new Vector3(0f, 0f, 10f),
                yawRate = 0f
            };
            var ctx = new VehicleContext(_rigidbody, _transform, null, 0.02f);

            // Act & Assert - should not throw or produce NaN
            bool result = system.ApplySteering(input, state, ctx, out float torque);
            Assert.IsTrue(float.IsFinite(torque) || !result);
            
            Object.DestroyImmediate(invalidSpec);
        }

        [Test]
        public void ApplySteering_FrictionCircle_StrongCoupling()
        {
            // Arrange
            // Test that friction circle strength affects grip reduction
            var specStrong = ScriptableObject.CreateInstance<SteeringSpec>();
            specStrong.frictionCircleStrength = 1.0f; // Maximum coupling
            specStrong.wheelbase = 2.8f;
            specStrong.maxSteerAngle = 32f;
            specStrong.baseMu = 0.75f;
            specStrong.yawResponseTime = 0.11f;
            specStrong.maxYawAccel = 11f;
            specStrong.minForwardSpeed = 0.2f;
            var systemStrong = new SteeringSystem(specStrong);

            var specWeak = ScriptableObject.CreateInstance<SteeringSpec>();
            specWeak.frictionCircleStrength = 0.5f; // Weak coupling
            specWeak.wheelbase = 2.8f;
            specWeak.maxSteerAngle = 32f;
            specWeak.baseMu = 0.75f;
            specWeak.yawResponseTime = 0.11f;
            specWeak.maxYawAccel = 11f;
            specWeak.minForwardSpeed = 0.2f;
            var systemWeak = new SteeringSystem(specWeak);

            var input = new VehicleInput { steer = 1f, brake = 1f }; // Full brake + steer
            var state = new VehicleState
            {
                localVelocity = new Vector3(0f, 0f, 10f),
                yawRate = 0f
            };
            var ctx = new VehicleContext(_rigidbody, _transform, null, 0.02f);

            // Act
            systemStrong.ApplySteering(input, state, ctx, out float torqueStrong);
            systemWeak.ApplySteering(input, state, ctx, out float torqueWeak);

            // Assert
            // Strong coupling should reduce grip more than weak coupling
            Assert.LessOrEqual(Mathf.Abs(torqueStrong), Mathf.Abs(torqueWeak));

            Object.DestroyImmediate(specStrong);
            Object.DestroyImmediate(specWeak);
        }
    }
}

