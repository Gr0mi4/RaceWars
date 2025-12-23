using NUnit.Framework;
using UnityEngine;
using Vehicle.Modules.DriveModels;
using Vehicle.Specs;

namespace Vehicle.Tests.Engine
{
    /// <summary>
    /// Unit tests for GearboxModel.
    /// Tests gear shifting, automatic transmission, shift timing, and reverse gear.
    /// </summary>
    [TestFixture]
    public class GearboxModelTests
    {
        private GearboxSpec _gearboxSpec;
        private GearboxModel _gearboxModel;

        [SetUp]
        public void SetUp()
        {
            _gearboxSpec = ScriptableObject.CreateInstance<GearboxSpec>();
            _gearboxSpec.gearRatios = new float[] { 3.5f, 2.0f, 1.4f, 1.0f, 0.8f };
            _gearboxSpec.finalDriveRatio = 3.9f;
            _gearboxSpec.reverseGearRatio = 3.5f;
            _gearboxSpec.transmissionType = GearboxSpec.TransmissionType.Automatic;
            _gearboxSpec.shiftTime = 0.2f;
            _gearboxSpec.autoShiftUpRPM = 6000f;
            _gearboxSpec.autoShiftDownRPM = 2500f;
            _gearboxSpec.minSpeedForUpshift = 2.0f;

            _gearboxModel = new GearboxModel(_gearboxSpec);
        }

        [TearDown]
        public void TearDown()
        {
            if (_gearboxSpec != null)
            {
                Object.DestroyImmediate(_gearboxSpec);
            }
        }

        [Test]
        public void GearboxModel_InitialState_StartsInFirstGear()
        {
            // Assert
            Assert.AreEqual(1, _gearboxModel.CurrentGear);
            Assert.IsFalse(_gearboxModel.IsShifting);
        }

        [Test]
        public void GetCurrentGearRatio_FirstGear_ReturnsCorrectRatio()
        {
            // Arrange
            _gearboxModel = new GearboxModel(_gearboxSpec);

            // Act
            float ratio = _gearboxModel.GetCurrentGearRatio();

            // Assert
            // First gear: 3.5 * 3.9 = 13.65
            Assert.AreEqual(13.65f, ratio, 0.01f);
        }

        [Test]
        public void ShiftUp_FromFirstGear_ShiftsToSecondGear()
        {
            // Arrange
            _gearboxModel = new GearboxModel(_gearboxSpec);
            Assert.AreEqual(1, _gearboxModel.CurrentGear);

            // Act
            bool result = _gearboxModel.ShiftUp();

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(2, _gearboxModel.CurrentGear);
            Assert.IsTrue(_gearboxModel.IsShifting);
        }

        [Test]
        public void ShiftUp_FromHighestGear_ReturnsFalse()
        {
            // Arrange
            _gearboxModel = new GearboxModel(_gearboxSpec);
            // Shift to highest gear
            while (_gearboxModel.CurrentGear < _gearboxSpec.gearRatios.Length)
            {
                _gearboxModel.ShiftUp();
                // Wait for shift to complete
                _gearboxModel.Update(0f, 0f, 0f, 0.3f);
            }

            // Act
            bool result = _gearboxModel.ShiftUp();

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void ShiftDown_FromSecondGear_ShiftsToFirstGear()
        {
            // Arrange
            _gearboxModel = new GearboxModel(_gearboxSpec);
            _gearboxModel.ShiftUp();
            _gearboxModel.Update(0f, 0f, 0f, 0.3f); // Wait for shift
            Assert.AreEqual(2, _gearboxModel.CurrentGear);

            // Act
            bool result = _gearboxModel.ShiftDown();

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(1, _gearboxModel.CurrentGear);
            Assert.IsTrue(_gearboxModel.IsShifting);
        }

        [Test]
        public void ShiftDown_FromFirstGear_ShiftsToNeutral()
        {
            // Arrange
            _gearboxModel = new GearboxModel(_gearboxSpec);
            Assert.AreEqual(1, _gearboxModel.CurrentGear);

            // Act
            bool result = _gearboxModel.ShiftDown();

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0, _gearboxModel.CurrentGear);
        }

        [Test]
        public void ShiftDown_FromNeutral_ShiftsToReverse()
        {
            // Arrange
            _gearboxModel = new GearboxModel(_gearboxSpec);
            _gearboxModel.ShiftDown(); // To neutral
            _gearboxModel.Update(0f, 0f, 0f, 0.3f); // Wait for shift
            Assert.AreEqual(0, _gearboxModel.CurrentGear);

            // Act
            bool result = _gearboxModel.ShiftDown();

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(-1, _gearboxModel.CurrentGear);
        }

        [Test]
        public void ShiftToReverse_FromNeutral_ShiftsToReverse()
        {
            // Arrange
            _gearboxModel = new GearboxModel(_gearboxSpec);
            _gearboxModel.ShiftDown(); // To neutral
            _gearboxModel.Update(0f, 0f, 0f, 0.3f); // Wait for shift

            // Act
            bool result = _gearboxModel.ShiftToReverse();

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(-1, _gearboxModel.CurrentGear);
        }

