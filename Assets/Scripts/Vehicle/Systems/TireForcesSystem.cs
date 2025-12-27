using UnityEngine;
using Vehicle.Core;
using Vehicle.Specs;

namespace Vehicle.Systems
{
    /// <summary>
    /// TireForcesSystem (with longitudinal slip):
    /// - Longitudinal force Fx comes from slip speed: (wheelOmega*R - vLong)
    /// - Lateral force Fy is damping-style from vLat (as before)
    /// - Full friction circle clamp: sqrt(Fx^2 + Fy^2) <= mu * Fz
    /// - Applies forces at contact patch (AddForceAtPosition)
    /// - Updates wheelOmega from applied torques and ground reaction torque
    /// </summary>
    public sealed class TireForcesSystem : IVehicleModule
    {
        private const float Epsilon = 1e-5f;

        // Wheel dynamics
        private const float DefaultWheelInertia = 1.2f; // kg*m^2
        private const float WheelOmegaDamping = 0.5f;   // 1/s
        private const float MinSpeedForSign = 0.1f;     // m/s

        // --- NEW: longitudinal slip stiffness (tuning knob) ---
        // Converts slipSpeed (m/s) into force demand (N).
        // We scale by Fz to get "load sensitivity" in a simple way.
        // Typical workable range: 10..60.
        private const float LongSlipStiffness = 25f; // 1/(m/s)

        // Optional: small rolling resistance torque (stabilizes “infinite coast” feel)
        private const float RollingResistanceTorque = 2.0f; // Nm (per wheel)

