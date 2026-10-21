using System;
using System.Collections.Generic;
using System.Linq;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Persistence;
using FishingIdle.Texts;

namespace FishingIdle.GameService.Missions
{
    /// <summary>One mission as the Missões window shows it. Everything here was decided by the service.</summary>
    public sealed class MissionView
    {
        public string Id { get; internal set; }

        /// <summary>Its place in the list (1 to 100).</summary>
        public int Number { get; internal set; }
        public string Type { get; internal set; }

        /// <summary>The goal in PT-BR ("Pesque 100 peixes"), built by GameTexts.Missions.</summary>
        public string Text { get; internal set; }
        public long Current { get; internal set; }
        public long Target { get; internal set; }

        /// <summary>Reached and not claimed yet: "Resgatar".</summary>
        public bool Ready { get; internal set; }
        public bool Claimed { get; internal set; }

        /// <summary>Completes only with the game online; in the local game it is shown, never completed.</summary>
        public bool OnlineOnly { get; internal set; }

        /// <summary>The best Arena rank held, for the "top N" goals (0 = not ranked yet, or another type).</summary>
        public long BestRank { get; internal set; }

        public long RewardCoins { get; internal set; }
        public long RewardShells { get; internal set; }
        public long RewardDollars { get; internal set; }

        public double Fraction => Target <= 0 ? 0 : Math.Min(1.0, Current / (double)Target);
    }

    public sealed class MissionsView
    {
        public List<MissionView> Missions { get; } = new List<MissionView>();
        public int Total { get; internal set; }
        public int ReadyCount { get; internal set; }
        public int ClaimedCount { get; internal set; }

        /// <summary>False in the local game: the online-only missions say "Liga quando o jogo estiver online".</summary>
        public bool Online { get; internal set; }
    }

    /// <summary>What a claim paid.</summary>
    public sealed class MissionClaimResult
    {
        public int Claimed { get; internal set; }
        public long Coins { get; internal set; }
        public long Shells { get; internal set; }
        public long Dollars { get; internal set; }
    }

    /// <summary>A mission that has just become ready to claim (for the notice).</summary>
    public sealed class MissionReadyNotice
    {
        public string Id { get; internal set; }
        public string Text { get; internal set; }
    }

    /// <summary>
    /// The Missões (M24-T54, A-177): 100 goals over the whole game, all visible from the start. Local today; with a server,
    /// the same interface over HTTP, the server being the one that decides progress and pays (docs/SEGURANCA.md).
    /// </summary>
    public interface IMissionService
    {
        /// <summary>Every mission, in order, with its progress, reward and state.</summary>
        MissionsView GetMissions();

        /// <summary>How many missions are ready to claim (the shortcut's badge). Cheap: no allocation.</summary>
        int ReadyCount { get; }

        /// <summary>Pays one ready mission, once.</summary>
        ServiceResult<MissionClaimResult> Claim(string missionId);

        /// <summary>Pays every ready mission.</summary>
        ServiceResult<MissionClaimResult> ClaimAll();

        /// <summary>The missions that became ready since the last call, oldest first (empty, no allocation, when none).</summary>
        IReadOnlyList<MissionReadyNotice> TakeNewlyReady();
    }

    public sealed class LocalMissionService : IMissionService
    {
        private static readonly IReadOnlyList<MissionReadyNotice> NoNotices = Array.Empty<MissionReadyNotice>();

        private readonly GameSession _session;
        private readonly List<MissionReadyNotice> _newlyReady = new List<MissionReadyNotice>();
        private int _readyCount;

        public LocalMissionService(GameSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));

            // Every write of the save first looks at the missions, so a goal reached for a moment (Moedas before a purchase)
            // is never missed, and its completion is written in the same save.
            _session.BeforePersist += () => Evaluate(true);
            _session.SaveReset += () =>
            {
                _newlyReady.Clear();
                Evaluate(false);
            };

