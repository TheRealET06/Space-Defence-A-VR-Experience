using System.Collections.Generic;
using UnityEngine;

namespace Brian
{
    public class Buffable : MonoBehaviour
    {
        public static readonly List<Buffable> All = new List<Buffable>();

        float percent;
        float until;

        public float Percent => Time.time < until ? percent : 0f;
        public float AttackMultiplier => 1f + Percent;
        public float SpeedMultiplier => 1f;
        public float RangeMultiplier => 1f;

        void OnEnable() => All.Add(this);

        void OnDisable() => All.Remove(this);

        public void Apply(float buffPercent, float duration)
        {
            bool active = Time.time < until;
            if (!active || buffPercent > percent)
            {
                percent = buffPercent;
                until = Time.time + duration;
            }
            else if (Mathf.Approximately(buffPercent, percent))
            {
                until = Mathf.Max(until, Time.time + duration);
            }
        }
    }
}
