using System;
using System.Collections.Generic;
using System.Linq;

namespace FishingIdle.GameService.Config
{
    /// <summary>
    /// The complete, validated balance data the rules run on, plus fast lookups.
    /// </summary>
    /// <remarks>
    /// Built only by <see cref="GameConfigLoader"/> after validation passes, so the rules can
    /// trust every lookup below. Nothing in here is ever edited at runtime: a balance change means
    /// loading a new GameConfig and swapping it in.
    /// </remarks>
    public sealed class GameConfig
    {
        private readonly Dictionary<string, SpeciesConfig> _species;
        private readonly Dictionary<string, MapConfig> _maps;
        private readonly Dictionary<string, RodConfig> _rods;
        private readonly Dictionary<string, SizeCategoryConfig> _sizeCategories;
        private readonly Dictionary<string, RarityTierConfig> _rarities;
        private readonly Dictionary<int, long> _fisherXpTable;
        private readonly Dictionary<int, long> _fishXpTable;
        private readonly Dictionary<string, int> _rarityRank;

        private readonly Dictionary<string, BoatConfig> _boats;
        private readonly Dictionary<string, BaitConfig> _baits;
        private readonly Dictionary<string, CrewMemberConfig> _crew;
        private readonly Dictionary<string, GeneralUpgradeConfig> _generalUpgrades;
        private readonly Dictionary<string, BoatConfig> _boatByMap;
        private readonly Dictionary<int, PassMilestoneConfig> _passMilestones;
        private readonly Dictionary<string, RodConfig> _rodByMap;
        private readonly Dictionary<string, ClothingPieceConfig> _clothing;

        internal GameConfig(
            FishCatalogConfig fishCatalog,
            MapsConfig maps,
            ProgressionConfig progression,
            RodsConfig rods,
            EconomyConfig economy,
            ArenaConfig arena,
            ExpeditionsConfig expeditions,
            ArenaBotsConfig arenaBots,
            MarketBotsConfig marketBots,
            EquipmentConfig equipment,
            CrewConfig crew,
            UpgradesConfig upgrades,
            PassConfig pass,
            WorldBossConfig worldBoss,
            string version)
        {
            Pass = pass;
            WorldBoss = worldBoss;
            _passMilestones = pass.Milestones.ToDictionary(m => m.Level);
            Equipment = equipment;
            Crew = crew;
            _crew = crew.Members.ToDictionary(m => m.Id, StringComparer.Ordinal);
            Upgrades = upgrades;
            _generalUpgrades = upgrades.GeneralUpgrades.ToDictionary(u => u.Id, StringComparer.Ordinal);
            _boats = equipment.Boats.ToDictionary(b => b.Id, StringComparer.Ordinal);
            _boatByMap = equipment.Boats.Where(b => !string.IsNullOrEmpty(b.MapId)).ToDictionary(b => b.MapId, StringComparer.Ordinal);
            _baits = equipment.Baits.ToDictionary(b => b.Id, StringComparer.Ordinal);
            FishCatalog = fishCatalog;
            Maps = maps;
            Progression = progression;
            Rods = rods;
            Economy = economy;
            Arena = arena;
            Expeditions = expeditions;
            ArenaBots = arenaBots;
            MarketBots = marketBots;
            Version = version;

            _species = fishCatalog.Species.ToDictionary(s => s.Id, StringComparer.Ordinal);
            _maps = maps.Maps.ToDictionary(m => m.Id, StringComparer.Ordinal);
            _rods = rods.Rods.ToDictionary(r => r.Id, StringComparer.Ordinal);
            _rodByMap = rods.Rods.Where(r => IsFragmentRod(r) && !string.IsNullOrEmpty(r.FragmentMapId)).ToDictionary(r => r.FragmentMapId, StringComparer.Ordinal);
            _clothing = (equipment.Clothing?.Pieces ?? new List<ClothingPieceConfig>()).ToDictionary(p => p.Id, StringComparer.Ordinal);
            _sizeCategories = progression.Size.Categories.ToDictionary(c => c.Id, StringComparer.Ordinal);
            _rarities = progression.Rarity.Tiers.ToDictionary(t => t.Id, StringComparer.Ordinal);
            _fisherXpTable = progression.Fisher.XpTable.ToDictionary(x => x.Level, x => x.XpToNextLevel);
            _fishXpTable = progression.FishLevel.XpTable.ToDictionary(x => x.Level, x => x.XpToNextLevel);
            _rarityRank = progression.Rarity.Tiers.Select((t, i) => new { t.Id, i }).ToDictionary(x => x.Id, x => x.i, StringComparer.Ordinal);
        }

