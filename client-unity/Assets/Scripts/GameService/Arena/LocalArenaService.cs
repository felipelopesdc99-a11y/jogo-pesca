using System;
using System.Collections.Generic;
using System.Linq;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Persistence;
using FishingIdle.GameService.Profile;

namespace FishingIdle.GameService.Arena
{
    public sealed class OpponentFishView
    {
        public int Position { get; internal set; }
        public string SpeciesId { get; internal set; }
        public string SpeciesName { get; internal set; }
        public int Level { get; internal set; }
        public string RarityId { get; internal set; }
        public string RarityName { get; internal set; }
    }

    /// <summary>An opponent card: name, rank and Cardume — never its Strength or a predicted result (GDD section 28).</summary>
    public sealed class OpponentView
    {
        public string ParticipantId { get; internal set; }
        public string Name { get; internal set; }
        public int Rank { get; internal set; }
        public List<OpponentFishView> Fish { get; } = new List<OpponentFishView>();
    }

    public sealed class BattleRecordView
    {
        public long AtMs { get; internal set; }
        public bool IsDefense { get; internal set; }
        public string OpponentName { get; internal set; }
        public bool PlayerWon { get; internal set; }
        public int RankBefore { get; internal set; }
        public int RankAfter { get; internal set; }
        public long HonorChange { get; internal set; }
    }

    public sealed class RankingEntryView
    {
        public int Rank { get; internal set; }
        public string Name { get; internal set; }
        public bool IsPlayer { get; internal set; }

        /// <summary>The strongest fish of the team, shown on the podium (top 3 only; null otherwise).</summary>
        public string LeadSpeciesId { get; internal set; }
        public string LeadSpeciesName { get; internal set; }
        public int LeadLevel { get; internal set; }
    }

    public sealed class ArenaShopItemView
    {
        public string Id { get; internal set; }
        public string Name { get; internal set; }
        public long PriceHonor { get; internal set; }
    }

    public sealed class ArenaView
    {
        public int Rank { get; internal set; }
        public int Participants { get; internal set; }
        public int Energy { get; internal set; }
        public int EnergyMax { get; internal set; }
        public int EnergyCost { get; internal set; }

        /// <summary>Seconds until the next Energy point; 0 when full.</summary>
        public double NextEnergySeconds { get; internal set; }

        public long Honor { get; internal set; }
        public int RerollsLeft { get; internal set; }

        /// <summary>Why attacking is refused right now; None when it is possible.</summary>
        public ServiceError AttackBlocker { get; internal set; }

        public List<OpponentView> Opponents { get; } = new List<OpponentView>();
        public List<BattleRecordView> History { get; } = new List<BattleRecordView>();
        /// <summary>The top of the ranking (A-112): positions 1 to <see cref="RankingPageSize"/> × pages, at most the top 100.</summary>
        public List<RankingEntryView> Ranking { get; } = new List<RankingEntryView>();
        public int RankingPageSize { get; internal set; }
        public List<ArenaShopItemView> ShopItems { get; } = new List<ArenaShopItemView>();
    }

    /// <summary>Everything the replay screen needs: both formations and the resolved events.</summary>
    public sealed class BattleReport
    {
        public bool PlayerWon { get; internal set; }
        public string OpponentName { get; internal set; }
        public int RankBefore { get; internal set; }
        public int RankAfter { get; internal set; }
        public long HonorChange { get; internal set; }
        public List<Fighter> PlayerTeam { get; internal set; }
        public List<Fighter> OpponentTeam { get; internal set; }
        public BattleOutcome Outcome { get; internal set; }
    }

    /// <summary>The asynchronous Arena (GDD sections 28–30), with local simulated opponents.</summary>
    public interface IArenaService
    {
        ArenaView GetArena();

        /// <summary>Draws a new opponent set, once per set (GDD section 28).</summary>
        ServiceResult<ArenaView> Reroll();

