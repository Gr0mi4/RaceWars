using UnityEngine;
using Vehicle.Core;
using System.IO;
using System.Text;
using System.Globalization;

namespace Vehicle.Systems
{
    /// <summary>
    /// Suspension system that applies forces based on suspension characteristics.
    /// This is a placeholder for future suspension system implementation.
    /// </summary>
    public sealed class SuspensionSystem : IVehicleModule
    {
        /// <summary>
        /// Initializes a new instance of the SuspensionSystem.
        /// </summary>
        public SuspensionSystem()
        {
        }

        /// <summary>
        /// Updates the suspension system.
        /// Currently empty - placeholder for future implementation.
        /// </summary>
        /// <param name="input">Current vehicle input.</param>
        /// <param name="state">Current vehicle state.</param>
        /// <param name="ctx">Vehicle context containing rigidbody, transform, and specifications.</param>
        public void Tick(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
        {
            var wheelSpec = ctx.wheelSpec ?? ctx.spec?.wheelSpec;
            var suspSpec = ctx.suspensionSpec ?? ctx.spec?.suspensionSpec;
            if (wheelSpec == null || wheelSpec.wheelOffsets == null || wheelSpec.wheelOffsets.Length == 0 || ctx.rb == null || suspSpec == null)
            {
                // #region agent log
                AppendLog("SuspensionSystem:skip", "H1",
                    ("reason", "missing spec or rb"),
                    ("hasWheelSpec", wheelSpec != null),
                    ("hasOffsets", wheelSpec?.wheelOffsets != null),
                    ("offsetsLen", wheelSpec?.wheelOffsets?.Length ?? 0),
                    ("hasSuspSpec", suspSpec != null),
                    ("runId", "pre-fix"),
                    ("location", "SuspensionSystem.Tick"));
                // #endregion
                return;
            }

            int count = wheelSpec.wheelOffsets.Length;
            if (state.wheels == null || state.wheels.Length != count)
            {
                state.wheels = new WheelRuntime[count];
            }

            float rest = Mathf.Max(0f, suspSpec.restLength);
            float maxComp = Mathf.Max(0f, suspSpec.maxCompression);
            float maxDroop = Mathf.Max(0f, suspSpec.maxDroop);
            float rayLength = rest + maxComp + maxDroop;

            for (int i = 0; i < count; i++)
            {
                Vector3 origin = ctx.tr.TransformPoint(wheelSpec.wheelOffsets[i]);
                Vector3 dir = -ctx.Up;

                bool grounded = false;
                Vector3 contactPoint = Vector3.zero;
                Vector3 normal = Vector3.up;
                float compression = 0f;
                float normalForce = 0f;

                if (Physics.Raycast(origin, dir, out RaycastHit hit, rayLength))
                {
                    float dist = hit.distance;
                    // If wheel is beyond droop range, treat as not grounded.
                    if (dist <= rest + maxDroop)
                    {
                        grounded = true;
                        contactPoint = hit.point;
                        normal = hit.normal;

                        compression = Mathf.Clamp(rest - dist, 0f, maxComp);

                        // Relative velocity along normal at contact.
                        float relVel = Vector3.Dot(ctx.rb.GetPointVelocity(hit.point), hit.normal);

                        float springForce = compression > 0f ? suspSpec.spring * compression : 0f;
                        float damperForce = 0f;
                        if (compression > 0f)
                        {
                            damperForce = suspSpec.damper * (-relVel);
                            damperForce = Mathf.Clamp(damperForce, -suspSpec.maxDamperForce, suspSpec.maxDamperForce);
                        }

                        normalForce = Mathf.Max(0f, springForce + damperForce);
                        normalForce = Mathf.Min(normalForce, suspSpec.maxForce);

                        if (normalForce > 0f)
                        {
                            Vector3 force = hit.normal * normalForce;
                            ctx.rb.AddForceAtPosition(force, hit.point, ForceMode.Force);
                        }

                        // #region agent log
                        AppendLog("SuspensionSystem:contact", "H2",
                            ("wheel", i),
                            ("compression", compression),
                            ("relVel", relVel),
                            ("springForce", springForce),
                            ("damperForce", damperForce),
                            ("normalForce", normalForce),
                            ("contactX", contactPoint.x),
                            ("contactY", contactPoint.y),
                            ("contactZ", contactPoint.z),
                            ("rest", rest),
                            ("maxComp", maxComp),
                            ("maxDroop", maxDroop),
                            ("dist", dist),
                            ("maxDamper", suspSpec.maxDamperForce),
                            ("maxForce", suspSpec.maxForce),
                            ("runId", "pre-fix"),
                            ("location", "SuspensionSystem.Tick"));
                        // #endregion
                    }
                }

                state.wheels[i].isGrounded = grounded;
                state.wheels[i].contactPoint = contactPoint;
                state.wheels[i].surfaceNormal = normal;
                state.wheels[i].normalForce = normalForce;
                state.wheels[i].compression = compression;

                // Approximate angular velocity from vehicle forward speed if grounded.
                float forwardSpeed = Vector3.Dot(ctx.rb.linearVelocity, ctx.Forward);
                state.wheels[i].angularVelocity = wheelSpec.wheelRadius > 0f ? forwardSpeed / wheelSpec.wheelRadius : 0f;
            }
        }

        // Minimal NDJSON logger
        private void AppendLog(string message, string hypothesisId, params (string key, object value)[] fields)
        {
            try
            {
                const string LogPath = "/Users/ivanhromau/Personal/RaceWars/.cursor/debug.log";
                var dir = Path.GetDirectoryName(LogPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                var sb = new StringBuilder(256);
                sb.Append('{');
                sb.Append("\"timestamp\":").Append(System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()).Append(',');
                sb.Append("\"sessionId\":\"debug-session\",");
                sb.Append("\"runId\":\"pre-fix\",");
                sb.Append("\"hypothesisId\":\"").Append(hypothesisId).Append("\",");
                sb.Append("\"message\":\"").Append(message).Append("\",");
                sb.Append("\"data\":{");
                for (int i = 0; i < fields.Length; i++)
                {
                    var (key, val) = fields[i];
                    sb.Append('\"').Append(key).Append("\":");
                    switch (val)
                    {
                        case bool b:
                            sb.Append(b ? "true" : "false");
                            break;
                        case int iv:
                            sb.Append(iv);
                            break;
                        case float fv:
                            sb.Append(fv.ToString(CultureInfo.InvariantCulture));
                            break;
                        case double dv:
                            sb.Append(dv.ToString(CultureInfo.InvariantCulture));
                            break;
                        case string sv:
                            sb.Append('\"').Append(sv).Append('\"');
                            break;
                        default:
                            sb.Append('\"').Append(val?.ToString() ?? "null").Append('\"');
                            break;
                    }
                    if (i < fields.Length - 1) sb.Append(',');
                }
                sb.Append("}}");
                File.AppendAllText(LogPath, sb.ToString() + "\n");
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[SuspensionSystem] log failed: {ex.Message}");
            }
        }
    }
}

