using System;
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

    void Awake()
    {
        if (currency == null)
        {
            currency =
                FindFirstObjectByType<CurrencyManager>();
        }

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

    void OnEnable()
    {
        if (currency != null)
        {
            currency.OnMoneyChanged +=
                HandleMoneyChanged;
        }
    }

    void OnDisable()
    {
        if (currency != null)
        {
            currency.OnMoneyChanged -=
                HandleMoneyChanged;
        }
    }

    void Update()
    {
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
    // Every Select controls the Mining Rocket, regardless
    // of whether the UI was already visible.
    public void OnTowerSelected()
    {
        ShowTowerUI();

        ExecuteMiningRocketAction();
    }

    // No automatic hiding when the controller ray leaves.
    public void OnTowerDeselected()
    {
    }

    // Called by XR Simple Interactable -> Activated.
    // Activate is reserved for selling stored resources.
    public void ActivatePrimaryAction()
    {
        TrySellStoredResources();
    }

    public void ShowTowerUI()
    {
        uiVisible = true;

        SetCanvasVisible(
            resourceCanvasGroup,
            true,
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

    void ExecuteMiningRocketAction()
    {
        if (
            miningInteraction == null ||
            cargo == null
        )
        {
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

        // Existing interaction already decides whether
        // to start mining or unload loaded cargo.
        miningInteraction.Interact();

        onActionRequested?.Invoke(action);

        RefreshUI();
    }

    void TrySellStoredResources()
    {
        if (barrelHub == null)
            return;

        if (barrelHub.BarrelCount <= 0)
        {
            Debug.Log(
                "[TowerInteraction] Sell ignored. No barrels stored.",
                gameObject
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
            onActionRejected?.Invoke(action);
            return false;
        }

        if (!upgradeController.CanUpgrade(action))
        {
            Debug.Log(
                $"[TowerUpgrade] {definition.displayName} is MAX.",
                gameObject
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
            currency.Add(
                definition.cost
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
                "SELECT: Harvest / Unload\n" +
                "ACTIVATE: Sell stored resources";
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
            // "<b>TOWER STATUS</b>\n\n" +

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