using UnityEngine;
using Vehicle.Specs;

namespace Vehicle.Core
{
    /// <summary>
    /// Immutable context structure passed to vehicle modules. Contains all necessary references
    /// for modules to interact with the vehicle's physics and configuration.
    /// </summary>
    public readonly struct VehicleContext
    {
        /// <summary>
        /// The rigidbody component of the vehicle.
        /// </summary>
        public readonly Rigidbody rb;
        
        /// <summary>
        /// The transform component of the vehicle.
        /// </summary>
        public readonly Transform tr;
        
        /// <summary>
        /// The car specification containing vehicle parameters.
        /// </summary>
        public readonly CarSpec spec;
        
        /// <summary>
        /// The fixed delta time for the current physics update.
        /// </summary>
        public readonly float dt;

        /// <summary>
        /// Forward direction of the vehicle in world space.
        /// </summary>
        public Vector3 Forward => tr.forward;
        
        /// <summary>
        /// Right direction of the vehicle in world space.
        /// </summary>
        public Vector3 Right => tr.right;
        
        /// <summary>
        /// Up direction of the vehicle in world space.
        /// </summary>
        public Vector3 Up => tr.up;

        /// <summary>
        /// Initializes a new instance of the VehicleContext.
        /// </summary>
        /// <param name="rb">The rigidbody component.</param>
        /// <param name="tr">The transform component.</param>
        /// <param name="spec">The car specification.</param>
        /// <param name="dt">The fixed delta time.</param>
        public VehicleContext(Rigidbody rb, Transform tr, CarSpec spec, float dt)
        {
            this.rb = rb;
            this.tr = tr;
            this.spec = spec;
            this.dt = dt;
        }
    }
}