        public FishCatalogConfig FishCatalog { get; }
        public MapsConfig Maps { get; }
        public ProgressionConfig Progression { get; }
        public RodsConfig Rods { get; }
        public EconomyConfig Economy { get; }
        public ArenaConfig Arena { get; }
        public ExpeditionsConfig Expeditions { get; }
        public ArenaBotsConfig ArenaBots { get; }
        public MarketBotsConfig MarketBots { get; }
        public EquipmentConfig Equipment { get; }

        /// <summary>The automatic Crew (crew.json, M24-T05).</summary>
        public CrewConfig Crew { get; }

        public bool TryGetCrewMember(string id, out CrewMemberConfig member) => _crew.TryGetValue(id ?? string.Empty, out member);

        /// <summary>The upgrades bought with Moedas (upgrades.json, M24-T06).</summary>
        public UpgradesConfig Upgrades { get; }

        public bool TryGetGeneralUpgrade(string id, out GeneralUpgradeConfig upgrade) => _generalUpgrades.TryGetValue(id ?? string.Empty, out upgrade);

        /// <summary>The Passe do Pescador (pass.json, M24-T17).</summary>
        public PassConfig Pass { get; }

        /// <summary>The Boss Mundial (world_boss.json, M24-T29).</summary>
        public WorldBossConfig WorldBoss { get; }

        /// <summary>The pass milestone reached at exactly <paramref name="level"/>, or null.</summary>
        public PassMilestoneConfig PassMilestoneAt(int level) => _passMilestones.TryGetValue(level, out var m) ? m : null;

        /// <summary>The boat whose fragments the catches on a map drop (M24-T16), or false when the map has none.</summary>
        public bool TryGetBoatForMap(string mapId, out BoatConfig boat) => _boatByMap.TryGetValue(mapId ?? string.Empty, out boat);

        /// <summary>Whether offline fishing drops boat fragments too (equipment.json → boat_fragments).</summary>
        public bool FragmentsDropOffline => Equipment.BoatFragments?.DropOnOfflineFishing ?? true;

        /// <summary>The rod whose fragments the catches on a map drop (M24-T32), or false when the map has none.</summary>
        public bool TryGetRodForMap(string mapId, out RodConfig rod) => _rodByMap.TryGetValue(mapId ?? string.Empty, out rod);

        /// <summary>Whether offline fishing drops rod fragments too (rods.json → fragment_rules).</summary>
        public bool RodFragmentsDropOffline => Rods.FragmentRules?.DropOnOfflineFishing ?? true;

        /// <summary>A rod assembled from fragments (M24-T32): every rod but the free Starter Rod.</summary>
        public static bool IsFragmentRod(RodConfig rod) => rod?.Acquisition != null && rod.Acquisition.Method == RodAcquisitionConfig.Fragments;

        /// <summary>The free Starter Rod, claimed in the Shop during the tutorial (GDD section 40).</summary>
        public static bool IsFreeClaimRod(RodConfig rod) => rod?.Acquisition != null && rod.Acquisition.Method == RodAcquisitionConfig.FreeClaim;

        // ---------------------------------------------------------------- clothing (M24-T33, A-164)

        /// <summary>equipment.json → clothing; null when the config has none.</summary>
        public ClothingConfig Clothing => Equipment.Clothing;

        /// <summary>The clothing pieces, in the config's order (empty without clothing).</summary>
        public IReadOnlyList<ClothingPieceConfig> ClothingPieces => (IReadOnlyList<ClothingPieceConfig>)Equipment.Clothing?.Pieces ?? Array.Empty<ClothingPieceConfig>();

        public bool TryGetClothingPiece(string id, out ClothingPieceConfig piece) => _clothing.TryGetValue(id ?? string.Empty, out piece);

        public int ClothingMaxLevel => Math.Max(1, Equipment.Clothing?.MaxLevel ?? 1);

        /// <summary>A piece's bonus at a level, on the same curve as the rods (LevelCurve.Value).</summary>
        public double ClothingBonusAt(ClothingPieceConfig piece, int level)
        {
            return LevelCurve.Value(piece.BonusAtLevel1, piece.BonusAtMaxLevel, level, ClothingMaxLevel, Equipment.Clothing?.BonusCurveExponent ?? 1.0);
        }

        /// <summary>Use XP to take a piece from <paramref name="level"/> to the next; 0 at the max level.</summary>
        public long ClothingXpToNextLevel(int level)
        {
            var xp = Equipment.Clothing?.LevelXp;
            if (xp == null || level < 1 || level >= ClothingMaxLevel)
            {
                return 0;
            }

            return LevelCurve.Cost(xp.First, xp.Growth, level);
        }

