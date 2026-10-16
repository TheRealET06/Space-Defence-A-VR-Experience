using UnityEngine;

/// <summary>
/// Tower that finds the nearest enemy in range, rotates toward it,
/// and fires a continuous laser that deals damage every frame.
/// </summary>
public class LaserTower : MonoBehaviour
{
    [Header("TowerData Reference")]
    public TowerData data;                      // assign this on every tower prefab

    [Header("Targeting")]
    [SerializeField] private float range = 12f;
    [SerializeField] private LayerMask enemyLayer;          // Put enemies on their own layer
    [SerializeField] private float retargetInterval = 0.2f; // How often to search for a new target

    [Header("Rotation")]
    [SerializeField] private Transform turretHead;          // The part that rotates (e.g. the barrel/head)
    [SerializeField] private float turnSpeed = 360f;        // Degrees per second
    [SerializeField] private float fireAngleTolerance = 5f; // Must be aimed within this many degrees to fire

    [Header("Laser")]
    [SerializeField] private Transform firePoint;           // Where the laser starts (child of turretHead)
    [SerializeField] private LineRenderer laserLine;
    [SerializeField] private float damagePerSecond = 20f;   // Applied as dps * deltaTime each frame

    [Header("Monitary")]
    [Range(0f, 1f)] public float refundPercent = 0.7f;

    // [HideInInspector] public TowerData data;                      // set by TowerPlacer
    [HideInInspector] public Vector3Int gridCell = new Vector3Int(-1, -1, -1);
    [HideInInspector] public GameObject root;                     // set by TowerPlacer
    public TowerData Data => data;
    public GameObject Root => root != null ? root : gameObject;
    public int SellValue => Mathf.RoundToInt(data.cost * refundPercent);

    private Enemystats currentTarget;
    private float retargetTimer;

    private void Reset()
    {
        // Auto-fill when the component is first added
        laserLine = GetComponentInChildren<LineRenderer>();
        turretHead = transform;
        firePoint = transform;
    }

    private void Awake()
    {
        if (laserLine != null)
        {
            laserLine.positionCount = 2;
            laserLine.enabled = false;
        }
    }

    private void Update()
    {
        // Periodically look for a target (cheaper than every frame)
        retargetTimer -= Time.deltaTime;
        if (retargetTimer <= 0f)
        {
            retargetTimer = retargetInterval;
            UpdateTarget();
        }

        // Drop target if it died or left range
        if (currentTarget != null && !IsValidTarget(currentTarget))
            currentTarget = null;

        if (currentTarget == null)
        {
            SetLaser(false);
            return;
        }

        AimAt(currentTarget.AimPoint);

        if (IsAimedAt(currentTarget.AimPoint))
            FireLaser();
        else
            SetLaser(false);
    }

    private void UpdateTarget()
    {
        // Keep current target while still valid (prevents jittery switching)
        if (currentTarget != null && IsValidTarget(currentTarget))
            return;

        Collider[] hits = Physics.OverlapSphere(transform.position, range, enemyLayer);

        float closestSqr = float.MaxValue;
        Enemystats closest = null;

        foreach (Collider hit in hits)
        {
            Enemystats enemy = hit.GetComponentInParent<Enemystats>();
            if (enemy == null || enemy.IsDead) continue;

            float sqr = (enemy.transform.position - transform.position).sqrMagnitude;
            if (sqr < closestSqr)
            {
                closestSqr = sqr;
                closest = enemy;
            }
        }

        currentTarget = closest;
    }

    private bool IsValidTarget(Enemystats enemy)
    {
        if (enemy == null || enemy.IsDead) return false;
        float sqr = (enemy.transform.position - transform.position).sqrMagnitude;
        return sqr <= range * range;
    }

    private void AimAt(Vector3 worldPoint)
    {
        Vector3 dir = worldPoint - turretHead.position;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        turretHead.rotation = Quaternion.RotateTowards(
            turretHead.rotation, targetRot, turnSpeed * Time.deltaTime);
    }

    private bool IsAimedAt(Vector3 worldPoint)
    {
        Vector3 dir = worldPoint - turretHead.position;
        return Vector3.Angle(turretHead.forward, dir) <= fireAngleTolerance;
    }

    private void FireLaser()
    {
        SetLaser(true);

        Vector3 start = firePoint.position;
        Vector3 end = currentTarget.AimPoint;

        if (laserLine != null)
        {
            laserLine.SetPosition(0, start);
            laserLine.SetPosition(1, end);
        }

        // Frame-rate independent damage: a little bit every Update
        currentTarget.Hurt(damagePerSecond * Time.deltaTime);
    }

    private void SetLaser(bool on)
    {
        if (laserLine != null && laserLine.enabled != on)
            laserLine.enabled = on;
    }

    private void OnDisable() => SetLaser(false);

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
