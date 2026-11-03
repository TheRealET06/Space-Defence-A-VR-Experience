using UnityEngine;

/// Marks an object as a slot that a Node-placement tower can be built on.
public class TowerNode : MonoBehaviour
{
    [SerializeField] NodeType nodeType = NodeType.Generic;   // towers must match this
    [SerializeField] Transform snapPoint;                    // optional: where the tower sits

    [HideInInspector] public Vector3Int cell;                // set by a spawner, if any
    public Brian.Tower Occupant { get; set; }                      // set by BuildController, cleared when the tower is destroyed

    public NodeType Type => nodeType;
    public bool HasTower => Occupant != null;
    public Vector3 SnapPoint => snapPoint != null ? snapPoint.position : transform.position;

    // Nodes placed by hand in the scene register themselves with the grid so BuildController can find them.
    void Start()
    {
        if (TowerGrid.Instance != null) TowerGrid.Instance.RegisterNode(this);
    }

    void OnDestroy()
    {
        if (TowerGrid.Instance != null) TowerGrid.Instance.UnregisterNode(this);
    }
}