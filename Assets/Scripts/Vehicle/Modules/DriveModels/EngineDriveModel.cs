using UnityEngine;
using Vehicle.Core;
using Vehicle.Specs;

namespace Vehicle.Modules.DriveModels
{
    /// <summary>
    /// Drive model that uses realistic engine and gearbox simulation.
    /// Calculates engine RPM from speed and gear ratio, uses power/torque curves,
    /// and applies forces based on engine characteristics. Supports reverse gear.
    /// </summary>
    public sealed class EngineDriveModel : IDriveModel
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

        private readonly EngineModel _engineModel;
        private GearboxModel _gearboxModel;
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
        /// Initializes a new instance of the EngineDriveModel.
        /// Engine and gearbox specs are obtained from VehicleContext (CarSpec) at runtime.
        /// </summary>
        /// <param name="forceMultiplier">Optional multiplier for the calculated force. Default is 1.0.</param>
        public EngineDriveModel(float forceMultiplier = 1.0f)
        {
            _engineModel = new EngineModel();
            _gearboxModel = null;
            _cachedGearboxSpec = null;
            _forceMultiplier = Mathf.Max(MinForceMultiplier, forceMultiplier);
        }

        /// <summary>
        /// Calculates desired engine RPM based on throttle input and actual engine RPM.
        /// When throttle is applied, desired RPM grows from idleRPM to maxRPM.
        /// When throttle is zero, desired RPM is idle (or actual if moving).
        /// </summary>
        /// <param name="throttle">Throttle input (0-1).</param>
        /// <param name="actualEngineRPM">Actual engine RPM calculated from current speed.</param>
        /// <param name="engineSpec">Engine specification containing idle and max RPM.</param>
        /// <returns>Desired engine RPM.</returns>
        private float CalculateDesiredRPM(float throttle, float actualEngineRPM, EngineSpec engineSpec)
        {
            if (throttle > MinThrottleThreshold)
            {
                // When throttle is applied, desired RPM grows from idleRPM to maxRPM
                return engineSpec.idleRPM + (engineSpec.maxRPM - engineSpec.idleRPM) * throttle;
            }
            else
            {
                // No throttle: desired RPM is idle (or actual if moving)
                return Mathf.Max(engineSpec.idleRPM, actualEngineRPM);
            }
        }

        /// <summary>
        /// Extracts base gear ratio from combined gear ratio (which includes final drive ratio).
        /// </summary>
        /// <param name="combinedGearRatio">Gear ratio that already includes final drive ratio.</param>
        /// <param name="finalDriveRatio">Final drive ratio to extract.</param>
        /// <returns>Base gear ratio without final drive ratio.</returns>
        private float ExtractBaseGearRatio(float combinedGearRatio, float finalDriveRatio)
        {
            return Mathf.Abs(combinedGearRatio) / finalDriveRatio;
        }

        /// <summary>
        /// Calculates actual engine RPM from current speed, wheel radius, and gear ratio.
        /// This represents what the engine RPM would be if directly connected to wheels at current speed.
        /// </summary>
        /// <param name="speed">Current vehicle speed in m/s.</param>
        /// <param name="wheelRadius">Wheel radius in meters.</param>
        /// <param name="combinedGearRatio">Gear ratio that already includes final drive ratio.</param>
        /// <param name="finalDriveRatio">Final drive ratio.</param>
        /// <returns>Actual engine RPM based on current speed.</returns>
        private float CalculateActualEngineRPM(float speed, float wheelRadius, float combinedGearRatio, float finalDriveRatio)
        {
            float actualWheelAngularVelocity = _engineModel.CalculateWheelAngularVelocityFromSpeed(speed, wheelRadius);
            float baseGearRatio = ExtractBaseGearRatio(combinedGearRatio, finalDriveRatio);
            return _engineModel.CalculateEngineRPMFromWheel(
                actualWheelAngularVelocity,
                baseGearRatio,
                finalDriveRatio
            );
        }

