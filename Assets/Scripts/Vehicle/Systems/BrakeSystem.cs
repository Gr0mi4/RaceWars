using UnityEngine;
using Vehicle.Core;

namespace Vehicle.Systems
{
    /// <summary>
    /// BrakingSystem:
    /// - Converts input.brake and input.handbrake into per-wheel brakeTorque requests.
    /// - Does NOT apply forces. TireForcesSystem converts torques to Fx and clamps by friction circle.
    /// </summary>
    public sealed class BrakingSystem : IVehicleModule
    {
        private const float Epsilon = 1e-5f;

        // Temporary tuning constants (replace with BrakeSpec later)
        private const float BrakeTorquePerKg = 3.2f;      // N*m per kg at full brake
        private const float HandbrakeTorquePerKg = 4.0f;  // N*m per kg at full handbrake (rear)

        public void Tick(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
        {
            if (state.wheels == null || state.wheels.Length == 0 || ctx.rb == null)
                return;

            float mass = Mathf.Max(1f, ctx.rb.mass);

            float brake01 = Mathf.Clamp01(Mathf.Abs(input.brake));
            float hand01  = Mathf.Clamp01(Mathf.Abs(input.handbrake));

            // Clear every tick
            for (int i = 0; i < state.wheels.Length; i++)
                state.wheels[i].brakeTorque = 0f;

            if (brake01 > Epsilon)
            {
                float tq = (BrakeTorquePerKg * mass) * brake01;
                for (int i = 0; i < state.wheels.Length; i++)
                    state.wheels[i].brakeTorque += tq;
            }

            // Handbrake: rear only (2,3)
            if (hand01 > Epsilon && state.wheels.Length >= 4)
            {
                float tq = (HandbrakeTorquePerKg * mass) * hand01;
                state.wheels[1].brakeTorque += tq;
                state.wheels[3].brakeTorque += tq;
            }
        }
    }
}
