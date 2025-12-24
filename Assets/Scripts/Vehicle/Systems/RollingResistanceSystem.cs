using UnityEngine;
using Vehicle.Core;
using Vehicle.Specs;
using System.IO;
using System.Text;
using System.Globalization;

namespace Vehicle.Systems
{
    /// <summary>
    /// Applies rolling resistance per grounded wheel: F_rr = Crr * N, opposite wheel motion.
    /// </summary>
    public sealed class RollingResistanceSystem : IVehicleModule
    {
        private const float MinSpeed = 0.05f;

        public void Tick(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
        {
            var wheelSpec = ctx.wheelSpec ?? ctx.spec?.wheelSpec;
            if (wheelSpec == null || state.wheels == null || state.wheels.Length == 0 || ctx.rb == null)
                return;

            int count = state.wheels.Length;
            float crr = Mathf.Max(0f, wheelSpec.rollingResistance);
            int grounded = 0;
            int applied = 0;

            for (int i = 0; i < count; i++)
            {
                var w = state.wheels[i];
                if (!w.isGrounded || w.normalForce <= 0f)
                    continue;
                grounded++;

                // Velocity at contact point
                Vector3 v = ctx.rb.GetPointVelocity(w.contactPoint);
                // Remove normal component to get tangential motion along surface
                Vector3 vTangent = v - Vector3.Dot(v, w.surfaceNormal) * w.surfaceNormal;
                float speed = vTangent.magnitude;
                if (speed < MinSpeed)
                    continue;

                Vector3 dir = -vTangent / speed; // oppose motion
                float magnitude = crr * w.normalForce;
                Vector3 force = dir * magnitude;

                ctx.rb.AddForceAtPosition(force, w.contactPoint, ForceMode.Force);
                applied++;
            }

            // #region agent log
            AppendLog("RollingResistance:summary", "H3",
                ("grounded", grounded),
                ("applied", applied),
                ("crr", crr),
                ("wheelCount", count),
                ("runId", "pre-fix"),
                ("location", "RollingResistanceSystem.Tick"));
            // #endregion
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
                UnityEngine.Debug.LogWarning($"[RollingResistanceSystem] log failed: {ex.Message}");
            }
        }
    }
}

