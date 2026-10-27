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
            if (!BlocksPlayer) NonBlocking.MakeTriggers(gameObject);
        }

        protected virtual bool BlocksPlayer => false;

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

        public virtual Vector3 RangeCenter => transform.position;
        public virtual float MaxSelectDistance => float.PositiveInfinity;
        public string Summary => Data != null ? Data.description : "";
        public virtual string Description => Join(Summary, "Upgrade: more attack and attack speed.");

        protected static string Join(string a, string b) => string.IsNullOrEmpty(a) ? b : $"{a}\n{b}";

        protected virtual List<(string label, string change)> UpgradePreview()
        {
            var list = new List<(string, string)>
            {
                ("Attack", $"+{Data.attackPerLevel * Buff.AttackMultiplier * AttackBonus:0.#}"),
                ("Attack speed", $"+{Data.attackSpeedPerLevel * Buff.SpeedMultiplier:0.##} /s")
            };
            if (Data.rangePerLevel > 0f) list.Add(("Range", $"+{Data.rangePerLevel * Buff.RangeMultiplier:0.##} m"));
            return list;
        }

        public virtual Vector3 PanelAnchor(Vector3 eye, out bool centered)
        {
            centered = false;
            return transform.position + Vector3.up * 0.35f;
        }

        public virtual void FillActions(List<TowerAction> actions)
        {
            var eco = Economy.Instance;
            if (CanUpgrade) actions.Add(new TowerAction($"Upgrade  ${UpgradeCost}", eco != null && eco.CanAfford(UpgradeCost), () =>
            {
                if (eco != null && !eco.CanAfford(UpgradeCost)) PanelMessage($"Can't afford: upgrade costs ${UpgradeCost},\nyou have ${eco.Money}.");
                else TryUpgrade();
            }, UpgradePreview()));
            else actions.Add(new TowerAction("Max level", false, null));
            actions.Add(new TowerAction($"Sell  +${SellValue}", true, () =>
            {
                Sell();
                if (TowerPanel.Instance != null) TowerPanel.Instance.Hide();
            }));
        }

        protected virtual void OnLevelChanged() { }

        protected void PanelMessage(string text, bool error = true)
        {
            if (TowerPanel.Instance != null && TowerPanel.Instance.Current == this) TowerPanel.Instance.Notify(text, error);
        }

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

    public readonly struct TowerAction
    {
        public readonly string label;
        public readonly bool enabled;
        public readonly System.Action run;
        public readonly List<(string label, string change)> preview;

        public TowerAction(string label, bool enabled, System.Action run, List<(string label, string change)> preview = null)
        {
            this.label = label;
            this.enabled = enabled;
            this.run = run;
            this.preview = preview;
        }
    }
}
