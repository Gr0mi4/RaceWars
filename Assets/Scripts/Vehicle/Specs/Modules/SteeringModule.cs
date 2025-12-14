using UnityEngine;
using Vehicle.Core;
using Vehicle.Modules.SteeringModels;

namespace Vehicle.Modules
{
    /// <summary>
    /// Steering module that applies yaw control to the vehicle.
    /// Supports both legacy direct rotation and new physics-based steering models.
    /// </summary>
    public sealed class SteeringModule : IVehicleModule
    {
        private readonly ISteeringModel _steeringModel;
        private readonly bool _useLegacyMode;
        private readonly float _minSpeedToSteer;
        private readonly float _strengthMultiplier;

        /// <summary>
        /// Creates a steering module using a physics-based steering model.
        /// </summary>
        public SteeringModule(ISteeringModel steeringModel)
        {
            _steeringModel = steeringModel;
            _useLegacyMode = false;
            _minSpeedToSteer = 0f;
            _strengthMultiplier = 0f;

            if (_steeringModel == null)
            {
                Debug.LogError("[SteeringModule] Steering model is null! Steering will be disabled.");
            }
        }

        /// <summary>
        /// Creates a steering module using legacy direct rotation (deprecated).
        /// </summary>
        public SteeringModule(float minSpeedToSteer, float strengthMultiplier)
        {
            _useLegacyMode = true;
            _minSpeedToSteer = Mathf.Max(0f, minSpeedToSteer);
            _strengthMultiplier = Mathf.Max(0.01f, strengthMultiplier);
            _steeringModel = null;
        }

        public void Tick(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
        {
            if (_useLegacyMode)
            {
                TickLegacy(input, ref state, ctx);
            }
            else
            {
                TickPhysics(input, ref state, ctx);
            }
        }

        private void TickLegacy(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
        {
            if (state.speed <= _minSpeedToSteer) return;

            float steerStrength = (ctx.spec != null ? ctx.spec.steerStrength : 0f) * _strengthMultiplier;
            float yawDegrees = input.steer * steerStrength * ctx.dt;

            if (Mathf.Abs(yawDegrees) < 0.0001f) return;

            Quaternion delta = Quaternion.Euler(0f, yawDegrees, 0f);
            ctx.rb.MoveRotation(ctx.rb.rotation * delta);
        }

        private void TickPhysics(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
        {
            if (_steeringModel == null)
            {
                return;
            }

            // Validate Rigidbody
            if (ctx.rb == null)
            {
                Debug.LogError("[SteeringModule] Rigidbody is null! Cannot apply steering.");
                return;
            }

            // Apply steering via model
            if (_steeringModel.ApplySteering(input, state, ctx, out float yawTorque))
            {
                // Apply torque around Y axis (yaw)
                Vector3 torque = new Vector3(0f, yawTorque, 0f);
                ctx.rb.AddTorque(torque, ForceMode.Force);
            }
        }
    }
}
