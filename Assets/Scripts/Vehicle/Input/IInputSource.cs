using Vehicle.Core;

namespace Vehicle.Input
{
    /// <summary>
    /// Interface for input sources that provide vehicle control input.
    /// Allows different input methods (keyboard, gamepad, AI, network, etc.) to be used interchangeably.
    /// </summary>
    public interface IInputSource
    {
        /// <summary>
        /// Reads the current input state and returns a VehicleInput structure.
        /// </summary>
        /// <returns>Current vehicle input values (throttle, brake, steer, handbrake).</returns>
        VehicleInput ReadInput();
    }
}