        /// <summary>
        /// Updates engine RPM based on rigid mechanical connection or free rotation.
        /// When rigidly connected, engine RPM follows wheel RPM. Otherwise, throttle controls RPM.
        /// </summary>
        /// <param name="isRigidlyConnected">Whether engine is rigidly connected to wheels.</param>
        /// <param name="actualEngineRPM">Actual engine RPM from current speed.</param>
        /// <param name="throttle">Throttle input (0-1).</param>
        /// <param name="engineSpec">Engine specification.</param>
        /// <param name="rpmChangeRate">Rate of RPM change (inertia factor).</param>
        private void UpdateEngineRPM(bool isRigidlyConnected, float actualEngineRPM, float throttle, EngineSpec engineSpec, float rpmChangeRate)
        {
            if (isRigidlyConnected)
            {
                // RIGID MECHANICAL CONNECTION: Engine RPM is directly linked to wheel RPM through gearbox
                // Cannot have engineRPM different from actualEngineRPM
                // Apply inertia for smooth transition
                _currentEngineRPM = Mathf.Lerp(_currentEngineRPM, actualEngineRPM, rpmChangeRate);
            }
            else
            {
                // FREE ROTATION: Clutch disengaged OR gear not engaged OR standing still
                // Engine can spin freely, throttle controls RPM
                float desiredRPM = CalculateDesiredRPM(throttle, actualEngineRPM, engineSpec);
                _currentEngineRPM = Mathf.Lerp(_currentEngineRPM, desiredRPM, rpmChangeRate);
            }
            
            // Clamp RPM to valid range
            _currentEngineRPM = Mathf.Clamp(_currentEngineRPM, engineSpec.idleRPM, engineSpec.maxRPM);
        }

        /// <summary>
        /// Calculates maximum speed for current gear based on engine max RPM.
        /// </summary>
        /// <param name="gearRatio">Current gear ratio (includes final drive ratio).</param>
        /// <param name="finalDriveRatio">Final drive ratio.</param>
        /// <param name="wheelRadius">Wheel radius in meters.</param>
        /// <param name="maxRPM">Maximum engine RPM.</param>
        /// <param name="gearEngaged">Whether gear is engaged.</param>
        /// <returns>Maximum speed in m/s for current gear. Returns 0 if gear not engaged or invalid inputs.</returns>
        private float CalculateMaxSpeedForGear(float gearRatio, float finalDriveRatio, float wheelRadius, float maxRPM, bool gearEngaged)
        {
            if (!gearEngaged || wheelRadius <= MinWheelRadiusThreshold)
            {
                return 0f;
            }

            float baseGearRatio = ExtractBaseGearRatio(gearRatio, finalDriveRatio);
            float maxWheelAngularVelocity = _engineModel.CalculateWheelAngularVelocityFromEngine(
                maxRPM,
                baseGearRatio,
                finalDriveRatio
            );
            return _engineModel.CalculateSpeedFromWheel(maxWheelAngularVelocity, wheelRadius);
        }

        /// <summary>
        /// Validates required specifications and initializes gearbox model if needed.
        /// </summary>
        /// <param name="ctx">Vehicle context containing specifications.</param>
        /// <param name="state">Vehicle state containing wheel radius.</param>
        /// <param name="engineSpec">Output parameter for engine specification.</param>
        /// <param name="gearboxSpec">Output parameter for gearbox specification.</param>
        /// <param name="wheelRadius">Output parameter for wheel radius.</param>
        /// <returns>True if all required specs are valid, false otherwise.</returns>
        private bool ValidateAndInitialize(in VehicleContext ctx, in VehicleState state, out EngineSpec engineSpec, out GearboxSpec gearboxSpec, out float wheelRadius)
        {
            engineSpec = ctx.engineSpec ?? ctx.spec?.engineSpec;
            gearboxSpec = ctx.gearboxSpec ?? ctx.spec?.gearboxSpec;

            if (engineSpec == null || gearboxSpec == null)
            {
                // Log warning only once to avoid spam
                if (_gearboxModel == null)
                {
                    Debug.LogWarning($"[EngineDriveModel] Missing required specs. EngineSpec: {(engineSpec != null ? "OK" : "NULL")}, GearboxSpec: {(gearboxSpec != null ? "OK" : "NULL")}. " +
                        $"Make sure EngineSpec and GearboxSpec are assigned in CarSpec.");
                }
                wheelRadius = DefaultWheelRadius;
                return false;
            }

            // Lazy initialization: create GearboxModel from context if not exists or spec changed
            if (_gearboxModel == null || _cachedGearboxSpec != gearboxSpec)
            {
                _gearboxModel = new GearboxModel(gearboxSpec);
                _cachedGearboxSpec = gearboxSpec;
            }

            // Get wheel radius from state (set by WheelModule) or use default
            wheelRadius = state.wheelRadius > MinWheelRadiusThreshold 
                ? state.wheelRadius 
                : (ctx.wheelSpec != null ? ctx.wheelSpec.wheelRadius : DefaultWheelRadius);

            return true;
        }

