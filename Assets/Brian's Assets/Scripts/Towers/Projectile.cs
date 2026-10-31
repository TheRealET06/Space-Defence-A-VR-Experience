using UnityEngine;

namespace Brian
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] float trailTime = 0.15f;
        [SerializeField] float trailWidth = 0.035f;

        Enemy target;
        float damage;
        float speed;
        float splash;
        PulseWave splashPrefab;
        float splashDuration;

        void Awake()
        {
            var r = GetComponentInChildren<Renderer>();
            if (r == null || GetComponent<TrailRenderer>() != null) return;
            var trail = gameObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = r.sharedMaterial;
            trail.time = trailTime;
            trail.minVertexDistance = 0.01f;
            trail.widthCurve = AnimationCurve.Linear(0f, trailWidth, 1f, 0f);
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
        }

        public void Launch(Enemy enemy, float dmg, float spd, float splashRadius = 0f, PulseWave splashEffect = null, float splashTime = 0.2f)
        {
            target = enemy;
            damage = dmg;
            speed = spd;
            splash = splashRadius;
            splashPrefab = splashEffect;
            splashDuration = splashTime;
            Destroy(gameObject, 5f);
        }

        void Impact()
        {
            if (splash <= 0f)
            {
                target.TakeDamage(damage);
                return;
            }
            Vector3 center = target.transform.position;
            if (splashPrefab != null)
            {
                Instantiate(splashPrefab, center, Quaternion.identity).Run(splash, damage, splashDuration);
                return;
            }
            float rSqr = splash * splash;
            foreach (var e in Enemy.Active.ToArray())
                if (e != null && e.IsAlive && (e.transform.position - center).sqrMagnitude <= rSqr)
                    e.TakeDamage(damage);
        }

        void Update()
        {
            if (target == null || !target.IsAlive)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 to = target.transform.position - transform.position;
            float step = speed * Time.deltaTime;
            if (to.magnitude <= step)
            {
                Impact();
                Destroy(gameObject);
                return;
            }

            transform.position += to.normalized * step;
            transform.rotation = Quaternion.LookRotation(to);
        }
    }
}
