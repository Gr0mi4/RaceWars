using UnityEngine;
using Vehicle.Core;
using Vehicle.Modules.SteeringModels;

namespace Vehicle.Modules
{
    /// <summary>
    /// Steering module that applies yaw control to the vehicle using physics-based steering models.
    /// </summary>
    public sealed class SteeringModule : IVehicleModule
    {
        private readonly ISteeringModel _steeringModel;

        /// <summary>
        /// Creates a steering module using a physics-based steering model.
        /// </summary>
        public SteeringModule(ISteeringModel steeringModel)
        {
            _steeringModel = steeringModel;

            if (_steeringModel == null)
            {
                Debug.LogError("[SteeringModule] Steering model is null! Steering will be disabled.");
            }
        }

        /// <summary>
        /// Updates the steering module, applying yaw torque based on steering input.
        /// </summary>
        /// <param name="input">Current vehicle input containing steering value (-1 to 1).</param>
        /// <param name="state">Current vehicle state (modified by reference).</param>
        /// <param name="ctx">Vehicle context containing rigidbody, transform, and specifications.</param>
        public void Tick(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
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

