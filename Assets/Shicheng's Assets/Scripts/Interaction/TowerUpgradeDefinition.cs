using System;
using UnityEngine;

[Serializable]
public class TowerUpgradeDefinition
{
    public TowerActionType action;

    public string displayName;

    [Min(0)]
    public int cost;
}