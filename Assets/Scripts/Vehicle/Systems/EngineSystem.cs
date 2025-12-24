using UnityEngine;
using Vehicle.Specs;

namespace Vehicle.Systems
{
    /// <summary>
    /// Engine system for calculating engine behavior including RPM, power, torque, and wheel force.
    /// Uses power and torque curves from EngineSpec to provide realistic engine characteristics.
    /// </summary>
    public sealed class EngineSystem
    {
        /// <summary>
        /// Conversion factor from horsepower to watts: 1 HP = 745.7 W.
        /// </summary>
        private const float HpToWatts = 745.7f;

        /// <summary>
        /// Calculates engine RPM from wheel angular velocity, gear ratio, and final drive ratio.
        /// Formula: ω_engine = ω_wheel × gearRatio × finalDriveRatio
        /// Then converts to RPM: RPM = (ω_engine × 60) / (2π)
        /// </summary>
        /// <param name="wheelAngularVelocity">Wheel angular velocity in rad/s.</param>
        /// <param name="gearRatio">Current gear ratio (positive for forward, negative for reverse).</param>
        /// <param name="finalDriveRatio">Final drive ratio (differential ratio).</param>
        /// <returns>Engine RPM. Returns 0 if inputs are invalid.</returns>
        public float CalculateEngineRPMFromWheel(float wheelAngularVelocity, float gearRatio, float finalDriveRatio)
        {
            if (finalDriveRatio <= 0f || Mathf.Abs(gearRatio) < 0.01f)
            {
                return 0f;
            }

            // Calculate engine angular velocity: ω_engine = ω_wheel × gearRatio × finalDriveRatio
            float engineAngularVelocity = wheelAngularVelocity * Mathf.Abs(gearRatio) * finalDriveRatio;

            // Convert to RPM: RPM = (rad/s) × (60 / 2π)
            float rpm = engineAngularVelocity * (60f / (2f * Mathf.PI));

            return Mathf.Max(0f, rpm);
        }

        /// <summary>
        /// Calculates wheel angular velocity from engine RPM, gear ratio, and final drive ratio.
        /// Formula: ω_wheel = ω_engine / (gearRatio × finalDriveRatio)
        /// </summary>
        /// <param name="engineRPM">Engine RPM (crankshaft revolutions per minute).</param>
        /// <param name="gearRatio">Current gear ratio (positive for forward, negative for reverse).</param>
        /// <param name="finalDriveRatio">Final drive ratio (differential ratio).</param>
        /// <returns>Wheel angular velocity in rad/s. Returns 0 if inputs are invalid.</returns>
        public float CalculateWheelAngularVelocityFromEngine(float engineRPM, float gearRatio, float finalDriveRatio)
        {
            if (finalDriveRatio <= 0f || Mathf.Abs(gearRatio) < 0.01f)
            {
                return 0f;
            }

            // Convert RPM to rad/s: ω = RPM × (2π / 60)
            float engineAngularVelocity = engineRPM * (2f * Mathf.PI / 60f);

            // Calculate wheel angular velocity: ω_wheel = ω_engine / (gearRatio × finalDriveRatio)
            float wheelAngularVelocity = engineAngularVelocity / (Mathf.Abs(gearRatio) * finalDriveRatio);

            return Mathf.Max(0f, wheelAngularVelocity);
        }

        /// <summary>
        /// Calculates vehicle speed from wheel angular velocity and wheel radius.
        /// Formula: v = ω_wheel × wheelRadius
        /// </summary>
        /// <param name="wheelAngularVelocity">Wheel angular velocity in rad/s.</param>
        /// <param name="wheelRadius">Wheel radius in meters.</param>
        /// <returns>Vehicle speed in m/s. Returns 0 if inputs are invalid.</returns>
        public float CalculateSpeedFromWheel(float wheelAngularVelocity, float wheelRadius)
        {
            if (wheelRadius <= 0f)
            {
                return 0f;
            }

            // Calculate speed: v = ω × r
            return wheelAngularVelocity * wheelRadius;
        }

