using UnityEngine;

namespace Vehicle.Specs
{
    /// <summary>
    /// ScriptableObject containing engine configuration parameters.
    /// Defines engine power and torque curves, RPM limits, and other engine characteristics.
    /// Create instances of this asset to define different engine types for vehicles.
    /// </summary>
    [CreateAssetMenu(menuName = "Vehicle/Engine Spec", fileName = "EngineSpec")]
    public sealed class EngineSpec : ScriptableObject
    {
        public enum CurveAuthority
        {
            TorqueIsAuthoritative,
            PowerIsAuthoritative
        }

        [Header("Engine Power")]
        /// <summary>
        /// Maximum engine power in horsepower (HP). This is the peak power output of the engine.
        /// Typical values: 100-500 HP for passenger cars, 500-1000+ HP for sports/supercars.
        /// </summary>
        [Min(1f)]
        [Tooltip("Maximum engine power in horsepower (HP)")]
        public float maxPower = 110f;

        [Header("Engine Torque")]
        /// <summary>
        /// Maximum engine torque in Newton-meters (Nm). This is the peak torque output of the engine.
        /// Typical values: 150-500 Nm for passenger cars, 500-1000+ Nm for sports/supercars.
        /// </summary>
        [Min(1f)]
        [Tooltip("Maximum engine torque in Newton-meters (Nm)")]
        public float maxTorque = 155f;

        [Header("RPM Limits")]
        /// <summary>
        /// Maximum RPM (revolutions per minute) the engine can reach. Redline limit.
        /// Typical values: 6000-8000 RPM for most cars, 8000-12000+ RPM for high-performance engines.
        /// </summary>
        [Range(1000f, 20000f)]
        [Tooltip("Maximum RPM (redline). Typical: 6000-8000 RPM")]
        public float maxRPM = 6500f;

        /// <summary>
        /// Idle RPM (revolutions per minute) when the engine is idling.
        /// Typical values: 600-1000 RPM.
        /// </summary>
        [Range(400f, 2000f)]
        [Tooltip("Idle RPM. Typical: 600-1000 RPM")]
        public float idleRPM = 800f;

        /// <summary>
        /// Idle torque in Newton-meters (Nm) produced at idle RPM when throttle is not applied.
        /// This is the minimum torque the engine produces at idle, allowing the vehicle to creep forward
        /// when a gear is engaged (clutch engaged). In real cars, this causes the vehicle to move slowly
        /// at idle RPM with engaged gear, unless the brake is applied.
        /// Typical values: 10-30% of maxTorque (e.g., 15-50 Nm for a 155 Nm engine).
        /// </summary>
        [Range(0f, 200f)]
        [Tooltip("Idle torque (Nm) at idle RPM. Allows vehicle to creep when gear is engaged. Typical: 10-30% of maxTorque")]
        public float idleTorque = 25f;

        /// <summary>
        /// Engine RPM inertia factor. Controls how quickly RPM changes (acceleration/deceleration rate).
        /// Higher values = faster RPM changes (more responsive), lower values = slower RPM changes (more inertia).
        /// This simulates the physical inertia of the engine - RPM cannot change instantly.
        /// Typical values: 5-20 (5 = very slow/smooth, 20 = fast/responsive).
        /// </summary>
        [Range(1f, 50f)]
        [Tooltip("RPM inertia factor. Higher = faster RPM changes. Typical: 5-20")]
        public float rpmInertia = 10f;

        [Header("Power and Torque Curves")]
        [Tooltip("Which curve is treated as the source of truth. The other curve can be derived for consistency (optional). Runtime should use a single consistent torque curve (no blending of two independent sources).")]
        public CurveAuthority curveAuthority = CurveAuthority.TorqueIsAuthoritative;

        [Tooltip("If enabled, automatically derives the secondary curve from the authoritative one in OnValidate so torqueCurve and powerCurve remain physically consistent (P=τω).")]
        public bool autoDeriveSecondaryCurve = true;

        [Tooltip("If enabled, updates maxPower/maxTorque to match the peak of the authoritative curve (recommended when deriving the secondary curve).")]
        public bool autoUpdatePeaksFromAuthoritative = true;

        [Range(8, 256)]
        [Tooltip("How many samples to use when deriving curves in OnValidate. Higher = more accurate, but noisier curves if too high.")]
        public int deriveSampleCount = 64;

        /// <summary>
        /// Power curve normalized by RPM (0-1). X-axis: normalized RPM (0 = idleRPM, 1 = maxRPM).
        /// Y-axis: power multiplier (0-1, where 1 = maxPower).
        /// This curve defines how engine power varies with RPM.
        /// </summary>
        [Tooltip("Power curve: X = normalized RPM (0=idle, 1=max), Y = power multiplier (0-1)")]
        public AnimationCurve powerCurve = AnimationCurve.Linear(0f, 0.3f, 1f, 1f);

        /// <summary>
        /// Torque curve normalized by RPM (0-1). X-axis: normalized RPM (0 = idleRPM, 1 = maxRPM).
        /// Y-axis: torque multiplier (0-1, where 1 = maxTorque).
        /// This curve defines how engine torque varies with RPM.
        /// </summary>
        [Tooltip("Torque curve: X = normalized RPM (0=idle, 1=max), Y = torque multiplier (0-1)")]
        public AnimationCurve torqueCurve = AnimationCurve.Linear(0f, 0.8f, 1f, 0.6f);

