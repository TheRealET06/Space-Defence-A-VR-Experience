using UnityEngine;

namespace Brian
{
    public class RangeIndicator : MonoBehaviour
    {
        Transform follow;

        public void SetRange(float range) => transform.localScale = Vector3.one * range * 2f;

        public void Follow(Transform target) => follow = target;

        void LateUpdate()
        {
            if (follow != null) transform.position = follow.position;
        }
    }
}
