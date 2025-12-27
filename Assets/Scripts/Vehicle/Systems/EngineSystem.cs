// EngineSystem.cs
using UnityEngine;
using Vehicle.Specs;

namespace Vehicle.Systems
{
    /// <summary>
    /// EngineSystem:
    /// - Converts between wheel/engine angular speeds and RPM
    /// - Reads torque/power curves from EngineSpec
    /// - Applies a HARD-CUT rev limiter based on *mechanical RPM* (from wheels)
    ///
    /// IMPORTANT:
    /// - In the new architecture, the rev limiter belongs to the ENGINE, not the gearbox.
    /// - DrivetrainSystem will compute mechanical RPM from wheels and ask EngineSystem for
    ///   torque with limiter applied.
    /// </summary>
    public sealed class EngineSystem
    {
        private const float HpToWatts = 745.7f;

        // ---------- Kinematics ----------

        public float CalculateEngineRPMFromWheel(float wheelAngularVelocityRadS, float gearRatio, float finalDriveRatio)
        {
            if (finalDriveRatio <= 0f || Mathf.Abs(gearRatio) < 0.0001f)
                return 0f;

            float engineOmega = Mathf.Abs(wheelAngularVelocityRadS) * Mathf.Abs(gearRatio) * finalDriveRatio; // rad/s
            float rpm = engineOmega * (60f / (2f * Mathf.PI));
            return Mathf.Max(0f, rpm);
        }

        public float CalculateWheelAngularVelocityFromEngine(float engineRPM, float gearRatio, float finalDriveRatio)
        {
            if (finalDriveRatio <= 0f || Mathf.Abs(gearRatio) < 0.0001f)
                return 0f;

            float engineOmega = engineRPM * (2f * Mathf.PI / 60f); // rad/s
            float wheelOmega = engineOmega / (Mathf.Abs(gearRatio) * finalDriveRatio);
            return Mathf.Max(0f, wheelOmega);
        }

        public float CalculateSpeedFromWheel(float wheelAngularVelocityRadS, float wheelRadius)
        {
            if (wheelRadius <= 0f) return 0f;
            return wheelAngularVelocityRadS * wheelRadius;
        }

        public float CalculateWheelAngularVelocityFromSpeed(float speedMS, float wheelRadius)
        {
            if (wheelRadius <= 0f) return 0f;
            return speedMS / wheelRadius;
        }

        // ---------- Curves ----------

        public float GetPowerWatts(float rpm, float throttle01, EngineSpec spec)
        {
            if (spec == null || throttle01 <= 0f) return 0f;

            float t = NormalizeRPM01(rpm, spec.idleRPM, spec.maxRPM);
            float mult = spec.powerCurve.Evaluate(t);
            float hp = spec.maxPower * mult * throttle01;
            return hp * HpToWatts;
        }

        public float GetTorqueNm(float rpm, float throttle01, EngineSpec spec, bool gearEngaged = true)
        {
            if (spec == null || !gearEngaged)
                return 0f;

            // "idle creep" behavior if you want it (your old logic)
            if (throttle01 <= 0f)
            {
                if (Mathf.Abs(rpm - spec.idleRPM) < 50f)
                    return spec.idleTorque;

                return 0f;
            }

            float t = NormalizeRPM01(rpm, spec.idleRPM, spec.maxRPM);
            float mult = spec.torqueCurve.Evaluate(t);
            return spec.maxTorque * mult * throttle01;
        }

        // ---------- Rev limiter (HARD CUT) ----------

        /// <summary>
        /// Returns engine torque with HARD-CUT rev limiter applied.
        ///
        /// HARD-CUT meaning:
        /// - At/above maxRPM: torque becomes 0 (like fuel/spark cut).
        /// - Below maxRPM: full curve torque.
        ///
        /// Input RPM must be *mechanical RPM* derived from wheelOmega (through gear ratios),
        /// not a smoothed "display" RPM.
        /// </summary>
        public float GetTorqueHardCutLimitedNm(float mechanicalRpm, float throttle01, EngineSpec spec, bool gearEngaged = true)
        {
            if (spec == null || !gearEngaged)
                return 0f;

            // If the driver isn't applying throttle, keep your idle behavior:
            // (and we don't need limiter for that)
            if (throttle01 <= 0f)
                return GetTorqueNm(mechanicalRpm, throttle01, spec, gearEngaged);

            // HARD CUT: above redline -> zero torque
            if (mechanicalRpm >= spec.maxRPM)
                return 0f;

            return GetTorqueNm(mechanicalRpm, throttle01, spec, gearEngaged);
        }

        private float NormalizeRPM01(float rpm, float idle, float max)
        {
            if (max <= idle) return 0f;
            float t = (rpm - idle) / (max - idle);
            return Mathf.Clamp01(t);
        }
    }
}
