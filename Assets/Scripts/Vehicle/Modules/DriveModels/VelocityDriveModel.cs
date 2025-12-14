using UnityEngine;
using Vehicle.Core;

namespace Vehicle.Modules.DriveModels
{
    public sealed class VelocityDriveModel : IDriveModel
    {
        private readonly float _poweredBlend;
        private readonly float _coastingBlend;

        public VelocityDriveModel(float poweredBlend, float coastingBlend)
        {
            _poweredBlend = Mathf.Clamp(poweredBlend, 0.01f, 0.5f);
            _coastingBlend = Mathf.Clamp(coastingBlend, 0.001f, 0.2f);
        }

        public void ApplyDrive(in VehicleInput input, in VehicleState state, in VehicleContext ctx)
        {
            float maxSpeed = ctx.spec != null ? ctx.spec.maxSpeed : 25f;
            
            // Get current velocity in world space and convert to local
            Vector3 vWorld = RigidbodyCompat.GetVelocity(ctx.rb);
            Vector3 vLocal = ctx.tr.InverseTransformDirection(vWorld);
            
            // Calculate acceleration intent: throttle forward, brake backward
            float accel = input.throttle - input.brake;
            
            // Brake-then-reverse logic (symmetric):
            // If moving forward and braking, force stop first before reversing
            if (input.brake > 0.01f && vLocal.z > 0.5f)
            {
                // Car is moving forward and user is braking - force stop first
                accel = 0f;
            }
            // If moving backward and throttling forward, force stop first before going forward
            else if (input.throttle > 0.01f && vLocal.z < -0.5f)
            {
                // Car is moving backward and user is throttling forward - force stop first
                accel = 0f;
            }
            
            float desiredZ = accel * maxSpeed;
            
            // Determine which blend to use based on pedal input
            float absAccel = Mathf.Abs(accel);
            float blend = absAccel > 0.01f ? _poweredBlend : _coastingBlend;
            
            // Only modify forward/backward component (Z), preserve lateral (X) and vertical (Y)
            float blendedZ = Mathf.Lerp(vLocal.z, desiredZ, blend);
            vLocal.z = blendedZ;
            // vLocal.x and vLocal.y remain unchanged (preserves lateral inertia and vertical velocity)
            
            // Convert back to world space
            Vector3 vWorldNew = ctx.tr.TransformDirection(vLocal);
            RigidbodyCompat.SetVelocity(ctx.rb, vWorldNew);
        }
    }
}

