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
        [SerializeField] float comfortDistance = 0.8f;
        [SerializeField] float messageDuration = 3.5f;

        const float Width = 360f;
        const float Pad = 16f;
        const float StatsTop = 50f;
        const float ButtonHeight = 52f;
        const float Gap = 8f;
        const int PerRow = 3;

        static readonly Color ErrorColor = new Color(1f, 0.62f, 0.4f);
        static readonly Color InfoColor = new Color(0.55f, 1f, 0.65f);

        RectTransform root;
        BoxCollider box;
        TextMeshProUGUI title, stats, description, message;
        RangeIndicator range;
        Tower current;
        float refresh;
        float height = 300f;
        float messageUntil;
        int hovered = -1;
        Vector3 anchor;
        bool centered;
        float scale;
        readonly List<Button> buttons = new List<Button>();
        readonly List<(string label, string value)> rows = new List<(string, string)>();
        readonly List<TowerAction> actions = new List<TowerAction>();

        public Tower Current => current;

        void Awake() => Instance = this;

        void Start()
        {
            root = UIKit.Canvas("Tower Panel", transform, new Vector2(Width, height), metersPerPixel);
            box = root.GetComponent<BoxCollider>();
            title = UIKit.Text(root, "", 22, Pad, 12, Width - Pad * 2 - 50, 30);
            UIKit.Button(root, "X", 18, Width - Pad - 42, 10, 42, 34, Hide);
            stats = UIKit.Text(root, "", 15, Pad, StatsTop, Width - Pad * 2, 130, TextAlignmentOptions.TopLeft, UIKit.DimText);
            description = UIKit.Text(root, "", 13, Pad, 0, Width - Pad * 2, 40, TextAlignmentOptions.TopLeft, UIKit.DimText);
            message = UIKit.Text(root, "", 15, Pad, 0, Width - Pad * 2, 40, TextAlignmentOptions.TopLeft, ErrorColor);

            if (rangePrefab != null)
            {
                range = Instantiate(rangePrefab);
                range.gameObject.SetActive(false);
            }
            root.gameObject.SetActive(false);
        }

        public void Show(Tower tower)
        {
            if (current != tower) messageUntil = 0f;
            hovered = -1;
            current = tower;
            root.gameObject.SetActive(true);
            if (range != null) range.gameObject.SetActive(tower.ShowsRange);
            Anchor();
            Refresh();
            Place();
        }

        public void Hide()
        {
            current = null;
            if (root != null) root.gameObject.SetActive(false);
            if (range != null) range.gameObject.SetActive(false);
        }

        public void Notify(string text, bool error = true)
        {
            if (message == null) return;
            message.text = text;
            message.color = error ? ErrorColor : InfoColor;
            messageUntil = Time.unscaledTime + messageDuration;
            Refresh();
        }

        void LateUpdate()
        {
            if (root == null || !root.gameObject.activeSelf) return;
            if (current == null)
            {
                Hide();
                return;
            }
            Place();
            refresh -= Time.unscaledDeltaTime;
            if (refresh <= 0f)
            {
                refresh = 0.2f;
                Refresh();
            }
        }

        void Anchor()
        {
            var cam = Camera.main;
            Vector3 eye = cam != null ? cam.transform.position : current.transform.position;
            anchor = current.PanelAnchor(eye, out centered);
            scale = metersPerPixel * Mathf.Max(1f, Vector3.Distance(eye, anchor) / comfortDistance);
        }

        void Place()
        {
            if (current == null) return;
            root.localScale = Vector3.one * scale;
            root.position = centered ? anchor : anchor + Vector3.up * (height * scale * 0.5f);
            UIKit.FaceCamera(root);
            if (range != null) range.transform.position = current.RangeCenter;
        }

        void Press(int index)
        {
            if (current == null || index >= actions.Count) return;
            var run = actions[index].run;
            if (run != null) run();
            Refresh();
        }

        string StatsText(List<(string label, string change)> preview)
        {
            var sb = new StringBuilder();
            foreach (var (label, value) in rows)
            {
                string change = Change(preview, label);
                sb.Append($"{label}<pos=40%><color=#ffffff>{value}</color>");
                if (change != null) sb.Append($"  <color=#8dff9a>{change}</color>");
                sb.Append('\n');
            }
            if (preview != null)
                foreach (var (label, change) in preview)
                    if (!rows.Exists(r => r.label == label)) sb.Append($"{label}<pos=40%><color=#8dff9a>{change}</color>\n");
            return sb.ToString().TrimEnd('\n');
        }

        static string Change(List<(string label, string change)> preview, string label)
        {
            if (preview == null) return null;
            foreach (var p in preview)
                if (p.label == label) return p.change;
            return null;
        }

        void Refresh()
        {
            if (current == null || current.Data == null) return;
            title.text = $"{current.Data.displayName}  <size=15><color=#9fd7ff>Lv {current.Level}</color></size>";

            actions.Clear();
            current.FillActions(actions);
            var preview = hovered >= 0 && hovered < actions.Count ? actions[hovered].preview : null;

            rows.Clear();
            current.FillStats(rows);
            string statsText = StatsText(preview);
            stats.text = statsText;
            float statsHeight = stats.GetPreferredValues(StatsText(null), Width - Pad * 2, 0f).y;
            foreach (var act in actions)
                if (act.preview != null) statsHeight = Mathf.Max(statsHeight, stats.GetPreferredValues(StatsText(act.preview), Width - Pad * 2, 0f).y);
            statsHeight += 6f;
            UIKit.Place(stats.rectTransform, Pad, StatsTop, Width - Pad * 2, statsHeight);

            float y = StatsTop + statsHeight;
            string info = current.Description;
            description.gameObject.SetActive(!string.IsNullOrEmpty(info));
            if (!string.IsNullOrEmpty(info))
            {
                description.text = info;
                float h = description.GetPreferredValues(info, Width - Pad * 2, 0f).y + 4f;
                UIKit.Place(description.rectTransform, Pad, y, Width - Pad * 2, h);
                y += h + 6f;
            }
            bool showMessage = Time.unscaledTime < messageUntil;
            message.gameObject.SetActive(showMessage);
            if (showMessage)
            {
                float mh = message.GetPreferredValues(message.text, Width - Pad * 2, 0f).y + 4f;
                UIKit.Place(message.rectTransform, Pad, y, Width - Pad * 2, mh);
                y += mh + 4f;
            }
            int lines = Mathf.CeilToInt(actions.Count / (float)PerRow);
            for (int i = 0; i < Mathf.Max(actions.Count, buttons.Count); i++)
            {
                if (i >= buttons.Count)
                {
                    int index = i;
                    var nb = UIKit.Button(root, "", 14, 0, 0, 10, 10, () => Press(index));
                    UIKit.OnHover(nb, () => { hovered = index; refresh = 0f; }, () => { if (hovered == index) hovered = -1; refresh = 0f; });
                    buttons.Add(nb);
                }
                var b = buttons[i];
                if (i >= actions.Count)
                {
                    b.gameObject.SetActive(false);
                    continue;
                }
                int line = i / PerRow;
                int inLine = Mathf.Min(PerRow, actions.Count - line * PerRow);
                float w = (Width - Pad * 2 - Gap * (inLine - 1)) / inLine;
                float x = Pad + (i % PerRow) * (w + Gap);
                UIKit.Place((RectTransform)b.transform, x, y + line * (ButtonHeight + Gap), w, ButtonHeight);
                var label = b.GetComponentInChildren<TextMeshProUGUI>();
                UIKit.Place(label.rectTransform, 0f, 0f, w, ButtonHeight);
                label.text = actions[i].label;
                UIKit.SetDimmed(b, !actions[i].enabled);
                b.interactable = actions[i].run != null;
                b.gameObject.SetActive(true);
            }

            height = y + lines * (ButtonHeight + Gap) - Gap + Pad;
            root.sizeDelta = new Vector2(Width, height);
            if (box != null) box.size = new Vector3(Width, height, 2f);
            if (range != null) range.SetRange(current.Range);
        }
    }
}