        /// <summary>Fragmentos de Roupa to assemble the next piece when <paramref name="assembled"/> are assembled already.</summary>
        public int ClothingAssembleCost(int assembled)
        {
            var costs = Equipment.Clothing?.FragmentsToAssemble;
            if (costs == null || costs.Count == 0)
            {
                return 1;
            }

            return Math.Max(1, costs[Math.Max(0, Math.Min(assembled, costs.Count - 1))]);
        }

        /// <summary>The boat every player owns from the start: the lowest tier.</summary>
        public BoatConfig StarterBoat => Equipment.Boats.OrderBy(b => b.Tier).First();

        public bool TryGetBoat(string id, out BoatConfig boat) => _boats.TryGetValue(id ?? string.Empty, out boat);
        public bool TryGetBait(string id, out BaitConfig bait) => _baits.TryGetValue(id ?? string.Empty, out bait);

        /// <summary>The version of today's baits (equipment.json → bait_migration, M24-T30); 0 when the config names none.</summary>
        public int BaitModelVersion => Equipment.BaitMigration?.BaitModelVersion ?? 0;
        public MarketFixedPriceConfig Market => Economy.MarketFixedPrice;
        public AuctionConfig Auction => Economy.Auction;

        /// <summary>Number of Cardume positions (6).</summary>
        public int CardumeSize => Arena.Cardume.MaxFish;

        /// <summary>
        /// Short fingerprint of the balance files' contents. Two identical sets of files always have
        /// the same version, so it identifies which balance produced an outcome (GDD section 43).
        /// </summary>
        public string Version { get; }

        public FishingConfig Fishing => Progression.Fishing;

        public IReadOnlyList<SizeCategoryConfig> SizeCategories => Progression.Size.Categories;

        /// <summary>The map every new player starts on: the one unlocked at the lowest level.</summary>
        public MapConfig StartingMap => Maps.Maps.OrderBy(m => m.UnlockFisherLevel).First();

        /// <summary>The free rod every new player owns: the lowest tier.</summary>
        public RodConfig StarterRod => Rods.Rods.OrderBy(r => r.Tier).First();

        public bool TryGetSpecies(string id, out SpeciesConfig species) => _species.TryGetValue(id ?? string.Empty, out species);
        public bool TryGetMap(string id, out MapConfig map) => _maps.TryGetValue(id ?? string.Empty, out map);
        public bool TryGetRod(string id, out RodConfig rod) => _rods.TryGetValue(id ?? string.Empty, out rod);
        public bool TryGetSizeCategory(string id, out SizeCategoryConfig category) => _sizeCategories.TryGetValue(id ?? string.Empty, out category);

        /// <summary>Whether a size category is a highlight of its own (Excepcional, Perfeição; "special" in progression.json).</summary>
        public bool IsSpecialSize(string id) => TryGetSizeCategory(id, out var category) && category.Special;
        public bool TryGetRarity(string id, out RarityTierConfig rarity) => _rarities.TryGetValue(id ?? string.Empty, out rarity);

        /// <summary>XP needed to go from <paramref name="level"/> to the next; 0 at the max level.</summary>
        public long FisherXpToNextLevel(int level)
        {
            if (level >= Progression.Fisher.MaxLevel)
            {
                return 0;
            }

            return _fisherXpTable.TryGetValue(level, out var xp) ? xp : 0;
        }

        /// <summary>Highest internal level of a rod (1 for rods without levels; rods.json → upgrade_rules, M24-T13).</summary>
        public int RodMaxLevel(RodConfig rod)
        {
            if (!rod.HasInternalLevels || rod.BonusesAtLevel1 == null || rod.BonusesAtMaxLevel == null)
            {
                return 1;
            }

            return Math.Max(1, Rods.UpgradeRules?.InternalLevels?.Max ?? 1);
        }

        /// <summary>Coins to go from <paramref name="level"/> to the next; 0 at the max level (LevelCurve.Cost).</summary>
        public long RodUpgradeCost(RodConfig rod, int level)
        {
            if (level < 1 || level >= RodMaxLevel(rod) || rod.UpgradeCost == null)
            {
                return 0;
            }

            return LevelCurve.Cost(rod.UpgradeCost.CoinsFirst, rod.UpgradeCost.CoinsGrowth, level);
        }

