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

        /// <summary>Multiplies every attribute of fish of this size (Perfeição: 1,05 = +5%). 1 when not set.</summary>
        public double StatMultiplier { get; set; } = 1.0;

        /// <summary>A size that is a highlight of its own (Excepcional, Perfeição): seal, celebration, protected on sale.</summary>
        public bool Special { get; set; }
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

        /// <summary>Chance of pulling a fish of this rarity out with no equipment bonus (0,5 = 50%). 1 when not set.</summary>
        public double CatchSuccessBase { get; set; } = 1.0;

        /// <summary>
        /// Multiplies the draw weight of size categories for fish of this rarity (e.g. "large": 0.85):
        /// rarer fish come big a little less often. Categories not listed keep their weight.
        /// </summary>
        public Dictionary<string, double> SizeWeightMultipliers { get; set; }

        /// <summary>The multiplier of one size category (1 when not listed).</summary>
        public double SizeWeightMultiplier(string sizeCategoryId)
        {
            return SizeWeightMultipliers != null && sizeCategoryId != null && SizeWeightMultipliers.TryGetValue(sizeCategoryId, out var m) ? m : 1.0;
        }
    }

    public sealed class FishingConfig
    {
        public double OnlineCycleSeconds { get; set; }
        public double OfflineCycleSeconds { get; set; }
        public double OfflineAccumulationCapHours { get; set; }

        /// <summary>Floor and ceiling of the Catch Success chance (docs/SISTEMA_SUCESSO_PESCA.md).</summary>
        public double CatchSuccessMin { get; set; } = 0.05;
        public double CatchSuccessMax { get; set; } = 0.95;
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

        /// <summary>Percentage points added to the Catch Success chance (0,05 = +5%).</summary>
        public double CatchSuccess { get; set; }
    }

    public sealed class RodBonusesPerLevelConfig
    {
        public List<double> RarityEfficiency { get; set; }
        public List<double> SizeQuality { get; set; }
        public List<double> ShellYield { get; set; }
        public List<double> CatchSuccess { get; set; }
    }

    // ---------------------------------------------------------------- economy.json

    public sealed class EconomyConfig
    {
        public int ConfigSchemaVersion { get; set; }
        public NpcFishSaleConfig NpcFishSale { get; set; }
        public ShellsConfig Shells { get; set; }
        public FishingBoxConfig FishingBox { get; set; }
        public AquariumConfig Aquarium { get; set; }
        public MarketFixedPriceConfig MarketFixedPrice { get; set; }
        public AuctionConfig Auction { get; set; }
    }

    /// <summary>The Auction rules (GDD section 35).</summary>
    public sealed class AuctionConfig
    {
        public int MaxActiveAuctionsPerSeller { get; set; }
        public double DurationHours { get; set; }
        public double MinBidIncrementRatio { get; set; }
        public double BidFeeRatio { get; set; }
        public double AntiSnipeWindowSeconds { get; set; }
        public double AntiSnipeResetToSeconds { get; set; }
        public double SellerEarlyCloseFeeRatio { get; set; }
        public double TimedCompletionFeeRatio { get; set; }
    }

    /// <summary>The fixed-price Market rules (GDD section 33).</summary>
    public sealed class MarketFixedPriceConfig
    {
        public int MaxActiveListingsPerPlayer { get; set; }
        public double MaxListingDurationDays { get; set; }
        public long ListingFeeCoins { get; set; }
        public double CompletedSaleFeeRatio { get; set; }
        public long MinimumListingPriceCoins { get; set; }
    }

    public sealed class AquariumConfig
    {
        public int HardCapacity { get; set; }
    }

    public sealed class NpcFishSaleConfig
    {
        public long MinimumPriceCoins { get; set; }

        /// <summary>Scales every fish sale price at once (1 = the catalog values).</summary>
        public double PriceMultiplier { get; set; } = 1.0;
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
        public EnergyConfig Energy { get; set; }
        public OpponentSelectionConfig OpponentSelection { get; set; }
        public HonorConfig Honor { get; set; }
        public CombatConfig Combat { get; set; }
        public ArenaShopConfig Shop { get; set; }
        public FormationConfig Formation { get; set; }
        public CardumeConfig Cardume { get; set; }
        public CardumeStrengthConfig CardumeStrength { get; set; }
    }

    public sealed class FormationConfig
    {
        public List<int> FrontPositions { get; set; }
        public List<int> BackPositions { get; set; }
        public List<int> TargetPriority { get; set; }
    }

    public sealed class EnergyConfig
    {
        public int Max { get; set; }
        public double RegenerationSecondsPerPoint { get; set; }
        public int CostPerInitiatedAttack { get; set; }
    }

    public sealed class OpponentSelectionConfig
    {
        public int OpponentsPerSet { get; set; }
        public double RankWindowPercentAbove { get; set; }
        public int RerollsPerSet { get; set; }
    }

    public sealed class HonorConfig
    {
        public long AttackerVictoryGain { get; set; }
        public long SuccessfulDefenseGain { get; set; }
        public long DefeatLoss { get; set; }
        public long MinimumBalance { get; set; }
    }

    public sealed class CombatConfig
    {
        public RangeConfig DamageRoll { get; set; }
        public DefenseMitigationConfig DefenseMitigation { get; set; }
        public SpeedConfig Speed { get; set; }
        public double TargetBattleDurationSeconds { get; set; }
    }

    public sealed class DefenseMitigationConfig
    {
        public double Constant { get; set; }
        public double MinimumDamageRatioOfAttack { get; set; }
    }

    public sealed class SpeedConfig
    {
        public double BaseIntervalSeconds { get; set; }
        public double ReferenceSpeed { get; set; }
    }

    public sealed class ArenaShopConfig
    {
        public string Currency { get; set; }
        public List<ArenaShopItemConfig> Items { get; set; }
    }

    public sealed class ArenaShopItemConfig
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public long PriceHonor { get; set; }
    }

    // ---------------------------------------------------------------- arena_bots.json (local MVP)

    public sealed class ArenaBotsConfig
    {
        public int ConfigSchemaVersion { get; set; }
        public int BotCount { get; set; }
        public List<string> Names { get; set; }
        public BotStrengthConfig StrengthByRank { get; set; }
        public IncomingAttacksConfig IncomingAttacks { get; set; }
    }

    public sealed class BotStrengthConfig
    {
        public int TopFishLevel { get; set; }
        public int BottomFishLevel { get; set; }
        public int TopCardumeSize { get; set; }
        public int BottomCardumeSize { get; set; }
        public double TopSizePercentile { get; set; }
        public double BottomSizePercentile { get; set; }
        public double SecondMapRankFraction { get; set; }
        public string SecondMapId { get; set; }
        public string FirstMapId { get; set; }
    }

    public sealed class IncomingAttacksConfig
    {
        public double CheckIntervalMinutes { get; set; }
        public double ChancePerCheck { get; set; }
        public double AttackerWindowPercentBelow { get; set; }
        public int MaxChecksPerCatchUp { get; set; }
    }

    // ---------------------------------------------------------------- market_bots.json (local MVP)

    public sealed class MarketBotsConfig
    {
        public int ConfigSchemaVersion { get; set; }
        public MarketValuationConfig Valuation { get; set; }
        public MarketSupplyConfig Supply { get; set; }
        public MarketDemandConfig Demand { get; set; }
        public AuctionBotsConfig Auctions { get; set; }
    }

    public sealed class AuctionBotsConfig
    {
        public int TargetAuctionCount { get; set; }
        public double RefreshIntervalMinutes { get; set; }
        public int NewAuctionsPerRefresh { get; set; }
        public double RodAuctionChance { get; set; }
        public RangeConfig StartingBidRatio { get; set; }
        public double BidCheckIntervalMinutes { get; set; }
        public double BidChancePerCheck { get; set; }
        public double MaxBidRatio { get; set; }
        public RangeConfig ExtraRaiseRatio { get; set; }
        public int MaxChecksPerCatchUp { get; set; }
    }

    public sealed class MarketValuationConfig
    {
        public double FishReferenceRatio { get; set; }
        public double FishLevelPremiumPerLevel { get; set; }
        public double RodReferenceRatio { get; set; }
    }

    public sealed class MarketSupplyConfig
    {
        public int TargetListingCount { get; set; }
        public double RefreshIntervalMinutes { get; set; }
        public int NewListingsPerRefresh { get; set; }
        public double ListingDurationHours { get; set; }
        public double RodListingChance { get; set; }
        public int MaxFishLevel { get; set; }
        public RangeConfig PriceRatio { get; set; }
    }

    public sealed class MarketDemandConfig
    {
        public double CheckIntervalMinutes { get; set; }
        public double ChanceAtReferencePrice { get; set; }
        public double MaxPriceRatio { get; set; }
        public int MaxChecksPerCatchUp { get; set; }
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

    // ---------------------------------------------------------------- expeditions.json

    public sealed class ExpeditionsConfig
    {
        public int ConfigSchemaVersion { get; set; }
        public ExpeditionEfficiencyConfig Efficiency { get; set; }
        public List<ExpeditionConfig> Expeditions { get; set; }
    }

    /// <summary>Reward multiplier from Cardume Strength vs Recommended Strength (GDD section 32).</summary>
    public sealed class ExpeditionEfficiencyConfig
    {
        public BelowRecommendedConfig BelowRecommended { get; set; }
        public double AtRecommendedMultiplier { get; set; }
        public AboveRecommendedConfig AboveRecommended { get; set; }
    }

    public sealed class BelowRecommendedConfig
    {
        public double Exponent { get; set; }
        public double Floor { get; set; }
    }

    public sealed class AboveRecommendedConfig
    {
        public double Slope { get; set; }
        public double Cap { get; set; }
    }

    public sealed class ExpeditionConfig
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public double DurationMinutes { get; set; }
        public double RecommendedStrength { get; set; }
        public long RewardCoins { get; set; }
        public double FishFindChance { get; set; }
    }

    // ---------------------------------------------------------------- equipment.json

    /// <summary>Boats and baits: both only raise the Catch Success chance (docs/SISTEMA_SUCESSO_PESCA.md).</summary>
    public sealed class EquipmentConfig
    {
        public int ConfigSchemaVersion { get; set; }
        public List<BoatConfig> Boats { get; set; }
        public List<BaitConfig> Baits { get; set; }
    }

    public sealed class BoatConfig
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public int Tier { get; set; }
        public double CatchSuccessBonus { get; set; }
        public long CostCoins { get; set; }
        public long CostShells { get; set; }
        public int UnlockFisherLevel { get; set; } = 1;
    }

    public sealed class BaitConfig
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public int Tier { get; set; }
        public double CatchSuccessBonus { get; set; }

        /// <summary>Fishing attempts one purchase lasts; each attempt uses one, caught or not.</summary>
        public int Charges { get; set; }
        public long CostCoins { get; set; }
        public long CostShells { get; set; }
        public int UnlockFisherLevel { get; set; } = 1;
    }
}
