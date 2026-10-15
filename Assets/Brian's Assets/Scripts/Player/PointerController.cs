using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Brian
{
    public class PointerController : MonoBehaviour
    {
        [SerializeField] float maxDistance = 25f;

        readonly RaycastHit[] hits = new RaycastHit[32];

        void Start()
        {
            var pc = PlayerControls.Instance;
            if (pc == null) return;
            pc.Select.performed += OnSelect;
            pc.Cancel.performed += OnCancel;
        }

        void OnDestroy()
        {
            var pc = PlayerControls.Instance;
            if (pc == null) return;
            pc.Select.performed -= OnSelect;
            pc.Cancel.performed -= OnCancel;
        }

        void OnSelect(InputAction.CallbackContext _)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsOver) return;

            Ray ray = PlayerControls.Instance.AimRay;
            int count = Physics.RaycastNonAlloc(ray, hits, maxDistance, ~0, QueryTriggerInteraction.Collide);
            Array.Sort(hits, 0, count, HitComparer.Instance);

            if (count > 0 && hits[0].collider.GetComponent<Canvas>() != null) return;

            var build = BuildController.Instance;
            if (build != null && build.IsBuilding)
            {
                build.TryPlace();
                return;
            }

            for (int i = 0; i < count; i++)
            {
                var col = hits[i].collider;
                var meteor = col.GetComponentInParent<Meteor>();
                if (meteor != null)
                {
                    meteor.Collect();
                    return;
                }
                var tower = col.GetComponentInParent<Tower>();
                if (tower != null)
                {
                    if (TowerPanel.Instance != null) TowerPanel.Instance.Show(tower);
                    return;
                }
            }
        }

        void OnCancel(InputAction.CallbackContext _)
        {
            var build = BuildController.Instance;
            if (build != null && build.IsBuilding) build.Cancel();
            else if (TowerPanel.Instance != null) TowerPanel.Instance.Hide();
        }

        class HitComparer : System.Collections.Generic.IComparer<RaycastHit>
        {
            public static readonly HitComparer Instance = new HitComparer();
            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
        }
    }
}
