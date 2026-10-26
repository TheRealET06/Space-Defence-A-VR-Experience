using UnityEngine;

namespace Brian
{
    public class BuildController : MonoBehaviour
    {
        public static BuildController Instance { get; private set; }

        [SerializeField] TowerGrid grid;
        [SerializeField] Material ghostMaterial;
        [SerializeField] RangeIndicator rangePrefab;
        [SerializeField] float pickRadius = 0.15f;
        [SerializeField] float maxDistance = 15f;

        TowerData selected;
        GameObject ghost;
        RangeIndicator range;
        Vector3Int cell;
        Vector3 point;
        bool valid;
        int ignoreUntilFrame;

        public bool IsBuilding => selected != null;
        public TowerData Selected => selected;

        void Awake() => Instance = this;

        void Start()
        {
            if (grid == null) grid = FindAnyObjectByType<TowerGrid>();
        }

        public void Select(TowerData data)
        {
            Cancel();
            if (data != null && !data.IsUnlocked) return;
            if (data == null || data.prefab == null) return;
            selected = data;
            ghost = MakeGhost(data.prefab.gameObject, ghostMaterial);
            if (rangePrefab != null)
            {
                range = Instantiate(rangePrefab);
                range.SetRange(data.range);
            }
            ignoreUntilFrame = Time.frameCount + 3;
            if (TowerPanel.Instance != null) TowerPanel.Instance.Hide();
        }

        public void Cancel()
        {
            selected = null;
            valid = false;
            if (ghost != null) Destroy(ghost);
            if (range != null) Destroy(range.gameObject);
        }

        void Update()
        {
            if (selected == null) return;
            if (GameManager.Instance != null && GameManager.Instance.IsOver)
            {
                Cancel();
                return;
            }

            Ray ray = PlayerControls.Instance != null ? PlayerControls.Instance.AimRay : new Ray(Vector3.zero, Vector3.forward);
            valid = grid != null && grid.TryGetFreePointAlongRay(ray, pickRadius, maxDistance, out cell, out point);

            ghost.SetActive(valid);
            if (range != null) range.gameObject.SetActive(valid);
            if (!valid) return;
            ghost.transform.SetPositionAndRotation(point, grid.transform.rotation);
            if (range != null) range.transform.position = point;
        }

        public void TryPlace()
        {
            if (selected == null || !valid || Time.frameCount < ignoreUntilFrame) return;
            var eco = Economy.Instance;
            if (eco == null || !eco.TrySpend(selected.cost)) return;

            Tower tower = Instantiate(selected.prefab, point, grid.transform.rotation);
            tower.Init(selected, cell);
            grid.SetOccupied(cell, true);

            if (!eco.CanAfford(selected.cost)) Cancel();
        }

        static GameObject MakeGhost(GameObject prefab, Material material)
        {
            var holder = new GameObject("Ghost Holder");
            holder.SetActive(false);
            GameObject g = Instantiate(prefab, holder.transform);
            g.name = prefab.name + " (ghost)";

            foreach (var mb in g.GetComponentsInChildren<MonoBehaviour>(true)) DestroyImmediate(mb);
            foreach (var c in g.GetComponentsInChildren<Collider>(true)) DestroyImmediate(c);
            if (material != null)
                foreach (var r in g.GetComponentsInChildren<Renderer>(true))
                {
                    var mats = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++) mats[i] = material;
                    r.sharedMaterials = mats;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }

            g.transform.SetParent(null, false);
            Destroy(holder);
            g.SetActive(false);
            return g;
        }
    }
}
