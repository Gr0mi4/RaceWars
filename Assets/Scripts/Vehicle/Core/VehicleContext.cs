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
        /// Engine specification containing engine power, torque, and RPM characteristics.
        /// Can be null if engine-based drive model is not used.
        /// </summary>
        public readonly EngineSpec engineSpec;

        /// <summary>
        /// Gearbox specification containing gear ratios and transmission settings.
        /// Can be null if engine-based drive model is not used.
        /// </summary>
        public readonly GearboxSpec gearboxSpec;

        /// <summary>
        /// Wheel specification containing wheel parameters.
        /// Can be null if wheel module is not used.
        /// </summary>
        public readonly WheelSpec wheelSpec;

        /// <summary>
        /// Chassis specification containing mass, center of mass, aerodynamics, and mass distribution.
        /// Can be null if chassis parameters are not used.
        /// </summary>
        public readonly ChassisSpec chassisSpec;

        /// <summary>
        /// Steering specification containing steering geometry, tire grip, and yaw control.
        /// Can be null if steering is not used.
        /// </summary>
        public readonly SteeringSpec steeringSpec;

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
        /// Initializes a new instance of the VehicleContext with basic parameters.
        /// </summary>
        /// <param name="rb">The rigidbody component.</param>
        /// <param name="tr">The transform component.</param>
        /// <param name="spec">The car specification.</param>
        /// <param name="dt">The fixed delta time.</param>
        public VehicleContext(Rigidbody rb, Transform tr, CarSpec spec, float dt)
            : this(rb, tr, spec, dt, null, null, null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the VehicleContext with all parameters including all component specs.
        /// </summary>
        /// <param name="rb">The rigidbody component.</param>
        /// <param name="tr">The transform component.</param>
        /// <param name="spec">The car specification.</param>
        /// <param name="dt">The fixed delta time.</param>
        /// <param name="engineSpec">Engine specification (optional).</param>
        /// <param name="gearboxSpec">Gearbox specification (optional).</param>
        /// <param name="wheelSpec">Wheel specification (optional).</param>
        /// <param name="chassisSpec">Chassis specification (optional).</param>
        /// <param name="steeringSpec">Steering specification (optional).</param>
        public VehicleContext(
            Rigidbody rb, 
            Transform tr, 
            CarSpec spec, 
            float dt,
            EngineSpec engineSpec,
            GearboxSpec gearboxSpec,
            WheelSpec wheelSpec,
            ChassisSpec chassisSpec,
            SteeringSpec steeringSpec)
        {
            this.rb = rb;
            this.tr = tr;
            this.spec = spec;
            this.dt = dt;
            this.engineSpec = engineSpec;
            this.gearboxSpec = gearboxSpec;
            this.wheelSpec = wheelSpec;
            this.chassisSpec = chassisSpec;
            this.steeringSpec = steeringSpec;
        }
    }
}
