using System.Collections.Generic;

namespace FishingIdle.GameService.Missions
{
    /// <summary>
    /// The objective types of missions.json (M24-T54, A-177): technical keys, stored in English and turned into PT-BR text by
    /// GameTexts.Missions. Each one reads either the save's state (level, Moedas, Encyclopedia, equipment, Cardume…) or a
    /// counter of <see cref="MissionCounterKeys"/> that the services add to when the event happens.
    /// </summary>
    public static class MissionTypes
    {
        // Fishing (counted at every catch put in the Fishing Box: online, offline, Expedition finds).
        public const string CatchCount = "catch_count";
        public const string CatchSpecies = "catch_species";
        public const string CatchRarity = "catch_rarity";
        public const string CatchSizeCategory = "catch_size_category";
        public const string CatchGeneStars = "catch_gene_stars";
        public const string CatchPerfectGene = "catch_perfect_gene";
        public const string CatchPerfectSpecimen = "catch_perfect_specimen";

        // The player's state.
        public const string FisherLevel = "fisher_level";
        public const string CoinsHeld = "coins_held";
        public const string ShellsHeld = "shells_held";
        public const string DollarsSpent = "dollars_spent";
        public const string BoxSell = "box_sell";

        // Aquarium and Cardume.
        public const string AquariumKeep = "aquarium_keep";
        public const string FishFeed = "fish_feed";
        public const string FishLevel = "fish_level";
        public const string CardumeSize = "cardume_size";
        public const string CardumeStrength = "cardume_strength";
        public const string CardumeAllRarityMin = "cardume_all_rarity_min";
        public const string CardumeGeneStars = "cardume_gene_stars";
        public const string CardumeAllLevel = "cardume_all_level";

        // Maps and Encyclopedia.
        public const string MapUnlocked = "map_unlocked";
        public const string EncyclopediaSpecies = "encyclopedia_species";
        public const string MapAllSpecies = "map_all_species";
        public const string RarityAllSpecies = "rarity_all_species";
        public const string SizeRecords = "size_records";

        // Equipment.
        public const string RodAssembled = "rod_assembled";
        public const string BoatAssembled = "boat_assembled";
        public const string BoatsAll = "boats_all";
        public const string ClothingAssembled = "clothing_assembled";
        public const string ClothingMaxLevel = "clothing_max_level";
        public const string BaitUsed = "bait_used";
        public const string PassRewardsClaimed = "pass_rewards_claimed";

        // Expedition and Arena.
        public const string ExpeditionStarted = "expedition_started";
        public const string ExpeditionsCompleted = "expeditions_completed";
        public const string ArenaDuels = "arena_duels";
        public const string ArenaWins = "arena_wins";
        public const string ArenaWinStreak = "arena_win_streak";
        public const string ArenaDistinctOpponents = "arena_distinct_opponents";
        public const string ArenaWinStronger = "arena_win_stronger";
        public const string ArenaRankTop = "arena_rank_top";

        // Market and Auctions.
        public const string MarketListing = "market_listing";
        public const string MarketBuy = "market_buy";
        public const string MarketSell = "market_sell";
        public const string AuctionBid = "auction_bid";
        public const string AuctionWon = "auction_won";

        // Boss Mundial and Clãs.
        public const string BossAttack = "boss_attack";
        public const string BossBand = "boss_band";
        public const string BossDays = "boss_days";
        public const string ClanJoin = "clan_join";
        public const string ClanFish = "clan_fish";
        public const string ClanSameDayBoss = "clan_same_day_boss";
        public const string ClanRankTop = "clan_rank_top";

        // Profile.
        public const string ProfileVisit = "profile_visit";

