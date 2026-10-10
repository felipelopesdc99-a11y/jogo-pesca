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

        /// <summary>Expedition reward multiplier for a departure from this map (A-114); 0 or missing = 1.</summary>
        public double ExpeditionRewardMultiplier { get; set; }
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

        /// <summary>Name rules and the avatars the player can pick (M18-T03).</summary>
        public PlayerIdentityConfig PlayerIdentity { get; set; }
    }

    public sealed class PlayerIdentityConfig
    {
        public int NameMin { get; set; } = 3;
        public int NameMax { get; set; } = 16;
        public List<AvatarConfig> Avatars { get; set; } = new List<AvatarConfig>();
    }

    public sealed class AvatarConfig
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
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

        /// <summary>
        /// Raised whenever the XP table changes in a way that moves existing players (M24-T03, TD-038). A save made
        /// on an older curve has its total XP converted to the matching level on this one, never going down.
        /// </summary>
        public int XpCurveVersion { get; set; }
        public List<XpLevelConfig> XpTable { get; set; }

        /// <summary>Dólares earned by playing (A-111): a few at every level milestone. Null = none.</summary>
        public DollarsPerLevelsConfig DollarsPerLevels { get; set; }
    }

    /// <summary>A-111: <see cref="Dollars"/> Dólares each time the Fisher reaches a multiple of <see cref="EveryLevels"/>.</summary>
    public sealed class DollarsPerLevelsConfig
    {
        public int EveryLevels { get; set; }
        public long Dollars { get; set; }

        /// <summary>Last level that still gives Dólares (A-153: Nv.100 while OD-055 is open); 0 = every milestone.</summary>
        public int UpToLevel { get; set; }
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

        /// <summary>One short line for the Shop (A-103).</summary>
        public string Description { get; set; }
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

        /// <summary>Conchas asked on top of the coins (A-099).</summary>
        public long PurchaseCostShells { get; set; }
    }

    public sealed class RodUpgradeCostConfig
    {
        public int ToLevel { get; set; }
        public long CostCoins { get; set; }

        /// <summary>Conchas asked on top of the coins (A-099).</summary>
        public long CostShells { get; set; }
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
        public CurrencyTradeConfig CurrencyTrade { get; set; }
        public AuctionConfig Auction { get; set; }

        /// <summary>The VIP (A-110): bought with Dólares, boosts offline Fisher XP for a while.</summary>
        public VipConfig Vip { get; set; }
    }

    /// <summary>The VIP (A-110).</summary>
    public sealed class VipConfig
    {
        public long PriceDollars { get; set; }
        public double DurationDays { get; set; }

        /// <summary>Extra share of Fisher XP on offline catches while the VIP is active (0.5 = +50%).</summary>
        public double OfflineFisherXpBonus { get; set; }
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

    /// <summary>Selling Conchas and Dólares between players (A-101).</summary>
    public sealed class CurrencyTradeConfig
    {
        public int MaxActiveListingsPerPlayer { get; set; }
        public double CompletedSaleFeeRatio { get; set; }
        public long MinimumPriceCoins { get; set; }
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
        /// <summary>Most catches the box holds before fishing pauses (OD-025); 0 = no limit.</summary>
        public int Capacity { get; set; }
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

        /// <summary>How much of the ranking is shown, and in pages of how many (A-112).</summary>
        public ArenaRankingConfig Ranking { get; set; }
    }

    public sealed class ArenaRankingConfig
    {
        public int TopShown { get; set; }
        public int PageSize { get; set; }
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

        /// <summary>Honor lost when another player attacks you and wins (OD-024).</summary>
        public long DefenseDefeatLoss { get; set; }
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

        /// <summary>What the item gives (OD-009): Conchas or Dólares.</summary>
        public ArenaShopRewardConfig Reward { get; set; }

        /// <summary>Purchases allowed in any 7 days; 0 = no limit.</summary>
        public int WeeklyLimit { get; set; }
    }

    public sealed class ArenaShopRewardConfig
    {
        /// <summary>"shells" or "dollars".</summary>
        public string Currency { get; set; }
        public long Amount { get; set; }
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

        /// <summary>One short line for the Shop (A-103).</summary>
        public string Description { get; set; }
        public int Tier { get; set; }
        public double CatchSuccessBonus { get; set; }
        public long CostCoins { get; set; }
        public long CostShells { get; set; }
    }

    public sealed class BaitConfig
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }

        /// <summary>One short line for the Shop (A-103).</summary>
        public string Description { get; set; }
        public int Tier { get; set; }
        public double CatchSuccessBonus { get; set; }

        /// <summary>Fishing attempts one purchase lasts; each attempt uses one, caught or not.</summary>
        public int Charges { get; set; }
        public long CostCoins { get; set; }
        public long CostShells { get; set; }
    }

    // ---------------------------------------------------------------- crew.json

    /// <summary>
    /// The automatic Crew (M24-T05, A-154): hired fishers and boats that earn Moedas and Fisher XP per second, with
    /// the game open or closed. Never fish.
    /// </summary>
    public sealed class CrewConfig
    {
        public int ConfigSchemaVersion { get; set; }

        /// <summary>Each member after the first unlocks when the player has at least this many of the previous one.</summary>
        public int UnlockPreviousCount { get; set; }

        /// <summary>Time without a sync that still counts as the game open (full income, no welcome summary).</summary>
        public double OnlineGapSeconds { get; set; }

        public CrewOfflineConfig Offline { get; set; }

        /// <summary>Per member: reaching each count multiplies that member's Moedas.</summary>
        public CrewMilestonesConfig Milestones { get; set; }

        /// <summary>Fleet: every member with at least the count multiplies the whole Crew's Moedas.</summary>
        public CrewMilestonesConfig FleetMilestones { get; set; }

        /// <summary>In hiring order.</summary>
        public List<CrewMemberConfig> Members { get; set; }
    }

    public sealed class CrewOfflineConfig
    {
        /// <summary>Hours away at the full rate.</summary>
        public double FullRateHours { get; set; }

        /// <summary>Share of the income after <see cref="FullRateHours"/> (0.5 = 50%).</summary>
        public double ReducedRate { get; set; }

        /// <summary>Hours away that count at all; the rest earns nothing.</summary>
        public double MaxHours { get; set; }
    }

    public sealed class CrewMilestonesConfig
    {
        /// <summary>Ascending unit counts.</summary>
        public List<int> Counts { get; set; }

        /// <summary>Multiplier applied once per count reached (2 = doubles).</summary>
        public double Multiplier { get; set; }
    }

    public sealed class CrewMemberConfig
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }

        /// <summary>The plural ("Canoeiros"), for "Libera com 10 Canoeiros"; the display name when missing.</summary>
        public string DisplayNamePlural { get; set; }

        /// <summary>The plural, or the display name when the file has none.</summary>
        public string PluralName => string.IsNullOrWhiteSpace(DisplayNamePlural) ? DisplayName : DisplayNamePlural;

        /// <summary>Moedas of the first unit.</summary>
        public long BaseCost { get; set; }

        /// <summary>Each extra unit costs this much times the previous one (1.07 to 1.10).</summary>
        public double CostGrowth { get; set; }

        /// <summary>Moedas per second of one unit, before milestones.</summary>
        public double CoinsPerSecond { get; set; }

        /// <summary>Fisher XP per second of one unit (milestones do not change it).</summary>
        public double XpPerSecond { get; set; }
    }

    // ---------------------------------------------------------------- upgrades.json

    /// <summary>
    /// Upgrades bought with Moedas (M24-T06, A-155): one-off ×2s per Crew member and general upgrades with levels.
    /// None asks for a Fisher level.
    /// </summary>
    public sealed class UpgradesConfig
    {
        public int ConfigSchemaVersion { get; set; }

        public CrewUpgradesConfig CrewUpgrades { get; set; }

        /// <summary>The general upgrades, in the order the window lists them.</summary>
        public List<GeneralUpgradeConfig> GeneralUpgrades { get; set; }
    }

    /// <summary>One upgrade per Crew member and per tier: unlocked by that member's units, bought once, ×multiplier on its Moedas.</summary>
    public sealed class CrewUpgradesConfig
    {
        /// <summary>What each bought upgrade multiplies that member's Moedas by (2 = doubles).</summary>
        public double Multiplier { get; set; }

        public List<CrewUpgradeTierConfig> Tiers { get; set; }

        /// <summary>The end of each upgrade's name, by member id ("do Canoeiro").</summary>
        public Dictionary<string, string> MemberSuffixes { get; set; }
    }

    public sealed class CrewUpgradeTierConfig
    {
        /// <summary>Units of the member needed to buy it.</summary>
        public int UnlockCount { get; set; }

        /// <summary>The price is this times the price of that member's unit number <see cref="UnlockCount"/>.</summary>
        public double CostFactor { get; set; }

        /// <summary>The start of the name ("Anzol Afiado"); the member's suffix completes it.</summary>
        public string Name { get; set; }
    }

    public sealed class GeneralUpgradeConfig
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }

        /// <summary>What it does: one of <see cref="UpgradeEffects"/>.</summary>
        public string Effect { get; set; }

        /// <summary>What each level adds (0.25 = +25%; for the offline hours, hours).</summary>
        public double ValuePerLevel { get; set; }

        public int MaxLevel { get; set; }

        /// <summary>Moedas of level 1; level N costs base_cost × cost_growth^(N−1).</summary>
        public long BaseCost { get; set; }

        public double CostGrowth { get; set; }
    }

    /// <summary>The effect keys a general upgrade can have (upgrades.json → effect). Stored keys, never shown.</summary>
    public static class UpgradeEffects
    {
        /// <summary>+% on the whole Crew's Moedas.</summary>
        public const string CrewCoins = "crew_coins";

        /// <summary>+% on the NPC sale price of every fish (Fishing Box and Aquarium); the Market between players is untouched.</summary>
        public const string FishSale = "fish_sale";

        /// <summary>Hours added to the Crew's offline cap (at the reduced rate).</summary>
        public const string CrewOfflineHours = "crew_offline_hours";

        /// <summary>+% on the Crew's Fisher XP.</summary>
        public const string CrewXp = "crew_xp";

        /// <summary>+% on the Moedas of selling the Fishing Box (the fish just caught).</summary>
        public const string FishingCoins = "fishing_coins";

        public static readonly IReadOnlyList<string> All = new[] { CrewCoins, FishSale, CrewOfflineHours, CrewXp, FishingCoins };
    }
}
