using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Brian
{
    public class HandHUD : MonoBehaviour
    {
        public static HandHUD Instance { get; private set; }

        [SerializeField] TowerData[] towers;
        [SerializeField] Vector3 handOffset = new Vector3(0f, 0.28f, 0f);
        [SerializeField] Vector3 handRotation = new Vector3(0f, -30f, 0f);
        [SerializeField] float metersPerPixel = 0.0008f;

        const float Width = 380f;

        RectTransform root;
        Image hpFill;
        TextMeshProUGUI hpText, moneyText, timerText, waveText, rewardText;
        TowerData hoveredTower;
        readonly List<(Button button, TowerData data)> buttons = new List<(Button, TowerData)>();
        float refresh;
        float flashUntil;
        bool attached;

        static readonly Color FlashColor = new Color(1f, 0.62f, 0.4f);
        static readonly Color TimerColor = new Color(1f, 0.83f, 0.35f);
        static readonly Color TimerLowColor = new Color(1f, 0.42f, 0.32f);

        void Awake() => Instance = this;

        void Start()
        {
            towers ??= new TowerData[0];
            float height = 252f + Mathf.CeilToInt(towers.Length / 2f) * 66f;
            root = UIKit.Canvas("Hand HUD", transform, new Vector2(Width, height), metersPerPixel);

            UIKit.Text(root, "HOME", 15, 16, 12, 100, 20, TextAlignmentOptions.TopLeft, UIKit.DimText);
            hpText = UIKit.Text(root, "", 15, 200, 12, 164, 20, TextAlignmentOptions.TopRight);
            UIKit.Box(root, new Color(1f, 1f, 1f, 0.12f), 16, 36, Width - 32, 14);
            hpFill = UIKit.Box(root, new Color(0.45f, 1f, 0.55f), 16, 36, Width - 32, 14);

            moneyText = UIKit.Text(root, "", 30, 16, 60, 200, 40);
            timerText = UIKit.Text(root, "", 30, 180, 60, 184, 40, TextAlignmentOptions.TopRight, TimerColor);
            waveText = UIKit.Text(root, "", 15, 16, 106, Width - 32, 46, TextAlignmentOptions.TopLeft, UIKit.DimText);
            rewardText = UIKit.Text(root, "", 13, 16, 154, Width - 32, 54, TextAlignmentOptions.TopLeft, UIKit.DimText);

            UIKit.Text(root, "BUILD", 15, 16, 210, 100, 20, TextAlignmentOptions.TopLeft, UIKit.DimText);
            for (int i = 0; i < towers.Length; i++)
            {
                var data = towers[i];
                if (data == null) continue;
                float x = 16 + (i % 2) * ((Width - 40) / 2 + 8);
                float y = 236 + (i / 2) * 66;
                var b = UIKit.Button(root, "", 15, x, y, (Width - 40) / 2, 58, () => Pick(data));
                UIKit.OnHover(b, () => hoveredTower = data, () => { if (hoveredTower == data) hoveredTower = null; });
                buttons.Add((b, data));
            }

            if (PlayerControls.Instance != null) PlayerControls.Instance.Menu.performed += OnMenu;
            Refresh();
        }

        void OnDestroy()
        {
            if (PlayerControls.Instance != null) PlayerControls.Instance.Menu.performed -= OnMenu;
        }

        void OnMenu(InputAction.CallbackContext _)
        {
            if (root != null) root.gameObject.SetActive(!root.gameObject.activeSelf);
        }

        void Attach()
        {
            if (attached) return;
            Transform hand = PlayerControls.Instance != null ? PlayerControls.Instance.LeftHand : null;
            if (hand == null) return;
            root.SetParent(hand, false);
            root.localPosition = handOffset;
            root.localRotation = Quaternion.Euler(handRotation);
            attached = true;
        }

        void Pick(TowerData data)
        {
            if (!data.IsUnlocked)
            {
                Flash($"{data.displayName} is locked. It unlocks at wave {data.unlockWave}.");
                return;
            }
            var eco = Economy.Instance;
            if (eco != null && !eco.CanAfford(data.cost))
            {
                Flash($"Can't afford {data.displayName}: costs ${data.cost}, you have ${eco.Money}.");
                return;
            }
            if (BuildController.Instance != null) BuildController.Instance.Select(data);
        }

        public void Flash(string text)
        {
            if (rewardText == null) return;
            rewardText.text = text;
            rewardText.color = FlashColor;
            flashUntil = Time.unscaledTime + 3.5f;
        }

        static string Clock(float seconds)
        {
            int s = Mathf.CeilToInt(seconds);
            return $"{s / 60}:{s % 60:00}";
        }

        void LateUpdate()
        {
            if (root == null) return;
            Attach();
            refresh -= Time.unscaledDeltaTime;
            if (refresh <= 0f)
            {
                refresh = 0.15f;
                Refresh();
            }
        }

        void Refresh()
        {
            var home = Home.Instance;
            float hp = home != null ? home.Fraction : 1f;
            hpText.text = home != null ? $"{home.Health} / {home.MaxHealth}" : "";
            var rt = hpFill.rectTransform;
            rt.sizeDelta = new Vector2((Width - 32) * hp, rt.sizeDelta.y);
            hpFill.color = Color.Lerp(new Color(1f, 0.3f, 0.25f), new Color(0.45f, 1f, 0.55f), hp);

            var eco = Economy.Instance;
            moneyText.text = eco != null ? $"${eco.Money}" : "";
            var mine = MiningTower.Instance;
            string mining = mine == null ? "" : mine.Running ? "\n<color=#8dffa0>Mining Tower: mining</color>" : "\nMining Tower: paused";

            var wave = WaveDirector.Instance;
            var gm = GameManager.Instance;
            timerText.text = "";
            if (gm != null && gm.State == GameState.Ready)
            {
                waveText.text = "<color=#9fd7ff>Press START on the controls board</color>" + mining;
            }
            else if (wave != null)
            {
                string head = $"Wave {wave.CurrentWave}/{wave.TotalWaves}";
                if (wave.CanStartWave)
                {
                    waveText.text = $"{head} ready: press START WAVE" + mining;
                }
                else if (wave.TimeLeft > 0f)
                {
                    waveText.text = $"{head}  ·  {wave.WaveEnemiesLeft} enemies left" + mining;
                    timerText.text = $"<size=14>ENDS IN</size> {Clock(wave.TimeLeft)}";
                    timerText.color = wave.TimeLeft <= 10f ? TimerLowColor : TimerColor;
                }
                else
                {
                    waveText.text = $"{head}  ·  clear the last {wave.WaveEnemiesLeft} enemies" + mining;
                    timerText.text = "<size=14>TIME UP</size> 0:00";
                    timerText.color = TimerLowColor;
                }
            }
            if (Time.unscaledTime >= flashUntil)
            {
                if (hoveredTower != null)
                    rewardText.text = $"<color=#ffffff><b>{hoveredTower.displayName}</b>  ${hoveredTower.cost}</color>\n{hoveredTower.description}";
                else
                    rewardText.text = "The Mining Tower mines and sells by itself during waves.\nWalk over to it to upgrade.";
                rewardText.color = UIKit.DimText;
            }

            var building = BuildController.Instance != null ? BuildController.Instance.Selected : null;
            foreach (var (button, data) in buttons)
            {
                if (!data.IsUnlocked)
                {
                    UIKit.SetDimmed(button, true);
                    UIKit.SetLabel(button, $"LOCKED\n<size=13><color=#b8c0cc>Unlocks wave {data.unlockWave}</color></size>");
                    continue;
                }

                bool afford = eco != null && eco.CanAfford(data.cost);
                UIKit.SetDimmed(button, !afford);
                string mark = building == data ? "<color=#9fd7ff>> </color>" : "";
                UIKit.SetLabel(button, $"{mark}{data.displayName}\n<size=13><color=#b8c0cc>${data.cost}</color></size>");
            }
        }
    }
}
