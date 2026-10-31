using System.Collections.Generic;
using UnityEngine;

namespace Brian
{
    public class Enemy : MonoBehaviour
    {
        public static readonly List<Enemy> Active = new List<Enemy>();

        public static Enemy NearestInRange(Vector3 from, Vector3 center, float range)
        {
            Enemy best = null;
            float bestSqr = float.MaxValue;
            float rangeSqr = range * range;
            foreach (var e in Active)
            {
                if (e == null || !e.IsAlive) continue;
                Vector3 p = e.transform.position;
                if ((p - center).sqrMagnitude > rangeSqr) continue;
                float d = (p - from).sqrMagnitude;
                if (d < bestSqr)
                {
                    bestSqr = d;
                    best = e;
                }
            }
            return best;
        }

        public static Enemy FirstInRange(Vector3 center, float range)
        {
            Enemy best = null;
            float bestProgress = float.MinValue;
            float rangeSqr = range * range;
            foreach (var e in Active)
            {
                if (e == null || !e.IsAlive) continue;
                if ((e.transform.position - center).sqrMagnitude > rangeSqr) continue;
                if (e.Progress > bestProgress)
                {
                    bestProgress = e.Progress;
                    best = e;
                }
            }
            return best;
        }

        [SerializeField] bool faceDirection = true;
        [SerializeField] float turnSpeed = 360f;

        public EnemyData Data { get; private set; }
        public float MaxHealth { get; private set; }
        public float Health { get; private set; }
        public float HealthFraction => MaxHealth > 0f ? Mathf.Clamp01(Health / MaxHealth) : 0f;
        public bool IsAlive => Health > 0f;
        public float Progress { get; private set; }
        public float Speed => Data == null ? 0f : Data.maxSpeed * Mathf.Lerp(Data.minSpeedFraction, 1f, HealthFraction);

        List<Vector3> waypoints;
        Vector3 up = Vector3.up;
        int next;

        public void Init(EnemyData data, List<Vector3> path, Vector3 upDirection, float healthMultiplier)
        {
            Data = data;
            NonBlocking.MakeTriggers(gameObject);
            MaxHealth = data.maxHealth * healthMultiplier;
            Health = MaxHealth;
            waypoints = path;
            up = upDirection;
            next = 1;
            Progress = 0f;
            transform.position = path[0];
        }

        void OnEnable() => Active.Add(this);

        void OnDisable() => Active.Remove(this);

        void Update()
        {
            if (waypoints == null || !IsAlive) return;

            float remaining = Speed * Time.deltaTime;
            while (remaining > 0f && next < waypoints.Count)
            {
                Vector3 target = waypoints[next];
                float dist = Vector3.Distance(transform.position, target);
                if (dist <= remaining)
                {
                    transform.position = target;
                    Progress += dist;
                    remaining -= dist;
                    next++;
                }
                else
                {
                    transform.position = Vector3.MoveTowards(transform.position, target, remaining);
                    Progress += remaining;
                    remaining = 0f;
                }
            }

            if (next >= waypoints.Count)
            {
                ReachHome();
                return;
            }

            if (faceDirection)
            {
                Vector3 dir = waypoints[next] - transform.position;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    Quaternion want = Quaternion.LookRotation(dir.normalized, up);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.deltaTime);
                }
            }
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || amount <= 0f) return;
            Health -= amount;
            if (Health <= 0f) Die();
        }

        void Die()
        {
            Health = 0f;
            Destroy(gameObject);
        }

        void ReachHome()
        {
            Health = 0f;
            if (Home.Instance != null) Home.Instance.Damage(Data.homeDamage);
            Destroy(gameObject);
        }
    }
}
