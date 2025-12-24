using UnityEngine;
using Vehicle.Core;
using Vehicle.Specs;

namespace Vehicle.Systems
{
    /// <summary>
    /// Drive system that applies driving forces to the vehicle based on engine and gearbox simulation.
    /// Calculates RPM from speed, updates gearbox, gets power/torque from engine curves,
    /// and applies force to the wheels. Supports forward and reverse movement.
    /// </summary>
    public sealed class DriveSystem : IVehicleModule
    {
        // Constants for thresholds and default values
        private const float MinThrottleThreshold = 0.01f;
        private const float MinSpeedThreshold = 0.01f;
        private const float MinWheelRadiusThreshold = 0.01f;
        private const float MinGearRatioThreshold = 0.01f;
        private const float DefaultWheelRadius = 0.3f;
        private const float ShiftInputThreshold = 0.5f;
        private const float BrakeInputThreshold = 0.1f;
        private const float LowSpeedThreshold = 1.0f;
        private const float RevLimiterMinThrottle = 0.1f;
        private const float MinForceMultiplier = 0.01f;

        private readonly EngineSystem _engineSystem;
        private GearboxSystem _gearboxSystem;
        private GearboxSpec _cachedGearboxSpec;
        private readonly float _forceMultiplier;
        
        /// <summary>
        /// Whether the clutch is engaged (connected to transmission).
        /// When true, engine torque is transmitted to wheels. When false, no torque is transmitted.
        /// Currently always true (clutch always engaged). In the future, this can be controlled by input.
        /// </summary>
        private bool _clutchEngaged = true;

        /// <summary>
        /// Current engine RPM (crankshaft revolutions per minute) with inertia.
        /// This is smoothed over time to simulate engine inertia.
        /// </summary>
        private float _currentEngineRPM = 0f;

        /// <summary>
        /// Current wheel angular velocity (rad/s) with inertia.
        /// This is smoothed over time to simulate wheel inertia.
        /// </summary>
        private float _currentWheelAngularVelocity = 0f;

        /// <summary>
        /// Initializes a new instance of the DriveSystem.
        /// Engine and gearbox specs are obtained from VehicleContext (CarSpec) at runtime.
        /// </summary>
        /// <param name="forceMultiplier">Optional multiplier for the calculated force. Default is 1.0.</param>
        public DriveSystem(float forceMultiplier = 1.0f)
        {
            _engineSystem = new EngineSystem();
            _gearboxSystem = null;
            _cachedGearboxSpec = null;
            _forceMultiplier = Mathf.Max(MinForceMultiplier, forceMultiplier);
        }