        [Test]
        public void GetCurrentGearRatio_ReverseGear_ReturnsNegativeRatio()
        {
            // Arrange
            _gearboxModel = new GearboxModel(_gearboxSpec);
            _gearboxModel.ShiftToReverse();
            _gearboxModel.Update(0f, 0f, 0f, 0.3f); // Wait for shift

            // Act
            float ratio = _gearboxModel.GetCurrentGearRatio();

            // Assert
            // Reverse: -3.5 * 3.9 = -13.65
            Assert.AreEqual(-13.65f, ratio, 0.01f);
        }

        [Test]
        public void GetCurrentGearRatio_Neutral_ReturnsZero()
        {
            // Arrange
            _gearboxModel = new GearboxModel(_gearboxSpec);
            _gearboxModel.ShiftDown(); // To neutral
            _gearboxModel.Update(0f, 0f, 0f, 0.3f); // Wait for shift

            // Act
            float ratio = _gearboxModel.GetCurrentGearRatio();

            // Assert
            Assert.AreEqual(0f, ratio, 0.001f);
        }

        [Test]
        public void GetCurrentGearRatio_WhileShifting_ReturnsZero()
        {
            // Arrange
            _gearboxModel = new GearboxModel(_gearboxSpec);
            _gearboxModel.ShiftUp(); // Starts shifting

            // Act
            float ratio = _gearboxModel.GetCurrentGearRatio();

            // Assert
            Assert.AreEqual(0f, ratio, 0.001f);
        }

        [Test]
        public void Update_ShiftTimer_CompletesAfterShiftTime()
        {
            // Arrange
            _gearboxModel = new GearboxModel(_gearboxSpec);
            _gearboxModel.ShiftUp();
            Assert.IsTrue(_gearboxModel.IsShifting);

            // Act - Update with time less than shiftTime
            _gearboxModel.Update(0f, 0f, 0f, 0.1f);

            // Assert - Still shifting
            Assert.IsTrue(_gearboxModel.IsShifting);

            // Act - Update with remaining time
            _gearboxModel.Update(0f, 0f, 0f, 0.15f);

            // Assert - Shift complete
            Assert.IsFalse(_gearboxModel.IsShifting);
        }

        [Test]
        public void Update_AutomaticTransmission_HighRPM_ShiftsUp()
        {
            // Arrange
            _gearboxModel = new GearboxModel(_gearboxSpec);
            float highRPM = 6500f; // Above autoShiftUpRPM
            float speed = 10f; // Above minSpeedForUpshift
            float throttle = 1.0f;

            // Act
            _gearboxModel.Update(highRPM, speed, throttle, 0.02f);
            _gearboxModel.Update(highRPM, speed, throttle, 0.25f); // Wait for shift

            // Assert
            Assert.AreEqual(2, _gearboxModel.CurrentGear);
        }

        [Test]
        public void Update_AutomaticTransmission_LowRPM_ShiftsDown()
        {
            // Arrange
            _gearboxModel = new GearboxModel(_gearboxSpec);
            _gearboxModel.ShiftUp();
            _gearboxModel.Update(0f, 0f, 0f, 0.25f); // Wait for shift to 2nd
            float lowRPM = 2000f; // Below autoShiftDownRPM
            float throttle = 0.5f;

            // Act
            _gearboxModel.Update(lowRPM, 5f, throttle, 0.02f);
            _gearboxModel.Update(lowRPM, 5f, throttle, 0.25f); // Wait for shift

            // Assert
            Assert.AreEqual(1, _gearboxModel.CurrentGear);
        }

        [Test]
        public void Update_AutomaticTransmission_LowSpeed_DoesNotShiftUp()
        {
            // Arrange
            _gearboxModel = new GearboxModel(_gearboxSpec);
            float highRPM = 6500f;
            float lowSpeed = 1.0f; // Below minSpeedForUpshift
            float throttle = 1.0f;

            // Act
            _gearboxModel.Update(highRPM, lowSpeed, throttle, 0.02f);
            _gearboxModel.Update(highRPM, lowSpeed, throttle, 0.25f);

            // Assert - Should not shift up due to low speed
            Assert.AreEqual(1, _gearboxModel.CurrentGear);
        }

        [Test]
        public void Update_AutomaticTransmission_NoThrottle_DoesNotShiftDown()
        {
            // Arrange
            _gearboxModel = new GearboxModel(_gearboxSpec);
            _gearboxModel.ShiftUp();
            _gearboxModel.Update(0f, 0f, 0f, 0.25f); // Wait for shift to 2nd
            float lowRPM = 2000f;
            float throttle = 0f; // No throttle

            // Act
            _gearboxModel.Update(lowRPM, 5f, throttle, 0.02f);
            _gearboxModel.Update(lowRPM, 5f, throttle, 0.25f);

            // Assert - Should not shift down without throttle
            Assert.AreEqual(2, _gearboxModel.CurrentGear);
        }

        [Test]
        public void ShiftUp_WhileShifting_ReturnsFalse()
        {
            // Arrange
            _gearboxModel = new GearboxModel(_gearboxSpec);
            _gearboxModel.ShiftUp(); // Starts shifting
            Assert.IsTrue(_gearboxModel.IsShifting);

            // Act
            bool result = _gearboxModel.ShiftUp();

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void ShiftToReverse_WhileShifting_ReturnsFalse()
        {
            // Arrange
            _gearboxModel = new GearboxModel(_gearboxSpec);
            _gearboxModel.ShiftUp(); // Starts shifting

            // Act
            bool result = _gearboxModel.ShiftToReverse();

            // Assert
            Assert.IsFalse(result);
        }
    }
}

