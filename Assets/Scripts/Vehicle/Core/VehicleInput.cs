namespace Vehicle.Core
{
    public struct VehicleInput
    {
        public float throttle;   // 0..1
        public float brake;      // 0..1 (reserved)
        public float steer;      // -1..1
        public float handbrake;  // 0..1 (reserved)

        public static VehicleInput Zero => new VehicleInput();
    }
}
