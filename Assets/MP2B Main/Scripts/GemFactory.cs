using System.Collections.Generic;
using UnityEngine;

namespace Brian
{
    /// <summary>
    /// Placed on the grid like a normal tower. Turns gas into gems: start a batch from the tower
    /// menu (costs gas), wait while the gem grows, then collect it from the menu.
    /// Stat mapping (from TowerData, with level scaling and buffs):
    ///   Attack       = gems produced per batch (rounded; set Attack to 1 for one gem)
    ///   AttackSpeed  = how fast a batch is processed
    /// Range is not used.
    /// </summary>
    public class GemFactory : Tower
    {
        enum State { Idle, Working, Done }

        [Header("Processing")]
        [SerializeField] GameObject gem;                // the growing gem visual (optional)
        [SerializeField] int gasCost = 10;              // gas used to start a batch
        [SerializeField] float processTime = 4f;        // seconds per batch at AttackSpeed 1
        [SerializeField] float maxGemScale = 0.4f;      // gem scale when the batch is finished

        State state = State.Idle;
        float progress;                                  // 0..1

        public bool working => state == State.Working;  // (kept from the old script)
        public bool done => state == State.Done;

        int GemsPerBatch => Mathf.Max(1, Mathf.RoundToInt(Attack));
        float Speed => Mathf.Max(0.05f, AttackSpeed);

        protected override void Awake()
        {
            base.Awake();
            UpdateVisual();
        }

        void Update()
        {
            if (Data == null) return;

            if (state == State.Working)
            {
                progress += Time.deltaTime * Speed / Mathf.Max(0.01f, processTime);
                if (progress >= 1f)
                {
                    progress = 1f;
                    state = State.Done;
                }
            }
            UpdateVisual();
        }

        void UpdateVisual()
        {
            if (gem == null) return;
            float s = state == State.Idle ? 0f : progress * maxGemScale;
            gem.transform.localScale = new Vector3(s, s, s);
        }

        bool StartProcessing()
        {
            var eco = Economy.Instance;
            if (eco == null || !eco.TrySpendGas(gasCost))
            {
                PanelMessage($"Needs {gasCost} gas, you have {(eco != null ? eco.Gas : 0)}.");
                return false;
            }
            state = State.Working;
            progress = 0f;
            return true;
        }

        bool CollectGems()
        {
            if (state != State.Done || Economy.Instance == null) return false;
            int n = GemsPerBatch;
            Economy.Instance.EarnGems(n);
            state = State.Idle;
            progress = 0f;
            UpdateVisual();
            PanelMessage($"Collected {n} gem{(n == 1 ? "" : "s")}.", error: false);
            return true;
        }

        /// <summary>Same as the tower menu's main option. Kept so existing event hookups still work.</summary>
        public void interact()
        {
            if (state == State.Done) CollectGems();
            else if (state == State.Idle) StartProcessing();
        }

        // ---- Tower panel ----

        public override string Description =>
            Join(Summary, $"Turns {gasCost} gas into gems. Start a batch from the menu, then collect it.\nUpgrade: more gems per batch and faster processing.");

        public override void FillStats(List<(string label, string value)> rows)
        {
            string status = state == State.Idle ? "Idle"
                          : state == State.Working ? $"Processing {progress * 100f:0}%"
                          : "Gem ready";
            rows.Add(("Status", status));
            rows.Add(("Output", $"{GemsPerBatch} gem{(GemsPerBatch == 1 ? "" : "s")} for {gasCost} gas"));
            rows.Add(("Process time", $"{processTime / Speed:0.#} s"));
        }

        public override void FillActions(List<TowerAction> actions)
        {
            var eco = Economy.Instance;
            switch (state)
            {
                case State.Done:
                    actions.Add(new TowerAction($"Collect  {GemsPerBatch} gem{(GemsPerBatch == 1 ? "" : "s")}", true, () => CollectGems()));
                    break;
                case State.Working:
                    actions.Add(new TowerAction($"Processing  {progress * 100f:0}%", false, null));
                    break;
                default:
                    actions.Add(new TowerAction($"Process gas  -{gasCost} gas", eco != null && eco.CanAffordGas(gasCost), () =>
                    {
                        if (StartProcessing()) PanelMessage("Processing started.", error: false);
                    }));
                    break;
            }
            base.FillActions(actions);
        }

        protected override List<(string label, string change)> UpgradePreview()
        {
            return new List<(string, string)>
            {
                ("Output", $"+{Data.attackPerLevel * Buff.AttackMultiplier * AttackBonus:0.#} gems"),
                ("Speed", $"+{Data.attackSpeedPerLevel * Buff.SpeedMultiplier:0.##}x")
            };
        }
    }
}