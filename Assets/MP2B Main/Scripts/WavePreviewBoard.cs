using System.Text;
using TMPro;
using UnityEngine;

namespace Brian
{
    /// World-space board (same style as ControlsBoard) listing the enemies in the next wave.
    /// Put it on a GameObject next to the controls board.
    public class WavePreviewBoard : MonoBehaviour
    {
        [SerializeField] float metersPerPixel = 0.0015f;
        [Tooltip("Most enemy types shown at once. Waves with more types show a '+N more' line.")]
        [SerializeField] int maxRows = 6;

        const float W = 620f;
        const float RowPx = 42f;

        TextMeshProUGUI title, subtitle, list;
        int shownWave = -1;

        void Start()
        {
            float listHeight = (maxRows + 2) * RowPx;
            float h = 120f + listHeight + 30f;
            var root = UIKit.Canvas("Wave Preview", transform, new Vector2(W, h), metersPerPixel);

            title = UIKit.Text(root, "NEXT WAVE", 34, 30, 22, W - 60, 46);
            subtitle = UIKit.Text(root, "", 22, 30, 72, W - 60, 34, TextAlignmentOptions.TopLeft, UIKit.DimText);
            list = UIKit.Text(root, "", 24, 30, 120, W - 60, listHeight, TextAlignmentOptions.TopLeft);
            list.lineSpacing = 20f;
        }

        void Update()
        {
            var wd = WaveDirector.Instance;
            if (list == null || wd == null) return;

            // The preview only changes when the "next wave" changes, so don't rebuild text every frame.
            int wave = wd.UpcomingWaveNumber;
            if (wave == shownWave) return;
            shownWave = wave;
            Rebuild(wd, wave);
        }

        void Rebuild(WaveDirector wd, int wave)
        {
            if (wave == 0)
            {
                title.text = "NEXT WAVE";
                subtitle.text = "No more waves after this one";
                list.text = "<color=#9fd7ff>Final wave: clear the field to win</color>";
                return;
            }

            var lines = wd.GetWavePreview(wave);
            int total = 0;
            foreach (var l in lines) total += l.count;

            title.text = $"NEXT WAVE  <size=24><color=#b8c0cc>{wave}/{wd.TotalWaves}</color></size>";
            subtitle.text = $"{wd.GetWaveName(wave)}  ·  {total} enemies";

            if (lines.Count == 0)
            {
                subtitle.text = $"{wd.GetWaveName(wave)}  ·  no enemies";
                list.text = $"<color=#9fd7ff>{wd.GetWaveDescription(wave)}</color>";
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("<color=#8a93a6>ENEMY<pos=56%>COUNT<pos=78%>HP</color>");

            int shown = Mathf.Min(lines.Count, maxRows);
            for (int i = 0; i < shown; i++)
            {
                var l = lines[i];
                string tag = l.isNew ? " <size=16><color=#ffd36e>NEW</color></size>" : "";
                sb.AppendLine(
                    $"<color=#9fd7ff>{l.enemy.displayName}</color>{tag}" +
                    $"<pos=56%>x{l.count}" +
                    $"<pos=78%>{Mathf.RoundToInt(l.health)}");
            }

            if (lines.Count > shown)
                sb.AppendLine($"<color=#8a93a6>+{lines.Count - shown} more types</color>");

            list.text = sb.ToString();
        }
    }
}