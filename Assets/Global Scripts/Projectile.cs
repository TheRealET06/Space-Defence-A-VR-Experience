using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 15f;
    public float damage = 25f;
    public float lifetime = 5f;

    Transform target;

    public void Seek(Transform t) => target = t;

    void Start() => Destroy(gameObject, lifetime);

    void Update()
    {
        // Target died before we arrived
        if (target == null) { Destroy(gameObject); return; }

        Vector3 dir = target.position - transform.position;
        float step = speed * Time.deltaTime;

        if (dir.magnitude <= step)
        {
            Hit();
            return;
        }

        transform.position += dir.normalized * step;
        transform.rotation = Quaternion.LookRotation(dir);
    }

    void Hit()
    {
        if (target.TryGetComponent(out Enemy enemy))
            enemy.TakeDamage(damage);
        Destroy(gameObject);
    }
}