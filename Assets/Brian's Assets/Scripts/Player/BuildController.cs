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
        [SerializeField] float nodePickRadius = 0.4f;   // asteroids/nodes are bigger than grid points
        [SerializeField] float maxDistance = 15f;

        TowerData selected;
        GameObject ghost;
        RangeIndicator range;
        Vector3Int cell;
        Vector3 point;
        Quaternion rotation;
        TowerNode node;                 // set when building a node tower (e.g. gem extractor)
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
            if (rangePrefab != null && !data.prefab.UsesNode)
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
            node = null;
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
            if (selected.prefab.UsesNode)
            {
                // Node towers can only go on a free node of the matching type
                valid = grid != null && grid.TryGetFreeNodeAlongRay(ray, nodePickRadius, maxDistance,
                                                                    selected.prefab.RequiredNode, out node);
                if (valid)
                {
                    point = node.SnapPoint;
                    rotation = grid.transform.rotation;   // upright like grid towers, not tumbling with the asteroid
                    cell = new Vector3Int(-1, -1, -1);   // not a grid cell; TowerGrid ignores it
                }
            }
            else
            {
                node = null;
                valid = grid != null && grid.TryGetFreePointAlongRay(ray, pickRadius, maxDistance, out cell, out point);
                if (valid) rotation = grid.transform.rotation;
            }

            ghost.SetActive(valid);
            if (range != null) range.gameObject.SetActive(valid);
            if (!valid) return;
            ghost.transform.SetPositionAndRotation(point, rotation);
            if (range != null) range.transform.position = point;
        }

        public void TryPlace()
        {
            if (selected == null || !valid || Time.frameCount < ignoreUntilFrame) return;
            var eco = Economy.Instance;
            int gemCost = selected.gemCost;
            if (eco == null || !eco.CanAfford(selected.cost) || !eco.CanAffordGems(gemCost)) return;
            eco.TrySpend(selected.cost);
            eco.TrySpendGems(gemCost);

            Tower tower = Instantiate(selected.prefab, point, rotation);
            tower.Init(selected, cell);
            if (selected.prefab.UsesNode && node != null)
            {
                tower.Node = node;
                node.Occupant = tower;
                tower.transform.SetParent(node.transform, true);   // follows/dies with the node, as TowerPlacer did
            }
            else
            {
                grid.SetOccupied(cell, true);
            }

            if (!eco.CanAfford(selected.cost) || !eco.CanAffordGems(gemCost)) Cancel();
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