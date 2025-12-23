using UnityEngine;
using UnityEngine.UI;
using Vehicle.Core;
using Vehicle.Specs;
using System.Reflection;

namespace Vehicle.UI
{
    [RequireComponent(typeof(Canvas))]
    public sealed class SpeedometerUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private VehicleController vehicleController;
        
        [Header("UI Elements - Input")]
        [SerializeField] private Text throttleText;
        [SerializeField] private Text brakeText;
        [SerializeField] private Text steerText;
        
        [Header("UI Elements - Motion")]
        [SerializeField] private Text speedText;
        [SerializeField] private Text sideSpeedText;
        [SerializeField] private Text yawRateText;
        [SerializeField] private Text accelerationText;
        
        [Header("UI Elements - Forces")]
        [SerializeField] private Text motorForceText;
        [SerializeField] private Text dragForceText;
        [SerializeField] private Text dampingForceText;
        [SerializeField] private Text netForceText;
        
        [Header("UI Elements - Engine")]
        [SerializeField] private Text rpmText;
        [SerializeField] private Text powerText;
        [SerializeField] private Text torqueText;
        
        [Header("UI Elements - Gearbox")]
        [SerializeField] private Text gearText;
        [SerializeField] private Text gearRatioText;
        [SerializeField] private Text transmissionTypeText;
        
        [Header("UI Elements - Wheels")]
        [SerializeField] private Text wheelRadiusText;
        
        [Header("UI Elements - Categories")]
        [SerializeField] private Text inputCategoryText;
        [SerializeField] private Text motionCategoryText;
        [SerializeField] private Text engineCategoryText;
        [SerializeField] private Text gearboxCategoryText;
        [SerializeField] private Text wheelsCategoryText;
        [SerializeField] private Text forcesCategoryText;
        
        [Header("Settings")]
        [SerializeField] private bool showForces = true;
        [SerializeField] private float updateInterval = 0.1f;
        [SerializeField] private float minScaleFactor = 0.7f;
        
        private float _nextUpdateTime;
        private float _prevSpeed;
        private float _prevTime;
        private CarSpec _cachedCarSpec;
        private EngineSpec _cachedEngineSpec;
        private GearboxSpec _cachedGearboxSpec;

        private void Start()
        {
            if (vehicleController == null)
            {
                vehicleController = FindFirstObjectByType<VehicleController>();
            }
            
            // Cache specs using reflection
            CacheCarSpec();
            CacheEngineAndGearboxSpecs();
            
            CreateUIElements();
        }

        private void CacheCarSpec()
        {
            if (vehicleController == null) return;
            
            // Use reflection to get carSpec (private SerializeField)
            var field = typeof(VehicleController).GetField("carSpec", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                _cachedCarSpec = field.GetValue(vehicleController) as CarSpec;
            }
        }

        private void CacheEngineAndGearboxSpecs()
        {
            if (_cachedCarSpec == null) return;
            
            // Get engineSpec and gearboxSpec from CarSpec
            _cachedEngineSpec = _cachedCarSpec.engineSpec;
            _cachedGearboxSpec = _cachedCarSpec.gearboxSpec;
        }
        
        /// <summary>
        /// Gets a human-readable name for the current gear.
        /// </summary>
        /// <param name="gear">Gear index (-1 = reverse, 0 = neutral, 1+ = forward gears).</param>
        /// <returns>Gear name as string (e.g., "R", "N", "1", "2", etc.).</returns>
        private string GetGearName(int gear)
        {
            return gear switch
            {
                -1 => "R",
                0 => "N",
                _ => gear.ToString()
            };
        }

