# Steering Physics System

## Overview

This system implements physics-based steering with Driver-style realistic handling:
- **Bicycle Model**: Steering effectiveness depends on forward speed
- **Grip Limits**: Yaw rate constrained by tire friction (μ × g)
- **Friction Circle**: Braking/acceleration reduces lateral grip
- **Torque-Based Control**: Realistic weight transfer via Rigidbody torque

## Setup Instructions

### 1. After Unity imports the new scripts:

1. Open `Assets/ScriptableObjects/Vehicles/Steering_Default.asset` in the Inspector
2. Assign `Steering_Physics_Default.asset` to the `Physics Model Spec` field
3. The system will now use physics-based steering

### 2. Tuning Parameters (in Steering_Physics_Default.asset):

- **wheelbase**: Distance between axles (2.8m default for sedan)
- **maxSteerAngle**: Maximum steering angle (32° default)
- **baseMu**: Tire friction coefficient (0.75 default, Driver-style)
- **frictionCircleStrength**: How much brake/throttle reduces lateral grip (0.95 default, strong coupling)
- **yawResponseTime**: Steering response lag (0.11s default, realistic)
- **maxYawAccel**: Maximum yaw acceleration (11 rad/s² default)
- **handbrakeGripMultiplier**: Grip reduction with handbrake (0.2 default)
- **minForwardSpeed**: Minimum speed for steering (0.2 m/s default)

### 3. Debug Logging

Enable `enableDebugLogs` in `Steering_Physics_Default.asset` to see detailed steering calculations in the Console.

## Testing

### Unit Tests

Comprehensive unit tests are available in `Assets/Tests/Editor/Vehicle/Steering/`:

- **PhysicsSteeringModelTests.cs**: Tests bicycle model, grip limits, friction circle, handbrake effects
- **SteeringModuleTests.cs**: Tests SteeringModule with physics-based steering
- **SteeringEdgeCasesTests.cs**: Tests edge cases (zero speed, invalid parameters, extreme values)

Run tests via Unity Test Runner (Window → General → Test Runner).

### Manual Testing Checklist

- [ ] Zero forward speed: Car should not rotate when stationary
- [ ] Low speed coasting: Steering should be minimal, no aggressive spin
- [ ] Braking while steering: Turning effectiveness should reduce significantly
- [ ] Accelerating while steering: Lateral grip should reduce noticeably
- [ ] Handbrake + steering: Controlled slides, not unrealistic spinning
- [ ] Sideways drift: Grip limits respected, no unrealistic rapid yaw
- [ ] High-speed cornering: Grip limits clearly felt
- [ ] Rapid input changes: Realistic response lag, no jitter

## Architecture

- `ISteeringModel`: Interface for steering models
- `PhysicsSteeringModel`: Implementation with bicycle model and grip limits
- `PhysicsSteeringModelSpec`: ScriptableObject with tuning parameters
- `SteeringModule`: Applies steering via torque using physics-based steering models

## Module Order

The pipeline order has been updated to:
1. StateCollector
2. Drive
3. LateralGrip
4. Steering (needs updated state from Drive for friction circle)
5. SpeedLimiter
6. Telemetry

