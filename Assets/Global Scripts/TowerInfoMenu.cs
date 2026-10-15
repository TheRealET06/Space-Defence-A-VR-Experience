using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Put this on the world-space canvas object. Leave that object INACTIVE in the scene;
// TowerSelector turns it on when you select a tower.
public class TowerInfoMenu : MonoBehaviour
{
    [SerializeField] TMP_Text statsText;
    [SerializeField] Button sellButton;
    [SerializeField] TMP_Text sellButtonLabel;
    [SerializeField] Button closeButton;
    [SerializeField] float heightOffset = 0.4f;
    [SerializeField] CurrencyManager currency;   // drag it in the Inspector

    Tower current;

    void Awake()
    {
        sellButton.onClick.AddListener(Sell);
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
    }

    public void Show(Tower t)
    {
        current = t;
        statsText.text =
            $"{t.Data.displayName}\n" +
            $"Range: {t.range}\n" +
            $"Fire rate: {t.fireRate}/s\n" +
            $"Sell value: ${t.SellValue}";
        sellButtonLabel.text = $"Sell (+${t.SellValue})";

        transform.position = t.transform.position + Vector3.up * heightOffset;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        current = null;
        gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (!current) { Hide(); return; }   // tower was destroyed some other way
        Transform cam = Camera.main.transform;
        transform.rotation = Quaternion.LookRotation(transform.position - cam.position);
    }

    void Sell()
    {
        if (!current) return;

        currency.AddMoney(current.SellValue);   // match your money system
        TowerGrid.Instance.SetOccupied(current.gridCell, false);
        Destroy(current.Root);
        Hide();
    }
}