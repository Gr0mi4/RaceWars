using UnityEngine;
using Vehicle.Specs;
using Vehicle.Core;

public class WheelsRenderer : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private WheelSpec wheelSpec;
    [SerializeField] private SuspensionSpec suspensionSpec;

    [SerializeField] private VehicleController controller;

    [Header("Visuals (order: FL, FR, RL, RR)")]
    [SerializeField] private Transform[] wheelVisuals = new Transform[4];

    [Header("Raycast / Visual Placement")]
    [Tooltip("Fallback if specs are missing (debug only).")]
    [SerializeField] private float fallbackRayLength = 0.6f;

    [Tooltip("Fallback if specs are missing (debug only).")]
    [SerializeField] private float fallbackWheelRadius = 0.32f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Visual Offset (center-of-wheel relative to mount)")]
    [SerializeField] private Vector3[] wheelVisualOffsets = new Vector3[4];


    private readonly RaycastHit[] _hits = new RaycastHit[4];
    private readonly bool[] _grounded = new bool[4];

    private float RayLengthResolved => (suspensionSpec != null) ? suspensionSpec.restLength : fallbackRayLength;

    private float WheelRadiusResolved => (wheelSpec != null) ? wheelSpec.wheelRadius : fallbackWheelRadius;

    private void LateUpdate()
    {
        ApplySteerVisuals();
        if (wheelSpec == null || wheelSpec.wheelOffsets == null || wheelSpec.wheelOffsets.Length < 4) return;
        if (wheelVisuals == null || wheelVisuals.Length < 4) return;

        float rayLength = RayLengthResolved;
        float wheelRadius = WheelRadiusResolved;

        for (int i = 0; i < 4; i++)
        {
            if (wheelVisuals[i] == null) continue;

            // 1) mount point in world (hardpoint)
            Vector3 mountWorld = transform.TransformPoint(wheelSpec.wheelOffsets[i]);

            // 2) cast down along vehicle "down" axis
            Vector3 dir = -transform.up;
            float castDist = rayLength + wheelRadius;

            _grounded[i] = Physics.Raycast(
                mountWorld,
                dir,
                out _hits[i],
                castDist,
                groundMask,
                QueryTriggerInteraction.Ignore
            );

            // 3) place wheel center:
            //    - if grounded: at contact point + radius up
            //    - if not grounded: hang at max droop (mount + dir * rayLength)
            Vector3 wheelCenterWorld = _grounded[i]
                ? (_hits[i].point + transform.up * wheelRadius)
                : (mountWorld + dir * rayLength);

            // 4) apply visual offset (stored in vehicle local space, then converted to world)
            wheelVisuals[i].position = wheelCenterWorld + transform.TransformVector(wheelVisualOffsets[i]);
        }
    }

    private void OnDrawGizmos()
    {
        if (wheelSpec == null || wheelSpec.wheelOffsets == null || wheelSpec.wheelOffsets.Length < 4) return;

        float rayLength = RayLengthResolved;
        float wheelRadius = WheelRadiusResolved;

        for (int i = 0; i < 4; i++)
        {
            Vector3 mountWorld = transform.TransformPoint(wheelSpec.wheelOffsets[i]);
            Vector3 dir = -transform.up;
            float castDist = rayLength + wheelRadius;

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(mountWorld, 0.04f);

            Gizmos.color = Color.white;
            Gizmos.DrawLine(mountWorld, mountWorld + dir * castDist);

            Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
            Vector3 approxWheelCenter = mountWorld + dir * rayLength + transform.up * wheelRadius;
            Gizmos.DrawWireSphere(approxWheelCenter, 0.05f);

            bool grounded = (_grounded != null && _grounded.Length > i && _grounded[i]);

            if (!grounded)
                continue;

            // contact point + normal from raycast (_hits) - reliable for drawing
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_hits[i].point, 0.05f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(_hits[i].point, _hits[i].point + _hits[i].normal * 0.25f);

            // need controller state for forces
            if (controller == null || controller.State.wheels == null || controller.State.wheels.Length <= i)
                continue;

            var wr = controller.State.wheels[i];

            // --- Fz column (use _hits point/normal for position, wr.normalForce for magnitude)
            float fz = Mathf.Max(0f, wr.normalForce);
            float scaleN = 1f / 50f;                  // 5000N -> 1m (tweak if needed)
            float len = Mathf.Clamp(fz * scaleN, 0f, 1.5f);

            Vector3 pFz = _hits[i].point;
            Vector3 nFz = _hits[i].normal.normalized;

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(pFz, pFz + nFz * len);
            Gizmos.DrawSphere(pFz + nFz * len, 0.025f);

            // --- Drive force arrow
            Vector3 f = wr.debugDriveForce;
            if (f.sqrMagnitude > 0.0001f)
            {
                float scale = 1f / 5f; // leave your current scale
                Gizmos.color = Color.magenta;

                Vector3 p = _hits[i].point;
                Gizmos.DrawLine(p, p + f * scale);
                Gizmos.DrawSphere(p + f * scale, 0.03f);
            }

            // Wheel friction
            Vector3 lat = wr.debugLateralForce;
            if (lat.sqrMagnitude > 0.0001f)
            {
                float scale = 1f / 500f; //
                Gizmos.color = Color.red;
                Vector3 p = _hits[i].point;
                Gizmos.DrawLine(p, p + lat * scale);
                Gizmos.DrawSphere(p + lat * scale, 0.03f);
            }

            // Fx (longitudinal)
            Vector3 fx = wr.debugLongForce;
            if (fx.sqrMagnitude > 0.0001f)
            {
                float scale = 1f / 2000f;
                Gizmos.color = Color.magenta;
                Vector3 p = _hits[i].point;
                Gizmos.DrawLine(p, p + fx * scale);
                Gizmos.DrawSphere(p + fx * scale, 0.02f);
            }

        // Fy (lateral)
        Vector3 fy = wr.debugLatForce;
        if (fy.sqrMagnitude > 0.0001f)
            {
                float scale = 1f / 2000f;
                Gizmos.color = Color.red;
                Vector3 p = _hits[i].point;
                Gizmos.DrawLine(p, p + fy * scale);
                Gizmos.DrawSphere(p + fy * scale, 0.02f);
            }
        }
    }

    /// <summary>
