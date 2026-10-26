using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Brian
{
    /// Shows a "tower unlocked" card (icon + headline + flavor text) in front of the player
    /// whenever a wave is cleared and a tower's unlockWave matches the next wave.
    public class UnlockPopup : MonoBehaviour
    {
        [SerializeField] TowerData[] towers;          // same list you give the HandHUD
        [SerializeField] float metersPerPixel = 0.0012f;
        [SerializeField] float distance = 1.2f;
        [SerializeField] float heightBelowEyes = 0.05f;

        const float W = 620f;
        const float H = 330f;

        RectTransform root;
        Image icon;
        TextMeshProUGUI headline, body;
        readonly Queue<TowerData> queue = new Queue<TowerData>();
        WaveDirector director;

        void Start()
        {
            towers ??= new TowerData[0];

            root = UIKit.Canvas("Unlock Popup", transform, new Vector2(W, H), metersPerPixel);
            UIKit.Text(root, "NEW TOWER", 16, 30, 18, W - 60, 24, TextAlignmentOptions.TopLeft, UIKit.DimText);

            icon = UIKit.Box(root, Color.white, 30, 62, 170, 170);
            icon.preserveAspect = true;

            headline = UIKit.Text(root, "", 32, 225, 62, W - 255, 50, TextAlignmentOptions.TopLeft, Color.white);
            body = UIKit.Text(root, "", 22, 225, 118, W - 255, 120, TextAlignmentOptions.TopLeft, UIKit.DimText);

            UIKit.Button(root, "CONTINUE", 26, (W - 240f) / 2f, 250, 240, 60, Dismiss);
            root.gameObject.SetActive(false);

            director = WaveDirector.Instance;
            if (director != null) director.ReadyForWave += OnReadyForWave;
        }

        void OnDestroy()
        {
            if (director != null) director.ReadyForWave -= OnReadyForWave;
        }

        void OnReadyForWave(int waveNumber)
        {
            foreach (var t in towers)
                if (t != null && t.unlockWave > 1 && t.unlockWave == waveNumber)
                    queue.Enqueue(t);

            if (!root.gameObject.activeSelf) ShowNext();
        }

        void ShowNext()
        {
            if (queue.Count == 0)
            {
                root.gameObject.SetActive(false);
                return;
            }

            var d = queue.Dequeue();
            headline.text = string.IsNullOrEmpty(d.unlockHeadline) ? $"{d.displayName} unlocked" : d.unlockHeadline;
            body.text = d.unlockText;
            icon.sprite = d.icon;
            icon.color = d.icon != null ? Color.white : new Color(1f, 1f, 1f, 0.1f);

            Place();
            root.gameObject.SetActive(true);
        }

        // Puts the card in front of wherever the player is looking right now, level with the horizon.
        void Place()
        {
            var cam = Camera.main;
            if (cam == null) return;

            Vector3 fwd = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            fwd.Normalize();

            root.position = cam.transform.position + fwd * distance + Vector3.down * heightBelowEyes;
            root.rotation = Quaternion.LookRotation(fwd, Vector3.up);
        }

        void Dismiss() => ShowNext();   // shows the next queued unlock, or hides the card if none are left
    }
}