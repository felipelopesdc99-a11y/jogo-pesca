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
        public MapThemeConfig VisualTheme { get; set; }
    }

    public sealed class MapThemeConfig
    {
        public string Summary { get; set; }
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
        public FishLevelConfig FishLevel { get; set; }
        public FeedingConfig Feeding { get; set; }
    }

    public sealed class FishLevelConfig
    {
        public int MaxLevel { get; set; }
        public double StatBonusPerLevelPercent { get; set; }
        public List<XpLevelConfig> XpTable { get; set; }
    }

    public sealed class FeedingConfig
    {
        public double InvestedXpRecoveryRatio { get; set; }
        public bool WarnOnValuableFeed { get; set; }
        public ValuableFeedRulesConfig ValuableFeedRules { get; set; }
    }

    /// <summary>Which fish ask for confirmation before being consumed as food.</summary>
    public sealed class ValuableFeedRulesConfig
    {
        public string RarityAtOrAbove { get; set; }
        public List<string> SizeCategories { get; set; }
        public int MinLevel { get; set; }
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
        public InfluenceConfig StatInfluence { get; set; }
        public InfluenceConfig FeedXpInfluence { get; set; }
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

        public RodAcquisitionConfig Acquisition { get; set; }
        public List<RodUpgradeCostConfig> UpgradeCosts { get; set; }
        public RodResaleConfig NpcResale { get; set; }
        public long NpcResaleValueCoins { get; set; }
        public bool TradableOnMarket { get; set; }

        /// <summary>Flat bonuses, for rods without internal levels (the Starter Rod).</summary>
        public RodBonusesConfig Bonuses { get; set; }

        /// <summary>Total bonus at each internal level (index 0 = Lv.1), for upgradable rods.</summary>
        public RodBonusesPerLevelConfig BonusesPerLevel { get; set; }
    }

    public sealed class RodAcquisitionConfig
    {
        /// <summary>"coin_purchase" rods are sold in the Shop; "free_claim_in_shop" is the Starter Rod.</summary>
        public string Method { get; set; }
        public long PurchaseCostCoins { get; set; }
        public int UnlockFisherLevel { get; set; }
    }

    public sealed class RodUpgradeCostConfig
    {
        public int ToLevel { get; set; }
        public long CostCoins { get; set; }
    }

    /// <summary>NPC resale: part of the purchase price plus part of what was spent on upgrades.</summary>
    public sealed class RodResaleConfig
    {
        public double BaseValueRatio { get; set; }
        public double UpgradeInvestmentRatio { get; set; }
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
        public AquariumConfig Aquarium { get; set; }
    }

    public sealed class AquariumConfig
    {
        public int HardCapacity { get; set; }
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

    // ---------------------------------------------------------------- arena.json (Cardume parts)

    public sealed class ArenaConfig
    {
        public int ConfigSchemaVersion { get; set; }
        public FormationConfig Formation { get; set; }
        public CardumeConfig Cardume { get; set; }
        public CardumeStrengthConfig CardumeStrength { get; set; }
    }

    public sealed class FormationConfig
    {
        public List<int> FrontPositions { get; set; }
        public List<int> BackPositions { get; set; }
    }

    public sealed class CardumeConfig
    {
        public int MinFish { get; set; }
        public int MaxFish { get; set; }
        public CompleteBonusConfig CompleteBonus { get; set; }
    }

    /// <summary>The 6/6 bonus: +3% to every stat while all slots are filled (GDD section 23).</summary>
    public sealed class CompleteBonusConfig
    {
        public int RequiresFilledSlots { get; set; }
        public double HpPercent { get; set; }
        public double AttackPercent { get; set; }
        public double DefensePercent { get; set; }
        public double SpeedPercent { get; set; }
    }

    /// <summary>Private Cardume Strength (GDD section 31).</summary>
    public sealed class CardumeStrengthConfig
    {
        public StrengthWeightsConfig Weights { get; set; }
        public double DisplayScale { get; set; }
    }

    public sealed class StrengthWeightsConfig
    {
        public double Attack { get; set; }
        public double Defense { get; set; }
        public double HpDivisor { get; set; }
        public double Speed { get; set; }
    }
}
