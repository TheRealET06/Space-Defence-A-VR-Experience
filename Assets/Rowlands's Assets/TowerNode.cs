using UnityEngine;

/// Marks a spawned object as a slot that a Node-placement tower can be built on.
public class TowerNode : MonoBehaviour
{
    [SerializeField] NodeType nodeType = NodeType.Generic;   // towers must match this
    [SerializeField] Transform snapPoint;                    // optional: where the tower sits

    [HideInInspector] public Vector3Int cell;                // set by TowerGridSpawner
    public Tower Occupant { get; set; }                      // set by TowerPlacer, cleared when the tower is destroyed

    public NodeType Type => nodeType;
    public bool HasTower => Occupant != null;
    public Vector3 SnapPoint => snapPoint != null ? snapPoint.position : transform.position;
}