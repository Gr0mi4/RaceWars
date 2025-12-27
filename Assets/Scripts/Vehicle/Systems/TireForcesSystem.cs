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
        private const float DefaultWheelInertia = 1.2f; // kg*m^2
        private const float WheelOmegaDamping = 0.5f;   // 1/s
        private const float MinSpeedForSign = 0.1f;     // m/s

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

        // Tire stiffness coefficients (how "responsive" the tire is to slip)
        /// <summary>
        /// Longitudinal stiffness coefficient for front wheels (dimensionless tuning factor).
        /// Higher = more responsive to slip ratio κ, steeper Fx(κ) curve.
        /// Typical range: 6..20. Start with 10.
        /// </summary>
        private const float CxFront = 10f;

        /// <summary>
        /// Longitudinal stiffness coefficient for rear wheels.
        /// </summary>
        private const float CxRear = 10f;

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
                // Slip angle (α) and slip ratio (κ) calculation
                // -----------------------------
                float vLongAbs = Mathf.Abs(vLong);
                float alphaRad;
                float kappa;

                // Dead zone: ignore lateral slip (α) at very low speeds to prevent force accumulation
                // BUT allow longitudinal slip (κ) for starting/acceleration
                // This prevents the car from "drifting" when stationary, but allows it to start moving
                float wheelSurfaceSpeed = w.wheelOmega * radius; // m/s (signed)

                if (vLongAbs < MinSpeedForSlip)
                {
                    // At standstill or very low speed, ignore lateral slip (α)
                    alphaRad = 0f;
                    // BUT still calculate κ for starting/acceleration
                    // When vLong ≈ 0 but wheelOmega > 0, we need κ to generate Fx for starting
                    kappa = (wheelSurfaceSpeed - vLong) / Mathf.Max(VRefKappa, Mathf.Max(0.1f, vLongAbs));
                }
                else
                {
                    // Normal slip calculations
                    // Slip angle: angle between wheel forward direction and velocity direction
                    // Positive = wheel pointing left of velocity (right-hand turn)
                    alphaRad = Mathf.Atan2(vLat, Mathf.Max(VRefAlpha, vLongAbs));

                    // Slip ratio: normalized difference between wheel surface speed and ground speed
                    // Positive = wheel spinning faster (acceleration slip)
                    // Negative = wheel slower than ground (braking slip/blocking)
                    kappa = (wheelSurfaceSpeed - vLong) / Mathf.Max(VRefKappa, vLongAbs);
                }

                // Store in debug for telemetry/visualization
                w.debugSlipAngleRad = alphaRad;
                w.debugSlipRatio = kappa;

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
                float cx = isFront ? CxFront : CxRear;
                float cy = isFront ? CyFront : CyRear;

                // Maximum available forces (friction budget per axis)
                float fxMax = muLong * fz;
                float fyMax = muLat * fz;

                // Raw forces from slip (before combined slip and build-up)
                // Fx from slip ratio κ: tanh gives smooth saturation
                float fx0 = fxMax * (float)System.Math.Tanh(cx * kappa);

                // Fy from slip angle α: tanh gives smooth saturation
                float fyAlpha = -fyMax * (float)System.Math.Tanh(cy * alphaRad);

                // Small lateral damping (stabilizer, optional - currently 0)
                float fyDamp = -vLat * LatDamp;

                // Total raw lateral force
                float fy0 = fyAlpha + fyDamp;

                // Store raw forces in debug
                w.debugFxRaw = fx0;
                w.debugFyRaw = fy0;

                // -----------------------------
                // 4) Combined slip (friction ellipse/circle)
                // -----------------------------
                // Normalize forces by their respective maximums
                // This allows different μLong and μLat, and proper combined slip behavior
                float ux = fx0 / Mathf.Max(Epsilon, fxMax);
                float uy = fy0 / Mathf.Max(Epsilon, fyMax);

                // Combined utilization: sqrt(ux² + uy²)
                // If > 1.0, forces exceed friction budget and need to be scaled down
                float u = Mathf.Sqrt(ux * ux + uy * uy);

                // Apply combined slip constraint (friction ellipse)
                float fx = fx0;
                float fy = fy0;
                if (u > 1f)
                {
                    // Scale down proportionally to stay within friction budget
                    fx /= u;
                    fy /= u;
                }

                // -----------------------------
                // 6) Apply forces
                // -----------------------------
                Vector3 fLong = fwd * fx;
                Vector3 fLatV = right * fy;
                Vector3 force = fLong + fLatV;

                ctx.rb.AddForceAtPosition(force, w.contactPoint, ForceMode.Force);

                // -----------------------------
                // 8) Update wheelOmega from torques
                // Ground reaction torque = Fx * R (opposes net torque)
                // Uses final Fx (after combined slip + build-up) for correct physics
                // -----------------------------
                float groundTq = fx * radius;
                float wheelNetTq = netTq - groundTq;

                float I = Mathf.Max(0.05f, DefaultWheelInertia);
                float domega = wheelNetTq / I;
                w.wheelOmega += domega * ctx.dt;

                // Clamp wheelOmega to prevent runaway (safety measure)
                // Typical max wheel speed: ~1000 rad/s (for 100 km/h with 0.3m radius)
                float maxWheelOmega = 1000f; // rad/s
                w.wheelOmega = Mathf.Clamp(w.wheelOmega, -maxWheelOmega, maxWheelOmega);

                // mild damping prevents runaway oscillations
                w.wheelOmega *= Mathf.Exp(-WheelOmegaDamping * ctx.dt);

                // -----------------------------
                // 7) Debug outputs
                // -----------------------------
                // World-space force vectors for gizmos/visualization
                w.debugLongForce = fLong;
                w.debugLatForce = fLatV;

                // Velocity and slip info
                w.debugVLong = vLong;
                w.debugVLat = vLat;


                // Legacy fields (for backward compatibility with telemetry)
                // These now represent "raw" forces before combined slip and build-up
                w.debugFxDesired = fx0;  // Raw Fx from κ
                w.debugFyDesired = fy0;  // Raw Fy from α

                // Friction budget (maximum available force)
                w.debugMuFz = fMax;

                // Utilization: how much of friction budget is used (normalized by axis)
                // Uses normalized components: sqrt((Fx/FxMax)² + (Fy/FyMax)²)
                // Note: fxMax and fyMax already declared above (lines 265-266)
                float uxFinal = fx / Mathf.Max(Epsilon, fxMax);
                float uyFinal = fy / Mathf.Max(Epsilon, fyMax);
                w.debugUtil = Mathf.Sqrt(uxFinal * uxFinal + uyFinal * uyFinal);

                // Legacy fields (duplicates for compatibility)
                w.debugDriveForce = fLong;
                w.debugLateralForce = fLatV;
            }
        }
    }
}
