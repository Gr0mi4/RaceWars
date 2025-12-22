using UnityEngine;

namespace Vehicle.Core
{
    /// <summary>
    /// Compatibility layer for Unity Rigidbody API changes between Unity versions.
    /// Unity 6000.0+ uses linearVelocity/angularVelocity, while older versions use velocity/angularVelocity.
    /// Unity 6000.0+ uses linearDamping/angularDamping, while older versions use drag/angularDrag.
    /// </summary>
    public static class RigidbodyCompat
    {
#if UNITY_6000_0_OR_NEWER
        /// <summary>
        /// Gets the linear velocity of the rigidbody (Unity 6000.0+ API).
        /// </summary>
        public static Vector3 GetVelocity(Rigidbody rb) => rb.linearVelocity;
        
        /// <summary>
        /// Sets the linear velocity of the rigidbody (Unity 6000.0+ API).
        /// </summary>
        public static void SetVelocity(Rigidbody rb, Vector3 v) => rb.linearVelocity = v;

        /// <summary>
        /// Gets the linear damping of the rigidbody (Unity 6000.0+ API).
        /// </summary>
        public static float GetLinearDamping(Rigidbody rb) => rb.linearDamping;
        
        /// <summary>
        /// Sets the linear damping of the rigidbody (Unity 6000.0+ API).
        /// </summary>
        public static void SetLinearDamping(Rigidbody rb, float v) => rb.linearDamping = v;

        /// <summary>
        /// Gets the angular damping of the rigidbody (Unity 6000.0+ API).
        /// </summary>
        public static float GetAngularDamping(Rigidbody rb) => rb.angularDamping;
        
        /// <summary>
        /// Sets the angular damping of the rigidbody (Unity 6000.0+ API).
        /// </summary>
        public static void SetAngularDamping(Rigidbody rb, float v) => rb.angularDamping = v;
#else
        /// <summary>
        /// Gets the velocity of the rigidbody (legacy Unity API).
        /// </summary>
        public static Vector3 GetVelocity(Rigidbody rb) => rb.velocity;
        
        /// <summary>
        /// Sets the velocity of the rigidbody (legacy Unity API).
        /// </summary>
        public static void SetVelocity(Rigidbody rb, Vector3 v) => rb.velocity = v;

        /// <summary>
        /// Gets the linear damping (drag) of the rigidbody (legacy Unity API).
        /// </summary>
        public static float GetLinearDamping(Rigidbody rb) => rb.drag;
        
        /// <summary>
        /// Sets the linear damping (drag) of the rigidbody (legacy Unity API).
        /// </summary>
        public static void SetLinearDamping(Rigidbody rb, float v) => rb.drag = v;

        /// <summary>
        /// Gets the angular damping (angularDrag) of the rigidbody (legacy Unity API).
        /// </summary>
        public static float GetAngularDamping(Rigidbody rb) => rb.angularDrag;
        
        /// <summary>
        /// Sets the angular damping (angularDrag) of the rigidbody (legacy Unity API).
        /// </summary>
        public static void SetAngularDamping(Rigidbody rb, float v) => rb.angularDrag = v;
#endif
    }
}
