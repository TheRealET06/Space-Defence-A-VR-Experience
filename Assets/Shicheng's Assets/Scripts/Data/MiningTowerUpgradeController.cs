using UnityEngine;

public class MiningTowerUpgradeController : MonoBehaviour
{
    [Header("Core Systems")]
    [SerializeField] MiningTowerTargetSelector targetSelector;
    [SerializeField] OrbitalAnchorProcessor processor;
    [SerializeField] BarrelHub barrelHub;
    [SerializeField] MiningTowerHealth health;

    [Header("Engineer Upgrade Sequence")]
    [SerializeField] GameObject[] engineerUpgradeMeshes;

    [Header("Power System Upgrade Sequence")]
    [SerializeField] GameObject[] powerSystemUpgradeMeshes;

    [Header("Walls Upgrade Sequence")]
    [SerializeField] GameObject[] wallsUpgradeMeshes;

    [Header("Weapons Upgrade Sequence")]
    [SerializeField] GameObject[] weaponsUpgradeMeshes;

    [Header("Mining Range")]
    [SerializeField] float rangeIncreasePerLevel = 2f;
    [SerializeField] int maxRangeLevel = 3;

    [Header("Processing")]
    [SerializeField] float processingIncreasePerLevel = 0.5f;
    [SerializeField] int maxProcessingLevel = 3;

    [Header("Engineer Effect")]
    [SerializeField] int baseEngineerCount = 1;
    [SerializeField] float repairPerEngineerPerSecond = 1f;

    [Header("Power Effect")]
    [SerializeField] float powerProcessingBonusPerLevel = 0.25f;

    [Header("Walls Effect")]
    [SerializeField] float shieldPerWallLevel = 25f;

    int rangeLevel;
    int processingLevel;
    int engineerLevel;
    int powerLevel;
    int wallsLevel;
    int weaponsLevel;

    public int RangeLevel => rangeLevel;
    public int ProcessingLevel => processingLevel;
    public int EngineerLevel => engineerLevel;
    public int PowerLevel => powerLevel;
    public int WallsLevel => wallsLevel;
    public int WeaponsLevel => weaponsLevel;

    public int MaxRangeLevel => maxRangeLevel;
    public int MaxProcessingLevel => maxProcessingLevel;

    public int MaxEngineerLevel =>
        GetSequenceLength(engineerUpgradeMeshes);

    public int MaxPowerLevel =>
        GetSequenceLength(powerSystemUpgradeMeshes);

    public int MaxWallsLevel =>
        GetSequenceLength(wallsUpgradeMeshes);

    public int MaxWeaponsLevel =>
        GetSequenceLength(weaponsUpgradeMeshes);

    public int ActiveEngineerCount =>
        baseEngineerCount + engineerLevel;

    public int MaxEngineerCount =>
        baseEngineerCount + MaxEngineerLevel;

    public float CurrentRepairRate =>
        ActiveEngineerCount *
        repairPerEngineerPerSecond;

    public float CurrentMiningRange =>
        targetSelector != null
            ? targetSelector.CaptureRange
            : 0f;

    public float MaxPossibleMiningRange =>
        CurrentMiningRange
        - rangeLevel * rangeIncreasePerLevel
        + maxRangeLevel * rangeIncreasePerLevel;

    public int CurrentStorageCapacity =>
        barrelHub != null
            ? barrelHub.Capacity
            : 0;

    public int MaxStorageCapacity =>
        barrelHub != null
            ? barrelHub.MaxRepositoryCount * 4
            : 0;

    public int CurrentStoredBarrels =>
        barrelHub != null
            ? barrelHub.BarrelCount
            : 0;

    public int CurrentRepositoryCount =>
        barrelHub != null
            ? barrelHub.UnlockedRepositoryCount
            : 0;

    public int MaxRepositoryCount =>
        barrelHub != null
            ? barrelHub.MaxRepositoryCount
            : 0;

    public float CurrentProcessingRate =>
        processor != null
            ? processor.BarrelsPerSecond
            : 0f;

    public float MaxPossibleProcessingRate =>
        CurrentProcessingRate
        - processingLevel * processingIncreasePerLevel
        - powerLevel * powerProcessingBonusPerLevel
        + maxProcessingLevel * processingIncreasePerLevel
        + MaxPowerLevel * powerProcessingBonusPerLevel;

    public float CurrentHealth =>
        health != null
            ? health.CurrentHealth
            : 0f;

    public float MaxHealth =>
        health != null
            ? health.MaxHealth
            : 0f;

    public float CurrentShield =>
        health != null
            ? health.CurrentShield
            : 0f;

    public float MaxShield =>
        health != null
            ? health.MaxShield
            : 0f;

    public float MaxPossibleShield =>
        MaxWallsLevel * shieldPerWallLevel;

    void Awake()
    {
        HideUpgradeSequence(engineerUpgradeMeshes);
        HideUpgradeSequence(powerSystemUpgradeMeshes);
        HideUpgradeSequence(wallsUpgradeMeshes);
        HideUpgradeSequence(weaponsUpgradeMeshes);
    }

    void Update()
    {
        if (health == null)
            return;

        if (ActiveEngineerCount <= 0)
            return;

        health.Repair(
            CurrentRepairRate *
            Time.deltaTime
        );
    }

