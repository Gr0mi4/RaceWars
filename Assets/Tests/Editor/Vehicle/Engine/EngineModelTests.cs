using NUnit.Framework;
using UnityEngine;
using Vehicle.Systems;
using Vehicle.Specs;

namespace Vehicle.Tests.Engine
{
    /// <summary>
    /// Unit tests for EngineSystem.
    /// Tests RPM calculation, power/torque retrieval from curves, and wheel force calculation.
    /// </summary>
    [TestFixture]
    public class EngineModelTests
    {
        private EngineSystem _engineSystem;
        private EngineSpec _engineSpec;

        [SetUp]
        public void SetUp()
        {
            _engineSystem = new EngineSystem();
            
            _engineSpec = ScriptableObject.CreateInstance<EngineSpec>();
            _engineSpec.maxPower = 110f; // HP
            _engineSpec.maxTorque = 155f; // Nm
            _engineSpec.maxRPM = 6500f;
            _engineSpec.idleRPM = 800f;
            _engineSpec.minSpeedForPower = 0.5f;
            
            // Create simple linear power curve (0.3 at idle, 1.0 at max)
            _engineSpec.powerCurve = AnimationCurve.Linear(0f, 0.3f, 1f, 1f);
            
            // Create simple linear torque curve (0.8 at idle, 0.6 at max)
            _engineSpec.torqueCurve = AnimationCurve.Linear(0f, 0.8f, 1f, 0.6f);
        }

        [TearDown]
        public void TearDown()
        {
            if (_engineSpec != null)
            {
                Object.DestroyImmediate(_engineSpec);
            }
        }

        [Test]
        public void CalculateRPM_ValidInputs_ReturnsCorrectRPM()
        {
            // Arrange
            float speed = 10f; // m/s
            float gearRatio = 3.5f;
            float wheelRadius = 0.3f; // m
            float finalDriveRatio = 3.9f;

            // Act
            // Step 1: Calculate wheel angular velocity from speed
            float wheelAngularVelocity = _engineSystem.CalculateWheelAngularVelocityFromSpeed(speed, wheelRadius);
            // Step 2: Calculate engine RPM from wheel angular velocity
            float rpm = _engineSystem.CalculateEngineRPMFromWheel(wheelAngularVelocity, gearRatio, finalDriveRatio);

            // Assert
            // Expected: RPM = (speed / (2π * wheelRadius)) * gearRatio * finalDriveRatio * 60
            // RPM = (10 / (2π * 0.3)) * 3.5 * 3.9 * 60
            // RPM ≈ (10 / 1.885) * 3.5 * 3.9 * 60 ≈ 4337 RPM
            Assert.Greater(rpm, 4000f);
            Assert.Less(rpm, 5000f);
        }

        [Test]
        public void CalculateRPM_ZeroSpeed_ReturnsZero()
        {
            // Arrange
            float speed = 0f;
            float gearRatio = 3.5f;
            float wheelRadius = 0.3f;
            float finalDriveRatio = 3.9f;

            // Act
            float wheelAngularVelocity = _engineSystem.CalculateWheelAngularVelocityFromSpeed(speed, wheelRadius);
            float rpm = _engineSystem.CalculateEngineRPMFromWheel(wheelAngularVelocity, gearRatio, finalDriveRatio);

            // Assert
            Assert.AreEqual(0f, rpm, 0.001f);
        }

        [Test]
        public void CalculateRPM_InvalidInputs_ReturnsZero()
        {
            // Arrange
            float speed = 10f;
            float gearRatio = 0f; // Invalid
            float wheelRadius = 0.3f;
            float finalDriveRatio = 3.9f;

            // Act
            float wheelAngularVelocity = _engineSystem.CalculateWheelAngularVelocityFromSpeed(speed, wheelRadius);
            float rpm = _engineSystem.CalculateEngineRPMFromWheel(wheelAngularVelocity, gearRatio, finalDriveRatio);

            // Assert
            Assert.AreEqual(0f, rpm, 0.001f);
        }

        [Test]
        public void CalculateRPM_ReverseGear_ReturnsPositiveRPM()
        {
            // Arrange
            float speed = -10f; // Reverse speed (negative)
            float gearRatio = -3.5f; // Reverse gear (negative)
            float wheelRadius = 0.3f;
            float finalDriveRatio = 3.9f;

            // Act
            // Use absolute value of speed for wheel angular velocity (direction doesn't matter for angular velocity)
            float wheelAngularVelocity = _engineSystem.CalculateWheelAngularVelocityFromSpeed(Mathf.Abs(speed), wheelRadius);
            float rpm = _engineSystem.CalculateEngineRPMFromWheel(wheelAngularVelocity, gearRatio, finalDriveRatio);

            // Assert
            // RPM should be positive even for reverse (engine still spins forward)
            Assert.Greater(rpm, 0f);
        }

        [Test]
        public void GetPower_ValidInputs_ReturnsCorrectPower()
        {
            // Arrange
            float rpm = 4000f; // Mid-range RPM
            float throttle = 1.0f; // Full throttle

            // Act
            float power = _engineSystem.GetPower(rpm, throttle, _engineSpec);

            // Assert
            // At 4000 RPM: normalized = (4000-800)/(6500-800) ≈ 0.561
            // Power multiplier from curve ≈ 0.3 + 0.561 * (1.0 - 0.3) ≈ 0.693
            // Power = 110 * 0.693 * 1.0 * 745.7 ≈ 56700 W
            Assert.Greater(power, 50000f);
            Assert.Less(power, 65000f);
        }