        /// <summary>
        /// Calculates wheel angular velocity from vehicle speed and wheel radius.
        /// Formula: ω_wheel = v / wheelRadius
        /// </summary>
        /// <param name="speed">Vehicle speed in m/s. Can be negative for reverse.</param>
        /// <param name="wheelRadius">Wheel radius in meters.</param>
        /// <returns>Wheel angular velocity in rad/s. Returns 0 if inputs are invalid.</returns>
        public float CalculateWheelAngularVelocityFromSpeed(float speed, float wheelRadius)
        {
            if (wheelRadius <= 0f)
            {
                return 0f;
            }

            // Calculate wheel angular velocity: ω = v / r
            return speed / wheelRadius;
        }

        /// <summary>
        /// Gets the current engine power at a given RPM and throttle position.
        /// Power is calculated from the power curve in EngineSpec, scaled by maxPower and throttle.
        /// </summary>
        /// <param name="rpm">Current engine RPM.</param>
        /// <param name="throttle">Throttle input (0-1).</param>
        /// <param name="engineSpec">Engine specification containing power curve and max power.</param>
        /// <returns>Current engine power in watts. Returns 0 if spec is null or invalid.</returns>
        public float GetPower(float rpm, float throttle, EngineSpec engineSpec)
        {
            if (engineSpec == null || throttle <= 0f)
            {
                return 0f;
            }

            // Normalize RPM to 0-1 range (0 = idleRPM, 1 = maxRPM)
            float normalizedRPM = NormalizeRPM(rpm, engineSpec.idleRPM, engineSpec.maxRPM);

            // Get power multiplier from curve (0-1)
            float powerMultiplier = engineSpec.powerCurve.Evaluate(normalizedRPM);

            // Calculate current power: P = maxPower * curve(rpm) * throttle
            float powerHP = engineSpec.maxPower * powerMultiplier * throttle;

            // Convert HP to watts
            return powerHP * HpToWatts;
        }

        /// <summary>
        /// Gets the current engine torque at a given RPM and throttle position.
        /// Torque is calculated from the torque curve in EngineSpec, scaled by maxTorque and throttle.
        /// When throttle is 0, returns idle torque if RPM is near idle (allows vehicle to creep at idle).
        /// </summary>
        /// <param name="rpm">Current engine RPM.</param>
        /// <param name="throttle">Throttle input (0-1).</param>
        /// <param name="engineSpec">Engine specification containing torque curve and max torque.</param>
        /// <param name="gearEngaged">Whether a gear is engaged (not neutral). If false, returns 0 even at idle.</param>
        /// <returns>Current engine torque in Newton-meters. Returns 0 if spec is null or gear not engaged.</returns>
        public float GetTorque(float rpm, float throttle, EngineSpec engineSpec, bool gearEngaged = true)
        {
            if (engineSpec == null || !gearEngaged)
            {
                return 0f;
            }

            // If throttle is 0, return idle torque only if RPM is at idle (allows creeping at idle)
            if (throttle <= 0f)
            {
                if (Mathf.Abs(rpm - engineSpec.idleRPM) < 50f)
                {
                    return engineSpec.idleTorque;
                }
                return 0f;
            }

            // Normalize RPM to 0-1 range (0 = idleRPM, 1 = maxRPM)
            float normalizedRPM = NormalizeRPM(rpm, engineSpec.idleRPM, engineSpec.maxRPM);

            // Get torque multiplier from curve (0-1) based on current RPM
            float torqueMultiplier = engineSpec.torqueCurve.Evaluate(normalizedRPM);

            // Calculate torque: T = maxTorque * curve(rpm) * throttle
            // This is the standard formula: torque depends on RPM (from curve) and throttle position
            // As throttle increases, torque increases. As RPM changes, torque changes according to curve.
            float torque = engineSpec.maxTorque * torqueMultiplier * throttle;
            
            return torque;
        }

