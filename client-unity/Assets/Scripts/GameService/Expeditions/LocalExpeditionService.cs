using System;
using System.Collections.Generic;
using System.Linq;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;
using FishingIdle.GameService.Persistence;
using FishingIdle.GameService.Profile;

namespace FishingIdle.GameService.Expeditions
{
    public sealed class ExpeditionOfferView
    {
        public string ExpeditionId { get; internal set; }
        public string Name { get; internal set; }
        public double DurationMinutes { get; internal set; }
        public long RecommendedStrength { get; internal set; }
        public long BaseRewardCoins { get; internal set; }
        public double FishFindChance { get; internal set; }

        /// <summary>Reward multiplier with the Cardume as it is now.</summary>
        public double Efficiency { get; internal set; }

        public long ExpectedCoins { get; internal set; }

        /// <summary>Why it cannot be started right now; None when it can.</summary>
        public ServiceError StartBlocker { get; internal set; }
    }

    public sealed class ActiveExpeditionView
    {
        public string ExpeditionId { get; internal set; }
        public string Name { get; internal set; }
        public long StartedAtMs { get; internal set; }
        public long EndsAtMs { get; internal set; }
        public long NowMs { get; internal set; }
        public long Strength { get; internal set; }
        public double Efficiency { get; internal set; }

        public double Progress => EndsAtMs <= StartedAtMs ? 1 : Math.Min(1.0, Math.Max(0.0, (double)(NowMs - StartedAtMs) / (EndsAtMs - StartedAtMs)));
        public double SecondsLeft => Math.Max(0, (EndsAtMs - NowMs) / 1000.0);
    }

    public sealed class ExpeditionResultView
    {
        public string ExpeditionId { get; internal set; }
        public string Name { get; internal set; }
        public long CompletedAtMs { get; internal set; }
        public long Coins { get; internal set; }
        public double Efficiency { get; internal set; }

        /// <summary>The fish found, or null. It is already in the Fishing Box.</summary>
        public CatchView FoundFish { get; internal set; }

        /// <summary>Whether a fish was found, even if it has left the Fishing Box since (sold or kept).</summary>
        public bool FoundAFish { get; internal set; }
    }

    public sealed class ExpeditionsView
    {
        public long CardumeStrength { get; internal set; }
        public int CardumeFilled { get; internal set; }
        public int CardumeSize { get; internal set; }
        public ActiveExpeditionView Active { get; internal set; }
        public List<ExpeditionOfferView> Expeditions { get; } = new List<ExpeditionOfferView>();
    }

    /// <summary>Cardume Expeditions (GDD section 32): 30 min to 6 h, online or offline, no risk.</summary>
    public interface IExpeditionService
    {
        ExpeditionsView GetExpeditions();

        ServiceResult<ActiveExpeditionView> Start(string expeditionId);

        /// <summary>Completes an Expedition whose time is up and pays it. Returns its result, or null.</summary>
        ExpeditionResultView Update();

        /// <summary>The finished Expedition the player has not seen yet, or null.</summary>
        ExpeditionResultView PendingResult();

        void AcknowledgeResult();

        /// <summary>Calls the Cardume back early (owner's request, A-122): it returns now and brings nothing.</summary>
        ServiceResult<ExpeditionsView> Cancel();

        /// <summary>Whether the Cardume is away, which locks conflicting owner actions.</summary>
        bool CardumeLocked { get; }
    }

    public sealed class LocalExpeditionService : IExpeditionService
    {
        private readonly GameSession _session;
        private readonly LocalFishingService _fishing;
        private readonly ICardumeService _cardume;

        public LocalExpeditionService(GameSession session, LocalFishingService fishing, ICardumeService cardume)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _fishing = fishing ?? throw new ArgumentNullException(nameof(fishing));
            _cardume = cardume ?? throw new ArgumentNullException(nameof(cardume));
            // An Expedition that ended while the game was closed is paid on start.
            Update();
        }

        private PlayerSave Save => _session.Save;
        private GameConfig Config => _session.Config;

        public bool CardumeLocked => Save.Expedition.Active;

