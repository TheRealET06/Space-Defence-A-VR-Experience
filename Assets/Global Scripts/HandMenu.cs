using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class HandMenu : MonoBehaviour
{
    [SerializeField] InputActionReference toggleAction;   // e.g. left X/Y or left primary button
    [SerializeField] GameObject menuRoot;                 // the world-space Canvas object
    [SerializeField] Transform buttonContainer;           // has a Vertical/Grid LayoutGroup
    [SerializeField] Button buttonPrefab;                 // has a TMP_Text child (+ optional Image)
    [SerializeField] TMP_Text moneyLabel;
    [SerializeField] TowerData[] towers;
    [SerializeField] TowerPlacer placer;
    [SerializeField] CurrencyManager currency;

    Button[] buttons;
    public bool IsOpen => menuRoot.activeSelf;
    public int LastClosedFrame { get; private set; } = -10;

    void OnEnable()
    {
        toggleAction.action.performed += OnToggle;
        currency.OnMoneyChanged += _ => Refresh();
    }
    void OnDisable() => toggleAction.action.performed -= OnToggle;

    void Start()
    {
        buttons = new Button[towers.Length];
        for (int i = 0; i < towers.Length; i++)
        {
            var data = towers[i];
            var b = Instantiate(buttonPrefab, buttonContainer);
            b.GetComponentInChildren<TMP_Text>().text = $"{data.displayName}\n${data.cost}";
            b.onClick.AddListener(() => Select(data));
            buttons[i] = b;
        }
        menuRoot.SetActive(false);
        Refresh();
    }

    void OnToggle(InputAction.CallbackContext _) => SetOpen(!IsOpen);

    void SetOpen(bool open)
    {
        if (!open) LastClosedFrame = Time.frameCount;
        menuRoot.SetActive(open);
        if (open) Refresh();
    }

    void Select(TowerData data)
    {
        if (!currency.CanAfford(data.cost)) return;   // button is disabled anyway, belt and braces
        placer.SelectTower(data);
        SetOpen(false);
    }

    void Refresh()
    {
        moneyLabel.text = $"${currency.Money}";
        for (int i = 0; i < towers.Length; i++)
            buttons[i].interactable = currency.CanAfford(towers[i].cost);
    }
}