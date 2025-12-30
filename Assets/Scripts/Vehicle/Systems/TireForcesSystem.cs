using UnityEngine;
using Vehicle.Core;
using Vehicle.Specs;

namespace Vehicle.Systems
{
    /// <summary>
    /// TireForcesSystem (tire model v2):
    /// - Computes Fx from slip ratio κ, Fy from slip angle α
    /// - Combined slip clamp (normalized utilization)
    /// - Applies forces at contact patch (AddForceAtPosition)
    /// - Updates wheelOmega from applied torques and ground reaction torque
    /// </summary>
    public sealed class TireForcesSystem : IVehicleModule
    {
        private const float Epsilon = 1e-5f;

        // Wheel dynamics
        // Default fallback values for wheel inertia calculation (used if WheelSpec is missing)
        private const float DefaultWheelMass = 18f;              // kg (wheel + tire assembly)
        private const float DefaultWheelInertiaCoefficient = 0.8f; // 0.5=disc, 1.0=ring, typical wheel ~0.8
        private const float WheelOmegaDamping = 0.5f;   // 1/s
        private const float MinSpeedForSign = 0.1f;     // m/s
        private const float LongitudinalERP = 0.2f;     // 0..1 (soft constraint: fraction of slip corrected per step)
        private const float MuStaticMultiplier = 1.05f; // static vs kinetic friction hysteresis (dimensionless)
        private const float MuKineticMultiplier = 1.0f;

        // Optional: small rolling resistance torque (stabilizes "infinite coast" feel)
        private const float RollingResistanceTorque = 2.0f; // Nm (per wheel)

        // ========================================================================
        // TIRE MODEL v2: Slip angle (α) / Slip ratio (κ) + Combined slip
        // ========================================================================

        // Low-speed protection for slip calculations (prevents division by zero / infinite α/κ)
        /// <summary>
        /// Reference speed for slip angle calculation (m/s). Prevents α from exploding at very low vLong.
        /// Typical range: 0.5..2.0. Lower = more sensitive at low speed, but risk of noise.
        /// </summary>
        private const float VRefAlpha = 1.0f; // m/s

        /// <summary>
        /// Reference speed for slip ratio calculation (m/s). Prevents κ from exploding at very low vLong.
        /// Typical range: 1.0..3.0. Should be ≥ VRefAlpha.
        /// </summary>
        private const float VRefKappa = 2.0f; // m/s

        // Friction coefficient scales (front/rear balance - key tuning knob for understeer/oversteer)
        /// <summary>
        /// Lateral friction scale for front wheels. 1.0 = same as base μ.
        /// Lower front = more understeer. Higher front = more oversteer.
        /// </summary>
        private const float MuLatScaleFront = 1.0f;

        /// <summary>
        /// Lateral friction scale for rear wheels. 1.0 = same as base μ.
        /// </summary>
        private const float MuLatScaleRear = 1.0f;

        /// <summary>
        /// Longitudinal friction scale for front wheels. Usually 1.0 (same as base μ).
        /// </summary>
        private const float MuLongScaleFront = 1.0f;

        /// <summary>
        /// Longitudinal friction scale for rear wheels. Usually 1.0 (same as base μ).
        /// </summary>
        private const float MuLongScaleRear = 1.0f;

        /// <summary>
        /// Lateral stiffness coefficient for front wheels (dimensionless tuning factor).
        /// Higher = more responsive to slip angle α, steeper Fy(α) curve.
        /// Typical range: 4..15. Start with 8.
        /// </summary>
        private const float CyFront = 8f;

        /// <summary>
        /// Lateral stiffness coefficient for rear wheels.
        /// </summary>
        private const float CyRear = 8f;

        // Lateral damping (small stabilizer - optional, start with 0)
        /// <summary>
        /// Lateral velocity damping coefficient (kg/s). Small stabilizer to prevent oscillations.
        /// Start with 0.0. If needed, typical range: 0..(mass * 0.5).
        /// Too high = "on rails" feel, kills slip angle response.
        /// </summary>
        private const float LatDamp = 0.0f; // kg/s

