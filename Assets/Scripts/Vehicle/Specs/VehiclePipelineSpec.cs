using System.Collections.Generic;
using UnityEngine;
using Vehicle.Core;

namespace Vehicle.Specs
{
    [CreateAssetMenu(menuName = "Vehicle/Pipeline Spec", fileName = "VehiclePipelineSpec")]
    public sealed class VehiclePipelineSpec : ScriptableObject
    {
        [SerializeField] private List<VehicleModuleSpec> modules = new List<VehicleModuleSpec>();

        public VehiclePipeline CreatePipeline()
        {
            var list = new List<IVehicleModule>(modules.Count);
            for (int i = 0; i < modules.Count; i++)
            {
                var spec = modules[i];
                if (spec == null) continue;
                list.Add(spec.CreateModule());
            }
            return new VehiclePipeline(list);
        }
    }
}