        public ExpeditionsView GetExpeditions()
        {
            var cardume = _cardume.GetCardume();
            var view = new ExpeditionsView { CardumeStrength = cardume.Strength, CardumeFilled = cardume.Filled, CardumeSize = cardume.Size, Active = Active() };
            foreach (var e in Config.Expeditions.Expeditions)
            {
                var efficiency = ExpeditionRules.Efficiency(Config, cardume.Strength, e.RecommendedStrength);
                view.Expeditions.Add(new ExpeditionOfferView
                {
                    ExpeditionId = e.Id,
                    Name = e.DisplayName,
                    DurationMinutes = e.DurationMinutes,
                    RecommendedStrength = (long)Math.Round(e.RecommendedStrength),
                    BaseRewardCoins = e.RewardCoins,
                    FishFindChance = ExpeditionRules.FishChance(e, efficiency),
                    Efficiency = efficiency,
                    ExpectedCoins = ExpeditionRules.Coins(e, efficiency, ExpeditionRules.MapMultiplier(Config, Save.CurrentMapId)),
                    StartBlocker = Blocker(cardume.Filled),
                });
            }

            return view;
        }

        public ServiceResult<ActiveExpeditionView> Start(string expeditionId)
        {
            var e = Config.Expeditions.Expeditions.FirstOrDefault(x => x.Id == expeditionId);
            if (e == null)
            {
                return ServiceResult<ActiveExpeditionView>.Fail(ServiceError.ExpeditionNotFound);
            }

            var cardume = _cardume.GetCardume();
            var blocker = Blocker(cardume.Filled);
            if (blocker != ServiceError.None)
            {
                return ServiceResult<ActiveExpeditionView>.Fail(blocker);
            }

            var now = _session.Clock.UtcNowMs;
            Save.ExpeditionsStarted++;
            Save.Expedition = new ExpeditionState
            {
                Active = true,
                ExpeditionId = e.Id,
                StartedAtMs = now,
                EndsAtMs = now + (long)Math.Round(e.DurationMinutes * 60000.0),
                Strength = cardume.Strength,
                MapId = Save.CurrentMapId,
                RunIndex = Save.ExpeditionsStarted,
            };
            _session.Persist();
            _session.Log("Expedition " + e.Id + " started with strength " + cardume.Strength + ".");
            return ServiceResult<ActiveExpeditionView>.Ok(Active());
        }

        public ServiceResult<ExpeditionsView> Cancel()
        {
            var state = Save.Expedition;
            if (state == null || !state.Active)
            {
                return ServiceResult<ExpeditionsView>.Fail(ServiceError.ExpeditionNotActive);
            }

            // An Expedition whose time is already up is paid, never cancelled.
            if (_session.Clock.UtcNowMs >= state.EndsAtMs)
            {
                Update();
                return ServiceResult<ExpeditionsView>.Fail(ServiceError.ExpeditionNotActive);
            }

            _session.Log("Expedition " + state.ExpeditionId + " cancelled; nothing paid.");
            Save.Expedition = new ExpeditionState();
            _session.Persist();
            return ServiceResult<ExpeditionsView>.Ok(GetExpeditions());
        }

        public ExpeditionResultView Update()
        {
            var state = Save.Expedition;
            if (!state.Active || _session.Clock.UtcNowMs < state.EndsAtMs)
            {
                return null;
            }

            var e = Config.Expeditions.Expeditions.FirstOrDefault(x => x.Id == state.ExpeditionId);
            var result = new ExpeditionResult { ExpeditionId = state.ExpeditionId, CompletedAtMs = state.EndsAtMs };
            if (e != null)
            {
                // Everything is decided from the snapshot taken at departure and a seeded roll.
                var rng = Rng.For(Save.RngSeed ^ 0x5EED_E4B3UL, state.RunIndex, 1);
                result.Efficiency = ExpeditionRules.Efficiency(Config, state.Strength, e.RecommendedStrength);
                // A-114: the reward grows with the map the Cardume left from (snapshot at departure).
                result.Coins = ExpeditionRules.Coins(e, result.Efficiency, ExpeditionRules.MapMultiplier(Config, state.MapId));
                Save.Coins += result.Coins;

                if (rng.NextDouble() < ExpeditionRules.FishChance(e, result.Efficiency)
                    && (Config.TryGetMap(state.MapId, out var map) || (map = Config.StartingMap) != null))
                {
                    var found = _fishing.AddFoundCatch(CatchRules.RollFound(Config, map, rng), state.EndsAtMs);
                    result.FoundCatchId = found.CatchId;
                }
            }

            Save.LastExpedition = result;
            Save.Expedition = new ExpeditionState();
            _session.Persist();
            _session.Log("Expedition " + result.ExpeditionId + " finished: " + result.Coins + " coins, fish " + result.FoundCatchId + ".");
            return ToView(result);
        }

