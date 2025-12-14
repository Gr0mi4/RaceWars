using Vehicle.Core;

namespace Vehicle.Modules.SteeringModels
{
    /// <summary>
    /// Interface for steering models that compute yaw torque based on input and vehicle state.
    /// Similar to IDriveModel pattern for modularity.
    /// </summary>
    public interface ISteeringModel
    {
        /// <summary>
        /// Computes the yaw torque to apply based on steering input and current vehicle state.
        /// </summary>
        /// <param name="input">Current vehicle input (steer, throttle, brake, handbrake)</param>
        /// <param name="state">Current vehicle state (velocity, yaw rate, etc.)</param>
        /// <param name="ctx">Vehicle context (rigidbody, transform, spec, dt)</param>
        /// <param name="yawTorque">Output: yaw torque to apply (in N⋅m, around Y axis)</param>
        /// <returns>True if steering should be applied, false otherwise (e.g., zero forward speed)</returns>
        bool ApplySteering(in VehicleInput input, in VehicleState state, in VehicleContext ctx, out float yawTorque);
    }
}

