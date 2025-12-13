using UnityEngine;
using Vehicle.Core;

namespace Vehicle.Modules
{
    public sealed class SteeringModule : IVehicleModule
    {
        private readonly float _minSpeedToSteer;
        private readonly float _strengthMultiplier;

        public SteeringModule(float minSpeedToSteer, float strengthMultiplier)
        {
            _minSpeedToSteer = Mathf.Max(0f, minSpeedToSteer);
            _strengthMultiplier = Mathf.Max(0.01f, strengthMultiplier);
        }

        public void Tick(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
        {
            if (state.speed <= _minSpeedToSteer) return;

            float steerStrength = (ctx.spec != null ? ctx.spec.steerStrength : 0f) * _strengthMultiplier;
            float yawDegrees = input.steer * steerStrength * ctx.dt;

            if (Mathf.Abs(yawDegrees) < 0.0001f) return;

            Quaternion delta = Quaternion.Euler(0f, yawDegrees, 0f);
            ctx.rb.MoveRotation(ctx.rb.rotation * delta);
        }
    }
}