        /// <summary>Attacks one of the three opponents. Spends Energy win or lose.</summary>
        ServiceResult<BattleReport> Attack(int opponentIndex);

        /// <summary>Processes attacks other players made against you since the last check (also while closed).</summary>
        List<BattleRecordView> Update();
    }

    public sealed class LocalArenaService : IArenaService
    {
        private const int HistoryLimit = 30;
        private const ulong DrawSalt = 0xA2E4_0D2AUL;
        private const ulong BattleSalt = 0xBA77_1E00UL;
        private const ulong IncomingSalt = 0x1C0_3117UL;

        private readonly GameSession _session;
        private readonly ICardumeService _cardume;
        private readonly Dictionary<int, ArenaBot> _botCache = new Dictionary<int, ArenaBot>();

        public LocalArenaService(GameSession session, ICardumeService cardume)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _cardume = cardume ?? throw new ArgumentNullException(nameof(cardume));
            _session.ConfigReplaced += (_, __) => _botCache.Clear();
            _session.SaveReset += () => _botCache.Clear();
            EnsureInitialized();
            Update();
        }

        private PlayerSave Save => _session.Save;
        private GameConfig Config => _session.Config;
        private ArenaState State => Save.Arena;
        private long Now => _session.Clock.UtcNowMs;

        public ArenaView GetArena()
        {
            EnsureInitialized();
            RegenerateEnergy();
            EnsureOpponents();

            var energy = Config.Arena.Energy;
            var view = new ArenaView
            {
                Rank = PlayerRank(),
                Participants = State.Ranking.Count,
                Energy = State.Energy,
                EnergyMax = energy.Max,
                EnergyCost = energy.CostPerInitiatedAttack,
                NextEnergySeconds = State.Energy >= energy.Max ? 0 : Math.Max(0, (State.EnergyUpdatedAtMs + RegenMs - Now) / 1000.0),
                Honor = State.Honor,
                RerollsLeft = Math.Max(0, Config.Arena.OpponentSelection.RerollsPerSet - State.RerollsUsed),
                AttackBlocker = AttackBlocker(),
            };

            foreach (var id in State.Opponents)
            {
                view.Opponents.Add(Opponent(id));
            }

            foreach (var record in State.History)
            {
                view.History.Add(ToView(record));
            }

            // The top of the ranking (A-112): a podium for the first 3, then pages, down to the top 100.
            var top = Math.Min(State.Ranking.Count, Math.Max(3, Config.Arena.Ranking?.TopShown ?? 100));
            view.RankingPageSize = Math.Max(1, Config.Arena.Ranking?.PageSize ?? 10);
            for (var i = 0; i < top; i++)
            {
                var id = State.Ranking[i];
                var entry = new RankingEntryView { Rank = i + 1, Name = NameOf(id), IsPlayer = id == ArenaBots.PlayerId };
                if (i < 3)
                {
                    var lead = TeamOf(id).Where(f => f != null).OrderByDescending(f => f.Level).FirstOrDefault();
                    if (lead != null)
                    {
                        entry.LeadSpeciesId = lead.SpeciesId;
                        entry.LeadSpeciesName = lead.SpeciesName;
                        entry.LeadLevel = lead.Level;
                    }
                }

                view.Ranking.Add(entry);
            }

            foreach (var item in Config.Arena.Shop?.Items ?? new List<ArenaShopItemConfig>())
            {
                view.ShopItems.Add(new ArenaShopItemView { Id = item.Id, Name = item.DisplayName, PriceHonor = item.PriceHonor });
            }

            return view;
        }

        public ServiceResult<ArenaView> Reroll()
        {
            EnsureInitialized();
            EnsureOpponents();
            if (State.RerollsUsed >= Config.Arena.OpponentSelection.RerollsPerSet)
            {
                return ServiceResult<ArenaView>.Fail(ServiceError.NoRerollsLeft);
            }

            State.RerollsUsed++;
            DrawOpponents();
            _session.Persist();
            return ServiceResult<ArenaView>.Ok(GetArena());
        }