        /// <summary>
        /// Handles manual gear shifting input.
        /// </summary>
        /// <param name="input">Vehicle input containing shift commands.</param>
        /// <param name="gearboxSpec">Gearbox specification.</param>
        private void HandleManualGearShifting(in VehicleInput input, GearboxSpec gearboxSpec)
        {
            if (gearboxSpec.transmissionType == GearboxSpec.TransmissionType.Manual)
            {
                if (input.shiftUp > ShiftInputThreshold)
                {
                    _gearboxModel.ShiftUp();
                }
                else if (input.shiftDown > ShiftInputThreshold)
                {
                    _gearboxModel.ShiftDown();
                }
            }
        }

        /// <summary>
        /// Determines throttle input based on user input and vehicle state.
        /// Handles forward throttle, reverse (brake), and coasting scenarios.
        /// </summary>
        /// <param name="input">Vehicle input containing throttle and brake values.</param>
        /// <param name="forwardSpeed">Current forward speed in m/s.</param>
        /// <param name="gearboxSpec">Gearbox specification.</param>
        /// <returns>Throttle value (0-1) for engine control.</returns>
        private float DetermineThrottleInput(in VehicleInput input, float forwardSpeed, GearboxSpec gearboxSpec)
        {
            // Handle reverse: if brake is pressed and speed is very low, shift to reverse
            if (input.brake > BrakeInputThreshold && Mathf.Abs(forwardSpeed) < LowSpeedThreshold)
            {
                // User wants reverse
                // Only auto-shift to reverse if transmission is automatic
                if (gearboxSpec.transmissionType == GearboxSpec.TransmissionType.Automatic)
                {
                    _gearboxModel.ShiftToReverse();
                }
                return input.brake; // Use brake input as throttle for reverse
            }
            
            if (input.throttle > MinThrottleThreshold)
            {
                // User wants forward
                // If we're in reverse and user presses throttle, shift to 1st gear (only for automatic)
                if (gearboxSpec.transmissionType == GearboxSpec.TransmissionType.Automatic &&
                    _gearboxModel.CurrentGear == -1 && Mathf.Abs(forwardSpeed) < LowSpeedThreshold)
                {
                    _gearboxModel.ShiftToNeutral();
                    _gearboxModel.ShiftUp(); // Go to 1st gear
                }
                return input.throttle;
            }

            // No input - coast
            return 0f;
        }

        /// <summary>
        /// Updates clutch engagement state based on gearbox shifting status.
        /// </summary>
        private void UpdateClutchState()
        {
            // During gear shifting, disengage clutch (no power transmission)
            // This simulates real car behavior where clutch is disengaged during shifting
            // The shiftTime from GearboxSpec determines how long the clutch stays disengaged
            _clutchEngaged = !_gearboxModel.IsShifting;
        }

        /// <summary>
        /// Calculates and updates engine RPM based on current speed, gear, and throttle.
        /// Handles rigid mechanical connection when clutch is engaged, or free rotation when disengaged.
        /// </summary>
        /// <param name="speed">Current vehicle speed in m/s.</param>
        /// <param name="wheelRadius">Wheel radius in meters.</param>
        /// <param name="gearRatio">Current gear ratio (includes final drive ratio).</param>
        /// <param name="finalDriveRatio">Final drive ratio.</param>
        /// <param name="throttle">Throttle input (0-1).</param>
        /// <param name="engineSpec">Engine specification.</param>
        /// <param name="gearEngaged">Whether gear is engaged.</param>
        /// <param name="dt">Delta time for inertia calculations.</param>
        private void CalculateAndUpdateRPM(float speed, float wheelRadius, float gearRatio, float finalDriveRatio, 
            float throttle, EngineSpec engineSpec, bool gearEngaged, float dt)
        {
            // Initialize current engine RPM if not set
            if (_currentEngineRPM < engineSpec.idleRPM)
            {
                _currentEngineRPM = engineSpec.idleRPM;
            }

            // Calculate actual engine RPM from current speed (feedback from physics)
            // This is what the engine RPM would be if it was directly connected to wheels at current speed
            float actualEngineRPM = CalculateActualEngineRPM(speed, wheelRadius, gearRatio, finalDriveRatio);

            // Apply rigid mechanical connection or free rotation
            // CRITICAL: When clutch is engaged AND gear is engaged AND speed > threshold, engine RPM is mechanically locked to wheel RPM
            bool isRigidlyConnected = _clutchEngaged && gearEngaged && Mathf.Abs(speed) > MinSpeedThreshold;
            float rpmChangeRate = engineSpec.rpmInertia * dt;
            
            UpdateEngineRPM(isRigidlyConnected, actualEngineRPM, throttle, engineSpec, rpmChangeRate);
        }

