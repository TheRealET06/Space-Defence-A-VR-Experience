using UnityEngine;
using UnityEngine.UI;

namespace Brian
{
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] Enemy enemy;
        [SerializeField] float height = 0.35f;
        [SerializeField] Vector2 size = new Vector2(0.3f, 0.04f);
        [SerializeField] Color fullColor = new Color(0.3f, 1f, 0.4f);
        [SerializeField] Color emptyColor = new Color(1f, 0.25f, 0.2f);
        [SerializeField] Color backgroundColor = new Color(0f, 0f, 0f, 0.6f);

        Transform bar;
        RectTransform fill;
        Image fillImage;
        Transform cam;
        float shown = -1f;

        void Awake()
        {
            if (enemy == null) enemy = GetComponentInParent<Enemy>();

            var root = new GameObject("HealthBar", typeof(RectTransform), typeof(Canvas));
            root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            bar = root.transform;
            ((RectTransform)bar).sizeDelta = size;

            MakeImage("Background", bar, backgroundColor);
            fillImage = MakeImage("Fill", bar, fullColor);
            fill = fillImage.rectTransform;
        }

        void OnDestroy()
        {
            if (bar != null) Destroy(bar.gameObject);
        }

        void LateUpdate()
        {
            if (enemy == null) return;

            bar.position = enemy.transform.position + Vector3.up * height;

            if (cam == null && Camera.main != null) cam = Camera.main.transform;
            if (cam != null) bar.rotation = Quaternion.LookRotation(bar.position - cam.position);

            float f = enemy.HealthFraction;
            if (Mathf.Approximately(f, shown)) return;
            shown = f;
            fill.anchorMax = new Vector2(f, 1f);
            fillImage.color = Color.Lerp(emptyColor, fullColor, f);
        }

        static Image MakeImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }
    }
}
