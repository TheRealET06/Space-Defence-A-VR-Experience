using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

[Serializable]
public class TowerActionEvent :
    UnityEvent<TowerActionType>
{
}

public class MiningTowerInteractionHub : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    CanvasGroup resourceCanvasGroup;

    [SerializeField]
    CanvasGroup actionCanvasGroup;

    [Header("Feedback UI")]
    [SerializeField]
    CanvasGroup feedbackCanvasGroup;

    [SerializeField]
    CanvasGroup popupPanelGroup;

    [SerializeField]
    TMP_Text popupTitleText;

    [SerializeField]
    TMP_Text popupMessageText;

    [SerializeField]
    float popupDuration = 4f;

    [SerializeField]
    TMP_Text statusText;

    [FormerlySerializedAs("nextActionText")]
    [SerializeField]
    TMP_Text towerStatusText;

    [SerializeField]
    TMP_Text actionHintText;

    [SerializeField]
    TMP_Text creditsText;

    [Header("Gameplay")]
    [SerializeField]
    MiningCargo cargo;

    [SerializeField]
    MiningTowerInteraction miningInteraction;

    [SerializeField]
    BarrelHub barrelHub;

    [Header("Economy")]
    [SerializeField]
    CurrencyManager currency;

    [Header("Upgrade System")]
    [SerializeField]
    MiningTowerUpgradeController upgradeController;

    [Header("Upgrade Definitions")]
    [SerializeField]
    TowerUpgradeDefinition[] upgrades;

    [Header("Events")]
    public TowerActionEvent onActionRequested;
    public TowerActionEvent onUpgradePurchased;
    public TowerActionEvent onActionRejected;

    bool uiVisible;

    float refreshTimer;

    const float RefreshInterval = 0.15f;

    Coroutine popupRoutine;

    bool previousCargoLoaded;
    int previousBarrelCount;

    // These flags are shared by every MiningTower instance.
    // Therefore each tutorial is shown only once per play session.
    static bool harvestLessonShown;
    static bool processingLessonShown;
    static bool sellingLessonShown;
    static bool upgradeLessonShown;

    // This also resets the static flags correctly when
    // Enter Play Mode Options disables domain reload.
    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetGlobalTutorialState()
    {
        harvestLessonShown = false;
        processingLessonShown = false;
        sellingLessonShown = false;
        upgradeLessonShown = false;
    }

    void Awake()
    {
        if (currency == null)
        {
            currency =
                FindFirstObjectByType<CurrencyManager>();
        }

        // ResourceCanvas is retained for MP2A,
        // but MP2B never displays it.
        SetCanvasVisible(
            resourceCanvasGroup,
            false,
            false
        );

        // ActionCanvas starts hidden.
        SetCanvasVisible(
            actionCanvasGroup,
            false,
            false
        );

        // FeedbackCanvas is always available,
        // but the temporary popup starts hidden.
        SetCanvasVisible(
            feedbackCanvasGroup,
            true,
            false
        );

        SetCanvasVisible(
            popupPanelGroup,
            false,
            false
        );

        previousCargoLoaded =
            cargo != null &&
            cargo.IsLoaded;

        previousBarrelCount =
            barrelHub != null
                ? barrelHub.BarrelCount
                : 0;
    }

    void OnEnable()
    {
        if (currency != null)
        {
            currency.OnMoneyChanged +=
                HandleMoneyChanged;
        }

        previousCargoLoaded =
            cargo != null &&
            cargo.IsLoaded;

        previousBarrelCount =
            barrelHub != null
                ? barrelHub.BarrelCount
                : 0;

        // MiningTowerInteractionHub is expected to become enabled
        // when tower construction is complete.
        ShowHarvestLessonOnce();
    }

    void OnDisable()
    {
        if (currency != null)
        {
            currency.OnMoneyChanged -=
                HandleMoneyChanged;
        }

        if (popupRoutine != null)
        {
            StopCoroutine(popupRoutine);
            popupRoutine = null;
        }
    }

    void Update()
    {
        // Tutorial state detection must continue even while
        // ActionCanvas is hidden.
        CheckTutorialStateChanges();

        if (!uiVisible)
            return;

        refreshTimer += Time.deltaTime;

        if (refreshTimer <
            RefreshInterval)
        {
            return;
        }

        refreshTimer = 0f;

        RefreshUI();
    }

    void HandleMoneyChanged(
        int newMoney
    )
    {
        if (uiVisible)
        {
            RefreshUI();
        }
    }

    // Called by XR Simple Interactable -> Select Entered.
    // Select operates the tower. It does not control the UI.
    public void OnTowerSelected()
    {
        ExecutePrimarySelectAction();
    }

    // No automatic action when Select exits.
    public void OnTowerDeselected()
    {
    }

    // Called by XR Simple Interactable -> Activated.
    // Activate only opens or closes ActionCanvas.
    public void ActivatePrimaryAction()
    {
        ToggleActionCanvas();
    }

    public void ToggleActionCanvas()
    {
        if (uiVisible)
        {
            HideTowerUI();
        }
        else
        {
            ShowTowerUI();
        }
    }

    public void ShowTowerUI()
    {
        uiVisible = true;

        // ResourceCanvas intentionally remains hidden.
        SetCanvasVisible(
            resourceCanvasGroup,
            false,
            false
        );

        SetCanvasVisible(
            actionCanvasGroup,
            true,
            true
        );

        RefreshUI();
    }

    public void HideTowerUI()
    {
        uiVisible = false;

        // ResourceCanvas intentionally remains hidden.
        SetCanvasVisible(
            resourceCanvasGroup,
            false,
            false
        );

        SetCanvasVisible(
            actionCanvasGroup,
            false,
            false
        );
    }

    // SELECT priority:
    // 1. Loaded cargo -> Unload.
    // 2. Stored barrels -> Sell.
    // 3. Otherwise -> Harvest.
    void ExecutePrimarySelectAction()
    {
        if (
            cargo != null &&
            cargo.IsLoaded
        )
        {
            ExecuteMiningRocketAction();
            return;
        }

        if (
            barrelHub != null &&
            barrelHub.BarrelCount > 0
        )
        {
            TrySellStoredResources();
            return;
        }

        ExecuteMiningRocketAction();
    }

    void ExecuteMiningRocketAction()
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

        Debug.Log(
            $"[TowerInteraction] {action} requested.",
            gameObject
        );

        // MiningTowerInteraction already decides whether
        // to begin harvesting or unload loaded cargo.
        miningInteraction.Interact();

        onActionRequested?.Invoke(action);

        RefreshUI();
    }

    void TrySellStoredResources()
    {
        if (barrelHub == null)
        {
            return;
        }

        if (barrelHub.BarrelCount <= 0)
        {
            Debug.Log(
                "[TowerInteraction] Sell ignored. No barrels stored.",
                gameObject
            );

            ShowError(
                "NOTHING TO SELL",
                "Process asteroid cargo before selling resources."
            );

            onActionRejected?.Invoke(
                TowerActionType.Sell
            );

            return;
        }

        Debug.Log(
            "[TowerInteraction] SELL requested.",
            gameObject
        );

        barrelHub.SellAll();

        onActionRequested?.Invoke(
            TowerActionType.Sell
        );

        // The first successful sale introduces the upgrade system.
        ShowUpgradeLessonOnce();

        RefreshUI();
    }

    public bool TryPurchaseUpgrade(
        TowerActionType action
    )
    {
        TowerUpgradeDefinition definition =
            FindUpgrade(action);

        if (
            definition == null ||
            currency == null ||
            upgradeController == null
        )
        {
            ShowError(
                "UPGRADE UNAVAILABLE",
                "This upgrade is currently unavailable."
            );

            onActionRejected?.Invoke(action);

            return false;
        }

        if (!upgradeController.CanUpgrade(action))
        {
            Debug.Log(
                $"[TowerUpgrade] {definition.displayName} is MAX.",
                gameObject
            );

            ShowError(
                "UPGRADE UNAVAILABLE",
                $"{definition.displayName} cannot be upgraded further."
            );

            onActionRejected?.Invoke(action);

            RefreshUI();

            return false;
        }

        if (!currency.TrySpend(definition.cost))
        {
            Debug.Log(
                $"[TowerUpgrade] Not enough Credits for " +
                $"{definition.displayName}.",
                gameObject
            );

            ShowError(
                "NOT ENOUGH CREDITS",
                $"Need {definition.cost} Credits for " +
                $"{definition.displayName}. Sell more resources first."
            );

            onActionRejected?.Invoke(action);

            RefreshUI();

            return false;
        }

        bool applied =
            upgradeController.ApplyUpgrade(
                action
            );

        if (!applied)
        {
            currency.AddMoney(
                definition.cost
            );

            ShowError(
                "UPGRADE FAILED",
                $"{definition.displayName} could not be applied."
            );

            onActionRejected?.Invoke(action);

            RefreshUI();

            return false;
        }

        Debug.Log(
            $"[TowerUpgrade] Purchased " +
            $"{definition.displayName} for " +
            $"${definition.cost}.",
            gameObject
        );

        onUpgradePurchased?.Invoke(action);

        RefreshUI();

        return true;
    }

    public void BuyMiningRangeUpgrade()
    {
        TryPurchaseUpgrade(
            TowerActionType.UpgradeMiningRange
        );
    }

    public void BuyStorageUpgrade()
    {
        TryPurchaseUpgrade(
            TowerActionType.UpgradeStorage
        );
    }

    // Kept for compatibility with the existing upgrade system.
    // It does not need a visible button in the new six-button UI.
    public void BuyProcessingSpeedUpgrade()
    {
        TryPurchaseUpgrade(
            TowerActionType.UpgradeProcessingSpeed
        );
    }

    public void BuyEngineerUpgrade()
    {
        TryPurchaseUpgrade(
            TowerActionType.UpgradeEngineer
        );
    }

    public void BuyPowerSystemUpgrade()
    {
        TryPurchaseUpgrade(
            TowerActionType.UpgradePowerSystem
        );
    }

    public void BuyWallsUpgrade()
    {
        TryPurchaseUpgrade(
            TowerActionType.UpgradeWalls
        );
    }

    public void BuyWeaponsUpgrade()
    {
        TryPurchaseUpgrade(
            TowerActionType.UpgradeWeapons
        );
    }

    TowerUpgradeDefinition FindUpgrade(
        TowerActionType action
    )
    {
        if (upgrades == null)
            return null;

        foreach (
            TowerUpgradeDefinition definition
            in upgrades
        )
        {
            if (
                definition != null &&
                definition.action == action
            )
            {
                return definition;
            }
        }

        return null;
    }

    void CheckTutorialStateChanges()
    {
        bool cargoLoaded =
            cargo != null &&
            cargo.IsLoaded;

        // A transition from no cargo to loaded cargo means
        // the first asteroid has been successfully collected.
        if (
            !previousCargoLoaded &&
            cargoLoaded
        )
        {
            ShowProcessingLessonOnce();
        }

        previousCargoLoaded =
            cargoLoaded;

        int barrelCount =
            barrelHub != null
                ? barrelHub.BarrelCount
                : 0;

        // The first produced barrel introduces selling.
        if (
            previousBarrelCount <= 0 &&
            barrelCount > 0
        )
        {
            ShowSellingLessonOnce();
        }

        previousBarrelCount =
            barrelCount;
    }

    // Can also be called explicitly by the construction script
    // if the InteractionHub remains enabled during construction.
    public void NotifyConstructionComplete()
    {
        ShowHarvestLessonOnce();
    }

    void ShowHarvestLessonOnce()
    {
        if (harvestLessonShown)
            return;

        harvestLessonShown = true;

        ShowLesson(
            "MINING ONLINE",
            "SELECT the Mining Tower to harvest the highest-value " +
            "asteroid currently available within mining range."
        );
    }

    void ShowProcessingLessonOnce()
    {
        if (processingLessonShown)
            return;

        processingLessonShown = true;

        ShowLesson(
            "CARGO ACQUIRED",
            "Asteroid material is now stored in the Mining Rocket. " +
            "SELECT the Mining Tower again to unload and process it."
        );
    }

    void ShowSellingLessonOnce()
    {
        if (sellingLessonShown)
            return;

        sellingLessonShown = true;

        ShowLesson(
            "RESOURCE READY",
            "Processing produced a resource barrel. " +
            "SELECT the Mining Tower to sell stored resources for Credits."
        );
    }

    void ShowUpgradeLessonOnce()
    {
        if (upgradeLessonShown)
            return;

        upgradeLessonShown = true;

        ShowLesson(
            "UPGRADES ONLINE",
            "Your first sale earned Credits. ACTIVATE the Mining Tower " +
            "to open the management panel and purchase upgrades."
        );
    }

    public void ShowLesson(
        string title,
        string message
    )
    {
        ShowPopup(
            title,
            message
        );
    }

    public void ShowError(
        string title,
        string message
    )
    {
        ShowPopup(
            title,
            message
        );
    }

    public void ShowUnlock(
        string title,
        string message
    )
    {
        ShowPopup(
            title,
            message
        );
    }

    void ShowPopup(
        string title,
        string message
    )
    {
        if (
            popupPanelGroup == null ||
            popupTitleText == null ||
            popupMessageText == null
        )
        {
            Debug.Log(
                $"[TowerFeedback] {title}: {message}",
                gameObject
            );

            return;
        }

        if (popupRoutine != null)
        {
            StopCoroutine(popupRoutine);
        }

        popupRoutine =
            StartCoroutine(
                PopupRoutine(
                    title,
                    message
                )
            );
    }

    IEnumerator PopupRoutine(
        string title,
        string message
    )
    {
        popupTitleText.text =
            title;

        popupMessageText.text =
            message;

        SetCanvasVisible(
            feedbackCanvasGroup,
            true,
            false
        );

        SetCanvasVisible(
            popupPanelGroup,
            true,
            false
        );

        yield return new WaitForSecondsRealtime(
            popupDuration
        );

        SetCanvasVisible(
            popupPanelGroup,
            false,
            false
        );

        popupRoutine = null;
    }

    void RefreshUI()
    {
        if (statusText != null)
        {
            statusText.text =
                "MINING TOWER";
        }

        if (
            creditsText != null &&
            currency != null
        )
        {
            creditsText.text =
                $"Credits: ${currency.Money}";
        }

        if (actionHintText != null)
        {
            actionHintText.text =
                "SELECT: Harvest / Unload / Sell\n" +
                "ACTIVATE: Show / Hide Management";
        }

        RefreshTowerStatus();
    }

    void RefreshTowerStatus()
    {
        if (
            towerStatusText == null ||
            upgradeController == null
        )
        {
            return;
        }

        string cargoState =
            cargo != null &&
            cargo.IsLoaded
                ? "LOADED"
                : "EMPTY";

        towerStatusText.text =
            "<b>MINING</b>\n" +
            $"Range Lv  {upgradeController.RangeLevel}/{upgradeController.MaxRangeLevel}" +
            $"      Range  {upgradeController.CurrentMiningRange:F1}/{upgradeController.MaxPossibleMiningRange:F1} m\n" +

            $"Proc Lv   {upgradeController.ProcessingLevel}/{upgradeController.MaxProcessingLevel}" +
            $"      Rate   {upgradeController.CurrentProcessingRate:F2}/{upgradeController.MaxPossibleProcessingRate:F2}/s\n" +

            "<b>STORAGE</b>\n" +
            $"Barrels   {upgradeController.CurrentStoredBarrels}/{upgradeController.CurrentStorageCapacity}" +
            $"      Capacity  {upgradeController.CurrentStorageCapacity}/{upgradeController.MaxStorageCapacity}\n" +

            $"Repos     {upgradeController.CurrentRepositoryCount}/{upgradeController.MaxRepositoryCount}" +
            $"      Cargo     {cargoState}\n" +

            "<b>SYSTEMS</b>\n" +
            $"Engineer  {upgradeController.ActiveEngineerCount}/{upgradeController.MaxEngineerCount}" +
            $"      Power   {upgradeController.PowerLevel}/{upgradeController.MaxPowerLevel}\n" +

            $"Walls     {upgradeController.WallsLevel}/{upgradeController.MaxWallsLevel}" +
            $"      Weapons {upgradeController.WeaponsLevel}/{upgradeController.MaxWeaponsLevel}\n" +

            "<b>DEFENSE</b>\n" +
            $"HP        {upgradeController.CurrentHealth:F0}/{upgradeController.MaxHealth:F0}" +
            $"      Shield  {upgradeController.CurrentShield:F0}/{upgradeController.MaxPossibleShield:F0}\n" +

            $"Repair    {upgradeController.CurrentRepairRate:F1} HP/s";
    }

    void SetCanvasVisible(
        CanvasGroup group,
        bool visible,
        bool interactive
    )
    {
        if (group == null)
            return;

        group.alpha =
            visible ? 1f : 0f;

        group.interactable =
            visible && interactive;

        group.blocksRaycasts =
            visible && interactive;
    }
}