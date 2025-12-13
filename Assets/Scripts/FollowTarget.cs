using UnityEngine;

public class FollowTarget : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 2, -6);
    public float smooth = 8f;

    void LateUpdate()
    {
        if (!target) return;
        Vector3 desired = target.position + target.TransformDirection(offset);
        transform.position = Vector3.Lerp(transform.position, desired, smooth * Time.deltaTime);

        // смотреть на цель
        Vector3 lookPoint = target.position + Vector3.up * 1f;
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(lookPoint - transform.position),
            smooth * Time.deltaTime
        );
    }
}
