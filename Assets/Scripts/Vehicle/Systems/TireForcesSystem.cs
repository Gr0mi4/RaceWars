using UnityEngine;
using Vehicle.Core;
using Vehicle.Specs;

namespace Vehicle.Systems
{
    /// <summary>
    /// TireForcesSystem:
    /// - Computes longitudinal (Fx) from driveTorque/brakeTorque and wheel dynamics (wheelOmega)
    /// - Computes lateral (Fy) from lateral slip speed (damping-style)
    /// - Applies full friction circle clamp: sqrt(Fx^2 + Fy^2) <= mu * Fz
    /// - Applies force at contact patch (AddForceAtPosition)
    /// - Updates wheelOmega (wheelspin / lockup become possible)
    /// - Writes debug vectors for gizmos
    /// </summary>
    public sealed class TireForcesSystem : IVehicleModule
    {
        private const float Epsilon = 1e-5f;

        // Move these to WheelSpec later if you want
        private const float DefaultWheelInertia = 1.2f;     // kg*m^2 (rough)
        private const float WheelOmegaDamping = 0.5f;       // 1/s (prevents runaway)
        private const float MinSpeedForSign = 0.1f;         // m/s for brake direction decision

        public void Tick(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
        {
            var wheelSpec = ctx.wheelSpec ?? ctx.spec?.wheelSpec;
            if (ctx.rb == null || wheelSpec == null)
                return;

            if (state.wheels == null || state.wheels.Length == 0)
                return;

            float mu = Mathf.Max(0f, wheelSpec.friction);

            float radius = state.wheelRadius > 0.01f ? state.wheelRadius : wheelSpec.wheelRadius;
            radius = Mathf.Max(0.01f, radius);

            // Lateral damping stiffness (your previous logic)
            float baseGrip = Mathf.Max(0f, wheelSpec.sideGrip);
            float grip = baseGrip;
            if (input.handbrake > 0.5f)
                grip *= Mathf.Clamp01(wheelSpec.handbrakeGripMultiplier);

            float mass = Mathf.Max(1f, ctx.rb.mass);
            float latStiffness = mass * grip; // kg/s

            // Reset debug each tick
            for (int i = 0; i < state.wheels.Length; i++)
            {
                state.wheels[i].debugLongForce = Vector3.zero;
                state.wheels[i].debugLatForce = Vector3.zero;
                state.wheels[i].debugUtil = 0f;

                // legacy fields (so your existing renderer keeps working)
                state.wheels[i].debugDriveForce = Vector3.zero;
                state.wheels[i].debugLateralForce = Vector3.zero;
            }

            for (int i = 0; i < state.wheels.Length; i++)
            {
                ref WheelRuntime w = ref state.wheels[i];

                if (!w.isGrounded)
                    continue;

                if (w.surfaceNormal.sqrMagnitude < Epsilon)
                    continue;

                Vector3 n = w.surfaceNormal.normalized;

                // Wheel forward direction (from SteeringSystem), fallback to body forward
                Vector3 fwd = (w.wheelForwardWorld.sqrMagnitude > Epsilon) ? w.wheelForwardWorld : ctx.Forward;
                fwd = Vector3.ProjectOnPlane(fwd, n);
                if (fwd.sqrMagnitude < Epsilon)
                    continue;
                fwd.Normalize();

                Vector3 right = Vector3.Cross(n, fwd);
                if (right.sqrMagnitude < Epsilon)
                    continue;
                right.Normalize();

                // Contact patch velocity
                Vector3 v = ctx.rb.GetPointVelocity(w.contactPoint);
                float vLong = Vector3.Dot(v, fwd);
                float vLat  = Vector3.Dot(v, right);

                // Normal load
                float fz = Mathf.Max(0f, w.normalForce);
                float fMax = mu * fz;
                if (fMax <= 0f)
                    continue;

                // ---------------------------------------------------------------------
                // 1) Longitudinal: convert torques -> desired Fx (before circle clamp)
                // ---------------------------------------------------------------------
                float driveTq = w.driveTorque;          // signed (forward/reverse)
                float brakeTq = Mathf.Max(0f, w.brakeTorque);

                // Brake torque opposes current rotation; if omega ~ 0, oppose motion
                float brakeSign = 0f;
                if (Mathf.Abs(w.wheelOmega) > 0.5f) brakeSign = Mathf.Sign(w.wheelOmega);
                else if (Mathf.Abs(vLong) > MinSpeedForSign) brakeSign = Mathf.Sign(vLong);

                float brakeApplied = brakeTq * brakeSign;

                float netTq = driveTq - brakeApplied;

                // Fx "requested" by net torque
                float fxDesired = netTq / radius;

                // ---------------------------------------------------------------------
                // 2) Lateral: damping-style Fy from vLat (your previous model)
                // ---------------------------------------------------------------------
                float fyDesired = 0f;
                if (Mathf.Abs(vLat) > 0.001f && latStiffness > 0f)
                {
                    fyDesired = (-vLat * latStiffness);
                }

                // ---------------------------------------------------------------------
                // 3) Full friction circle clamp (Fx + Fy together)
                // ---------------------------------------------------------------------
                float fx = fxDesired;
                float fy = fyDesired;

                float mag = Mathf.Sqrt(fx * fx + fy * fy);
                if (mag > fMax && mag > Epsilon)
                {
                    float k = fMax / mag;
                    fx *= k;
                    fy *= k;
                }

                // ---------------------------------------------------------------------
                // 4) Apply forces at contact patch
                // ---------------------------------------------------------------------
                Vector3 fLong = fwd * fx;
                Vector3 fLatV = right * fy;
                Vector3 force = fLong + fLatV;

                ctx.rb.AddForceAtPosition(force, w.contactPoint, ForceMode.Force);

                // ---------------------------------------------------------------------
                // 5) Update wheelOmega (wheelspin / lockup)
                //     ground reaction torque = Fx * R
                // ---------------------------------------------------------------------
                float groundTq = fx * radius;
                float wheelNetTq = netTq - groundTq;

                float I = DefaultWheelInertia;
                float domega = wheelNetTq / Mathf.Max(0.05f, I);
                w.wheelOmega += domega * ctx.dt;

                // mild damping
                w.wheelOmega *= Mathf.Exp(-WheelOmegaDamping * ctx.dt);

                // ---------------------------------------------------------------------
                // Debug outputs
                // ---------------------------------------------------------------------
                w.debugLongForce = fLong;
                w.debugLatForce = fLatV;

                // utilization of friction budget (how close to limit)
                float used = Mathf.Sqrt(fxDesired * fxDesired + fyDesired * fyDesired);
                w.debugUtil = used / Mathf.Max(Epsilon, fMax);

                // legacy debug (keep your old arrows working)
                w.debugDriveForce = fLong;
                w.debugLateralForce = fLatV;
            }
        }
    }
}