        // Minimum speed threshold for slip calculations (dead zone)
        // Below this speed, ignore slip angle/ratio to prevent force accumulation at standstill
        private const float MinSpeedForSlip = 0.5f; // m/s (increased to prevent drift at low speeds)

        public void Tick(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
        {
            var wheelSpec = ctx.wheelSpec ?? ctx.spec?.wheelSpec;
            if (ctx.rb == null || wheelSpec == null) return;
            if (state.wheels == null || state.wheels.Length == 0) return;

            float mu = Mathf.Max(0f, wheelSpec.friction);

            float radius = state.wheelRadius > 0.01f ? state.wheelRadius : wheelSpec.wheelRadius;
            radius = Mathf.Max(0.01f, radius);

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

                // Initialize build-up state: reset to zero if not grounded OR if forces are uninitialized
                // This prevents force accumulation from uninitialized memory and force "jump" when wheel touches ground
                if (!state.wheels[i].isGrounded)
                {
                    state.wheels[i].fxPrev = 0f;
                    state.wheels[i].fyPrev = 0f;
                }
                // Also initialize on first frame (if values are NaN or uninitialized)
                else if (float.IsNaN(state.wheels[i].fxPrev) || float.IsNaN(state.wheels[i].fyPrev))
                {
                    state.wheels[i].fxPrev = 0f;
                    state.wheels[i].fyPrev = 0f;
                }
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

                // Contact patch velocity (projected onto contact plane to remove vertical component)
                Vector3 v = ctx.rb.GetPointVelocity(w.contactPoint);
                Vector3 vPlane = Vector3.ProjectOnPlane(v, n); // Project onto contact plane
                float vLong = Vector3.Dot(vPlane, fwd);
                float vLat = Vector3.Dot(vPlane, right);

                // -----------------------------
                // Slip angle (α) calculation (κ will be computed AFTER force resolution)
                // -----------------------------
                float vLongAbs = Mathf.Abs(vLong);
                float alphaRad;

                if (vLongAbs < MinSpeedForSlip)
                {
                    // At standstill or very low speed, ignore lateral slip (α)
                    alphaRad = 0f;
                }
                else
                {
                    // Normal slip calculations
                    // Slip angle: angle between wheel forward direction and velocity direction
                    // Positive = wheel pointing left of velocity (right-hand turn)
                    alphaRad = Mathf.Atan2(vLat, Mathf.Max(VRefAlpha, vLongAbs));
                }

                // α/κ will be stored after constraint solve (post-solve diagnostics)

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

                // Rolling resistance opposes rotation (small stabilizer)
                float rrSign = (Mathf.Abs(w.wheelOmega) > 0.5f) ? Mathf.Sign(w.wheelOmega)
                              : (Mathf.Abs(vLong) > MinSpeedForSign ? Mathf.Sign(vLong) : 0f);
                float rollingTq = RollingResistanceTorque * rrSign;

                // Net torque on wheel BEFORE ground reaction
                float netTq = driveTq - brakeApplied - rollingTq;

                // -----------------------------
                // 2) Tire forces from slip angle (α) and slip ratio (κ)
                // -----------------------------
                // Determine if this is a front or rear wheel (for front/rear tuning)
                // WheelIndex: front = 0,2; rear = 1,3
                bool isFront = Vehicle.Core.WheelIndex.IsFront(i);

                // Select front/rear parameters
                float muLong = mu * (isFront ? MuLongScaleFront : MuLongScaleRear);
                float muLat = mu * (isFront ? MuLatScaleFront : MuLatScaleRear);
                float cy = isFront ? CyFront : CyRear;

                // Maximum available forces (friction budget per axis)
                float fyMax = muLat * fz;

                // Fy from slip angle α: tanh gives smooth saturation
                float fyAlpha = -fyMax * (float)System.Math.Tanh(cy * alphaRad);

                // Small lateral damping (stabilizer, optional - currently 0)
                float fyDamp = -vLat * LatDamp;

                // Total raw lateral force
                float fy0 = fyAlpha + fyDamp;

                // Clamp Fy to its axis budget (combined budget will be applied to Fx below)
                float fy = Mathf.Clamp(fy0, -fyMax, fyMax);

                // -----------------------------
                // Longitudinal: constraint/impulse solve (stick), capped by friction budget
                // -----------------------------
                float invMassEff = ComputeInvEffectiveMassAlongAxis(ctx.rb, w.contactPoint, fwd);
                if (invMassEff <= 0f) continue;

                // Remaining longitudinal friction budget given lateral usage (friction ellipse)
                float fyNorm = fy / Mathf.Max(Epsilon, fyMax); // Fy/(muLat*Fz)
                float fxScale = Mathf.Sqrt(Mathf.Max(0f, 1f - fyNorm * fyNorm));
                float fxMax = (muLong * fz) * fxScale;

                float I = Mathf.Max(0.05f, ComputeWheelInertia(wheelSpec, radius));

                // Soft constraint target: reduce current slip velocity by ERP fraction
                float slipV = (w.wheelOmega * radius) - vLong; // m/s
                float b = -LongitudinalERP * slipV;

                // Predictive stick solve: find impulse J such that (ω_next*R - v_next) = b
                float denom = (radius * radius) / I + invMassEff;
                if (denom <= Epsilon) continue;

                float Jreq =
                    ((w.wheelOmega * radius) + (ctx.dt * netTq * radius) / I - vLong - b) / denom;

                // Static/kinetic hysteresis to prevent stick-slip chatter
                float JmaxKinetic = (fxMax * MuKineticMultiplier) * ctx.dt;
                float JmaxStatic = (fxMax * MuStaticMultiplier) * ctx.dt;

                float J;
                if (Mathf.Abs(Jreq) <= Mathf.Abs(JmaxStatic))
                {
                    // Sticking: fully satisfy the constraint
                    J = Jreq;
                }
                else
                {
                    // Sliding: saturate at kinetic friction limit
                    J = Mathf.Clamp(Jreq, -JmaxKinetic, JmaxKinetic);
                }

                float fxDesired = Jreq / Mathf.Max(Epsilon, ctx.dt);
                float fx = J / Mathf.Max(Epsilon, ctx.dt);

                Vector3 fLong = fwd * fx;
                Vector3 fLatV = right * fy;
                Vector3 force = fLong + fLatV;

                ctx.rb.AddForceAtPosition(force, w.contactPoint, ForceMode.Force);

                // -----------------------------
                // 8) Update wheelOmega from torques
                // Apply the same impulse-consistent wheel update used in the solve:
                // ω_next = ω + dt*(netTq/I) - (R/I)*J
                // -----------------------------
                w.wheelOmega += (ctx.dt * netTq) / I - (radius / I) * J;

                // Clamp wheelOmega to prevent runaway (safety measure)
                // Typical max wheel speed: ~1000 rad/s (for 100 km/h with 0.3m radius)
                float maxWheelOmega = 1000f; // rad/s
                w.wheelOmega = Mathf.Clamp(w.wheelOmega, -maxWheelOmega, maxWheelOmega);

                // Wheel omega damping only on engine braking (closed throttle + gear engaged)
                // This simulates engine braking effect, not artificial drag during acceleration
                bool gearEngaged = state.currentGear != 0 && state.clutchEngaged01 > 0.5f;
                bool closedThrottle = input.throttle < 0.05f;
                if (gearEngaged && closedThrottle)
                {
                    w.wheelOmega *= Mathf.Exp(-WheelOmegaDamping * ctx.dt);
                }

                // -----------------------------
                // 7) Debug outputs
                // -----------------------------
                // World-space force vectors for gizmos/visualization
                w.debugLongForce = fLong;
                w.debugLatForce = fLatV;

                // Velocity and slip info
                w.debugVLong = vLong;
                w.debugVLat = vLat;

                // Post-solve slip ratio κ (diagnostic result)
                float vLongNext = vLong + invMassEff * J; // predicted contact speed along wheel forward
                float wheelSurfaceNext = w.wheelOmega * radius;
                float denomK = Mathf.Max(VRefKappa, Mathf.Max(Mathf.Abs(vLongNext), Mathf.Abs(wheelSurfaceNext)));
                float kappa = (wheelSurfaceNext - vLongNext) / Mathf.Max(Epsilon, denomK);
                w.debugSlipRatio = kappa;

                // Post-solve slip angle α (diagnostic result, uses predicted vLongNext)
                float vLongNextAbs = Mathf.Abs(vLongNext);
                float alphaDiag = (vLongNextAbs < MinSpeedForSlip)
                    ? 0f
                    : Mathf.Atan2(vLat, Mathf.Max(VRefAlpha, vLongNextAbs));
                w.debugSlipAngleRad = alphaDiag;

                // Legacy fields (for backward compatibility with telemetry)
                w.debugFxDesired = fxDesired;  // desired Fx from stick constraint (before clamp)
                w.debugFyDesired = fy0;        // raw Fy from α (before axis clamp)

                // Friction budget (maximum available force)
                w.debugMuFz = fMax;

                // Utilization: how much of friction budget is used (normalized by axis)
                float uxFinal = fx / Mathf.Max(Epsilon, muLong * fz);
                float uyFinal = fy / Mathf.Max(Epsilon, muLat * fz);
                w.debugUtil = Mathf.Sqrt(uxFinal * uxFinal + uyFinal * uyFinal);

                // Legacy fields (duplicates for compatibility)
                w.debugDriveForce = fLong;
                w.debugLateralForce = fLatV;

                // v2 fields
                w.debugFxRaw = fxDesired;
                w.debugFyRaw = fy0;
                w.debugFxFinal = fx;
                w.debugFyFinal = fy;
            }
        }