        private void Update()
        {
            if (vehicleController == null) return;
            if (Time.time < _nextUpdateTime) return;
            
            _nextUpdateTime = Time.time + updateInterval;
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            var rb = vehicleController.GetComponent<Rigidbody>();
            if (rb == null) return;
            
            var transform = vehicleController.transform;
            Vector3 worldVel = RigidbodyCompat.GetVelocity(rb);
            Vector3 localVel = transform.InverseTransformDirection(worldVel);
            
            float speed = worldVel.magnitude;
            float sideSpeed = localVel.x;
            float yawRate = rb.angularVelocity.y;
            
            // Calculate acceleration (change in speed over actual time)
            float acceleration = 0f;
            if (_prevTime > 0f)
            {
                float deltaTime = Time.time - _prevTime;
                if (deltaTime > 0.001f)
                {
                    acceleration = (speed - _prevSpeed) / deltaTime;
                }
            }
            _prevSpeed = speed;
            _prevTime = Time.time;
            
            // Get input
            var inputProvider = vehicleController.GetComponent<Vehicle.Input.VehicleInputProvider>();
            VehicleInput input = inputProvider != null ? inputProvider.CurrentInput : VehicleInput.Zero;
            
            // Calculate expected acceleration from engine forces (for debugging)
            float expectedAcceleration = 0f;
            if (showForces && _cachedCarSpec != null && rb.mass > 0.001f)
            {
                float wheelForce = 0f;
                float dragForce = 0f;
                float dampingForce = 0f;

                // Get state via reflection to calculate engine forces
                var stateField = typeof(VehicleController).GetField("_state", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (stateField != null && _cachedEngineSpec != null && _cachedGearboxSpec != null)
                {
                    var state = stateField.GetValue(vehicleController);
                    var rpmField = typeof(VehicleState).GetField("engineRPM");
                    var gearField = typeof(VehicleState).GetField("currentGear");
                    var wheelRadiusField = typeof(VehicleState).GetField("wheelRadius");
                    
                    if (rpmField != null && gearField != null)
                    {
                        float rpm = (float)rpmField.GetValue(state);
                        int gear = (int)gearField.GetValue(state);
                        float wheelRadiusAccel = 0.3f;
                        if (wheelRadiusField != null)
                        {
                            wheelRadiusAccel = (float)wheelRadiusField.GetValue(state);
                            if (wheelRadiusAccel < 0.01f) wheelRadiusAccel = 0.3f;
                        }

                        if (rpm > 0f)
                        {
                            // Calculate gear ratio
                            float gearRatio = 0f;
                            if (gear == -1)
                            {
                                gearRatio = _cachedGearboxSpec.reverseGearRatio * _cachedGearboxSpec.finalDriveRatio;
                            }
                            else if (gear > 0 && gear <= _cachedGearboxSpec.gearRatios.Length)
                            {
                                int arrayIndex = gear - 1;
                                gearRatio = _cachedGearboxSpec.gearRatios[arrayIndex] * _cachedGearboxSpec.finalDriveRatio;
                            }

                            // Calculate wheel force using EngineModel
                            var engineModel = new Vehicle.Modules.DriveModels.EngineModel();
                            float forwardSpeedLocal = Vector3.Dot(rb.linearVelocity, vehicleController.transform.forward);
                            wheelForce = engineModel.CalculateWheelForce(
                                forwardSpeedLocal,
                                input.throttle,
                                _cachedEngineSpec,
                                rpm,
                                gearRatio,
                                _cachedGearboxSpec.finalDriveRatio,
                                wheelRadiusAccel
                            );
                        }
                    }
                }

                // Calculate drag and damping
                const float airDensity = 1.225f;
                if (speed > 0.1f && _cachedCarSpec.frontArea > 0f && _cachedCarSpec.dragCoefficient > 0f)
                {
                    dragForce = 0.5f * airDensity * _cachedCarSpec.dragCoefficient * 
                                _cachedCarSpec.frontArea * speed * speed;
                }
                float linearDamping = RigidbodyCompat.GetLinearDamping(rb);
                if (speed > 0.001f)
                {
                    dampingForce = linearDamping * speed * rb.mass;
                }

                float netForce = wheelForce - dragForce - dampingForce;
                expectedAcceleration = netForce / rb.mass;
            }
            
            // Update Input category
            if (throttleText != null)
                throttleText.text = $"  Throttle: {input.throttle:F2}";
            if (brakeText != null)
                brakeText.text = $"  Brake: {input.brake:F2}";
            if (steerText != null)
                steerText.text = $"  Steer: {input.steer:F2}";
            
            // Update Motion category
            if (speedText != null)
            {
                float speedKmh = speed * 3.6f;
                speedText.text = $"  Speed: {speed:F2} m/s ({speedKmh:F1} km/h)";
            }
            if (sideSpeedText != null)
            {
                float sideSpeedKmh = sideSpeed * 3.6f;
                sideSpeedText.text = $"  Side Speed: {sideSpeed:F2} m/s ({sideSpeedKmh:F1} km/h)";
            }
            if (yawRateText != null)
                yawRateText.text = $"  Yaw Rate: {yawRate:F2} rad/s";
            if (accelerationText != null)
                accelerationText.text = $"  Acceleration: {acceleration:F2} m/s² (Expected: {expectedAcceleration:F2})";
            
            // Get VehicleState (we'll need to access it through reflection or make it public)
            // For now, we'll calculate RPM and gear from available data if possible
            // In a real implementation, VehicleController should expose state or we use reflection
            
            // Update Engine section
            if (_cachedEngineSpec != null)
            {
                float rpm = 0f; // Will be set from state if available
                float powerHP = 0f;
                float torqueNm = 0f;
                
                // Try to get RPM from state via reflection
                var stateField = typeof(VehicleController).GetField("_state", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (stateField != null)
                {
                    var state = stateField.GetValue(vehicleController);
                    var rpmField = typeof(VehicleState).GetField("engineRPM");
                    if (rpmField != null)
                    {
                        rpm = (float)rpmField.GetValue(state);
                    }
                }
                
                // Calculate power and torque if we have RPM and throttle
                if (rpm > 0f && input.throttle > 0f)
                {
                    float normalizedRPM = Mathf.Clamp01((rpm - _cachedEngineSpec.idleRPM) / 
                        (_cachedEngineSpec.maxRPM - _cachedEngineSpec.idleRPM));
                    
                    float powerMultiplier = _cachedEngineSpec.powerCurve.Evaluate(normalizedRPM);
                    float torqueMultiplier = _cachedEngineSpec.torqueCurve.Evaluate(normalizedRPM);
                    
                    powerHP = _cachedEngineSpec.maxPower * powerMultiplier * input.throttle;
                    torqueNm = _cachedEngineSpec.maxTorque * torqueMultiplier * input.throttle;
                }
                
                if (rpmText != null)
                    rpmText.text = $"  RPM: {rpm:F0} / {_cachedEngineSpec.maxRPM:F0}";
                if (powerText != null)
                    powerText.text = $"  Power: {powerHP:F1} HP ({powerHP * 0.7457f:F1} kW)";
                if (torqueText != null)
                    torqueText.text = $"  Torque: {torqueNm:F1} Nm";
            }
            
            // Update Gearbox section
            if (_cachedGearboxSpec != null)
            {
                int currentGear = 0;
                float currentGearRatio = 0f;
                
                // Try to get gear from state via reflection
                var stateField = typeof(VehicleController).GetField("_state", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (stateField != null)
                {
                    var state = stateField.GetValue(vehicleController);
                    var gearField = typeof(VehicleState).GetField("currentGear");
                    if (gearField != null)
                    {
                        currentGear = (int)gearField.GetValue(state);
                    }
                }
                
                // Calculate gear ratio
                if (currentGear == -1)
                {
                    currentGearRatio = _cachedGearboxSpec.reverseGearRatio * _cachedGearboxSpec.finalDriveRatio;
                }
                else if (currentGear > 0 && currentGear <= _cachedGearboxSpec.gearRatios.Length)
                {
                    int arrayIndex = currentGear - 1;
                    currentGearRatio = _cachedGearboxSpec.gearRatios[arrayIndex] * _cachedGearboxSpec.finalDriveRatio;
                }
                
                string gearName = GetGearName(currentGear);
                string transmissionType = _cachedGearboxSpec.transmissionType.ToString();
                
                if (gearText != null)
                    gearText.text = $"  Gear: {gearName}";
                if (gearRatioText != null)
                    gearRatioText.text = $"  Gear Ratio: {currentGearRatio:F2}";
                if (transmissionTypeText != null)
                    transmissionTypeText.text = $"  Transmission: {transmissionType}";
            }
            
            // Update Wheels section
            float wheelRadius = 0f;
            var stateFieldWheel = typeof(VehicleController).GetField("_state", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (stateFieldWheel != null)
            {
                var state = stateFieldWheel.GetValue(vehicleController);
                var wheelRadiusField = typeof(VehicleState).GetField("wheelRadius");
                if (wheelRadiusField != null)
                {
                    wheelRadius = (float)wheelRadiusField.GetValue(state);
                }
            }
            
            if (wheelRadius > 0.01f && wheelRadiusText != null)
            {
                wheelRadiusText.text = $"  Wheel Radius: {wheelRadius:F3} m";
            }
            
            // Calculate and update Forces (engine-based)
            if (showForces && _cachedCarSpec != null)
            {
                float wheelForce = 0f;
                float dragForce = 0f;
                float dampingForce = 0f;
                
                // Get state via reflection to calculate engine forces
                var stateField = typeof(VehicleController).GetField("_state", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (stateField != null && _cachedEngineSpec != null && _cachedGearboxSpec != null)
                {
                    var state = stateField.GetValue(vehicleController);
                    var rpmField = typeof(VehicleState).GetField("engineRPM");
                    var gearField = typeof(VehicleState).GetField("currentGear");
                    var wheelRadiusField = typeof(VehicleState).GetField("wheelRadius");
                    
                    if (rpmField != null && gearField != null)
                    {
                        float rpm = (float)rpmField.GetValue(state);
                        int gear = (int)gearField.GetValue(state);
                        float wheelRadiusForcesCalc = 0.3f;
                        if (wheelRadiusField != null)
                        {
                            wheelRadiusForcesCalc = (float)wheelRadiusField.GetValue(state);
                            if (wheelRadiusForcesCalc < 0.01f) wheelRadiusForcesCalc = 0.3f;
                        }

                        if (rpm > 0f)
                        {
                            // Calculate gear ratio
                            float gearRatio = 0f;
                            if (gear == -1)
                            {
                                gearRatio = _cachedGearboxSpec.reverseGearRatio * _cachedGearboxSpec.finalDriveRatio;
                            }
                            else if (gear > 0 && gear <= _cachedGearboxSpec.gearRatios.Length)
                            {
                                int arrayIndex = gear - 1;
                                gearRatio = _cachedGearboxSpec.gearRatios[arrayIndex] * _cachedGearboxSpec.finalDriveRatio;
                            }

                            // Calculate wheel force using EngineModel
                            var engineModel = new Vehicle.Modules.DriveModels.EngineModel();
                            float forwardSpeedForcesCalc = Vector3.Dot(rb.linearVelocity, vehicleController.transform.forward);
                            wheelForce = engineModel.CalculateWheelForce(
                                forwardSpeedForcesCalc,
                                input.throttle,
                                _cachedEngineSpec,
                                rpm,
                                gearRatio,
                                _cachedGearboxSpec.finalDriveRatio,
                                wheelRadiusForcesCalc
                            );
                        }
                    }
                }
                
                // Calculate drag and damping
                const float airDensity = 1.225f;
                if (speed > 0.1f && _cachedCarSpec.frontArea > 0f && _cachedCarSpec.dragCoefficient > 0f)
                {
                    dragForce = 0.5f * airDensity * _cachedCarSpec.dragCoefficient * 
                                _cachedCarSpec.frontArea * speed * speed;
                }
                
                float linearDamping = RigidbodyCompat.GetLinearDamping(rb);
                if (speed > 0.001f)
                {
                    dampingForce = linearDamping * speed * rb.mass;
                }
                
                float netForce = wheelForce - dragForce - dampingForce;
                
                // Calculate power in horsepower (1 HP = 745.7 W = 745.7 N·m/s)
                // Power = Force × Speed
                float wheelPowerHP = 0f;
                if (speed > 0.001f && wheelForce > 0f)
                {
                    float wheelPowerW = wheelForce * speed; // Watts
                    wheelPowerHP = wheelPowerW / 745.7f; // Horsepower
                }
                
                float dragPowerHP = 0f;
                if (speed > 0.001f && dragForce > 0f)
                {
                    float dragPowerW = dragForce * speed;
                    dragPowerHP = dragPowerW / 745.7f;
                }
                
                float dampingPowerHP = 0f;
                if (speed > 0.001f && dampingForce > 0f)
                {
                    float dampingPowerW = dampingForce * speed;
                    dampingPowerHP = dampingPowerW / 745.7f;
                }
                
                float netPowerHP = 0f;
                if (speed > 0.001f && netForce > 0f)
                {
                    float netPowerW = netForce * speed;
                    netPowerHP = netPowerW / 745.7f;
                }
                
                if (motorForceText != null)
                    motorForceText.text = $"  Wheel Force: {wheelForce:F0} N ({wheelPowerHP:F1} HP)";
                if (dragForceText != null)
                    dragForceText.text = $"  Drag Force: {dragForce:F0} N ({dragPowerHP:F1} HP)";
                if (dampingForceText != null)
                    dampingForceText.text = $"  Damping Force: {dampingForce:F0} N ({dampingPowerHP:F1} HP)";
                if (netForceText != null)
                {
                    float mass = rb.mass;
                    netForceText.text = $"  Net Force: {netForce:F0} N ({netPowerHP:F1} HP) [Mass: {mass:F1} kg]";
                }
            }
        }

        private void CreateUIElements()
        {
            // Find or create Canvas
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
            }
            
            // Add CanvasScaler for proper scaling
            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }
            
            // Calculate current screen resolution
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            float referenceWidth = 1920f;
            float referenceHeight = 1080f;
            
            // Calculate scale based on width and height
            float scaleX = screenWidth / referenceWidth;
            float scaleY = screenHeight / referenceHeight;
            float scale = Mathf.Min(scaleX, scaleY);
            
            // Apply minimum scale to prevent UI from becoming too small
            scale = Mathf.Max(scale, minScaleFactor);
            
            // Adjust reference resolution to achieve desired scale
            Vector2 adjustedReference = new Vector2(
                referenceWidth / scale,
                referenceHeight / scale
            );
            
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = adjustedReference;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            
            // Add GraphicRaycaster
            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }
            
            // Create container
            GameObject container = new GameObject("TelemetryContainer");
            container.transform.SetParent(transform, false);
            
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 0);
            containerRect.anchorMax = new Vector2(0, 0);
            containerRect.pivot = new Vector2(0, 0);
            containerRect.anchoredPosition = new Vector2(20, 20);
            containerRect.sizeDelta = new Vector2(400, 900);
            
            float yOffset = 0;
            float lineHeight = 30;
            float categorySpacing = 20;
            
            // [INPUT] category
            inputCategoryText = CreateTextElement(container.transform, "InputCategory", "[INPUT]", 
                new Vector2(0, yOffset), 20, Color.white, FontStyle.Bold);
            yOffset -= lineHeight;
            
            throttleText = CreateTextElement(container.transform, "ThrottleText", "  Throttle: 0.00", 
                new Vector2(0, yOffset), 16, Color.black, FontStyle.Bold);
            yOffset -= lineHeight;
            
            brakeText = CreateTextElement(container.transform, "BrakeText", "  Brake: 0.00", 
                new Vector2(0, yOffset), 16, Color.black, FontStyle.Bold);
            yOffset -= lineHeight;
            
            steerText = CreateTextElement(container.transform, "SteerText", "  Steer: 0.00", 
                new Vector2(0, yOffset), 16, Color.black, FontStyle.Bold);
            yOffset -= categorySpacing;
            
            // [MOTION] category
            motionCategoryText = CreateTextElement(container.transform, "MotionCategory", "[MOTION]", 
                new Vector2(0, yOffset), 20, Color.white, FontStyle.Bold);
            yOffset -= lineHeight;
            
            speedText = CreateTextElement(container.transform, "SpeedText", "  Speed: 0.00 m/s", 
                new Vector2(0, yOffset), 16, Color.black, FontStyle.Bold);
            yOffset -= lineHeight;
            
            sideSpeedText = CreateTextElement(container.transform, "SideSpeedText", "  Side Speed: 0.00 m/s", 
                new Vector2(0, yOffset), 16, Color.black, FontStyle.Bold);
            yOffset -= lineHeight;
            
            yawRateText = CreateTextElement(container.transform, "YawRateText", "  Yaw Rate: 0.00 rad/s", 
                new Vector2(0, yOffset), 16, Color.black, FontStyle.Bold);
            yOffset -= lineHeight;
            
            accelerationText = CreateTextElement(container.transform, "AccelerationText", "  Acceleration: 0.00 m/s²", 
                new Vector2(0, yOffset), 16, Color.black, FontStyle.Bold);
            yOffset -= categorySpacing;
            
            // [ENGINE] category
            engineCategoryText = CreateTextElement(container.transform, "EngineCategory", "[ENGINE]", 
                new Vector2(0, yOffset), 20, Color.white, FontStyle.Bold);
            yOffset -= lineHeight;
            
            rpmText = CreateTextElement(container.transform, "RpmText", "  RPM: 0 / 0", 
                new Vector2(0, yOffset), 16, Color.black, FontStyle.Bold);
            yOffset -= lineHeight;
            
            powerText = CreateTextElement(container.transform, "PowerText", "  Power: 0.0 HP", 
                new Vector2(0, yOffset), 16, Color.black, FontStyle.Bold);
            yOffset -= lineHeight;
            
            torqueText = CreateTextElement(container.transform, "TorqueText", "  Torque: 0.0 Nm", 
                new Vector2(0, yOffset), 16, Color.black, FontStyle.Bold);
            yOffset -= categorySpacing;
            
            // [GEARBOX] category
            gearboxCategoryText = CreateTextElement(container.transform, "GearboxCategory", "[GEARBOX]", 
                new Vector2(0, yOffset), 20, Color.white, FontStyle.Bold);
            yOffset -= lineHeight;
            
            gearText = CreateTextElement(container.transform, "GearText", "  Gear: N", 
                new Vector2(0, yOffset), 16, Color.black, FontStyle.Bold);
            yOffset -= lineHeight;
            
            gearRatioText = CreateTextElement(container.transform, "GearRatioText", "  Gear Ratio: 0.00", 
                new Vector2(0, yOffset), 16, Color.black, FontStyle.Bold);
            yOffset -= lineHeight;
            
            transmissionTypeText = CreateTextElement(container.transform, "TransmissionTypeText", "  Transmission: Automatic", 
                new Vector2(0, yOffset), 16, Color.black, FontStyle.Bold);
            yOffset -= categorySpacing;
            
            // [WHEELS] category
            wheelsCategoryText = CreateTextElement(container.transform, "WheelsCategory", "[WHEELS]", 
                new Vector2(0, yOffset), 20, Color.white, FontStyle.Bold);
            yOffset -= lineHeight;
            
            wheelRadiusText = CreateTextElement(container.transform, "WheelRadiusText", "  Wheel Radius: 0.000 m", 
                new Vector2(0, yOffset), 16, Color.black, FontStyle.Bold);
            yOffset -= categorySpacing;
            
            // [FORCES] category
            if (showForces)
            {
                forcesCategoryText = CreateTextElement(container.transform, "ForcesCategory", "[FORCES]", 
                    new Vector2(0, yOffset), 20, Color.white, FontStyle.Bold);
                yOffset -= lineHeight;
                
                motorForceText = CreateTextElement(container.transform, "MotorForceText", "  Motor Force: 0 N", 
                    new Vector2(0, yOffset), 16, Color.black, FontStyle.Bold);
                yOffset -= lineHeight;
                
                dragForceText = CreateTextElement(container.transform, "DragForceText", "  Drag Force: 0 N", 
                    new Vector2(0, yOffset), 16, Color.black, FontStyle.Bold);
                yOffset -= lineHeight;
                
                dampingForceText = CreateTextElement(container.transform, "DampingForceText", "  Damping Force: 0 N", 
                    new Vector2(0, yOffset), 16, Color.black, FontStyle.Bold);
                yOffset -= lineHeight;
                
                netForceText = CreateTextElement(container.transform, "NetForceText", "  Net Force: 0 N", 
                    new Vector2(0, yOffset), 16, Color.black, FontStyle.Bold);
            }
        }

        private Text CreateTextElement(Transform parent, string name, string defaultText, Vector2 position, 
            int fontSize, Color color, FontStyle style = FontStyle.Normal)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);
            
            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(380, fontSize + 6);
            
            Text text = textObj.AddComponent<Text>();
            text.text = defaultText;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = color;
            text.fontStyle = style;
            text.alignment = TextAnchor.UpperLeft;
            
            return text;
        }
    }
}
