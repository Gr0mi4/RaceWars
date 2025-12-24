using UnityEngine;
using Vehicle.Specs;

namespace Vehicle.Systems
{
    /// <summary>
    /// Gearbox system for managing transmission behavior including gear shifting,
    /// automatic transmission logic, and reverse gear handling.
    /// </summary>
    public sealed class GearboxSystem
    {
        private readonly GearboxSpec _spec;
        private int _currentGear;
        private float _shiftTimer;
        private bool _isShifting;

        /// <summary>
        /// Gets the current gear index.
        /// -1 = reverse, 0 = neutral, 1+ = forward gears (1st, 2nd, etc.).
        /// </summary>
        public int CurrentGear => _currentGear;

        /// <summary>
        /// Gets whether the transmission is currently shifting gears.
        /// During shifting, no power is transmitted to the wheels.
        /// </summary>
        public bool IsShifting => _isShifting;

        /// <summary>
        /// Initializes a new instance of the GearboxSystem with the specified gearbox specification.
        /// </summary>
        /// <param name="spec">Gearbox specification containing gear ratios, shift points, and transmission type.</param>
        public GearboxSystem(GearboxSpec spec)
        {
            _spec = spec ?? throw new System.ArgumentNullException(nameof(spec));
            _currentGear = 1; // Start in 1st gear
            _shiftTimer = 0f;
            _isShifting = false;
        }

        /// <summary>
        /// Updates the gearbox state, handling automatic shifting and shift timing.
        /// Should be called every physics frame.
        /// </summary>
        /// <param name="currentRPM">Current engine RPM.</param>
        /// <param name="speed">Current vehicle speed in m/s (absolute value).</param>
        /// <param name="throttle">Current throttle input (0-1).</param>
        /// <param name="deltaTime">Time elapsed since last update in seconds.</param>
        public void Update(float currentRPM, float speed, float throttle, float deltaTime)
        {
            if (_spec == null)
            {
                return;
            }

            // Update shift timer
            if (_isShifting)
            {
                _shiftTimer -= deltaTime;
                if (_shiftTimer <= 0f)
                {
                    _isShifting = false;
                    _shiftTimer = 0f;
                }
                return; // Don't allow shifting while already shifting
            }

            // Handle automatic transmission (only if transmission type is Automatic)
            if (_spec.transmissionType == GearboxSpec.TransmissionType.Automatic)
            {
                // Only auto-shift if we're in a forward gear (not reverse or neutral)
                if (_currentGear > 0)
                {
                    // Check for upshift
                    if (currentRPM >= _spec.autoShiftUpRPM && 
                        speed >= _spec.minSpeedForUpshift &&
                        _currentGear < _spec.gearRatios.Length)
                    {
                        ShiftUp();
                    }
                    // Check for downshift
                    else if (currentRPM <= _spec.autoShiftDownRPM && 
                             _currentGear > 1 &&
                             throttle > 0.1f) // Only downshift if throttle is applied
                    {
                        ShiftDown();
                    }
                }
            }
            // Manual transmission: shifting is handled externally by DriveSystem based on user input
        }

        /// <summary>
        /// Gets the current gear ratio (including final drive ratio).
        /// Returns positive value for forward gears, negative for reverse, 0 for neutral.
        /// </summary>
        /// <returns>Current gear ratio multiplied by final drive ratio. Returns 0 if invalid.</returns>
        public float GetCurrentGearRatio()
        {
            if (_spec == null || _isShifting)
            {
                return 0f; // No power during shift
            }

            if (_currentGear == -1)
            {
                // Reverse gear
                return -_spec.reverseGearRatio * _spec.finalDriveRatio;
            }
            else if (_currentGear == 0)
            {
                // Neutral
                return 0f;
            }
            else if (_currentGear > 0 && _currentGear <= _spec.gearRatios.Length)
            {
                // Forward gear (1-based index, array is 0-based)
                int arrayIndex = _currentGear - 1;
                return _spec.gearRatios[arrayIndex] * _spec.finalDriveRatio;
            }

            return 0f;
        }

        /// <summary>
        /// Shifts to the next higher gear (upshift).
        /// </summary>
        /// <returns>True if shift was successful, false if already in highest gear or currently shifting.</returns>
        public bool ShiftUp()
        {
            if (_isShifting || _spec == null)
            {
                return false;
            }

            // Can't shift up from reverse - must go to neutral first
            if (_currentGear == -1)
            {
                _currentGear = 0;
                StartShift();
                return true;
            }

            // Can't shift up from neutral - go to 1st gear
            if (_currentGear == 0)
            {
                _currentGear = 1;
                StartShift();
                return true;
            }

            // Check if we're already in the highest gear
            if (_currentGear >= _spec.gearRatios.Length)
            {
                return false;
            }

            _currentGear++;
            StartShift();
            return true;
        }

        /// <summary>
        /// Shifts to the next lower gear (downshift).
        /// </summary>
        /// <returns>True if shift was successful, false if already in 1st gear or currently shifting.</returns>
        public bool ShiftDown()
        {
            if (_isShifting || _spec == null)
            {
                return false;
            }

            // Can't shift down from reverse
            if (_currentGear == -1)
            {
                return false;
            }

            // From neutral, go to reverse
            if (_currentGear == 0)
            {
                _currentGear = -1;
                StartShift();
                return true;
            }

            // From 1st gear, go to neutral
            if (_currentGear == 1)
            {
                _currentGear = 0;
                StartShift();
                return true;
            }

            _currentGear--;
            StartShift();
            return true;
        }

        /// <summary>
        /// Shifts directly to reverse gear.
        /// </summary>
        /// <returns>True if shift was successful, false if currently shifting.</returns>
        public bool ShiftToReverse()
        {
            if (_isShifting || _spec == null)
            {
                return false;
            }

            // Can only shift to reverse from neutral or 1st gear at very low speed
            if (_currentGear == -1)
            {
                return false; // Already in reverse
            }

            _currentGear = -1;
            StartShift();
            return true;
        }

        /// <summary>
        /// Shifts to neutral gear.
        /// </summary>
        /// <returns>True if shift was successful, false if currently shifting.</returns>
        public bool ShiftToNeutral()
        {
            if (_isShifting || _spec == null)
            {
                return false;
            }

            _currentGear = 0;
            StartShift();
            return true;
        }

        /// <summary>
        /// Starts the shift timer. Called internally when a gear change occurs.
        /// </summary>
        private void StartShift()
        {
            _isShifting = true;
            _shiftTimer = _spec.shiftTime;
        }
    }
}

