using UnityEngine;
using Vehicle.Core;

namespace Vehicle.Modules.DriveModels
{
    public sealed class ForceDriveModel : IDriveModel
    {
        private readonly float _motorForceMultiplier;

        public ForceDriveModel(float motorForceMultiplier)
        {
            _motorForceMultiplier = Mathf.Max(0.01f, motorForceMultiplier);
        }

        public void ApplyDrive(in VehicleInput input, in VehicleState state, in VehicleContext ctx)
        {
            float motorForce = (ctx.spec != null ? ctx.spec.motorForce : 0f) * _motorForceMultiplier;
            Vector3 force = ctx.Forward * (input.throttle * motorForce);

            ctx.rb.AddForce(force, ForceMode.Force);
        }
    }
}