        public ServiceResult<BattleReport> Attack(int opponentIndex)
        {
            EnsureInitialized();
            RegenerateEnergy();
            EnsureOpponents();

            var blocker = AttackBlocker();
            if (blocker != ServiceError.None)
            {
                return ServiceResult<BattleReport>.Fail(blocker);
            }

            if (opponentIndex < 0 || opponentIndex >= State.Opponents.Count)
            {
                return ServiceResult<BattleReport>.Fail(ServiceError.OpponentNotFound);
            }

            var opponentId = State.Opponents[opponentIndex];
            var playerTeam = PlayerTeam();
            var opponentTeam = TeamOf(opponentId);

            State.BattleIndex++;
            var outcome = BattleEngine.Resolve(Config, playerTeam, opponentTeam, Rng.For(Save.RngSeed ^ BattleSalt, State.BattleIndex, 0));

            // Energy is spent win or lose; a full bar starts its regeneration clock now.
            if (State.Energy >= Config.Arena.Energy.Max)
            {
                State.EnergyUpdatedAtMs = Now;
            }

            State.Energy -= Config.Arena.Energy.CostPerInitiatedAttack;

            var rankBefore = PlayerRank();
            var opponentRank = RankOf(opponentId);
            var won = outcome.Winner == 0;
            long honor;
            if (won)
            {
                // Direct swap: nobody in between moves (GDD section 29).
                if (opponentRank < rankBefore)
                {
                    Swap(ArenaBots.PlayerId, opponentId);
                }

                honor = Config.Arena.Honor.AttackerVictoryGain;
            }
            else
            {
                honor = -Math.Min(Config.Arena.Honor.DefeatLoss, Math.Max(0, State.Honor - Config.Arena.Honor.MinimumBalance));
            }

            State.Honor += honor;
            var record = new BattleRecord
            {
                AtMs = Now,
                Kind = "attack",
                OpponentId = opponentId,
                PlayerWon = won,
                RankBefore = rankBefore,
                RankAfter = PlayerRank(),
                HonorChange = honor,
            };
            AddHistory(record);

            // A new set only after an attack (GDD section 28).
            State.Opponents.Clear();
            State.RerollsUsed = 0;
            _session.Persist();
            _session.Log("Arena attack on " + opponentId + ": " + (won ? "won" : "lost") + ", rank " + record.RankBefore + " -> " + record.RankAfter);

            return ServiceResult<BattleReport>.Ok(new BattleReport
            {
                PlayerWon = won,
                OpponentName = NameOf(opponentId),
                RankBefore = record.RankBefore,
                RankAfter = record.RankAfter,
                HonorChange = honor,
                PlayerTeam = playerTeam,
                OpponentTeam = opponentTeam,
                Outcome = outcome,
            });
        }

