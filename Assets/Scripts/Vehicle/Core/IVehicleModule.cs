namespace Vehicle.Core
{
    public interface IVehicleModule
    {
        void Tick(in VehicleInput input, ref VehicleState state, in VehicleContext ctx);
    }

    // Optional hook for modules that want collision events (e.g., damage system later).
    public interface IVehicleCollisionListener
    {
        void OnCollisionEnter(UnityEngine.Collision collision, in VehicleContext ctx, ref VehicleState state);
    }
}
