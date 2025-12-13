using UnityEngine;

namespace Vehicle.Core
{
    public struct VehicleState
    {
        public Vector3 worldVelocity;
        public Vector3 localVelocity;
        public float speed;
        public float yawRate;
    }
}
