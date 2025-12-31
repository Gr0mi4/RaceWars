using UnityEngine;
using Vehicle.Specs;

namespace Vehicle.Systems
{
    /// <summary>
    /// EngineSystem:
    /// - kinematics: wheelOmega <-> engineRPM
    /// - torque curve
    /// - HARD CUT rev limiter (positive torque = 0 above redline)
    /// - engine drag torque (engine braking)
    ///
    /// "Shaft torque" = what engine actually sends into drivetrain (can be negative on overrun).
    /// </summary>
    public sealed class EngineSystem
    {
        private const float HpToWatts = 745.699872f;

        // ---------------- KINEMATICS ----------------

        public float CalculateEngineRPMFromWheel(float wheelOmegaRadS, float gearRatio, float finalDriveRatio)
        {
            if (finalDriveRatio <= 0f || Mathf.Abs(gearRatio) < 1e-6f)
                return 0f;

            float engineOmega = Mathf.Abs(wheelOmegaRadS) * Mathf.Abs(gearRatio) * finalDriveRatio; // rad/s
            return engineOmega * (60f / (2f * Mathf.PI));
        }

        public float CalculateWheelOmegaFromEngineRPM(float engineRpm, float gearRatio, float finalDriveRatio)
        {
            if (finalDriveRatio <= 0f || Mathf.Abs(gearRatio) < 1e-6f)
                return 0f;

            float engineOmega = engineRpm * (2f * Mathf.PI / 60f);
            return engineOmega / (Mathf.Abs(gearRatio) * finalDriveRatio);
        }

        // ---------------- DRIVE TORQUE (positive) ----------------

        public float GetDriveTorqueNm(float rpm, float throttle01, EngineSpec spec)
        {
            if (spec == null)
                return 0f;

            throttle01 = Mathf.Clamp01(throttle01);

            // optional idle creep behavior (kept)
            if (throttle01 <= 0f)
            {
                if (Mathf.Abs(rpm - spec.idleRPM) < 50f)
                    return spec.idleTorque;
                return 0f;
            }

            float torqueNm = GetMaxDriveTorqueNmAtRpm(rpm, spec);
            return torqueNm * throttle01;
        }

        /// <summary>
        /// Returns positive drive torque capability at this RPM (Nm) BEFORE throttle.
        /// This reads a single physically-consistent source (either torqueCurve or powerCurve->P/ω),
        /// depending on EngineSpec.curveAuthority.
        /// </summary>
        private float GetMaxDriveTorqueNmAtRpm(float rpm, EngineSpec spec)
        {
            rpm = Mathf.Clamp(rpm, 0f, spec.maxRPM);

            float t = NormalizeRPM01(rpm, spec.idleRPM, spec.maxRPM);

            if (spec.curveAuthority == EngineSpec.CurveAuthority.PowerIsAuthoritative)
            {
                float omega = Mathf.Max(1e-3f, rpm * (2f * Mathf.PI / 60f));
                float powerW = Mathf.Max(0f, spec.maxPower * HpToWatts * spec.powerCurve.Evaluate(t));
                float torqueFromPower = powerW / omega;
                return Mathf.Max(0f, torqueFromPower);
            }
            else
            {
                float mult = spec.torqueCurve.Evaluate(t);
                return Mathf.Max(0f, spec.maxTorque * mult);
            }
        }

        /// <summary>
        /// HARD CUT: above redline -> no positive drive torque.
        /// </summary>
        public float GetDriveTorqueHardCutNm(float mechanicalRpm, float throttle01, EngineSpec spec)
        {
            if (spec == null)
                return 0f;

            throttle01 = Mathf.Clamp01(throttle01);

            if (throttle01 <= 0f)
                return GetDriveTorqueNm(mechanicalRpm, throttle01, spec);

            if (mechanicalRpm >= spec.maxRPM)
                return 0f;

            return GetDriveTorqueNm(mechanicalRpm, throttle01, spec);
        }

        // ---------------- ENGINE DRAG (always resists rotation) ----------------

        /// <summary>
        /// Engine drag magnitude (Nm), positive величина.
        /// В drivetrain её нужно применять ПРОТИВ вращения (против sign(engineOmega)).
        /// </summary>
        public float GetEngineDragTorqueNm(float rpm, float throttle01, EngineSpec spec)
        {
            if (spec == null) return 0f;

            throttle01 = Mathf.Clamp01(throttle01);

            // "good game defaults" - позже вынесешь в EngineSpec
            const float baseFrictionNm = 12f;         // постоянное трение
            const float viscousAtRedlineNm = 35f;     // растёт с rpm
            const float closedThrottleExtraNm = 55f;  // доп. торможение при закрытом дросселе

            float redline = Mathf.Max(spec.idleRPM + 1f, spec.maxRPM);
            float t = Mathf.Clamp01(rpm / redline);

            float friction = baseFrictionNm;
            float viscous = viscousAtRedlineNm * (t * t); // квадратично приятно

            float closed = Mathf.Lerp(closedThrottleExtraNm, 0f, throttle01); // throttle=0 => максимум

            float drag = friction + viscous + closed;
            return Mathf.Clamp(drag, 0f, 250f);
        }

        // ---------------- SHAFT TORQUE (drive - drag) ----------------

        /// <summary>
        /// Returns shaft torque on engine side (Nm).
        /// Positive => accelerates engine / pulls car forward through gears.
        /// Negative => engine braking (resists being back-driven by wheels).
        ///
        /// IMPORTANT: for in-gear physics, pass mechanicalRpm (from wheels).
        /// </summary>
        public float GetShaftTorqueNm(float mechanicalRpm, float throttle01, float engineOmegaSign, EngineSpec spec)
        {
            if (spec == null) return 0f;

            float drive = GetDriveTorqueHardCutNm(mechanicalRpm, throttle01, spec);

            float dragMag = GetEngineDragTorqueNm(mechanicalRpm, throttle01, spec);

            // drag opposes rotation:
            // if engine is being spun forward (omegaSign>0) => subtract drag
            // if reverse => add drag
            float dragSigned = -Mathf.Sign(engineOmegaSign) * dragMag;

            // If sign is zero (standing), assume forward
            if (Mathf.Abs(engineOmegaSign) < 1e-4f)
                dragSigned = -dragMag;

            // Shaft torque = drive + signed drag (drag is typically negative when rotating forward).
            // return drive + dragSigned;
            // While active testing, disabled engine drag due to realism issuses;
            return drive;
        }

        private float NormalizeRPM01(float rpm, float idle, float max)
        {
            if (max <= idle) return 0f;
            return Mathf.Clamp01((rpm - idle) / (max - idle));
        }
    }
}
