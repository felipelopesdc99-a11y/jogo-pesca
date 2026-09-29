using System.Collections.Generic;
using FishingIdle.GameService.Aquarium;

namespace FishingIdle.GameService.Market
{
    /// <summary>A rod on the Market: what it is and what it does, never the owner's Inventory id.</summary>
    public sealed class MarketRodView
    {
        public string RodId { get; internal set; }
        public string Name { get; internal set; }
        public int Tier { get; internal set; }
        public bool HasLevels { get; internal set; }
        public int Level { get; internal set; }
        public int MaxLevel { get; internal set; }
        public double RarityBonus { get; internal set; }
        public double SizeBonus { get; internal set; }
        public double ShellBonus { get; internal set; }
    }

    /// <summary>A fish or an item as a Market card shows it.</summary>
    public sealed class GoodsView
    {
        public bool IsFish { get; internal set; }
        public string Name { get; internal set; }

        /// <summary>Filled for fish.</summary>
        public FishView Fish { get; internal set; }

        /// <summary>Filled for rods.</summary>
        public MarketRodView Rod { get; internal set; }

        /// <summary>What the game (NPC) would pay for it.</summary>
        public long NpcValueCoins { get; internal set; }

        /// <summary>What the simulated market considers a fair price (local MVP only).</summary>
        public long ReferenceCoins { get; internal set; }
    }

    public sealed class ListingView
    {
        public long ListingId { get; internal set; }
        public bool IsMine { get; internal set; }
        public string SellerName { get; internal set; }
        public long PriceCoins { get; internal set; }
        public long ListedAtMs { get; internal set; }
        public long ExpiresAtMs { get; internal set; }
        public double RemainingSeconds { get; internal set; }
        public GoodsView Goods { get; internal set; }

        /// <summary>For the player's own listings: fee and what arrives if it sells.</summary>
        public long FeeCoins { get; internal set; }
        public long NetCoins { get; internal set; }
    }

    public sealed class WithdrawalView
    {
        public long WithdrawalId { get; internal set; }

        /// <summary>"bought", "cancelled" or "expired".</summary>
        public string Reason { get; internal set; }

        public long AtMs { get; internal set; }
        public GoodsView Goods { get; internal set; }

        /// <summary>Why it cannot be withdrawn right now (a full Aquarium); None when it can.</summary>
        public Core.ServiceError Blocker { get; internal set; }
    }

    public sealed class MarketEventView
    {
        public long AtMs { get; internal set; }

        /// <summary>MarketEvent kind: sold, expired, auction_sold, auction_unsold, auction_won, auction_lost, outbid.</summary>
        public string Kind { get; internal set; }

        public bool Sold { get; internal set; }
        public string GoodsName { get; internal set; }
        public long PriceCoins { get; internal set; }
        public long FeeCoins { get; internal set; }
        public long NetCoins => PriceCoins - FeeCoins;
        public string BuyerName { get; internal set; }
    }

    /// <summary>Something the player owns that could be listed, and whether it can be now.</summary>
    public sealed class SellCandidateView
    {
        public bool IsFish { get; internal set; }

        /// <summary>Aquarium fish id or Inventory item id.</summary>
        public long SourceId { get; internal set; }

        public GoodsView Goods { get; internal set; }
        public Core.ServiceError Blocker { get; internal set; }
    }

    public sealed class MarketView
    {
        public long Coins { get; internal set; }
        public int MaxListings { get; internal set; }
        public double ListingDurationDays { get; internal set; }
        public double SaleFeeRatio { get; internal set; }
        public long MinimumPriceCoins { get; internal set; }
        public int AquariumCount { get; internal set; }
        public int AquariumCapacity { get; internal set; }
        public List<ListingView> MyListings { get; } = new List<ListingView>();
        public List<WithdrawalView> Withdrawals { get; } = new List<WithdrawalView>();
        public List<MarketEventView> Events { get; } = new List<MarketEventView>();
    }

    public sealed class FilterOption
    {
        public string Id { get; internal set; }
        public string Name { get; internal set; }
    }

    /// <summary>The values the Buy filters can take, in display order.</summary>
    public sealed class MarketFilterOptions
    {
        public List<FilterOption> Species { get; } = new List<FilterOption>();
        public List<FilterOption> Rarities { get; } = new List<FilterOption>();
        public List<FilterOption> SizeCategories { get; } = new List<FilterOption>();
    }

    public enum MarketKindFilter
    {
        All,
        Fish,
        Rods,
    }

    public enum MarketSort
    {
        PriceAscending,
        PriceDescending,
        SizeDescending,
        SizeAscending,
        Newest,
    }

    /// <summary>Buy-tab search: every filter is optional and they combine (GDD section 34).</summary>
    public sealed class MarketQuery
    {
        public MarketKindFilter Kind { get; set; }
        public string SpeciesId { get; set; }
        public string RarityId { get; set; }
        public string SizeCategoryId { get; set; }
        public double? MinSizeCm { get; set; }
        public double? MaxSizeCm { get; set; }
        public int? MinLevel { get; set; }
        public int? MaxLevel { get; set; }
        public long? MinPrice { get; set; }
        public long? MaxPrice { get; set; }
        public MarketSort Sort { get; set; }
    }

    public sealed class MarketPurchase
    {
        public WithdrawalView Item { get; internal set; }
        public long PricePaid { get; internal set; }
        public long NewBalance { get; internal set; }
    }

    public sealed class WithdrawAllResult
    {
        public int Withdrawn { get; internal set; }

        /// <summary>Fish that stayed because the Aquarium filled up.</summary>
        public int LeftBehind { get; internal set; }
    }

    /// <summary>An auction card (GDD section 35).</summary>
    public sealed class AuctionView
    {
        public long AuctionId { get; internal set; }
        public bool IsMine { get; internal set; }
        public string SellerName { get; internal set; }
        public GoodsView Goods { get; internal set; }
        public long StartingBidCoins { get; internal set; }

        /// <summary>0 while nobody has bid.</summary>
        public long HighestBidCoins { get; internal set; }

        public string HighestBidderName { get; internal set; }
        public bool PlayerIsHighest { get; internal set; }
        public bool PlayerHasBid { get; internal set; }
        public int BidCount { get; internal set; }

        /// <summary>The smallest valid next bid and the 1% fee it would cost.</summary>
        public long MinNextBidCoins { get; internal set; }
        public long MinNextBidFeeCoins { get; internal set; }

        public long EndsAtMs { get; internal set; }
        public double RemainingSeconds { get; internal set; }

        /// <summary>The seller can end it now only when there is a bid.</summary>
        public bool CanEndNow { get; internal set; }

        /// <summary>What the seller would receive ending now (bid − 3%).</summary>
        public long EndNowNetCoins { get; internal set; }
    }

    public sealed class AuctionsView
    {
        public long Coins { get; internal set; }

        /// <summary>Coins locked in the player's winning bids.</summary>
        public long ReservedCoins { get; internal set; }

        public int MaxActive { get; internal set; }
        public double DurationHours { get; internal set; }
        public double MinIncrementRatio { get; internal set; }
        public double BidFeeRatio { get; internal set; }
        public double EarlyCloseFeeRatio { get; internal set; }

        /// <summary>The player's own active auction, or null.</summary>
        public AuctionView Mine { get; internal set; }

        /// <summary>Other sellers' auctions, ending soonest first.</summary>
        public List<AuctionView> Open { get; } = new List<AuctionView>();
    }

    public sealed class BidResult
    {
        public AuctionView Auction { get; internal set; }
        public long FeeCoins { get; internal set; }
        public long NewBalance { get; internal set; }
    }
}
