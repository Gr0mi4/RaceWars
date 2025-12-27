using UnityEngine;
using UDebug = UnityEngine.Debug;
using Vehicle.Core;
using Vehicle.Specs;

namespace Vehicle.Systems
{
    /// <summary>
    /// Steering system.
    ///
    /// This version writes "wheel forward direction" for front wheels into WheelRuntime,
    /// so DriveSystem can apply traction along the steered wheel direction (critical for FWD).
    ///
    /// NOTE: Yaw-torque steering (directly rotating the body) is disabled by default to avoid
    /// fighting with wheel-based forces. You can re-enable it later as a stability assist.
    /// </summary>
    public sealed class SteeringSystem : IVehicleModule
    {
        private readonly SteeringSpec _spec;

        private const float Epsilon = 0.001f;
        private const float GravityMagnitude = 9.81f;
        private const float SteerExpo = 1.5f; // soften around center

        // Optional: keep old yaw controller as assist (disabled by default)
        private const bool EnableYawAssist = false;

        public SteeringSystem(SteeringSpec spec)
        {
            _spec = spec;

            if (_spec == null)
            {
                UDebug.LogError("[SteeringSystem] SteeringSpec is null! Steering will be disabled.");
                return;
            }

            if (_spec.wheelbase < Epsilon)
            {
                UDebug.LogWarning($"[SteeringSystem] Invalid wheelbase ({_spec.wheelbase}), steering may behave incorrectly.");
            }
        }

