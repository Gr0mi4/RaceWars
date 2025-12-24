using UnityEngine;
using Vehicle.Core;
using Vehicle.Specs;
using Vehicle.Systems;

namespace Vehicle.UI.Telemetry
{
    /// <summary>
    /// Detailed telemetry module for engine diagnostics with comprehensive parameters.
    /// </summary>
    public sealed class EngineTelemetryModule : TelemetryModuleBase
    {
        private readonly EngineSystem _engineSystem;
        private float _prevRPM;
        private float _prevRPMTime;

        public override string ModuleName => "[ENGINE]";

        public EngineTelemetryModule(bool enabled = true) : base(enabled)
        {
            _engineSystem = new EngineSystem();
            _prevRPM = 0f;
            _prevRPMTime = 0f;
        }

        protected override string BuildDisplayText(in VehicleInput input, in VehicleState state, in VehicleContext ctx)
        {
            if (ctx.engineSpec == null)
            {
                return string.Empty;
            }

            float rpm = state.engineRPM;
            float maxRPM = ctx.engineSpec.maxRPM;
            float idleRPM = ctx.engineSpec.idleRPM;
            float throttle = input.throttle;

            // Calculate normalized RPM
            float normalizedRPM = 0f;
            if (maxRPM > idleRPM)
            {
                normalizedRPM = Mathf.Clamp01((rpm - idleRPM) / (maxRPM - idleRPM));
            }

            // Calculate RPM change rate
            float rpmChangeRate = 0f;
            if (_prevRPMTime > 0f && ctx.dt > 0f)
            {
                rpmChangeRate = (rpm - _prevRPM) / ctx.dt;
            }
            _prevRPM = rpm;
            _prevRPMTime = Time.time;

            // Check if at RPM limit
            bool isAtRPMLimit = rpm >= maxRPM;

            // Calculate actual engine RPM from speed (for comparison)
            float actualEngineRPM = 0f;
            if (ctx.gearboxSpec != null && state.wheelRadius > 0.01f)
            {
                float currentGearRatio = GetCurrentGearRatio(state.currentGear, ctx.gearboxSpec);
                if (Mathf.Abs(currentGearRatio) > 0.01f)
                {
                    float speed = Mathf.Abs(Vector3.Dot(RigidbodyCompat.GetVelocity(ctx.rb), ctx.tr.forward));
                    float wheelAngularVelocity = _engineSystem.CalculateWheelAngularVelocityFromSpeed(speed, state.wheelRadius);
                    float baseGearRatio = Mathf.Abs(currentGearRatio) / ctx.gearboxSpec.finalDriveRatio;
                    actualEngineRPM = _engineSystem.CalculateEngineRPMFromWheel(
                        wheelAngularVelocity,
                        baseGearRatio,
                        ctx.gearboxSpec.finalDriveRatio
                    );
                }
            }

            // Calculate power and torque
            float powerHP = 0f;
            float powerKW = 0f;
            float powerMultiplier = 0f;
            float engineTorque = 0f;
            float torqueMultiplier = 0f;
            bool gearEngaged = IsGearEngaged(state.currentGear);

            if (rpm > 0f)
            {
                powerMultiplier = ctx.engineSpec.powerCurve.Evaluate(normalizedRPM);
                powerHP = ctx.engineSpec.maxPower * powerMultiplier * throttle;
                powerKW = powerHP * 0.7457f;

                torqueMultiplier = ctx.engineSpec.torqueCurve.Evaluate(normalizedRPM);
                engineTorque = _engineSystem.GetTorque(rpm, throttle, ctx.engineSpec, gearEngaged);
            }

            // Calculate wheel torque and force
            float wheelTorque = 0f;
            float wheelForce = 0f;
            float powerAtWheels = 0f;
            float powerEfficiency = 0f;

            if (ctx.gearboxSpec != null && gearEngaged && engineTorque > 0f)
            {
                float currentGearRatio = GetCurrentGearRatio(state.currentGear, ctx.gearboxSpec);
                float absGearRatio = Mathf.Abs(currentGearRatio);
                wheelTorque = engineTorque * absGearRatio;

                if (state.wheelRadius > 0.01f)
                {
                    wheelForce = wheelTorque / state.wheelRadius;
                }

                // Calculate power at wheels
                float speed = Mathf.Abs(Vector3.Dot(RigidbodyCompat.GetVelocity(ctx.rb), ctx.tr.forward));
                if (speed > 0.001f)
                {
                    powerAtWheels = Mathf.Abs(wheelForce) * speed / 745.7f; // HP
                }

                // Calculate efficiency
                if (powerHP > 0.001f)
                {
                    powerEfficiency = (powerAtWheels / powerHP) * 100f;
                }
            }

            // Determine force calculation method
            string forceMethod = "N/A";
            if (ctx.gearboxSpec != null && state.wheelRadius > 0.01f)
            {
                float speed = Mathf.Abs(Vector3.Dot(RigidbodyCompat.GetVelocity(ctx.rb), ctx.tr.forward));
                if (speed < ctx.engineSpec.minSpeedForPower)
                {
                    forceMethod = "Torque-based";
                }
                else
                {
                    forceMethod = "Power-based";
                }
            }

            // Check idle torque status
            bool isIdleTorque = throttle <= 0f && Mathf.Abs(rpm - idleRPM) < 50f;

            string text = ModuleName + "\n";
            
            // Basic parameters
            text += FormatValue("RPM", rpm, "", 0) + $" / {maxRPM:F0} ({normalizedRPM * 100f:F1}%)\n";
            text += FormatValue("Normalized RPM", normalizedRPM, "", 3);
            text += FormatValue("RPM Change Rate", rpmChangeRate, "RPM/s", 1);
            text += FormatBool("At RPM Limit", isAtRPMLimit);
            text += FormatValue("Actual RPM (from speed)", actualEngineRPM, "", 0);
            text += FormatValue("RPM Difference", rpm - actualEngineRPM, "", 0) + "\n";

            // Power
            text += FormatValue("Power", powerHP, "HP", 1) + $" ({powerKW:F1} kW)\n";
            text += FormatValue("Power Multiplier", powerMultiplier, "", 3);
            text += FormatValue("Power at Wheels", powerAtWheels, "HP", 1);
            text += FormatValue("Power Efficiency", powerEfficiency, "%", 1) + "\n";

            // Torque
            text += FormatValue("Engine Torque", engineTorque, "Nm", 1);
            text += FormatValue("Torque Multiplier", torqueMultiplier, "", 3);
            text += FormatValue("Wheel Torque", wheelTorque, "Nm", 1);
            text += FormatValue("Wheel Force", wheelForce, "N", 0) + "\n";

            // Force calculation
            text += FormatString("Force Method", forceMethod);
            text += FormatValue("Speed vs minSpeedForPower", 
                Mathf.Abs(Vector3.Dot(RigidbodyCompat.GetVelocity(ctx.rb), ctx.tr.forward)), "m/s", 2) + 
                $" (min: {ctx.engineSpec.minSpeedForPower:F2})\n";
            text += FormatValue("Throttle", throttle, "", 2);
            text += FormatBool("Gear Engaged", gearEngaged);
            text += FormatBool("Idle Torque Active", isIdleTorque) + "\n";

            // Static parameters (optional, can be shown if needed)
            // Uncomment if needed for debugging:
            // text += FormatValue("Max Power", ctx.engineSpec.maxPower, "HP", 1);
            // text += FormatValue("Max Torque", ctx.engineSpec.maxTorque, "Nm", 1);
            // text += FormatValue("Idle RPM", idleRPM, "", 0);
            // text += FormatValue("Max RPM", maxRPM, "", 0);
            // text += FormatValue("RPM Inertia", ctx.engineSpec.rpmInertia, "", 1);

            return text;
        }

        private float GetCurrentGearRatio(int currentGear, GearboxSpec gearboxSpec)
        {
            if (currentGear == -1)
            {
                return gearboxSpec.reverseGearRatio * gearboxSpec.finalDriveRatio;
            }
            else if (currentGear > 0 && currentGear <= gearboxSpec.gearRatios.Length)
            {
                int arrayIndex = currentGear - 1;
                return gearboxSpec.gearRatios[arrayIndex] * gearboxSpec.finalDriveRatio;
            }
            return 0f;
        }

        private bool IsGearEngaged(int currentGear)
        {
            return currentGear != 0;
        }
    }
}

