using UnityEngine;
using UnityEngine.InputSystem;

public class TowerPlacer : MonoBehaviour
{
    [SerializeField] TowerGrid grid;
    [SerializeField] Transform rayOrigin;
    [SerializeField] InputActionReference placeAction;
    [SerializeField] InputActionReference cancelAction;   // optional: deselect tower
    [SerializeField] CurrencyManager currency;
    [SerializeField] HandMenu menu;

    [Header("Aiming")]
    [SerializeField] float pickRadius = 0.08f;
    [SerializeField] float maxDistance = 10f;

    TowerData selected;
    bool selectedNeedsNode;          // read from the prefab's Tower.placement
    NodeType selectedNodeType;       // read from the prefab's Tower.requiredNodeType
    GameObject ghost;
    Vector3Int currentCell;
    Vector3 currentPoint;
    TowerNode currentNode;
    bool hasValidPoint;

    void OnEnable()
    {
        placeAction.action.performed += OnPlace;
        if (cancelAction != null) cancelAction.action.performed += OnCancel;
    }
    void OnDisable()
    {
        placeAction.action.performed -= OnPlace;
        if (cancelAction != null) cancelAction.action.performed -= OnCancel;
    }

    public void SelectTower(TowerData data)
    {
        selected = data;

        Tower prefabTower = data.towerPrefab.GetComponentInChildren<Tower>();
        selectedNeedsNode = prefabTower != null && prefabTower.placement == Tower.PlacementMode.Node;
        selectedNodeType = prefabTower != null ? prefabTower.requiredNodeType : NodeType.Generic;

        if (ghost) Destroy(ghost);
        ghost = Instantiate(data.ghostPrefab);
        foreach (var c in ghost.GetComponentsInChildren<Collider>()) c.enabled = false; //fix????
        ghost.SetActive(false);
    }

    void OnCancel(InputAction.CallbackContext _) => ClearSelection();

    void ClearSelection()
    {
        selected = null;
        selectedNeedsNode = false;
        currentNode = null;
        hasValidPoint = false;
        if (ghost) Destroy(ghost);
    }

    void Update()
    {
        if (UnityEngine.InputSystem.XR.XRController.leftHand != null &&
            UnityEngine.InputSystem.XR.XRController.leftHand.IsPressed())
            Debug.Log("Left hand active");

        if (selected == null || menu.IsOpen) { if (ghost) ghost.SetActive(false); hasValidPoint = false; return; }

        var ray = new Ray(rayOrigin.position, rayOrigin.forward);

        currentNode = null;
        if (selectedNeedsNode)
        {
            // Snap onto a free spawned node of the matching type
            hasValidPoint = grid.TryGetFreeNodeAlongRay(ray, pickRadius, maxDistance, selectedNodeType, out currentNode);
            if (hasValidPoint) currentPoint = currentNode.SnapPoint;
        }
        else
        {
            hasValidPoint = grid.TryGetFreePointAlongRay(ray, pickRadius, maxDistance, out currentCell, out currentPoint);
        }

        ghost.SetActive(hasValidPoint);
        if (hasValidPoint)
            ghost.transform.SetPositionAndRotation(currentPoint, grid.transform.rotation);
    }

    void OnPlace(InputAction.CallbackContext _)
    {
        if (selected == null || !hasValidPoint) return;
        if (menu.IsOpen || Time.frameCount - menu.LastClosedFrame < 3) return; // the click that picked the tower
        if (!currency.TrySpend(selected.cost)) return;                          // not enough money

        GameObject obj = Instantiate(selected.towerPrefab, currentPoint, grid.transform.rotation);
        Tower tower = obj.GetComponentInChildren<Tower>();   // also finds it on the root itself
        tower.data = selected;
        tower.root = obj;

        if (selectedNeedsNode)
        {
            // The node's grid cell is already occupied; just claim the node
            tower.node = currentNode;
            //currentNode.Occupant = tower;
            obj.transform.SetParent(currentNode.transform, true);   // follows/dies with the node
        }
        else
        {
            tower.gridCell = currentCell;
            grid.SetOccupied(currentCell, true);
        }

        if (!currency.CanAfford(selected.cost)) ClearSelection(); // optional: auto-deselect when broke
    }
}