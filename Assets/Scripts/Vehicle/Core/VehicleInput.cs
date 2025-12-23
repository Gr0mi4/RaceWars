namespace Vehicle.Core
{
    /// <summary>
    /// Represents input values for vehicle control. All values are normalized to 0-1 or -1 to 1 ranges.
    /// </summary>
    public struct VehicleInput
    {
        /// <summary>
        /// Throttle input value. Range: 0 (no throttle) to 1 (full throttle).
        /// </summary>
        public float throttle;
        
        /// <summary>
        /// Brake input value. Range: 0 (no brake) to 1 (full brake). Currently reserved for future use.
        /// </summary>
        public float brake;
        
        /// <summary>
        /// Steering input value. Range: -1 (full left) to 1 (full right), 0 is straight.
        /// </summary>
        public float steer;
        
        /// <summary>
        /// Handbrake input value. Range: 0 (no handbrake) to 1 (full handbrake). Currently reserved for future use.
        /// </summary>
        public float handbrake;

        /// <summary>
        /// Shift up input. 1 when shift up is pressed, 0 otherwise.
        /// Used for manual gear shifting.
        /// </summary>
        public float shiftUp;

        /// <summary>
        /// Shift down input. 1 when shift down is pressed, 0 otherwise.
        /// Used for manual gear shifting.
        /// </summary>
        public float shiftDown;

        /// <summary>
        /// Returns a VehicleInput with all values set to zero (no input).
        /// </summary>
        public static VehicleInput Zero => new VehicleInput();
    }
}
