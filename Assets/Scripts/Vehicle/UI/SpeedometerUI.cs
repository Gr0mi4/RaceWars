using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Vehicle.Core;
using Vehicle.Specs;
using Vehicle.UI.Telemetry;

namespace Vehicle.UI
{
    [RequireComponent(typeof(Canvas))]
    public sealed class SpeedometerUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private VehicleController vehicleController;

        [Header("Module Visibility Settings")]
        [SerializeField] private bool showInputModule = true;
        [SerializeField] private bool showMotionModule = true;
        [SerializeField] private bool showEngineModule = true;
        [SerializeField] private bool showTransmissionModule = true;
        [SerializeField] private bool showForcesModule = true;
        [SerializeField] private bool showWheelsModule = false;
        [SerializeField] private bool showSuspensionModule = false;
        [SerializeField] private bool showTireModelModule = true;

        [Header("Settings")]
        [SerializeField] private float updateInterval = 0.1f;
        [SerializeField] private float minScaleFactor = 0.7f;
        [SerializeField] private int dataFontSize = 14;
        [SerializeField] private Color categoryColor = Color.white;
        [SerializeField] private Color dataColor = Color.black;

        private float _nextUpdateTime;
        private CarSpec _cachedCarSpec;

        // Telemetry modules
        private InputTelemetryModule _inputModule;
        private MotionTelemetryModule _motionModule;
        private PowertrainTelemetryModule _engineModule;
        private TransmissionTelemetryModule _transmissionModule;
        private ForcesTelemetryModule _forcesModule;
        private WheelsTelemetryModule _wheelsModule;
        private SuspensionTelemetryModule _suspensionModule;
        private TireModelTelemetryModule _tireModelModule;

        // UI elements
        private GameObject _container;
        private Text _mainText;
        private RectTransform _containerRect;

        private void Start()
        {
            if (vehicleController == null)
            {
                vehicleController = FindFirstObjectByType<VehicleController>();
            }

            CacheCarSpec();
            InitializeModules();
            CreateUIElements();
        }

        private void CacheCarSpec()
        {
            if (vehicleController == null) return;

            var field = typeof(VehicleController).GetField("carSpec",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                _cachedCarSpec = field.GetValue(vehicleController) as CarSpec;
            }
        }

