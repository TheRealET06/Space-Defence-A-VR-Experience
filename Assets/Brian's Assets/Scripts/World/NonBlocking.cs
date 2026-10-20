using UnityEngine;

namespace Brian
{
    public class NonBlocking : MonoBehaviour
    {
        int lastChildCount = -1;

        void Start() => Apply();

        void LateUpdate()
        {
            if (transform.childCount != lastChildCount) Apply();
        }

        void Apply()
        {
            lastChildCount = transform.childCount;
            foreach (var c in GetComponentsInChildren<Collider>(true))
            {
                if (c.isTrigger) continue;
                if (c is MeshCollider mesh && !mesh.convex) c.enabled = false;
                else c.isTrigger = true;
            }
        }
    }
}
