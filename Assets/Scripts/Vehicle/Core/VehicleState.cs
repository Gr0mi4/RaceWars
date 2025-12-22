using UnityEngine;

namespace Vehicle.Core
{
    /// <summary>
    /// Represents the current state of the vehicle. Updated each frame and passed between modules.
    /// </summary>
    public struct VehicleState
    {
        /// <summary>
        /// Velocity of the vehicle in world space (m/s).
        /// </summary>
        public Vector3 worldVelocity;
        
        /// <summary>
        /// Velocity of the vehicle in local space (m/s).
        /// </summary>
        public Vector3 localVelocity;
        
        /// <summary>
        /// Current speed of the vehicle (m/s), calculated from local velocity magnitude.
        /// </summary>
        public float speed;
        
        /// <summary>
        /// Current yaw rate (angular velocity around Y axis) in radians per second.
        /// </summary>
        public float yawRate;
    }
}