        /// <summary>
        /// Updates the drive system, applying driving forces based on throttle input.
        /// </summary>
        /// <param name="input">Current vehicle input containing throttle value.</param>
        /// <param name="state">Current vehicle state (modified to store RPM and gear).</param>
        /// <param name="ctx">Vehicle context containing rigidbody, transform, and specifications.</param>
        public void Tick(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
        {
            ApplyDrive(input, ref state, ctx);
        }

        /// <summary>
        /// Calculates desired engine RPM based on throttle input and actual engine RPM.
        /// When throttle is applied, desired RPM grows from idleRPM to maxRPM.
        /// When throttle is zero, desired RPM is idle (or actual if moving).
        /// </summary>
        private float CalculateDesiredRPM(float throttle, float actualEngineRPM, EngineSpec engineSpec)
        {
            if (throttle > MinThrottleThreshold)
            {
                return engineSpec.idleRPM + (engineSpec.maxRPM - engineSpec.idleRPM) * throttle;
            }
            else
            {
                return Mathf.Max(engineSpec.idleRPM, actualEngineRPM);
            }
        }

        /// <summary>
        /// Extracts base gear ratio from combined gear ratio (which includes final drive ratio).
        /// </summary>
        private float ExtractBaseGearRatio(float combinedGearRatio, float finalDriveRatio)
        {
            return Mathf.Abs(combinedGearRatio) / finalDriveRatio;
        }

        /// <summary>
        /// Calculates actual engine RPM from current speed, wheel radius, and gear ratio.
        /// </summary>
        private float CalculateActualEngineRPM(float speed, float wheelRadius, float combinedGearRatio, float finalDriveRatio)
        {
            float actualWheelAngularVelocity = _engineSystem.CalculateWheelAngularVelocityFromSpeed(speed, wheelRadius);
            float baseGearRatio = ExtractBaseGearRatio(combinedGearRatio, finalDriveRatio);
            return _engineSystem.CalculateEngineRPMFromWheel(
                actualWheelAngularVelocity,
                baseGearRatio,
                finalDriveRatio
            );
        }

        /// <summary>
        /// Updates engine RPM based on rigid mechanical connection or free rotation.
        /// </summary>
        private void UpdateEngineRPM(bool isRigidlyConnected, float actualEngineRPM, float throttle, EngineSpec engineSpec, float rpmChangeRate)
        {
            if (isRigidlyConnected)
            {
                _currentEngineRPM = Mathf.Lerp(_currentEngineRPM, actualEngineRPM, rpmChangeRate);
            }
            else
            {
                float desiredRPM = CalculateDesiredRPM(throttle, actualEngineRPM, engineSpec);
                _currentEngineRPM = Mathf.Lerp(_currentEngineRPM, desiredRPM, rpmChangeRate);
            }
            
            _currentEngineRPM = Mathf.Clamp(_currentEngineRPM, engineSpec.idleRPM, engineSpec.maxRPM);
        }

        /// <summary>
        /// Calculates maximum speed for current gear based on engine max RPM.
        /// </summary>
        private float CalculateMaxSpeedForGear(float gearRatio, float finalDriveRatio, float wheelRadius, float maxRPM, bool gearEngaged)
        {
            if (!gearEngaged || wheelRadius <= MinWheelRadiusThreshold)
            {
                return 0f;
            }

            float baseGearRatio = ExtractBaseGearRatio(gearRatio, finalDriveRatio);
            float maxWheelAngularVelocity = _engineSystem.CalculateWheelAngularVelocityFromEngine(
                maxRPM,
                baseGearRatio,
                finalDriveRatio
            );
            return _engineSystem.CalculateSpeedFromWheel(maxWheelAngularVelocity, wheelRadius);
        }

        /// <summary>
        /// Validates required specifications and initializes gearbox system if needed.
        /// </summary>
        private bool ValidateAndInitialize(in VehicleContext ctx, in VehicleState state, out EngineSpec engineSpec, out GearboxSpec gearboxSpec, out float wheelRadius)
        {
            engineSpec = ctx.engineSpec ?? ctx.spec?.engineSpec;
            gearboxSpec = ctx.gearboxSpec ?? ctx.spec?.gearboxSpec;

            if (engineSpec == null || gearboxSpec == null)
            {
                if (_gearboxSystem == null)
                {
                    UnityEngine.Debug.LogWarning($"[DriveSystem] Missing required specs. EngineSpec: {(engineSpec != null ? "OK" : "NULL")}, GearboxSpec: {(gearboxSpec != null ? "OK" : "NULL")}. " +
                        $"Make sure EngineSpec and GearboxSpec are assigned in CarSpec.");
                }
                wheelRadius = DefaultWheelRadius;
                return false;
            }

            // Lazy initialization: create GearboxSystem from context if not exists or spec changed
            if (_gearboxSystem == null || _cachedGearboxSpec != gearboxSpec)
            {
                _gearboxSystem = new GearboxSystem(gearboxSpec);
                _cachedGearboxSpec = gearboxSpec;
            }

            wheelRadius = state.wheelRadius > MinWheelRadiusThreshold 
                ? state.wheelRadius 
                : (ctx.wheelSpec != null ? ctx.wheelSpec.wheelRadius : DefaultWheelRadius);

            return true;
        }

        /// <summary>
        /// Handles manual gear shifting input.
        /// </summary>
        private void HandleManualGearShifting(in VehicleInput input, GearboxSpec gearboxSpec)
        {
            if (gearboxSpec.transmissionType == GearboxSpec.TransmissionType.Manual)
            {
                if (input.shiftUp > ShiftInputThreshold)
                {
                    _gearboxSystem.ShiftUp();
                }
                else if (input.shiftDown > ShiftInputThreshold)
                {
                    _gearboxSystem.ShiftDown();
                }
            }
        }

        /// <summary>
        /// Determines throttle input based on user input and vehicle state.
        /// </summary>
        private float DetermineThrottleInput(in VehicleInput input, float forwardSpeed, GearboxSpec gearboxSpec)
        {
            if (input.brake > BrakeInputThreshold && Mathf.Abs(forwardSpeed) < LowSpeedThreshold)
            {
                if (gearboxSpec.transmissionType == GearboxSpec.TransmissionType.Automatic)
                {
                    _gearboxSystem.ShiftToReverse();
                }
                return input.brake;
            }
            
            if (input.throttle > MinThrottleThreshold)
            {
                if (gearboxSpec.transmissionType == GearboxSpec.TransmissionType.Automatic &&
                    _gearboxSystem.CurrentGear == -1 && Mathf.Abs(forwardSpeed) < LowSpeedThreshold)
                {
                    _gearboxSystem.ShiftToNeutral();
                    _gearboxSystem.ShiftUp();
                }
                return input.throttle;
            }

            return 0f;
        }

        /// <summary>
        /// Updates clutch engagement state based on gearbox shifting status.
        /// </summary>
        private void UpdateClutchState()
        {
            _clutchEngaged = !_gearboxSystem.IsShifting;
        }

        /// <summary>
        /// Calculates and updates engine RPM based on current speed, gear, and throttle.
        /// </summary>
        private void CalculateAndUpdateRPM(float speed, float wheelRadius, float gearRatio, float finalDriveRatio, 
            float throttle, EngineSpec engineSpec, bool gearEngaged, float dt)
        {
            if (_currentEngineRPM < engineSpec.idleRPM)
            {
                _currentEngineRPM = engineSpec.idleRPM;
            }

            float actualEngineRPM = CalculateActualEngineRPM(speed, wheelRadius, gearRatio, finalDriveRatio);
            bool isRigidlyConnected = _clutchEngaged && gearEngaged && Mathf.Abs(speed) > MinSpeedThreshold;
            float rpmChangeRate = engineSpec.rpmInertia * dt;
            
            UpdateEngineRPM(isRigidlyConnected, actualEngineRPM, throttle, engineSpec, rpmChangeRate);
        }

        /// <summary>
        /// Calculates engine torque, wheel torque, and applies force to the vehicle.
        /// </summary>
        private void CalculateAndApplyForce(float speed, float wheelRadius, float gearRatio, float finalDriveRatio,
            float throttle, EngineSpec engineSpec, GearboxSpec gearboxSpec, bool gearEngaged, in VehicleContext ctx)
        {
            float actualEngineRPM = CalculateActualEngineRPM(speed, wheelRadius, gearRatio, finalDriveRatio);
            bool engineAtRPMLimit = actualEngineRPM >= engineSpec.maxRPM;
            
            float maxSpeedForGear = CalculateMaxSpeedForGear(gearRatio, finalDriveRatio, wheelRadius, engineSpec.maxRPM, gearEngaged);

            float torqueCalculationRPM = _currentEngineRPM;
            if (Mathf.Abs(speed) < MinSpeedThreshold && throttle > MinThrottleThreshold && _currentEngineRPM < engineSpec.idleRPM)
            {
                torqueCalculationRPM = engineSpec.idleRPM;
            }

            float engineTorque = 0f;
            if (!engineAtRPMLimit)
            {
                engineTorque = _engineSystem.GetTorque(torqueCalculationRPM, throttle, engineSpec, gearEngaged);
            }
            else
            {
                engineTorque = _engineSystem.GetTorque(engineSpec.maxRPM, RevLimiterMinThrottle, engineSpec, gearEngaged);
            }

            float wheelTorque = 0f;
            if (_clutchEngaged && gearEngaged)
            {
                float absGearRatio = Mathf.Abs(gearRatio);
                wheelTorque = engineTorque * absGearRatio * finalDriveRatio;
            }

            float wheelForce = 0f;
            if (wheelRadius > MinWheelRadiusThreshold && wheelTorque > 0f)
            {
                wheelForce = wheelTorque / wheelRadius;
            }

            bool canApplyForce = _clutchEngaged && gearEngaged && wheelForce > 0f;
            if (canApplyForce)
            {
                if (maxSpeedForGear > MinSpeedThreshold && Mathf.Abs(speed) >= maxSpeedForGear)
                {
                    canApplyForce = false;
                }
            }

            if (canApplyForce)
            {
                wheelForce *= _forceMultiplier;
                Vector3 forceDirection = Mathf.Sign(gearRatio) > 0 ? ctx.Forward : -ctx.Forward;
                Vector3 force = forceDirection * Mathf.Abs(wheelForce);
                ctx.rb.AddForce(force, ForceMode.Force);
            }
        }

        /// <summary>
        /// Updates vehicle state with current engine RPM, wheel angular velocity, and gear.
        /// </summary>
        private void UpdateVehicleState(ref VehicleState state, float gearRatio, float finalDriveRatio, EngineSpec engineSpec, float dt)
        {
            float targetWheelAngularVelocity = _engineSystem.CalculateWheelAngularVelocityFromEngine(
                _currentEngineRPM,
                gearRatio,
                finalDriveRatio
            );

            float wheelInertiaRate = engineSpec.rpmInertia * dt;
            _currentWheelAngularVelocity = Mathf.Lerp(_currentWheelAngularVelocity, targetWheelAngularVelocity, wheelInertiaRate);

            state.engineRPM = _currentEngineRPM;
            state.wheelAngularVelocity = _currentWheelAngularVelocity;
            state.currentGear = _gearboxSystem.CurrentGear;
        }

        /// <summary>
        /// Applies driving force to the vehicle's rigidbody based on engine and gearbox simulation.
        /// </summary>
        private void ApplyDrive(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
        {
            if (!ValidateAndInitialize(ctx, state, out EngineSpec engineSpec, out GearboxSpec gearboxSpec, out float wheelRadius))
            {
                return;
            }

            float forwardSpeed = Vector3.Dot(ctx.rb.linearVelocity, ctx.Forward);
            float speed = forwardSpeed;

            HandleManualGearShifting(input, gearboxSpec);

            float throttle = DetermineThrottleInput(input, forwardSpeed, gearboxSpec);

            float gearRatio = _gearboxSystem.GetCurrentGearRatio();
            bool gearEngaged = !_gearboxSystem.IsShifting && Mathf.Abs(gearRatio) > MinGearRatioThreshold;

            UpdateClutchState();

            CalculateAndUpdateRPM(speed, wheelRadius, gearRatio, gearboxSpec.finalDriveRatio, 
                throttle, engineSpec, gearEngaged, ctx.dt);

            _gearboxSystem.Update(_currentEngineRPM, Mathf.Abs(speed), throttle, ctx.dt);

            gearRatio = _gearboxSystem.GetCurrentGearRatio();
            gearEngaged = !_gearboxSystem.IsShifting && Mathf.Abs(gearRatio) > MinGearRatioThreshold;

            CalculateAndUpdateRPM(speed, wheelRadius, gearRatio, gearboxSpec.finalDriveRatio, 
                throttle, engineSpec, gearEngaged, ctx.dt);

            CalculateAndApplyForce(speed, wheelRadius, gearRatio, gearboxSpec.finalDriveRatio,
                throttle, engineSpec, gearboxSpec, gearEngaged, ctx);

            UpdateVehicleState(ref state, gearRatio, gearboxSpec.finalDriveRatio, engineSpec, ctx.dt);
        }
    }
}

