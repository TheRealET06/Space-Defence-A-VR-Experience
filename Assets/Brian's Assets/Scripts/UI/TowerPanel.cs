using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Brian
{
    public class TowerPanel : MonoBehaviour
    {
        public static TowerPanel Instance { get; private set; }

        [SerializeField] RangeIndicator rangePrefab;
        [SerializeField] float metersPerPixel = 0.0006f;
        [SerializeField] float heightAbove = 0.35f;

        const float Width = 340f;
        const float Height = 250f;

        RectTransform root;
        TextMeshProUGUI title, stats;
        Button upgrade, sell;
        RangeIndicator range;
        Tower current;
        float refresh;
        readonly List<(string label, string value)> rows = new List<(string, string)>();

        public Tower Current => current;

        void Awake() => Instance = this;

        void Start()
        {
            root = UIKit.Canvas("Tower Panel", transform, new Vector2(Width, Height), metersPerPixel);
            title = UIKit.Text(root, "", 22, 16, 12, Width - 32, 30);
            stats = UIKit.Text(root, "", 15, 16, 48, Width - 32, 130, TextAlignmentOptions.TopLeft, UIKit.DimText);
            upgrade = UIKit.Button(root, "", 15, 16, 186, 140, 48, () => { if (current != null) current.TryUpgrade(); Refresh(); });
            sell = UIKit.Button(root, "", 15, 164, 186, 110, 48, () => { if (current != null) current.Sell(); Hide(); });
            UIKit.Button(root, "X", 18, 282, 186, 42, 48, Hide);

            if (rangePrefab != null)
            {
                range = Instantiate(rangePrefab);
                range.gameObject.SetActive(false);
            }
            root.gameObject.SetActive(false);
        }

        public void Show(Tower tower)
        {
            current = tower;
            root.gameObject.SetActive(true);
            if (range != null)
            {
                range.gameObject.SetActive(true);
                range.Follow(tower.transform);
            }
            Refresh();
        }

        public void Hide()
        {
            current = null;
            if (root != null) root.gameObject.SetActive(false);
            if (range != null) range.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            if (root == null || !root.gameObject.activeSelf) return;
            if (current == null)
            {
                Hide();
                return;
            }
            root.position = current.transform.position + Vector3.up * heightAbove;
            UIKit.FaceCamera(root);
            refresh -= Time.unscaledDeltaTime;
            if (refresh <= 0f)
            {
                refresh = 0.2f;
                Refresh();
            }
        }

        void Refresh()
        {
            if (current == null || current.Data == null) return;
            title.text = $"{current.Data.displayName}  <size=15><color=#9fd7ff>Lv {current.Level}</color></size>";

            rows.Clear();
            current.FillStats(rows);
            var sb = new StringBuilder();
            foreach (var (label, value) in rows) sb.AppendLine($"{label}<pos=46%><color=#ffffff>{value}</color>");
            stats.text = sb.ToString();

            if (range != null) range.SetRange(current.Range);

            var eco = Economy.Instance;
            if (current.CanUpgrade)
            {
                UIKit.SetLabel(upgrade, $"Upgrade  ${current.UpgradeCost}");
                upgrade.interactable = eco != null && eco.CanAfford(current.UpgradeCost);
            }
            else
            {
                UIKit.SetLabel(upgrade, "Max level");
                upgrade.interactable = false;
            }
            UIKit.SetLabel(sell, $"Sell  +${current.SellValue}");
        }
    }
}