        /// <summary>
        /// Calculates engine torque, wheel torque, and applies force to the vehicle.
        /// Handles rev limiter, gear-limited maximum speed, and force application.
        /// </summary>
        /// <param name="speed">Current vehicle speed in m/s.</param>
        /// <param name="wheelRadius">Wheel radius in meters.</param>
        /// <param name="gearRatio">Current gear ratio (includes final drive ratio).</param>
        /// <param name="finalDriveRatio">Final drive ratio.</param>
        /// <param name="throttle">Throttle input (0-1).</param>
        /// <param name="engineSpec">Engine specification.</param>
        /// <param name="gearboxSpec">Gearbox specification.</param>
        /// <param name="gearEngaged">Whether gear is engaged.</param>
        /// <param name="ctx">Vehicle context containing rigidbody and transform.</param>
        private void CalculateAndApplyForce(float speed, float wheelRadius, float gearRatio, float finalDriveRatio,
            float throttle, EngineSpec engineSpec, GearboxSpec gearboxSpec, bool gearEngaged, in VehicleContext ctx)
        {
            // Check if engine is at RPM limit (rev limiter)
            float actualEngineRPM = CalculateActualEngineRPM(speed, wheelRadius, gearRatio, finalDriveRatio);
            bool engineAtRPMLimit = actualEngineRPM >= engineSpec.maxRPM;
            
            // Calculate maximum speed for current gear
            float maxSpeedForGear = CalculateMaxSpeedForGear(gearRatio, finalDriveRatio, wheelRadius, engineSpec.maxRPM, gearEngaged);

            // Calculate engine torque from current engine RPM
            // Special case: if standing still and throttle applied, use idleRPM for torque calculation
            float torqueCalculationRPM = _currentEngineRPM;
            if (Mathf.Abs(speed) < MinSpeedThreshold && throttle > MinThrottleThreshold && _currentEngineRPM < engineSpec.idleRPM)
            {
                torqueCalculationRPM = engineSpec.idleRPM;
            }

            // Calculate engine torque (with rev limiter handling)
            float engineTorque = 0f;
            if (!engineAtRPMLimit)
            {
                engineTorque = _engineModel.GetTorque(torqueCalculationRPM, throttle, engineSpec, gearEngaged);
            }
            else
            {
                // At RPM limit: engine produces minimal torque (just enough to maintain, not accelerate)
                engineTorque = _engineModel.GetTorque(engineSpec.maxRPM, RevLimiterMinThrottle, engineSpec, gearEngaged);
            }

            // Calculate wheel torque from engine torque through gearbox
            float wheelTorque = 0f;
            if (_clutchEngaged && gearEngaged)
            {
                float absGearRatio = Mathf.Abs(gearRatio);
                wheelTorque = engineTorque * absGearRatio * finalDriveRatio;
            }

            // Calculate wheel force from wheel torque
            float wheelForce = 0f;
            if (wheelRadius > MinWheelRadiusThreshold && wheelTorque > 0f)
            {
                wheelForce = wheelTorque / wheelRadius;
            }

            // Apply force to vehicle (if clutch engaged and gear engaged and not at max speed)
            bool canApplyForce = _clutchEngaged && gearEngaged && wheelForce > 0f;
            if (canApplyForce)
            {
                // Check if current speed exceeds maximum for this gear
                if (maxSpeedForGear > MinSpeedThreshold && Mathf.Abs(speed) >= maxSpeedForGear)
                {
                    // At maximum speed for gear - engine is at RPM limit
                    canApplyForce = false;
                }
            }

            if (canApplyForce)
            {
                // Apply force multiplier
                wheelForce *= _forceMultiplier;

                // Apply force in the forward direction (gear ratio sign determines direction)
                Vector3 forceDirection = Mathf.Sign(gearRatio) > 0 ? ctx.Forward : -ctx.Forward;
                Vector3 force = forceDirection * Mathf.Abs(wheelForce);

                ctx.rb.AddForce(force, ForceMode.Force);
            }
        }

