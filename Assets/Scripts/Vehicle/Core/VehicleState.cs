using UnityEngine;

namespace Vehicle.Core
{
    /// <summary>
    /// Represents the current state of the vehicle. Updated each frame and passed between modules.
    /// </summary>
    public struct VehicleState
    {
        /// <summary>
        /// Runtime per-wheel data (contact, forces, pose).
        /// </summary>
        public WheelRuntime[] wheels;

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

        /// <summary>
        /// Current engine RPM (revolutions per minute of the crankshaft). Set by engine/gearbox modules.
        /// This is the rotational speed of the engine crankshaft.
        /// </summary>
        public float engineRPM;

        /// <summary>
        /// Current wheel angular velocity in radians per second. Set by drive modules.
        /// This is the rotational speed of the wheels. Related to engine RPM through gearbox.
        /// Formula: ω_wheel = ω_engine / (gearRatio × finalDriveRatio)
        /// </summary>
        public float wheelAngularVelocity;

        /// <summary>
        /// Current gear index. -1 = reverse, 0 = neutral, 1+ = forward gears (1st, 2nd, etc.).
        /// Set by gearbox modules.
        /// </summary>
        public int currentGear;

        /// <summary>
        /// Wheel radius in meters. Set by wheel modules.
        /// </summary>
        public float wheelRadius;

        /// <summary>
        /// Wheel radius in meters. Set by wheel modules.
        /// </summary>
        /// /// <summary>
        /// Steering    
        /// </summary>
        public float steerAngleDeg;

        public float clutchEngaged01; // 0..1        
    }

    /// <summary>
    /// Per-wheel runtime data used by suspension/drive/steering systems.
    /// </summary>
    public struct WheelRuntime
    {
        public bool isGrounded;
        public Vector3 contactPoint;
        public Vector3 surfaceNormal;
        public float normalForce;
        public float compression;
        public float angularVelocity;
        public float compressionFiltered;
        public Vector3 debugDriveForce;
        public Vector3 debugLateralForce;
        public Vector3 wheelForwardWorld;
        public float wheelOmega;
        public float driveTorque;
        public float brakeTorque;
        public Vector3 debugLongForce;
        public Vector3 debugLatForce;
        public float debugUtil;
        public float debugFxDesired;
        public float debugFyDesired;
        public float debugMuFz;
        public float debugVLong;
        public float debugShaftTorque;
        public float debugOmegaTarget;
        public float debugMechRPM;
        public float debugLimiter;          // 0/1
        public float debugEngineDragTorque; // signed at wheel
    }
}
