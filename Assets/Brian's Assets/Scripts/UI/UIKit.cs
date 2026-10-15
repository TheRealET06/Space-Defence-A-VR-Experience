using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Brian
{
    public static class UIKit
    {
        public static readonly Color PanelColor = new Color(0.04f, 0.05f, 0.08f, 0.88f);
        public static readonly Color ButtonColor = new Color(0.16f, 0.2f, 0.3f, 1f);
        public static readonly Color AccentColor = new Color(0.62f, 0.84f, 1f, 1f);
        public static readonly Color DimText = new Color(0.7f, 0.75f, 0.82f, 1f);

        static Type xrRaycaster;

        public static RectTransform Canvas(string name, Transform parent, Vector2 sizePx, float metersPerPixel)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;

            xrRaycaster ??= Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
            if (xrRaycaster != null) go.AddComponent(xrRaycaster);

            var rt = (RectTransform)go.transform;
            rt.sizeDelta = sizePx;
            rt.localScale = Vector3.one * metersPerPixel;

            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(sizePx.x, sizePx.y, 2f);
            box.isTrigger = true;

            var bg = go.AddComponent<Image>();
            bg.color = PanelColor;
            return rt;
        }

        public static RectTransform Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        public static TextMeshProUGUI Text(RectTransform parent, string text, float size, float x, float y, float w, float h,
            TextAlignmentOptions align = TextAlignmentOptions.TopLeft, Color? color = null)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = color ?? Color.white;
            t.raycastTarget = false;
            t.richText = true;
            t.textWrappingMode = TextWrappingModes.Normal;
            Place((RectTransform)go.transform, x, y, w, h);
            return t;
        }

        public static Image Box(RectTransform parent, Color color, float x, float y, float w, float h)
        {
            var go = new GameObject("Box", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            Place((RectTransform)go.transform, x, y, w, h);
            return img;
        }

        public static Button Button(RectTransform parent, string label, float size, float x, float y, float w, float h, Action onClick)
        {
            var go = new GameObject("Button", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = ButtonColor;
            var button = go.AddComponent<Button>();
            button.targetGraphic = img;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(() => onClick());
            var rt = Place((RectTransform)go.transform, x, y, w, h);
            Text(rt, label, size, 0f, 0f, w, h, TextAlignmentOptions.Center);
            return button;
        }

        public static void SetLabel(Button button, string label)
        {
            var t = button.GetComponentInChildren<TextMeshProUGUI>();
            if (t != null) t.text = label;
        }

        public static void FaceCamera(Transform t)
        {
            var cam = Camera.main;
            if (cam != null) t.rotation = Quaternion.LookRotation(t.position - cam.transform.position);
        }
    }
}
