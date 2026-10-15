using UnityEngine;

public class Tower : MonoBehaviour
{
    [Header("Targeting")]
    public float range = 8f;
    public LayerMask enemyLayer;
    public Transform rotatingPart;      // optional: turret head

    [Header("Shooting")]
    public float fireRate = 1f;          // shots per second
    public Projectile projectilePrefab;
    public Transform firePoint;

    [Header("Monitary")]
    [Range(0f, 1f)] public float refundPercent = 0.7f;

    [HideInInspector] public TowerData data;                      // set by TowerPlacer
    [HideInInspector] public Vector3Int gridCell = new Vector3Int(-1, -1, -1);
    [HideInInspector] public GameObject root;                     // set by TowerPlacer
    public TowerData Data => data;
    public GameObject Root => root != null ? root : gameObject;
    public int SellValue => Mathf.RoundToInt(data.cost * refundPercent);

    Transform target;
    float cooldown;


    

    void Start() => InvokeRepeating(nameof(FindTarget), 0f, 0.2f);

    void FindTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, range, enemyLayer);

        float best = float.MaxValue;
        Transform nearest = null;
        foreach (var c in hits)
        {
            float d = (c.transform.position - transform.position).sqrMagnitude;
            if (d < best) { best = d; nearest = c.transform; }
        }
        target = nearest;
    }

    void Update()
    {
        cooldown -= Time.deltaTime;
        if (target == null) return;

        if (rotatingPart != null)
        {
            Vector3 dir = target.position - rotatingPart.position;
            dir.y = 0f;
            if (dir != Vector3.zero)
                rotatingPart.rotation = Quaternion.Slerp(
                    rotatingPart.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
        }

        if (cooldown <= 0f)
        {
            Shoot();
            cooldown = 1f / fireRate;
        }
    }

    void Shoot()
    {
        Projectile p = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        p.Seek(target);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}