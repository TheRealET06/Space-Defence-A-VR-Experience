using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class TowerActionEvent :
    UnityEvent<TowerActionType>
{
}

[Serializable]
public class MiningTowerUpgradeEvent :
    UnityEvent<MiningTowerUpgradeType>
{
}

public class MiningTowerInteractionHub : MonoBehaviour
{
    private class UnlockReveal
    {
        public string title;
        public string message;
        public Action unlockAction;

        public UnlockReveal(
            string title,
            string message,
            Action unlockAction)
        {
            this.title = title;
            this.message = message;
            this.unlockAction = unlockAction;
        }
    }

    // =========================================================
    // UI
    // =========================================================

    [Header("Upgrade Panel")]
    [SerializeField]
    private GameObject upgradePanel;

    [Header("Upgrade Button Objects")]
    [SerializeField]
    private GameObject miningHeadButton;

    [SerializeField]
    private GameObject storageButton;

    [SerializeField]
    private GameObject engineerButton;

    [SerializeField]
    private GameObject powerButton;

    [SerializeField]
    private GameObject wallsButton;

    [SerializeField]
    private GameObject weaponsButton;

    [Header("Upgrade Button Labels")]
    [SerializeField]
    private TMP_Text miningHeadButtonText;

    [SerializeField]
    private TMP_Text storageButtonText;

    [SerializeField]
    private TMP_Text engineerButtonText;

    [SerializeField]
    private TMP_Text powerButtonText;

    [SerializeField]
    private TMP_Text wallsButtonText;

    [SerializeField]
    private TMP_Text weaponsButtonText;

    [Header("Popup")]
    [SerializeField]
    private GameObject popupPanel;

    [SerializeField]
    private TMP_Text popupTitleText;

    [SerializeField]
    private TMP_Text popupMessageText;

    [SerializeField]
    private float temporaryMessageDuration = 3f;

    [SerializeField]
    private float unlockRevealDuration = 3f;

    [SerializeField]
    private float revealGapDuration = 0.2f;

    // =========================================================
    // UPGRADE PRICES
    // =========================================================

    [Header("Mining Head Costs")]
    [SerializeField]
    private int[] miningHeadCosts =
    {
        0,
        5,
        25,
        80,
        200
    };

    [Header("Storage Costs")]
    [SerializeField]
    private int[] storageCosts =
    {
        0,
        10,
        15,
        25,
        40,
        60,
        100,
        160
    };

    [Header("Engineer Costs")]
    [SerializeField]
    private int[] engineerCosts =
    {
        5,
        15,
        60
    };

    [Header("Power Costs")]
    [SerializeField]
    private int[] powerCosts =
    {
        20,
        40,
        80,
        160
    };

    [Header("Walls Costs")]
    [SerializeField]
    private int[] wallsCosts =
    {
        15,
        20,
        25,
        35,
        50,
        70,
        100,
        140
    };

    [Header("Weapons Costs")]
    [SerializeField]
    private int[] weaponsCosts =
    {
        15,
        25,
        30,
        40,
        60,
        80,
        120,
        160
    };

    // =========================================================
    // GAMEPLAY
    // =========================================================

    [Header("Gameplay")]
    [SerializeField]
    private MiningCargo cargo;

    [SerializeField]
    private MiningTowerInteraction miningInteraction;

    [SerializeField]
    private BarrelHub barrelHub;

    // =========================================================
    // ECONOMY
    // =========================================================

    [Header("Economy")]
    [SerializeField]
    private CurrencyManager currency;

    // =========================================================
    // UPGRADE SYSTEM
    // =========================================================

    [Header("Upgrade System")]
    [SerializeField]
    private MiningTowerUpgradeController upgradeController;

    [SerializeField]
    private MiningTowerProgression progression;

    // =========================================================
    // EVENTS
    // =========================================================

    [Header("Events")]
    public TowerActionEvent onActionRequested;

    public TowerActionEvent onActionRejected;

    public MiningTowerUpgradeEvent onUpgradePurchased;

    public MiningTowerUpgradeEvent onUpgradeRejected;

    // =========================================================
    // GAMEPLAY UNLOCK STATE
    // =========================================================

