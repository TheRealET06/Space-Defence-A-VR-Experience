using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
/*
namespace Brian
{
    public class ControlsBoard : MonoBehaviour
    {
        [SerializeField] float metersPerPixel = 0.0015f;

        Button startButton;
        GameState shown = (GameState)(-1);

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

            startButton = UIKit.Button(root, "START", 34, 200, 110 + listHeight, 300, 90, OnStartPressed);
        }

        void OnStartPressed()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            if (gm.State == GameState.Ready) gm.StartGame();
            else gm.Restart();
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (startButton == null || gm == null || gm.State == shown) return;
            shown = gm.State;
            UIKit.SetLabel(startButton, shown == GameState.Ready ? "START" : "RESTART");
        }
    }
}
*/