    public bool CanUpgrade(
        TowerActionType action
    )
    {
        switch (action)
        {
            case TowerActionType.UpgradeMiningRange:
                return
                    targetSelector != null &&
                    rangeLevel < maxRangeLevel;

            case TowerActionType.UpgradeStorage:
                return
                    barrelHub != null &&
                    barrelHub.UnlockedRepositoryCount <
                    barrelHub.MaxRepositoryCount;

            case TowerActionType.UpgradeProcessingSpeed:
                return
                    processor != null &&
                    processingLevel <
                    maxProcessingLevel;

            case TowerActionType.UpgradeEngineer:
                return engineerLevel <
                       MaxEngineerLevel;

            case TowerActionType.UpgradePowerSystem:
                return powerLevel <
                       MaxPowerLevel;

            case TowerActionType.UpgradeWalls:
                return wallsLevel <
                       MaxWallsLevel;

            case TowerActionType.UpgradeWeapons:
                return weaponsLevel <
                       MaxWeaponsLevel;
        }

        return false;
    }

    public bool ApplyUpgrade(
        TowerActionType action
    )
    {
        if (!CanUpgrade(action))
        {
            Debug.LogWarning(
                $"[TowerUpgrade] Upgrade unavailable: {action}",
                gameObject
            );

            return false;
        }

        switch (action)
        {
            case TowerActionType.UpgradeMiningRange:
                return UpgradeMiningRange();

            case TowerActionType.UpgradeStorage:
                return UpgradeStorage();

            case TowerActionType.UpgradeProcessingSpeed:
                return UpgradeProcessing();

            case TowerActionType.UpgradeEngineer:
                return UpgradeEngineer();

            case TowerActionType.UpgradePowerSystem:
                return UpgradePowerSystem();

            case TowerActionType.UpgradeWalls:
                return UpgradeWalls();

            case TowerActionType.UpgradeWeapons:
                return UpgradeWeapons();
        }

        return false;
    }

    bool UpgradeMiningRange()
    {
        if (targetSelector == null)
            return false;

        targetSelector.IncreaseCaptureRange(
            rangeIncreasePerLevel
        );

        rangeLevel++;

        Debug.Log(
            $"[TowerUpgrade] Range {rangeLevel}/{maxRangeLevel}, " +
            $"{CurrentMiningRange:F1} m.",
            gameObject
        );

        return true;
    }

    bool UpgradeStorage()
    {
        if (barrelHub == null)
            return false;

        bool success =
            barrelHub.UnlockNextRepository();

        if (!success)
            return false;

        Debug.Log(
            $"[TowerUpgrade] Storage = " +
            $"{CurrentStorageCapacity}/{MaxStorageCapacity}.",
            gameObject
        );

        return true;
    }

    bool UpgradeProcessing()
    {
        if (processor == null)
            return false;

        processor.IncreaseProcessingSpeed(
            processingIncreasePerLevel
        );

        processingLevel++;

        Debug.Log(
            $"[TowerUpgrade] Processing " +
            $"{processingLevel}/{maxProcessingLevel}, " +
            $"{CurrentProcessingRate:F2}/s.",
            gameObject
        );

        return true;
    }

    bool UpgradeEngineer()
    {
        bool success =
            ActivateNextMesh(
                engineerUpgradeMeshes,
                engineerLevel,
                "Engineer"
            );

        if (!success)
            return false;

        engineerLevel++;

        Debug.Log(
            $"[TowerUpgrade] Engineers = " +
            $"{ActiveEngineerCount}/{MaxEngineerCount}.",
            gameObject
        );

        return true;
    }

    bool UpgradePowerSystem()
    {
        bool success =
            ActivateNextMesh(
                powerSystemUpgradeMeshes,
                powerLevel,
                "Power System"
            );

        if (!success)
            return false;

        powerLevel++;

        if (processor != null)
        {
            processor.IncreaseProcessingSpeed(
                powerProcessingBonusPerLevel
            );
        }

        Debug.Log(
            $"[TowerUpgrade] Power = " +
            $"{powerLevel}/{MaxPowerLevel}.",
            gameObject
        );

        return true;
    }

    bool UpgradeWalls()
    {
        bool success =
            ActivateNextMesh(
                wallsUpgradeMeshes,
                wallsLevel,
                "Walls"
            );

        if (!success)
            return false;

        wallsLevel++;

        if (health != null)
        {
            health.AddShieldCapacity(
                shieldPerWallLevel
            );
        }

        Debug.Log(
            $"[TowerUpgrade] Walls = " +
            $"{wallsLevel}/{MaxWallsLevel}.",
            gameObject
        );

        return true;
    }

    bool UpgradeWeapons()
    {
        bool success =
            ActivateNextMesh(
                weaponsUpgradeMeshes,
                weaponsLevel,
                "Weapons"
            );

        if (!success)
            return false;

        weaponsLevel++;

        Debug.Log(
            $"[TowerUpgrade] Weapons = " +
            $"{weaponsLevel}/{MaxWeaponsLevel}.",
            gameObject
        );

        return true;
    }

    bool ActivateNextMesh(
        GameObject[] sequence,
        int currentLevel,
        string categoryName
    )
    {
        if (
            sequence == null ||
            currentLevel < 0 ||
            currentLevel >= sequence.Length
        )
        {
            return false;
        }

        GameObject nextObject =
            sequence[currentLevel];

        if (nextObject == null)
        {
            Debug.LogError(
                $"[TowerUpgrade] {categoryName} " +
                $"element {currentLevel} is NULL.",
                gameObject
            );

            return false;
        }

        nextObject.SetActive(true);

        Debug.Log(
            $"[TowerUpgrade] Activated " +
            $"{categoryName}: {nextObject.name}.",
            nextObject
        );

        return true;
    }

    void HideUpgradeSequence(
        GameObject[] sequence
    )
    {
        if (sequence == null)
            return;

        foreach (GameObject obj in sequence)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
    }

    int GetSequenceLength(
        GameObject[] sequence
    )
    {
        return sequence != null
            ? sequence.Length
            : 0;
    }
}