        /// <summary>
        /// Updates vehicle state with current engine RPM, wheel angular velocity, and gear.
        /// </summary>
        /// <param name="state">Vehicle state to update.</param>
        /// <param name="gearRatio">Current gear ratio (includes final drive ratio).</param>
        /// <param name="finalDriveRatio">Final drive ratio.</param>
        /// <param name="engineSpec">Engine specification.</param>
        /// <param name="dt">Delta time for inertia calculations.</param>
        private void UpdateVehicleState(ref VehicleState state, float gearRatio, float finalDriveRatio, EngineSpec engineSpec, float dt)
        {
            // Calculate wheel angular velocity from engine RPM (for state update)
            float targetWheelAngularVelocity = _engineModel.CalculateWheelAngularVelocityFromEngine(
                _currentEngineRPM,
                gearRatio,
                finalDriveRatio
            );

            // Apply wheel angular velocity inertia (smooth transition)
            float wheelInertiaRate = engineSpec.rpmInertia * dt;
            _currentWheelAngularVelocity = Mathf.Lerp(_currentWheelAngularVelocity, targetWheelAngularVelocity, wheelInertiaRate);

            // Update state
            state.engineRPM = _currentEngineRPM;
            state.wheelAngularVelocity = _currentWheelAngularVelocity;
            state.currentGear = _gearboxModel.CurrentGear;
        }

        /// <summary>
        /// Applies driving force to the vehicle's rigidbody based on engine and gearbox simulation.
        /// Calculates RPM from speed, updates gearbox, gets power/torque from engine curves,
        /// and applies force to the wheels. Supports forward and reverse movement.
        /// </summary>
        /// <param name="input">Current vehicle input containing throttle and brake values.</param>
        /// <param name="state">Current vehicle state (modified to store RPM and gear).</param>
        /// <param name="ctx">Vehicle context containing rigidbody, transform, and specifications.</param>
        public void ApplyDrive(in VehicleInput input, ref VehicleState state, in VehicleContext ctx)
        {
            // Validate specs and initialize gearbox model
            if (!ValidateAndInitialize(ctx, state, out EngineSpec engineSpec, out GearboxSpec gearboxSpec, out float wheelRadius))
            {
                return;
            }

            // Calculate forward speed (signed: positive = forward, negative = backward)
            float forwardSpeed = Vector3.Dot(ctx.rb.linearVelocity, ctx.Forward);
            float speed = forwardSpeed;

            // Handle manual gear shifting
            HandleManualGearShifting(input, gearboxSpec);

            // Determine throttle input based on user input
            float throttle = DetermineThrottleInput(input, forwardSpeed, gearboxSpec);

            // Get current gear ratio and check engagement
            float gearRatio = _gearboxModel.GetCurrentGearRatio();
            bool gearEngaged = !_gearboxModel.IsShifting && Mathf.Abs(gearRatio) > MinGearRatioThreshold;

            // Update clutch state based on gearbox shifting status
            UpdateClutchState();

            // Calculate and update engine RPM (first pass)
            CalculateAndUpdateRPM(speed, wheelRadius, gearRatio, gearboxSpec.finalDriveRatio, 
                throttle, engineSpec, gearEngaged, ctx.dt);

            // Update gearbox (use current RPM for shifting decisions)
            _gearboxModel.Update(_currentEngineRPM, Mathf.Abs(speed), throttle, ctx.dt);

            // Get updated gear ratio after potential shift
            gearRatio = _gearboxModel.GetCurrentGearRatio();
            gearEngaged = !_gearboxModel.IsShifting && Mathf.Abs(gearRatio) > MinGearRatioThreshold;

            // Recalculate RPM with updated gear ratio (second pass after potential shift)
            CalculateAndUpdateRPM(speed, wheelRadius, gearRatio, gearboxSpec.finalDriveRatio, 
                throttle, engineSpec, gearEngaged, ctx.dt);

            // Calculate and apply force to vehicle
            CalculateAndApplyForce(speed, wheelRadius, gearRatio, gearboxSpec.finalDriveRatio,
                throttle, engineSpec, gearboxSpec, gearEngaged, ctx);

            // Update vehicle state
            UpdateVehicleState(ref state, gearRatio, gearboxSpec.finalDriveRatio, engineSpec, ctx.dt);
        }

    }
}

