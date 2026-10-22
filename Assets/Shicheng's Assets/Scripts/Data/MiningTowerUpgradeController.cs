using UnityEngine;

public class MiningTowerUpgradeController : MonoBehaviour
{
    // =========================================================
    // CORE SYSTEMS
    // =========================================================

    [Header("Core Systems")]
    [SerializeField]
    private MiningTowerTargetSelector targetSelector;

    [SerializeField]
    private OrbitalAnchorProcessor processor;

    [SerializeField]
    private BarrelHub barrelHub;

    [SerializeField]
    private MiningTowerHealth health;

    // =========================================================
    // VISUAL UPGRADE SEQUENCES
    // =========================================================

    [Header("Mining Head Upgrade Sequence")]
    [Tooltip("Meshes for Mining Head Lv2 -> Lv5.")]
    [SerializeField]
    private GameObject[] miningHeadUpgradeMeshes;

    [Header("Engineer Upgrade Sequence")]
    [Tooltip("Meshes for Engineer Lv1 -> Lv3.")]
    [SerializeField]
    private GameObject[] engineerUpgradeMeshes;

    [Header("Power Upgrade Sequence")]
    [Tooltip("Meshes for Power Lv1 -> Lv4.")]
    [SerializeField]
    private GameObject[] powerUpgradeMeshes;

    [Header("Walls Upgrade Sequence")]
    [Tooltip("Meshes for Walls Lv1 -> Lv8.")]
    [SerializeField]
    private GameObject[] wallsUpgradeMeshes;

    [Header("Weapons Upgrade Sequence")]
    [Tooltip("Meshes for Weapons Lv1 -> Lv8.")]
    [SerializeField]
    private GameObject[] weaponsUpgradeMeshes;

    // =========================================================
    // MINING HEAD
    // =========================================================

    [Header("Mining Head")]
    [SerializeField]
    private float miningHeadRangeIncreasePerLevel = 2f;

    [SerializeField]
    private int maxMiningHeadLevel = 5;

    // Mining Head exists at Lv1 on the base tower.
    private int miningHeadLevel = 1;

    // =========================================================
    // ENGINEER
    // =========================================================

    [Header("Engineer")]
    [SerializeField]
    private float repairPerEngineerPerSecond = 1f;

    private int engineerLevel;

    // =========================================================
    // POWER
    // =========================================================

    [Header("Power")]
    [SerializeField]
    private float powerProcessingBonusPerLevel = 0.25f;

    private int powerLevel;

    // =========================================================
    // WALLS
    // =========================================================

    [Header("Walls")]
    [SerializeField]
    private float shieldPerWallLevel = 25f;

    private int wallsLevel;

    // =========================================================
    // WEAPONS
    // =========================================================

    private int weaponsLevel;

    // =========================================================
    // PUBLIC LEVELS
    // =========================================================

    public int MiningHeadLevel =>
        miningHeadLevel;

    public int StorageLevel =>
        barrelHub != null
            ? barrelHub.UnlockedRepositoryCount
            : 0;

    public int EngineerLevel =>
        engineerLevel;

    public int PowerLevel =>
        powerLevel;

    public int WallsLevel =>
        wallsLevel;

    public int WeaponsLevel =>
        weaponsLevel;

    // =========================================================
    // MAX LEVELS
    // =========================================================

    public int MaxMiningHeadLevel =>
        maxMiningHeadLevel;

    public int MaxStorageLevel =>
        barrelHub != null
            ? barrelHub.MaxRepositoryCount
            : 0;

    public int MaxEngineerLevel =>
        GetSequenceLength(
            engineerUpgradeMeshes
        );

    public int MaxPowerLevel =>
        GetSequenceLength(
            powerUpgradeMeshes
        );

    public int MaxWallsLevel =>
        GetSequenceLength(
            wallsUpgradeMeshes
        );

    public int MaxWeaponsLevel =>
        GetSequenceLength(
            weaponsUpgradeMeshes
        );

    // =========================================================
    // CURRENT STATS
    // =========================================================

    public int ActiveEngineerCount =>
        engineerLevel;

    public float CurrentRepairRate =>
        engineerLevel *
        repairPerEngineerPerSecond;

    public float CurrentMiningRange =>
        targetSelector != null
            ? targetSelector.CaptureRange
            : 0f;

    public int CurrentStorageCapacity =>
        barrelHub != null
            ? barrelHub.Capacity
            : 0;

    public int CurrentStoredBarrels =>
        barrelHub != null
            ? barrelHub.BarrelCount
            : 0;

    public float CurrentProcessingRate =>
        processor != null
            ? processor.BarrelsPerSecond
            : 0f;

    public float CurrentHealth =>
        health != null
            ? health.CurrentHealth
            : 0f;

