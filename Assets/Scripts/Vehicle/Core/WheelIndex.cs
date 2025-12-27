namespace Vehicle.Core
{
    /// <summary>
    /// Wheel index convention (IMPORTANT: keep consistent everywhere):
    /// - 0 = Front Left  (FL)
    /// - 1 = Rear  Left  (RL)
    /// - 2 = Front Right (FR)
    /// - 3 = Rear  Right (RR)
    ///
    /// Axles:
    /// - Front axle: 0, 2
    /// - Rear  axle: 1, 3
    /// </summary>
    public static class WheelIndex
    {
        public const int FrontLeft = 0;
        public const int RearLeft = 1;
        public const int FrontRight = 2;
        public const int RearRight = 3;

        public static readonly int[] Front = { FrontLeft, FrontRight };
        public static readonly int[] Rear = { RearLeft, RearRight };
        public static readonly int[] All4 = { FrontLeft, RearLeft, FrontRight, RearRight };

        public static bool IsFront(int idx) => idx == FrontLeft || idx == FrontRight;
        public static bool IsRear(int idx) => idx == RearLeft || idx == RearRight;

        public static string Name4(int idx) => idx switch
        {
            FrontLeft => "FL",
            RearLeft => "RL",
            FrontRight => "FR",
            RearRight => "RR",
            _ => idx.ToString()
        };
    }
}


