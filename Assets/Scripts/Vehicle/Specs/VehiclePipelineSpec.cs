using System.Collections.Generic;
using UnityEngine;
using Vehicle.Core;

namespace Vehicle.Specs
{
    /// <summary>
    /// ScriptableObject that defines the vehicle pipeline configuration.
    /// Contains a list of module specifications that will be instantiated and executed in order.
    /// </summary>
    [CreateAssetMenu(menuName = "Vehicle/Pipeline Spec", fileName = "VehiclePipelineSpec")]
    public sealed class VehiclePipelineSpec : ScriptableObject
    {
        /// <summary>
        /// List of module specifications. Modules are created and executed in this order.
        /// </summary>
        [SerializeField] private List<VehicleModuleSpec> modules = new List<VehicleModuleSpec>();

        /// <summary>
        /// Creates a VehiclePipeline instance from the module specifications.
        /// Modules that fail to create are skipped with a warning.
        /// </summary>
        /// <returns>A new VehiclePipeline with all successfully created modules.</returns>
        public VehiclePipeline CreatePipeline()
        {
            var list = new List<IVehicleModule>(modules.Count);
            for (int i = 0; i < modules.Count; i++)
            {
                var spec = modules[i];
                if (spec == null) continue;
                
                var module = spec.CreateModule();
                if (module == null)
                {
                    Debug.LogWarning($"[VehiclePipelineSpec] Module at index {i} failed to create. Skipping.");
                    continue;
                }
                
                list.Add(module);
            }
            return new VehiclePipeline(list);
        }
    }
}