    public float MaxHealth =>
        health != null
            ? health.MaxHealth
            : 0f;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        HideUpgradeSequence(
            miningHeadUpgradeMeshes
        );

        HideUpgradeSequence(
            engineerUpgradeMeshes
        );

        HideUpgradeSequence(
            powerUpgradeMeshes
        );

        HideUpgradeSequence(
            wallsUpgradeMeshes
        );

        HideUpgradeSequence(
            weaponsUpgradeMeshes
        );
    }

    private void Update()
    {
        if (
            health == null ||
            engineerLevel <= 0
        )
        {
            return;
        }

        health.Repair(
            CurrentRepairRate *
            Time.deltaTime
        );
    }

    // =========================================================
    // GENERIC LEVEL ACCESS
    // =========================================================

    public int GetLevel(
        MiningTowerUpgradeType type)
    {
        switch (type)
        {
            case MiningTowerUpgradeType.MiningHead:
                return MiningHeadLevel;

            case MiningTowerUpgradeType.Storage:
                return StorageLevel;

            case MiningTowerUpgradeType.Engineer:
                return EngineerLevel;

            case MiningTowerUpgradeType.Power:
                return PowerLevel;

            case MiningTowerUpgradeType.Walls:
                return WallsLevel;

            case MiningTowerUpgradeType.Weapons:
                return WeaponsLevel;

            default:
                return 0;
        }
    }

    public int GetMaxLevel(
        MiningTowerUpgradeType type)
    {
        switch (type)
        {
            case MiningTowerUpgradeType.MiningHead:
                return MaxMiningHeadLevel;

            case MiningTowerUpgradeType.Storage:
                return MaxStorageLevel;

            case MiningTowerUpgradeType.Engineer:
                return MaxEngineerLevel;

            case MiningTowerUpgradeType.Power:
                return MaxPowerLevel;

            case MiningTowerUpgradeType.Walls:
                return MaxWallsLevel;

            case MiningTowerUpgradeType.Weapons:
                return MaxWeaponsLevel;

            default:
                return 0;
        }
    }

    // =========================================================
    // CAN UPGRADE
    // =========================================================

    public bool CanUpgrade(
        MiningTowerUpgradeType type)
    {
        switch (type)
        {
            case MiningTowerUpgradeType.MiningHead:
                return
                    targetSelector != null &&
                    MiningHeadLevel <
                    MaxMiningHeadLevel;

            case MiningTowerUpgradeType.Storage:
                return
                    barrelHub != null &&
                    StorageLevel <
                    MaxStorageLevel;

            case MiningTowerUpgradeType.Engineer:
                return
                    EngineerLevel <
                    MaxEngineerLevel;

            case MiningTowerUpgradeType.Power:
                return
                    PowerLevel <
                    MaxPowerLevel;

            case MiningTowerUpgradeType.Walls:
                return
                    WallsLevel <
                    MaxWallsLevel;

            case MiningTowerUpgradeType.Weapons:
                return
                    WeaponsLevel <
                    MaxWeaponsLevel;

            default:
                return false;
        }
    }

    // =========================================================
    // APPLY UPGRADE
    // =========================================================

    public bool ApplyUpgrade(
        MiningTowerUpgradeType type)
    {
        if (!CanUpgrade(type))
        {
            Debug.LogWarning(
                $"[TowerUpgrade] Upgrade unavailable: {type}",
                gameObject
            );

            return false;
        }

        switch (type)
        {
            case MiningTowerUpgradeType.MiningHead:
                return UpgradeMiningHead();

            case MiningTowerUpgradeType.Storage:
                return UpgradeStorage();

            case MiningTowerUpgradeType.Engineer:
                return UpgradeEngineer();

            case MiningTowerUpgradeType.Power:
                return UpgradePower();

            case MiningTowerUpgradeType.Walls:
                return UpgradeWalls();

            case MiningTowerUpgradeType.Weapons:
                return UpgradeWeapons();

            default:
                return false;
        }
    }

    // =========================================================
    // MINING HEAD
    // =========================================================

    private bool UpgradeMiningHead()
    {
        if (targetSelector == null)
        {
            return false;
        }

        // Lv1 -> Lv2 uses mesh index 0.
        int meshIndex =
            MiningHeadLevel - 1;

        ActivateOptionalMesh(
            miningHeadUpgradeMeshes,
            meshIndex,
            "Mining Head"
        );

        targetSelector.IncreaseCaptureRange(
            miningHeadRangeIncreasePerLevel
        );

        miningHeadLevel++;

        Debug.Log(
            "[TowerUpgrade] Mining Head = " +
            $"Lv.{MiningHeadLevel}/{MaxMiningHeadLevel}, " +
            $"Range = {CurrentMiningRange:F1} m.",
            gameObject
        );

        return true;
    }

    // =========================================================
    // STORAGE
    // =========================================================

    private bool UpgradeStorage()
    {
        if (barrelHub == null)
        {
            return false;
        }

        bool success =
            barrelHub.UnlockNextRepository();

        if (!success)
        {
            return false;
        }

        Debug.Log(
            "[TowerUpgrade] Storage = " +
            $"Lv.{StorageLevel}/{MaxStorageLevel}, " +
            $"Capacity = {CurrentStorageCapacity}.",
            gameObject
        );

        return true;
    }

    // =========================================================
    // ENGINEER
    // =========================================================

    private bool UpgradeEngineer()
    {
        bool success =
            ActivateRequiredMesh(
                engineerUpgradeMeshes,
                engineerLevel,
                "Engineer"
            );

        if (!success)
        {
            return false;
        }

        engineerLevel++;

        Debug.Log(
            "[TowerUpgrade] Engineer = " +
            $"Lv.{EngineerLevel}/{MaxEngineerLevel}, " +
            $"Repair = {CurrentRepairRate:F1} HP/s.",
            gameObject
        );

        return true;
    }

    // =========================================================
    // POWER
    // =========================================================

    private bool UpgradePower()
    {
        bool success =
            ActivateRequiredMesh(
                powerUpgradeMeshes,
                powerLevel,
                "Power"
            );

        if (!success)
        {
            return false;
        }

        powerLevel++;

        // Power is the only upgrade controlling
        // processing speed.
        if (processor != null)
        {
            processor.IncreaseProcessingSpeed(
                powerProcessingBonusPerLevel
            );
        }

        Debug.Log(
            "[TowerUpgrade] Power = " +
            $"Lv.{PowerLevel}/{MaxPowerLevel}, " +
            $"Processing Rate = {CurrentProcessingRate:F2}/s.",
            gameObject
        );

        return true;
    }

    // =========================================================
    // WALLS
    // =========================================================

    private bool UpgradeWalls()
    {
        bool success =
            ActivateRequiredMesh(
                wallsUpgradeMeshes,
                wallsLevel,
                "Walls"
            );

        if (!success)
        {
            return false;
        }

        wallsLevel++;

        // This uses the currently available MiningTowerHealth API.
        if (health != null)
        {
            health.AddShieldCapacity(
                shieldPerWallLevel
            );
        }

        Debug.Log(
            "[TowerUpgrade] Walls = " +
            $"Lv.{WallsLevel}/{MaxWallsLevel}.",
            gameObject
        );

        return true;
    }

    // =========================================================
    // WEAPONS
    // =========================================================

    private bool UpgradeWeapons()
    {
        bool success =
            ActivateRequiredMesh(
                weaponsUpgradeMeshes,
                weaponsLevel,
                "Weapons"
            );

        if (!success)
        {
            return false;
        }

        weaponsLevel++;

        Debug.Log(
            "[TowerUpgrade] Weapons = " +
            $"Lv.{WeaponsLevel}/{MaxWeaponsLevel}.",
            gameObject
        );

        return true;
    }

    // =========================================================
    // VISUAL HELPERS
    // =========================================================

    private bool ActivateRequiredMesh(
        GameObject[] sequence,
        int index,
        string categoryName)
    {
        if (
            sequence == null ||
            index < 0 ||
            index >= sequence.Length
        )
        {
            Debug.LogWarning(
                $"[TowerUpgrade] Missing {categoryName} " +
                $"mesh for target level {index + 1}.",
                gameObject
            );

            return false;
        }

        GameObject obj =
            sequence[index];

        if (obj == null)
        {
            Debug.LogError(
                $"[TowerUpgrade] {categoryName} " +
                $"mesh index {index} is NULL.",
                gameObject
            );

            return false;
        }

        obj.SetActive(true);

        return true;
    }

    private void ActivateOptionalMesh(
        GameObject[] sequence,
        int index,
        string categoryName)
    {
        if (
            sequence == null ||
            index < 0 ||
            index >= sequence.Length
        )
        {
            return;
        }

        GameObject obj =
            sequence[index];

        if (obj == null)
        {
            return;
        }

        obj.SetActive(true);

        Debug.Log(
            $"[TowerUpgrade] Activated " +
            $"{categoryName}: {obj.name}.",
            obj
        );
    }

    private void HideUpgradeSequence(
        GameObject[] sequence)
    {
        if (sequence == null)
        {
            return;
        }

        foreach (
            GameObject obj
            in sequence
        )
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
    }

    private int GetSequenceLength(
        GameObject[] sequence)
    {
        return sequence != null
            ? sequence.Length
            : 0;
    }
}