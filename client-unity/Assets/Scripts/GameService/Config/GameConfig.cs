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
            string version)
        {
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

        /// <summary>Highest internal level of a rod (1 for rods without levels).</summary>
        public int RodMaxLevel(RodConfig rod)
        {
            if (!rod.HasInternalLevels || rod.BonusesPerLevel?.RarityEfficiency == null)
            {
                return 1;
            }

            return Math.Max(1, rod.BonusesPerLevel.RarityEfficiency.Count);
        }

        /// <summary>Coins to go from <paramref name="level"/> to the next; 0 at the max level.</summary>
        public long RodUpgradeCost(RodConfig rod, int level)
        {
            if (level >= RodMaxLevel(rod) || rod.UpgradeCosts == null)
            {
                return 0;
            }

            var step = rod.UpgradeCosts.FirstOrDefault(c => c.ToLevel == level + 1);
            return step?.CostCoins ?? 0;
        }

        public bool IsPurchasable(RodConfig rod) => rod.Acquisition != null && rod.Acquisition.Method == "coin_purchase";

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

        /// <summary>The rod's bonuses at an internal level (1–10). Rods without levels ignore it.</summary>
        public RodBonusesConfig RodBonusesAt(RodConfig rod, int level)
        {
            if (!rod.HasInternalLevels || rod.BonusesPerLevel == null)
            {
                return rod.Bonuses ?? new RodBonusesConfig();
            }

            var perLevel = rod.BonusesPerLevel;
            var index = Math.Max(0, level - 1);
            return new RodBonusesConfig
            {
                RarityEfficiency = At(perLevel.RarityEfficiency, index),
                SizeQuality = At(perLevel.SizeQuality, index),
                ShellYield = At(perLevel.ShellYield, index),
            };
        }

        private static double At(IReadOnlyList<double> values, int index)
        {
            if (values == null || values.Count == 0)
            {
                return 0;
            }

            return values[Math.Min(index, values.Count - 1)];
        }
    }
}