        /// <summary>
        /// Validates the engine specification parameters.
        /// Called automatically by Unity when values change in the Inspector.
        /// </summary>
        private void OnValidate()
        {
            // Ensure maxRPM is greater than idleRPM
            if (maxRPM <= idleRPM)
            {
                maxRPM = idleRPM + 100f;
            }

            // Ensure curves are valid
            if (powerCurve == null)
            {
                powerCurve = AnimationCurve.Linear(0f, 0.3f, 1f, 1f);
            }

            if (torqueCurve == null)
            {
                torqueCurve = AnimationCurve.Linear(0f, 0.8f, 1f, 0.6f);
            }

            maxPower = Mathf.Max(1f, maxPower);
            maxTorque = Mathf.Max(1f, maxTorque);

            // Clamp curve values to reasonable ranges
            if (powerCurve.length > 0)
            {
                for (int i = 0; i < powerCurve.length; i++)
                {
                    var key = powerCurve[i];
                    key.value = Mathf.Clamp01(key.value);
                    powerCurve.MoveKey(i, key);
                }
            }

            if (torqueCurve.length > 0)
            {
                for (int i = 0; i < torqueCurve.length; i++)
                {
                    var key = torqueCurve[i];
                    key.value = Mathf.Clamp01(key.value);
                    torqueCurve.MoveKey(i, key);
                }
            }

            if (autoDeriveSecondaryCurve)
            {
                DeriveSecondaryCurve();
            }
        }

        // --------------------------------------------------------------------
        // Evaluation helpers (used by runtime systems)
        // --------------------------------------------------------------------

        public float NormalizeRpm01(float rpm)
        {
            float denom = Mathf.Max(1f, maxRPM - idleRPM);
            return Mathf.Clamp01((rpm - idleRPM) / denom);
        }

        public float RpmFromNormalized01(float t)
        {
            t = Mathf.Clamp01(t);
            return Mathf.Lerp(idleRPM, maxRPM, t);
        }

        public float OmegaRadSFromRpm(float rpm)
        {
            return rpm * (2f * Mathf.PI / 60f);
        }

        public float MaxPowerWatts => maxPower * 745.699872f;

        // --------------------------------------------------------------------
        // Curve derivation (Editor-time consistency enforcement)
        // --------------------------------------------------------------------

        private void DeriveSecondaryCurve()
        {
            int n = Mathf.Clamp(deriveSampleCount, 8, 256);

            // Sample authoritative physical curve (either τ(rpm) or P(rpm))
            float peakTorque = 0f;
            float peakPowerW = 0f;

            // First pass: find peaks in physical units
            for (int i = 0; i < n; i++)
            {
                float t = (n == 1) ? 0f : (float)i / (n - 1);
                float rpm = RpmFromNormalized01(t);
                float omega = Mathf.Max(1e-3f, OmegaRadSFromRpm(rpm));

                float torqueNm;
                float powerW;

                if (curveAuthority == CurveAuthority.TorqueIsAuthoritative)
                {
                    float mult = torqueCurve.Evaluate(t);
                    torqueNm = Mathf.Max(0f, maxTorque * mult);
                    powerW = torqueNm * omega;
                }
                else
                {
                    float mult = powerCurve.Evaluate(t);
                    powerW = Mathf.Max(0f, MaxPowerWatts * mult);
                    torqueNm = powerW / omega;
                }

                peakTorque = Mathf.Max(peakTorque, torqueNm);
                peakPowerW = Mathf.Max(peakPowerW, powerW);
            }

            // Avoid degenerate peaks
            peakTorque = Mathf.Max(1e-3f, peakTorque);
            peakPowerW = Mathf.Max(1e-3f, peakPowerW);

            // Optionally update scalar peaks to match authoritative curve
            if (autoUpdatePeaksFromAuthoritative)
            {
                if (curveAuthority == CurveAuthority.TorqueIsAuthoritative)
                {
                    maxPower = peakPowerW / 745.699872f;
                }
                else
                {
                    maxTorque = peakTorque;
                }
            }

            // Second pass: build normalized secondary curve
            var keys = new Keyframe[n];
            for (int i = 0; i < n; i++)
            {
                float t = (n == 1) ? 0f : (float)i / (n - 1);
                float rpm = RpmFromNormalized01(t);
                float omega = Mathf.Max(1e-3f, OmegaRadSFromRpm(rpm));

                float torqueNm;
                float powerW;

                if (curveAuthority == CurveAuthority.TorqueIsAuthoritative)
                {
                    float torqueMult = torqueCurve.Evaluate(t);
                    torqueNm = Mathf.Max(0f, maxTorque * torqueMult);
                    powerW = torqueNm * omega;

                    float pNorm = Mathf.Clamp01(powerW / Mathf.Max(1e-3f, MaxPowerWatts));
                    keys[i] = new Keyframe(t, pNorm);
                }
                else
                {
                    float powerMult = powerCurve.Evaluate(t);
                    powerW = Mathf.Max(0f, MaxPowerWatts * powerMult);
                    torqueNm = powerW / omega;

                    float tNorm = Mathf.Clamp01(torqueNm / Mathf.Max(1e-3f, maxTorque));
                    keys[i] = new Keyframe(t, tNorm);
                }
            }

            var derived = new AnimationCurve(keys);

            // Keep it smooth-ish by default
            derived.preWrapMode = WrapMode.ClampForever;
            derived.postWrapMode = WrapMode.ClampForever;

            if (curveAuthority == CurveAuthority.TorqueIsAuthoritative)
                powerCurve = derived;
            else
                torqueCurve = derived;
        }
    }
}

