using System;
using System.Collections.Generic;
using System.Linq;
using FishingIdle.GameService.Arena;
using FishingIdle.GameService.Clans;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;
using FishingIdle.GameService.Persistence;
using FishingIdle.GameService.Profile;
using FishingIdle.GameService.Shop;

namespace FishingIdle.GameService.Missions
{
    /// <summary>Where a mission stands: how far (Current) out of how much (Target). Done when Current reaches Target.</summary>
    public struct MissionProgress
    {
        public MissionProgress(long current, long target)
        {
            var goal = Math.Max(1, target);
            Target = goal;
            Current = Math.Max(0, Math.Min(current, goal));
        }

        public long Current { get; }
        public long Target { get; }
        public bool Done => Current >= Target;
    }

    /// <summary>
    /// The progress of every mission type (M24-T54, A-177): read from the save's state when it shows it (level, Moedas,
    /// Encyclopedia, equipment, Cardume…) and from the event counters (<see cref="MissionCounterKeys"/>) otherwise. Pure:
    /// the same save and config always give the same progress. Online-only missions never progress in the local game.
    /// </summary>
    public sealed class MissionRules
    {
        private readonly GameConfig _config;
        private readonly PlayerSave _save;
        private readonly bool _online;

        // Built once per evaluation, only when a mission asks for it.
        private Dictionary<string, long> _caughtByRarity;
        private List<FishInstance> _cardume;
        private long? _cardumeStrength;

        public MissionRules(GameConfig config, PlayerSave save, bool online)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _online = online;
        }

        public MissionProgress Progress(MissionConfig m)
        {
            var target = Target(m);
            if (m.OnlineOnly && !_online)
            {
                return new MissionProgress(0, target);
            }

            return new MissionProgress(Current(m, target), target);
        }

        /// <summary>The goal: the mission's target, or what "all" means in today's config.</summary>
        public long Target(MissionConfig m)
        {
            switch (m.Type)
            {
                case MissionTypes.MapAllSpecies:
                    return _config.TryGetMap(m.MapId, out var map) ? map.FishPool.Select(p => p.SpeciesId).Distinct().Count() : 1;
                case MissionTypes.RarityAllSpecies:
                    return _config.FishCatalog.Species.Count(s => s.Rarity == m.Rarity);
                case MissionTypes.BoatsAll:
                    return _config.FragmentBoats.Count;
                case MissionTypes.CardumeAllRarityMin:
                case MissionTypes.CardumeAllLevel:
                    return _config.CardumeSize;
                case MissionTypes.EncyclopediaSpecies:
                    return m.Target > 0 ? m.Target : _config.FishCatalog.Species.Count;
                case MissionTypes.ClothingAssembled:
                case MissionTypes.ClothingMaxLevel:
                    return m.Target > 0 ? m.Target : _config.ClothingPieces.Count;
                case MissionTypes.BossBand:
                    return m.Target > 0 ? m.Target : _config.WorldBoss.Bands.Count;
                default:
                    return MissionTypes.YesNo.Contains(m.Type) ? 1 : m.Target;
            }
        }

        /// <summary>The goal as the player reads it in the text (the rank of a "top N" goal, the band of boss_band…).</summary>
        public long TextTarget(MissionConfig m)
        {
            switch (m.Type)
            {
                case MissionTypes.ArenaRankTop:
                case MissionTypes.ClanRankTop:
                    return m.Target;
                default:
                    return Target(m);
            }
        }

