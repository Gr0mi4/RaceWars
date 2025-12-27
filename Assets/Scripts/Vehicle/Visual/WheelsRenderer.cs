// WheelsRenderer.cs
// цель: максимально понятный дебаг "колёсной физики" (тяга/тормоз/сцепление/моменты) в Gizmos.
//
// ВАЖНО ПРО ИНДЕКСЫ (твоя актуальная конвенция):
//   FRONT wheels: 0 and 2
//   REAR  wheels: 1 and 3
//
// Этот рендерер НЕ трогает гизмосы подвески (mount/raycast/contact/Fz) — они остаются как у тебя.
// Мы добавляем/структурируем гизмосы именно для:
//  1) Ускоряющих/тормозящих моментов (torque) на колесе
//  2) Применённых сил сцепления (Fx/Fy) + desired до клипа
//  3) Фрикционного лимита mu*Fz и "utilization"
//  4) Вкладов каждого колеса в yaw-момент вокруг COM (очень полезно для “почему рулится странно?”)

using UnityEngine;
using Vehicle.Core;
using Vehicle.Specs;

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

    private readonly RaycastHit[] _hits = new RaycastHit[4];
    private readonly bool[] _grounded = new bool[4];

    private float RayLengthResolved => (suspensionSpec != null) ? suspensionSpec.restLength : fallbackRayLength;
    private float WheelRadiusResolved => (wheelSpec != null) ? wheelSpec.wheelRadius : fallbackWheelRadius;

    // Palette so “если несколько колёс — разными цветами”
    // Мы используем базовый цвет на колесо, а затем оттеняем под тип гизмоса (тяга/тормоз/боковая).
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

            // ---------------------------
            //  A) Подвеска/контакты (НЕ трогаем, как просил)
            // ---------------------------
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

            // contact point + normal from raycast (_hits)
            Vector3 contact = _hits[i].point;
            Vector3 normal = _hits[i].normal.normalized;

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(contact, 0.05f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(contact, contact + normal * 0.25f);

            // ---------------------------
            //  B) Если нет физики колеса — дальше рисовать нечего
            // ---------------------------
            if (controller == null || controller.State.wheels == null || controller.State.wheels.Length <= i)
                continue;

            var wr = controller.State.wheels[i];

            // ---------------------------
            //  C) Вспомогательные оси колеса (для понимания "куда смотрит колесо")
            // ---------------------------
            // wheelForwardWorld задаётся SteeringSystem (или fallback в TireForcesSystem -> ctx.Forward).
            // На этой оси считается Fx (продольная сила).
            Vector3 fwd = wr.wheelForwardWorld.sqrMagnitude > 1e-6f ? wr.wheelForwardWorld.normalized : transform.forward;
            fwd = Vector3.ProjectOnPlane(fwd, normal);
            if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.ProjectOnPlane(transform.forward, normal);
            fwd.Normalize();

            Vector3 right = Vector3.Cross(normal, fwd);
            if (right.sqrMagnitude < 1e-6f) right = transform.right;
            right.Normalize();

            // Маленькие оси в контакте:
            //  - белая: fwd (куда колесо "катится")
            //  - серая: right (боковая ось колеса)
            Gizmos.color = Color.white;
            Gizmos.DrawLine(contact, contact + fwd * 0.25f);

            Gizmos.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            Gizmos.DrawLine(contact, contact + right * 0.20f);

            // ---------------------------
            //  E) 1) УСКОРЯЮЩИЕ / ТОРМОЗЯЩИЕ МОМЕНТЫ (TORQUE)
            // ---------------------------
            // ВАЖНО: torque — это не force. Это момент вокруг оси вращения колеса.
            // Мы рисуем его "дугой" в плоскости вращения колеса.
            //
            // - driveTorque: может быть + (разгон) или - (как торможение/drag из drivetrain)
            // - brakeTorque: всегда >=0 (запрос тормоза), но фактическое направление зависит от wheelOmega/vLong
            //
            // Чтобы видеть "что именно тормозит":
            //  - Разгоняющий момент: driveTorque, если он толкает колесо "вперёд по fwd".
            //  - Тормозящий момент: brakeTorque (по оценке направления) + отрицательный driveTorque.
            float wheelR = Mathf.Max(0.01f, (controller.State.wheelRadius > 0.01f ? controller.State.wheelRadius : WheelRadiusResolved));

            // Ось вращения колеса (примерно) = right
            Vector3 spinAxis = right;

            // 1) drive torque arc
            if (Mathf.Abs(wr.driveTorque) > 1e-3f)
            {
                // Цвет: “на колесо” + тип:
                //   - если wr.driveTorque положительный => разгон (яркий зелёный оттенок)
                //   - если отрицательный => тормозит/drag (яркий красный оттенок)
                Color baseC = WheelColors[i];
                Color c = wr.driveTorque >= 0f
                    ? Color.Lerp(baseC, new Color(0.1f, 1.0f, 0.1f, 1f), 0.55f)
                    : Color.Lerp(baseC, new Color(1.0f, 0.1f, 0.1f, 1f), 0.55f);

                // Длина/интенсивность дуги = |torque| * torqueScale (ограничим, чтобы не улетало)
                float arcAmount = Mathf.Clamp01(Mathf.Abs(wr.driveTorque) * torqueScale);

                // Направление дуги:
                // для визуализации считаем, что +torque пытается увеличить wheelOmega в сторону "вперёд".
                // Поэтому знак дуги завяжем на (driveTorque sign) и текущий wheelOmega, чтобы было “ощущение” направления.
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

            // 2) brake torque arc (оценка направления как в TireForcesSystem)
            if (wr.brakeTorque > 1e-3f)
            {
                // Определим “в какую сторону” тормоз противодействует:
                // - если колесо уже крутится => тормоз противоположен wheelOmega
                // - иначе если машина движется по vLong => тормоз противоположен vLong
                float brakeOpposesSign = 0f;
                if (Mathf.Abs(wr.wheelOmega) > 0.5f) brakeOpposesSign = Mathf.Sign(wr.wheelOmega);
                else if (Mathf.Abs(wr.debugVLong) > 0.1f) brakeOpposesSign = Mathf.Sign(wr.debugVLong);
                else brakeOpposesSign = 1f; // fallback

                // тормозящий момент “против” направления вращения => дуга будет противоположного знака
                float torqueSign = -brakeOpposesSign;

                Color baseC = WheelColors[i];
                Color c = Color.Lerp(baseC, new Color(1.0f, 0.55f, 0.05f, 1f), 0.65f); // оранжевый: тормоз
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
            //  F) 2) СИЛЫ СЦЕПЛЕНИЯ (Fx/Fy) — применённые (после friction circle clamp)
            // ---------------------------
            // wr.debugLongForce / wr.debugLatForce в твоём TireForcesSystem = уже применённые силы (после clamp).
            //
            // Это самое важное для понимания поведения:
            // - Fx (magenta) = тяга/торможение в плоскости дороги
            // - Fy (red)     = боковая сила удержания траектории
            //
            // Если Fy слишком огромная на скорости => “по рельсам” (то, что ты видишь на FWD).
            // Если Fy слишком маленькая на передней оси => “не рулится”.
            Vector3 fxApplied = wr.debugLongForce;
            Vector3 fyApplied = wr.debugLatForce;

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

            // ---------------------------
            //  G) Desired forces (до клипа)
            // ---------------------------
            // wr.debugFxDesired / wr.debugFyDesired — у тебя это скаляры “до friction circle”.
            // Чтобы визуально понять “упираемся ли в сцепление”:
            // - если desired сильно больше applied => мы в клипе, и это и есть “снос/букс/блок”
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
            //  H) 3) Фрикционный бюджет mu*Fz и utilization
            // ---------------------------
            // wr.debugMuFz = mu*Fz (максимальный модуль результирующей силы в плоскости)
            // wr.debugUtil = (|desired| / muFz) примерно (у тебя так посчитано)
            if (drawFrictionBudget && wr.debugMuFz > 1e-3f)
            {
                // Рисуем “кольцо лимита”: радиус пропорционален muFz * forceScale.
                // Чем больше радиус — тем больше доступный grip.
                float r = Mathf.Clamp(wr.debugMuFz * forceScale, 0.05f, 1.5f);

                // Цвет: голубой лимит
                Gizmos.color = new Color(0.2f, 0.9f, 1.0f, 0.75f);
                DrawRing(contact, normal, r, 18);

                // Utilization: маленький маркер на кольце
                // util ~ 1 => “на грани”
                float util = Mathf.Clamp01(wr.debugUtil);
                Gizmos.color = util < 0.85f
                    ? new Color(0.2f, 1.0f, 0.2f, 0.9f)   // зелёный: запас
                    : new Color(1.0f, 0.2f, 0.2f, 0.9f); // красный: упёрлись

                // Маркер: просто точка на окружности по направлению applied force
                Vector3 fPlane = fxApplied + fyApplied;
                Vector3 dirPlane = fPlane.sqrMagnitude > 1e-6f ? fPlane.normalized : fwd;
                dirPlane = Vector3.ProjectOnPlane(dirPlane, normal).normalized;
                Vector3 marker = contact + dirPlane * (r * util);

                Gizmos.DrawSphere(marker, 0.03f);
            }

            // ---------------------------
            //  I) 4) Yaw moment around COM (почему “переруливает/недоруливает”)
            // ---------------------------
            // Момент вокруг вертикальной оси (Y) = (r x F).y,
            // где r — вектор от COM к точке контакта, F — сила в контакте.
            //
            // ВАЖНО: это даёт “картину” поворота:
            // - большие моменты с задней оси на RWD => склонность к oversteer
            // - если перед почти не даёт момента => “не рулится”
            if (drawYawMoments && rb != null)
            {
                Vector3 com = rb.worldCenterOfMass;
                Vector3 rVec = contact - com;

                Vector3 fTotal = fxApplied + fyApplied;
                if (fTotal.sqrMagnitude > 1e-4f)
                {
                    float yaw = Vector3.Cross(rVec, fTotal).y; // N*m (примерно)
                    float len = Mathf.Clamp(yaw * yawMomentScale, -1.5f, 1.5f);

                    // Цвет: индивидуальный на колесо, чтобы видеть кто даёт вклад
                    Gizmos.color = new Color(WheelColors[i].r, WheelColors[i].g, WheelColors[i].b, 0.9f);

                    // Рисуем вертикальную стрелку у COM:
                    // вверх = yaw положительный, вниз = отрицательный
                    Vector3 start = com + Vector3.up * 0.15f + (Vector3.right * (0.05f * (i - 1.5f))); // немного разнести, чтобы не совпадали
                    DrawArrow(start, Vector3.up * len);
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
    /// Интерпретация:
    /// - Положительный момент (sign=+1) — “крутит колесо вперёд”
    /// - Отрицательный — “тормозит/тащит назад”
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
}
