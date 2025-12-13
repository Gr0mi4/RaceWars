using Vehicle.Core;
using Vehicle.Modules.DriveModels;

namespace Vehicle.Modules
{
    public sealed class DriveModule : IVehicleModule
    {
        private readonly IDriveModel _driveModel;

        public DriveModule(IDriveModel driveModel)
        {
            _driveModel = driveModel;
        }

        public void Tick(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
        {
            _driveModel?.ApplyDrive(input, state, ctx);
        }
    }
}
