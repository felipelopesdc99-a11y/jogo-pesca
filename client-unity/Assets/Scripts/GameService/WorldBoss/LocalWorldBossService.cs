using System;
using System.Collections.Generic;
using System.Linq;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;
using FishingIdle.GameService.Persistence;
using FishingIdle.GameService.Profile;
using FishingIdle.GameService.Shop;

namespace FishingIdle.GameService.WorldBoss
{
    /// <summary>One reward band as the window shows it.</summary>
    public sealed class WorldBossBandView
    {
        /// <summary>1-based.</summary>
        public int Number { get; internal set; }
        public double DamagePercent { get; internal set; }

        /// <summary>The damage it asks for today (0 before the first attack of the day: no maximum yet).</summary>
        public long ThresholdDamage { get; internal set; }
        public long Coins { get; internal set; }
        public long Shells { get; internal set; }
        public long ClothingFragments { get; internal set; }
        public long BoatFragments { get; internal set; }
        public string BoatName { get; internal set; }
        public bool Reached { get; internal set; }
    }

    /// <summary>A line of the day's ranking.</summary>
    public sealed class WorldBossRankView
    {
        public int Rank { get; internal set; }
        public string Name { get; internal set; }
        public long Damage { get; internal set; }
        public bool IsPlayer { get; internal set; }

        /// <summary>Dólares this rank gets (0 while the online rewards are off).</summary>
        public long Dollars { get; internal set; }
    }

    /// <summary>A fish of the Cardume drawn attacking the boss.</summary>
    public sealed class WorldBossFishView
    {
        /// <summary>1–6.</summary>
        public int Position { get; internal set; }
        public string SpeciesId { get; internal set; }
        public string SpeciesName { get; internal set; }
        public int Level { get; internal set; }

        /// <summary>This fish's share of the last attack (0 before the first one).</summary>
        public long LastDamage { get; internal set; }
    }

    /// <summary>What one finished event day gave (paid once, at the end of the window or the next opening).</summary>
    public sealed class WorldBossResultView
    {
        public DateTime Date { get; internal set; }
        public long Damage { get; internal set; }
        public int Attacks { get; internal set; }
        public int Band { get; internal set; }

        /// <summary>The position in the day's ranking (0 = no damage).</summary>
        public int Rank { get; internal set; }
        public long Coins { get; internal set; }
        public long Shells { get; internal set; }
        public long ClothingFragments { get; internal set; }
        public long BoatFragments { get; internal set; }
        public string BoatName { get; internal set; }

        /// <summary>The boat fragments assembled the boat.</summary>
        public bool BoatAssembled { get; internal set; }
        public long Dollars { get; internal set; }

        public bool IsEmpty => Coins <= 0 && Shells <= 0 && ClothingFragments <= 0 && BoatFragments <= 0 && Dollars <= 0;
    }

    /// <summary>Everything the "Boss Mundial" window and the HUD icon show (M24-T29, A-165).</summary>
    public sealed class WorldBossView
    {
        public string BossName { get; internal set; }

        /// <summary>The window is open now (19:00 to 19:10, Brasília).</summary>
        public bool Open { get; internal set; }

        /// <summary>Seconds until the next opening (0 while open).</summary>
        public double SecondsToStart { get; internal set; }

        /// <summary>Seconds until the window closes (0 while closed).</summary>
        public double SecondsLeft { get; internal set; }

        /// <summary>The opening time in the event's time zone, for "todo dia às 19:00".</summary>
        public int StartHour { get; internal set; }
        public int StartMinute { get; internal set; }
        public int DurationMinutes { get; internal set; }
        public double AttackIntervalSeconds { get; internal set; }
        public int MaxAttacks { get; internal set; }

        /// <summary>Today's event (the player took part today).</summary>
        public long Damage { get; internal set; }
        public int Attacks { get; internal set; }
        public int Band { get; internal set; }

        /// <summary>The next band to reach (0 when the last is reached).</summary>
        public int NextBand { get; internal set; }

        /// <summary>The day's maximum damage (100% of the bands); 0 before the first attack.</summary>
        public long ReferenceDamage { get; internal set; }
        public List<WorldBossBandView> Bands { get; } = new List<WorldBossBandView>();

        /// <summary>Why the Cardume does not attack (CardumeEmpty, CardumeLocked); None when it does.</summary>
        public ServiceError AttackBlocker { get; internal set; }

