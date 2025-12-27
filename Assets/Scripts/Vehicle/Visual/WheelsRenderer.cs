// WheelsRenderer.cs
// Goal: highly readable wheel physics debug visualization in Gizmos (traction / braking / grip / moments).
//
// WheelIndex convention (IMPORTANT):
// - 0 = FL, 1 = RL, 2 = FR, 3 = RR
// - Front axle: 0 & 2, Rear axle: 1 & 3
//
// This renderer does NOT change suspension gizmos (mount/raycast/contact/Fz) logic.
// It adds structured gizmos for:
// 1) Per-wheel drive/brake torque visualization
// 2) Applied tire forces (Fx/Fy) + "desired" (pre-clamp) forces
// 3) Friction budget (mu*Fz) and utilization
// 4) Per-wheel yaw moment contribution around COM

using UnityEngine;
using Vehicle.Core;
using Vehicle.Specs;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class WheelsRenderer : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private WheelSpec wheelSpec;
    [SerializeField] private SuspensionSpec suspensionSpec;
    [SerializeField] private VehicleController controller;

    [Header("Visuals (order in this array must match WheelRuntime indices)")]
    [SerializeField] private Transform[] wheelVisuals = new Transform[4];

    [Header("Raycast / Visual Placement")]
    [Tooltip("Fallback if specs are missing (debug only).")]
    [SerializeField] private float fallbackRayLength = 0.6f;
    [Tooltip("Fallback if specs are missing (debug only).")]
    [SerializeField] private float fallbackWheelRadius = 0.32f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Visual Offset (center-of-wheel relative to mount)")]
    [SerializeField] private Vector3[] wheelVisualOffsets = new Vector3[4];

    [Header("Debug / Gizmos: scales")]
    [Tooltip("Scale for force vectors (Fx/Fy). 2000N -> 1m means scale=1/2000.")]
    [SerializeField] private float forceScale = 1f / 2000f;

    [Tooltip("Scale for torque arrows (Nm). 1000Nm -> ~1m arc magnitude feels OK. Tune.")]
    [SerializeField] private float torqueScale = 1f / 1000f;

    [Tooltip("Scale for yaw-moment arrow around COM. 5000 N*m -> 1m.")]
    [SerializeField] private float yawMomentScale = 1f / 5000f;

    [Tooltip("Radius of torque arc drawn at contact patch.")]
    [SerializeField] private float torqueArcRadius = 0.15f;

    [Tooltip("How many segments to draw an arc (more = smoother).")]
    [Range(6, 32)]
    [SerializeField] private int arcSegments = 14;

    [Tooltip("Show desired forces (before friction circle clamp).")]
    [SerializeField] private bool drawDesiredForces = true;

    [Tooltip("Show friction limit ring (mu*Fz) and utilization.")]
    [SerializeField] private bool drawFrictionBudget = true;

    [Tooltip("Show per-wheel yaw moment contribution around COM.")]
    [SerializeField] private bool drawYawMoments = true;

    [Header("Debug / Gizmos: toggles")]
    [Tooltip("Draw suspension/contact helpers (mount point, ray, contact point, normal).")]
    [SerializeField] private bool drawSuspensionAndContacts = true;

    [Tooltip("Draw wheel local axes at the contact patch (fwd/right).")]
    [SerializeField] private bool drawWheelAxes = true;

    [Tooltip("Draw drive/brake torque arcs at the contact patch.")]
    [SerializeField] private bool drawTorqueArcs = true;

    [Tooltip("Draw applied tire forces (Fx and Fy) after friction clamp.")]
    [SerializeField] private bool drawAppliedForces = true;

    [Tooltip("Draw numeric label at the contact patch (Fx/Fy/Fz/muFz/util/alpha/kappa/vLong/vLat).")]
    [SerializeField] private bool drawContactLabels = true;

    [Tooltip("Draw tire-model debug extras (slip angle arrow, vLong/vLat vectors, zero-lateral indicator, velocity direction, raw-vs-final marker).")]
    [SerializeField] private bool drawTireModelDebug = true;

    private readonly RaycastHit[] _hits = new RaycastHit[4];
    private readonly bool[] _grounded = new bool[4];

    private float RayLengthResolved => (suspensionSpec != null) ? suspensionSpec.restLength : fallbackRayLength;
    private float WheelRadiusResolved => (wheelSpec != null) ? wheelSpec.wheelRadius : fallbackWheelRadius;

