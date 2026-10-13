using System.Collections.Generic;
using Newtonsoft.Json;

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

        /// <summary>Numbers the level curve (1 = Nv.1 to Nv.10 with +4% a level; 2 = Nv.1 to Nv.100, M24-T13).</summary>
        public int LevelCurveVersion { get; set; }

        /// <summary>Attribute bonus at the max level, in percent (0 at Nv.1); the curve in between is <see cref="LevelCurve"/>.</summary>
        public double StatBonusAtMaxLevelPercent { get; set; }
        public double StatBonusCurveExponent { get; set; } = 1.0;

        /// <summary>The level on the current curve of a fish that was at level 1–10 of curve 1 (index 0 = old Nv.1).</summary>
        public List<int> LegacyLevelsV1 { get; set; }
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

        // The Dólares of the level milestones (A-111) moved to the free track of the Passe do Pescador (pass.json,
        // M24-T17, A-158): the old "dollars_per_levels" block is no longer read.
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
        /// Multiplies the chance that a fish of this rarity bites while fishing (online and offline), after the
        /// map's weights and the rod's rarity bonus (M24-T22, A-159): 0,35 = 35% of the chance it would have.
        /// Commons fill the rest, so the chance per fish of every other rarity is exactly this share of the
        /// plain draw. Ignored for the common rarity; Expeditions and the Market's sellers use the plain draw. 1 when not set.
        /// </summary>
        public double FishingBiteMultiplier { get; set; } = 1.0;

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
        public RodUpgradeRulesConfig UpgradeRules { get; set; }

        /// <summary>How rods come from fragments (M24-T32, A-163). Null = the defaults (offline fishing drops them too).</summary>
        public RodFragmentRulesConfig FragmentRules { get; set; }
        public List<RodConfig> Rods { get; set; }
    }

    public sealed class RodFragmentRulesConfig
    {
        /// <summary>Whether offline fishing (A-034) also drops rod fragments, at the same chance. Expeditions never do.</summary>
        public bool DropOnOfflineFishing { get; set; } = true;
    }

    /// <summary>The level curve every rod with levels shares (M24-T13).</summary>
    public sealed class RodUpgradeRulesConfig
    {
        public IntRangeConfig InternalLevels { get; set; }
        public double BonusCurveExponent { get; set; } = 1.0;

        /// <summary>Numbers the level curve (1 = Nv.1 to Nv.10 from tables; 2 = Nv.1 to Nv.100 on a curve).</summary>
        public int LevelCurveVersion { get; set; }
    }

    /// <summary>
    /// Price of each level of a rod or boat (M24-T13): from level N to N + 1 costs
    /// first × growth^(N − 1), rounded by <see cref="LevelCurve.Cost"/>, in Moedas and in Conchas.
    /// </summary>
    public sealed class LevelCostConfig
    {
        public long CoinsFirst { get; set; }
        public double CoinsGrowth { get; set; } = 1.0;
        public long ShellsFirst { get; set; }
        public double ShellsGrowth { get; set; } = 1.0;
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

        /// <summary>
        /// Rods by fragments (M24-T32, A-163): the map whose catches drop this rod's fragments (null for the starter rod),
        /// how many assemble it and the chance per fish pulled out there. The map never asks for this rod's tier, so the
        /// rod that opens the next map always drops on a map the player already fishes.
        /// </summary>
        public string FragmentMapId { get; set; }
        public int FragmentsRequired { get; set; }
        public double FragmentDropChance { get; set; }

        /// <summary>Price of each level, for rods with levels (M24-T13).</summary>
        public LevelCostConfig UpgradeCost { get; set; }
        public RodResaleConfig NpcResale { get; set; }
        public long NpcResaleValueCoins { get; set; }
        public bool TradableOnMarket { get; set; }

        /// <summary>Flat bonuses, for rods without internal levels (the Starter Rod).</summary>
        public RodBonusesConfig Bonuses { get; set; }

        /// <summary>Total bonuses at Nv.1 and at the max level, for rods with levels; the curve in between is <see cref="LevelCurve"/>.</summary>
        /// <remarks>Named explicitly: the snake_case strategy does not split digits (it would read "bonuses_at_level1").</remarks>
        [JsonProperty("bonuses_at_level_1")]
        public RodBonusesConfig BonusesAtLevel1 { get; set; }
        public RodBonusesConfig BonusesAtMaxLevel { get; set; }

        /// <summary>The level on the current curve of a rod that was at level 1–10 of curve 1 (index 0 = old Nv.1).</summary>
        public List<int> LegacyLevelsV1 { get; set; }
    }

    public sealed class RodAcquisitionConfig
    {
        public const string Fragments = "fragments";
        public const string FreeClaim = "free_claim_in_shop";

        /// <summary>"fragments" rods are assembled from fragments (M24-T32); "free_claim_in_shop" is the Starter Rod.</summary>
        public string Method { get; set; }

        /// <summary>The rod's reference value on the Market and for the NPC resale (the price it had in the Shop before M24-T32).</summary>
        public long MarketReferenceCoins { get; set; }
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

        /// <summary>The prices of the removed Crew and Upgrades, only to refund old saves (M24-T46, A-172).</summary>
        public CrewRemovalRefundConfig CrewRemovalRefund { get; set; }
    }

    /// <summary>The VIP (A-110).</summary>
    public sealed class VipConfig
    {
        public long PriceDollars { get; set; }
        public double DurationDays { get; set; }

        /// <summary>Extra share of Fisher XP on offline catches while the VIP is active (0.5 = +50%).</summary>
        public double OfflineFisherXpBonus { get; set; }

        /// <summary>
        /// Seconds per offline fishing attempt while the VIP is active (M24-T27, A-160), in place of
        /// fishing.offline_cycle_seconds; 0 (or absent) = the VIP does not change the offline cycle.
        /// </summary>
        public double OfflineCycleSeconds { get; set; }
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

        /// <summary>Highest level of a rod the traders list (M24-T13; levels go to 100).</summary>
        public int MaxRodLevel { get; set; } = 1;
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

        /// <summary>The level curve every boat with levels shares (M24-T13).</summary>
        public BoatLevelsConfig BoatLevels { get; set; }

        /// <summary>How boats come from fragments (M24-T16, A-157). Null = the defaults (offline fishing drops them too).</summary>
        public BoatFragmentsConfig BoatFragments { get; set; }
        public List<BoatConfig> Boats { get; set; }
        public List<BaitConfig> Baits { get; set; }

        /// <summary>The baits before M24-T30 (A-161) and how a save that still has them moves to today's. Null = nothing to move.</summary>
        public BaitMigrationConfig BaitMigration { get; set; }

        /// <summary>The boats removed in M24-T42 (A-168) and how a save that has them moves to today's. Null = nothing to move.</summary>
        public BoatMigrationConfig BoatMigration { get; set; }

        /// <summary>The clothing pieces and the Fragmento de Roupa (M24-T33, A-164). Null = no clothing.</summary>
        public ClothingConfig Clothing { get; set; }
    }

    /// <summary>
    /// Clothing (M24-T33, A-164): five pieces, one of each, assembled with one kind of fragment that drops on every map.
    /// Each piece adds a bonus on one axis and levels up with use (fish pulled out while it is worn), at no cost.
    /// </summary>
    public sealed class ClothingConfig
    {
        /// <summary>Chance that a fish pulled out on any map drops 1 Fragmento de Roupa (0 to 1).</summary>
        public double FragmentDropChance { get; set; }
        public bool DropOnOfflineFishing { get; set; } = true;

        /// <summary>Fragments to assemble the 1st, 2nd … piece, whichever the player picks; past the list, the last value.</summary>
        public List<int> FragmentsToAssemble { get; set; }
        public int MaxLevel { get; set; } = 1;
        public double BonusCurveExponent { get; set; } = 1.0;

        /// <summary>Use XP every assembled piece gets per fish pulled out.</summary>
        public long UseXpPerFish { get; set; } = 1;

        /// <summary>Use XP from level N to N + 1: first × growth^(N − 1), rounded like the prices (LevelCurve.Cost).</summary>
        public ClothingLevelXpConfig LevelXp { get; set; }
        public List<ClothingPieceConfig> Pieces { get; set; }
    }

    public sealed class ClothingLevelXpConfig
    {
        public long First { get; set; }
        public double Growth { get; set; } = 1.0;
    }

    public sealed class ClothingPieceConfig
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }

        /// <summary>One of <see cref="ClothingAxes"/>.</summary>
        public string BonusAxis { get; set; }

        [JsonProperty("bonus_at_level_1")]
        public double BonusAtLevel1 { get; set; }
        public double BonusAtMaxLevel { get; set; }
    }

    /// <summary>The axes a clothing piece can add to (equipment.json → clothing → pieces → bonus_axis).</summary>
    public static class ClothingAxes
    {
        /// <summary>Points on the Catch Success chance, like the rod, boat and bait (up to the 95% ceiling).</summary>
        public const string CatchSuccess = "catch_success";

        /// <summary>Added to the rod's rarity efficiency, size quality and Shell yield (rods.json → bonus_axes).</summary>
        public const string RarityEfficiency = "rarity_efficiency";
        public const string SizeQuality = "size_quality";
        public const string ShellYield = "shell_yield";

        /// <summary>+% Fisher XP on every fish pulled out.</summary>
        public const string FisherXp = "fisher_xp";

        public static readonly IReadOnlyList<string> All = new[] { CatchSuccess, RarityEfficiency, SizeQuality, ShellYield, FisherXp };
    }

    /// <summary>
    /// M24-T30 (A-161): the bait model version of today's baits and the baits of the version before, kept only to convert
    /// the charges a save still holds (Persistence.BaitModelMigration).
    /// </summary>
    public sealed class BaitMigrationConfig
    {
        public int BaitModelVersion { get; set; } = 1;
        public List<BaitConfig> LegacyBaits { get; set; }
    }

    public sealed class BoatFragmentsConfig
    {
        /// <summary>Whether offline fishing (A-034) also drops fragments, at the same chance. Expeditions never do.</summary>
        public bool DropOnOfflineFishing { get; set; } = true;
    }

    public sealed class BoatLevelsConfig
    {
        public int MaxLevel { get; set; } = 1;
        public double BonusCurveExponent { get; set; } = 1.0;
    }

    public sealed class BoatConfig
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }

        /// <summary>One short line for the Shop (A-103).</summary>
        public string Description { get; set; }
        public int Tier { get; set; }

        /// <summary>Points added to the Catch Success chance; for a boat with levels, the bonus at Nv.1.</summary>
        public double CatchSuccessBonus { get; set; }

        /// <summary>Whether the boat levels up (M24-T13). The starter boat does not.</summary>
        public bool HasLevels { get; set; }
        public double CatchSuccessBonusAtMaxLevel { get; set; }
        public LevelCostConfig UpgradeCost { get; set; }

        /// <summary>
        /// The map whose catches drop this boat's fragments (M24-T16, A-157); null for the starter boat. Boats are not
        /// sold: <see cref="FragmentsRequired"/> fragments assemble one.
        /// </summary>
        public string MapId { get; set; }
        public int FragmentsRequired { get; set; }

        /// <summary>Chance that a fish pulled out on <see cref="MapId"/> drops 1 fragment of this boat (0 to 1).</summary>
        public double FragmentDropChance { get; set; }

        /// <summary>
        /// Other maps whose catches also drop this boat's fragments, each with its own chance (M24-T42, A-168: back to 5
        /// boats, maps 6 to 10 drop boats 1 to 5). Null or empty = only <see cref="MapId"/>.
        /// </summary>
        public List<BoatFragmentMapConfig> ExtraFragmentMaps { get; set; }
    }

    public sealed class BoatFragmentMapConfig
    {
        public string MapId { get; set; }
        public double FragmentDropChance { get; set; }
    }

    /// <summary>
    /// The boats removed on 12/10/2026 (M24-T42, A-168), kept to convert old saves: their fragments go to another boat and,
    /// when one was assembled, the Coins and Shells its levels cost come back.
    /// </summary>
    public sealed class BoatMigrationConfig
    {
        public int BoatModelVersion { get; set; } = 1;
        public List<RemovedBoatConfig> RemovedBoats { get; set; }
    }

    public sealed class RemovedBoatConfig
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }

        /// <summary>The boat whose fragments this one's become (the boat its map drops now).</summary>
        public string FragmentsGoTo { get; set; }

        /// <summary>Fragments it took to assemble it; given back as fragments of <see cref="FragmentsGoTo"/> when it was assembled.</summary>
        public int FragmentsRequired { get; set; }
        public LevelCostConfig UpgradeCost { get; set; }
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

    // ---------------------------------------------------------------- economy.json → crew_removal_refund

    /// <summary>
    /// The prices of the Crew and the Upgrades removed in M24-T46 (A-172, TD-053), kept only to give back what a save spent
    /// on them (<see cref="Persistence.CrewRemovalMigration"/>). Nothing else in the game reads them.
    /// </summary>
    public sealed class CrewRemovalRefundConfig
    {
        /// <summary>The Crew members (crew.json before A-172): id, price of the first unit and growth per unit.</summary>
        public List<RetiredCrewMemberConfig> CrewMembers { get; set; }

        /// <summary>The Crew members' upgrades (upgrades.json → crew_upgrades.tiers before A-172), in tier order.</summary>
        public List<RetiredCrewUpgradeTierConfig> CrewUpgradeTiers { get; set; }

        /// <summary>The general upgrades (upgrades.json → general_upgrades before A-172).</summary>
        public List<RetiredGeneralUpgradeConfig> GeneralUpgrades { get; set; }
    }

    public sealed class RetiredCrewMemberConfig
    {
        public string Id { get; set; }

        /// <summary>Moedas of the first unit.</summary>
        public double BaseCost { get; set; }

        /// <summary>Each extra unit cost this much times the previous one.</summary>
        public double CostGrowth { get; set; }
    }

    public sealed class RetiredCrewUpgradeTierConfig
    {
        /// <summary>Units of the member it asked for; its price was cost_factor × base_cost × cost_growth^unlock_count.</summary>
        public int UnlockCount { get; set; }

        public double CostFactor { get; set; }
    }

    public sealed class RetiredGeneralUpgradeConfig
    {
        public string Id { get; set; }

        /// <summary>Level N cost base_cost × cost_growth^(N−1).</summary>
        public double BaseCost { get; set; }

        public double CostGrowth { get; set; }

        public int MaxLevel { get; set; }
    }

    // ---------------------------------------------------------------- pass.json

    /// <summary>
    /// The Passe do Pescador (M24-T17, A-158): one permanent pass with a milestone every few Fisher levels, each with a
    /// free and a premium reward. The premium is bought once with Dólares.
    /// </summary>
    public sealed class PassConfig
    {
        public int ConfigSchemaVersion { get; set; }

        /// <summary>Which pass the save's progress belongs to. Seasons are the owner's call (OD-059); today there is one.</summary>
        public string PassId { get; set; }
        public long PremiumPriceDollars { get; set; }

        /// <summary>In ascending level order.</summary>
        public List<PassMilestoneConfig> Milestones { get; set; }
    }

    public sealed class PassMilestoneConfig
    {
        /// <summary>The Fisher level that reaches it.</summary>
        public int Level { get; set; }
        public PassRewardConfig Free { get; set; }
        public PassRewardConfig Premium { get; set; }
    }

    /// <summary>One reward: any of Moedas, Conchas, Dólares, fragments of one boat, of one rod and of clothing (zero = none of that).</summary>
    public sealed class PassRewardConfig
    {
        public long Coins { get; set; }
        public long Shells { get; set; }
        public long Dollars { get; set; }

        /// <summary>The boat whose fragments it gives (equipment.json), or null.</summary>
        public string BoatId { get; set; }
        public int Fragments { get; set; }

        /// <summary>The rod whose fragments it gives (rods.json, M24-T32), or null.</summary>
        public string RodId { get; set; }
        public int RodFragments { get; set; }

        /// <summary>Fragmentos de Roupa (M24-T33).</summary>
        public int ClothingFragments { get; set; }
    }

    /// <summary>
    /// The Boss Mundial (world_boss.json, M24-T29, A-165): a daily window (Brasília time) in which the Cardume attacks a
    /// boss with infinite health; the day's damage gives a reward band.
    /// </summary>
    public sealed class WorldBossConfig
    {
        public int ConfigSchemaVersion { get; set; }

        /// <summary>The boss's name (a suggestion until the owner decides, OD-069).</summary>
        public string DisplayName { get; set; }
        public WorldBossScheduleConfig Schedule { get; set; }
        public WorldBossAttackConfig Attack { get; set; }

        /// <summary>In ascending damage_percent order; the player gets the highest band reached.</summary>
        public List<WorldBossBandConfig> Bands { get; set; }
        public WorldBossRankingConfig Ranking { get; set; }

        /// <summary>In ascending level order, the first at level 1; the row of the highest level not above the Fisher's applies.</summary>
        public List<WorldBossIncomeConfig> ReferenceIncome { get; set; }
    }

    public sealed class WorldBossScheduleConfig
    {
        /// <summary>The event's fixed time zone (−180 = Brasília, UTC−3).</summary>
        public int UtcOffsetMinutes { get; set; }

        /// <summary>Minute of that day the window opens (1140 = 19:00).</summary>
        public int StartMinuteOfDay { get; set; }
        public int DurationMinutes { get; set; }
    }

    public sealed class WorldBossAttackConfig
    {
        public double IntervalSeconds { get; set; }

        /// <summary>Late attacks credited at once after a short freeze; the time with the game closed never counts.</summary>
        public int MaxCatchUpAttacks { get; set; }

        /// <summary>The boss's Defense in the Arena damage rule (0 = no reduction).</summary>
        public double BossDefense { get; set; }
    }

    public sealed class WorldBossBandConfig
    {
        /// <summary>Percent of the day's maximum damage (the strongest Cardume of the day over the whole window).</summary>
        public double DamagePercent { get; set; }

        /// <summary>Minutes of the reference income paid in Moedas / Conchas.</summary>
        public double IncomeMinutes { get; set; }
        public double ShellMinutes { get; set; }
        public int ClothingFragments { get; set; }

        /// <summary>Fragments of the boat of the highest map the Fisher level unlocks.</summary>
        public int BoatFragments { get; set; }
    }

    public sealed class WorldBossRankingConfig
    {
        /// <summary>Dólares of the 1st to the Nth of the day.</summary>
        public List<long> DollarsByRank { get; set; }

        /// <summary>Off in the local game (the player is alone in the ranking); on only with a server and real players.</summary>
        public bool OnlineRewardsEnabled { get; set; }
    }

    public sealed class WorldBossIncomeConfig
    {
        public int Level { get; set; }
        public long CoinsPerHour { get; set; }
        public long ShellsPerHour { get; set; }
    }
}