        // --------------------------------------------------------------------
        // Effective mass (required for correct impulse solve at contact point)
        // --------------------------------------------------------------------

        private static float ComputeInvEffectiveMassAlongAxis(Rigidbody rb, Vector3 contactPoint, Vector3 axisUnit)
        {
            // k = 1/m + u · ( (I^{-1} (r×u)) × r )
            // where impulse is applied at contactPoint along u.
            float m = Mathf.Max(1e-3f, rb.mass);

            Vector3 u = axisUnit;
            float uLen = u.magnitude;
            if (uLen < 1e-6f) return 0f;
            u /= uLen;

            Vector3 r = contactPoint - rb.worldCenterOfMass;

            Matrix4x4 Iinv = ComputeWorldInertiaInv(rb);
            Vector3 rxu = Vector3.Cross(r, u);
            Vector3 Iinv_rxu = Iinv.MultiplyVector(rxu);
            Vector3 term = Vector3.Cross(Iinv_rxu, r);

            float k = (1f / m) + Vector3.Dot(u, term);
            return Mathf.Max(0f, k);
        }

        private static Matrix4x4 ComputeWorldInertiaInv(Rigidbody rb)
        {
            Vector3 I = rb.inertiaTensor;
            Vector3 invDiag = new Vector3(
                1f / Mathf.Max(1e-6f, I.x),
                1f / Mathf.Max(1e-6f, I.y),
                1f / Mathf.Max(1e-6f, I.z)
            );

            // inertia tensor orientation in world space
            Quaternion q = rb.rotation * rb.inertiaTensorRotation;
            Matrix4x4 R = Matrix4x4.Rotate(q);
            Matrix4x4 D = Matrix4x4.Scale(invDiag);
            Matrix4x4 Rt = R.transpose;

            // Iinv_world = R * D * R^T
            return R * D * Rt;
        }

        // --------------------------------------------------------------------
        // Wheel moment of inertia from physical parameters
        // --------------------------------------------------------------------

        /// <summary>
        /// Computes wheel moment of inertia using I = k * m * R².
        /// k is the inertia coefficient: 0.5 for solid disc, 1.0 for thin ring.
        /// Real wheels with tires are typically 0.7-0.9 (mass concentrated at rim/tire).
        /// </summary>
        /// <param name="spec">Wheel specification (may be null, uses defaults)</param>
        /// <param name="radius">Wheel radius in meters</param>
        /// <returns>Moment of inertia in kg·m²</returns>
        private static float ComputeWheelInertia(WheelSpec spec, float radius)
        {
            float mass = spec?.wheelMass ?? DefaultWheelMass;
            float k = spec?.wheelInertiaCoefficient ?? DefaultWheelInertiaCoefficient;
            float r = radius > 0.01f ? radius : 0.3f;

            // I = k * m * R²
            return k * mass * r * r;
        }
    }
}