        public void Tick(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
        {
            var wheelSpec = ctx.wheelSpec ?? ctx.spec?.wheelSpec;
            if (ctx.rb == null || wheelSpec == null) return;
            if (state.wheels == null || state.wheels.Length == 0) return;

            float mu = Mathf.Max(0f, wheelSpec.friction);

            float radius = state.wheelRadius > 0.01f ? state.wheelRadius : wheelSpec.wheelRadius;
            radius = Mathf.Max(0.01f, radius);

            // Lateral damping (your previous approach)
            float baseGrip = Mathf.Max(0f, wheelSpec.sideGrip);
            float grip = baseGrip;
            if (input.handbrake > 0.5f)
                grip *= Mathf.Clamp01(wheelSpec.handbrakeGripMultiplier);

            float mass = Mathf.Max(1f, ctx.rb.mass);
            float latStiffness = mass * grip; // kg/s

            // Reset debug
            for (int i = 0; i < state.wheels.Length; i++)
            {
                state.wheels[i].debugLongForce = Vector3.zero;
                state.wheels[i].debugLatForce = Vector3.zero;
                state.wheels[i].debugUtil = 0f;

                state.wheels[i].debugDriveForce = Vector3.zero;
                state.wheels[i].debugLateralForce = Vector3.zero;

                state.wheels[i].debugFxDesired = 0f;
                state.wheels[i].debugFyDesired = 0f;
                state.wheels[i].debugMuFz = 0f;
                state.wheels[i].debugVLong = 0f;
            }

            for (int i = 0; i < state.wheels.Length; i++)
            {
                ref WheelRuntime w = ref state.wheels[i];
                if (!w.isGrounded) continue;
                if (w.surfaceNormal.sqrMagnitude < Epsilon) continue;

                Vector3 n = w.surfaceNormal.normalized;

                // Wheel forward direction (from SteeringSystem), fallback to body forward
                Vector3 fwd = (w.wheelForwardWorld.sqrMagnitude > Epsilon) ? w.wheelForwardWorld : ctx.Forward;
                fwd = Vector3.ProjectOnPlane(fwd, n);
                if (fwd.sqrMagnitude < Epsilon) continue;
                fwd.Normalize();

                Vector3 right = Vector3.Cross(n, fwd);
                if (right.sqrMagnitude < Epsilon) continue;
                right.Normalize();

                // Contact patch velocity
                Vector3 v = ctx.rb.GetPointVelocity(w.contactPoint);
                float vLong = Vector3.Dot(v, fwd);
                float vLat = Vector3.Dot(v, right);

                // Normal load and friction budget
                float fz = Mathf.Max(0f, w.normalForce);
                float fMax = mu * fz;
                if (fMax <= 0f) continue;

                // -----------------------------
                // 1) Torques on wheel (input)
                // -----------------------------
                float driveTq = w.driveTorque;          // signed
                float brakeTq = Mathf.Max(0f, w.brakeTorque);

                // Brake torque opposes rotation; if omega ~ 0, oppose motion
                float brakeSign = 0f;
                if (Mathf.Abs(w.wheelOmega) > 0.5f) brakeSign = Mathf.Sign(w.wheelOmega);
                else if (Mathf.Abs(vLong) > MinSpeedForSign) brakeSign = Mathf.Sign(vLong);

                float brakeApplied = brakeTq * brakeSign;

                // Rolling resistance opposes rotation (small стабилизатор)
                float rrSign = (Mathf.Abs(w.wheelOmega) > 0.5f) ? Mathf.Sign(w.wheelOmega)
                              : (Mathf.Abs(vLong) > MinSpeedForSign ? Mathf.Sign(vLong) : 0f);
                float rollingTq = RollingResistanceTorque * rrSign;

                // Net torque on wheel BEFORE ground reaction
                float netTq = driveTq - brakeApplied - rollingTq;

                // -----------------------------
                // 2) Longitudinal slip -> Fx demand
                // -----------------------------
                // Slip speed (m/s): wheel surface speed minus ground speed along wheel forward.
                float wheelSurfaceSpeed = w.wheelOmega * radius; // m/s (signed)
                float slipSpeed = wheelSurfaceSpeed - vLong;     // m/s (signed)

                // Simple “brush-like” model: FxDesired grows with slipSpeed and Fz.
                // Units: (N) = (N) * (1/(m/s)) * (m/s)
                float cLong = fz * LongSlipStiffness;
                float fxDesired = cLong * slipSpeed;

                // -----------------------------
                // 3) Lateral force (your damping model)
                // -----------------------------
                float fyDesired = 0f;
                if (Mathf.Abs(vLat) > 0.001f && latStiffness > 0f)
                    fyDesired = (-vLat * latStiffness);

                // -----------------------------
                // 4) Friction circle clamp (Fx + Fy)
                // -----------------------------
                float fx = fxDesired;
                float fy = fyDesired;

                float mag = Mathf.Sqrt(fx * fx + fy * fy);
                if (mag > fMax && mag > Epsilon)
                {
                    float k = fMax / mag;
                    fx *= k;
                    fy *= k;
                }

                // -----------------------------
                // 5) Apply forces
                // -----------------------------
                Vector3 fLong = fwd * fx;
                Vector3 fLatV = right * fy;
                Vector3 force = fLong + fLatV;

                ctx.rb.AddForceAtPosition(force, w.contactPoint, ForceMode.Force);

                // -----------------------------
                // 6) Update wheelOmega from torques
                // Ground reaction torque = Fx * R (opposes net torque)
                // -----------------------------
                float groundTq = fx * radius;
                float wheelNetTq = netTq - groundTq;

                float I = Mathf.Max(0.05f, DefaultWheelInertia);
                float domega = wheelNetTq / I;
                w.wheelOmega += domega * ctx.dt;

                // mild damping prevents runaway oscillations
                w.wheelOmega *= Mathf.Exp(-WheelOmegaDamping * ctx.dt);

                // -----------------------------
                // Debug outputs
                // -----------------------------
                w.debugLongForce = fLong;
                w.debugLatForce = fLatV;

                w.debugVLong = vLong;
                w.debugFxDesired = fxDesired;
                w.debugFyDesired = fyDesired;
                w.debugMuFz = fMax;

                // utilization (based on desired vs budget)
                float used = Mathf.Sqrt(fxDesired * fxDesired + fyDesired * fyDesired);
                w.debugUtil = used / Mathf.Max(Epsilon, fMax);

                // legacy fields
                w.debugDriveForce = fLong;
                w.debugLateralForce = fLatV;
            }
        }
    }
}
