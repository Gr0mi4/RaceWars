using Vehicle.Core;

namespace Vehicle.Modules.DriveModels
{
    public interface IDriveModel
    {
        void ApplyDrive(in VehicleInput input, in VehicleState state, in VehicleContext ctx);
    }
}
