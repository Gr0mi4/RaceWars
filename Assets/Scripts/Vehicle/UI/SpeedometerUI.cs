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
        
        [Header("UI Elements - Categories")]
        [SerializeField] private Text inputCategoryText;
        [SerializeField] private Text motionCategoryText;
        [SerializeField] private Text forcesCategoryText;
        
        [Header("Settings")]
        [SerializeField] private bool showForces = true;
        [SerializeField] private float updateInterval = 0.1f;
        
        private float _nextUpdateTime;
        private float _prevSpeed;
        private float _prevTime;
        private CarSpec _cachedCarSpec;

        private void Start()
        {
            if (vehicleController == null)
            {
                vehicleController = FindObjectOfType<VehicleController>();
            }
            
            // Cache CarSpec using reflection
            CacheCarSpec();
            
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
            
            // Calculate acceleration
            float acceleration = 0f;
            if (_prevTime > 0f && Time.fixedDeltaTime > 0f)
            {
                acceleration = (speed - _prevSpeed) / Time.fixedDeltaTime;
            }
            _prevSpeed = speed;
            _prevTime = Time.time;
            
            // Get input
            var inputProvider = vehicleController.GetComponent<Vehicle.Input.VehicleInputProvider>();
            VehicleInput input = inputProvider != null ? inputProvider.CurrentInput : VehicleInput.Zero;
            
            // Update Input category
            if (throttleText != null)
                throttleText.text = $"  Throttle: {input.throttle:F2}";
            if (brakeText != null)
                brakeText.text = $"  Brake: {input.brake:F2}";
            if (steerText != null)
                steerText.text = $"  Steer: {input.steer:F2}";
            
            // Update Motion category
            if (speedText != null)
                speedText.text = $"  Speed: {speed:F2} m/s";
            if (sideSpeedText != null)
                sideSpeedText.text = $"  Side Speed: {sideSpeed:F2} m/s";
            if (yawRateText != null)
                yawRateText.text = $"  Yaw Rate: {yawRate:F2} rad/s";
            if (accelerationText != null)
                accelerationText.text = $"  Acceleration: {acceleration:F2} m/s²";
            
            // Calculate and update Forces
            if (showForces && _cachedCarSpec != null)
            {
                float motorForce = input.throttle * _cachedCarSpec.motorForce;
                
                float dragForce = 0f;
                const float airDensity = 1.225f;
                if (speed > 0.1f && _cachedCarSpec.frontArea > 0f && _cachedCarSpec.dragCoefficient > 0f)
                {
                    dragForce = 0.5f * airDensity * _cachedCarSpec.dragCoefficient * 
                                _cachedCarSpec.frontArea * speed * speed;
                }
                
                float dampingForce = 0f;
                float linearDamping = RigidbodyCompat.GetLinearDamping(rb);
                if (speed > 0.001f)
                {
                    dampingForce = linearDamping * speed * rb.mass;
                }
                
                float netForce = motorForce - dragForce - dampingForce;
                
                if (motorForceText != null)
                    motorForceText.text = $"  Motor Force: {motorForce:F0} N";
                if (dragForceText != null)
                    dragForceText.text = $"  Drag Force: {dragForce:F0} N";
                if (dampingForceText != null)
                    dampingForceText.text = $"  Damping Force: {dampingForce:F0} N";
                if (netForceText != null)
                    netForceText.text = $"  Net Force: {netForce:F0} N";
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
            if (GetComponent<CanvasScaler>() == null)
            {
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }
            
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
            containerRect.sizeDelta = new Vector2(350, 600);
            
            float yOffset = 0;
            float lineHeight = 25;
            float categorySpacing = 15;
            
            // [INPUT] category
            inputCategoryText = CreateTextElement(container.transform, "InputCategory", "[INPUT]", 
                new Vector2(0, yOffset), 16, Color.white, FontStyle.Bold);
            yOffset -= lineHeight;
            
            throttleText = CreateTextElement(container.transform, "ThrottleText", "  Throttle: 0.00", 
                new Vector2(0, yOffset), 14, Color.black);
            yOffset -= lineHeight;
            
            brakeText = CreateTextElement(container.transform, "BrakeText", "  Brake: 0.00", 
                new Vector2(0, yOffset), 14, Color.black);
            yOffset -= lineHeight;
            
            steerText = CreateTextElement(container.transform, "SteerText", "  Steer: 0.00", 
                new Vector2(0, yOffset), 14, Color.black);
            yOffset -= categorySpacing;
            
            // [MOTION] category
            motionCategoryText = CreateTextElement(container.transform, "MotionCategory", "[MOTION]", 
                new Vector2(0, yOffset), 16, Color.white, FontStyle.Bold);
            yOffset -= lineHeight;
            
            speedText = CreateTextElement(container.transform, "SpeedText", "  Speed: 0.00 m/s", 
                new Vector2(0, yOffset), 14, Color.black);
            yOffset -= lineHeight;
            
            sideSpeedText = CreateTextElement(container.transform, "SideSpeedText", "  Side Speed: 0.00 m/s", 
                new Vector2(0, yOffset), 14, Color.black);
            yOffset -= lineHeight;
            
            yawRateText = CreateTextElement(container.transform, "YawRateText", "  Yaw Rate: 0.00 rad/s", 
                new Vector2(0, yOffset), 14, Color.black);
            yOffset -= lineHeight;
            
            accelerationText = CreateTextElement(container.transform, "AccelerationText", "  Acceleration: 0.00 m/s²", 
                new Vector2(0, yOffset), 14, Color.black);
            yOffset -= categorySpacing;
            
            // [FORCES] category
            if (showForces)
            {
                forcesCategoryText = CreateTextElement(container.transform, "ForcesCategory", "[FORCES]", 
                    new Vector2(0, yOffset), 16, Color.white, FontStyle.Bold);
                yOffset -= lineHeight;
                
                motorForceText = CreateTextElement(container.transform, "MotorForceText", "  Motor Force: 0 N", 
                    new Vector2(0, yOffset), 14, Color.black);
                yOffset -= lineHeight;
                
                dragForceText = CreateTextElement(container.transform, "DragForceText", "  Drag Force: 0 N", 
                    new Vector2(0, yOffset), 14, Color.black);
                yOffset -= lineHeight;
                
                dampingForceText = CreateTextElement(container.transform, "DampingForceText", "  Damping Force: 0 N", 
                    new Vector2(0, yOffset), 14, Color.black);
                yOffset -= lineHeight;
                
                netForceText = CreateTextElement(container.transform, "NetForceText", "  Net Force: 0 N", 
                    new Vector2(0, yOffset), 14, Color.black);
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
            rect.sizeDelta = new Vector2(330, fontSize + 4);
            
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