        /// <summary>Every type the game knows (the validator refuses any other).</summary>
        public static readonly IReadOnlyCollection<string> All = new HashSet<string>
        {
            CatchCount, CatchSpecies, CatchRarity, CatchSizeCategory, CatchGeneStars, CatchPerfectGene, CatchPerfectSpecimen,
            FisherLevel, CoinsHeld, ShellsHeld, DollarsSpent, BoxSell,
            AquariumKeep, FishFeed, FishLevel, CardumeSize, CardumeStrength, CardumeAllRarityMin, CardumeGeneStars, CardumeAllLevel,
            MapUnlocked, EncyclopediaSpecies, MapAllSpecies, RarityAllSpecies, SizeRecords,
            RodAssembled, BoatAssembled, BoatsAll, ClothingAssembled, ClothingMaxLevel, BaitUsed, PassRewardsClaimed,
            ExpeditionStarted, ExpeditionsCompleted, ArenaDuels, ArenaWins, ArenaWinStreak, ArenaDistinctOpponents, ArenaWinStronger, ArenaRankTop,
            MarketListing, MarketBuy, MarketSell, AuctionBid, AuctionWon,
            BossAttack, BossBand, BossDays, ClanJoin, ClanFish, ClanSameDayBoss, ClanRankTop,
            ProfileVisit,
        };

        /// <summary>Types whose goal is computed from the config ("all of them"): the mission's target is not used.</summary>
        public static readonly IReadOnlyCollection<string> ComputedTarget = new HashSet<string>
        {
            MapAllSpecies, RarityAllSpecies, BoatsAll, CardumeAllRarityMin, CardumeAllLevel,
        };

        /// <summary>Types where target 0 means "all" (all species, all clothing pieces) or "the top band" (boss_band).</summary>
        public static readonly IReadOnlyCollection<string> ZeroMeansAll = new HashSet<string>
        {
            EncyclopediaSpecies, ClothingAssembled, ClothingMaxLevel, BossBand,
        };

        /// <summary>Types that are done or not (a position in a ranking, a map, an item): the screen shows 0 / 1 or 1 / 1.</summary>
        public static readonly IReadOnlyCollection<string> YesNo = new HashSet<string>
        {
            ArenaRankTop, ClanRankTop, MapUnlocked, RodAssembled, BoatAssembled, ClanJoin, ClanFish,
        };
    }

    /// <summary>
    /// The keys of the event counters kept in the save (PlayerSave.Missions.Counters). Technical, never shown. A key with a
    /// ":" carries a parameter: "catch_size:large", "bait:bait_01".
    /// </summary>
    public static class MissionCounterKeys
    {
        public const string CatchSizePrefix = "catch_size:";
        public const string CatchStarsPrefix = "catch_stars:";
        public const string CatchPerfectGene = "catch_perfect_gene";
        public const string CatchPerfectSpecimen = "catch_perfect_specimen";
        public const string BoxSold = "box_sold";
        public const string FishFed = "fish_fed";
        public const string BaitPrefix = "bait:";
        public const string DollarsSpent = "dollars_spent";
        public const string ExpeditionsCompleted = "expeditions_completed";
        public const string ArenaDuels = "arena_duels";
        public const string ArenaWins = "arena_wins";
        public const string ArenaStreak = "arena_streak";
        public const string ArenaBestStreak = "arena_best_streak";
        public const string ArenaStrongerWins = "arena_stronger_wins";
        public const string ArenaBestRank = "arena_best_rank";
        public const string MarketListings = "market_listings";
        public const string MarketBuys = "market_buys";
        public const string MarketBuysFish = "market_buys:fish";
        public const string MarketSoldFish = "market_sold_fish";
        public const string MarketSoldFishRarityPrefix = "market_sold_fish:";
        public const string AuctionBids = "auction_bids";
        public const string AuctionsWon = "auctions_won";
        public const string AuctionsWonContested = "auctions_won_contested";
        public const string BossAttacks = "boss_attacks";
        public const string BossDays = "boss_days";
        public const string BossLastDay = "boss_last_day";
        public const string BossBestBand = "boss_best_band";
        public const string ProfileVisits = "profile_visits";
    }
}
