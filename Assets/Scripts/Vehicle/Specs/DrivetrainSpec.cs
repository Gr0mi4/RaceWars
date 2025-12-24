using UnityEngine;

namespace Vehicle.Specs
{
    /// <summary>
    /// ScriptableObject containing drivetrain configuration parameters.
    /// This is a placeholder for future drivetrain system implementation.
    /// Future parameters will include:
    /// - Drive type (FWD/RWD/AWD)
    /// - Torque distribution (for AWD)
    /// - Differential type (open/LSD/locked)
    /// - Final drive ratio (if separate from GearboxSpec)
    /// </summary>
    [CreateAssetMenu(menuName = "Vehicle/Drivetrain Spec", fileName = "DrivetrainSpec")]
    public sealed class DrivetrainSpec : ScriptableObject
    {
        // TODO: Add drivetrain parameters when implementing drivetrain system
    }
}

