using UnityEngine;
using UnityEngine.InputSystem;
 // XRI 2.x; in XRI 3.x you may also need ...Interactors

/// Aim a controller ray at the 3D grid; a ghost tower snaps to the first free
/// point the ray passes near, and the place action spawns the tower there.
public class TowerPlacer : MonoBehaviour
{
    [SerializeField] TowerGrid grid;
    [SerializeField] UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor rayInteractor;
    [SerializeField] InputActionReference placeAction;
    [SerializeField] GameObject towerPrefab;
    [SerializeField] GameObject ghostPrefab;   // semi-transparent, no colliders

    [Header("Aiming")]
    [SerializeField] float pickRadius = 0.08f; // how close the ray must pass to a point (m)
    [SerializeField] float maxDistance = 10f;

    GameObject ghost;
    Vector3Int currentCell;
    Vector3 currentPoint;
    bool hasValidPoint;

    void OnEnable()  => placeAction.action.performed += OnPlace;
    void OnDisable() => placeAction.action.performed -= OnPlace;

    void Start()
    {
        ghost = Instantiate(ghostPrefab);
        ghost.SetActive(false);
    }

    void Update()
    {
        Transform origin = rayInteractor.rayOriginTransform;
        Ray ray = new Ray(origin.position, origin.forward);

        hasValidPoint = grid.TryGetFreePointAlongRay(ray, pickRadius, maxDistance,
                                                     out currentCell, out currentPoint);

        ghost.SetActive(hasValidPoint);
        if (hasValidPoint)
            ghost.transform.SetPositionAndRotation(currentPoint, grid.transform.rotation);
    }

    void OnPlace(InputAction.CallbackContext _)
    {
        if (!hasValidPoint) return;

        Instantiate(towerPrefab, currentPoint, grid.transform.rotation);
        grid.SetOccupied(currentCell, true);
    }
}
