using System.Collections.Generic;
using UnityEngine;

namespace Brian
{
    public class MiningTower : Tower
    {
        public static MiningTower Instance { get; private set; }

        [SerializeField] TowerData data;
        [SerializeField] float panelSideOffset = 1.2f;
        [SerializeField] float panelEyeHeight = 1.5f;
        [SerializeField] float armReach = 12f;
        [SerializeField] float retryInterval = 1f;
        [SerializeField] int[] barrelPrices = { 10, 15, 28, 35 };
        [SerializeField] float[] powerSpeeds = { 1f, 1.5f, 2.25f, 3.375f };
        [SerializeField] int[] powerCosts = { 103, 278, 887 };
        [SerializeField] float[] cargoYields = { 1f, 1.6f, 2.56f, 4.1f };
        [SerializeField] int[] cargoCosts = { 68, 163, 299 };
        [SerializeField] float processingBoost = 2f;

        MiningTowerController controller;
        MiningTowerTargetSelector selector;
        MiningCargo cargo;
        MiningCargoTransferController transfer;
        OrbitalAnchorProcessor processor;
        BarrelHub hub;
        MiningTowerUpgradeController upgrades;
        float retry;

        public int PowerLevel { get; private set; } = 1;
        public int CargoLevel { get; private set; } = 1;
        public int MaxPowerLevel => powerSpeeds.Length;
        public int MaxCargoLevel => cargoYields.Length;
        public float Speed => powerSpeeds[PowerLevel - 1];
        public float CargoYield => cargoYields[CargoLevel - 1];
        public bool Running { get; private set; }
        public int LastSale { get; private set; }

        bool ArmBusy => controller != null && controller.State != MiningTowerController.TowerState.Idle;
        bool CargoLoaded => cargo != null && cargo.IsLoaded;
        bool Processing => transfer != null && transfer.IsBusy;

        public override bool ShowsRange => false;
        public override string Description =>
            "Your base. If enemies destroy it, you lose. It mines and sells asteroids by itself while the wave timer runs.\n" +
            "- Mining Head: reach the next ring of richer asteroids\n" +
            "- Power: every step runs faster\n" +
            "- Cargo: more barrels from each asteroid\n" +
            "Each upgrade pays for itself in about a minute, but money spent here is not defending you.";

        protected override bool BlocksPlayer => true;

        protected override void Awake()
        {
            base.Awake();
            Instance = this;
            controller = GetComponentInChildren<MiningTowerController>(true);
            selector = GetComponentInChildren<MiningTowerTargetSelector>(true);
            cargo = GetComponentInChildren<MiningCargo>(true);
            transfer = GetComponentInChildren<MiningCargoTransferController>(true);
            processor = GetComponentInChildren<OrbitalAnchorProcessor>(true);
            hub = GetComponentInChildren<BarrelHub>(true);
            upgrades = GetComponentInChildren<MiningTowerUpgradeController>(true);
        }

        void Start()
        {
            if (Data == null && data != null) Init(data, new Vector3Int(-1, -1, -1));
            if (hub != null) while (hub.UnlockNextRepository()) { }
            ApplySpeed();
            ApplyCargo();
            if (AsteroidField.TryGetFace(FindAnyObjectByType<TowerGrid>(), out var face))
            {
                AlignEdge(face);
                if (selector != null) selector.SetPlane(face.origin, face.normal, armReach);
            }
        }

        void AlignEdge(AsteroidField.Face face)
        {
            bool any = false;
            float edge = float.MinValue;
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (!(r is MeshRenderer || r is SkinnedMeshRenderer) || r.GetComponentInParent<SimpleWaypointFollower>() != null) continue;
                var b = r.bounds;
                Vector3 n = face.normal;
                float reach = Vector3.Dot(b.center - face.origin, n) + Mathf.Abs(b.extents.x * n.x) + Mathf.Abs(b.extents.y * n.y) + Mathf.Abs(b.extents.z * n.z);
                edge = Mathf.Max(edge, reach);
                any = true;
            }
            if (any) transform.position -= face.normal * edge;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (hub != null && hub.BarrelCount > 0 && (!Processing || hub.IsFull)) SellBarrels();

            var wave = WaveDirector.Instance;
            var gm = GameManager.Instance;
            Running = wave != null && wave.WaveRunning && wave.TimeLeft > 0f && (gm == null || gm.IsPlaying);
            if (!Running || controller == null || ArmBusy) return;

            if (CargoLoaded)
            {
                if (!Processing) transfer.TryStartUnload();
                return;
            }

            retry -= Time.deltaTime;
            if (retry > 0f) return;
            retry = retryInterval;
            controller.StartMiningCycle();
        }

        void SellBarrels()
        {
            int value = 0;
            foreach (AsteroidFamily f in System.Enum.GetValues(typeof(AsteroidFamily)))
                value += hub.GetResourceCount(f) * barrelPrices[Mathf.Clamp((int)f, 0, barrelPrices.Length - 1)];
            hub.SellAll();
            if (Economy.Instance != null) Economy.Instance.Earn(value);
            LastSale = value;
        }

