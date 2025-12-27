using UnityEngine;
using Vehicle.Core;

namespace Vehicle.Systems
{
    /// <summary>
    /// Wheel system (lightweight).
    /// Stores wheel radius in state so other systems (drive/suspension) have a single source of truth.
    /// Lateral tire forces are handled by TireForcesSystem (per-wheel at contact).
    /// </summary>
    public sealed class WheelSystem : IVehicleModule
    {
        private readonly float _wheelRadius;

        public WheelSystem(float wheelRadius, float sideGrip, float handbrakeGripMultiplier)
        {
            _wheelRadius = Mathf.Max(0.01f, wheelRadius);
            // sideGrip/handbrakeGripMultiplier kept in WheelSpec and used by TireForcesSystem.
            // We keep constructor signature so your pipeline code doesn't change.
        }

        public void Tick(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
        {
            state.wheelRadius = _wheelRadius;
        }
    }
}
