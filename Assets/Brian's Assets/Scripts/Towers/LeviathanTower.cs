using System.Collections.Generic;
using UnityEngine;

namespace Brian
{
    public class LeviathanTower : Tower
    {
        public override string Description => Join(Summary, "Upgrade: one more infant, and all infants get stronger.");

        protected override List<(string label, string change)> UpgradePreview() => new List<(string, string)>
        {
            ("Infants", $"+{Data.infantsPerLevel}"),
            ("Infant attack", $"+{Data.infantAttack * Data.infantStatPerLevel:0.#}"),
            ("Infant speed", $"+{Data.infantAttackSpeed * Data.infantStatPerLevel:0.##} /s")
        };

        readonly List<LeviathanInfant> infants = new List<LeviathanInfant>();

        float StatScale => 1f + Data.infantStatPerLevel * (Level - 1);
        public int InfantCount => Data.infantCount + Data.infantsPerLevel * (Level - 1);
        public float InfantAttack => Data.infantAttack * StatScale;
        public float InfantAttackSpeed => Data.infantAttackSpeed * StatScale;
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
