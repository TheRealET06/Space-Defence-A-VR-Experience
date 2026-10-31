using System.Collections.Generic;
using UnityEngine;

namespace Brian
{
    public class LeviathanTower : Tower
    {
        public override string Description => Join(Summary, "Upgrade: one more infant, and all infants get stronger.");

        protected override List<(string label, string change)> UpgradePreview() => new List<(string, string)>
        {
            ("Infants", $"+{InfantsAt(Level + 1) - InfantsAt(Level)}"),
            ("Infant attack", $"+{InfantAttackAt(Level + 1) - InfantAttackAt(Level):0.#}")
        };

        readonly List<LeviathanInfant> infants = new List<LeviathanInfant>();

        bool HasLevelTable => Data.infantsByLevel != null && Data.infantsByLevel.Length > 0;
        float StatScale => HasLevelTable ? 1f : 1f + Data.infantStatPerLevel * (Level - 1);
        public int InfantCount => InfantsAt(Level);
        public float InfantAttack => InfantAttackAt(Level);
        public float InfantAttackSpeed => Data.infantAttackSpeed * StatScale;

        int InfantsAt(int level) => HasLevelTable
            ? Data.infantsByLevel[Mathf.Clamp(level - 1, 0, Data.infantsByLevel.Length - 1)]
            : Data.infantCount + Data.infantsPerLevel * (level - 1);

        float InfantAttackAt(int level) => ByLevel(Data.infantAttackByLevel, level, Data.infantAttack * (1f + Data.infantStatPerLevel * (level - 1)));
        public float InfantMoveSpeed => Data.infantMoveSpeed;
        public float InfantMeleeRange => Data.infantMeleeRange;
        public int InfantTotal => infants.Count;
        public int IndexOf(LeviathanInfant infant) => infants.IndexOf(infant);

        protected override void OnLevelChanged()
        {
            if (Data == null || Data.infantPrefab == null) return;
            infants.RemoveAll(i => i == null);
            while (infants.Count < InfantCount)
            {
                Vector3 pos = transform.position + Random.insideUnitSphere * 0.1f;
                LeviathanInfant infant = Instantiate(Data.infantPrefab, pos, Quaternion.identity);
                infant.Bind(this);
                infants.Add(infant);
            }
        }

        void OnDestroy()
        {
            foreach (var i in infants)
                if (i != null) Destroy(i.gameObject);
        }

        public override void FillStats(List<(string label, string value)> rows)
        {
            rows.Add(("Range", WithBuff(BaseRange, Range, "0.##") + " m"));
            rows.Add(("Infants", InfantCount.ToString()));
            rows.Add(("Infant attack", InfantAttack.ToString("0.#")));
            rows.Add(("Infant speed", InfantAttackSpeed.ToString("0.##") + " /s"));
        }
    }
}