        /// <summary>The Cardume, positions 1–6 (null = empty), with each fish's share of the last attack.</summary>
        public List<WorldBossFishView> Team { get; } = new List<WorldBossFishView>();
        public long LastAttackDamage { get; internal set; }

        /// <summary>Today's ranking. In the local game only the player (A-100).</summary>
        public List<WorldBossRankView> Ranking { get; } = new List<WorldBossRankView>();
        public bool OnlineRewards { get; internal set; }
        public List<long> DollarsByRank { get; } = new List<long>();

        /// <summary>The last finished event the player took part in (null while today's is running or never took part).</summary>
        public WorldBossResultView LastResult { get; internal set; }
    }

    /// <summary>What one <see cref="IWorldBossService.Update"/> brought, for the notices.</summary>
    public sealed class WorldBossUpdate
    {
        /// <summary>Today's event started for this player (first moment in the window with the game open).</summary>
        public bool Started { get; internal set; }

        /// <summary>The band just reached (0 = none).</summary>
        public int BandReached { get; internal set; }
        public int AttacksCredited { get; internal set; }

        /// <summary>A finished day paid now (null = nothing paid now).</summary>
        public WorldBossResultView Paid { get; internal set; }
    }

    /// <summary>
    /// The Boss Mundial (M24-T29, A-165): every day at 19:00 (Brasília) for 10 minutes, with the game open, the Cardume attacks a
    /// boss with infinite health every few seconds; the day's damage reaches reward bands, paid once when the window ends.
    /// </summary>
    public interface IWorldBossService
    {
        WorldBossView GetWorldBoss();

        /// <summary>
        /// Called by the running game a few times per second: credits the attacks due (only while the game is open), the band
        /// reached, and pays a finished day once.
        /// </summary>
        WorldBossUpdate Update();
    }

    public sealed class LocalWorldBossService : IWorldBossService
    {
        private const ulong AttackSalt = 0xB055_0D1EUL;

        private readonly GameSession _session;
        private readonly ICardumeService _cardume;
        private readonly IFishingService _fishing;

        // Presentation help only: the share of each fish in the last attack this run (never saved).
        private readonly long[] _lastBySlot = new long[6];
        private long _lastAttackDamage;

        // The first Update of this run only anchors the attack clock: the time with the game closed never counts.
        private bool _anchored;

        public LocalWorldBossService(GameSession session, ICardumeService cardume, IFishingService fishing)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _cardume = cardume ?? throw new ArgumentNullException(nameof(cardume));
            _fishing = fishing ?? throw new ArgumentNullException(nameof(fishing));
            _session.SaveReset += () =>
            {
                _anchored = false;
                _lastAttackDamage = 0;
                Array.Clear(_lastBySlot, 0, _lastBySlot.Length);
            };
        }

        private PlayerSave Save => _session.Save;
        private GameConfig Config => _session.Config;
        private WorldBossConfig Boss => Config.WorldBoss;

        private WorldBossState State
        {
            get
            {
                if (Save.WorldBoss == null)
                {
                    Save.WorldBoss = new WorldBossState();
                }

                return Save.WorldBoss;
            }
        }