        [Test]
        public void GetPower_ZeroThrottle_ReturnsZero()
        {
            // Arrange
            float rpm = 4000f;
            float throttle = 0f;

            // Act
            float power = _engineSystem.GetPower(rpm, throttle, _engineSpec);

            // Assert
            Assert.AreEqual(0f, power, 0.001f);
        }

        [Test]
        public void GetPower_NullSpec_ReturnsZero()
        {
            // Arrange
            float rpm = 4000f;
            float throttle = 1.0f;

            // Act
            float power = _engineSystem.GetPower(rpm, throttle, null);

            // Assert
            Assert.AreEqual(0f, power, 0.001f);
        }

        [Test]
        public void GetTorque_ValidInputs_ReturnsCorrectTorque()
        {
            // Arrange
            float rpm = 3000f; // Mid-range RPM
            float throttle = 0.8f; // 80% throttle

            // Act
            float torque = _engineSystem.GetTorque(rpm, throttle, _engineSpec);

            // Assert
            // At 3000 RPM: normalized = (3000-800)/(6500-800) ≈ 0.386
            // Torque multiplier from curve ≈ 0.8 - 0.386 * (0.8 - 0.6) ≈ 0.723
            // Torque = 155 * 0.723 * 0.8 ≈ 89.6 Nm
            Assert.Greater(torque, 80f);
            Assert.Less(torque, 100f);
        }

        [Test]
        public void GetTorque_ZeroThrottle_ReturnsZero()
        {
            // Arrange
            float rpm = 3000f;
            float throttle = 0f;

            // Act
            float torque = _engineSystem.GetTorque(rpm, throttle, _engineSpec);

            // Assert
            Assert.AreEqual(0f, torque, 0.001f);
        }

        [Test]
        public void CalculateWheelForce_LowSpeed_UsesTorque()
        {
            // Arrange
            float speed = 0.2f; // Below minSpeedForPower
            float throttle = 1.0f;
            float currentRPM = 2000f;
            float gearRatio = 3.5f;
            float finalDriveRatio = 3.9f;
            float wheelRadius = 0.3f;

            // Act
            float force = _engineSystem.CalculateWheelForce(
                speed, throttle, _engineSpec, currentRPM, gearRatio, finalDriveRatio, wheelRadius);

            // Assert
            // Should use torque-based calculation at low speed
            // Force should be positive (forward)
            Assert.Greater(force, 0f);
        }

        [Test]
        public void CalculateWheelForce_HighSpeed_UsesPower()
        {
            // Arrange
            float speed = 20f; // Above minSpeedForPower
            float throttle = 1.0f;
            float currentRPM = 5000f;
            float gearRatio = 1.0f; // High gear
            float finalDriveRatio = 3.9f;
            float wheelRadius = 0.3f;

            // Act
            float force = _engineSystem.CalculateWheelForce(
                speed, throttle, _engineSpec, currentRPM, gearRatio, finalDriveRatio, wheelRadius);

            // Assert
            // Should use power-based calculation at high speed
            // Force = Power / Speed
            Assert.Greater(force, 0f);
        }

        [Test]
        public void CalculateWheelForce_ReverseGear_ReturnsNegativeForce()
        {
            // Arrange
            float speed = -5f; // Reverse speed
            float throttle = 0.5f;
            float currentRPM = 2000f;
            float gearRatio = -3.5f; // Reverse gear (negative)
            float finalDriveRatio = 3.9f;
            float wheelRadius = 0.3f;

            // Act
            float force = _engineSystem.CalculateWheelForce(
                speed, throttle, _engineSpec, currentRPM, gearRatio, finalDriveRatio, wheelRadius);

            // Assert
            // Force should be negative for reverse
            Assert.Less(force, 0f);
        }

        [Test]
        public void CalculateWheelForce_ZeroThrottle_ReturnsZero()
        {
            // Arrange
            float speed = 10f;
            float throttle = 0f;
            float currentRPM = 3000f;
            float gearRatio = 2.0f;
            float finalDriveRatio = 3.9f;
            float wheelRadius = 0.3f;

            // Act
            float force = _engineSystem.CalculateWheelForce(
                speed, throttle, _engineSpec, currentRPM, gearRatio, finalDriveRatio, wheelRadius);

            // Assert
            Assert.AreEqual(0f, force, 0.001f);
        }

        [Test]
        public void CalculateWheelForce_NullSpec_ReturnsZero()
        {
            // Arrange
            float speed = 10f;
            float throttle = 1.0f;
            float currentRPM = 3000f;
            float gearRatio = 2.0f;
            float finalDriveRatio = 3.9f;
            float wheelRadius = 0.3f;

            // Act
            float force = _engineSystem.CalculateWheelForce(
                speed, throttle, null, currentRPM, gearRatio, finalDriveRatio, wheelRadius);

            // Assert
            Assert.AreEqual(0f, force, 0.001f);
        }
    }
}


