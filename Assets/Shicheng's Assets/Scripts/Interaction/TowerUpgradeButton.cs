using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TowerUpgradeButton : MonoBehaviour
{
    [SerializeField]
    TowerActionType action;

    [SerializeField]
    string displayName;

    [SerializeField]
    MiningTowerInteractionHub interactionHub;

    [SerializeField]
    MiningTowerUpgradeController upgradeController;

    [SerializeField]
    CurrencyManager currency;

    [SerializeField]
    Button button;

    [SerializeField]
    TMP_Text label;

    [SerializeField]
    int cost;

    float refreshTimer;

    void Awake()
    {
        if (currency == null)
        {
            currency =
                FindFirstObjectByType<CurrencyManager>();
        }

        if (button == null)
        {
            button =
                GetComponent<Button>();
        }

        if (
            label == null
        )
        {
            label =
                GetComponentInChildren<TMP_Text>();
        }

        button.onClick.AddListener(
            Purchase
        );
    }

    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(
                Purchase
            );
        }
    }

    void Update()
    {
        refreshTimer += Time.deltaTime;

        if (refreshTimer < 0.2f)
            return;

        refreshTimer = 0f;

        Refresh();
    }

    void Purchase()
    {
        if (interactionHub == null)
            return;

        interactionHub.TryPurchaseUpgrade(
            action
        );

        Refresh();
    }

    void Refresh()
    {
        if (
            upgradeController == null ||
            button == null ||
            label == null
        )
        {
            return;
        }

        bool canUpgrade =
            upgradeController.CanUpgrade(
                action
            );

        bool canAfford =
            currency != null &&
            currency.CanAfford(cost);

        button.interactable =
            canUpgrade &&
            canAfford;

        if (!canUpgrade)
        {
            label.text =
                $"{displayName}\nMAX";

            return;
        }

        label.text =
            $"{displayName}\n${cost}";
    }
}