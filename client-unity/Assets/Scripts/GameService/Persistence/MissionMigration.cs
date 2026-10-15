using System;
using System.Collections.Generic;
using System.Linq;
using FishingIdle.GameService.Arena;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Fishing;
using FishingIdle.GameService.Missions;

namespace FishingIdle.GameService.Persistence
{
    /// <summary>
    /// Save version 24 (M24-T54, A-177): the Missões arrive with their counters at zero, but a save from before already shows
    /// part of what they count. This fills each counter with what the old save proves (never more), so the missions it
    /// reached show up ready to claim:
    /// <list type="bullet">
    /// <item>catches by size: the Exceptional and Perfeição statistics, and the fish still in the Fishing Box and the Aquarium;</item>
    /// <item>gene stars, perfect genes and perfect specimens: the fish still in the Fishing Box and the Aquarium;</item>
    /// <item>fish sold (all sales to the game), a fish fed (one with XP invested), Expeditions finished (started less the one on its way);</item>
    /// <item>the Arena: the duels, wins, best streak, wins that moved up and opponents in the battle history (the last 30), and today's rank;</item>
    /// <item>the Market: listings, purchases, sales and auctions in the current lists and the recent notices;</item>
    /// <item>the Boss Mundial: the attacks, the day and the band of the last day played;</item>
    /// <item>size records: the catches in the Fishing Box marked as a new record; Dólares spent: the Passe Premium, when bought.</item>
    /// </list>
    /// What the state shows by itself (level, Moedas, Encyclopedia, maps, equipment, Aquarium, Cardume, Passe, clan) needs no
    /// counter. Idempotent: runs once, then clears <see cref="MissionState.SeedPending"/>.
    /// </summary>
    internal static class MissionMigration
    {
        /// <summary>Returns how many counters it filled.</summary>
        internal static int SeedFromSave(GameConfig config, PlayerSave save)
        {
            var state = save.Missions;
            if (state == null || !state.SeedPending)
            {
                return 0;
            }

            var seeded = new Dictionary<string, long>(StringComparer.Ordinal);
            void Seed(string key, long value)
            {
                if (value <= 0)
                {
                    return;
                }

                seeded.TryGetValue(key, out var have);
                seeded[key] = Math.Max(have, value);
            }

            // Fish the player still has: their size category and genes.
            var fish = save.FishingBox.Where(c => c != null).Select(c => (c.SizeCategoryId, c.Genes))
                .Concat(save.Aquarium.Where(f => f != null).Select(f => (f.SizeCategoryId, f.Genes)))
                .ToList();
            foreach (var group in fish.Where(f => !string.IsNullOrEmpty(f.SizeCategoryId)).GroupBy(f => f.SizeCategoryId))
            {
                Seed(MissionCounterKeys.CatchSizePrefix + group.Key, group.Count());
            }

            Seed(MissionCounterKeys.CatchSizePrefix + "exceptional", save.Stats?.ExceptionalCatches ?? 0);
            Seed(MissionCounterKeys.CatchSizePrefix + "perfect", save.Stats?.PerfectCatches ?? 0);

            var stars = new Dictionary<int, long>();
            long perfectGene = 0, perfectSpecimen = 0;
            foreach (var f in fish.Where(f => GeneRules.IsValid(config, f.Genes)))
            {
                var view = GeneRules.View(config, f.Genes);
                stars.TryGetValue(view.Stars, out var n);
                stars[view.Stars] = n + 1;
                if (view.PerfectCount > 0) perfectGene++;
                if (view.PerfectCount == f.Genes.Length) perfectSpecimen++;
            }

            foreach (var pair in stars)
            {
                Seed(MissionCounterKeys.CatchStarsPrefix + pair.Key, pair.Value);
            }

            Seed(MissionCounterKeys.CatchPerfectGene, perfectGene);
            Seed(MissionCounterKeys.CatchPerfectSpecimen, perfectSpecimen);

            Seed(MissionCounterKeys.BoxSold, save.Stats?.FishSold ?? 0);
            Seed(MissionCounterKeys.FishFed, save.Aquarium.Count(f => f != null && f.InvestedXp > 0));
            Seed(MissionCounterKeys.ExpeditionsCompleted, save.ExpeditionsStarted - (save.Expedition != null && save.Expedition.Active ? 1 : 0));

            // The Arena's battle history (most recent first, bounded).
            var attacks = (save.Arena?.History ?? new List<BattleRecord>()).Where(b => b != null && b.Kind == "attack").ToList();
            Seed(MissionCounterKeys.ArenaDuels, attacks.Count);
            Seed(MissionCounterKeys.ArenaWins, attacks.Count(b => b.PlayerWon));
            Seed(MissionCounterKeys.ArenaStrongerWins, attacks.Count(b => b.PlayerWon && b.RankAfter < b.RankBefore));
            long run = 0, best = 0, current = 0;
            var counting = true;
            foreach (var b in attacks)
            {
                run = b.PlayerWon ? run + 1 : 0;
                best = Math.Max(best, run);
                if (counting)
                {
                    if (b.PlayerWon) current++;
                    else counting = false;
                }
            }

            Seed(MissionCounterKeys.ArenaBestStreak, best);
            Seed(MissionCounterKeys.ArenaStreak, current);
            var rankIndex = save.Arena?.Ranking?.IndexOf(ArenaBots.PlayerId) ?? -1;
            Seed(MissionCounterKeys.ArenaBestRank, rankIndex >= 0 ? rankIndex + 1 : 0);
            foreach (var opponent in attacks.Select(b => b.OpponentId).Where(id => !string.IsNullOrEmpty(id)).Distinct())
            {
                if (!state.ArenaOpponents.Contains(opponent) && state.ArenaOpponents.Count < MissionTracker.ArenaOpponentsCap)
                {
                    state.ArenaOpponents.Add(opponent);
                }
            }

            // The Market: what is listed now and the notices kept.
            var market = save.Market ?? new MarketState();
            var events = market.Events.Where(e => e != null).ToList();
            Seed(MissionCounterKeys.MarketListings, market.MyListings.Count + events.Count(e => e.Kind == MarketEvent.KindSold || e.Kind == MarketEvent.KindExpired));
            var bought = market.Withdrawals.Where(w => w != null && w.Reason == WithdrawalItem.ReasonBought).ToList();
            Seed(MissionCounterKeys.MarketBuys, bought.Count);
            Seed(MissionCounterKeys.MarketBuysFish, bought.Count(w => w.Goods?.Kind == MarketGoods.KindFish));
            var soldFish = events.Where(e => (e.Kind == MarketEvent.KindSold || e.Kind == MarketEvent.KindAuctionSold) && e.Goods?.Kind == MarketGoods.KindFish && e.Goods.Fish != null).ToList();
            Seed(MissionCounterKeys.MarketSoldFish, soldFish.Count);
            foreach (var group in soldFish.GroupBy(e => config.TryGetSpecies(e.Goods.Fish.SpeciesId, out var s) ? s.Rarity : null).Where(g => g.Key != null))
            {
                Seed(MissionCounterKeys.MarketSoldFishRarityPrefix + group.Key, group.Count());
            }

            var won = Math.Max(events.Count(e => e.Kind == MarketEvent.KindAuctionWon), market.Withdrawals.Count(w => w != null && w.Reason == WithdrawalItem.ReasonAuctionWon));
            Seed(MissionCounterKeys.AuctionsWon, won);
            Seed(MissionCounterKeys.AuctionBids, market.Auctions.Count(a => a != null && a.PlayerBid) + events.Count(e => e.Kind == MarketEvent.KindAuctionWon || e.Kind == MarketEvent.KindAuctionLost));

            // The Boss Mundial: the last day played.
            var boss = save.WorldBoss;
            if (boss != null && boss.Day > 0 && boss.Attacks > 0)
            {
                Seed(MissionCounterKeys.BossAttacks, boss.Attacks);
                Seed(MissionCounterKeys.BossDays, 1);
                Seed(MissionCounterKeys.BossLastDay, boss.Day);
                Seed(MissionCounterKeys.BossBestBand, boss.Band);
            }

            // Size records the Fishing Box still shows, and the Passe Premium bought with Dólares.
            foreach (var species in save.FishingBox.Where(c => c != null && (c.Flags & BoxCatchFlags.PersonalRecord) != 0).Select(c => c.SpeciesId).Distinct())
            {
                if (!state.RecordSpecies.Contains(species))
                {
                    state.RecordSpecies.Add(species);
                }
            }

            if (save.Pass != null && save.Pass.Premium)
            {
                Seed(MissionCounterKeys.DollarsSpent, config.Pass.PremiumPriceDollars);
            }

            foreach (var pair in seeded)
            {
                state.Counters.TryGetValue(pair.Key, out var have);
                state.Counters[pair.Key] = Math.Max(have, pair.Value);
            }

            state.SeedPending = false;
            return seeded.Count;
        }
    }
}