        public void Tick(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
        {
            if (_spec == null || ctx.rb == null)
                return;

            // Ensure wheels array exists (it is owned by VehicleState)
            EnsureWheelArray(ref state, ctx);

            // 1) Compute steer angle (deg) the same way as before, but we always compute it
            // even at low speed, because wheel direction is needed for drive direction.
            float steerAngleDeg = ComputeSteerAngleDeg(input, state);

            // Store for telemetry / debug
            state.steerAngleDeg = steerAngleDeg;

            // 2) Write per-wheel forward directions (front wheels rotated by steerAngleDeg)
            WriteWheelForwardDirections(ref state, ctx, steerAngleDeg);

            // 3) Optional yaw assist (old behavior) - disabled by default
            if (EnableYawAssist && ApplyYawAssist(input, state, ctx, out float yawTorque))
            {
                ctx.rb.AddTorque(new Vector3(0f, yawTorque, 0f), ForceMode.Force);
            }
        }

        // ------------------------------------------------------------
        // Steering angle
        // ------------------------------------------------------------

        private float ComputeSteerAngleDeg(in VehicleInput input, in VehicleState state)
        {
            float steer = input.steer;
            if (Mathf.Abs(steer) < Epsilon)
                return 0f;

            float absVx = Mathf.Abs(state.localVelocity.z);

            float steerInput = ApplySteerCurve(steer);
            float speedScale = GetSpeedSteerScale(absVx);

            return steerInput * _spec.maxSteerAngle * speedScale;
        }

        private float ApplySteerCurve(float rawInput)
        {
            float sign = Mathf.Sign(rawInput);
            float mag = Mathf.Pow(Mathf.Abs(rawInput), SteerExpo);
            return sign * mag;
        }

        private float GetSpeedSteerScale(float speed)
        {
            float s0 = Mathf.Max(Epsilon, _spec.steerReductionStartSpeed);
            float s1 = Mathf.Max(s0 + Epsilon, _spec.steerReductionEndSpeed);
            if (speed <= s0) return 1f;
            if (speed >= s1) return _spec.highSpeedSteerMultiplier;
            float t = (speed - s0) / (s1 - s0);
            return Mathf.Lerp(1f, _spec.highSpeedSteerMultiplier, t);
        }

        // ------------------------------------------------------------
        // Wheel forward directions for drive force
        // ------------------------------------------------------------

        private void EnsureWheelArray(ref VehicleState state, in VehicleContext ctx)
        {
            int count = (ctx.wheelSpec != null && ctx.wheelSpec.wheelOffsets != null)
                ? ctx.wheelSpec.wheelOffsets.Length
                : 0;

            if (count <= 0)
                return;

            if (state.wheels == null || state.wheels.Length != count)
                state.wheels = new WheelRuntime[count];
        }

        private void WriteWheelForwardDirections(ref VehicleState state, in VehicleContext ctx, float steerAngleDeg)
        {
            if (state.wheels == null || state.wheels.Length == 0)
                return;

            // No WheelSpec? then all wheels follow body forward
            if (ctx.wheelSpec == null || ctx.wheelSpec.wheelOffsets == null || ctx.wheelSpec.wheelOffsets.Length != state.wheels.Length)
            {
                for (int i = 0; i < state.wheels.Length; i++)
                    state.wheels[i].wheelForwardWorld = ctx.Forward;

                return;
            }

            Quaternion steerRot = Quaternion.AngleAxis(steerAngleDeg, ctx.Up);

            for (int i = 0; i < state.wheels.Length; i++)
            {
                // Front wheel detection by offset Z (same convention as in your other systems)
                bool isFront = ctx.wheelSpec.wheelOffsets[i].z >= 0f;

                Vector3 fwd = ctx.Forward;
                if (isFront)
                    fwd = steerRot * ctx.Forward;

                state.wheels[i].wheelForwardWorld = fwd;
            }
        }

        // ------------------------------------------------------------
        // Optional yaw assist (old bicycle-model controller)
        // ------------------------------------------------------------

        private bool ApplyYawAssist(in VehicleInput input, in VehicleState state, in VehicleContext ctx, out float yawTorque)
        {
            yawTorque = 0f;

            if (Mathf.Abs(input.steer) < Epsilon)
                return false;

            float vx = state.localVelocity.z;
            float absVx = Mathf.Abs(vx);

            if (absVx < _spec.minForwardSpeed)
                return false;

            float steerInput = ApplySteerCurve(input.steer);
            float steerAngleDeg = steerInput * _spec.maxSteerAngle * GetSpeedSteerScale(absVx);
            float steerAngleRad = steerAngleDeg * Mathf.Deg2Rad;

            float effectiveVx = Mathf.Max(absVx, _spec.minForwardSpeed);
            float yawRateDesired = (effectiveVx / _spec.wheelbase) * Mathf.Tan(steerAngleRad);

            float baseMu = (ctx.wheelSpec != null && ctx.wheelSpec.friction > Epsilon) ? ctx.wheelSpec.friction : 0.8f;
            float ayMax = baseMu * GravityMagnitude;
            float yawRateMaxBase = ayMax / effectiveVx;

            float brakeUsage = Mathf.Abs(input.brake);
            float throttleUsage = Mathf.Abs(input.throttle) * _spec.throttleFrictionEffect;
            float longUsage = Mathf.Clamp01(brakeUsage + throttleUsage);

            float frictionCircleFactor = longUsage * _spec.frictionCircleStrength;
            float latGripFactor = Mathf.Sqrt(Mathf.Max(0f, 1f - frictionCircleFactor * frictionCircleFactor));
            float yawRateMax = yawRateMaxBase * latGripFactor;

            if (input.handbrake > 0.5f)
                yawRateMax *= _spec.handbrakeGripMultiplier;

            float yawRateTarget = Mathf.Clamp(yawRateDesired, -yawRateMax, yawRateMax);

            float yawRateError = yawRateTarget - state.yawRate;
            float yawAccel = Mathf.Clamp(yawRateError / _spec.yawResponseTime, -_spec.maxYawAccel, _spec.maxYawAccel);

            float momentOfInertia = ctx.rb.mass * _spec.wheelbase * _spec.wheelbase;
            yawTorque = yawAccel * momentOfInertia;

            if (!float.IsFinite(yawTorque))
            {
                UDebug.LogWarning($"[SteeringSystem] Invalid yawTorque calculated: {yawTorque}. Clamping to zero.");
                yawTorque = 0f;
                return false;
            }

            float maxTorque = ctx.rb.mass * 1000f;
            yawTorque = Mathf.Clamp(yawTorque, -maxTorque, maxTorque);

            return true;
        }
    }
}
