using UnityEngine;
using Vehicle.Core;

namespace Vehicle.Modules
{
    public sealed class TelemetryModule : IVehicleModule, IVehicleCollisionListener
    {
        private readonly bool _enabled;
        private readonly float _intervalSeconds;
        private readonly bool _logCollisions;

        private float _nextTime;
        private float _prevSpeed;
        private float _prevTime;

        public TelemetryModule(bool enabled, float intervalSeconds, bool logCollisions)
        {
            _enabled = enabled;
            _intervalSeconds = Mathf.Max(0.05f, intervalSeconds);
            _logCollisions = logCollisions;
            _prevSpeed = 0f;
            _prevTime = 0f;
        }

        public void Tick(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
        {
            if (!_enabled) return;
            if (Time.time < _nextTime) return;

            _nextTime = Time.time + _intervalSeconds;

            // Get current velocity
            Vector3 velocity = RigidbodyCompat.GetVelocity(ctx.rb);
            float speed = velocity.magnitude;
            float forwardSpeed = Vector3.Dot(velocity, ctx.tr.forward);

            // Calculate acceleration (change in speed over time)
            float acceleration = 0f;
            if (_prevTime > 0f && ctx.dt > 0f)
            {
                acceleration = (speed - _prevSpeed) / ctx.dt;
            }
            _prevSpeed = speed;
            _prevTime = Time.time;

            // Calculate engine-based forces (if engine spec is available)
            float wheelForce = 0f;
            float dragForce = 0f;
            float dampingForce = 0f;
            float netForce = 0f;

            if (ctx.spec != null)
            {
                // Aerodynamic Drag Force: F = 0.5 * ρ * Cd * A * v²
                const float airDensity = 1.225f;
                if (speed > 0.1f && ctx.spec.frontArea > 0f && ctx.spec.dragCoefficient > 0f)
                {
                    dragForce = 0.5f * airDensity * ctx.spec.dragCoefficient * ctx.spec.frontArea * speed * speed;
                }

                // Damping Force: F = -damping * velocity * mass
                float linearDamping = RigidbodyCompat.GetLinearDamping(ctx.rb);
                if (speed > 0.001f)
                {
                    dampingForce = linearDamping * speed * ctx.rb.mass;
                }
            }

            // Calculate wheel force from engine (if engine spec is available)
            if (ctx.engineSpec != null && ctx.gearboxSpec != null && state.engineRPM > 0f)
            {
                // Get current gear ratio
                float currentGearRatio = 0f;
                if (state.currentGear == -1)
                {
                    currentGearRatio = ctx.gearboxSpec.reverseGearRatio * ctx.gearboxSpec.finalDriveRatio;
                }
                else if (state.currentGear > 0 && state.currentGear <= ctx.gearboxSpec.gearRatios.Length)
                {
                    int arrayIndex = state.currentGear - 1;
                    currentGearRatio = ctx.gearboxSpec.gearRatios[arrayIndex] * ctx.gearboxSpec.finalDriveRatio;
                }

                float wheelRadius = state.wheelRadius > 0.01f 
                    ? state.wheelRadius 
                    : (ctx.wheelSpec != null ? ctx.wheelSpec.wheelRadius : 0.3f);

                // Calculate wheel force using EngineModel
                var engineModel = new Vehicle.Modules.DriveModels.EngineModel();
                wheelForce = engineModel.CalculateWheelForce(
                    forwardSpeed,
                    input.throttle,
                    ctx.engineSpec,
                    state.engineRPM,
                    currentGearRatio,
                    ctx.gearboxSpec.finalDriveRatio,
                    wheelRadius
                );
            }

            // Net Force (wheel - drag - damping)
            netForce = wheelForce - dragForce - dampingForce;

            // Get side speed (lateral velocity - left/right movement)
            float sideSpeed = state.localVelocity.x;

            // Build telemetry string with all sections
            string telemetry = "<color=black>=== VEHICLE TELEMETRY ===\n";

            // INPUT section
            telemetry += "[INPUT]\n";
            telemetry += $"  Throttle: {input.throttle:F2}\n";
            telemetry += $"  Brake: {input.brake:F2}\n";
            telemetry += $"  Steer: {input.steer:F2}\n";

            // MOTION section
            telemetry += "[MOTION]\n";
            telemetry += $"  Speed: {speed:F2} m/s ({speed * 3.6f:F1} km/h)\n";
            telemetry += $"  Side Speed: {sideSpeed:F2} m/s\n";
            telemetry += $"  Yaw Rate: {state.yawRate:F2} rad/s\n";
            telemetry += $"  Acceleration: {acceleration:F2} m/s²\n";

            // ENGINE section (if engine spec is available)
            if (ctx.engineSpec != null)
            {
                float rpm = state.engineRPM;
                float powerHP = 0f;
                float torqueNm = 0f;

                // Calculate power and torque if we have RPM
                if (rpm > 0f && input.throttle > 0f)
                {
                    // Normalize RPM
                    float normalizedRPM = Mathf.Clamp01((rpm - ctx.engineSpec.idleRPM) / 
                        (ctx.engineSpec.maxRPM - ctx.engineSpec.idleRPM));
                    
                    // Get from curves
                    float powerMultiplier = ctx.engineSpec.powerCurve.Evaluate(normalizedRPM);
                    float torqueMultiplier = ctx.engineSpec.torqueCurve.Evaluate(normalizedRPM);
                    
                    powerHP = ctx.engineSpec.maxPower * powerMultiplier * input.throttle;
                    torqueNm = ctx.engineSpec.maxTorque * torqueMultiplier * input.throttle;
                }

                telemetry += "[ENGINE]\n";
                telemetry += $"  RPM: {rpm:F0} / {ctx.engineSpec.maxRPM:F0}\n";
                telemetry += $"  Power: {powerHP:F1} HP ({powerHP * 0.7457f:F1} kW)\n";
                telemetry += $"  Torque: {torqueNm:F1} Nm\n";
                telemetry += $"  Max Power: {ctx.engineSpec.maxPower:F1} HP\n";
                telemetry += $"  Max Torque: {ctx.engineSpec.maxTorque:F1} Nm\n";
            }

            // GEARBOX section (if gearbox spec is available)
            if (ctx.gearboxSpec != null)
            {
                string gearName = GetGearName(state.currentGear);
                float currentGearRatio = 0f;

                // Calculate current gear ratio
                if (state.currentGear == -1)
                {
                    currentGearRatio = ctx.gearboxSpec.reverseGearRatio * ctx.gearboxSpec.finalDriveRatio;
                }
                else if (state.currentGear > 0 && state.currentGear <= ctx.gearboxSpec.gearRatios.Length)
                {
                    int arrayIndex = state.currentGear - 1;
                    currentGearRatio = ctx.gearboxSpec.gearRatios[arrayIndex] * ctx.gearboxSpec.finalDriveRatio;
                }

                string transmissionType = ctx.gearboxSpec.transmissionType.ToString();

                telemetry += "[GEARBOX]\n";
                telemetry += $"  Gear: {gearName}\n";
                telemetry += $"  Gear Ratio: {currentGearRatio:F2}\n";
                telemetry += $"  Transmission: {transmissionType}\n";
                telemetry += $"  Final Drive: {ctx.gearboxSpec.finalDriveRatio:F2}\n";
            }

            // WHEELS section
            if (state.wheelRadius > 0.01f)
            {
                telemetry += "[WHEELS]\n";
                telemetry += $"  Wheel Radius: {state.wheelRadius:F3} m\n";
            }

            // FORCES section (engine-based)
            telemetry += "[FORCES]\n";
            if (ctx.engineSpec != null)
            {
                telemetry += $"  Wheel Force: {wheelForce:F0} N\n";
            }
            telemetry += $"  Drag Force: {dragForce:F0} N\n";
            telemetry += $"  Damping Force: {dampingForce:F0} N\n";
            telemetry += $"  Net Force: {netForce:F0} N\n";

            telemetry += "</color>";

            Debug.Log(telemetry);
        }

        public void OnCollisionEnter(Collision collision, in VehicleContext ctx, ref VehicleState state)
        {
            if (!_enabled || !_logCollisions) return;

            float rel = collision.relativeVelocity.magnitude;
            Debug.Log($"[Vehicle] Collision with '{collision.gameObject.name}', relativeSpeed={rel:0.00}");
        }

        /// <summary>
        /// Gets a human-readable name for the current gear.
        /// </summary>
        /// <param name="gear">Gear index (-1 = reverse, 0 = neutral, 1+ = forward gears).</param>
        /// <returns>Gear name as string (e.g., "R", "N", "1", "2", etc.).</returns>
        private string GetGearName(int gear)
        {
            return gear switch
            {
                -1 => "R",
                0 => "N",
                _ => gear.ToString()
            };
        }
    }
}
