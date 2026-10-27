using System.Collections.Generic;
using UnityEngine;

namespace Brian
{
    public class OphanimTower : Tower
    {
        public override string Description => Join(Summary, "Upgrade: stronger pulse and a bigger buff.");

        protected override List<(string label, string change)> UpgradePreview()
        {
            var list = base.UpgradePreview();
            list.Add(("Ally buff", $"+{Data.buffPercentPerLevel * 100f:0}%"));
            return list;
        }

        float cooldown;

        public int Allies { get; private set; }
        protected override float AttackBonus => 1f + Data.attackPerAlly * Mathf.Min(Allies, Data.maxAllies);
        public float BuffPercent => Data.buffPercent + Data.buffPercentPerLevel * (Level - 1);

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
            base.FillStats(rows);
            rows.Add(("Ally buff", $"+{BuffPercent * 100f:0}%"));
            rows.Add(("Allies in range", $"{Allies}  <color=#9fd7ff>(+{Data.attackPerAlly * Mathf.Min(Allies, Data.maxAllies) * 100f:0}% atk)</color>"));
        }
    }
}
