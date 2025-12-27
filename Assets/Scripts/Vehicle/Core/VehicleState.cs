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
    // ========================================================================
    // CONTACT & SUSPENSION (set by SuspensionSystem)
    // ========================================================================

    /// <summary>
    /// Whether this wheel is in contact with the ground (raycast hit).
    /// </summary>
    public bool isGrounded;

    /// <summary>
    /// World-space contact point where the wheel touches the ground (from raycast hit.point).
    /// </summary>
    public Vector3 contactPoint;

    /// <summary>
    /// Surface normal at the contact point (from raycast hit.normal).
    /// Used to project forces onto the contact plane.
    /// </summary>
    public Vector3 surfaceNormal;

    /// <summary>
    /// Normal force (Fz) acting on this wheel in Newtons.
    /// Calculated from spring+damper forces. This is the "friction budget" (μ * Fz).
    /// </summary>
    public float normalForce;

    /// <summary>
    /// Raw suspension compression distance in meters (restLength - hitDistance).
    /// Not smoothed, may jitter on rough surfaces.
    /// </summary>
    public float compression;

    /// <summary>
    /// Filtered/smoothed suspension compression in meters.
    /// Used for stable force calculations and anti-roll bar.
    /// </summary>
    public float compressionFiltered;

    // ========================================================================
    // WHEEL GEOMETRY & ORIENTATION (set by SteeringSystem)
    // ========================================================================

    /// <summary>
    /// World-space forward direction of the wheel (for front wheels: rotated by steer angle).
    /// Used as the "fwd" axis for calculating vLong, Fx, and slip angle α.
    /// </summary>
    public Vector3 wheelForwardWorld;

    // ========================================================================
    // WHEEL KINEMATICS
    // ========================================================================

    /// <summary>
    /// Angular velocity of the wheel around its rotation axis in rad/s (signed).
    /// Updated by TireForcesSystem from drive/brake torques and ground reaction.
    /// Used to calculate wheel surface speed (wheelOmega * radius) for slip ratio κ.
    /// </summary>
    public float wheelOmega;

    /// <summary>
    /// Angular velocity derived from forward speed (vForward / radius) in rad/s.
    /// Set by SuspensionSystem for telemetry/debug. Not used in physics calculations.
    /// </summary>
    public float angularVelocity;

    // ========================================================================
    // DRIVE & BRAKE INPUTS (set by DrivetrainSystem / BrakingSystem)
    // ========================================================================

    /// <summary>
    /// Drive torque applied to this wheel in N⋅m (signed: positive = forward, negative = reverse).
    /// Set by DrivetrainSystem based on engine torque and drive type (FWD/RWD/AWD).
    /// </summary>
    public float driveTorque;

    /// <summary>
    /// Brake torque applied to this wheel in N⋅m (always ≥ 0).
    /// Set by BrakingSystem. Actual direction depends on wheelOmega/vLong sign.
    /// </summary>
    public float brakeTorque;

    // ========================================================================
    // TIRE MODEL v2: STATE (for build-up/relaxation)
    // ========================================================================

    /// <summary>
    /// Previous frame's longitudinal force Fx (after combined slip + build-up).
    /// Used for relaxation filter: Fx = lerp(fxPrev, FxTarget, alpha).
    /// Gives "mass" feel and prevents instant force jumps.
    /// </summary>
    public float fxPrev;

    /// <summary>
    /// Previous frame's lateral force Fy (after combined slip + build-up).
    /// Used for relaxation filter: Fy = lerp(fyPrev, FyTarget, alpha).
    /// </summary>
    public float fyPrev;

    // ========================================================================
    // DEBUG: TIRE MODEL v2 (slip angles, forces)
    // ========================================================================

    /// <summary>
    /// Slip angle α in radians: angle between wheel forward direction and velocity direction.
    /// Positive = wheel pointing left of velocity (right-hand turn). This is the primary cause of Fy.
    /// </summary>
    public float debugSlipAngleRad;

    /// <summary>
    /// Slip ratio κ (normalized): (wheelSurfaceSpeed - vLong) / max(vRef, |vLong|).
    /// Positive = wheel spinning faster than ground (acceleration slip).
    /// Negative = wheel slower than ground (braking slip/blocking). Primary cause of Fx.
    /// </summary>
    public float debugSlipRatio;

    /// <summary>
    /// Raw longitudinal force Fx before combined slip and build-up (from tanh(Cx * κ)).
    /// Shows what the tire "wants" before friction circle limits.
    /// </summary>
    public float debugFxRaw;

    /// <summary>
    /// Raw lateral force Fy before combined slip and build-up (from tanh(Cy * α)).
    /// Shows what the tire "wants" before friction circle limits.
    /// </summary>
    public float debugFyRaw;

    /// <summary>
    /// Final longitudinal force Fx after combined slip (friction circle) and build-up (relaxation).
    /// This is the actual force applied to the vehicle via AddForceAtPosition.
    /// </summary>
    public float debugFxFinal;

    /// <summary>
    /// Final lateral force Fy after combined slip (friction circle) and build-up (relaxation).
    /// This is the actual force applied to the vehicle via AddForceAtPosition.
    /// </summary>
    public float debugFyFinal;

    // ========================================================================
    // DEBUG: CURRENT TIRE MODEL (legacy, will be replaced by v2 above)
    // ========================================================================

    /// <summary>
    /// World-space longitudinal force vector (fwd * fx) for gizmos/debug visualization.
    /// Legacy field, duplicates debugLongForce.
    /// </summary>
    public Vector3 debugDriveForce;

    /// <summary>
    /// World-space lateral force vector (right * fy) for gizmos/debug visualization.
    /// Legacy field, duplicates debugLateralForce.
    /// </summary>
    public Vector3 debugLateralForce;

    /// <summary>
    /// World-space longitudinal force vector (fwd * fx) for gizmos/debug visualization.
    /// </summary>
    public Vector3 debugLongForce;

    /// <summary>
    /// World-space lateral force vector (right * fy) for gizmos/debug visualization.
    /// </summary>
    public Vector3 debugLatForce;

    /// <summary>
    /// Desired longitudinal force Fx before friction circle clamp (from slipSpeed * stiffness).
    /// Legacy: will be replaced by debugFxRaw after v2 migration.
    /// </summary>
    public float debugFxDesired;

    /// <summary>
    /// Desired lateral force Fy before friction circle clamp (from -vLat * latStiffness).
    /// Legacy: will be replaced by debugFyRaw after v2 migration.
    /// </summary>
    public float debugFyDesired;

    /// <summary>
    /// Maximum available force from friction: μ * Fz (friction budget).
    /// Used for friction circle clamp and utilization calculation.
    /// </summary>
    public float debugMuFz;

    /// <summary>
    /// Longitudinal velocity component in wheel's forward direction (m/s).
    /// Used for slip calculations and telemetry.
    /// </summary>
    public float debugVLong;

    /// <summary>
    /// Lateral velocity component in wheel's right direction (m/s).
    /// Positive = sliding right, negative = sliding left.
    /// </summary>
    public float debugVLat;

    /// <summary>
    /// Flag indicating if lateral forces should be zeroed (for drift prevention).
    /// True when vLong is too low or lateral speed is excessive.
    /// </summary>
    public bool debugShouldZeroLateral;

    /// <summary>
    /// Tire utilization: sqrt(Fx² + Fy²) / (μ * Fz), clamped to [0, 1+].
    /// 1.0 = at friction limit, >1.0 = exceeded (shouldn't happen after clamp).
    /// </summary>
    public float debugUtil;

}
}
