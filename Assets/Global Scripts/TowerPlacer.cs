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
    GameObject ghost;
    Vector3Int currentCell;
    Vector3 currentPoint;
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
        // TowerSelector.SelectionBlocked = true;
        if (ghost) Destroy(ghost);
        ghost = Instantiate(data.ghostPrefab);
        foreach (var c in ghost.GetComponentsInChildren<Collider>()) c.enabled = false; //fix????
        ghost.SetActive(false);
    }

    void OnCancel(InputAction.CallbackContext _) => ClearSelection();

    void ClearSelection()
    {
        selected = null;
       // TowerSelector.SelectionBlocked = false;
        hasValidPoint = false;
        if (ghost) Destroy(ghost);
    }

    void Update()
    {

        if (UnityEngine.InputSystem.XR.XRController.leftHand != null &&
    UnityEngine.InputSystem.XR.XRController.leftHand.IsPressed())
            Debug.Log("Left hand active");


        if (selected == null || menu.IsOpen) { if (ghost) ghost.SetActive(false); hasValidPoint = false; return; }

        Transform origin = rayOrigin;
        var ray = new Ray(rayOrigin.position, rayOrigin.forward);

        hasValidPoint = grid.TryGetFreePointAlongRay(ray, pickRadius, maxDistance,out currentCell, out currentPoint);
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
        tower.gridCell = currentCell;
        tower.root = obj;
        grid.SetOccupied(currentCell, true);

        if (!currency.CanAfford(selected.cost)) ClearSelection(); // optional: auto-deselect when broke
    }
}