        public WorldBossUpdate Update()
        {
            var update = new WorldBossUpdate();
            var now = _session.Clock.UtcNowMs;
            var boss = Boss;
            var changed = false;
            var anchoredBefore = _anchored;
            _anchored = true;

            // A record of an older day is settled before today's starts (its window is over; nothing more to attack). Today's
            // is settled at the end, after the attack due at 19:10 itself.
            var today = WorldBossRules.DayOf(boss, now);
            if (State.Day != today)
            {
                changed |= Settle(now, update);
            }

            var start = WorldBossRules.WindowStartMs(boss, today);
            var end = WorldBossRules.WindowEndMs(boss, today);
            if (now >= start && now < end && State.Day != today)
            {
                // First moment in today's window with the game open: today's record starts here (no attack counted before).
                Save.WorldBoss = new WorldBossState { Day = today, LastAttackIndex = WorldBossRules.AttackIndexAt(boss, today, now) };
                _lastAttackDamage = 0;
                Array.Clear(_lastBySlot, 0, _lastBySlot.Length);
                update.Started = true;
                changed = true;
                _session.Log("World boss: day " + today + " started.");
            }
            else if (State.Day == today && now >= start && !State.Paid)
            {
                var index = WorldBossRules.AttackIndexAt(boss, today, now);
                if (!anchoredBefore)
                {
                    // The game was closed: the attacks of that time are lost, the event continues from now.
                    if (index > State.LastAttackIndex)
                    {
                        State.LastAttackIndex = index;
                        changed = true;
                    }
                }
                else if (index > State.LastAttackIndex)
                {
                    changed = true;
                    var credit = Math.Min(index - State.LastAttackIndex, Math.Max(1, boss.Attack.MaxCatchUpAttacks));
                    if (AttackBlocker() == ServiceError.None)
                    {
                        var team = Team();
                        State.ReferenceDamage = Math.Max(State.ReferenceDamage, WorldBossRules.FullWindowDamage(Config, team));
                        for (var k = index - credit + 1; k <= index; k++)
                        {
                            var damage = WorldBossRules.Attack(Config, team, Rng.For(Save.RngSeed ^ AttackSalt, today, k), _lastBySlot);
                            State.Damage = State.Damage > long.MaxValue - damage ? long.MaxValue : State.Damage + damage;
                            State.Attacks++;
                            _lastAttackDamage = damage;
                            update.AttacksCredited++;
                        }

                        var band = WorldBossRules.BandFor(boss, State.Damage, State.ReferenceDamage);
                        if (band > State.Band)
                        {
                            State.Band = band;
                            update.BandReached = band;
                        }
                    }

                    State.LastAttackIndex = index;
                }
            }

            // Today's window may have just closed: pay now.
            changed |= Settle(now, update);
            if (changed)
            {
                _session.Persist();
            }

            return update;
        }

        public WorldBossView GetWorldBoss()
        {
            var boss = Boss;
            var now = _session.Clock.UtcNowMs;
            var today = WorldBossRules.DayOf(boss, now);
            var open = WorldBossRules.IsOpen(boss, now);
            var state = State;
            var view = new WorldBossView
            {
                BossName = boss.DisplayName,
                Open = open,
                SecondsToStart = open ? 0 : Math.Max(0, (WorldBossRules.NextStartMs(boss, now) - now) / 1000.0),
                SecondsLeft = open ? Math.Max(0, (WorldBossRules.WindowEndMs(boss, today) - now) / 1000.0) : 0,
                StartHour = boss.Schedule.StartMinuteOfDay / 60,
                StartMinute = boss.Schedule.StartMinuteOfDay % 60,
                DurationMinutes = boss.Schedule.DurationMinutes,
                AttackIntervalSeconds = boss.Attack.IntervalSeconds,
                MaxAttacks = WorldBossRules.AttacksPerWindow(boss),
                AttackBlocker = AttackBlocker(),
                OnlineRewards = boss.Ranking.OnlineRewardsEnabled,
            };
            view.DollarsByRank.AddRange(boss.Ranking.DollarsByRank);

            var isToday = state.Day == today && !state.Paid;
            var damage = isToday ? state.Damage : 0;
            var reference = isToday ? state.ReferenceDamage : 0;
            view.Damage = damage;
            view.Attacks = isToday ? state.Attacks : 0;
            view.Band = isToday ? state.Band : 0;
            view.ReferenceDamage = (long)Math.Round(reference);
            view.LastAttackDamage = isToday ? _lastAttackDamage : 0;

            var level = Save.FisherLevel;
            var boat = WorldBossRules.BoatFor(Config, level);
            for (var b = 1; b <= boss.Bands.Count; b++)
            {
                var band = boss.Bands[b - 1];
                view.Bands.Add(new WorldBossBandView
                {
                    Number = b,
                    DamagePercent = band.DamagePercent,
                    ThresholdDamage = reference > 0 ? WorldBossRules.Threshold(boss, b, reference) : 0,
                    Coins = WorldBossRules.Coins(boss, band, level),
                    Shells = WorldBossRules.Shells(boss, band, level),
                    ClothingFragments = Config.Clothing != null ? band.ClothingFragments : 0,
                    BoatFragments = boat != null ? band.BoatFragments : 0,
                    BoatName = boat?.DisplayName,
                    Reached = b <= view.Band,
                });
            }

            view.NextBand = view.Band < boss.Bands.Count ? view.Band + 1 : 0;

            var cardume = _cardume.GetCardume();
            foreach (var slot in cardume.Slots)
            {
                view.Team.Add(slot.Fish == null ? null : new WorldBossFishView
                {
                    Position = slot.Position,
                    SpeciesId = slot.Fish.SpeciesId,
                    SpeciesName = slot.Fish.SpeciesName,
                    Level = slot.Fish.Level,
                    LastDamage = isToday && slot.Position >= 1 && slot.Position <= 6 ? _lastBySlot[slot.Position - 1] : 0,
                });
            }

            // The local ranking has only the player (A-100); with a server, the other players' lines come from it.
            if (damage > 0)
            {
                view.Ranking.Add(new WorldBossRankView { Rank = 1, Name = Save.PlayerName, Damage = damage, IsPlayer = true, Dollars = WorldBossRules.DollarsFor(boss, 1) });
            }

            if (state.Day != 0 && state.Paid)
            {
                view.LastResult = Result(state);
            }

            return view;
        }

