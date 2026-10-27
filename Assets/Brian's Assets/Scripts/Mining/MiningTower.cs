using System.Collections.Generic;
using UnityEngine;

namespace Brian
{
    public class MiningTower : Tower
    {
        public static MiningTower Instance { get; private set; }

        [SerializeField] TowerData data;
        [SerializeField] float selectDistance = 7f;
        [SerializeField] float panelSideOffset = 3f;
        [SerializeField] float panelEyeHeight = 1.5f;
        [SerializeField] float retryInterval = 1f;
        [SerializeField] int[] barrelPrices = { 10, 12, 15, 20 };
        [SerializeField] float[] powerSpeeds = { 1f, 1.6f, 2.2f, 3f };
        [SerializeField] int[] powerCosts = { 40, 80, 160 };
        [SerializeField] float[] cargoYields = { 1f, 1.5f, 2f, 2.5f };
        [SerializeField] int[] cargoCosts = { 30, 60, 120 };

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

        public override Vector3 RangeCenter => selector != null ? selector.transform.position : transform.position;
        public override float MaxSelectDistance => selectDistance;
        public override string Description =>
            "Mines and sells asteroids by itself while a wave is running.\n" +
            "- Mining Head: reach the next ring of richer asteroids\n" +
            "- Power: every step runs faster\n" +
            "- Cargo: more barrels from each asteroid";

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
            Running = wave != null && wave.WaveRunning && (gm == null || gm.IsPlaying);
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
            if (processor != null) processor.SpeedMultiplier = Speed;
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
            PanelMessage($"Mining Head Lv {Level}: reach {Range:0} m, can mine {(AsteroidFamily)(Level - 1)}.", false);
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
            if (!Running) return "Paused (no wave)";
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
            return transform.position + side * panelSideOffset + toEye * 1.5f + Vector3.up * panelEyeHeight;
        }

        public override void FillStats(List<(string label, string value)> rows)
        {
            rows.Add(("Status", Activity()));
            rows.Add(("Reach", $"{Range:0} m"));
            rows.Add(("Can mine", Level == 1 ? "Slate" : $"Slate to {(AsteroidFamily)(Level - 1)}"));
            rows.Add(("Speed", $"x{Speed:0.0}"));
            rows.Add(("Cargo", $"x{CargoYield:0.0} barrels"));
            rows.Add(("Last sale", LastSale > 0 ? $"${LastSale}" : "-"));
        }

        public override void FillActions(List<TowerAction> actions)
        {
            var eco = Economy.Instance;
            int money = eco != null ? eco.Money : 0;

            if (CanUpgrade) actions.Add(new TowerAction($"Mining Head\nLv {Level + 1}  ${UpgradeCost}", money >= UpgradeCost, UpgradeMiningHead, new List<(string, string)>
            {
                ("Reach", $"+{Data.rangePerLevel:0} m"),
                ("Can mine", $"+{(AsteroidFamily)Level}")
            }));
            else actions.Add(new TowerAction("Mining Head\nMAX", false, null));

            if (PowerLevel < MaxPowerLevel) actions.Add(new TowerAction($"Power\nLv {PowerLevel + 1}  ${powerCosts[PowerLevel - 1]}", money >= powerCosts[PowerLevel - 1], UpgradePower, new List<(string, string)>
            {
                ("Speed", $"-> x{powerSpeeds[PowerLevel]:0.0}")
            }));
            else actions.Add(new TowerAction("Power\nMAX", false, null));

            if (CargoLevel < MaxCargoLevel) actions.Add(new TowerAction($"Cargo\nLv {CargoLevel + 1}  ${cargoCosts[CargoLevel - 1]}", money >= cargoCosts[CargoLevel - 1], UpgradeCargo, new List<(string, string)>
            {
                ("Cargo", $"-> x{cargoYields[CargoLevel]:0.0}")
            }));
            else actions.Add(new TowerAction("Cargo\nMAX", false, null));
        }
    }
}
