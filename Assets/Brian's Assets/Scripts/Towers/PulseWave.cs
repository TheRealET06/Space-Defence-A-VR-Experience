using System.Collections.Generic;
using UnityEngine;

namespace Brian
{
    public class PulseWave : MonoBehaviour
    {
        readonly HashSet<Enemy> hit = new HashSet<Enemy>();
        readonly List<Enemy> toHit = new List<Enemy>();

        float maxRadius;
        float damage;
        float duration = 0.6f;
        float t;
        Renderer rend;
        MaterialPropertyBlock block;
        Color baseColor = Color.white;
        string colorProp;

        void Awake()
        {
            rend = GetComponentInChildren<Renderer>();
            block = new MaterialPropertyBlock();
            if (rend != null && rend.sharedMaterial != null)
            {
                foreach (string p in new[] { "_BaseColor", "_Color" })
                    if (rend.sharedMaterial.HasProperty(p))
                    {
                        colorProp = p;
                        baseColor = rend.sharedMaterial.GetColor(p);
                        break;
                    }
            }
            transform.localScale = Vector3.zero;
        }

        public void Run(float radius, float dmg, float dur)
        {
            maxRadius = radius;
            damage = dmg;
            duration = Mathf.Max(0.05f, dur);
        }

        void Update()
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float r = maxRadius * k;
            transform.localScale = Vector3.one * r * 2f;

            toHit.Clear();
            float rSqr = r * r;
            foreach (var e in Enemy.Active)
                if (e != null && e.IsAlive && !hit.Contains(e) && (e.transform.position - transform.position).sqrMagnitude <= rSqr)
                    toHit.Add(e);
            foreach (var e in toHit)
            {
                hit.Add(e);
                e.TakeDamage(damage);
            }

            if (rend != null && colorProp != null)
            {
                Color c = baseColor;
                c.a *= (1f - k) * (1f - k);
                rend.GetPropertyBlock(block);
                block.SetColor(colorProp, c);
                rend.SetPropertyBlock(block);
            }

            if (k >= 1f) Destroy(gameObject);
        }
    }
}
