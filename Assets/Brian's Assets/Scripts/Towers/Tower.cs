using System.Collections.Generic;
using UnityEngine;

namespace Brian
{
    public abstract class Tower : MonoBehaviour
    {
        public static readonly List<Tower> All = new List<Tower>();

        public TowerData Data { get; private set; }
        public int Level { get; private set; } = 1;
        public Vector3Int Cell { get; private set; }
        public int TotalSpent { get; private set; }
        public Buffable Buff { get; private set; }

        public float BaseAttack => Data.attack + Data.attackPerLevel * (Level - 1);
        public float BaseAttackSpeed => Data.attackSpeed + Data.attackSpeedPerLevel * (Level - 1);
        public float BaseRange => Data.range + Data.rangePerLevel * (Level - 1);
        public float Attack => BaseAttack * Buff.AttackMultiplier * AttackBonus;
        public float AttackSpeed => BaseAttackSpeed * Buff.SpeedMultiplier;
        public float Range => BaseRange * Buff.RangeMultiplier;

        public bool CanUpgrade => Level < Data.maxLevel;
        public int UpgradeCost => Mathf.RoundToInt(Data.upgradeCost * Mathf.Pow(Data.upgradeCostGrowth, Level - 1));
        public int SellValue => Mathf.RoundToInt(TotalSpent * Data.refundFraction);

        protected virtual void Awake()
        {
            Buff = GetComponent<Buffable>();
            if (Buff == null) Buff = gameObject.AddComponent<Buffable>();
        }

        protected virtual void OnEnable() => All.Add(this);

        protected virtual void OnDisable() => All.Remove(this);

        public void Init(TowerData data, Vector3Int cell)
        {
            Data = data;
            Cell = cell;
            TotalSpent = data.cost;
            Level = 1;
            OnLevelChanged();
        }

        public bool TryUpgrade()
        {
            if (Data == null || !CanUpgrade || Economy.Instance == null) return false;
            int cost = UpgradeCost;
            if (!Economy.Instance.TrySpend(cost)) return false;
            TotalSpent += cost;
            Level++;
            OnLevelChanged();
            return true;
        }

        public void Sell()
        {
            if (Economy.Instance != null) Economy.Instance.Earn(SellValue);
            if (TowerGrid.Instance != null) TowerGrid.Instance.SetOccupied(Cell, false);
            Destroy(gameObject);
        }

        protected virtual float AttackBonus => 1f;

        protected virtual void OnLevelChanged() { }

        public virtual void FillStats(List<(string label, string value)> rows)
        {
            rows.Add(("Attack", WithBuff(BaseAttack, Attack, "0.#")));
            rows.Add(("Attack speed", WithBuff(BaseAttackSpeed, AttackSpeed, "0.##") + " /s"));
            rows.Add(("Range", WithBuff(BaseRange, Range, "0.##") + " m"));
        }

        protected string WithBuff(float baseValue, float value, string format)
        {
            string s = value.ToString(format);
            if (value > baseValue + 0.001f) s += $"  <color=#9fd7ff>(+{(value / baseValue - 1f) * 100f:0}%)</color>";
            return s;
        }
    }
}
