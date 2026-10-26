using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Brian
{
    public class ControlsBoard : MonoBehaviour
    {
        [SerializeField] float metersPerPixel = 0.0015f;

        Button startButton;
        string shownLabel;
        bool shownInteractable;

        void Start()
        {
            const float w = 700f;
            float listHeight = PlayerControls.All.Length * 70f;
            float h = 230f + listHeight;
            var root = UIKit.Canvas("Controls", transform, new Vector2(w, h), metersPerPixel);
            UIKit.Text(root, "CONTROLS", 34, 30, 22, w - 60, 46);

            var sb = new StringBuilder();
            foreach (var b in PlayerControls.All)
                sb.AppendLine($"<color=#9fd7ff>{b.xrName}</color><pos=30%>{b.label}");
            var t = UIKit.Text(root, sb.ToString(), 26, 30, 84, w - 60, listHeight + 10, TextAlignmentOptions.TopLeft, UIKit.DimText);
            t.lineSpacing = 30f;

            startButton = UIKit.Button(root, "START", 30, 200, 110 + listHeight, 300, 90, OnButtonPressed);
        }

        void OnButtonPressed()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            // Won or lost: the button becomes RESTART.
            if (gm.IsOver)
            {
                gm.Restart();
                return;
            }

            // First press starts the game, then falls through to launch wave 1.
            if (gm.State == GameState.Ready) gm.StartGame();

            // StartNextWave ignores the call unless no wave is active, so a double poke can't stack waves.
            var wd = WaveDirector.Instance;
            if (wd != null) wd.StartNextWave();
        }

        void Update()
        {
            var gm = GameManager.Instance;
            var wd = WaveDirector.Instance;
            if (startButton == null || gm == null) return;

            string label;
            bool interactable;

            if (gm.IsOver)
            {
                label = "RESTART";
                interactable = true;
            }
            else if (gm.State == GameState.Ready)
            {
                label = "START";
                interactable = true;
            }
            else if (wd == null)
            {
                label = "NO WAVES";
                interactable = false;
            }
            else if (wd.CanStartWave)
            {
                label = $"START WAVE {wd.CurrentWave}";
                interactable = true;
            }
            else
            {
                label = $"WAVE {wd.CurrentWave}/{wd.TotalWaves}";
                interactable = false;
            }

            // Only touch the UI when something changed.
            if (label != shownLabel)
            {
                shownLabel = label;
                UIKit.SetLabel(startButton, label);
            }
            if (interactable != shownInteractable || startButton.interactable != interactable)
            {
                shownInteractable = interactable;
                startButton.interactable = interactable;
            }
        }
    }
}