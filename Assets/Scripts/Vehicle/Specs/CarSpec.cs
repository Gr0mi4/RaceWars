using UnityEngine;

namespace Vehicle.Specs
{
    [CreateAssetMenu(menuName = "Vehicle/Car Spec", fileName = "CarSpec")]
    public sealed class CarSpec : ScriptableObject
    {
        [Header("Rigidbody Defaults")]
        [Min(1f)] public float minMass = 800f;
        [Min(0f)] public float linearDamping = 0.05f;
        [Min(0f)] public float angularDamping = 0.5f;
        public Vector3 centerOfMass = new Vector3(0f, -0.4f, 0f);

        [Header("Tuning")]
        [Min(0f)] public float motorForce = 12000f;
        [Min(0f)] public float steerStrength = 120f;
        [Min(0f)] public float maxSpeed = 25f;

        [Header("Aerodynamics")]
        [Range(0.1f, 10f)] public float frontArea = 2.0f;
        [Range(0.1f, 2.0f)] public float dragCoefficient = 0.35f;
    }
}
