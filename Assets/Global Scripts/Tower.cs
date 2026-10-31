using UnityEngine;
using UnityEngine.Events;

public class Tower : MonoBehaviour
{
    public enum TowerType { Projectile, Laser, Economy }
    public enum PlacementMode { Grid, Node }
    [Header("TowerData Reference")]
    public TowerData data;                      // assign this on every tower prefab

    [Header("Type")]
    public TowerType towerType = TowerType.Projectile;

    [Header("Placement")]
    public PlacementMode placement = PlacementMode.Grid;   // Grid = free cell, Node = spawned node
    public NodeType requiredNodeType = NodeType.Generic;   // only used when placement is Node

    [Header("Targeting (Projectile / Laser)")]
    public float range = 8f;
    public LayerMask enemyLayer;
    public Transform rotatingPart;              // turret head (optional for projectile, falls back to this transform for laser)
    [SerializeField] private float retargetInterval = 0.2f;

    [Header("Shooting (Projectile)")]
    public float fireRate = 1f;                 // shots per second
    public Projectile projectilePrefab;
    public Transform firePoint;                 // used by both projectile and laser

    [Header("Laser")]
    [SerializeField] private LineRenderer laserLine;
    [SerializeField] private float damagePerSecond = 20f;   // applied as dps * deltaTime each frame
    [SerializeField] private float turnSpeed = 360f;        // degrees per second
    [SerializeField] private float fireAngleTolerance = 5f; // must be aimed within this many degrees to fire

    [Header("Economy (non-combat)")]
    [SerializeField] private int incomePerTick = 10;        // money given each payout
    [SerializeField] private float incomeInterval = 5f;     // seconds between payouts
    public UnityEvent<int> onIncome;                        // hook your money manager here (receives the amount)

    [Header("Monitary")]
    [Range(0f, 1f)] public float refundPercent = 0.7f;

    [SerializeField] bool debugTargeting;

    [HideInInspector] public Vector3Int gridCell = new Vector3Int(-1, -1, -1);
    [HideInInspector] public GameObject root;   // set by TowerPlacer
    [HideInInspector] public TowerNode node;   // set by TowerPlacer for Economy towers
    public TowerData Data => data;
    public GameObject Root => root != null ? root : gameObject;
    public int SellValue => Mathf.RoundToInt(data.cost * refundPercent);

    /// <summary>True for tower types that find and attack enemies.</summary>
    public bool IsCombat => towerType != TowerType.Economy;

    private Enemy currentTarget;
    private float retargetTimer;
    private float cooldown;
    private float incomeTimer;

    private Transform Head => rotatingPart != null ? rotatingPart : transform;
    private Transform Muzzle => firePoint != null ? firePoint : transform;

    // ------------------------------------------------------------------ setup

