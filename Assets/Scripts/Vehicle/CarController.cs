using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class CarController : MonoBehaviour
{
    [Header("Tuning")]
    [Min(0f)] public float motorForce = 12000f;
    [Min(0f)] public float steerStrength = 120f;
    [Min(0f)] public float maxSpeed = 25f;

    [Header("Rigidbody Defaults")]
    [Min(1f)] public float minMass = 800f;
    [Min(0f)] public float linearDamping = 0.05f;
    [Min(0f)] public float angularDamping = 0.5f;
    public Vector3 centerOfMass = new Vector3(0f, -0.4f, 0f);

    [Header("Drive Mode")]
    public DriveMode driveMode = DriveMode.Velocity;

    [Header("Debug")]
    public bool enableDebug = false;
    [Min(0.05f)] public float debugIntervalSeconds = 0.25f;

    public enum DriveMode
    {
        Velocity,
        AddForce
    }

    private Rigidbody _rb;
    private float _nextDebugTime;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        ApplyRigidbodyDefaults();
    }

    private void ApplyRigidbodyDefaults()
    {
        _rb.mass = Mathf.Max(_rb.mass, minMass);
        _rb.linearDamping = linearDamping;
        _rb.angularDamping = angularDamping;
        _rb.useGravity = true;
        _rb.centerOfMass = centerOfMass;
    }

    private void FixedUpdate()
    {
        ReadInput(out float throttle, out float steer);

        ApplySteering(steer);
        ApplyDrive(throttle);

        DebugTick(throttle, steer);
    }

    private static void ReadInput(out float throttle, out float steer)
    {
        // Prefer axes (works for keyboard/gamepad) and clamp to valid range.
        throttle = Mathf.Clamp(Input.GetAxisRaw("Vertical"), -1f, 1f);
        steer = Mathf.Clamp(Input.GetAxisRaw("Horizontal"), -1f, 1f);
    }

    private void ApplySteering(float steerInput)
    {
        float speed = _rb.linearVelocity.magnitude;
        if (speed <= 0.5f) return;

        float yawDegrees = steerInput * steerStrength * Time.fixedDeltaTime;
        if (Mathf.Abs(yawDegrees) < 0.0001f) return;

        Quaternion delta = Quaternion.Euler(0f, yawDegrees, 0f);
        _rb.MoveRotation(_rb.rotation * delta);
    }

    private void ApplyDrive(float throttleInput)
    {
        switch (driveMode)
        {
            case DriveMode.Velocity:
                DriveWithVelocity(throttleInput);
                break;

            case DriveMode.AddForce:
                DriveWithForce(throttleInput);
                break;
        }
    }

    private void DriveWithVelocity(float throttleInput)
    {
        Vector3 desired = transform.forward * (throttleInput * maxSpeed);
        desired.y = _rb.linearVelocity.y; // keep vertical motion (gravity/jumps)

        const float blend = 0.25f;
        _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, desired, blend);
    }

    private void DriveWithForce(float throttleInput)
    {
        Vector3 force = transform.forward * (throttleInput * motorForce);
        _rb.AddForce(force, ForceMode.Force);

        LimitFlatSpeed(maxSpeed);
    }

    private void LimitFlatSpeed(float maxFlatSpeed)
    {
        Vector3 v = _rb.linearVelocity;
        Vector3 flat = new Vector3(v.x, 0f, v.z);

        float m = flat.magnitude;
        if (m <= maxFlatSpeed) return;

        Vector3 limited = flat / m * maxFlatSpeed;
        _rb.linearVelocity = new Vector3(limited.x, v.y, limited.z);
    }

    private void DebugTick(float throttle, float steer)
    {
        if (!enableDebug) return;
        if (Time.time < _nextDebugTime) return;

        _nextDebugTime = Time.time + debugIntervalSeconds;

        float speed = _rb.linearVelocity.magnitude;
        Debug.Log($"[CarController] throttle={throttle:0.00} steer={steer:0.00} speed={speed:0.00} pos={transform.position}");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!enableDebug) return;

        float relSpeed = collision.relativeVelocity.magnitude;
        Debug.Log($"[CarController] Collision with '{collision.gameObject.name}', relativeSpeed={relSpeed:0.00}");
    }
}