#if UNITY_EDITOR
    private static GUIStyle _contactLabelStyle;
    private static Texture2D _contactLabelBg;

    private static GUIStyle GetContactLabelStyle()
    {
        if (_contactLabelStyle != null)
            return _contactLabelStyle;

        if (_contactLabelBg == null)
        {
            _contactLabelBg = new Texture2D(2, 2);
            _contactLabelBg.hideFlags = HideFlags.HideAndDontSave;
            var col = new Color(0, 0, 0, 0.7f);
            _contactLabelBg.SetPixels(new[] { col, col, col, col });
            _contactLabelBg.Apply();
        }

        _contactLabelStyle = new GUIStyle
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        _contactLabelStyle.normal.textColor = Color.white;
        _contactLabelStyle.normal.background = _contactLabelBg;
        return _contactLabelStyle;
    }
#endif

    // Per-wheel color palette (helps when multiple wheels are visualized at once).
    // We take a base color per wheel and tint it for different gizmo types.
    private static readonly Color[] WheelColors =
    {
        new Color(0.90f, 0.25f, 0.25f, 1f), // wheel 0
        new Color(0.25f, 0.90f, 0.25f, 1f), // wheel 1
        new Color(0.25f, 0.45f, 0.95f, 1f), // wheel 2
        new Color(0.95f, 0.80f, 0.25f, 1f), // wheel 3
    };

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

        Rigidbody rb = null;
        if (controller != null) rb = controller.GetComponent<Rigidbody>();

        for (int i = 0; i < 4; i++)
        {
            Vector3 mountWorld = transform.TransformPoint(wheelSpec.wheelOffsets[i]);
            Vector3 dir = -transform.up;
            float castDist = rayLength + wheelRadius;

            // Don't depend on LateUpdate state: raycast here so gizmos work in edit mode too.
            bool grounded = Physics.Raycast(
                mountWorld,
                dir,
                out RaycastHit hit,
                castDist,
                groundMask,
                QueryTriggerInteraction.Ignore
            );

            if (!grounded)
                continue;

            // contact point + normal from raycast
            Vector3 contact = hit.point;
            Vector3 normal = hit.normal.normalized;

            if (drawSuspensionAndContacts)
            {
                // ---------------------------
                //  A) Suspension / contacts helpers
                // ---------------------------
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(mountWorld, 0.04f);

                Gizmos.color = Color.white;
                Gizmos.DrawLine(mountWorld, mountWorld + dir * castDist);

                Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
                Vector3 approxWheelCenter = mountWorld + dir * rayLength + transform.up * wheelRadius;
                Gizmos.DrawWireSphere(approxWheelCenter, 0.05f);

                Gizmos.color = Color.green;
                Gizmos.DrawSphere(contact, 0.05f);

                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(contact, contact + normal * 0.25f);
            }

            // ---------------------------
            //  B) If there is no wheel runtime physics data, stop here
            // ---------------------------
            if (controller == null || controller.State.wheels == null || controller.State.wheels.Length <= i)
                continue;

            var wr = controller.State.wheels[i];

            // ---------------------------
            //  C) Wheel axes helpers (where the wheel "points")
            // ---------------------------
            // wheelForwardWorld is set by SteeringSystem (or falls back to body forward).
            // Fx is applied along this axis.
            Vector3 fwd = wr.wheelForwardWorld.sqrMagnitude > 1e-6f ? wr.wheelForwardWorld.normalized : transform.forward;
            fwd = Vector3.ProjectOnPlane(fwd, normal);
            if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.ProjectOnPlane(transform.forward, normal);
            fwd.Normalize();

            Vector3 right = Vector3.Cross(normal, fwd);
            if (right.sqrMagnitude < 1e-6f) right = transform.right;
            right.Normalize();

            if (drawWheelAxes)
            {
                // Small axes at the contact point:
                // - white: fwd (rolling direction)
                // - grey : right (wheel lateral axis)
                Gizmos.color = Color.white;
                Gizmos.DrawLine(contact, contact + fwd * 0.25f);

                Gizmos.color = new Color(0.8f, 0.8f, 0.8f, 1f);
                Gizmos.DrawLine(contact, contact + right * 0.20f);
            }

            // ---------------------------
            //  E) 1) DRIVE / BRAKE TORQUE (Nm)
            // ---------------------------
            // Important: torque is not force. It's a moment around the wheel spin axis.
            // We draw it as an arc in the wheel plane.
            //
            // - driveTorque: can be + (drive) or - (engine braking / drag)
            // - brakeTorque: >= 0 request, sign depends on wheelOmega/vLong
            float wheelR = Mathf.Max(0.01f, (controller.State.wheelRadius > 0.01f ? controller.State.wheelRadius : WheelRadiusResolved));

            // Approx wheel spin axis = right
            Vector3 spinAxis = right;

            // 1) drive torque arc
            if (drawTorqueArcs && Mathf.Abs(wr.driveTorque) > 1e-3f)
            {
                // Color tint:
                // - positive driveTorque => acceleration (green-ish)
                // - negative driveTorque => braking/drag (red-ish)
                Color baseC = WheelColors[i];
                Color c = wr.driveTorque >= 0f
                    ? Color.Lerp(baseC, new Color(0.1f, 1.0f, 0.1f, 1f), 0.55f)
                    : Color.Lerp(baseC, new Color(1.0f, 0.1f, 0.1f, 1f), 0.55f);

                // Arc intensity = |torque| * torqueScale (clamped for readability)
                float arcAmount = Mathf.Clamp01(Mathf.Abs(wr.driveTorque) * torqueScale);

                // Arc direction:
                // For visualization we assume +torque tries to increase wheelOmega in the "forward" direction.
                float torqueSign = Mathf.Sign(wr.driveTorque);

                DrawTorqueArc(
                    contact,
                    normal,
                    fwd,
                    spinAxis,
                    torqueArcRadius,
                    arcAmount,
                    torqueSign,
                    c,
                    arcSegments
                );
            }

            // 2) brake torque arc (direction estimation matches TireForcesSystem)
            if (drawTorqueArcs && wr.brakeTorque > 1e-3f)
            {
                // Determine which direction the brake opposes:
                // - if wheel is spinning => opposite to wheelOmega
                // - else if moving => opposite to vLong
                float brakeOpposesSign = 0f;
                if (Mathf.Abs(wr.wheelOmega) > 0.5f) brakeOpposesSign = Mathf.Sign(wr.wheelOmega);
                else if (Mathf.Abs(wr.debugVLong) > 0.1f) brakeOpposesSign = Mathf.Sign(wr.debugVLong);
                else brakeOpposesSign = 1f; // fallback

                // Braking torque is "against" rotation => draw arc with opposite sign
                float torqueSign = -brakeOpposesSign;

                Color baseC = WheelColors[i];
                Color c = Color.Lerp(baseC, new Color(1.0f, 0.55f, 0.05f, 1f), 0.65f); // orange: braking
                float arcAmount = Mathf.Clamp01(wr.brakeTorque * torqueScale);

                DrawTorqueArc(
                    contact,
                    normal,
                    fwd,
                    spinAxis,
                    torqueArcRadius * 0.85f,
                    arcAmount,
                    torqueSign,
                    c,
                    arcSegments
                );
            }

            // ---------------------------
            //  F) 2) TIRE FORCES (Fx/Fy) — applied (after friction circle clamp)
            // ---------------------------
            // wr.debugLongForce / wr.debugLatForce are the applied forces.
            // - Fx (magenta): drive/brake in the road plane
            // - Fy (red)    : lateral force (turning)
            Vector3 fxApplied = wr.debugLongForce;
            Vector3 fyApplied = wr.debugLatForce;

            if (drawAppliedForces)
            {
                if (fxApplied.sqrMagnitude > 1e-6f)
                {
                    // magenta: applied Fx
                    Gizmos.color = new Color(1f, 0f, 1f, 1f);
                    DrawArrow(contact, fxApplied * forceScale);
                }

                if (fyApplied.sqrMagnitude > 1e-6f)
                {
                    // red: applied Fy
                    Gizmos.color = new Color(1f, 0.1f, 0.1f, 1f);
                    DrawArrow(contact, fyApplied * forceScale);
                }
            }

            // ---------------------------
            //  G) Desired forces (pre-clamp)
            // ---------------------------
            // Use this to see whether we are limited by grip:
            // - if desired >> applied => we are clamped by the friction circle
            if (drawDesiredForces)
            {
                // Semi-transparent: desired
                float fxDes = wr.debugFxDesired;
                float fyDes = wr.debugFyDesired;

                if (Mathf.Abs(fxDes) > 1e-3f)
                {
                    Gizmos.color = new Color(1f, 0f, 1f, 0.35f);
                    DrawArrow(contact, (fwd * fxDes) * forceScale);
                }
                if (Mathf.Abs(fyDes) > 1e-3f)
                {
                    Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.35f);
                    DrawArrow(contact, (right * fyDes) * forceScale);
                }
            }

            // ---------------------------
            //  H) 3) Friction budget (mu*Fz) and utilization
            // ---------------------------
            // wr.debugMuFz = mu*Fz (max available resultant force magnitude in the contact plane)
            // wr.debugUtil = utilization (how close we are to the limit)
            if (drawFrictionBudget && wr.debugMuFz > 1e-3f)
            {
                // Draw a "limit ring": radius is proportional to muFz * forceScale.
                // Bigger ring => more available grip.
                float r = Mathf.Clamp(wr.debugMuFz * forceScale, 0.05f, 1.5f);

                // Cyan: friction limit ring
                Gizmos.color = new Color(0.2f, 0.9f, 1.0f, 0.75f);
                DrawRing(contact, normal, r, 18);

                // Utilization: marker on the ring
                // util ~ 1 => at the limit
                float util = Mathf.Clamp01(wr.debugUtil);
                Gizmos.color = util < 0.85f
                    ? new Color(0.2f, 1.0f, 0.2f, 0.9f)   // green: margin
                    : new Color(1.0f, 0.2f, 0.2f, 0.9f); // red: saturated

                // Marker: a dot in the direction of the applied force in the contact plane
                Vector3 fPlane = fxApplied + fyApplied;
                Vector3 dirPlane = fPlane.sqrMagnitude > 1e-6f ? fPlane.normalized : fwd;
                dirPlane = Vector3.ProjectOnPlane(dirPlane, normal).normalized;
                Vector3 marker = contact + dirPlane * (r * util);

                Gizmos.DrawSphere(marker, 0.03f);
            }

            // ---------------------------
            //  H.1) 3D numeric labels at the contact patch (always for grounded wheels)
            // ---------------------------
            if (drawContactLabels)
                DrawContactPatchLabels(contact, normal, wr, fxApplied, fyApplied, i);

            // ---------------------------
            //  I) 4) Yaw moment around COM (why it over/under-steers)
            // ---------------------------
            // Yaw moment around Y axis = (r x F).y,
            // where r is COM->contact vector, F is contact force.
            if (drawYawMoments && rb != null)
            {
                Vector3 com = rb.worldCenterOfMass;
                Vector3 rVec = contact - com;

                Vector3 fTotal = fxApplied + fyApplied;
                if (fTotal.sqrMagnitude > 1e-4f)
                {
                    float yaw = Vector3.Cross(rVec, fTotal).y; // N*m (approx)
                    float len = Mathf.Clamp(yaw * yawMomentScale, -1.5f, 1.5f);

                    // Per-wheel color to see who contributes
                    Gizmos.color = new Color(WheelColors[i].r, WheelColors[i].g, WheelColors[i].b, 0.9f);

                    // Draw a vertical arrow near COM:
                    // up = positive yaw, down = negative
                    Vector3 start = com + Vector3.up * 0.15f + (Vector3.right * (0.05f * (i - 1.5f))); // small separation so arrows don't overlap
                    DrawArrow(start, Vector3.up * len);
                }
            }

            if (drawTireModelDebug)
            {
                // ---------------------------
                //  J) TIRE MODEL v2 DEBUG: Slip angle, velocities, zero-lateral flag
                // ---------------------------
                // Visualize key tire-model parameters for debugging handling issues

                // 1) Slip angle (α) - slip direction
                // Angle between wheel direction and motion direction
                float alphaRad = wr.debugSlipAngleRad;
                if (Mathf.Abs(alphaRad) > 0.001f)
                {
                    // Approx slip direction from alpha sign
                    Vector3 slipDirection = right * Mathf.Sign(alphaRad);
                    float slipMagnitude = Mathf.Abs(alphaRad) * 0.3f; // visualization scale

                    // Yellow arrow: slip direction
                    Gizmos.color = new Color(1f, 1f, 0f, 0.8f);
                    DrawArrow(contact, slipDirection * slipMagnitude);
                }

                // 2) Velocity components (vLong, vLat) in wheel coordinates
                float vLong = wr.debugVLong;
                float vLat = wr.debugVLat;

                // Green arrow: longitudinal speed (vLong)
                if (Mathf.Abs(vLong) > 0.01f)
                {
                    Gizmos.color = new Color(0f, 1f, 0f, 0.6f);
                    Vector3 vLongVec = fwd * vLong * 0.1f; // scale: 1 m/s => 0.1 m
                    DrawArrow(contact, vLongVec);
                }

                // Blue arrow: lateral speed (vLat)
                if (Mathf.Abs(vLat) > 0.01f)
                {
                    Gizmos.color = new Color(0f, 0.5f, 1f, 0.6f);
                    Vector3 vLatVec = right * vLat * 0.1f; // scale: 1 m/s => 0.1 m
                    DrawArrow(contact, vLatVec);
                }

                // 3) ShouldZeroLateral flag indicator
                // Red ring = lateral forces are being forced to zero
                if (wr.debugShouldZeroLateral)
                {
                    Gizmos.color = new Color(1f, 0f, 0f, 0.7f);
                    Gizmos.DrawWireSphere(contact + normal * 0.1f, 0.08f);
                }

                // 4) Wheel forward vs actual velocity direction
                // Shows difference between wheel direction and actual motion direction
                Vector3 actualVelocity = controller.GetComponent<Rigidbody>().GetPointVelocity(contact);
                Vector3 actualVelocityPlane = Vector3.ProjectOnPlane(actualVelocity, normal);
                if (actualVelocityPlane.sqrMagnitude > 0.01f)
                {
                    actualVelocityPlane.Normalize();
                    // Orange arrow: actual motion direction
                    Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
                    DrawArrow(contact, actualVelocityPlane * 0.2f);
                }

                // 5) Raw vs final forces marker (useful when build-up/filtering is added)
                float fxRaw = wr.debugFxRaw;
                float fyRaw = wr.debugFyRaw;
                float fxFinal = wr.debugFxFinal;
                float fyFinal = wr.debugFyFinal;

                float fxDiff = Mathf.Abs(fxRaw - fxFinal);
                float fyDiff = Mathf.Abs(fyRaw - fyFinal);
                if (fxDiff > 10f || fyDiff > 10f)
                {
                    Gizmos.color = new Color(0.8f, 0f, 0.8f, 0.8f);
                    Gizmos.DrawSphere(contact + normal * 0.15f, 0.04f);
                }
            }
        }
    }

    // ------------------------------------------------------------
    //  Visual steering
    // ------------------------------------------------------------
    private void ApplySteerVisuals()
    {
        if (controller == null)
            return;

        var wheels = controller.State.wheels;
        if (wheels == null || wheels.Length < 3)
            return;

        if (wheelVisuals == null || wheelVisuals.Length < 3)
            return;

        const int FrontLeft = 0;
        const int FrontRight = 2;

        Vector3 upAxis = transform.up;

        ApplyFrontWheelVisualYaw(wheelVisuals[FrontLeft], wheels[FrontLeft].wheelForwardWorld, upAxis);
        ApplyFrontWheelVisualYaw(wheelVisuals[FrontRight], wheels[FrontRight].wheelForwardWorld, upAxis);
    }

    private void ApplyFrontWheelVisualYaw(Transform wheelVisual, Vector3 wheelForwardWorld, Vector3 upAxis)
    {
        if (wheelVisual == null)
            return;

        if (wheelForwardWorld.sqrMagnitude < 1e-6f)
            return;

        Vector3 flatForward = Vector3.ProjectOnPlane(wheelForwardWorld, upAxis);
        if (flatForward.sqrMagnitude < 1e-6f)
            return;

        flatForward.Normalize();

        float steerDeg = Vector3.SignedAngle(transform.forward, flatForward, upAxis);
        wheelVisual.localRotation = Quaternion.Euler(0f, steerDeg, 0f);
    }

    // ------------------------------------------------------------
    //  Gizmo helpers
    // ------------------------------------------------------------

    /// <summary>
    /// Draws an arrow from a point.
    /// Vector 'v' includes both direction and length in world units.
    /// </summary>
    private static void DrawArrow(Vector3 start, Vector3 v)
    {
        Vector3 end = start + v;
        Gizmos.DrawLine(start, end);

        // tiny "head"
        float head = Mathf.Clamp(v.magnitude * 0.15f, 0.03f, 0.12f);
        if (head <= 1e-4f) return;

        Vector3 dir = v.normalized;
        Vector3 side = Vector3.Cross(dir, Vector3.up);
        if (side.sqrMagnitude < 1e-6f) side = Vector3.Cross(dir, Vector3.right);
        side.Normalize();

        Vector3 a = end - dir * head + side * head * 0.5f;
        Vector3 b = end - dir * head - side * head * 0.5f;

        Gizmos.DrawLine(end, a);
        Gizmos.DrawLine(end, b);
    }

    /// <summary>
    /// Draws a ring (circle) in a plane defined by normal.
    /// Good for showing friction budget (mu*Fz) in contact patch plane.
    /// </summary>
    private static void DrawRing(Vector3 center, Vector3 normal, float radius, int segments)
    {
        normal.Normalize();
        Vector3 axisA = Vector3.Cross(normal, Vector3.up);
        if (axisA.sqrMagnitude < 1e-6f) axisA = Vector3.Cross(normal, Vector3.right);
        axisA.Normalize();
        Vector3 axisB = Vector3.Cross(normal, axisA).normalized;

        Vector3 prev = center + axisA * radius;
        float step = (Mathf.PI * 2f) / Mathf.Max(6, segments);

        for (int i = 1; i <= segments; i++)
        {
            float t = step * i;
            Vector3 p = center + (axisA * Mathf.Cos(t) + axisB * Mathf.Sin(t)) * radius;
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
    }

    /// <summary>
    /// Draws a torque "arc" in wheel rotation plane.
    ///
    /// Parameters:
    /// - center: contact point
    /// - normal: ground normal (defines wheel plane with forward)
    /// - forward: wheel forward direction in plane
    /// - spinAxis: axis of rotation (approx = wheel right)
    /// - radius: visual radius of arc
    /// - amount01: 0..1 how large arc is
    /// - sign: +1 / -1 direction
    ///
    /// Interpretation:
    /// - Positive torque (sign=+1) tries to spin the wheel forward
    /// - Negative torque tries to brake / pull it backward
    /// </summary>
    private static void DrawTorqueArc(
        Vector3 center,
        Vector3 normal,
        Vector3 forward,
        Vector3 spinAxis,
        float radius,
        float amount01,
        float sign,
        Color color,
        int segments
    )
    {
        if (amount01 <= 0f) return;

        normal.Normalize();
        forward.Normalize();
        spinAxis.Normalize();

        // wheel rotation plane basis:
        // in a no-camber world: plane is spanned by forward and normal (up along suspension normal).
        Vector3 a = forward;
        Vector3 b = Vector3.Cross(spinAxis, a); // should be ~normal direction
        b = Vector3.ProjectOnPlane(b, spinAxis);
        if (b.sqrMagnitude < 1e-6f) b = normal;
        b.Normalize();

        // Arc angle: from small (30 deg) up to (220 deg) depending on amount01
        float minAngle = 30f * Mathf.Deg2Rad;
        float maxAngle = 220f * Mathf.Deg2Rad;
        float arc = Mathf.Lerp(minAngle, maxAngle, amount01);

        // We start the arc slightly “ahead”, so it doesn’t overlap all arrows
        float startAngle = -arc * 0.5f;
        float step = arc / Mathf.Max(6, segments);

        Gizmos.color = color;

        Vector3 PrevPoint(float ang)
        {
            // sign controls direction
            float s = sign >= 0f ? 1f : -1f;
            float ca = Mathf.Cos(ang * s);
            float sa = Mathf.Sin(ang * s);
            return center + (a * ca + b * sa) * radius;
        }

        Vector3 prev = PrevPoint(startAngle);
        for (int i = 1; i <= segments; i++)
        {
            float ang = startAngle + step * i;
            Vector3 p = PrevPoint(ang);
            Gizmos.DrawLine(prev, p);
            prev = p;
        }

        // add a tiny arrow head at the end of arc (tangent direction)
        // tangent roughly = derivative of param curve
        Vector3 end = prev;
        Vector3 endPrev = PrevPoint(startAngle + step * (segments - 1));
        Vector3 tangent = (end - endPrev);
        if (tangent.sqrMagnitude > 1e-6f)
        {
            tangent.Normalize();
            DrawArrow(endPrev, tangent * 0.12f);
        }
    }

    /// <summary>
    /// Draws numeric 3D labels near the contact patch.
    /// Shows key parameters: Fx, Fy, Fz, mu*Fz, utilization, slip angle, slip ratio.
    /// </summary>
    private void DrawContactPatchLabels(
        Vector3 contact,
        Vector3 normal,
        Vehicle.Core.WheelRuntime wr,
        Vector3 fxApplied,
        Vector3 fyApplied,
        int wheelIndex
    )
    {
#if UNITY_EDITOR
        // Label position: slightly above the contact patch
        Vector3 labelPosition = contact + normal * 0.15f;

        // Build the label string with key values
        float fx = fxApplied.magnitude;
        float fy = fyApplied.magnitude;
        float fz = wr.normalForce;
        float muFz = wr.debugMuFz;
        float util = wr.debugUtil;
        float alphaDeg = wr.debugSlipAngleRad * Mathf.Rad2Deg;
        float kappa = wr.debugSlipRatio;
        float vLong = wr.debugVLong;
        float vLat = wr.debugVLat;

        // Validate values (NaN / Infinity)
        if (float.IsNaN(fx) || float.IsInfinity(fx)) fx = 0f;
        if (float.IsNaN(fy) || float.IsInfinity(fy)) fy = 0f;
        if (float.IsNaN(fz) || float.IsInfinity(fz)) fz = 0f;
        if (float.IsNaN(muFz) || float.IsInfinity(muFz)) muFz = 0f;
        if (float.IsNaN(util) || float.IsInfinity(util)) util = 0f;
        if (float.IsNaN(alphaDeg) || float.IsInfinity(alphaDeg)) alphaDeg = 0f;
        if (float.IsNaN(kappa) || float.IsInfinity(kappa)) kappa = 0f;
        if (float.IsNaN(vLong) || float.IsInfinity(vLong)) vLong = 0f;
        if (float.IsNaN(vLat) || float.IsInfinity(vLat)) vLat = 0f;

        // Format values for display
        string labelText = $"Wheel {GetWheelName(wheelIndex)}\n" +
                          $"Fx: {fx:F0}N  Fy: {fy:F0}N  Fz: {fz:F0}N\n" +
                          $"μFz: {muFz:F0}N  Util: {util:F2}\n" +
                          $"α: {alphaDeg:F1}°  κ: {kappa:F3}\n" +
                          $"vLong: {vLong:F2}m/s  vLat: {vLat:F2}m/s";

        Handles.Label(labelPosition, labelText, GetContactLabelStyle());
#endif
    }

    /// <summary>
    /// Returns display name for a wheel (FL, RL, FR, RR).
    /// WheelIndex convention: 0=FL,1=RL,2=FR,3=RR (front=0/2, rear=1/3)
    /// </summary>
    private string GetWheelName(int wheelIndex)
    {
        return WheelIndex.Name4(wheelIndex);
    }

}
