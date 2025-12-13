using UnityEngine;

namespace Vehicle.Core
{
    public static class RigidbodyCompat
    {
#if UNITY_6000_0_OR_NEWER
        public static Vector3 GetVelocity(Rigidbody rb) => rb.linearVelocity;
        public static void SetVelocity(Rigidbody rb, Vector3 v) => rb.linearVelocity = v;

        public static float GetLinearDamping(Rigidbody rb) => rb.linearDamping;
        public static void SetLinearDamping(Rigidbody rb, float v) => rb.linearDamping = v;

        public static float GetAngularDamping(Rigidbody rb) => rb.angularDamping;
        public static void SetAngularDamping(Rigidbody rb, float v) => rb.angularDamping = v;
#else
        public static Vector3 GetVelocity(Rigidbody rb) => rb.velocity;
        public static void SetVelocity(Rigidbody rb, Vector3 v) => rb.velocity = v;

        public static float GetLinearDamping(Rigidbody rb) => rb.drag;
        public static void SetLinearDamping(Rigidbody rb, float v) => rb.drag = v;

        public static float GetAngularDamping(Rigidbody rb) => rb.angularDrag;
        public static void SetAngularDamping(Rigidbody rb, float v) => rb.angularDrag = v;
#endif
    }
}
