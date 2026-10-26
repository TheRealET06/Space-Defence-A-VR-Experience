using UnityEngine;

namespace Brian
{
    [CreateAssetMenu(menuName = "Brian/Tower Data")]
    public class TowerData : ScriptableObject
    {
        public string displayName = "Tower";
        public Sprite icon;
        public Tower prefab;
        public int cost = 50;

        [Header("Level 1 stats")]
        public float attack = 10f;
        public float attackSpeed = 1f;
        public float range = 1.5f;

        [Header("Gain per level")]
        public float attackPerLevel = 4f;
        public float attackSpeedPerLevel = 0.2f;
        public float rangePerLevel;

        [Header("Upgrades and selling")]
        public int maxLevel = 5;
        public int upgradeCost = 40;
        public float upgradeCostGrowth = 1.5f;
        [Range(0f, 1f)] public float refundFraction = 0.7f;

        [Header("General Tower (i.e Legacy Bioship)")]
        public Projectile projectilePrefab;
        public float projectileSpeed = 4f;

        [Header("Ophanim")]
        public PulseWave pulsePrefab;
        public float pulseDuration = 0.6f;
        public float buffPercent = 0.2f;
        public float buffPercentPerLevel = 0.05f;
        public float attackPerAlly = 0.33f;
        public int maxAllies = 6;

        [Header("Leviathan")]
        public LeviathanInfant infantPrefab;
        public int infantCount = 2;
        public int infantsPerLevel = 1;
        public float infantAttack = 6f;
        public float infantAttackSpeed = 1.5f;
        public float infantMoveSpeed = 1.2f;
        public float infantMeleeRange = 0.12f;
        public float infantStatPerLevel = 0.15f;
    }
}
