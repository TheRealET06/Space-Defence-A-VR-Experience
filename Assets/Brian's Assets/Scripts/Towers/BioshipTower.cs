using UnityEngine;

namespace Brian
{
    public class BioshipTower : Tower
    {
        [SerializeField] Transform firePoint;
        [SerializeField] float turnSpeed = 6f;

        float cooldown;

        void Update()
        {
            if (Data == null) return;
            cooldown -= Time.deltaTime;

            Enemy target = Enemy.NearestInRange(transform.position, transform.position, Range);
            if (target == null) return;

            Vector3 dir = target.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);

            if (cooldown > 0f) return;
            cooldown = 1f / Mathf.Max(0.05f, AttackSpeed);
            Fire(target);
        }

        void Fire(Enemy target)
        {
            if (Data.projectilePrefab == null)
            {
                target.TakeDamage(Attack);
                return;
            }
            Vector3 from = firePoint != null ? firePoint.position : transform.position;
            Projectile p = Instantiate(Data.projectilePrefab, from, Quaternion.identity);
            p.Launch(target, Attack, Data.projectileSpeed);
        }
    }
}
