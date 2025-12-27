using UnityEngine;
using Vehicle.Core;

namespace Vehicle.Systems
{
    /// <summary>
    /// Raycast-based suspension system.
    /// Calculates vertical suspension forces (spring + damper),
    /// and applies dynamic weight transfer (longitudinal + lateral).
    /// </summary>
    public sealed class SuspensionSystem : IVehicleModule
    {

        public void Tick(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
        {
            // ------------------------------------------------------------
            // 0. Basic validation
            // ------------------------------------------------------------
            if (ctx.rb == null || ctx.wheelSpec == null || ctx.suspensionSpec == null)
                return;

            var wheelSpec = ctx.wheelSpec;
            var suspSpec = ctx.suspensionSpec;

            int wheelCount = wheelSpec.wheelOffsets.Length;
            if (wheelCount == 0)
                return;

            if (state.wheels == null || state.wheels.Length != wheelCount)
                state.wheels = new WheelRuntime[wheelCount];

            // ------------------------------------------------------------
            // 1. Pre-calc constants
            // ------------------------------------------------------------
            float mass = ctx.rb.mass;
            float g = 9.81f;
            float dt = Time.fixedDeltaTime;

            // Static load per wheel (baseline)
            float staticFzPerWheel = (mass * g) / wheelCount;

            // Suspension geometry
            float restLength = Mathf.Max(0.01f, suspSpec.restLength);
            float maxCompression = Mathf.Max(0f, suspSpec.maxCompression);
            float maxDroop = Mathf.Max(0f, suspSpec.maxDroop);
            float rayLength = restLength + maxCompression + maxDroop;

            // Spring & damper (auto-tuned)
            float sagRatio = 0.35f;
            float sag = Mathf.Max(0.01f, restLength * sagRatio);
            float springK = staticFzPerWheel / sag;

            float cCrit = 2f * Mathf.Sqrt(springK * (mass / wheelCount));
            float damperC = 0.35f * cCrit;

            // ------------------------------------------------------------
            // 4. Per-wheel suspension
            // ------------------------------------------------------------
            for (int i = 0; i < wheelCount; i++)
            {
                Vector3 localOffset = wheelSpec.wheelOffsets[i];
                Vector3 rayOrigin = ctx.tr.TransformPoint(localOffset);
                Vector3 rayDir = -ctx.Up;

                bool grounded = false;
                Vector3 contactPoint = Vector3.zero;
                Vector3 surfaceNormal = ctx.Up;

                float compression = 0f;
                float normalForce = 0f;

                // --------------------------------------------------------
                // 4.1 Raycast (wheel-ground contact)
                // --------------------------------------------------------
                if (Physics.Raycast(rayOrigin, rayDir, out RaycastHit hit, rayLength, ~0, QueryTriggerInteraction.Ignore))
                {
                    float distance = hit.distance - wheelSpec.wheelRadius;
                    distance = Mathf.Max(0f, distance);

                    if (distance <= restLength + maxDroop)
                    {
                        grounded = true;
                        contactPoint = hit.point;
                        surfaceNormal = hit.normal;

                        // ------------------------------------------------
                        // 4.2 Suspension compression (smoothed)
                        // ------------------------------------------------
                        float targetCompression = Mathf.Clamp(restLength - distance, 0f, maxCompression);

                        var wr = state.wheels[i];
                        if (!wr.isGrounded)
                            wr.compressionFiltered = 0f;

                        float tau = Mathf.Max(0.001f, suspSpec.compressionSmoothingTime);
                        float alpha = 1f - Mathf.Exp(-dt / tau);

                        wr.compressionFiltered = Mathf.Lerp(
                            wr.compressionFiltered,
                            targetCompression,
                            alpha
                        );

                        compression = wr.compressionFiltered;
                        state.wheels[i] = wr;

                        // ------------------------------------------------
                        // 4.3 Spring + damper force
                        // ------------------------------------------------
                        Vector3 axis = ctx.Up;
                        float relVel = Vector3.Dot(ctx.rb.GetPointVelocity(hit.point), axis);

                        float springForce = compression > 0f ? springK * compression : 0f;
                        float damperForce = -damperC * relVel;
                        damperForce = Mathf.Clamp(
                            damperForce,
                            -suspSpec.maxDamperForce,
                            suspSpec.maxDamperForce
                        );

                        float baseFz = springForce + damperForce;

                        // ------------------------------------------------
                        // 4.5 Apply force
                        // ------------------------------------------------
                        float totalFz = Mathf.Clamp(baseFz, 0f, suspSpec.maxForce);

                        if (totalFz > 0f)
                        {
                            ctx.rb.AddForceAtPosition(axis * totalFz, hit.point, ForceMode.Force);
                        }

                        normalForce = totalFz;
                    }
                }

                // --------------------------------------------------------
                // 4.6 Write wheel state
                // --------------------------------------------------------
                state.wheels[i].isGrounded = grounded;
                state.wheels[i].contactPoint = contactPoint;
                state.wheels[i].surfaceNormal = surfaceNormal;
                state.wheels[i].normalForce = normalForce;
                state.wheels[i].compression = compression;

                float forwardSpeed = Vector3.Dot(ctx.rb.linearVelocity, ctx.Forward);
                state.wheels[i].angularVelocity =
                    wheelSpec.wheelRadius > 0f
                        ? forwardSpeed / wheelSpec.wheelRadius
                        : 0f;
            }
        }
    }
}