        /// <summary>Conchas to go from <paramref name="level"/> to the next; 0 at the max level.</summary>
        public long RodUpgradeShellCost(RodConfig rod, int level)
        {
            if (level < 1 || level >= RodMaxLevel(rod) || rod.UpgradeCost == null)
            {
                return 0;
            }

            return LevelCurve.Cost(rod.UpgradeCost.ShellsFirst, rod.UpgradeCost.ShellsGrowth, level);
        }

        /// <summary>Highest level of a boat (1 for the starter boat and boats without levels; M24-T13).</summary>
        public int BoatMaxLevel(BoatConfig boat)
        {
            return boat.HasLevels ? Math.Max(1, Equipment.BoatLevels?.MaxLevel ?? 1) : 1;
        }

        /// <summary>The boat's Catch Success bonus at a level (equipment.json → boat_levels, M24-T13).</summary>
        public double BoatBonusAt(BoatConfig boat, int level)
        {
            if (!boat.HasLevels)
            {
                return boat.CatchSuccessBonus;
            }

            return LevelCurve.Value(boat.CatchSuccessBonus, boat.CatchSuccessBonusAtMaxLevel, level, BoatMaxLevel(boat),
                Equipment.BoatLevels?.BonusCurveExponent ?? 1.0);
        }

        /// <summary>Coins to take a boat from <paramref name="level"/> to the next; 0 at the max level.</summary>
        public long BoatUpgradeCost(BoatConfig boat, int level)
        {
            if (level < 1 || level >= BoatMaxLevel(boat) || boat.UpgradeCost == null)
            {
                return 0;
            }

            return LevelCurve.Cost(boat.UpgradeCost.CoinsFirst, boat.UpgradeCost.CoinsGrowth, level);
        }

        /// <summary>Conchas to take a boat from <paramref name="level"/> to the next; 0 at the max level.</summary>
        public long BoatUpgradeShellCost(BoatConfig boat, int level)
        {
            if (level < 1 || level >= BoatMaxLevel(boat) || boat.UpgradeCost == null)
            {
                return 0;
            }

            return LevelCurve.Cost(boat.UpgradeCost.ShellsFirst, boat.UpgradeCost.ShellsGrowth, level);
        }

        /// <summary>
        /// The fish's attribute bonus at a level: 0 at Nv.1, stat_bonus_at_max_level_percent ÷ 100 at the max, on the
        /// curve in between (progression.json → fish_level, M24-T13). Attributes are multiplied by 1 + this.
        /// </summary>
        public double FishStatBonus(int level)
        {
            var fish = Progression.FishLevel;
            return LevelCurve.Value(0.0, fish.StatBonusAtMaxLevelPercent / 100.0, level, fish.MaxLevel, fish.StatBonusCurveExponent);
        }

        /// <summary>XP a fish needs to go from <paramref name="level"/> to the next; 0 at the max level.</summary>
        public long FishXpToNextLevel(int level)
        {
            if (level >= Progression.FishLevel.MaxLevel)
            {
                return 0;
            }

            return _fishXpTable.TryGetValue(level, out var xp) ? xp : 0;
        }

        /// <summary>Position of a rarity in the power hierarchy (order of rarity.tiers); unknown = -1.</summary>
        public int RarityRank(string rarityId)
        {
            return rarityId != null && _rarityRank.TryGetValue(rarityId, out var rank) ? rank : -1;
        }

        public int AquariumCapacity => Economy.Aquarium.HardCapacity;

        /// <summary>The rod's bonuses at an internal level (1 to 100, on the curve of LevelCurve). Rods without levels ignore it.</summary>
        public RodBonusesConfig RodBonusesAt(RodConfig rod, int level)
        {
            if (!rod.HasInternalLevels || rod.BonusesAtLevel1 == null || rod.BonusesAtMaxLevel == null)
            {
                return rod.Bonuses ?? new RodBonusesConfig();
            }

            var a = rod.BonusesAtLevel1;
            var z = rod.BonusesAtMaxLevel;
            var max = RodMaxLevel(rod);
            var exponent = Rods.UpgradeRules?.BonusCurveExponent ?? 1.0;
            return new RodBonusesConfig
            {
                RarityEfficiency = LevelCurve.Value(a.RarityEfficiency, z.RarityEfficiency, level, max, exponent),
                SizeQuality = LevelCurve.Value(a.SizeQuality, z.SizeQuality, level, max, exponent),
                ShellYield = LevelCurve.Value(a.ShellYield, z.ShellYield, level, max, exponent),
                CatchSuccess = LevelCurve.Value(a.CatchSuccess, z.CatchSuccess, level, max, exponent),
            };
        }
    }
}
