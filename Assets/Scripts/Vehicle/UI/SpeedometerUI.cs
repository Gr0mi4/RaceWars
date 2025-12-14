using UnityEngine;
using UnityEngine.UI;
using Vehicle.Core;

namespace Vehicle.UI
{
    [RequireComponent(typeof(Canvas))]
    public sealed class SpeedometerUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private VehicleController vehicleController;
        
        [Header("UI Elements")]
        [SerializeField] private Text speedText;
        [SerializeField] private Text forwardSpeedText;
        [SerializeField] private Text lateralSpeedText;
        [SerializeField] private Text verticalSpeedText;
        [SerializeField] private Text yawRateText;
        [SerializeField] private Text throttleText;
        [SerializeField] private Text brakeText;
        [SerializeField] private Text steerText;
        
        [Header("Settings")]
        [SerializeField] private bool showSpeed = true;
        [SerializeField] private bool showForwardSpeed = true;
        [SerializeField] private bool showLateralSpeed = true;
        [SerializeField] private bool showVerticalSpeed = false;
        [SerializeField] private bool showYawRate = true;
        [SerializeField] private bool showInput = true;
        [SerializeField] private float updateInterval = 0.1f;
        
        private float _nextUpdateTime;
        private VehicleState _lastState;
        private VehicleInput _lastInput;

        private void Start()
        {
            // Auto-find VehicleController if not assigned
            if (vehicleController == null)
            {
                vehicleController = FindObjectOfType<VehicleController>();
            }
            
            // Create UI elements if not assigned
            CreateUIElements();
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
            // Get state from VehicleController using reflection or public access
            // Since VehicleState is private, we'll need to access it through VehicleController
            // For now, we'll read from Rigidbody directly and calculate
            
            var rb = vehicleController.GetComponent<Rigidbody>();
            if (rb == null) return;
            
            var transform = vehicleController.transform;
            Vector3 worldVel = RigidbodyCompat.GetVelocity(rb);
            Vector3 localVel = transform.InverseTransformDirection(worldVel);
            
            float speed = worldVel.magnitude;
            float forwardSpeed = localVel.z;
            float lateralSpeed = localVel.x;
            float verticalSpeed = localVel.y;
            float yawRate = rb.angularVelocity.y;
            
            // Get input from VehicleInputProvider
            var inputProvider = vehicleController.GetComponent<Vehicle.Input.VehicleInputProvider>();
            VehicleInput input = inputProvider != null ? inputProvider.CurrentInput : VehicleInput.Zero;
            
            // Update UI
            if (showSpeed && speedText != null)
                speedText.text = $"Speed: {speed:F2} m/s ({speed * 3.6f:F1} km/h)";
            
            if (showForwardSpeed && forwardSpeedText != null)
                forwardSpeedText.text = $"Forward: {forwardSpeed:F2} m/s";
            
            if (showLateralSpeed && lateralSpeedText != null)
                lateralSpeedText.text = $"Lateral: {lateralSpeed:F2} m/s";
            
            if (showVerticalSpeed && verticalSpeedText != null)
                verticalSpeedText.text = $"Vertical: {verticalSpeed:F2} m/s";
            
            if (showYawRate && yawRateText != null)
                yawRateText.text = $"Yaw Rate: {yawRate:F2} rad/s";
            
            if (showInput)
            {
                if (throttleText != null)
                    throttleText.text = $"Throttle: {input.throttle:F2}";
                if (brakeText != null)
                    brakeText.text = $"Brake: {input.brake:F2}";
                if (steerText != null)
                    steerText.text = $"Steer: {input.steer:F2}";
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
            GameObject container = new GameObject("SpeedometerContainer");
            container.transform.SetParent(transform, false);
            
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 0);
            containerRect.anchorMax = new Vector2(0, 0);
            containerRect.pivot = new Vector2(0, 0);
            containerRect.anchoredPosition = new Vector2(20, 20);
            containerRect.sizeDelta = new Vector2(300, 400);
            
            // Create Text elements
            float yOffset = 0;
            float lineHeight = 30;
            
            if (showSpeed)
            {
                speedText = CreateTextElement(container.transform, "SpeedText", "Speed: 0.00 m/s (0.0 km/h)", 
                    new Vector2(0, yOffset), 18, Color.white, FontStyle.Bold);
                yOffset -= lineHeight;
            }
            
            if (showForwardSpeed)
            {
                forwardSpeedText = CreateTextElement(container.transform, "ForwardSpeedText", "Forward: 0.00 m/s", 
                    new Vector2(0, yOffset), 14, Color.green);
                yOffset -= lineHeight;
            }
            
            if (showLateralSpeed)
            {
                lateralSpeedText = CreateTextElement(container.transform, "LateralSpeedText", "Lateral: 0.00 m/s", 
                    new Vector2(0, yOffset), 14, Color.yellow);
                yOffset -= lineHeight;
            }
            
            if (showVerticalSpeed)
            {
                verticalSpeedText = CreateTextElement(container.transform, "VerticalSpeedText", "Vertical: 0.00 m/s", 
                    new Vector2(0, yOffset), 14, Color.cyan);
                yOffset -= lineHeight;
            }
            
            if (showYawRate)
            {
                yawRateText = CreateTextElement(container.transform, "YawRateText", "Yaw Rate: 0.00 rad/s", 
                    new Vector2(0, yOffset), 14, Color.magenta);
                yOffset -= lineHeight;
            }
            
            if (showInput)
            {
                yOffset -= 10; // Spacer
                throttleText = CreateTextElement(container.transform, "ThrottleText", "Throttle: 0.00", 
                    new Vector2(0, yOffset), 14, new Color(0.2f, 1f, 0.2f));
                yOffset -= lineHeight;
                
                brakeText = CreateTextElement(container.transform, "BrakeText", "Brake: 0.00", 
                    new Vector2(0, yOffset), 14, new Color(1f, 0.2f, 0.2f));
                yOffset -= lineHeight;
                
                steerText = CreateTextElement(container.transform, "SteerText", "Steer: 0.00", 
                    new Vector2(0, yOffset), 14, new Color(0.2f, 0.2f, 1f));
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
            rect.sizeDelta = new Vector2(280, fontSize + 4);
            
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

