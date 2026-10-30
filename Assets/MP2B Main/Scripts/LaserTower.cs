using System.Collections.Generic;
using UnityEngine;

namespace Brian
{
    /// <summary>
    /// Tower that finds the nearest enemy in range, rotates toward it,
    /// and fires a continuous laser that deals damage every frame.
    /// Inherits data, levels, buffs, upgrade/sell from Tower.
    /// </summary>
    public class LaserTower : Tower
    {
        [Header("Targeting")]
        [SerializeField] private float retargetInterval = 0.2f; // How often to search for a new target

        [Header("Rotation")]
        [SerializeField] private Transform turretHead;          // The part that rotates (e.g. the barrel/head)
        [SerializeField] private float turnSpeed = 360f;        // Degrees per second
        [SerializeField] private float fireAngleTolerance = 5f; // Must be aimed within this many degrees to fire

        [Header("Laser")]
        [SerializeField] private Transform firePoint;           // Where the laser starts (child of turretHead)
        [SerializeField] private LineRenderer laserLine;

        private Enemy currentTarget;
        private float retargetTimer;
        private double gasFuel;                                 // gas already paid for but not yet burned

        private void Reset()
        {
            // Auto-fill when the component is first added
            laserLine = GetComponentInChildren<LineRenderer>();
            turretHead = transform;
            firePoint = transform;
        }

        protected override void Awake()
        {
            base.Awake();
            if (turretHead == null) turretHead = transform;
            if (firePoint == null) firePoint = turretHead;
            if (laserLine != null)
            {
                laserLine.positionCount = 2;
                laserLine.enabled = false;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SetLaser(false);
        }

        private void Update()
        {
            if (Data == null) return;

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

            Vector3 aimPoint = currentTarget.transform.position;
            AimAt(aimPoint);

            if (IsAimedAt(aimPoint) && TryUseGas(Time.deltaTime))
                FireLaser(aimPoint);
            else
                SetLaser(false);
        }

        private void UpdateTarget()
        {
            // Keep current target while still valid (prevents jittery switching)
            if (currentTarget != null && IsValidTarget(currentTarget))
                return;

            currentTarget = Enemy.NearestInRange(transform.position, transform.position, Range);
        }

        private bool IsValidTarget(Enemy enemy)
        {
            if (enemy == null) return false;
            float r = Range;
            return (enemy.transform.position - transform.position).sqrMagnitude <= r * r;
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

        /// <summary>
        /// Burns gas for this frame of firing. Gas is spent from Economy in whole units;
        /// the leftover is kept as fuel. Returns false when the player is out of gas.
        /// </summary>
        private bool TryUseGas(float dt)
        {
            double needed = Data.gasPerSecond * dt;
            if (needed <= 0 || Economy.Instance == null) return true;

            while (gasFuel < needed)
            {
                if (!Economy.Instance.TrySpendGas(1)) return false;
                gasFuel += 1;
            }
            gasFuel -= needed;
            return true;
        }

        private void FireLaser(Vector3 aimPoint)
        {
            SetLaser(true);

            if (laserLine != null)
            {
                laserLine.SetPosition(0, firePoint.position);
                laserLine.SetPosition(1, aimPoint);
            }

            // Frame-rate independent damage. Attack is treated as damage per second,
            // and already includes level scaling and buffs.
            currentTarget.TakeDamage(Attack * Time.deltaTime);
        }

        private void SetLaser(bool on)
        {
            if (laserLine != null && laserLine.enabled != on)
                laserLine.enabled = on;
        }

        // ---- Tower panel text ----

        public override string Description =>
            Join(Summary, $"Costs {(Data != null ? Data.gemCost : 0)} gems to build and burns gas while firing.\nUpgrade: more damage.");

        public override void FillStats(List<(string label, string value)> rows)
        {
            base.FillStats(rows);
            rows.Add(("Gas use", $"{Data.gasPerSecond:0.#} /s when firing"));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            float r = Data != null ? Range : 12f;
            Gizmos.DrawWireSphere(transform.position, r);
        }
    }
}