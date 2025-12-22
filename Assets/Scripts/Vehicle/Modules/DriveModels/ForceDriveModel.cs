using UnityEngine;
using Vehicle.Core;

namespace Vehicle.Modules.DriveModels
{
    /// <summary>
    /// Drive model that applies forces directly to the rigidbody using AddForce.
    /// Provides realistic acceleration and respects physics constraints.
    /// </summary>
    public sealed class ForceDriveModel : IDriveModel
    {
        private readonly float _motorForceMultiplier;

        /// <summary>
        /// Initializes a new instance of the ForceDriveModel with a force multiplier.
        /// </summary>
        /// <param name="motorForceMultiplier">Multiplier for the base motor force from CarSpec. Must be greater than 0.01.</param>
        public ForceDriveModel(float motorForceMultiplier)
        {
            _motorForceMultiplier = Mathf.Max(0.01f, motorForceMultiplier);
        }

        /// <summary>
        /// Applies driving force to the vehicle's rigidbody in the forward direction.
        /// Force magnitude is calculated as: motorForce * motorForceMultiplier * throttle.
        /// </summary>
        /// <param name="input">Current vehicle input containing throttle value (0-1).</param>
        /// <param name="state">Current vehicle state (not used in this model).</param>
        /// <param name="ctx">Vehicle context containing rigidbody and car specifications.</param>
        public void ApplyDrive(in VehicleInput input, in VehicleState state, in VehicleContext ctx)
        {
            float motorForce = (ctx.spec != null ? ctx.spec.motorForce : 0f) * _motorForceMultiplier;
            Vector3 force = ctx.Forward * (input.throttle * motorForce);

            ctx.rb.AddForce(force, ForceMode.Force);
        }
    }
}
