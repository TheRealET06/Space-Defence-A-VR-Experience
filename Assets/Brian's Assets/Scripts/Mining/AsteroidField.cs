using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Brian
{
    public class AsteroidField : MonoBehaviour
    {
        [Serializable]
        public class Band
        {
            public ResourceAsteroid prefab;
            public float minDistance = 4f;
            public float maxDistance = 7f;
            public int startCount = 2;
            public int cap = 4;
        }

        [SerializeField] Band[] bands;
        [SerializeField] float spawnInterval = 9f;
        [SerializeField] Vector2 size = new Vector2(32f, 32f);
        [SerializeField] float edgeMargin = 1.5f;
        [SerializeField] float floorClearance = 1f;
        [SerializeField] float bandMargin = 0.3f;
        [SerializeField] float minElevation = -10f;
        [SerializeField] float lowestArmAngle = -40f;
        [SerializeField] float spacing = 2.5f;
        [SerializeField] int attempts = 40;

        MiningTower tower;
        Transform arm;
        List<ResourceAsteroid>[] alive;
        readonly List<int> open = new List<int>();
        float timer;

        void Start()
        {
            tower = FindAnyObjectByType<MiningTower>();
            if (tower == null || bands == null || bands.Length == 0)
            {
                Debug.LogError("AsteroidField: needs a Mining Tower in the scene and at least one band.", this);
                enabled = false;
                return;
            }
            arm = FindChild(tower.transform, "Arm");

            alive = new List<ResourceAsteroid>[bands.Length];
            for (int i = 0; i < bands.Length; i++) alive[i] = new List<ResourceAsteroid>();
            for (int i = 0; i < bands.Length; i++)
                for (int n = 0; n < Mathf.Min(bands[i].startCount, bands[i].cap); n++) Spawn(i);
            timer = spawnInterval;
        }

        void Update()
        {
            for (int i = 0; i < bands.Length; i++) alive[i].RemoveAll(a => a == null || a.IsChunk);

            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = spawnInterval;

            open.Clear();
            for (int i = 0; i < bands.Length; i++)
                if (alive[i].Count < bands[i].cap) open.Add(i);
            if (open.Count > 0) Spawn(open[Random.Range(0, open.Count)]);
        }

        void Spawn(int index)
        {
            var band = bands[index];
            if (band.prefab == null) return;
            for (int a = 0; a < attempts; a++)
            {
                if (!TryPick(band, out Vector3 p)) continue;
                alive[index].Add(Instantiate(band.prefab, p, Random.rotation, transform));
                return;
            }
        }

        bool TryPick(Band band, out Vector3 p)
        {
            Vector3 center = tower.RangeCenter;
            float near = band.minDistance + bandMargin;
            float far = band.maxDistance - bandMargin;
            float dist = Mathf.Pow(Random.Range(near * near * near, far * far * far), 1f / 3f);
            float elevation = Mathf.Asin(Random.Range(Mathf.Sin(minElevation * Mathf.Deg2Rad), 1f));
            float azimuth = Random.Range(0f, Mathf.PI * 2f);
            Vector3 dir = new Vector3(Mathf.Cos(elevation) * Mathf.Cos(azimuth), Mathf.Sin(elevation), Mathf.Cos(elevation) * Mathf.Sin(azimuth));
            p = center + dir * dist;

            Vector3 local = p - transform.position;
            if (local.y < floorClearance) return false;
            if (Mathf.Abs(local.x) > size.x * 0.5f - edgeMargin) return false;
            if (Mathf.Abs(local.z) > size.y * 0.5f - edgeMargin) return false;

            if (arm != null)
            {
                Vector3 fromArm = p - arm.position;
                float armElevation = Mathf.Atan2(fromArm.y, new Vector2(fromArm.x, fromArm.z).magnitude) * Mathf.Rad2Deg;
                if (armElevation < lowestArmAngle) return false;
            }

            float spacingSqr = spacing * spacing;
            foreach (var list in alive)
                foreach (var other in list)
                    if (other != null && (other.transform.position - p).sqrMagnitude < spacingSqr) return false;
            return true;
        }

        static Transform FindChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                var found = FindChild(child, name);
                if (found != null) return found;
            }
            return null;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.6f, 0.8f, 1f, 0.4f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * 6f, new Vector3(size.x, 12f, size.y));
        }
    }
}
