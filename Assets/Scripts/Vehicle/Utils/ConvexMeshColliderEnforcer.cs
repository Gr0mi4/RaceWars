using UnityEngine;

namespace Vehicle.Utils
{
    /// <summary>
    /// Ensures all MeshColliders under this GameObject are convex to avoid runtime errors
    /// with dynamic rigidbodies. Attach to the vehicle root that has the Rigidbody.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class ConvexMeshColliderEnforcer : MonoBehaviour
    {
        private void Awake()
        {
            var colliders = GetComponentsInChildren<MeshCollider>(includeInactive: true);
            foreach (var mc in colliders)
            {
                if (!mc.convex)
                {
                    mc.convex = true;
                }
            }
        }
    }
}

