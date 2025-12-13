using UnityEngine;
using Vehicle.Core;

namespace Vehicle.Modules
{
    public sealed class SpeedLimiterModule : IVehicleModule
    {
        private readonly bool _flatOnly;
        private readonly float _maxSpeedMultiplier;

        public SpeedLimiterModule(bool flatOnly, float maxSpeedMultiplier)
        {
            _flatOnly = flatOnly;
            _maxSpeedMultiplier = Mathf.Max(0.01f, maxSpeedMultiplier);
        }

        public void Tick(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
        {
            float maxSpeed = (ctx.spec != null ? ctx.spec.maxSpeed : 0f) * _maxSpeedMultiplier;
            if (maxSpeed <= 0f) return;

            Vector3 v = RigidbodyCompat.GetVelocity(ctx.rb);

            if (_flatOnly)
            {
                Vector3 flat = new Vector3(v.x, 0f, v.z);
                float m = flat.magnitude;
                if (m <= maxSpeed) return;

                Vector3 limited = flat / m * maxSpeed;
                RigidbodyCompat.SetVelocity(ctx.rb, new Vector3(limited.x, v.y, limited.z));
            }
            else
            {
                float m = v.magnitude;
                if (m <= maxSpeed) return;

                Vector3 limited = v / m * maxSpeed;
                RigidbodyCompat.SetVelocity(ctx.rb, limited);
            }
        }
    }
}
