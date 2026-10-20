using UnityEngine;

namespace Brian
{
    public class Meteor : MonoBehaviour
    {
        [SerializeField] int reward = 10;
        [SerializeField] float spinSpeed = 20f;
        [SerializeField] float popInTime = 0.4f;

        public Vector3Int Cell { get; private set; }
        public int Reward => reward;

        MeteorField field;
        Vector3 axis;
        Vector3 baseScale;
        float age;
        bool collected;

        void Awake()
        {
            baseScale = transform.localScale;
            axis = Random.onUnitSphere;
            if (popInTime > 0f) transform.localScale = Vector3.zero;
        }

        public void Place(MeteorField owner, Vector3Int cell)
        {
            field = owner;
            Cell = cell;
        }

        void Update()
        {
            if (age < popInTime)
            {
                age += Time.deltaTime;
                transform.localScale = baseScale * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(age / popInTime));
            }
            transform.Rotate(axis, spinSpeed * Time.deltaTime, Space.World);
        }

        public void Collect()
        {
            if (collected) return;
            collected = true;
            if (Economy.Instance != null) Economy.Instance.Earn(reward);
            if (field != null) field.Release(this);
            Destroy(gameObject);
        }
    }
}