        private long Current(MissionConfig m, long target)
        {
            var save = _save;
            switch (m.Type)
            {
                case MissionTypes.CatchCount:
                    return save.Stats?.TotalCatches ?? 0;
                case MissionTypes.CatchSpecies:
                    return save.SpeciesRecords.TryGetValue(m.SpeciesId ?? string.Empty, out var record) ? record.TimesCaught : 0;
                case MissionTypes.CatchRarity:
                    return CaughtByRarity().TryGetValue(m.Rarity ?? string.Empty, out var caught) ? caught : 0;
                case MissionTypes.CatchSizeCategory:
                    return MissionTracker.Get(save, MissionCounterKeys.CatchSizePrefix + m.SizeCategory);
                case MissionTypes.CatchGeneStars:
                    return StarsAtLeast(m.Stars);
                case MissionTypes.CatchPerfectGene:
                    return MissionTracker.Get(save, MissionCounterKeys.CatchPerfectGene);
                case MissionTypes.CatchPerfectSpecimen:
                    return MissionTracker.Get(save, MissionCounterKeys.CatchPerfectSpecimen);

                case MissionTypes.FisherLevel:
                    return save.FisherLevel;
                case MissionTypes.CoinsHeld:
                    return save.Coins;
                case MissionTypes.ShellsHeld:
                    return save.Shells;
                case MissionTypes.DollarsSpent:
                    return MissionTracker.Get(save, MissionCounterKeys.DollarsSpent);
                case MissionTypes.BoxSell:
                    return MissionTracker.Get(save, MissionCounterKeys.BoxSold);

                case MissionTypes.AquariumKeep:
                    // Every fish ever put in the Aquarium has its own id; the next id says how many there were.
                    return Math.Max(0, save.NextFishId - 1);
                case MissionTypes.FishFeed:
                    return MissionTracker.Get(save, MissionCounterKeys.FishFed);
                case MissionTypes.FishLevel:
                    return save.Aquarium.Count == 0 ? 0 : save.Aquarium.Max(f => f?.Level ?? 0);
                case MissionTypes.CardumeSize:
                    return Cardume().Count;
                case MissionTypes.CardumeStrength:
                    return CardumeStrength();
                case MissionTypes.CardumeAllRarityMin:
                {
                    var min = _config.RarityRank(m.Rarity);
                    return Cardume().Count(f => _config.TryGetSpecies(f.SpeciesId, out var s) && _config.RarityRank(s.Rarity) >= min);
                }

                case MissionTypes.CardumeGeneStars:
                    return Cardume().Count(f => GeneRules.StarsOf(_config, f.Genes) >= m.Stars);
                case MissionTypes.CardumeAllLevel:
                    return Cardume().Count(f => f.Level >= m.Level);

                case MissionTypes.MapUnlocked:
                    return _config.TryGetMap(m.MapId, out var unlockMap) && save.FisherLevel >= unlockMap.UnlockFisherLevel ? 1 : 0;
                case MissionTypes.EncyclopediaSpecies:
                    return save.SpeciesRecords.Keys.Count(id => _config.TryGetSpecies(id, out _));
                case MissionTypes.MapAllSpecies:
                    return _config.TryGetMap(m.MapId, out var poolMap)
                        ? poolMap.FishPool.Select(p => p.SpeciesId).Distinct().Count(id => save.SpeciesRecords.ContainsKey(id))
                        : 0;
                case MissionTypes.RarityAllSpecies:
                    return _config.FishCatalog.Species.Count(s => s.Rarity == m.Rarity && save.SpeciesRecords.ContainsKey(s.Id));
                case MissionTypes.SizeRecords:
                    return save.Missions?.RecordSpecies?.Count ?? 0;

                case MissionTypes.RodAssembled:
                    return RodFragmentRules.Owns(save, m.RodId) ? 1 : 0;
                case MissionTypes.BoatAssembled:
                    return _config.TryGetBoat(m.BoatId, out var boat) && BoatFragmentRules.Owns(_config, save, boat) ? 1 : 0;
                case MissionTypes.BoatsAll:
                    return _config.FragmentBoats.Count(b => BoatFragmentRules.Owns(_config, save, b));
                case MissionTypes.ClothingAssembled:
                    return ClothingRules.AssembledCount(_config, save);
                case MissionTypes.ClothingMaxLevel:
                    return _config.ClothingPieces.Count(p => ClothingRules.Owns(save, p.Id) && ClothingRules.Level(_config, save, p.Id) >= _config.ClothingMaxLevel);
                case MissionTypes.BaitUsed:
                    return MissionTracker.Get(save, MissionCounterKeys.BaitPrefix + m.BaitId);
                case MissionTypes.PassRewardsClaimed:
                    return (save.Pass?.FreeClaimed?.Count ?? 0) + (save.Pass?.PremiumClaimed?.Count ?? 0);

                case MissionTypes.ExpeditionStarted:
                    return save.ExpeditionsStarted;
                case MissionTypes.ExpeditionsCompleted:
                    return MissionTracker.Get(save, MissionCounterKeys.ExpeditionsCompleted);
                case MissionTypes.ArenaDuels:
                    return MissionTracker.Get(save, MissionCounterKeys.ArenaDuels);
                case MissionTypes.ArenaWins:
                    return MissionTracker.Get(save, MissionCounterKeys.ArenaWins);
                case MissionTypes.ArenaWinStreak:
                    return MissionTracker.Get(save, MissionCounterKeys.ArenaBestStreak);
                case MissionTypes.ArenaDistinctOpponents:
                    return save.Missions?.ArenaOpponents?.Count ?? 0;
                case MissionTypes.ArenaWinStronger:
                    return MissionTracker.Get(save, MissionCounterKeys.ArenaStrongerWins);
                case MissionTypes.ArenaRankTop:
                {
                    var best = BestArenaRank();
                    return best > 0 && best <= m.Target ? 1 : 0;
                }

                case MissionTypes.MarketListing:
                    return MissionTracker.Get(save, MissionCounterKeys.MarketListings);
                case MissionTypes.MarketBuy:
                    return MissionTracker.Get(save, m.GoodsKind == MarketGoods.KindFish ? MissionCounterKeys.MarketBuysFish : MissionCounterKeys.MarketBuys);
                case MissionTypes.MarketSell:
                    return MissionTracker.Get(save, string.IsNullOrEmpty(m.Rarity) ? MissionCounterKeys.MarketSoldFish : MissionCounterKeys.MarketSoldFishRarityPrefix + m.Rarity);
                case MissionTypes.AuctionBid:
                    return MissionTracker.Get(save, MissionCounterKeys.AuctionBids);
                case MissionTypes.AuctionWon:
                    return MissionTracker.Get(save, m.Contested ? MissionCounterKeys.AuctionsWonContested : MissionCounterKeys.AuctionsWon);

                case MissionTypes.BossAttack:
                    return MissionTracker.Get(save, MissionCounterKeys.BossAttacks);
                case MissionTypes.BossBand:
                    return MissionTracker.Get(save, MissionCounterKeys.BossBestBand);
                case MissionTypes.BossDays:
                    return MissionTracker.Get(save, MissionCounterKeys.BossDays);
                case MissionTypes.ClanJoin:
                    return ClanRules.MyClan(save) != null ? 1 : 0;
                case MissionTypes.ClanFish:
                {
                    var clan = ClanRules.MyClan(save);
                    return clan != null && ClanRules.MyFish(save, ClanRules.MyMember(save, clan)) != null ? 1 : 0;
                }

                case MissionTypes.ClanRankTop:
                {
                    var rank = save.Clans?.LastResult?.Rank ?? 0;
                    return rank > 0 && rank <= m.Target ? 1 : 0;
                }

                case MissionTypes.ClanSameDayBoss:
                    // Needs the other members' Boss attacks, which only a server knows (online_only).
                    return 0;

                case MissionTypes.ProfileVisit:
                    return MissionTracker.Get(save, MissionCounterKeys.ProfileVisits);
                default:
                    return 0;
            }
        }