        public List<BattleRecordView> Update()
        {
            EnsureInitialized();
            var incoming = Config.ArenaBots.IncomingAttacks;
            var intervalMs = (long)Math.Round(incoming.CheckIntervalMinutes * 60000.0);
            var defenses = new List<BattleRecordView>();
            var due = (Now - State.IncomingCheckedAtMs) / intervalMs;
            if (due <= 0)
            {
                return defenses;
            }

            var checks = Math.Min(due, Math.Max(1, incoming.MaxChecksPerCatchUp));
            var firstCheck = State.IncomingCheckedAtMs / intervalMs;
            for (long k = 1; k <= checks; k++)
            {
                var rng = Rng.For(Save.RngSeed ^ IncomingSalt, firstCheck + k, 0);
                if (rng.NextDouble() >= incoming.ChancePerCheck)
                {
                    continue;
                }

                var rank = PlayerRank();
                var window = Math.Max(1, (int)Math.Ceiling(rank * incoming.AttackerWindowPercentBelow / 100.0));
                var candidates = Enumerable.Range(rank + 1, Math.Max(0, Math.Min(State.Ranking.Count, rank + window) - rank))
                    .Select(r => State.Ranking[r - 1])
                    .Where(id => id != ArenaBots.PlayerId)
                    .ToList();
                if (candidates.Count == 0)
                {
                    continue;
                }

                var attackerId = candidates[rng.NextIntInclusive(0, candidates.Count - 1)];
                State.BattleIndex++;
                var outcome = BattleEngine.Resolve(Config, TeamOf(attackerId), PlayerTeam(), Rng.For(Save.RngSeed ^ BattleSalt, State.BattleIndex, 1));
                var defended = outcome.Winner == 1;
                var record = new BattleRecord
                {
                    AtMs = State.IncomingCheckedAtMs + k * intervalMs,
                    Kind = "defense",
                    OpponentId = attackerId,
                    PlayerWon = defended,
                    RankBefore = rank,
                };

                if (defended)
                {
                    record.HonorChange = Config.Arena.Honor.SuccessfulDefenseGain;
                    State.Honor += record.HonorChange;
                }
                else
                {
                    Swap(ArenaBots.PlayerId, attackerId);
                }

                record.RankAfter = PlayerRank();
                AddHistory(record);
                defenses.Add(ToView(record));
            }

            State.IncomingCheckedAtMs = due > checks ? Now : State.IncomingCheckedAtMs + checks * intervalMs;
            _session.Persist();
            return defenses;
        }

        // ------------------------------------------------------------------ internals

        private long RegenMs => (long)Math.Round(Config.Arena.Energy.RegenerationSecondsPerPoint * 1000.0);

        private void EnsureInitialized()
        {
            var count = Config.ArenaBots.BotCount;
            var valid = State.Ranking.Count == count + 1
                        && State.Ranking.Contains(ArenaBots.PlayerId)
                        && State.Ranking.Where(id => id != ArenaBots.PlayerId).All(id => ArenaBots.TryParse(id, out var i) && i >= 0 && i < count);
            if (valid)
            {
                return;
            }

            // New Arena (or the opponent list changed in the config): bots in order, the player last.
            State.Ranking = Enumerable.Range(0, count).Select(ArenaBots.IdFor).Concat(new[] { ArenaBots.PlayerId }).ToList();
            State.Opponents.Clear();
            State.RerollsUsed = 0;
            if (State.EnergyUpdatedAtMs == 0)
            {
                State.Energy = Config.Arena.Energy.Max;
                State.EnergyUpdatedAtMs = Now;
                State.IncomingCheckedAtMs = Now;
            }

            _session.Persist();
        }

        private void RegenerateEnergy()
        {
            var max = Config.Arena.Energy.Max;
            if (State.Energy >= max || Now < State.EnergyUpdatedAtMs)
            {
                if (State.Energy >= max)
                {
                    State.EnergyUpdatedAtMs = Now;
                }

                return;
            }

            var points = (Now - State.EnergyUpdatedAtMs) / RegenMs;
            if (points <= 0)
            {
                return;
            }

            State.Energy = (int)Math.Min(max, State.Energy + points);
            State.EnergyUpdatedAtMs = State.Energy >= max ? Now : State.EnergyUpdatedAtMs + points * RegenMs;
        }

        private void EnsureOpponents()
        {
            if (State.Opponents.Count == 0)
            {
                DrawOpponents();
                // Saved right away, so reopening the game cannot redraw it (GDD section 28).
                _session.Persist();
            }
        }