        /// <summary>
        /// Calculates the force applied to the wheels based on engine power, torque, speed, and gear ratio.
        /// At low speeds, uses torque-based calculation. At higher speeds, uses power-based calculation.
        /// Formula at low speed: F = (T * gearRatio * finalDriveRatio) / wheelRadius
        /// Formula at high speed: F = P / v (where P is power in watts, v is speed in m/s)
        /// Note: This method is used for telemetry/display purposes. The actual force application
        /// in DriveSystem uses the direct chain: engineRPM → torque → wheelTorque → wheelForce.
        /// </summary>
        /// <param name="speed">Vehicle speed in m/s. Can be negative for reverse.</param>
        /// <param name="throttle">Throttle input (0-1).</param>
        /// <param name="engineSpec">Engine specification.</param>
        /// <param name="currentRPM">Current engine RPM (crankshaft revolutions per minute).</param>
        /// <param name="gearRatio">Current gear ratio (positive for forward, negative for reverse).</param>
        /// <param name="finalDriveRatio">Final drive ratio (differential ratio).</param>
        /// <param name="wheelRadius">Wheel radius in meters.</param>
        /// <returns>Force in Newtons applied to the wheels. Positive for forward, negative for reverse.</returns>
        public float CalculateWheelForce(
            float speed,
            float throttle,
            EngineSpec engineSpec,
            float currentRPM,
            float gearRatio,
            float finalDriveRatio,
            float wheelRadius,
            bool gearEngaged = true)
        {
            if (engineSpec == null || wheelRadius <= 0f || !gearEngaged)
            {
                return 0f;
            }

            float absSpeed = Mathf.Abs(speed);
            float absGearRatio = Mathf.Abs(gearRatio);

            // At very low speeds, use torque-based calculation to avoid division by zero
            // and to provide better low-speed acceleration
            if (absSpeed < engineSpec.minSpeedForPower)
            {
                // Get current torque (gearEngaged is passed to allow idle torque when throttle = 0)
                float torque = GetTorque(currentRPM, throttle, engineSpec, gearEngaged);

                // Calculate force from torque: F = (T * gearRatio * finalDriveRatio) / wheelRadius
                // This is the standard formula: Torque at wheel = Engine Torque * Gear Ratio * Final Drive
                float wheelTorque = torque * absGearRatio * finalDriveRatio;
                float force = wheelTorque / wheelRadius;

                // Apply direction based on gear (forward = positive, reverse = negative)
                return Mathf.Sign(gearRatio) * force;
            }
            else
            {
                // At higher speeds, use power-based calculation: F = P / v
                // This is more accurate at speed because power is constant, force decreases with speed
                float power = GetPower(currentRPM, throttle, engineSpec);

                // Avoid division by zero
                float effectiveSpeed = Mathf.Max(absSpeed, engineSpec.minSpeedForPower);
                float force = power / effectiveSpeed;

                // Apply direction based on gear (forward = positive, reverse = negative)
                return Mathf.Sign(gearRatio) * force;
            }
        }

        /// <summary>
        /// Normalizes RPM value to 0-1 range based on idle and max RPM.
        /// </summary>
        /// <param name="rpm">Current RPM value.</param>
        /// <param name="idleRPM">Idle RPM (maps to 0).</param>
        /// <param name="maxRPM">Maximum RPM (maps to 1).</param>
        /// <returns>Normalized RPM value (0-1). Clamped to valid range.</returns>
        private float NormalizeRPM(float rpm, float idleRPM, float maxRPM)
        {
            if (maxRPM <= idleRPM)
            {
                return 0f;
            }

            float normalized = (rpm - idleRPM) / (maxRPM - idleRPM);
            return Mathf.Clamp01(normalized);
        }
    }
}

