using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
//using UnityEngine.SocialPlatforms.Impl;

namespace Brian
{
    /// <summary>
    /// Built on a gem asteroid (NodeType.Gem). Charges up, then fires a short mining beam.
    /// Mined gems are stored in the extractor until the player collects them with the
    /// "Collect" option in the tower menu. Collected gems go to Economy.Gems, which is
    /// separate from money.
    /// Stat mapping (from TowerData, with level scaling and buffs):
    ///   Attack       = gems mined per second while the beam is on
    ///   AttackSpeed  = how fast it charges between beams
    /// Range is not used: the extractor mines the asteroid it sits on.
    /// </summary>

    public class GemExtractor : Tower
    {
        [Header("Visuals")]
        [SerializeField] float spinSpeed = 8f;          // degrees per second

        [Header("Mining cycle")]
        [SerializeField] float chargeTime = 5f;         // seconds to charge at AttackSpeed 1
        [SerializeField] float beamTime = 1f;           // seconds the beam stays on

        [Header("Events")]
        public UnityEvent laserStart;
        public UnityEvent laserEnd;
        public UnityEvent collected;                    

        CrystalAsteroid asteroid;
        bool on;
        float timer;
        double stored;

        public override bool UsesNode => true;
        public override NodeType RequiredNode => NodeType.Gem;

        public bool BeamOn => on;
        public CrystalAsteroid Asteroid => asteroid;
        public int StoredGems => (int)stored;
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
            if (asteroid == null && Node != null) asteroid = Node.GetComponentInParent<CrystalAsteroid>();

            var director = WaveDirector.Instance;
            var gm = GameManager.Instance;
            Running = director != null && director.WaveRunning && (gm == null || gm.IsPlaying);
            if (!Running) { return; } // run only when there is a wave

            if (asteroid == null || asteroid.Depleted)
            {
                if (on) EndBeam();
                return;
            }

            if (on)
            {
                timer += Time.deltaTime;
                //Bank();
                if (timer >= beamTime || asteroid.Depleted) EndBeam();
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
            laserStart.Invoke();
        }

        void EndBeam()
        {
            on = false;
            timer = 0f;
            //Bank();
            stored += asteroid.Extract(Attack);
            laserEnd.Invoke();
        }

        /// <summary>
        /// Moves stored gems into the player's gem count (whole gems only; the fraction stays stored).
        /// Called by the tower menu's Collect option (can also be hooked to an event).
        /// </summary>
        public void Collect()
        {
            int whole = (int)stored;
            if (whole <= 0 || Economy.Instance == null) return;
            Economy.Instance.EarnGems(whole);
            stored -= whole;
            collected.Invoke();
        }


        protected override void OnDisable()
        {
            base.OnDisable();
            if (on) { on = false; laserEnd.Invoke(); }
        }

        // ---- Tower panel text ----

        public override string Description =>
            Join(Summary, "Can only be built on a gem asteroid. Collect mined gems from its menu. Upgrade: mines more gems per second and charges faster.");

        public override void FillStats(List<(string label, string value)> rows)
        {
            rows.Add(("Stored", $"{StoredGems} gems"));
            rows.Add(("Mining", WithBuff(BaseAttack, Attack, "0.#") + " gems/charge"));
            rows.Add(("Charge time", $"{chargeTime / ChargeSpeed:0.#} s"));
            if (asteroid != null) rows.Add(("Gems left", $"{asteroid.Remaining:0}"));
        }

        public override void FillActions(List<TowerAction> actions)
        {
            int ready = StoredGems;
            actions.Add(new TowerAction($"Collect  {ready} gems", ready > 0 && Economy.Instance != null, () =>
            {
                int before = StoredGems;
                Collect();
                PanelMessage(before > 0 ? $"Collected {before} gems." : "No gems to collect yet.", error: false);
            }));
            base.FillActions(actions);
        }

        protected override List<(string label, string change)> UpgradePreview()
        {
            return new List<(string, string)>
            {
                ("Mining", $"+{Data.attackPerLevel * Buff.AttackMultiplier * AttackBonus:0.#} gems/s"),
                ("Charge speed", $"+{Data.attackSpeedPerLevel * Buff.SpeedMultiplier:0.##}x")
            };
        }

    }
}