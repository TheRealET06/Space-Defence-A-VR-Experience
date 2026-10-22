using System;
using TMPro;
using UnityEngine;

public class MiningTowerProgression : MonoBehaviour
{
    private struct StageTarget
    {
        public int miningHead;
        public int storage;
        public int engineer;
        public int power;
        public int walls;
        public int weapons;

        public StageTarget(
            int miningHead,
            int storage,
            int engineer,
            int power,
            int walls,
            int weapons)
        {
            this.miningHead = miningHead;
            this.storage = storage;
            this.engineer = engineer;
            this.power = power;
            this.walls = walls;
            this.weapons = weapons;
        }
    }

    // =========================================================
    // EVENTS
    // =========================================================

    public event Action<int> ThresholdCompleted;

    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField]
    private MiningTowerUpgradeController upgradeController;

    [Header("Thresholds on the Horizon")]
    [SerializeField]
    private TMP_Text objectiveText;

    [Header("Debug")]
    [SerializeField]
    private bool logRubricEvents = true;

    // =========================================================
    // PROGRESSION
    // =========================================================

    [SerializeField]
    private int currentStage = 1;

    private bool progressionStarted;

    private readonly bool[] stageGateCatchTriggered =
        new bool[22];

    private bool upgradePanelLockedCatchTriggered;
    private bool insufficientCreditsCatchTriggered;
    private bool maximumLevelCatchTriggered;

    public int CurrentStage =>
        currentStage;

    public bool AllThresholdsComplete =>
        currentStage > 21;

    public int UniqueErrorCatchCount =>
        CountErrorCatches();

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (upgradeController == null)
        {
            upgradeController =
                GetComponentInChildren<MiningTowerUpgradeController>(
                    true
                );
        }

        if (objectiveText != null)
        {
            objectiveText.gameObject.SetActive(false);
        }
    }

    // =========================================================
    // START
    // =========================================================

    public void BeginProgression()
    {
        if (progressionStarted)
        {
            return;
        }

        progressionStarted = true;

        currentStage = 1;

        if (objectiveText != null)
        {
            objectiveText.gameObject.SetActive(false);
        }

        Log(
            "Threshold 01/21 active: FIRST HARVEST."
        );
    }

    private void EnsureStarted()
    {
        if (!progressionStarted)
        {
            BeginProgression();
        }
    }

    // =========================================================
    // THRESHOLDS 1-3
    // =========================================================

    public void NotifyHarvestCompleted()
    {
        EnsureStarted();

        if (currentStage == 1)
        {
            AdvanceStage();
        }
    }

    public void NotifyProcessingCompleted()
    {
        EnsureStarted();

        if (currentStage == 2)
        {
            AdvanceStage();
        }
    }

    public void NotifySaleCompleted()
    {
        EnsureStarted();

        if (currentStage == 3)
        {
            AdvanceStage();
        }
    }

    // =========================================================
    // UPGRADE PANEL GATE
    // =========================================================

    public bool CanOpenUpgradePanel(
        out string errorTitle,
        out string errorMessage)
    {
        EnsureStarted();

        errorTitle = null;
        errorMessage = null;

        if (currentStage >= 4)
        {
            return true;
        }

        errorTitle =
            "UPGRADES LOCKED";

        errorMessage =
            "The upgrade system is not available yet. " +
            "Complete the current objective first: " +
            GetStageInstruction(currentStage);

        RegisterUpgradePanelLockedCatch();

        return false;
    }

    // =========================================================
    // UPGRADE VALIDATION
    // =========================================================

    public bool TryValidateUpgrade(
        MiningTowerUpgradeType type,
        out string errorTitle,
        out string errorMessage)
    {
        EnsureStarted();

        errorTitle = null;
        errorMessage = null;

        if (upgradeController == null)
        {
            errorTitle =
                "UPGRADE SYSTEM UNAVAILABLE";

            errorMessage =
                "The Mining Tower upgrade controller is unavailable.";

            return false;
        }

        if (currentStage > 21)
        {
            return true;
        }

        if (currentStage < 4)
        {
            errorTitle =
                "UPGRADES LOCKED";

            errorMessage =
                "Complete the current objective first: " +
                GetStageInstruction(currentStage);

            RegisterUpgradePanelLockedCatch();

            return false;
        }

        StageTarget target =
            GetStageTarget(currentStage);

        int currentLevel =
            upgradeController.GetLevel(type);

        int allowedLevel =
            GetAllowedLevel(
                type,
                target
            );

        int requestedLevel =
            currentLevel + 1;

        if (requestedLevel > allowedLevel)
        {
            errorTitle =
                $"LOCKED BY THRESHOLD {currentStage:00}";

            errorMessage =
                $"{GetUpgradeName(type)} Lv.{requestedLevel} " +
                $"is currently locked. " +
                $"This threshold allows it up to Lv.{allowedLevel}. " +
                $"Complete: {GetStageInstruction(currentStage)}";

            RegisterStageGateCatch(
                currentStage
            );

            return false;
        }

        return true;
    }

    public void NotifyUpgradePurchased(
        MiningTowerUpgradeType type)
    {
        EnsureStarted();

        if (
            currentStage < 4 ||
            currentStage > 21
        )
        {
            return;
        }

        CheckUpgradeThresholdCompletion();
    }

    // =========================================================
    // THRESHOLD CHECK
    // =========================================================

    private void CheckUpgradeThresholdCompletion()
    {
        while (
            currentStage >= 4 &&
            currentStage <= 21 &&
            IsStageComplete(currentStage)
        )
        {
            AdvanceStage();
        }
    }

    private bool IsStageComplete(
        int stage)
    {
        if (upgradeController == null)
        {
            return false;
        }

        StageTarget target =
            GetStageTarget(stage);

        return
            upgradeController.MiningHeadLevel >=
                target.miningHead &&

            upgradeController.StorageLevel >=
                target.storage &&

            upgradeController.EngineerLevel >=
                target.engineer &&

            upgradeController.PowerLevel >=
                target.power &&

            upgradeController.WallsLevel >=
                target.walls &&

            upgradeController.WeaponsLevel >=
                target.weapons;
    }

    // =========================================================
    // ADVANCE
    // =========================================================

    private void AdvanceStage()
    {
        int completedStage =
            currentStage;

        Log(
            $"MASTER THRESHOLD " +
            $"{completedStage:00}/21 CLEARED"
        );

        currentStage++;

        if (currentStage > 21)
        {
            if (objectiveText != null)
            {
                objectiveText.gameObject.SetActive(true);

                objectiveText.text =
                    "<b>ALL 21 THRESHOLDS CLEARED</b>\n" +
                    "Mining Tower progression complete.";
            }

            Log(
                "All 21 Mastery Thresholds complete."
            );
        }
        else
        {
            ShowNextThreshold();
        }

        // Inform the Hub after the threshold has been cleared.
        ThresholdCompleted?.Invoke(
            completedStage
        );
    }

    // =========================================================
    // THRESHOLDS ON THE HORIZON
    // =========================================================

    private void ShowNextThreshold()
    {
        if (objectiveText == null)
        {
            return;
        }

        objectiveText.gameObject.SetActive(true);

        objectiveText.text =
            $"<b>NEXT THRESHOLD {currentStage:00}/21</b>\n" +
            $"<b>{GetStageName(currentStage)}</b>\n" +
            GetStageInstruction(currentStage);

        Log(
            $"HORIZON REVEALED: " +
            $"{currentStage:00}/21 - " +
            GetStageName(currentStage)
        );
    }

    // =========================================================
    // ERROR CATCH TRACKING
    // =========================================================

    private void RegisterStageGateCatch(
        int stage)
    {
        if (
            stage < 4 ||
            stage > 21
        )
        {
            return;
        }

        if (stageGateCatchTriggered[stage])
        {
            return;
        }

        stageGateCatchTriggered[stage] = true;

        LogErrorCatchCount(
            $"Threshold {stage:00} prerequisite gate"
        );
    }

    private void RegisterUpgradePanelLockedCatch()
    {
        if (upgradePanelLockedCatchTriggered)
        {
            return;
        }

        upgradePanelLockedCatchTriggered = true;

        LogErrorCatchCount(
            "Upgrade panel attempted before first sale"
        );
    }

    public void RegisterInsufficientCreditsCatch()
    {
        if (insufficientCreditsCatchTriggered)
        {
            return;
        }

        insufficientCreditsCatchTriggered = true;

        LogErrorCatchCount(
            "Insufficient Credits"
        );
    }

    public void RegisterMaxLevelCatch()
    {
        if (maximumLevelCatchTriggered)
        {
            return;
        }

        maximumLevelCatchTriggered = true;

        LogErrorCatchCount(
            "Maximum upgrade level reached"
        );
    }

    private int CountErrorCatches()
    {
        int count = 0;

        if (upgradePanelLockedCatchTriggered)
        {
            count++;
        }

        if (insufficientCreditsCatchTriggered)
        {
            count++;
        }

        if (maximumLevelCatchTriggered)
        {
            count++;
        }

        for (
            int stage = 4;
            stage <= 21;
            stage++
        )
        {
            if (stageGateCatchTriggered[stage])
            {
                count++;
            }
        }

        return count;
    }

    private void LogErrorCatchCount(
        string description)
    {
        Log(
            $"ERROR CATCH {CountErrorCatches():00}/21: " +
            description
        );
    }

    // =========================================================
    // ALLOWED LEVEL
    // =========================================================

    private int GetAllowedLevel(
        MiningTowerUpgradeType type,
        StageTarget target)
    {
        switch (type)
        {
            case MiningTowerUpgradeType.MiningHead:
                return target.miningHead;

            case MiningTowerUpgradeType.Storage:
                return target.storage;

            case MiningTowerUpgradeType.Engineer:
                return target.engineer;

            case MiningTowerUpgradeType.Power:
                return target.power;

            case MiningTowerUpgradeType.Walls:
                return target.walls;

            case MiningTowerUpgradeType.Weapons:
                return target.weapons;

            default:
                return 0;
        }
    }

    // =========================================================
    // 21 STAGE TARGETS
    // =========================================================

    private StageTarget GetStageTarget(
        int stage)
    {
        switch (stage)
        {
            case 1:
            case 2:
            case 3:
                return new StageTarget(
                    0, 0, 0, 0, 0, 0
                );

            case 4:
                return new StageTarget(
                    2, 1, 1, 0, 0, 0
                );

            case 5:
                return new StageTarget(
                    2, 2, 1, 0, 0, 0
                );

            case 6:
                return new StageTarget(
                    2, 2, 2, 0, 0, 0
                );

            case 7:
                return new StageTarget(
                    2, 2, 2, 1, 0, 0
                );

            case 8:
                return new StageTarget(
                    3, 2, 2, 1, 0, 0
                );

            case 9:
                return new StageTarget(
                    3, 4, 2, 2, 0, 0
                );

            case 10:
                return new StageTarget(
                    3, 4, 2, 2, 1, 0
                );

            case 11:
                return new StageTarget(
                    4, 4, 2, 2, 1, 0
                );

            case 12:
                return new StageTarget(
                    4, 4, 2, 2, 1, 1
                );

            case 13:
                return new StageTarget(
                    4, 6, 2, 2, 2, 2
                );

            case 14:
                return new StageTarget(
                    4, 6, 3, 2, 3, 3
                );

            case 15:
                return new StageTarget(
                    4, 6, 3, 3, 4, 4
                );

            case 16:
                return new StageTarget(
                    4, 7, 3, 3, 4, 4
                );

            case 17:
                return new StageTarget(
                    4, 7, 3, 4, 4, 4
                );

            case 18:
                return new StageTarget(
                    4, 8, 3, 4, 4, 4
                );

            case 19:
                return new StageTarget(
                    5, 8, 3, 4, 4, 4
                );

            case 20:
                return new StageTarget(
                    5, 8, 3, 4, 6, 6
                );

            case 21:
                return new StageTarget(
                    5, 8, 3, 4, 8, 8
                );

            default:
                return new StageTarget(
                    5, 8, 3, 4, 8, 8
                );
        }
    }

    // =========================================================
    // STAGE NAMES
    // =========================================================

    private string GetStageName(
        int stage)
    {
        switch (stage)
        {
            case 1:
                return "FIRST HARVEST";

            case 2:
                return "FIRST PROCESSING";

            case 3:
                return "FIRST SALE";

            case 4:
                return "FROST INFRASTRUCTURE";

            case 5:
                return "STORAGE EXPANSION I";

            case 6:
                return "ENGINEERING EXPANSION";

            case 7:
                return "POWER ONLINE";

            case 8:
                return "BASALT EXTRACTION";

            case 9:
                return "MID-TIER INFRASTRUCTURE";

            case 10:
                return "WALL DEFENSE ONLINE";

            case 11:
                return "ABYSSAL EXTRACTION";

            case 12:
                return "WEAPONS ONLINE";

            case 13:
                return "INDUSTRIAL EXPANSION";

            case 14:
                return "ADVANCED ENGINEERING";

            case 15:
                return "ADVANCED POWER AND DEFENSE";

            case 16:
                return "STORAGE EXPANSION III";

            case 17:
                return "MAXIMUM POWER";

            case 18:
                return "MAXIMUM STORAGE";

            case 19:
                return "CRYSTAL EXTRACTION";

            case 20:
                return "ADVANCED DEFENSE";

            case 21:
                return "MAXIMUM DEFENSE";

            default:
                return "COMPLETE";
        }
    }

    // =========================================================
    // STAGE INSTRUCTIONS
    // =========================================================

    private string GetStageInstruction(
        int stage)
    {
        switch (stage)
        {
            case 1:
                return
                    "Harvest your first asteroid.";

            case 2:
                return
                    "Unload and process the collected asteroid.";

            case 3:
                return
                    "Sell your first processed resource.";

            case 4:
                return
                    "Reach Mining Head Lv.2, Storage Lv.1, " +
                    "and Engineer Lv.1.";

            case 5:
                return
                    "Reach Storage Lv.2.";

            case 6:
                return
                    "Reach Engineer Lv.2.";

            case 7:
                return
                    "Reach Power Lv.1.";

            case 8:
                return
                    "Reach Mining Head Lv.3.";

            case 9:
                return
                    "Reach Storage Lv.4 and Power Lv.2.";

            case 10:
                return
                    "Reach Walls Lv.1.";

            case 11:
                return
                    "Reach Mining Head Lv.4.";

            case 12:
                return
                    "Reach Weapons Lv.1.";

            case 13:
                return
                    "Reach Storage Lv.6, Walls Lv.2, " +
                    "and Weapons Lv.2.";

            case 14:
                return
                    "Reach Engineer Lv.3, Walls Lv.3, " +
                    "and Weapons Lv.3.";

            case 15:
                return
                    "Reach Power Lv.3, Walls Lv.4, " +
                    "and Weapons Lv.4.";

            case 16:
                return
                    "Reach Storage Lv.7.";

            case 17:
                return
                    "Reach Power Lv.4.";

            case 18:
                return
                    "Reach Storage Lv.8.";

            case 19:
                return
                    "Reach Mining Head Lv.5.";

            case 20:
                return
                    "Reach Walls Lv.6 and Weapons Lv.6.";

            case 21:
                return
                    "Reach Walls Lv.8 and Weapons Lv.8.";

            default:
                return
                    "Mining Tower progression complete.";
        }
    }

    // =========================================================
    // NAMES
    // =========================================================

    private string GetUpgradeName(
        MiningTowerUpgradeType type)
    {
        switch (type)
        {
            case MiningTowerUpgradeType.MiningHead:
                return "Mining Head";

            case MiningTowerUpgradeType.Storage:
                return "Storage";

            case MiningTowerUpgradeType.Engineer:
                return "Engineer";

            case MiningTowerUpgradeType.Power:
                return "Power";

            case MiningTowerUpgradeType.Walls:
                return "Walls";

            case MiningTowerUpgradeType.Weapons:
                return "Weapons";

            default:
                return "Upgrade";
        }
    }

    private void Log(
        string message)
    {
        if (!logRubricEvents)
        {
            return;
        }

        Debug.Log(
            $"[MP2B Progression] {message}",
            gameObject
        );
    }
}