    private bool processingUnlocked;
    private bool sellingUnlocked;
    private bool upgradeSystemUnlocked;

    private bool miningHeadUnlocked;
    private bool storageUnlocked;
    private bool engineerUnlocked;
    private bool powerUnlocked;
    private bool wallsUnlocked;
    private bool weaponsUnlocked;

    // =========================================================
    // INTERNAL STATE
    // =========================================================

    private bool upgradePanelVisible;

    private bool previousCargoLoaded;
    private int previousBarrelCount;

    private Coroutine temporaryMessageRoutine;
    private Coroutine revealRoutine;

    private readonly Queue<UnlockReveal> revealQueue =
        new Queue<UnlockReveal>();

    private bool isUnlockRevealPlaying;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        FindMissingReferences();

        HideAllUpgradeButtons();

        upgradePanelVisible = false;

        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
        }

        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }

        previousCargoLoaded =
            cargo != null &&
            cargo.IsLoaded;

        previousBarrelCount =
            barrelHub != null
                ? barrelHub.BarrelCount
                : 0;

        progression?.BeginProgression();

        RefreshUpgradeButtonLabels();
    }

    private void OnEnable()
    {
        FindMissingReferences();

        if (progression != null)
        {
            progression.ThresholdCompleted -=
                HandleThresholdCompleted;

            progression.ThresholdCompleted +=
                HandleThresholdCompleted;
        }

        previousCargoLoaded =
            cargo != null &&
            cargo.IsLoaded;

        previousBarrelCount =
            barrelHub != null
                ? barrelHub.BarrelCount
                : 0;

        SyncUnlockStateFromProgression();

        ShowTemporaryMessage(
            "MINING ONLINE",
            "SELECT the Mining Tower to harvest the " +
            "highest-value asteroid within range."
        );
    }

    private void OnDisable()
    {
        if (progression != null)
        {
            progression.ThresholdCompleted -=
                HandleThresholdCompleted;
        }

        if (temporaryMessageRoutine != null)
        {
            StopCoroutine(
                temporaryMessageRoutine
            );

            temporaryMessageRoutine = null;
        }

        if (revealRoutine != null)
        {
            StopCoroutine(
                revealRoutine
            );

            revealRoutine = null;
        }
    }

    private void Update()
    {
        CheckGameplayStateChanges();
    }

    // =========================================================
    // REFERENCES
    // =========================================================

    private void FindMissingReferences()
    {
        if (currency == null)
        {
            currency =
                FindFirstObjectByType<CurrencyManager>();
        }

        if (cargo == null)
        {
            cargo =
                GetComponentInChildren<MiningCargo>(
                    true
                );
        }

        if (miningInteraction == null)
        {
            miningInteraction =
                GetComponentInChildren<MiningTowerInteraction>(
                    true
                );
        }

        if (barrelHub == null)
        {
            barrelHub =
                GetComponentInChildren<BarrelHub>(
                    true
                );
        }

        if (upgradeController == null)
        {
            upgradeController =
                GetComponentInChildren<MiningTowerUpgradeController>(
                    true
                );
        }

        if (progression == null)
        {
            progression =
                GetComponentInChildren<MiningTowerProgression>(
                    true
                );
        }
    }

    // =========================================================
    // XR INTERACTION
    // =========================================================

    public void OnTowerSelected()
    {
        if (isUnlockRevealPlaying)
        {
            return;
        }

        ExecutePrimarySelectAction();
    }

    public void OnTowerDeselected()
    {
    }

    public void ActivatePrimaryAction()
    {
        if (isUnlockRevealPlaying)
        {
            return;
        }

        if (!upgradeSystemUnlocked)
        {
            ShowError(
                "UPGRADES LOCKED",
                "Sell your first processed resource to " +
                "unlock the Engineering Console."
            );

            return;
        }

        if (upgradePanel == null)
        {
            return;
        }

        if (upgradePanelVisible)
        {
            HideTowerUI();
            return;
        }

        if (progression != null)
        {
            if (
                !progression.CanOpenUpgradePanel(
                    out string errorTitle,
                    out string errorMessage
                )
            )
            {
                ShowError(
                    errorTitle,
                    errorMessage
                );

                return;
            }
        }

        ShowTowerUI();
    }

    public void ShowTowerUI()
    {
        upgradePanelVisible = true;

        if (upgradePanel != null)
        {
            upgradePanel.SetActive(true);
        }

        RefreshUpgradeButtonLabels();
    }

    public void HideTowerUI()
    {
        upgradePanelVisible = false;

        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
        }
    }

    // =========================================================
    // SELECT STATE MACHINE
    // =========================================================

    private void ExecutePrimarySelectAction()
    {
        int barrelCount =
            barrelHub != null
                ? barrelHub.BarrelCount
                : 0;

        bool cargoLoaded =
            cargo != null &&
            cargo.IsLoaded;

        // Sell has highest priority.
        if (barrelCount > 0)
        {
            if (!sellingUnlocked)
            {
                ShowError(
                    "TRADE LINK OFFLINE",
                    "Complete your first processing cycle " +
                    "to unlock resource selling."
                );

                return;
            }

            TrySellStoredResources();

            return;
        }

        // Loaded cargo means the player intends to unload/process.
        if (cargoLoaded)
        {
            if (!processingUnlocked)
            {
                ShowError(
                    "PROCESSOR OFFLINE",
                    "Complete the current unlock reveal before " +
                    "unloading the harvested cargo."
                );

                return;
            }

            ExecuteMiningRocketAction();

            return;
        }

        // Harvest is available from the beginning.
        ExecuteMiningRocketAction();
    }

    // =========================================================
    // HARVEST / PROCESSING
    // =========================================================

    private void ExecuteMiningRocketAction()
    {
        if (
            miningInteraction == null ||
            cargo == null
        )
        {
            ShowError(
                "SYSTEM UNAVAILABLE",
                "The Mining Rocket is not ready."
            );

            return;
        }

        TowerActionType action =
            cargo.IsLoaded
                ? TowerActionType.Unload
                : TowerActionType.Harvest;

        miningInteraction.Interact();

        onActionRequested?.Invoke(
            action
        );
    }

    // =========================================================
    // SELLING
    // =========================================================

    private void TrySellStoredResources()
    {
        if (!sellingUnlocked)
        {
            ShowError(
                "TRADE LINK OFFLINE",
                "Complete your first processing cycle " +
                "before selling resources."
            );

            return;
        }

        if (barrelHub == null)
        {
            ShowError(
                "SELL SYSTEM UNAVAILABLE",
                "The Mining Tower storage system is unavailable."
            );

            return;
        }

        if (barrelHub.BarrelCount <= 0)
        {
            ShowError(
                "NOTHING TO SELL",
                "Process asteroid cargo into resource barrels first."
            );

            onActionRejected?.Invoke(
                TowerActionType.Sell
            );

            return;
        }

        barrelHub.SellAll();

        onActionRequested?.Invoke(
            TowerActionType.Sell
        );

        progression?.NotifySaleCompleted();

        RefreshUpgradeButtonLabels();
    }

    // =========================================================
    // GAMEPLAY STATE DETECTION
    // =========================================================

    private void CheckGameplayStateChanges()
    {
        bool cargoLoaded =
            cargo != null &&
            cargo.IsLoaded;

        if (
            !previousCargoLoaded &&
            cargoLoaded
        )
        {
            progression?.NotifyHarvestCompleted();
        }

        previousCargoLoaded =
            cargoLoaded;

        int barrelCount =
            barrelHub != null
                ? barrelHub.BarrelCount
                : 0;

        if (
            previousBarrelCount <= 0 &&
            barrelCount > 0
        )
        {
            progression?.NotifyProcessingCompleted();
        }

        previousBarrelCount =
            barrelCount;
    }

    // =========================================================
    // THRESHOLD -> REVEAL ROUTING
    // =========================================================

    private void HandleThresholdCompleted(
        int completedThreshold)
    {
        switch (completedThreshold)
        {
            // -------------------------------------------------
            // REVEAL 1: PROCESSING
            // -------------------------------------------------

            case 1:
                QueueUnlockReveal(
                    "PROCESSING ONLINE",
                    "The Orbital Processor converts harvested " +
                    "asteroid cargo into resource barrels. " +
                    "SELECT the Mining Tower again while cargo " +
                    "is loaded to unload and begin processing.",
                    UnlockProcessing
                );
                break;

            // -------------------------------------------------
            // REVEAL 2: SELLING
            // -------------------------------------------------

            case 2:
                QueueUnlockReveal(
                    "TRADE LINK ONLINE",
                    "Processed resource barrels can now be sold " +
                    "for Credits. SELECT the Mining Tower while " +
                    "barrels are stored to sell them.",
                    UnlockSelling
                );
                break;

            // -------------------------------------------------
            // REVEALS 3-6
            // -------------------------------------------------

            case 3:
                QueueUnlockReveal(
                    "ENGINEERING CONSOLE ONLINE",
                    "The Mining Tower can now be specialized. " +
                    "ACTIVATE the tower to open its Engineering " +
                    "Console and spend Credits on upgrades.",
                    UnlockUpgradeSystem
                );

                QueueUnlockReveal(
                    "MINING HEAD UNLOCKED",
                    "The Mining Head controls extraction capability. " +
                    "Higher levels extend mining reach and unlock " +
                    "access to increasingly valuable asteroid families.",
                    UnlockMiningHead
                );

                QueueUnlockReveal(
                    "STORAGE UNLOCKED",
                    "Repository Arrays increase the number of processed " +
                    "resource barrels the Mining Tower can hold before " +
                    "they must be sold.",
                    UnlockStorage
                );

                QueueUnlockReveal(
                    "ENGINEER UNLOCKED",
                    "Engineers maintain the Mining Tower automatically. " +
                    "Higher Engineer levels increase passive repair " +
                    "capability.",
                    UnlockEngineer
                );
                break;

            // -------------------------------------------------
            // REVEAL 7: POWER
            // -------------------------------------------------

            case 6:
                QueueUnlockReveal(
                    "POWER CORE UNLOCKED",
                    "Power upgrades accelerate the Orbital Processor, " +
                    "allowing harvested cargo to become resource " +
                    "barrels more quickly.",
                    UnlockPower
                );
                break;

            // -------------------------------------------------
            // REVEAL 8: WALLS
            // -------------------------------------------------

            case 9:
                QueueUnlockReveal(
                    "DEFENSIVE WALLS UNLOCKED",
                    "Defensive Walls reinforce the Mining Tower. " +
                    "Each level increases its shield capacity and " +
                    "improves survivability.",
                    UnlockWalls
                );
                break;

            // -------------------------------------------------
            // REVEAL 9: WEAPONS
            // -------------------------------------------------

            case 11:
                QueueUnlockReveal(
                    "WEAPON ARRAY UNLOCKED",
                    "Weapon modules transform the Mining Tower into " +
                    "an armed outpost. Higher levels construct " +
                    "additional defensive weapon systems.",
                    UnlockWeapons
                );
                break;
        }
    }

    // =========================================================
    // REVEAL QUEUE
    // =========================================================

    private void QueueUnlockReveal(
        string title,
        string message,
        Action unlockAction)
    {
        revealQueue.Enqueue(
            new UnlockReveal(
                title,
                message,
                unlockAction
            )
        );

        if (revealRoutine == null)
        {
            revealRoutine =
                StartCoroutine(
                    ProcessRevealQueue()
                );
        }
    }

    private IEnumerator ProcessRevealQueue()
    {
        isUnlockRevealPlaying = true;

        if (temporaryMessageRoutine != null)
        {
            StopCoroutine(
                temporaryMessageRoutine
            );

            temporaryMessageRoutine = null;
        }

        HideTowerUI();

        while (revealQueue.Count > 0)
        {
            UnlockReveal reveal =
                revealQueue.Dequeue();

            Debug.Log(
                $"[Unlock Reveal] {reveal.title}: " +
                reveal.message,
                gameObject
            );

            if (
                popupPanel != null &&
                popupTitleText != null &&
                popupMessageText != null
            )
            {
                popupTitleText.text =
                    reveal.title;

                popupMessageText.text =
                    reveal.message;

                popupPanel.SetActive(true);

                yield return new WaitForSecondsRealtime(
                    unlockRevealDuration
                );

                // Critical rubric ordering:
                // hide reveal first...
                popupPanel.SetActive(false);
            }

            // ...then make the mechanic/UI available.
            reveal.unlockAction?.Invoke();

            RefreshUpgradeButtonLabels();

            yield return new WaitForSecondsRealtime(
                revealGapDuration
            );
        }

        isUnlockRevealPlaying = false;
        revealRoutine = null;
    }

    // =========================================================
    // UNLOCK ACTIONS
    // =========================================================

    private void UnlockProcessing()
    {
        processingUnlocked = true;

        Debug.Log(
            "[Unlock] Processing available.",
            gameObject
        );
    }

    private void UnlockSelling()
    {
        sellingUnlocked = true;

        Debug.Log(
            "[Unlock] Selling available.",
            gameObject
        );
    }

    private void UnlockUpgradeSystem()
    {
        upgradeSystemUnlocked = true;

        Debug.Log(
            "[Unlock] Engineering Console available.",
            gameObject
        );
    }

    private void UnlockMiningHead()
    {
        miningHeadUnlocked = true;

        SetButtonVisible(
            miningHeadButton,
            true
        );
    }

    private void UnlockStorage()
    {
        storageUnlocked = true;

        SetButtonVisible(
            storageButton,
            true
        );
    }

    private void UnlockEngineer()
    {
        engineerUnlocked = true;

        SetButtonVisible(
            engineerButton,
            true
        );
    }

    private void UnlockPower()
    {
        powerUnlocked = true;

        SetButtonVisible(
            powerButton,
            true
        );
    }

    private void UnlockWalls()
    {
        wallsUnlocked = true;

        SetButtonVisible(
            wallsButton,
            true
        );
    }

    private void UnlockWeapons()
    {
        weaponsUnlocked = true;

        SetButtonVisible(
            weaponsButton,
            true
        );
    }

    // =========================================================
    // RESTORE STATE
    // =========================================================

    private void SyncUnlockStateFromProgression()
    {
        if (progression == null)
        {
            return;
        }

        int stage =
            progression.CurrentStage;

        if (stage >= 2)
        {
            processingUnlocked = true;
        }

        if (stage >= 3)
        {
            sellingUnlocked = true;
        }

        if (stage >= 4)
        {
            upgradeSystemUnlocked = true;

            miningHeadUnlocked = true;
            storageUnlocked = true;
            engineerUnlocked = true;

            SetButtonVisible(
                miningHeadButton,
                true
            );

            SetButtonVisible(
                storageButton,
                true
            );

            SetButtonVisible(
                engineerButton,
                true
            );
        }

        if (stage >= 7)
        {
            powerUnlocked = true;

            SetButtonVisible(
                powerButton,
                true
            );
        }

        if (stage >= 10)
        {
            wallsUnlocked = true;

            SetButtonVisible(
                wallsButton,
                true
            );
        }

        if (stage >= 12)
        {
            weaponsUnlocked = true;

            SetButtonVisible(
                weaponsButton,
                true
            );
        }
    }

    // =========================================================
    // BUTTON VISIBILITY
    // =========================================================

    private void HideAllUpgradeButtons()
    {
        SetButtonVisible(
            miningHeadButton,
            false
        );

        SetButtonVisible(
            storageButton,
            false
        );

        SetButtonVisible(
            engineerButton,
            false
        );

        SetButtonVisible(
            powerButton,
            false
        );

        SetButtonVisible(
            wallsButton,
            false
        );

        SetButtonVisible(
            weaponsButton,
            false
        );
    }

    private void SetButtonVisible(
        GameObject button,
        bool visible)
    {
        if (button != null)
        {
            button.SetActive(
                visible
            );
        }
    }

    // =========================================================
    // PURCHASE
    // =========================================================

    public bool TryPurchaseUpgrade(
        MiningTowerUpgradeType type)
    {
        if (!IsUpgradeUnlocked(type))
        {
            ShowError(
                "UPGRADE NOT DISCOVERED",
                $"{GetUpgradeDisplayName(type)} has not been " +
                "unlocked yet. Complete the current objective first."
            );

            onUpgradeRejected?.Invoke(
                type
            );

            return false;
        }

        if (
            currency == null ||
            upgradeController == null
        )
        {
            ShowError(
                "UPGRADE SYSTEM UNAVAILABLE",
                "The Mining Tower upgrade system is unavailable."
            );

            onUpgradeRejected?.Invoke(
                type
            );

            return false;
        }

        if (progression != null)
        {
            if (
                !progression.TryValidateUpgrade(
                    type,
                    out string lockTitle,
                    out string lockMessage
                )
            )
            {
                ShowError(
                    lockTitle,
                    lockMessage
                );

                onUpgradeRejected?.Invoke(
                    type
                );

                return false;
            }
        }

        if (!upgradeController.CanUpgrade(type))
        {
            progression?.RegisterMaxLevelCatch();

            ShowError(
                "MAXIMUM LEVEL",
                $"{GetUpgradeDisplayName(type)} is already at " +
                "its maximum level."
            );

            onUpgradeRejected?.Invoke(
                type
            );

            return false;
        }

        int currentLevel =
            upgradeController.GetLevel(
                type
            );

        int targetLevel =
            currentLevel + 1;

        if (
            !TryGetUpgradeCost(
                type,
                targetLevel,
                out int cost
            )
        )
        {
            ShowError(
                "PRICE NOT CONFIGURED",
                $"{GetUpgradeDisplayName(type)} Lv.{targetLevel} " +
                "does not have a valid price configured."
            );

            return false;
        }

        if (!currency.TrySpend(cost))
        {
            progression?.RegisterInsufficientCreditsCatch();

            ShowError(
                "NOT ENOUGH CREDITS",
                $"{GetUpgradeDisplayName(type)} Lv.{targetLevel} " +
                $"costs {cost} Credits. " +
                "Sell more processed resources first."
            );

            onUpgradeRejected?.Invoke(
                type
            );

            return false;
        }

        bool success =
            upgradeController.ApplyUpgrade(
                type
            );

        if (!success)
        {
            currency.AddMoney(
                cost
            );

            ShowError(
                "UPGRADE FAILED",
                $"{GetUpgradeDisplayName(type)} could not be upgraded."
            );

            onUpgradeRejected?.Invoke(
                type
            );

            return false;
        }

        Debug.Log(
            "[TowerUpgrade] Purchased " +
            $"{GetUpgradeDisplayName(type)} " +
            $"Lv.{targetLevel} for {cost} Credits.",
            gameObject
        );

        onUpgradePurchased?.Invoke(
            type
        );

        progression?.NotifyUpgradePurchased(
            type
        );

        RefreshUpgradeButtonLabels();

        return true;
    }

    private bool IsUpgradeUnlocked(
        MiningTowerUpgradeType type)
    {
        switch (type)
        {
            case MiningTowerUpgradeType.MiningHead:
                return miningHeadUnlocked;

            case MiningTowerUpgradeType.Storage:
                return storageUnlocked;

            case MiningTowerUpgradeType.Engineer:
                return engineerUnlocked;

            case MiningTowerUpgradeType.Power:
                return powerUnlocked;

            case MiningTowerUpgradeType.Walls:
                return wallsUnlocked;

            case MiningTowerUpgradeType.Weapons:
                return weaponsUnlocked;

            default:
                return false;
        }
    }

    // =========================================================
    // SIX BUTTON METHODS
    // =========================================================

    public void BuyMiningHeadUpgrade()
    {
        TryPurchaseUpgrade(
            MiningTowerUpgradeType.MiningHead
        );
    }

    public void BuyStorageUpgrade()
    {
        TryPurchaseUpgrade(
            MiningTowerUpgradeType.Storage
        );
    }

    public void BuyEngineerUpgrade()
    {
        TryPurchaseUpgrade(
            MiningTowerUpgradeType.Engineer
        );
    }

    public void BuyPowerUpgrade()
    {
        TryPurchaseUpgrade(
            MiningTowerUpgradeType.Power
        );
    }

    public void BuyWallsUpgrade()
    {
        TryPurchaseUpgrade(
            MiningTowerUpgradeType.Walls
        );
    }

    public void BuyWeaponsUpgrade()
    {
        TryPurchaseUpgrade(
            MiningTowerUpgradeType.Weapons
        );
    }

    // =========================================================
    // COSTS
    // =========================================================

    private bool TryGetUpgradeCost(
        MiningTowerUpgradeType type,
        int targetLevel,
        out int cost)
    {
        cost = 0;

        int[] table =
            GetCostTable(type);

        if (table == null)
        {
            return false;
        }

        int index =
            targetLevel - 1;

        if (
            index < 0 ||
            index >= table.Length
        )
        {
            return false;
        }

        cost =
            table[index];

        return cost >= 0;
    }

    private int[] GetCostTable(
        MiningTowerUpgradeType type)
    {
        switch (type)
        {
            case MiningTowerUpgradeType.MiningHead:
                return miningHeadCosts;

            case MiningTowerUpgradeType.Storage:
                return storageCosts;

            case MiningTowerUpgradeType.Engineer:
                return engineerCosts;

            case MiningTowerUpgradeType.Power:
                return powerCosts;

            case MiningTowerUpgradeType.Walls:
                return wallsCosts;

            case MiningTowerUpgradeType.Weapons:
                return weaponsCosts;

            default:
                return null;
        }
    }

    // =========================================================
    // BUTTON LABELS
    // =========================================================

    private void RefreshUpgradeButtonLabels()
    {
        if (upgradeController == null)
        {
            return;
        }

        RefreshButtonLabel(
            miningHeadButtonText,
            MiningTowerUpgradeType.MiningHead
        );

        RefreshButtonLabel(
            storageButtonText,
            MiningTowerUpgradeType.Storage
        );

        RefreshButtonLabel(
            engineerButtonText,
            MiningTowerUpgradeType.Engineer
        );

        RefreshButtonLabel(
            powerButtonText,
            MiningTowerUpgradeType.Power
        );

        RefreshButtonLabel(
            wallsButtonText,
            MiningTowerUpgradeType.Walls
        );

        RefreshButtonLabel(
            weaponsButtonText,
            MiningTowerUpgradeType.Weapons
        );
    }

    private void RefreshButtonLabel(
        TMP_Text label,
        MiningTowerUpgradeType type)
    {
        if (
            label == null ||
            upgradeController == null
        )
        {
            return;
        }

        int currentLevel =
            upgradeController.GetLevel(
                type
            );

        int maxLevel =
            upgradeController.GetMaxLevel(
                type
            );

        string displayName =
            GetUpgradeDisplayName(type);

        if (currentLevel >= maxLevel)
        {
            label.text =
                $"{displayName} Lv.{currentLevel}\nMAX";

            return;
        }

        int targetLevel =
            currentLevel + 1;

        if (
            TryGetUpgradeCost(
                type,
                targetLevel,
                out int cost
            )
        )
        {
            label.text =
                $"{displayName} Lv.{currentLevel}\n" +
                $"Lv↑: {cost} C"; // .{targetLevel}
        }
        else
        {
            label.text =
                $"{displayName} Lv.{currentLevel}";
        }
    }

    private string GetUpgradeDisplayName(
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

    // =========================================================
    // TEMPORARY POPUPS
    // =========================================================

    public void ShowError(
        string title,
        string message)
    {
        ShowTemporaryMessage(
            title,
            message
        );
    }

    public void ShowUnlock(
        string title,
        string message)
    {
        ShowTemporaryMessage(
            title,
            message
        );
    }

    private void ShowTemporaryMessage(
        string title,
        string message)
    {
        // Do not overwrite a rubric Unlock Reveal.
        if (isUnlockRevealPlaying)
        {
            return;
        }

        if (
            popupPanel == null ||
            popupTitleText == null ||
            popupMessageText == null
        )
        {
            Debug.Log(
                $"[TowerPopup] {title}: {message}",
                gameObject
            );

            return;
        }

        if (temporaryMessageRoutine != null)
        {
            StopCoroutine(
                temporaryMessageRoutine
            );
        }

        temporaryMessageRoutine =
            StartCoroutine(
                TemporaryMessageRoutine(
                    title,
                    message
                )
            );
    }

    private IEnumerator TemporaryMessageRoutine(
        string title,
        string message)
    {
        popupTitleText.text =
            title;

        popupMessageText.text =
            message;

        popupPanel.SetActive(true);

        yield return new WaitForSecondsRealtime(
            temporaryMessageDuration
        );

        popupPanel.SetActive(false);

        temporaryMessageRoutine = null;
    }
}