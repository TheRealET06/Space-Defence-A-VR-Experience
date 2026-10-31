using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
//using static Unity.VisualScripting.Round<TInput, TOutput>;

namespace Brian
{
    /// <summary>
    /// Built on a gas planet (NodeType.Gas). Charges up, then fires a short beam that draws
    /// off the gas the planet has built up. The gas is stored in the extractor until the player
    /// collects it with the "Collect" option in the tower menu; collecting adds it to Economy.Gas,
    /// which is separate from money and gems.
    /// Stat mapping (from TowerData, with level scaling and buffs):
    ///   Attack       = yield multiplier on the gas drawn off (set Attack to 1 for 100%)
    ///   AttackSpeed  = how fast it charges between beams
    /// Range is not used: the extractor works on the planet it sits on.
    /// </summary>
    public class GasExtractor : Tower
    {
        [Header("Visuals")]
        [SerializeField] float spinSpeed = 8f;          // degrees per second

        [Header("Extraction cycle")]
        [SerializeField] float chargeTime = 5f;         // seconds to charge at AttackSpeed 1
        [SerializeField] float beamTime = 1f;           // seconds the beam stays on

        [Header("Events")]
        public UnityEvent laserStart;
        public UnityEvent laserEnd;
        public UnityEvent collected;                    // fires when Collect() banks at least 1 gas

        GasPlanet planet;
        bool on;
        float timer;
        double stored;                                   // extracted but not yet collected

        public override bool UsesNode => true;
        public override NodeType RequiredNode => NodeType.Gas;

        public bool BeamOn => on;
        public GasPlanet Planet => planet;
        public int StoredGas => (int)stored;

        float ChargeSpeed => Mathf.Max(0.05f, AttackSpeed);
        public bool Running { get; private set; }

        void Start()
        {
            transform.Rotate(Random.Range(0, 90), Random.Range(0, 180), 0);
        }

        void Update()
        {
            if (Data == null) return;

            transform.Rotate(0, 0, spinSpeed * Time.deltaTime);

            // Node is assigned by BuildController right after Init
            if (planet == null && Node != null) planet = Node.GetComponentInParent<GasPlanet>();

            var director = WaveDirector.Instance;
            var gm = GameManager.Instance;
            Running = director != null && director.WaveRunning && (gm == null || gm.IsPlaying);
            if (!Running) { return; } // run only when there is a wave

            if (planet == null)
            {
                if (on) EndBeam();
                return;
            }

            if (on)
            {
                timer += Time.deltaTime;
                if (timer >= beamTime) EndBeam();
            }
            else
            {
                timer += Time.deltaTime * ChargeSpeed;
                if (timer >= chargeTime) StartBeam();
            }
        }

        void StartBeam()
        {
            on = true;
            timer = 0f;
            stored += planet.Extract() * Attack;
            laserStart.Invoke();
        }

        void EndBeam()
        {
            on = false;
            timer = 0f;
            laserEnd.Invoke();
        }

        /// <summary>
        /// Moves stored gas into the player's gas count (whole units only; the fraction stays stored).
        /// Called by the tower menu's Collect option (can also be hooked to an event).
        /// </summary>
        public void Collect()
        {
            int whole = (int)stored;
            if (whole <= 0 || Economy.Instance == null) return;
            Economy.Instance.EarnGas(whole);
            stored -= whole;
            collected.Invoke();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (on) { on = false; laserEnd.Invoke(); }
        }

        // ---- Tower panel ----

        public override string Description =>
            Join(Summary, "Can only be built on a gas planet. Collect extracted gas from its menu. Upgrade: higher yield and faster charging.");

        public override void FillStats(List<(string label, string value)> rows)
        {
            rows.Add(("Stored", $"{StoredGas} gas"));
            rows.Add(("Yield", $"{Attack * 100f:0}%"));
            rows.Add(("Charge time", $"{chargeTime / ChargeSpeed:0.#} s"));
            if (planet != null) rows.Add(("Planet gas", $"{planet.Stored:0}"));
        }

        public override void FillActions(List<TowerAction> actions)
        {
            int ready = StoredGas;
            actions.Add(new TowerAction($"Collect  {ready} gas", ready > 0 && Economy.Instance != null, () =>
            {
                int before = StoredGas;
                Collect();
                PanelMessage(before > 0 ? $"Collected {before} gas." : "No gas to collect yet.", error: false);
            }));
            base.FillActions(actions);
        }

        protected override List<(string label, string change)> UpgradePreview()
        {
            return new List<(string, string)>
            {
                ("Yield", $"+{Data.attackPerLevel * Buff.AttackMultiplier * AttackBonus * 100f:0}%"),
                ("Charge speed", $"+{Data.attackSpeedPerLevel * Buff.SpeedMultiplier:0.##}x")
            };
        }
    }
}