        // ------------------------------------------------------------------ internals

        private ServiceError AttackBlocker()
        {
            // The same rule as the Arena: a Cardume on an Expedition is busy, an empty one cannot attack.
            if (Save.Expedition.Active) return ServiceError.CardumeLocked;
            if (Save.CardumeSlots.All(id => id == 0)) return ServiceError.CardumeEmpty;
            return ServiceError.None;
        }

        /// <summary>The Cardume's attributes with its modifiers (the 6/6 bonus), positions 1–6, null for an empty one.</summary>
        private List<FishStats> Team()
        {
            return _cardume.GetCardume().Slots.Select(s => s.Fish == null ? null : s.EffectiveStats).ToList();
        }

        /// <summary>Pays the record once its window is over. Returns whether the save changed.</summary>
        private bool Settle(long now, WorldBossUpdate update)
        {
            var state = State;
            var boss = Boss;
            if (state.Day == 0 || state.Paid || now < WorldBossRules.WindowEndMs(boss, state.Day))
            {
                return false;
            }

            state.Paid = true;
            state.Rank = state.Damage > 0 ? 1 : 0;
            var band = Math.Min(state.Band, boss.Bands.Count);
            var level = Save.FisherLevel;
            var assembled = false;
            if (band > 0)
            {
                var reward = boss.Bands[band - 1];
                var coins = WorldBossRules.Coins(boss, reward, level);
                var shells = WorldBossRules.Shells(boss, reward, level);
                Save.Coins = SafeAdd(Save.Coins, coins);
                Save.Shells = SafeAdd(Save.Shells, shells);
                state.PaidCoins = coins;
                state.PaidShells = shells;

                if (reward.ClothingFragments > 0 && Config.Clothing != null)
                {
                    ClothingRules.AddFragments(Save, reward.ClothingFragments);
                    state.PaidClothingFragments = reward.ClothingFragments;
                }

                var boat = WorldBossRules.BoatFor(Config, level);
                if (reward.BoatFragments > 0 && boat != null)
                {
                    // A boat assembled now changes the catch chance: the cycles completed before go with the old one.
                    _fishing.Sync();
                    var gain = BoatFragmentRules.Add(Config, Save, boat, reward.BoatFragments);
                    if (gain != null)
                    {
                        state.PaidBoatFragments = gain.Amount;
                        state.PaidBoatId = boat.Id;
                        assembled = gain.Assembled;
                    }
                }
            }

            var dollars = WorldBossRules.DollarsFor(boss, state.Rank);
            if (dollars > 0)
            {
                Save.Dollars = SafeAdd(Save.Dollars, dollars);
                state.PaidDollars = dollars;
            }

            _session.Log("World boss: day " + state.Day + " settled: damage " + state.Damage + ", band " + band + ", " + state.PaidCoins + " coins, "
                         + state.PaidShells + " shells, " + state.PaidClothingFragments + " clothing fragments, " + state.PaidBoatFragments
                         + " boat fragments, " + state.PaidDollars + " dollars.");
            if (state.Damage > 0)
            {
                var result = Result(state);
                result.BoatAssembled = assembled;
                update.Paid = result;
            }

            return true;
        }

        private WorldBossResultView Result(WorldBossState state)
        {
            Config.TryGetBoat(state.PaidBoatId, out var boat);
            return new WorldBossResultView
            {
                Date = WorldBossRules.DateOf(state.Day),
                Damage = state.Damage,
                Attacks = state.Attacks,
                Band = state.Band,
                Rank = state.Rank,
                Coins = state.PaidCoins,
                Shells = state.PaidShells,
                ClothingFragments = state.PaidClothingFragments,
                BoatFragments = state.PaidBoatFragments,
                BoatName = boat?.DisplayName,
                Dollars = state.PaidDollars,
            };
        }

        private static long SafeAdd(long a, long b) => b > long.MaxValue - a ? long.MaxValue : a + b;
    }
}
