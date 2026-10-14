using UnityEngine;

[CreateAssetMenu(menuName = "TD/Tower Data")]
public class TowerData : ScriptableObject
{
    public string displayName;
    public int cost;
    public GameObject towerPrefab;
    public GameObject ghostPrefab;   // semi-transparent, no colliders
    public Sprite icon;
}