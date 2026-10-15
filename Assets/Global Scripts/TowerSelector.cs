using UnityEngine;
using UnityEngine.InputSystem;

// Put this on the right-hand controller (or anywhere) and assign the fields.
// Debug logs tell you exactly where selection stops. Turn them off when it works.
public class TowerSelector : MonoBehaviour
{
    [SerializeField] Transform rayOrigin;               // the controller's aim transform
    [SerializeField] InputActionReference selectAction; // your B button action
    [SerializeField] LayerMask towerMask;               // only the "Tower" layer
    [SerializeField] float maxDistance = 20f;
    [SerializeField] TowerInfoMenu menu;                // must be the SCENE object, not the prefab asset
    [SerializeField] bool debugLogs = true;

    void OnEnable() { selectAction.action.Enable(); selectAction.action.performed += OnSelect; }
    void OnDisable() { selectAction.action.performed -= OnSelect; }

    void Update()
    {
        if (debugLogs) Debug.DrawRay(rayOrigin.position, rayOrigin.forward * maxDistance, Color.green);  // Scene view
    }

    void Log(string msg) { if (debugLogs) Debug.Log("[TowerSelector] " + msg, this); }

    void OnSelect(InputAction.CallbackContext ctx)
    {
        Log("button pressed");

        if (!Physics.Raycast(rayOrigin.position, rayOrigin.forward, out var hit, maxDistance, towerMask))
        {
            Log("ray hit nothing on towerMask (check mask, layer, collider, aim)");
            return;
        }
        Log($"hit '{hit.collider.name}' on layer '{LayerMask.LayerToName(hit.collider.gameObject.layer)}'");

        Tower tower = hit.collider.GetComponentInParent<Tower>();
        if (!tower) tower = hit.collider.GetComponentInChildren<Tower>();
        if (!tower) { Log("hit collider has no Tower above or below it"); return; }

        if (tower.Data == null) { Debug.LogError("[TowerSelector] Tower.data is null: TowerPlacer isn't assigning tower.data = selected", this); return; }

        Log("showing menu");
        menu.Show(tower);
    }
}