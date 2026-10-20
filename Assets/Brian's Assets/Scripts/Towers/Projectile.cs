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

        public void Launch(Enemy enemy, float dmg, float spd)
        {
            target = enemy;
            damage = dmg;
            speed = spd;
            Destroy(gameObject, 5f);
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
                target.TakeDamage(damage);
                Destroy(gameObject);
                return;
            }

            transform.position += to.normalized * step;
            transform.rotation = Quaternion.LookRotation(to);
        }
    }
}
