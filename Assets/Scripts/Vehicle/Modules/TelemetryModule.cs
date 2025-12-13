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

        public TelemetryModule(bool enabled, float intervalSeconds, bool logCollisions)
        {
            _enabled = enabled;
            _intervalSeconds = Mathf.Max(0.05f, intervalSeconds);
            _logCollisions = logCollisions;
        }

        public void Tick(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
        {
            if (!_enabled) return;
            if (Time.time < _nextTime) return;

            _nextTime = Time.time + _intervalSeconds;

            Debug.Log(
                $"[Vehicle] throttle={input.throttle:0.00} steer={input.steer:0.00} speed={state.speed:0.00} pos={ctx.tr.position}"
            );
        }

        public void OnCollisionEnter(Collision collision, in VehicleContext ctx, ref VehicleState state)
        {
            if (!_enabled || !_logCollisions) return;

            float rel = collision.relativeVelocity.magnitude;
            Debug.Log($"[Vehicle] Collision with '{collision.gameObject.name}', relativeSpeed={rel:0.00}");
        }
    }
}
