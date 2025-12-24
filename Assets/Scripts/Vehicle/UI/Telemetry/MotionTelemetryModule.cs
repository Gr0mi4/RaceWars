using UnityEngine;
using Vehicle.Core;
using Vehicle.Specs;

namespace Vehicle.UI.Telemetry
{
    /// <summary>
    /// Telemetry module for displaying vehicle motion data (speed, acceleration, yaw rate).
    /// </summary>
    public sealed class MotionTelemetryModule : TelemetryModuleBase
    {
        private float _prevSpeed;
        private float _prevTime;

        public override string ModuleName => "[MOTION]";

        public MotionTelemetryModule(bool enabled = true) : base(enabled)
        {
            _prevSpeed = 0f;
            _prevTime = 0f;
        }

        protected override string BuildDisplayText(in VehicleInput input, in VehicleState state, in VehicleContext ctx)
        {
            float speed = state.speed;
            float speedKmh = speed * 3.6f;
            float sideSpeed = state.localVelocity.x;
            float sideSpeedKmh = sideSpeed * 3.6f;
            float yawRate = state.yawRate;

            // Calculate acceleration
            float acceleration = 0f;
            if (_prevTime > 0f && ctx.dt > 0f)
            {
                acceleration = (speed - _prevSpeed) / ctx.dt;
            }
            _prevSpeed = speed;
            _prevTime = Time.time;

            string text = ModuleName + "\n";
            text += FormatValue("Speed", speed, "m/s", 2) + $" ({speedKmh:F1} km/h)\n";
            text += FormatValue("Side Speed", sideSpeed, "m/s", 2) + $" ({sideSpeedKmh:F1} km/h)\n";
            text += FormatValue("Yaw Rate", yawRate, "rad/s", 2);
            text += FormatValue("Acceleration", acceleration, "m/s²", 2);
            return text;
        }
    }
}