        public ExpeditionResultView PendingResult()
        {
            var result = Save.LastExpedition;
            return result == null || result.Seen ? null : ToView(result);
        }

        public void AcknowledgeResult()
        {
            if (Save.LastExpedition != null && !Save.LastExpedition.Seen)
            {
                Save.LastExpedition.Seen = true;
                _session.Persist();
            }
        }

        private ExpeditionResultView ToView(ExpeditionResult result)
        {
            var e = Config.Expeditions.Expeditions.FirstOrDefault(x => x.Id == result.ExpeditionId);
            var box = Save.FishingBox.FirstOrDefault(c => c.Id == result.FoundCatchId);
            return new ExpeditionResultView
            {
                ExpeditionId = result.ExpeditionId,
                Name = e?.DisplayName ?? result.ExpeditionId,
                CompletedAtMs = result.CompletedAtMs,
                Coins = result.Coins,
                Efficiency = result.Efficiency,
                FoundFish = box != null ? CatchViews.Create(Config, box) : null,
                FoundAFish = result.FoundCatchId != 0,
            };
        }

        private ActiveExpeditionView Active()
        {
            var state = Save.Expedition;
            if (!state.Active)
            {
                return null;
            }

            var e = Config.Expeditions.Expeditions.FirstOrDefault(x => x.Id == state.ExpeditionId);
            return new ActiveExpeditionView
            {
                ExpeditionId = state.ExpeditionId,
                Name = e?.DisplayName ?? state.ExpeditionId,
                StartedAtMs = state.StartedAtMs,
                EndsAtMs = state.EndsAtMs,
                NowMs = _session.Clock.UtcNowMs,
                Strength = state.Strength,
                Efficiency = e == null ? 1 : ExpeditionRules.Efficiency(Config, state.Strength, e.RecommendedStrength),
            };
        }

        private ServiceError Blocker(int filled)
        {
            if (Save.Expedition.Active) return ServiceError.ExpeditionActive;
            if (filled < Math.Max(1, Config.Arena.Cardume.MinFish)) return ServiceError.CardumeEmpty;
            return ServiceError.None;
        }
    }

    /// <summary>Expedition reward formulas (expeditions.json → efficiency).</summary>
    public static class ExpeditionRules
    {
        /// <summary>
        /// Below recommended: (strength / recommended) ^ exponent, never under the floor.
        /// At recommended: 100%. Above: 1 + slope × (ratio − 1), never over the cap.
        /// </summary>
        public static double Efficiency(GameConfig config, double strength, double recommended)
        {
            var eff = config.Expeditions.Efficiency;
            if (recommended <= 0)
            {
                return eff.AtRecommendedMultiplier;
            }

            var ratio = Math.Max(0, strength) / recommended;
            if (ratio < 1)
            {
                return Math.Max(eff.BelowRecommended.Floor, Math.Pow(ratio, eff.BelowRecommended.Exponent) * eff.AtRecommendedMultiplier);
            }

            return Math.Min(eff.AboveRecommended.Cap, eff.AtRecommendedMultiplier + eff.AboveRecommended.Slope * (ratio - 1));
        }

        public static long Coins(ExpeditionConfig e, double efficiency, double mapMultiplier = 1.0)
        {
            return (long)Math.Round(e.RewardCoins * efficiency * Math.Max(1.0, mapMultiplier), MidpointRounding.AwayFromZero);
        }

        /// <summary>The map's expedition reward multiplier (maps.json, A-114); 1 when the map is unknown or has none.</summary>
        public static double MapMultiplier(GameConfig config, string mapId)
        {
            return mapId != null && config.TryGetMap(mapId, out var map) && map.ExpeditionRewardMultiplier > 0 ? map.ExpeditionRewardMultiplier : 1.0;
        }

        /// <summary>Fish-find chance: the configured chance, reduced when under-strength, never raised above it.</summary>
        public static double FishChance(ExpeditionConfig e, double efficiency)
        {
            return e.FishFindChance * Math.Min(1.0, efficiency);
        }
    }
}
