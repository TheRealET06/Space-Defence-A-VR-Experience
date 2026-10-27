using UnityEngine;

public class MiningTowerTargetSelector : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField]
    private MiningTowerUpgradeController upgradeController;

    // =========================================================
    // TARGETING
    // =========================================================

    [Header("Targeting")]
    [SerializeField]
    private float captureRange = 8f;

    [Tooltip("Origin used for distance checks. If empty, this object is used.")]
    [SerializeField]
    private Transform scanOrigin;

    [Tooltip(
        "Arm pivot used to determine whether a target " +
        "is inside the vertical aiming range."
    )]
    [SerializeField]
    private Transform aimOrigin;

    // =========================================================
    // ARM LIMITS
    // =========================================================

    [Header("Arm Vertical Limits")]
    [SerializeField]
    private float minArmAngleX = -90f;

    [SerializeField]
    private float maxArmAngleX = 45f;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]
    [SerializeField]
    private bool autoSearchOnStart = false;

    [SerializeField]
    private bool drawCaptureRange = true;

    [SerializeField]
    private bool logRejectedTargets = false;

    [Header("Selection")]
    [SerializeField]
    private bool randomTarget = false;

    private readonly System.Collections.Generic.List<ResourceAsteroid> candidates =
        new System.Collections.Generic.List<ResourceAsteroid>();

    // =========================================================
    // STATE
    // =========================================================

    private ResourceAsteroid currentTarget;

    public float CaptureRange =>
        captureRange;

    public ResourceAsteroid CurrentTarget =>
        currentTarget;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (upgradeController == null)
        {
            upgradeController =
                GetComponentInParent<MiningTowerUpgradeController>();
        }
    }

    private void Start()
    {
        if (autoSearchOnStart)
        {
            FindBestTarget();
        }
    }

    // =========================================================
    // TARGET SEARCH
    // =========================================================

    public ResourceAsteroid FindBestTarget()
    {
        ResourceAsteroid[] allAsteroids =
            FindObjectsByType<ResourceAsteroid>(
                FindObjectsSortMode.None
            );

        Vector3 rangeOrigin =
            scanOrigin != null
                ? scanOrigin.position
                : transform.position;

        Vector3 verticalAimOrigin =
            aimOrigin != null
                ? aimOrigin.position
                : rangeOrigin;

        ResourceAsteroid bestTarget = null;

        candidates.Clear();

        int bestValue =
            int.MinValue;

        float bestDistance =
            float.MaxValue;

        int miningHeadLevel =
            GetCurrentMiningHeadLevel();

        foreach (
            ResourceAsteroid asteroid
            in allAsteroids
        )
        {
            if (asteroid == null)
            {
                continue;
            }

            // Chunks are never selected as initial mining targets.
            if (asteroid.IsChunk)
            {
                continue;
            }

            // Mining Head controls which asteroid families
            // are currently harvestable.
            if (
                !CanMineFamily(
                    asteroid.Family,
                    miningHeadLevel
                )
            )
            {
                if (logRejectedTargets)
                {
                    Debug.Log(
                        $"Rejected {asteroid.name}: " +
                        $"{asteroid.Family} requires Mining Head " +
                        $"Lv.{GetRequiredMiningHeadLevel(asteroid.Family)}, " +
                        $"current level = Lv.{miningHeadLevel}.",
                        asteroid.gameObject
                    );
                }

                continue;
            }

            // -------------------------------------------------
            // RANGE CHECK
            // -------------------------------------------------

            float distance =
                Vector3.Distance(
                    rangeOrigin,
                    asteroid.transform.position
                );

            if (distance > captureRange)
            {
                if (logRejectedTargets)
                {
                    Debug.Log(
                        $"Rejected {asteroid.name}: " +
                        $"outside capture range " +
                        $"({distance:F1} m > {captureRange:F1} m).",
                        asteroid.gameObject
                    );
                }

                continue;
            }

            // -------------------------------------------------
            // ARM ANGLE CHECK
            // -------------------------------------------------

            float requiredArmAngleX =
                CalculateRequiredArmAngleX(
                    verticalAimOrigin,
                    asteroid.transform.position
                );

            bool canAim =
                requiredArmAngleX >= minArmAngleX &&
                requiredArmAngleX <= maxArmAngleX;

            if (!canAim)
            {
                if (logRejectedTargets)
                {
                    Debug.Log(
                        $"Rejected {asteroid.name}: " +
                        $"required Arm X = {requiredArmAngleX:F1}°, " +
                        $"allowed range = " +
                        $"{minArmAngleX:F1}° to " +
                        $"{maxArmAngleX:F1}°.",
                        asteroid.gameObject
                    );
                }

                continue;
            }

            // -------------------------------------------------
            // VALUE SELECTION
            // -------------------------------------------------

            candidates.Add(asteroid);

            int value =
                asteroid.PriorityValue;

            bool higherValue =
                value > bestValue;

            bool sameValueButCloser =
                value == bestValue &&
                distance < bestDistance;

            if (
                higherValue ||
                sameValueButCloser
            )
            {
                bestTarget =
                    asteroid;

                bestValue =
                    value;

                bestDistance =
                    distance;
            }
        }

        if (randomTarget && candidates.Count > 0)
        {
            bestTarget =
                candidates[Random.Range(0, candidates.Count)];

            bestDistance =
                Vector3.Distance(
                    rangeOrigin,
                    bestTarget.transform.position
                );
        }

        currentTarget =
            bestTarget;

        // -----------------------------------------------------
        // RESULT
        // -----------------------------------------------------

        if (currentTarget != null)
        {
            float requiredArmX =
                CalculateRequiredArmAngleX(
                    verticalAimOrigin,
                    currentTarget.transform.position
                );

            Debug.Log(
                "[MiningTower Target]\n" +
                $"Mining Head = Lv.{miningHeadLevel}\n" +
                $"Target = {currentTarget.name}\n" +
                $"Family = {currentTarget.Family}\n" +
                $"Value = {currentTarget.PriorityValue}\n" +
                $"Distance = {bestDistance:F2} m\n" +
                $"Required Arm X = {requiredArmX:F1}°",
                currentTarget.gameObject
            );
        }
        else
        {
            Debug.Log(
                "[MiningTower Target] No valid target found.\n" +
                $"Mining Head = Lv.{miningHeadLevel}\n" +
                $"Capture Range = {captureRange:F1} m\n" +
                $"Highest Harvestable Family = " +
                $"{GetHighestUnlockedFamilyName(miningHeadLevel)}",
                gameObject
            );
        }

        return currentTarget;
    }

    // =========================================================
    // MINING HEAD FAMILY RULES
    // =========================================================

    private int GetCurrentMiningHeadLevel()
    {
        if (upgradeController == null)
        {
            upgradeController =
                GetComponentInParent<MiningTowerUpgradeController>();
        }

        // Base Mining Head is always Lv1.
        if (upgradeController == null)
        {
            return 1;
        }

        return Mathf.Clamp(
            upgradeController.MiningHeadLevel,
            1,
            5
        );
    }

    public bool CanMineFamily(
        AsteroidFamily family)
    {
        return CanMineFamily(
            family,
            GetCurrentMiningHeadLevel()
        );
    }

    private bool CanMineFamily(
        AsteroidFamily family,
        int miningHeadLevel)
    {
        int requiredLevel =
            GetRequiredMiningHeadLevel(
                family
            );

        return
            miningHeadLevel >= requiredLevel;
    }

    public int GetRequiredMiningHeadLevel(
        AsteroidFamily family)
    {
        switch (family)
        {
            case AsteroidFamily.Slate:
                return 1;

            case AsteroidFamily.Frost:
                return 2;

            case AsteroidFamily.Basalt:
                return 3;

            case AsteroidFamily.Abyssal:
                return 4;

            case AsteroidFamily.Crystal:
                return 5;

            default:
                return int.MaxValue;
        }
    }

    private string GetHighestUnlockedFamilyName(
        int miningHeadLevel)
    {
        switch (
            Mathf.Clamp(
                miningHeadLevel,
                1,
                5
            )
        )
        {
            case 1:
                return "Slate";

            case 2:
                return "Frost";

            case 3:
                return "Basalt";

            case 4:
                return "Abyssal";

            case 5:
                return "Crystal";

            default:
                return "Slate";
        }
    }

    // =========================================================
    // ARM CALCULATION
    // =========================================================

    private float CalculateRequiredArmAngleX(
        Vector3 origin,
        Vector3 targetPosition)
    {
        Vector3 direction =
            targetPosition - origin;

        float horizontalDistance =
            new Vector2(
                direction.x,
                direction.z
            ).magnitude;

        float elevationAngle =
            Mathf.Atan2(
                direction.y,
                horizontalDistance
            ) *
            Mathf.Rad2Deg;

        // The model aims upward using negative local X rotation.
        return -elevationAngle;
    }

    // =========================================================
    // TARGET STATE
    // =========================================================

    public void ClearTarget()
    {
        currentTarget = null;
    }

    // =========================================================
    // RANGE UPGRADE
    // =========================================================

    public void IncreaseCaptureRange(
        float amount)
    {
        captureRange =
            Mathf.Max(
                0f,
                captureRange + amount
            );
    }

    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        if (!drawCaptureRange)
        {
            return;
        }

        Vector3 origin =
            scanOrigin != null
                ? scanOrigin.position
                : transform.position;

        Gizmos.DrawWireSphere(
            origin,
            captureRange
        );
    }
}