        private void InitializeModules()
        {
            _inputModule = new InputTelemetryModule(showInputModule);
            _motionModule = new MotionTelemetryModule(showMotionModule);
            _engineModule = new PowertrainTelemetryModule(showEngineModule);
            _transmissionModule = new TransmissionTelemetryModule(showTransmissionModule);
            _forcesModule = new ForcesTelemetryModule(showForcesModule);
            _wheelsModule = new WheelsTelemetryModule(showWheelsModule);
            _suspensionModule = new SuspensionTelemetryModule(showSuspensionModule);
            _tireModelModule = new TireModelTelemetryModule(showTireModelModule);
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
            if (rb == null || _mainText == null) return;

            // Get vehicle state via reflection
            var stateField = typeof(VehicleController).GetField("_state",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (stateField == null) return;

            var stateObj = stateField.GetValue(vehicleController);
            VehicleState state = GetVehicleStateFromObject(stateObj);

            // Get input
            var inputProvider = vehicleController.GetComponent<Vehicle.Input.VehicleInputProvider>();
            VehicleInput input = inputProvider != null ? inputProvider.CurrentInput : VehicleInput.Zero;

            // Build vehicle context
            VehicleContext ctx = BuildVehicleContext(rb, vehicleController.transform);

            // Update module visibility from Inspector
            _inputModule.SetEnabled(showInputModule);
            _motionModule.SetEnabled(showMotionModule);
            _engineModule.SetEnabled(showEngineModule);
            _transmissionModule.SetEnabled(showTransmissionModule);
            _forcesModule.SetEnabled(showForcesModule);
            _wheelsModule.SetEnabled(showWheelsModule);
            _suspensionModule.SetEnabled(showSuspensionModule);
            _tireModelModule.SetEnabled(showTireModelModule);

            // Build display text from all enabled modules
            string displayText = string.Empty;

            if (_inputModule.IsEnabled)
            {
                displayText += _inputModule.GetDisplayText(input, state, ctx) + "\n";
            }

            if (_motionModule.IsEnabled)
            {
                displayText += _motionModule.GetDisplayText(input, state, ctx) + "\n";
            }

            if (_engineModule.IsEnabled)
            {
                displayText += _engineModule.GetDisplayText(input, state, ctx) + "\n";
            }

            if (_transmissionModule.IsEnabled)
            {
                displayText += _transmissionModule.GetDisplayText(input, state, ctx) + "\n";
            }

            if (_wheelsModule.IsEnabled)
            {
                displayText += _wheelsModule.GetDisplayText(input, state, ctx) + "\n";
            }

            if (_forcesModule.IsEnabled)
            {
                displayText += _forcesModule.GetDisplayText(input, state, ctx) + "\n";
            }

            if (_suspensionModule.IsEnabled)
            {
                displayText += _suspensionModule.GetDisplayText(input, state, ctx) + "\n";
            }

            if (_tireModelModule.IsEnabled)
            {
                displayText += _tireModelModule.GetDisplayText(input, state, ctx) + "\n";
            }

            _mainText.text = displayText;
        }

        private VehicleState GetVehicleStateFromObject(object stateObj)
        {
            // VehicleState — struct, его можно безопасно скопировать
            return stateObj is VehicleState s ? s : default;
        }

        private VehicleContext BuildVehicleContext(Rigidbody rb, Transform tr)
        {
            if (_cachedCarSpec == null)
            {
                return new VehicleContext(rb, tr, null, Time.deltaTime);
            }

            return new VehicleContext(
                rb,
                tr,
                _cachedCarSpec,
                Time.deltaTime,
                _cachedCarSpec.engineSpec,
                _cachedCarSpec.gearboxSpec,
                _cachedCarSpec.wheelSpec,
                _cachedCarSpec.chassisSpec,
                _cachedCarSpec.steeringSpec,
                _cachedCarSpec.suspensionSpec,
                _cachedCarSpec.drivetrainSpec
            );
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
            if (_container == null)
            {
                _container = new GameObject("TelemetryContainer");
                _container.transform.SetParent(transform, false);

                _containerRect = _container.AddComponent<RectTransform>();
                _containerRect.anchorMin = new Vector2(0, 1);
                _containerRect.anchorMax = new Vector2(0, 1);
                _containerRect.pivot = new Vector2(0, 1);
                _containerRect.anchoredPosition = new Vector2(20, -20);
                _containerRect.sizeDelta = new Vector2(500, 1000);
            }

            // Create main text element
            if (_mainText == null)
            {
                GameObject textObj = new GameObject("TelemetryText");
                textObj.transform.SetParent(_container.transform, false);

                RectTransform rect = textObj.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(480, 980);

                _mainText = textObj.AddComponent<Text>();
                _mainText.text = "";
                _mainText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _mainText.fontSize = dataFontSize;
                _mainText.color = dataColor;
                _mainText.fontStyle = FontStyle.Normal;
                _mainText.alignment = TextAnchor.UpperLeft;
                _mainText.horizontalOverflow = HorizontalWrapMode.Overflow;
                _mainText.verticalOverflow = VerticalWrapMode.Overflow;
            }
        }

        private void OnValidate()
        {
            // Update module visibility when values change in Inspector
            if (Application.isPlaying)
            {
                if (_inputModule != null) _inputModule.SetEnabled(showInputModule);
                if (_motionModule != null) _motionModule.SetEnabled(showMotionModule);
                if (_engineModule != null) _engineModule.SetEnabled(showEngineModule);
                if (_transmissionModule != null) _transmissionModule.SetEnabled(showTransmissionModule);
                if (_forcesModule != null) _forcesModule.SetEnabled(showForcesModule);
                if (_wheelsModule != null) _wheelsModule.SetEnabled(showWheelsModule);
            }
        }
    }
}
