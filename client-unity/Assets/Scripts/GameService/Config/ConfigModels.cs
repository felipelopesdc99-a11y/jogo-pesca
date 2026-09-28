using System.Collections.Generic;

namespace FishingIdle.GameService.Config
{
    // Typed views over the balance files in /config. Property names map to the snake_case keys in
    // the JSON through JsonSettings (SnakeCaseNamingStrategy), so "base_fisher_xp" ↔ BaseFisherXp.
    // Only the fields the implemented milestones use are modelled; unknown keys are ignored, so
    // later milestones add properties here without touching the files' format.

    // ---------------------------------------------------------------- fish_catalog.json

    public sealed class FishCatalogConfig
    {
        public int ConfigSchemaVersion { get; set; }
        public List<SpeciesConfig> Species { get; set; }
    }

    public sealed class SpeciesConfig
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string PrimaryMapId { get; set; }
        public string Rarity { get; set; }
        public RangeConfig SizeCm { get; set; }
        public BaseStatsConfig BaseStats { get; set; }
        public double BaseSaleValueCoins { get; set; }
        public double BaseFeedXp { get; set; }
        public double BaseFisherXp { get; set; }
    }

    public sealed class RangeConfig
    {
        public double Min { get; set; }
        public double Max { get; set; }
    }

    public sealed class BaseStatsConfig
    {
        public double Hp { get; set; }
        public double Attack { get; set; }
        public double Defense { get; set; }
        public double Speed { get; set; }
    }

    // ---------------------------------------------------------------- maps.json

    public sealed class MapsConfig
    {
        public int ConfigSchemaVersion { get; set; }
        public TravelConfig Travel { get; set; }
        public List<MapConfig> Maps { get; set; }
    }

    public sealed class TravelConfig
    {
        public double DurationSeconds { get; set; }
    }

    public sealed class MapConfig
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public int UnlockFisherLevel { get; set; }
        public int MinimumRodTier { get; set; }
        public List<string> AvailableRarities { get; set; }
        public List<FishPoolEntryConfig> FishPool { get; set; }
    }

    public sealed class FishPoolEntryConfig
    {
        public string SpeciesId { get; set; }
        public double CatchWeight { get; set; }
    }

    // ---------------------------------------------------------------- progression.json

    public sealed class ProgressionConfig
    {
        public int ConfigSchemaVersion { get; set; }
        public FisherConfig Fisher { get; set; }
        public SizeConfig Size { get; set; }
        public RarityConfig Rarity { get; set; }
        public FishingConfig Fishing { get; set; }
    }

    public sealed class FisherConfig
    {
        public int MaxLevel { get; set; }
        public List<XpLevelConfig> XpTable { get; set; }
    }

    public sealed class XpLevelConfig
    {
        public int Level { get; set; }
        public long XpToNextLevel { get; set; }
    }

    public sealed class SizeConfig
    {
        public List<SizeCategoryConfig> Categories { get; set; }
        public InfluenceConfig SaleValueInfluence { get; set; }
    }

    public sealed class SizeCategoryConfig
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public double DrawWeight { get; set; }
        public double PercentileMin { get; set; }
        public double PercentileMax { get; set; }
        public double FisherXpMultiplier { get; set; }
    }

    public sealed class InfluenceConfig
    {
        public double Influence { get; set; }
    }

    public sealed class RarityConfig
    {
        public List<RarityTierConfig> Tiers { get; set; }
    }

    public sealed class RarityTierConfig
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public double StatMultiplier { get; set; }
        public double FisherXpMultiplier { get; set; }
        public double FeedXpMultiplier { get; set; }
        public double SaleValueMultiplier { get; set; }
    }

    public sealed class FishingConfig
    {
        public double OnlineCycleSeconds { get; set; }
        public double OfflineCycleSeconds { get; set; }
        public double OfflineAccumulationCapHours { get; set; }
    }

    // ---------------------------------------------------------------- rods.json

    public sealed class RodsConfig
    {
        public int ConfigSchemaVersion { get; set; }
        public List<RodConfig> Rods { get; set; }
    }

    public sealed class RodConfig
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public int Tier { get; set; }
        public bool HasInternalLevels { get; set; }
        public List<string> CanCatchRarities { get; set; }
        public bool GeneratesShells { get; set; }

        /// <summary>Flat bonuses, for rods without internal levels (the Starter Rod).</summary>
        public RodBonusesConfig Bonuses { get; set; }

        /// <summary>Total bonus at each internal level (index 0 = Lv.1), for upgradable rods.</summary>
        public RodBonusesPerLevelConfig BonusesPerLevel { get; set; }
    }

    public sealed class RodBonusesConfig
    {
        public double RarityEfficiency { get; set; }
        public double SizeQuality { get; set; }
        public double ShellYield { get; set; }
    }

    public sealed class RodBonusesPerLevelConfig
    {
        public List<double> RarityEfficiency { get; set; }
        public List<double> SizeQuality { get; set; }
        public List<double> ShellYield { get; set; }
    }

    // ---------------------------------------------------------------- economy.json

    public sealed class EconomyConfig
    {
        public int ConfigSchemaVersion { get; set; }
        public NpcFishSaleConfig NpcFishSale { get; set; }
        public ShellsConfig Shells { get; set; }
        public FishingBoxConfig FishingBox { get; set; }
    }

    public sealed class NpcFishSaleConfig
    {
        public long MinimumPriceCoins { get; set; }
    }

    public sealed class ShellsConfig
    {
        public double BaseDropChancePerCatch { get; set; }
        public IntRangeConfig AmountPerDrop { get; set; }
    }

    public sealed class IntRangeConfig
    {
        public int Min { get; set; }
        public int Max { get; set; }
    }

    public sealed class FishingBoxConfig
    {
        public BulkSaleProtectionConfig BulkSaleProtection { get; set; }
    }

    /// <summary>Which catches trigger the "Revisar peixes / Confirmar" dialog on a bulk sale.</summary>
    public sealed class BulkSaleProtectionConfig
    {
        public List<string> Rarities { get; set; }
        public List<string> SizeCategories { get; set; }
    }
}
