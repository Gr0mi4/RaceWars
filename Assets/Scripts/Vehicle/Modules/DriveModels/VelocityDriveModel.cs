using UnityEngine;
using Vehicle.Core;

namespace Vehicle.Modules.DriveModels
{
    /// <summary>
    /// Drive model that directly sets velocity using Lerp interpolation.
    /// Provides arcade-style driving with immediate response and smooth acceleration/deceleration.
    /// Uses different blend rates for powered (throttle/brake) and coasting states.
    /// </summary>
    public sealed class VelocityDriveModel : IDriveModel
    {
        private readonly float _poweredBlend;
        private readonly float _coastingBlend;

        /// <summary>
        /// Initializes a new instance of the VelocityDriveModel with blend rates.
        /// </summary>
        /// <param name="poweredBlend">Blend rate when throttle or brake is applied (0.01-0.5). Higher = faster response.</param>
        /// <param name="coastingBlend">Blend rate when no input (0.001-0.2). Lower = more inertia.</param>
        public VelocityDriveModel(float poweredBlend, float coastingBlend)
        {
            _poweredBlend = Mathf.Clamp(poweredBlend, 0.01f, 0.5f);
            _coastingBlend = Mathf.Clamp(coastingBlend, 0.001f, 0.2f);
        }

        /// <summary>
        /// Applies velocity-based driving by interpolating current velocity toward desired velocity.
        /// Preserves lateral and vertical velocity components, only modifies forward/backward (Z axis).
        /// Implements brake-then-reverse logic to prevent instant direction changes.
        /// </summary>
        /// <param name="input">Current vehicle input containing throttle and brake values.</param>
        /// <param name="state">Current vehicle state (not used in this model).</param>
        /// <param name="ctx">Vehicle context containing rigidbody, transform, and car specifications.</param>
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