        /// <summary>The best Arena rank the player has held (the counter, or today's rank when it is better). 0 = none yet.</summary>
        public long BestArenaRank()
        {
            var best = MissionTracker.Get(_save, MissionCounterKeys.ArenaBestRank);
            var index = _save.Arena?.Ranking?.IndexOf(ArenaBots.PlayerId) ?? -1;
            var now = index >= 0 ? index + 1 : 0;
            if (now > 0 && (best <= 0 || now < best))
            {
                best = now;
            }

            return best;
        }

        /// <summary>Catches of each rarity, from the Encyclopedia (every catch of a species counts there).</summary>
        private Dictionary<string, long> CaughtByRarity()
        {
            if (_caughtByRarity != null)
            {
                return _caughtByRarity;
            }

            _caughtByRarity = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (var pair in _save.SpeciesRecords)
            {
                if (pair.Value != null && _config.TryGetSpecies(pair.Key, out var species))
                {
                    _caughtByRarity.TryGetValue(species.Rarity, out var have);
                    _caughtByRarity[species.Rarity] = SafeMath.Add(have, pair.Value.TimesCaught);
                }
            }

            return _caughtByRarity;
        }

        private long StarsAtLeast(int stars)
        {
            long total = 0;
            var max = _config.Progression.Genes?.QualityStars?.Count ?? 5;
            for (var s = Math.Max(1, stars); s <= Math.Max(max, stars); s++)
            {
                total = SafeMath.Add(total, MissionTracker.Get(_save, MissionCounterKeys.CatchStarsPrefix + s));
            }

            return total;
        }

        /// <summary>The fish in the Cardume, in position order (empty positions skipped).</summary>
        private List<FishInstance> Cardume()
        {
            if (_cardume != null)
            {
                return _cardume;
            }

            _cardume = new List<FishInstance>();
            foreach (var id in _save.CardumeSlots)
            {
                if (id == 0)
                {
                    continue;
                }

                var fish = _save.Aquarium.FirstOrDefault(f => f != null && f.Id == id);
                if (fish != null)
                {
                    _cardume.Add(fish);
                }
            }

            return _cardume;
        }

        /// <summary>The Cardume's Strength as the Cardume screen shows it (with the complete-Cardume bonus when it applies).</summary>
        private long CardumeStrength()
        {
            if (_cardumeStrength.HasValue)
            {
                return _cardumeStrength.Value;
            }

            var fish = Cardume();
            var bonus = CardumeRules.CompleteBonusActive(_config, fish.Count);
            double raw = 0;
            foreach (var f in fish)
            {
                if (!_config.TryGetSpecies(f.SpeciesId, out var species))
                {
                    continue;
                }

                var genes = GeneRules.IsValid(_config, f.Genes) ? f.Genes : GeneRules.Neutral(_config);
                var stats = CardumeRules.Effective(_config, FishRules.Stats(_config, species, f.SizeMm, f.Level, f.SizeCategoryId, genes), bonus);
                raw += CardumeRules.RawStrength(_config, stats);
            }

            _cardumeStrength = Math.Max(0L, CardumeRules.Display(_config, raw));
            return _cardumeStrength.Value;
        }
    }
}