        /// <summary>Three unique opponents from up to ~10% above the player's rank; near #1, the nearest valid ranks.</summary>
        private void DrawOpponents()
        {
            var selection = Config.Arena.OpponentSelection;
            var rank = PlayerRank();
            var window = Math.Max(1, (int)Math.Ceiling(rank * selection.RankWindowPercentAbove / 100.0));
            var candidates = new List<int>();
            for (var r = Math.Max(1, rank - window); r < rank; r++)
            {
                candidates.Add(r);
            }

            for (var r = rank + 1; candidates.Count < selection.OpponentsPerSet && r <= State.Ranking.Count; r++)
            {
                candidates.Add(r);
            }

            State.SetIndex++;
            var rng = Rng.For(Save.RngSeed ^ DrawSalt, State.SetIndex, State.RerollsUsed);
            var picked = new List<string>();
            while (picked.Count < selection.OpponentsPerSet && candidates.Count > 0)
            {
                var i = rng.NextIntInclusive(0, candidates.Count - 1);
                picked.Add(State.Ranking[candidates[i] - 1]);
                candidates.RemoveAt(i);
            }

            State.Opponents = picked.OrderBy(RankOf).ToList();
        }

        private ServiceError AttackBlocker()
        {
            if (Save.Expedition.Active) return ServiceError.CardumeLocked;
            if (Save.CardumeSlots.All(id => id == 0)) return ServiceError.CardumeEmpty;
            if (State.Energy < Config.Arena.Energy.CostPerInitiatedAttack) return ServiceError.NotEnoughEnergy;
            return ServiceError.None;
        }

        private List<Fighter> PlayerTeam()
        {
            var cardume = _cardume.GetCardume();
            return cardume.Slots.Select(s => s.Fish == null ? null : new Fighter
            {
                SpeciesId = s.Fish.SpeciesId,
                SpeciesName = s.Fish.SpeciesName,
                RarityId = s.Fish.RarityId,
                RarityName = s.Fish.RarityName,
                Level = s.Fish.Level,
                Stats = s.EffectiveStats,
            }).ToList();
        }

        private List<Fighter> TeamOf(string id)
        {
            if (id == ArenaBots.PlayerId)
            {
                return PlayerTeam();
            }

            return Bot(id)?.Cardume ?? new List<Fighter> { null, null, null, null, null, null };
        }

        private ArenaBot Bot(string id)
        {
            if (!ArenaBots.TryParse(id, out var index))
            {
                return null;
            }

            if (!_botCache.TryGetValue(index, out var bot))
            {
                bot = ArenaBots.Build(Config, index);
                _botCache[index] = bot;
            }

            return bot;
        }

        private OpponentView Opponent(string id)
        {
            var view = new OpponentView { ParticipantId = id, Name = NameOf(id), Rank = RankOf(id) };
            var team = TeamOf(id);
            for (var i = 0; i < team.Count; i++)
            {
                var f = team[i];
                if (f != null)
                {
                    view.Fish.Add(new OpponentFishView { Position = i + 1, SpeciesId = f.SpeciesId, SpeciesName = f.SpeciesName, Level = f.Level, RarityId = f.RarityId, RarityName = f.RarityName });
                }
            }

            return view;
        }

        private string NameOf(string id) => id == ArenaBots.PlayerId ? Save.PlayerName : Bot(id)?.Name ?? id;

        private int RankOf(string id) => State.Ranking.IndexOf(id) + 1;

        private int PlayerRank() => RankOf(ArenaBots.PlayerId);

        private void Swap(string a, string b)
        {
            var ia = State.Ranking.IndexOf(a);
            var ib = State.Ranking.IndexOf(b);
            State.Ranking[ia] = b;
            State.Ranking[ib] = a;
        }

        private void AddHistory(BattleRecord record)
        {
            State.History.Insert(0, record);
            if (State.History.Count > HistoryLimit)
            {
                State.History.RemoveRange(HistoryLimit, State.History.Count - HistoryLimit);
            }
        }

        private BattleRecordView ToView(BattleRecord r) => new BattleRecordView
        {
            AtMs = r.AtMs,
            IsDefense = r.Kind == "defense",
            OpponentName = NameOf(r.OpponentId),
            PlayerWon = r.PlayerWon,
            RankBefore = r.RankBefore,
            RankAfter = r.RankAfter,
            HonorChange = r.HonorChange,
        };
    }
}
