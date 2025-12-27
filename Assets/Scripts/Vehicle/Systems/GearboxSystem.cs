using UnityEngine;
using Vehicle.Specs;

namespace Vehicle.Systems
{
    /// <summary>
    /// Gearbox / transmission logic.
    ///
    /// Gear indexing:
    /// -1 = Reverse
    ///  0 = Neutral
    ///  1..N = Forward gears (1-based)
    ///
    /// IMPORTANT:
    /// - "Gear ratio only" means the selected gear ratio (e.g. 3.54)
    /// - "Combined ratio" means gear ratio * finalDriveRatio
    /// </summary>
    public sealed class GearboxSystem
    {
        private readonly GearboxSpec _spec;

        private int _currentGear = 1;
        private float _shiftTimer;
        private bool _isShifting;

        // Prevent shift spam (especially important once wheel slip exists)
        private float _shiftCooldownTimer;
        private const float MinTimeBetweenShifts = 0.15f;

        public int CurrentGear => _currentGear;
        public bool IsShifting => _isShifting;

        public GearboxSystem(GearboxSpec spec)
        {
            _spec = spec ?? throw new System.ArgumentNullException(nameof(spec));

            // Start in 1st gear by default
            _currentGear = 1;
            _shiftTimer = 0f;
            _isShifting = false;
            _shiftCooldownTimer = 0f;
        }

        public void Update(float currentRPM, float speedAbs, float throttle01, float dt)
        {
            if (_spec == null)
                return;

            throttle01 = Mathf.Clamp01(throttle01);
            speedAbs = Mathf.Abs(speedAbs);

            // cooldown timer
            if (_shiftCooldownTimer > 0f)
                _shiftCooldownTimer = Mathf.Max(0f, _shiftCooldownTimer - dt);

            // shifting timer
            if (_isShifting)
            {
                _shiftTimer -= dt;
                if (_shiftTimer <= 0f)
                {
                    _isShifting = false;
                    _shiftTimer = 0f;
                }
                return;
            }

            // Automatic shifting logic
            if (_spec.transmissionType == GearboxSpec.TransmissionType.Automatic)
            {
                // Only auto-shift in forward gears
                if (_currentGear > 0 && _shiftCooldownTimer <= 0f)
                {
                    bool canUpshift =
                        currentRPM >= _spec.autoShiftUpRPM &&
                        speedAbs >= _spec.minSpeedForUpshift &&
                        _currentGear < _spec.gearRatios.Length;

                    bool canDownshift =
                        currentRPM <= _spec.autoShiftDownRPM &&
                        _currentGear > 1 &&
                        throttle01 > 0.1f;

                    if (canUpshift) ShiftUp();
                    else if (canDownshift) ShiftDown();
                }
            }
        }

        // --------------------------------------------------------------------
        // Ratios
        // --------------------------------------------------------------------

        /// <summary>
        /// Returns gear ratio WITHOUT final drive (signed).
        /// Reverse -> negative.
        /// Neutral -> 0.
        /// </summary>
        public float GetGearRatioOnly()
        {
            if (_spec == null || _isShifting)
                return 0f;

            if (_currentGear == -1)
                return -_spec.reverseGearRatio;

            if (_currentGear == 0)
                return 0f;

            if (_currentGear > 0 && _currentGear <= _spec.gearRatios.Length)
            {
                int idx = _currentGear - 1;
                return _spec.gearRatios[idx];
            }

            return 0f;
        }

        /// <summary>
        /// Returns gear ratio * finalDriveRatio (signed).
        /// This matches your previous GetCurrentGearRatio() behavior.
        /// </summary>
        public float GetCombinedRatio()
        {
            float gearOnly = GetGearRatioOnly();
            if (Mathf.Abs(gearOnly) < 1e-6f)
                return 0f;

            return gearOnly * _spec.finalDriveRatio;
        }

        /// <summary>
        /// Backward-compatible name (keeps old code working):
        /// returns COMBINED ratio (gear * finalDrive).
        /// </summary>
        public float GetCurrentGearRatio() => GetCombinedRatio();

        // --------------------------------------------------------------------
        // Shift operations
        // --------------------------------------------------------------------

        public bool ShiftUp()
        {
            if (!CanStartShift())
                return false;

            if (_currentGear == -1)
            {
                _currentGear = 0;
                StartShift();
                return true;
            }

            if (_currentGear == 0)
            {
                _currentGear = 1;
                StartShift();
                return true;
            }

            if (_currentGear >= _spec.gearRatios.Length)
                return false;

            _currentGear++;
            StartShift();
            return true;
        }

        public bool ShiftDown()
        {
            if (!CanStartShift())
                return false;

            if (_currentGear == -1)
                return false;

            if (_currentGear == 0)
            {
                _currentGear = -1;
                StartShift();
                return true;
            }

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

        public bool ShiftToReverse()
        {
            if (!CanStartShift())
                return false;

            if (_currentGear == -1)
                return false;

            _currentGear = -1;
            StartShift();
            return true;
        }

        public bool ShiftToNeutral()
        {
            if (!CanStartShift())
                return false;

            _currentGear = 0;
            StartShift();
            return true;
        }

        private bool CanStartShift()
        {
            if (_spec == null) return false;
            if (_isShifting) return false;
            if (_shiftCooldownTimer > 0f) return false;
            return true;
        }

        private void StartShift()
        {
            _isShifting = true;
            _shiftTimer = Mathf.Max(0.01f, _spec.shiftTime);

            // cooldown prevents immediate double shifts in the next frame
            _shiftCooldownTimer = Mathf.Max(_shiftCooldownTimer, MinTimeBetweenShifts);
        }
    }
}
