using UnityEngine;
using TMPro;
using Vehicle.Core;

public sealed class Tachometer : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private VehicleController vehicle;

    [Header("UI")]

    [SerializeField] private RectTransform needlePivot;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TMP_Text gearText;
    [SerializeField] private TMP_Text exactRPMText;

    [Header("Mapping")]
    [SerializeField] private float minRpm = 0f;
    [SerializeField] private float maxRpm = 9000f;
    [SerializeField] private float minAngle = -120f;
    [SerializeField] private float maxAngle = 120f;

    [Header("Smoothing")]
    [SerializeField] private float needleSmooth = 12f;

    private float _angle;

    private void Reset()
    {
        vehicle = Object.FindFirstObjectByType<VehicleController>();
    }

    private void Update()
    {
        if (!vehicle || !needlePivot) return;

        var state = vehicle.State;

        float rpm = state.engineRPM;
        int gear = state.currentGear;
        float speedKmh = state.speed * 3.6f;

        if (exactRPMText) exactRPMText.text = $"{rpm:0} RPM";
        if (speedText) speedText.text = $"{speedKmh:0} km/h";
        if (gearText) gearText.text = gear switch { -1 => "R", 0 => "N", _ => gear.ToString() };

        float t = Mathf.InverseLerp(minRpm, maxRpm, rpm);
        float target = Mathf.Lerp(minAngle, maxAngle, t);

        _angle = Mathf.Lerp(_angle, target, 1f - Mathf.Exp(-needleSmooth * Time.deltaTime));
        needlePivot.localRotation = Quaternion.Euler(0f, 0f, _angle);
    }
}