/// Visual steering: rotate ONLY front wheel pivots to match the *actual* wheel direction used by physics.
/// We read WheelRuntime.wheelForwardWorld (written by SteeringSystem) and convert it to a yaw angle.
/// </summary>
/// <summary>
/// Visual steering: rotate ONLY front wheel visuals (FL/FR) to match the *actual* wheel direction used by physics.
/// Reads WheelRuntime.wheelForwardWorld (written by SteeringSystem) and applies yaw to the front wheel transforms.
/// </summary>
private void ApplySteerVisuals()
{
    // --- Preconditions -------------------------------------------------------
    if (controller == null)
        return;

    var wheels = controller.State.wheels;
    if (wheels == null || wheels.Length < 2)
        return;

    if (wheelVisuals == null || wheelVisuals.Length < 2)
        return;

    // Wheel order assumption: 0=FL, 1=FR, 2=RL, 3=RR
    const int FrontLeft = 0;
    const int FrontRight = 1;

    Vector3 upAxis = transform.up;

    ApplyFrontWheelVisualYaw(wheelVisuals[FrontLeft], wheels[FrontLeft].wheelForwardWorld, upAxis);
    ApplyFrontWheelVisualYaw(wheelVisuals[FrontRight], wheels[FrontRight].wheelForwardWorld, upAxis);
}

/// <summary>
/// Apply yaw rotation to a front wheel visual so it matches the physics wheel forward direction.
/// </summary>
private void ApplyFrontWheelVisualYaw(Transform wheelVisual, Vector3 wheelForwardWorld, Vector3 upAxis)
{
    if (wheelVisual == null)
        return;

    if (wheelForwardWorld.sqrMagnitude < 1e-6f)
        return;

    // Flatten to vehicle plane (yaw only)
    Vector3 flatForward = Vector3.ProjectOnPlane(wheelForwardWorld, upAxis);
    if (flatForward.sqrMagnitude < 1e-6f)
        return;

    flatForward.Normalize();

    // Signed yaw angle between car forward and wheel forward
    float steerDeg = Vector3.SignedAngle(transform.forward, flatForward, upAxis);

    // IMPORTANT:
    // If your wheel model's "forward" axis isn't +Z, you may need to add an offset here.
    // For now we apply yaw around local Y.
    wheelVisual.localRotation = Quaternion.Euler(0f, steerDeg, 0f);
}
}