            // An old save opens with the missions its state already proves ready (no notice for them: the badge shows them).
            if (Evaluate(false))
            {
                _session.Persist();
            }
        }

        /// <summary>
        /// The local game has no other real players: the online-only missions never complete here (A-177). A server answers
        /// true and fills what they need (the clan's Boss attacks, the clan ranking).
        /// </summary>
        public bool Online => false;

        private PlayerSave Save => _session.Save;
        private GameConfig Config => _session.Config;
        private List<MissionConfig> Missions => Config.Missions?.Missions ?? new List<MissionConfig>();

        public int ReadyCount => _readyCount;

        public MissionsView GetMissions()
        {
            Evaluate(true);
            var rules = new MissionRules(Config, Save, Online);
            var state = Save.Missions;
            var view = new MissionsView { Online = Online };
            var number = 0;
            foreach (var m in Missions)
            {
                number++;
                var completed = state.Completed.Contains(m.Id);
                var claimed = state.Claimed.Contains(m.Id);
                var progress = rules.Progress(m);
                var item = new MissionView
                {
                    Id = m.Id,
                    Number = number,
                    Type = m.Type,
                    Text = Text(m, rules),
                    Target = progress.Target,
                    // A completed mission shows its goal full, even if the state went back down (Moedas spent).
                    Current = completed ? progress.Target : progress.Current,
                    Ready = completed && !claimed,
                    Claimed = claimed,
                    OnlineOnly = m.OnlineOnly,
                    BestRank = m.Type == MissionTypes.ArenaRankTop ? rules.BestArenaRank() : 0,
                    RewardCoins = m.Reward?.Coins ?? 0,
                    RewardShells = m.Reward?.Shells ?? 0,
                    RewardDollars = m.Reward?.Dollars ?? 0,
                };
                view.Missions.Add(item);
                if (item.Ready) view.ReadyCount++;
                if (item.Claimed) view.ClaimedCount++;
            }

            view.Total = view.Missions.Count;
            return view;
        }

        public ServiceResult<MissionClaimResult> Claim(string missionId)
        {
            var mission = Missions.FirstOrDefault(m => m.Id == missionId);
            if (mission == null)
            {
                return ServiceResult<MissionClaimResult>.Fail(ServiceError.MissionNotFound);
            }

            var state = Save.Missions;
            if (state.Claimed.Contains(mission.Id))
            {
                return ServiceResult<MissionClaimResult>.Fail(ServiceError.MissionAlreadyClaimed);
            }

            Evaluate(true);
            if (!state.Completed.Contains(mission.Id))
            {
                return ServiceResult<MissionClaimResult>.Fail(ServiceError.MissionNotReady);
            }

            var result = new MissionClaimResult();
            Pay(mission, result);
            _session.Persist();
            _session.Log("Mission " + mission.Id + " claimed: " + result.Coins + " coins, " + result.Shells + " shells, " + result.Dollars + " dollars.");
            return ServiceResult<MissionClaimResult>.Ok(result);
        }

        public ServiceResult<MissionClaimResult> ClaimAll()
        {
            Evaluate(true);
            var state = Save.Missions;
            var result = new MissionClaimResult();
            foreach (var mission in Missions)
            {
                if (state.Completed.Contains(mission.Id) && !state.Claimed.Contains(mission.Id))
                {
                    Pay(mission, result);
                }
            }

            if (result.Claimed == 0)
            {
                return ServiceResult<MissionClaimResult>.Fail(ServiceError.MissionNothingToClaim);
            }

            _session.Persist();
            _session.Log("Missions claimed: " + result.Claimed + " (" + result.Coins + " coins, " + result.Shells + " shells, " + result.Dollars + " dollars).");
            return ServiceResult<MissionClaimResult>.Ok(result);
        }

        public IReadOnlyList<MissionReadyNotice> TakeNewlyReady()
        {
            if (_newlyReady.Count == 0)
            {
                return NoNotices;
            }

            var taken = _newlyReady.ToList();
            _newlyReady.Clear();
            return taken;
        }

        /// <summary>Marks paid first, then adds the reward (SafeMath): a mission is never paid twice.</summary>
        private void Pay(MissionConfig mission, MissionClaimResult result)
        {
            Save.Missions.Claimed.Add(mission.Id);
            var reward = mission.Reward ?? new MissionRewardConfig();
            Save.Coins = SafeMath.Add(Save.Coins, Math.Max(0, reward.Coins));
            Save.Shells = SafeMath.Add(Save.Shells, Math.Max(0, reward.Shells));
            Save.Dollars = SafeMath.Add(Save.Dollars, Math.Max(0, reward.Dollars));
            result.Claimed++;
            result.Coins = SafeMath.Add(result.Coins, Math.Max(0, reward.Coins));
            result.Shells = SafeMath.Add(result.Shells, Math.Max(0, reward.Shells));
            result.Dollars = SafeMath.Add(result.Dollars, Math.Max(0, reward.Dollars));
            _readyCount = Math.Max(0, _readyCount - 1);
        }

        /// <summary>
        /// Looks at every mission not completed yet and marks the ones whose goal is reached (they stay completed). Returns
        /// whether any changed. <paramref name="notify"/> queues a notice for each one.
        /// </summary>
        private bool Evaluate(bool notify)
        {
            var save = Save;
            if (save?.Missions == null || Config.Missions?.Missions == null)
            {
                _readyCount = 0;
                return false;
            }

            var state = save.Missions;
            MissionRules rules = null;
            var changed = false;
            var ready = 0;
            foreach (var m in Config.Missions.Missions)
            {
                if (state.Completed.Contains(m.Id))
                {
                    if (!state.Claimed.Contains(m.Id))
                    {
                        ready++;
                    }

                    continue;
                }

                rules = rules ?? new MissionRules(Config, save, Online);
                if (!rules.Progress(m).Done)
                {
                    continue;
                }

                state.Completed.Add(m.Id);
                changed = true;
                if (!state.Claimed.Contains(m.Id))
                {
                    ready++;
                    if (notify)
                    {
                        _newlyReady.Add(new MissionReadyNotice { Id = m.Id, Text = Text(m, rules) });
                    }
                }
            }

            // The best Arena rank is remembered for the "top N" goals and their text.
            if (rules != null)
            {
                var best = rules.BestArenaRank();
                if (best > 0 && best != MissionTracker.Get(save, MissionCounterKeys.ArenaBestRank))
                {
                    state.Counters[MissionCounterKeys.ArenaBestRank] = best;
                }
            }

            _readyCount = ready;
            return changed;
        }

        /// <summary>The goal in PT-BR, from the type, the target and the names in the config.</summary>
        private string Text(MissionConfig m, MissionRules rules)
        {
            var target = rules.TextTarget(m);
            string name = null;
            switch (m.Type)
            {
                case MissionTypes.CatchSpecies:
                    name = Config.TryGetSpecies(m.SpeciesId, out var species) ? species.DisplayName : m.SpeciesId;
                    if (species != null && !Fishing.CatchRules.IsCommon(species) && Config.TryGetRarity(species.Rarity, out var speciesRarity))
                    {
                        // A species above Comum says its rarity: "Pesque o Tubarão-boca-grande (Mítico)".
                        name = GameTexts.Missions.WithRarity(name, speciesRarity.DisplayName);
                    }

                    break;
                case MissionTypes.CatchRarity:
                case MissionTypes.RarityAllSpecies:
                case MissionTypes.CardumeAllRarityMin:
                case MissionTypes.MarketSell:
                    name = m.Rarity == null ? null : Config.TryGetRarity(m.Rarity, out var rarity) ? rarity.DisplayName : m.Rarity;
                    break;
                case MissionTypes.CatchSizeCategory:
                    name = Config.TryGetSizeCategory(m.SizeCategory, out var size) ? size.DisplayName : m.SizeCategory;
                    break;
                case MissionTypes.MapUnlocked:
                case MissionTypes.MapAllSpecies:
                    name = Config.TryGetMap(m.MapId, out var map) ? map.DisplayName : m.MapId;
                    break;
                case MissionTypes.RodAssembled:
                    name = Config.TryGetRod(m.RodId, out var rod) ? rod.DisplayName : m.RodId;
                    break;
                case MissionTypes.BoatAssembled:
                    name = Config.TryGetBoat(m.BoatId, out var boat) ? boat.DisplayName : m.BoatId;
                    break;
                case MissionTypes.BaitUsed:
                    name = Config.TryGetBait(m.BaitId, out var bait) ? bait.DisplayName : m.BaitId;
                    break;
            }

            var number = m.Type == MissionTypes.CatchGeneStars || m.Type == MissionTypes.CardumeGeneStars ? m.Stars
                : m.Type == MissionTypes.CardumeAllLevel ? m.Level
                : m.Type == MissionTypes.CatchPerfectGene || m.Type == MissionTypes.CatchPerfectSpecimen ? (Config.Progression.Genes?.MaxValue ?? 31)
                : 0;
            var all = (m.Type == MissionTypes.EncyclopediaSpecies || m.Type == MissionTypes.ClothingAssembled || m.Type == MissionTypes.ClothingMaxLevel || m.Type == MissionTypes.BossBand)
                      && m.Target <= 0;
            return GameTexts.Missions.Objective(m.Type, target, name, number, all, m.GoodsKind == MarketGoods.KindFish, m.Contested);
        }
    }
}
