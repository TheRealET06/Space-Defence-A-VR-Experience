using System.Collections.Generic;
using UnityEngine;

namespace Brian
{
    public class LeviathanInfant : MonoBehaviour
    {
        static readonly List<Enemy> InRange = new List<Enemy>();

        [SerializeField] float turnSpeed = 8f;
        [SerializeField] float idleRadius = 0.2f;
        [SerializeField] float idleHeight = 0.12f;
        [SerializeField] float retargetInterval = 0.25f;

        LeviathanTower mother;
        Buffable buff;
        Enemy target;
        float cooldown;
        float retarget;

        void Awake()
        {
            buff = GetComponent<Buffable>();
            if (buff == null) buff = gameObject.AddComponent<Buffable>();
        }

        public void Bind(LeviathanTower owner) => mother = owner;

        void Update()
        {
            if (mother == null)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 home = mother.transform.position;
            float range = mother.Range;
            int index = Mathf.Max(0, mother.IndexOf(this));
            int total = Mathf.Max(1, mother.InfantTotal);
            Vector3 slot = Quaternion.Euler(0f, 360f * index / total, 0f) * Vector3.forward;

            retarget -= Time.deltaTime;
            if (retarget <= 0f || target == null || !target.IsAlive || (target.transform.position - home).sqrMagnitude > range * range)
            {
                retarget = retargetInterval;
                target = PickTarget(home, range, index);
            }

            Vector3 goal = target != null
                ? target.transform.position + slot * (mother.InfantMeleeRange * 0.8f)
                : home + slot * idleRadius + Vector3.up * (idleHeight + 0.03f * Mathf.Sin(Time.time * 1.5f + index));

            Vector3 to = goal - transform.position;
            float step = mother.InfantMoveSpeed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, goal, step);

            Vector3 look = target != null ? target.transform.position - transform.position : to;
            if (look.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), turnSpeed * Time.deltaTime);

            cooldown -= Time.deltaTime;
            if (target != null && cooldown <= 0f &&
                (target.transform.position - transform.position).magnitude <= mother.InfantMeleeRange + 0.02f)
            {
                target.TakeDamage(mother.InfantAttack * buff.AttackMultiplier);
                cooldown = 1f / Mathf.Max(0.05f, mother.InfantAttackSpeed * buff.SpeedMultiplier);
            }
        }

        static Enemy PickTarget(Vector3 home, float range, int index)
        {
            InRange.Clear();
            float rangeSqr = range * range;
            foreach (var e in Enemy.Active)
                if (e != null && e.IsAlive && (e.transform.position - home).sqrMagnitude <= rangeSqr)
                    InRange.Add(e);
            if (InRange.Count == 0) return null;
            InRange.Sort((a, b) => (a.transform.position - home).sqrMagnitude.CompareTo((b.transform.position - home).sqrMagnitude));
            return InRange[index % InRange.Count];
        }
    }
}
