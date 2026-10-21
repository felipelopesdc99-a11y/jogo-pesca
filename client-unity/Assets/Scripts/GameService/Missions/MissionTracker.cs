using System.Collections.Generic;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;
using FishingIdle.GameService.Persistence;

namespace FishingIdle.GameService.Missions
{
    /// <summary>
    /// The hooks the services call when something a mission counts happens (M24-T54, A-177): a catch, a sale, a duel, a bid…
    /// They only add to the counters in the save (PlayerSave.Missions); whether a mission is complete is decided by
    /// <see cref="MissionRules"/> when the session writes the save. Every addition goes through SafeMath, so a counter
    /// never wraps around.
    /// </summary>
    internal static class MissionTracker
    {
        /// <summary>How many distinct Arena opponents are remembered (the largest goal is far below it).</summary>
        internal const int ArenaOpponentsCap = 1000;

        internal static void Add(PlayerSave save, string key, long amount = 1)
        {
            if (save?.Missions == null || amount <= 0 || string.IsNullOrEmpty(key))
            {
                return;
            }

            var counters = save.Missions.Counters ?? (save.Missions.Counters = new Dictionary<string, long>());
            counters.TryGetValue(key, out var have);
            counters[key] = SafeMath.Add(have, amount);
        }

        internal static long Get(PlayerSave save, string key)
        {
            return save?.Missions?.Counters != null && save.Missions.Counters.TryGetValue(key, out var value) ? value : 0;
        }

        private static void Max(PlayerSave save, string key, long value)
        {
            if (save?.Missions?.Counters == null)
            {
                return;
            }

            if (Get(save, key) < value)
            {
                save.Missions.Counters[key] = value;
            }
        }

        /// <summary>
        /// A catch put in the Fishing Box (online, offline, an Expedition find): its size category, its gene stars, a
        /// perfect gene, a perfect specimen and a beaten size record. Species and rarity counts come from the Encyclopedia.
        /// </summary>
        internal static void OnCatch(GameConfig config, PlayerSave save, BoxCatch entry, bool recordBeaten)
        {
            if (entry == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(entry.SizeCategoryId))
            {
                Add(save, MissionCounterKeys.CatchSizePrefix + entry.SizeCategoryId);
            }

            if (GeneRules.IsValid(config, entry.Genes))
            {
                var genes = GeneRules.View(config, entry.Genes);
                Add(save, MissionCounterKeys.CatchStarsPrefix + genes.Stars);
                if (genes.PerfectCount > 0)
                {
                    Add(save, MissionCounterKeys.CatchPerfectGene);
                }

                if (genes.PerfectCount == entry.Genes.Length)
                {
                    Add(save, MissionCounterKeys.CatchPerfectSpecimen);
                }
            }

            if (recordBeaten && save?.Missions?.RecordSpecies != null && !save.Missions.RecordSpecies.Contains(entry.SpeciesId))
            {
                save.Missions.RecordSpecies.Add(entry.SpeciesId);
            }
        }

        internal static void OnBoxSold(PlayerSave save, long count) => Add(save, MissionCounterKeys.BoxSold, count);

        internal static void OnFed(PlayerSave save) => Add(save, MissionCounterKeys.FishFed);

        internal static void OnBaitUsed(PlayerSave save, string baitId) => Add(save, MissionCounterKeys.BaitPrefix + baitId);

        internal static void OnDollarsSpent(PlayerSave save, long amount) => Add(save, MissionCounterKeys.DollarsSpent, amount);

        internal static void OnExpeditionCompleted(PlayerSave save) => Add(save, MissionCounterKeys.ExpeditionsCompleted);

        /// <summary>
        /// An attack in the Arena. A win against someone ranked above the player (the swap moved the player up) counts as
        /// a win against a stronger player: the rank is the strength the Arena shows (A-177).
        /// </summary>
        internal static void OnArenaAttack(PlayerSave save, string opponentId, bool won, int rankBefore, int rankAfter)
        {
            Add(save, MissionCounterKeys.ArenaDuels);
            if (won)
            {
                Add(save, MissionCounterKeys.ArenaWins);
                Add(save, MissionCounterKeys.ArenaStreak);
                Max(save, MissionCounterKeys.ArenaBestStreak, Get(save, MissionCounterKeys.ArenaStreak));
                if (rankAfter < rankBefore)
                {
                    Add(save, MissionCounterKeys.ArenaStrongerWins);
                }
            }
            else if (save?.Missions?.Counters != null)
            {
                save.Missions.Counters[MissionCounterKeys.ArenaStreak] = 0;
            }

            var opponents = save?.Missions?.ArenaOpponents;
            if (opponents != null && !string.IsNullOrEmpty(opponentId) && opponents.Count < ArenaOpponentsCap && !opponents.Contains(opponentId))
            {
                opponents.Add(opponentId);
            }
        }

        internal static void OnMarketListing(PlayerSave save) => Add(save, MissionCounterKeys.MarketListings);

        /// <summary>A purchase from another player's listing (the simulated traders in the local game).</summary>
        internal static void OnMarketBuy(PlayerSave save, MarketGoods goods)
        {
            Add(save, MissionCounterKeys.MarketBuys);
            if (goods?.Kind == MarketGoods.KindFish)
            {
                Add(save, MissionCounterKeys.MarketBuysFish);
            }
        }

        /// <summary>One of the player's fish sold on the Market or in an auction (only fish count).</summary>
        internal static void OnMarketSold(GameConfig config, PlayerSave save, MarketGoods goods)
        {
            if (goods?.Kind != MarketGoods.KindFish || goods.Fish == null)
            {
                return;
            }

            Add(save, MissionCounterKeys.MarketSoldFish);
            if (config.TryGetSpecies(goods.Fish.SpeciesId, out var species))
            {
                Add(save, MissionCounterKeys.MarketSoldFishRarityPrefix + species.Rarity);
            }
        }

        internal static void OnAuctionBid(PlayerSave save) => Add(save, MissionCounterKeys.AuctionBids);

        /// <summary>
        /// An auction the player won. Contested = someone else bid on it too: the player never bids twice in a row, so 2 or
        /// more bids on an auction the player won always include another bidder.
        /// </summary>
        internal static void OnAuctionWon(PlayerSave save, int bidCount)
        {
            Add(save, MissionCounterKeys.AuctionsWon);
            if (bidCount >= 2)
            {
                Add(save, MissionCounterKeys.AuctionsWonContested);
            }
        }

        /// <summary>Attacks credited on the Boss Mundial on an event day, and the band reached.</summary>
        internal static void OnBossAttacks(PlayerSave save, long day, long attacks, int band)
        {
            if (attacks <= 0)
            {
                return;
            }

            Add(save, MissionCounterKeys.BossAttacks, attacks);
            if (Get(save, MissionCounterKeys.BossLastDay) != day)
            {
                Add(save, MissionCounterKeys.BossDays);
                if (save?.Missions?.Counters != null)
                {
                    save.Missions.Counters[MissionCounterKeys.BossLastDay] = day;
                }
            }

            Max(save, MissionCounterKeys.BossBestBand, band);
        }

        internal static void OnProfileVisit(PlayerSave save) => Add(save, MissionCounterKeys.ProfileVisits);
    }
}
