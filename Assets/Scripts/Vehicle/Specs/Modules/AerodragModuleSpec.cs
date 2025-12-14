using UnityEngine;
using Vehicle.Core;
using Vehicle.Modules;

namespace Vehicle.Specs.Modules
{
    [CreateAssetMenu(menuName = "Vehicle/Modules/Aerodrag", fileName = "AerodragModuleSpec")]
    public sealed class AerodragModuleSpec : Vehicle.Specs.VehicleModuleSpec
    {
        [Header("Air Properties")]
        [Range(0.5f, 2.0f)]
        [Tooltip("Air density in kg/m³. Standard at sea level is 1.225")]
        public float airDensity = 1.225f;

        [Header("Drag Settings")]
        [Range(0.01f, 1.0f)]
        [Tooltip("Minimum speed (m/s) before drag is applied")]
        public float minSpeed = 0.1f;

        [Header("Note")]
        [Tooltip("frontArea and dragCoefficient are set in CarSpec (vehicle-specific)")]
        public string note = "Aerodynamic parameters (frontArea, dragCoefficient) are defined per vehicle in CarSpec";

        public override IVehicleModule CreateModule()
            => new AerodragModule(airDensity, minSpeed);
    }
}