        void ApplySpeed()
        {
            if (controller != null) controller.SpeedMultiplier = Speed;
            if (transfer != null) transfer.SpeedMultiplier = Speed;
            if (processor != null) processor.SpeedMultiplier = Speed * processingBoost;
        }

        void ApplyCargo()
        {
            if (cargo != null) cargo.YieldMultiplier = CargoYield;
        }

        bool Pay(int cost, string what)
        {
            var eco = Economy.Instance;
            if (eco != null && eco.TrySpend(cost)) return true;
            PanelMessage($"Can't afford {what}: costs ${cost},\nyou have ${(eco != null ? eco.Money : 0)}.");
            return false;
        }

        void UpgradeMiningHead()
        {
            var eco = Economy.Instance;
            if (eco != null && !eco.CanAfford(UpgradeCost))
            {
                PanelMessage($"Can't afford Mining Head: costs ${UpgradeCost},\nyou have ${eco.Money}.");
                return;
            }
            if (!TryUpgrade()) return;
            if (upgrades != null) upgrades.ApplyUpgrade(MiningTowerUpgradeType.MiningHead);
            PanelMessage($"Mining Head Lv {Level}: reach {BaseRange:0.##} m into the grid, can mine {(AsteroidFamily)(Level - 1)}.", false);
        }

        void UpgradePower()
        {
            if (!Pay(powerCosts[PowerLevel - 1], "Power")) return;
            PowerLevel++;
            ApplySpeed();
            if (upgrades != null) upgrades.ApplyUpgrade(MiningTowerUpgradeType.Power);
            PanelMessage($"Power Lv {PowerLevel}: everything runs at x{Speed:0.0} speed.", false);
        }

        void UpgradeCargo()
        {
            if (!Pay(cargoCosts[CargoLevel - 1], "Cargo")) return;
            CargoLevel++;
            ApplyCargo();
            if (upgrades != null) upgrades.ApplyUpgrade(MiningTowerUpgradeType.Engineer);
            PanelMessage($"Cargo Lv {CargoLevel}: x{CargoYield:0.0} barrels from each asteroid.", false);
        }

        string Activity()
        {
            if (!Running) return "Paused (timer not running)";
            if (Processing) return "Processing";
            if (ArmBusy) return "Harvesting";
            return "Searching";
        }

        public override Vector3 PanelAnchor(Vector3 eye, out bool centered)
        {
            centered = true;
            Vector3 toEye = eye - transform.position;
            toEye.y = 0f;
            if (toEye.sqrMagnitude < 0.01f) toEye = Vector3.back;
            toEye.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, toEye);
            return transform.position + side * panelSideOffset + toEye * 0.6f + Vector3.up * panelEyeHeight;
        }

        public override void FillStats(List<(string label, string value)> rows)
        {
            rows.Add(("Status", Activity()));
            rows.Add(("Reach", $"{BaseRange:0.##} m into the grid"));
            rows.Add(("Can mine", Level == 1 ? "Slate" : $"Slate to {(AsteroidFamily)(Level - 1)}"));
            rows.Add(("Speed", $"x{Speed:0.0}"));
            rows.Add(("Cargo", $"x{CargoYield:0.0} barrels"));
            rows.Add(("Last sale", LastSale > 0 ? $"${LastSale}" : "-"));
        }

        public override void FillActions(List<TowerAction> actions)
        {
            var eco = Economy.Instance;
            int money = eco != null ? eco.Money : 0;

            if (CanUpgrade) actions.Add(new TowerAction($"Mining Head\nLv {Level} -> {Level + 1}  ${UpgradeCost}", money >= UpgradeCost, UpgradeMiningHead, new List<(string, string)>
            {
                ("Reach", $"+{Data.rangePerLevel:0.##} m"),
                ("Can mine", $"+{(AsteroidFamily)Level}")
            }));
            else actions.Add(new TowerAction($"Mining Head\nLv {Level}  MAX", false, null));

            if (PowerLevel < MaxPowerLevel) actions.Add(new TowerAction($"Power\nLv {PowerLevel} -> {PowerLevel + 1}  ${powerCosts[PowerLevel - 1]}", money >= powerCosts[PowerLevel - 1], UpgradePower, new List<(string, string)>
            {
                ("Speed", $"-> x{powerSpeeds[PowerLevel]:0.0}")
            }));
            else actions.Add(new TowerAction($"Power\nLv {PowerLevel}  MAX", false, null));

            if (CargoLevel < MaxCargoLevel) actions.Add(new TowerAction($"Cargo\nLv {CargoLevel} -> {CargoLevel + 1}  ${cargoCosts[CargoLevel - 1]}", money >= cargoCosts[CargoLevel - 1], UpgradeCargo, new List<(string, string)>
            {
                ("Cargo", $"-> x{cargoYields[CargoLevel]:0.0}")
            }));
            else actions.Add(new TowerAction($"Cargo\nLv {CargoLevel}  MAX", false, null));
        }
    }
}
