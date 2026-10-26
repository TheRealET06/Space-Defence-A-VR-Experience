using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Brian
{
    public class HandHUD : MonoBehaviour
    {
        [SerializeField] TowerData[] towers;
        [SerializeField] Vector3 handOffset = new Vector3(0f, 0.28f, 0f);
        [SerializeField] Vector3 handRotation = new Vector3(0f, -30f, 0f);
        [SerializeField] float metersPerPixel = 0.0008f;

        const float Width = 380f;

        RectTransform root;
        Image hpFill;
        TextMeshProUGUI hpText, moneyText, incomeText, waveText, rewardText;
        readonly List<(Button button, TowerData data)> buttons = new List<(Button, TowerData)>();
        float refresh;
        bool attached;

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
            incomeText = UIKit.Text(root, "", 15, 200, 72, 164, 24, TextAlignmentOptions.TopRight, UIKit.DimText);
            waveText = UIKit.Text(root, "", 15, 16, 110, Width - 32, 44, TextAlignmentOptions.TopLeft, UIKit.DimText);
            rewardText = UIKit.Text(root, "", 13, 16, 158, Width - 32, 40, TextAlignmentOptions.TopLeft, UIKit.DimText);

            UIKit.Text(root, "BUILD", 15, 16, 210, 100, 20, TextAlignmentOptions.TopLeft, UIKit.DimText);
            for (int i = 0; i < towers.Length; i++)
            {
                var data = towers[i];
                if (data == null) continue;
                float x = 16 + (i % 2) * ((Width - 40) / 2 + 8);
                float y = 236 + (i / 2) * 66;
                var b = UIKit.Button(root, "", 15, x, y, (Width - 40) / 2, 58, () => Pick(data));
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
            if (!data.IsUnlocked) return;
            if (BuildController.Instance != null) BuildController.Instance.Select(data);
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
            incomeText.text = eco != null ? $"+${eco.IncomePerSecond:0.#} / s" : "";

            var wave = WaveDirector.Instance;
            var gm = GameManager.Instance;
            if (gm != null && gm.State == GameState.Ready)
            {
                waveText.text = "<color=#9fd7ff>Place towers, then press START on the controls board</color>";
            }
            else if (wave != null)
            {
                waveText.text = wave.CanStartWave
                    ? $"Wave {wave.CurrentWave}/{wave.TotalWaves} ready  ·  press START WAVE on the controls board"
                    : $"Wave {wave.CurrentWave}/{wave.TotalWaves}   ·   {wave.WaveEnemiesLeft} enemies left";
            }
            if (wave != null)
            {
                string r = "";
                if (wave.BasicEnemy != null) r += $"{wave.BasicEnemy.displayName} ${wave.BasicEnemy.killReward}";
                if (wave.HardEnemy != null) r += $"   ·   {wave.HardEnemy.displayName} ${wave.HardEnemy.killReward}";
                rewardText.text = "Kill reward:  " + r + "\nMeteor: select to collect";
            }

            var building = BuildController.Instance != null ? BuildController.Instance.Selected : null;
            foreach (var (button, data) in buttons)
            {
                if (!data.IsUnlocked)
                {
                    button.interactable = false;
                    UIKit.SetLabel(button, $"LOCKED\n<size=13><color=#b8c0cc>Unlocks wave {data.unlockWave}</color></size>");
                    continue;
                }

                bool afford = eco != null && eco.CanAfford(data.cost);
                button.interactable = afford;
                string mark = building == data ? "<color=#9fd7ff>> </color>" : "";
                UIKit.SetLabel(button, $"{mark}{data.displayName}\n<size=13><color=#b8c0cc>${data.cost}</color></size>");
            }
        }
    }
}
