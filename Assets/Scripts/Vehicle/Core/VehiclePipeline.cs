using System.Collections.Generic;

namespace Vehicle.Core
{
    /// <summary>
    /// Executes vehicle modules in sequence. Manages the execution order and collision notifications.
    /// </summary>
    public sealed class VehiclePipeline
    {
        private readonly List<IVehicleModule> _modules;

        /// <summary>
        /// Initializes a new instance of the VehiclePipeline with the specified modules.
        /// </summary>
        /// <param name="modules">List of modules to execute in order. Can be null, which creates an empty pipeline.</param>
        public VehiclePipeline(List<IVehicleModule> modules)
        {
            _modules = modules ?? new List<IVehicleModule>();
        }

        /// <summary>
        /// Executes all modules in the pipeline, passing the current input, state, and context.
        /// </summary>
        /// <param name="input">Current vehicle input.</param>
        /// <param name="state">Current vehicle state (modified by modules).</param>
        /// <param name="ctx">Vehicle context containing rigidbody, transform, and specifications.</param>
        public void Tick(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
        {
            for (int i = 0; i < _modules.Count; i++)
                _modules[i].Tick(input, ref state, ctx);
        }

        /// <summary>
        /// Notifies all collision listener modules about a collision event.
        /// </summary>
        /// <param name="collision">Collision information.</param>
        /// <param name="ctx">Vehicle context at the time of collision.</param>
        /// <param name="state">Current vehicle state (modified by collision listeners).</param>
        public void NotifyCollision(UnityEngine.Collision collision, in VehicleContext ctx, ref VehicleState state)
        {
            for (int i = 0; i < _modules.Count; i++)
            {
                if (_modules[i] is IVehicleCollisionListener listener)
                    listener.OnCollisionEnter(collision, ctx, ref state);
            }
        }
    }
}
