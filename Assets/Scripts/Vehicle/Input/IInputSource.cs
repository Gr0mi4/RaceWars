using Vehicle.Core;

namespace Vehicle.Input
{
    public interface IInputSource
    {
        VehicleInput ReadInput();
    }
}
