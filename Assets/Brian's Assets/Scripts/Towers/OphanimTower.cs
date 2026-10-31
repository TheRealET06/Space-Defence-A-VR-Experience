using System.Collections.Generic;
using UnityEngine;

namespace Brian
{
    public class OphanimTower : Tower
    {
        public override string Description => Join(Summary, "Upgrade: stronger pulse and a bigger buff.");

        protected override List<(string label, string change)> UpgradePreview() => new List<(string, string)>
        {
            ("Pulse damage", $"+{AttackAt(Level + 1) - AttackAt(Level):0.#}"),
            ("Ally buff", $"+{(BuffAt(Level + 1) - BuffAt(Level)) * 100f:0}%")
        };

        float cooldown;

        public int Allies { get; private set; }
        public float BuffPercent => BuffAt(Level);

        float BuffAt(int level) => ByLevel(Data.buffByLevel, level, Data.buffPercent + Data.buffPercentPerLevel * (level - 1));

        void Update()
        {
            if (Data == null) return;
            cooldown -= Time.deltaTime;
            if (cooldown > 0f) return;
            cooldown = 1f / Mathf.Max(0.05f, AttackSpeed);
            Allies = CountAllies(Range);
            Pulse();
        }

        void Pulse()
        {
            float range = Range;
            Vector3 center = transform.position;

            if (Data.pulsePrefab != null)
            {
                PulseWave wave = Instantiate(Data.pulsePrefab, center, Quaternion.identity);
                wave.Run(range, Attack, Data.pulseDuration);
            }
            else
            {
                foreach (var e in Enemy.Active.ToArray())
                    if (e != null && e.IsAlive && (e.transform.position - center).sqrMagnitude <= range * range)
                        e.TakeDamage(Attack);
            }

            float duration = 1f / Mathf.Max(0.05f, AttackSpeed) + 0.5f;
            float rangeSqr = range * range;
            foreach (var b in Buffable.All)
                if (b != null && b != Buff && (b.transform.position - center).sqrMagnitude <= rangeSqr)
                    b.Apply(BuffPercent, duration);
        }

        int CountAllies(float range)
        {
            int n = 0;
            float rangeSqr = range * range;
            foreach (var t in All)
                if (t != null && t != this && (t.transform.position - transform.position).sqrMagnitude <= rangeSqr)
                    n++;
            return n;
        }

        public override void FillStats(List<(string label, string value)> rows)
        {
            rows.Add(("Pulse damage", BaseAttack.ToString("0.#")));
            rows.Add(("Pulse every", $"{1f / Mathf.Max(0.05f, AttackSpeed):0.#} s"));
            rows.Add(("Range", BaseRange.ToString("0.##") + " m"));
            rows.Add(("Ally buff", $"+{BuffPercent * 100f:0}% damage"));
            rows.Add(("Towers buffed", Allies.ToString()));
        }
    }
}
