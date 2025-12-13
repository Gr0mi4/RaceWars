using UnityEngine;
using Vehicle.Specs;

namespace Vehicle.Core
{
    public readonly struct VehicleContext
    {
        public readonly Rigidbody rb;
        public readonly Transform tr;
        public readonly CarSpec spec;
        public readonly float dt;

        public Vector3 Forward => tr.forward;
        public Vector3 Right => tr.right;
        public Vector3 Up => tr.up;

        public VehicleContext(Rigidbody rb, Transform tr, CarSpec spec, float dt)
        {
            this.rb = rb;
            this.tr = tr;
            this.spec = spec;
            this.dt = dt;
        }
    }
}