    private void Reset()
    {
        // Auto-fill when the component is first added
        laserLine = GetComponentInChildren<LineRenderer>();
        rotatingPart = transform;
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

    private void Start()
    {
        switch (towerType)
        {
            case TowerType.Projectile:
                if (enemyLayer.value == 0) Debug.LogWarning($"{name}: Enemy Layer is Nothing, so this tower can never find targets", this);
                if (projectilePrefab == null) Debug.LogWarning($"{name}: Projectile Prefab not assigned", this);
                if (firePoint == null) Debug.LogWarning($"{name}: Fire Point not assigned", this);
                break;

            case TowerType.Laser:
                if (enemyLayer.value == 0) Debug.LogWarning($"{name}: Enemy Layer is Nothing, so this tower can never find targets", this);
                if (laserLine == null) Debug.LogWarning($"{name}: Laser Line not assigned", this);
                break;

            case TowerType.Economy:
                if (incomeInterval <= 0f) Debug.LogWarning($"{name}: Income Interval must be above 0", this);
                break;
        }
    }

    private void OnDisable() => SetLaser(false);

    // ------------------------------------------------------------------ update

    private void Update()
    {
        // Non-combat towers skip all targeting
        if (towerType == TowerType.Economy)
        {
            UpdateEconomy();
            return;
        }

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

        if (towerType == TowerType.Laser)
            UpdateLaser();
        else
            UpdateProjectile();
    }

    private void UpdateProjectile()
    {
        cooldown -= Time.deltaTime;
        if (currentTarget == null) return;

        if (rotatingPart != null)
        {
            Vector3 dir = currentTarget.transform.position - rotatingPart.position;
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

    private void UpdateLaser()
    {
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

    // ------------------------------------------------------------------ economy

    private void UpdateEconomy()
    {
        if (incomeInterval <= 0f) return;

        incomeTimer += Time.deltaTime;

        // while-loop so a long frame hitch doesn't swallow payouts
        while (incomeTimer >= incomeInterval)
        {
            incomeTimer -= incomeInterval;
            PayIncome();
        }
    }

    private void PayIncome()
    {
        if (debugTargeting) Debug.Log($"{name}: generated {incomePerTick} money", this);

        // Hook up your money manager through the onIncome event in the Inspector,
        // or call it directly here, e.g. MoneyManager.Instance.Add(incomePerTick);
        onIncome?.Invoke(incomePerTick);
    }

    // ------------------------------------------------------------------ targeting

    private void UpdateTarget()
    {
        // Laser keeps its current target while still valid (prevents jittery switching)
        if (towerType == TowerType.Laser && currentTarget != null && IsValidTarget(currentTarget))
            return;

        Collider[] hits = Physics.OverlapSphere(transform.position, range, enemyLayer);

        float closestSqr = float.MaxValue;
        Enemy closest = null;

        foreach (Collider hit in hits)
        {
            Enemy enemy = hit.GetComponentInParent<Enemy>();   // the collider may be on a child mesh
            if (enemy == null || enemy.IsDead) continue;

            float sqr = (enemy.transform.position - transform.position).sqrMagnitude;
            if (sqr < closestSqr)
            {
                closestSqr = sqr;
                closest = enemy;
            }
        }

        currentTarget = closest;

        if (debugTargeting)
            Debug.Log($"{name}: {hits.Length} colliders within {range}m, target = {(closest ? closest.name : "none")}", this);
    }

    private bool IsValidTarget(Enemy enemy)
    {
        if (enemy == null || enemy.IsDead) return false;
        float sqr = (enemy.transform.position - transform.position).sqrMagnitude;
        return sqr <= range * range;
    }

    // ------------------------------------------------------------------ projectile

    private void Shoot()
    {
        if (debugTargeting) Debug.Log($"{name} firing at {currentTarget.name}", this);

        Projectile p = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        p.Seek(currentTarget.transform);
    }

    // ------------------------------------------------------------------ laser

    private void AimAt(Vector3 worldPoint)
    {
        Transform head = Head;
        Vector3 dir = worldPoint - head.position;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        head.rotation = Quaternion.RotateTowards(head.rotation, targetRot, turnSpeed * Time.deltaTime);
    }

    private bool IsAimedAt(Vector3 worldPoint)
    {
        Transform head = Head;
        Vector3 dir = worldPoint - head.position;
        return Vector3.Angle(head.forward, dir) <= fireAngleTolerance;
    }

    private void FireLaser()
    {
        SetLaser(true);

        if (laserLine != null)
        {
            laserLine.SetPosition(0, Muzzle.position);
            laserLine.SetPosition(1, currentTarget.AimPoint);
        }

        // Frame-rate independent damage: a little bit every Update
        currentTarget.Hurt(damagePerSecond * Time.deltaTime);
    }

    private void SetLaser(bool on)
    {
        if (laserLine != null && laserLine.enabled != on)
            laserLine.enabled = on;
    }

    // ------------------------------------------------------------------ gizmos

    private void OnDrawGizmosSelected()
    {
        if (!IsCombat) return;   // no range circle for non-combat towers

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }

    private void OnDestroy()
    {
        // Frees the node when the tower is sold or destroyed, so you can build on it again
        if (node != null) node.Occupant = null;
    }
}