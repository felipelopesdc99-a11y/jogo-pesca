using System;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;
using FishingIdle.GameService.Persistence;
using FishingIdle.GameService.Upgrades;

namespace FishingIdle.GameService.Crew
{
    /// <summary>
    /// The automatic Crew (M24-T05, A-154): hire fishers and boats that earn Moedas and Fisher XP per second, with
    /// the game open or closed. Never fish.
    /// </summary>
    /// <remarks>
    /// The client only asks: look at the Crew, hire, sync. The income is never sent by the client: it comes out of
    /// <see cref="Sync"/>, from the clock (<see cref="IClock"/>, which never goes back, TD-030) and the saved cursor
    /// (<see cref="CrewState.LastCreditedAtMs"/>), so calling it twice credits nothing twice. No roll is involved: the
    /// income is the same for the same units and time.
    /// </remarks>
    public interface ICrewService
    {
        /// <summary>The Crew with each member's price for <paramref name="mode"/>.</summary>
        CrewView GetCrew(CrewBuyMode mode);

        /// <summary>Hires 1, 10, 100 or as many as the Moedas pay of a member. The income owed is credited first.</summary>
        ServiceResult<CrewHireResult> Hire(string memberId, CrewBuyMode mode);

        /// <summary>
        /// Credits the income since the last sync. A short gap (the game open) counts in full; a long one (closed,
        /// asleep) counts as offline and leaves a <see cref="CrewOfflineReport"/>. Safe to call as often as you like.
        /// </summary>
        CrewUpdate Sync();

        /// <summary>Credits what is owed and writes the save (on quit or pause).</summary>
        void MarkSeen();

        /// <summary>The offline summary produced since it was last taken, or null. Taking it clears it.</summary>
        CrewOfflineReport TakeOfflineReport();
    }

    public sealed class LocalCrewService : ICrewService
    {
        /// <summary>
        /// The save is written at most this often for income alone (hires and quitting write at once). The income and
        /// its cursor are written together, so a crash loses nothing: the time is simply credited on the next start.
        /// </summary>
        private const long PersistIntervalMs = 30000;

        private readonly GameSession _session;
        private CrewOfflineReport _pendingOffline;
        private long _lastPersistAtMs;

        public LocalCrewService(GameSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _session.ConfigReplaced += (oldConfig, newConfig) => Settle(oldConfig);
            _session.SaveReset += () => _pendingOffline = null;

            // The time since the game last ran is credited now (offline income and its summary).
            Settle(Config);
            _session.Persist();
            _lastPersistAtMs = Now;
        }

        private PlayerSave Save => _session.Save;
        private GameConfig Config => _session.Config;
        private long Now => _session.Clock.UtcNowMs;

        public CrewView GetCrew(CrewBuyMode mode)
        {
            var config = Config;
            var crew = Save.Crew;
            var upgrades = Save.Upgrades;
            var fleet = CrewRules.FleetMultiplier(config, crew);
            var fewest = CrewRules.FewestUnits(config, crew);
            var fleetNext = CrewRules.NextMilestone(config.Crew.FleetMilestones, fewest);
            var view = new CrewView
            {
                CoinsPerSecond = CrewRules.CoinsPerSecond(config, crew, upgrades),
                XpPerSecond = CrewRules.XpPerSecond(config, crew, upgrades),
                CoinsEarned = crew.CoinsEarned,
                Coins = Save.Coins,
                Mode = mode,
                FleetMilestonesReached = CrewRules.FleetMilestonesReached(config, crew),
                FleetMultiplier = fleet,
                FleetNextMilestone = fleetNext,
                FleetPreviousMilestone = PreviousMilestone(config.Crew.FleetMilestones, fewest),
                FleetFewestUnits = fewest,
                MilestoneMultiplier = config.Crew.Milestones.Multiplier,
                FleetMilestoneMultiplier = config.Crew.FleetMilestones.Multiplier,
                MemberCount = config.Crew.Members.Count,
                OfflineFullRateHours = config.Crew.Offline.FullRateHours,
                OfflineReducedRate = config.Crew.Offline.ReducedRate,
                OfflineMaxHours = config.Crew.Offline.MaxHours + UpgradeRules.OfflineExtraHours(config, upgrades),
                UnlockPreviousCount = config.Crew.UnlockPreviousCount,
            };
            view.MilestoneCounts.AddRange(config.Crew.Milestones.Counts);

            for (var i = 0; i < config.Crew.Members.Count; i++)
            {
                var member = config.Crew.Members[i];
                var units = crew.UnitsOf(member.Id);
                if (fleetNext > 0 && units >= fleetNext)
                {
                    view.FleetMembersReady++;
                }

                var unlocked = CrewRules.IsUnlocked(config, crew, i);
                // "Máx": what the Moedas pay now; at least 1, so a price is always shown.
                var amount = mode == CrewBuyMode.Max ? Math.Max(1L, CrewRules.MaxAffordable(member, units, Save.Coins)) : AmountFor(mode);
                var cost = CrewRules.Cost(member, units, amount);
                var item = new CrewMemberView
                {
                    MemberId = member.Id,
                    Name = member.DisplayName,
                    NamePlural = member.PluralName,
                    Position = i + 1,
                    Units = units,
                    Unlocked = unlocked,
                    CoinsPerSecond = CrewRules.MemberCoinsPerSecond(config, member, units, fleet, upgrades),
                    Multiplier = CrewRules.MemberMultiplier(config, units) * UpgradeRules.CrewMemberMultiplier(config, upgrades, member.Id),
                    NextMilestone = CrewRules.NextMilestone(config.Crew.Milestones, units),
                    PreviousMilestone = PreviousMilestone(config.Crew.Milestones, units),
                    BuyAmount = amount,
                    BuyCost = cost,
                    BuyBlocker = !unlocked ? ServiceError.CrewMemberLocked : !CrewRules.CanPay(cost, Save.Coins) ? ServiceError.NotEnoughCoins : ServiceError.None,
                };

                if (i > 0)
                {
                    var previous = config.Crew.Members[i - 1];
                    item.UnlockAfterNamePlural = previous.PluralName;
                    item.UnlockAfterCount = config.Crew.UnlockPreviousCount;
                    item.UnlockAfterHave = crew.UnitsOf(previous.Id);
                }

                view.Members.Add(item);
            }

            return view;
        }

        public ServiceResult<CrewHireResult> Hire(string memberId, CrewBuyMode mode)
        {
            var config = Config;
            var index = config.Crew.Members.FindIndex(m => m.Id == memberId);
            if (index < 0)
            {
                return ServiceResult<CrewHireResult>.Fail(ServiceError.CrewMemberNotFound);
            }

            if (!Enum.IsDefined(typeof(CrewBuyMode), mode))
            {
                return ServiceResult<CrewHireResult>.Fail(ServiceError.InvalidAmount);
            }

            // What the Crew earned up to now is at the old rate.
            Settle(config);

            var crew = Save.Crew;
            if (!CrewRules.IsUnlocked(config, crew, index))
            {
                return ServiceResult<CrewHireResult>.Fail(ServiceError.CrewMemberLocked);
            }

            var member = config.Crew.Members[index];
            var units = crew.UnitsOf(member.Id);
            var amount = mode == CrewBuyMode.Max ? CrewRules.MaxAffordable(member, units, Save.Coins) : AmountFor(mode);
            var cost = amount > 0 ? CrewRules.Cost(member, units, amount) : CrewRules.Unaffordable;
            if (amount <= 0 || !CrewRules.CanPay(cost, Save.Coins))
            {
                return ServiceResult<CrewHireResult>.Fail(ServiceError.NotEnoughCoins);
            }

            var fleetBefore = CrewRules.FleetMilestonesReached(config, crew);
            var nextWasLocked = index + 1 < config.Crew.Members.Count && !CrewRules.IsUnlocked(config, crew, index + 1);
            var milestoneBefore = CrewRules.MilestonesReached(config.Crew.Milestones, units);

            Save.Coins -= cost;
            crew.Units[member.Id] = units + amount;

            var after = units + amount;
            var milestoneAfter = CrewRules.MilestonesReached(config.Crew.Milestones, after);
            var fleetAfter = CrewRules.FleetMilestonesReached(config, crew);
            var result = new CrewHireResult
            {
                MemberId = member.Id,
                Name = member.DisplayName,
                NamePlural = member.PluralName,
                Bought = amount,
                CoinsSpent = cost,
                Units = after,
                MilestoneReached = milestoneAfter > milestoneBefore ? config.Crew.Milestones.Counts[milestoneAfter - 1] : 0,
                FleetMilestoneReached = fleetAfter > fleetBefore ? config.Crew.FleetMilestones.Counts[fleetAfter - 1] : 0,
                UnlockedName = nextWasLocked && CrewRules.IsUnlocked(config, crew, index + 1) ? config.Crew.Members[index + 1].DisplayName : null,
                CoinsPerSecond = CrewRules.CoinsPerSecond(config, crew, Save.Upgrades),
                MilestoneMultiplier = config.Crew.Milestones.Multiplier,
                FleetMilestoneMultiplier = config.Crew.FleetMilestones.Multiplier,
            };

            Persist();
            _session.Log("Crew: hired " + amount + " x " + member.Id + " for " + cost + " coins (now " + after + ").");
            return ServiceResult<CrewHireResult>.Ok(result);
        }

        public CrewUpdate Sync()
        {
            var update = Settle(Config);
            if (Now - _lastPersistAtMs >= PersistIntervalMs)
            {
                Persist();
            }

            return update;
        }

        public void MarkSeen()
        {
            Settle(Config);
            Persist();
        }

        public CrewOfflineReport TakeOfflineReport()
        {
            var report = _pendingOffline;
            _pendingOffline = null;
            return report;
        }

        /// <summary>
        /// Credits what is owed at the current rates, before something changes them (an Upgrade bought, M24-T06). Does
        /// not write the save: the caller does, with its own change.
        /// </summary>
        internal void SettleNow() => Settle(Config);

        /// <summary>The owner's test tools (A-123): the time that just passed counts as time the game was open.</summary>
        internal void CreditAsOnline()
        {
            var crew = Save.Crew;
            var now = Now;
            if (crew.LastCreditedAtMs > 0 && now > crew.LastCreditedAtMs)
            {
                Credit(Config, (now - crew.LastCreditedAtMs) / 1000.0, new CrewUpdate().LevelsReached, out _, out _, out _);
            }

            crew.LastCreditedAtMs = Math.Max(crew.LastCreditedAtMs, now);
        }

        // ------------------------------------------------------------------ internals

        /// <summary>
        /// Credits the income since the cursor under <paramref name="config"/> and moves the cursor to now. A gap
        /// longer than online_gap_seconds is offline time (full rate, then the reduced rate, up to the cap).
        /// </summary>
        private CrewUpdate Settle(GameConfig config)
        {
            var update = new CrewUpdate();
            var crew = Save.Crew;
            var now = Now;
            if (crew.LastCreditedAtMs <= 0 || crew.LastCreditedAtMs > now)
            {
                // A new player, a save from before the Crew (v12) or a cursor in the future (edited save): start
                // counting from now. Nothing is owed for time before the cursor existed.
                crew.LastCreditedAtMs = now;
                return update;
            }

            var elapsedMs = now - crew.LastCreditedAtMs;
            if (elapsedMs <= 0)
            {
                return update;
            }

            crew.LastCreditedAtMs = now;
            if (elapsedMs <= (long)Math.Round(config.Crew.OnlineGapSeconds * 1000.0))
            {
                Credit(config, elapsedMs / 1000.0, update.LevelsReached, out var coins, out var xp, out var dollars);
                update.CoinsGained = coins;
                update.XpGained = xp;
                update.DollarsGained = dollars;
                return update;
            }

            // The game was not seen running: offline income, at the offline rate and cap (the Caixa Térmica raises it).
            var extraHours = UpgradeRules.OfflineExtraHours(config, Save.Upgrades);
            var share = CrewRules.OfflineSeconds(config.Crew, elapsedMs, extraHours);
            var report = _pendingOffline ?? new CrewOfflineReport
            {
                FullRateHours = config.Crew.Offline.FullRateHours,
                ReducedRate = config.Crew.Offline.ReducedRate,
                MaxHours = config.Crew.Offline.MaxHours + extraHours,
            };
            Credit(config, share.EffectiveSeconds, report.LevelsReached, out var offlineCoins, out var offlineXp, out var offlineDollars);
            report.AwayMs += elapsedMs;
            report.FullRateMs += (long)Math.Round(share.FullSeconds * 1000.0);
            report.ReducedRateMs += (long)Math.Round(share.ReducedSeconds * 1000.0);
            report.Capped |= share.Capped;
            report.CoinsGained += offlineCoins;
            report.XpGained += offlineXp;
            report.DollarsGained += offlineDollars;

            // A summary only when the Crew earned something: a player with no one hired sees only the fishing one.
            if (report.CoinsGained > 0 || report.XpGained > 0)
            {
                _pendingOffline = report;
            }

            _session.Log("Crew offline: " + (elapsedMs / 1000) + " s away, " + offlineCoins + " coins, " + offlineXp + " XP.");
            return update;
        }

        /// <summary>Adds <paramref name="seconds"/> of income: whole Moedas and XP now, the fractions kept for later.</summary>
        private void Credit(GameConfig config, double seconds, System.Collections.Generic.List<int> levels, out long coins, out long xp, out long dollars)
        {
            coins = 0;
            xp = 0;
            dollars = 0;
            if (!(seconds > 0))
            {
                return;
            }

            var crew = Save.Crew;
            coins = Whole(CrewRules.CoinsPerSecond(config, crew, Save.Upgrades) * seconds, crew.CoinsCarry, out var coinsCarry);
            crew.CoinsCarry = coinsCarry;
            xp = Whole(CrewRules.XpPerSecond(config, crew, Save.Upgrades) * seconds, crew.XpCarry, out var xpCarry);
            crew.XpCarry = xpCarry;

            if (coins > 0)
            {
                Save.Coins = SafeAdd(Save.Coins, coins);
                crew.CoinsEarned = SafeAdd(crew.CoinsEarned, coins);
            }

            if (xp > 0)
            {
                crew.XpEarned = SafeAdd(crew.XpEarned, xp);
                dollars = FisherLevelRules.AddXp(config, Save, xp, levels);
            }
        }

        /// <summary>The whole part of <paramref name="amount"/> + <paramref name="carry"/>; the fraction goes to <paramref name="newCarry"/>.</summary>
        private static long Whole(double amount, double carry, out double newCarry)
        {
            var total = amount + (carry > 0 && carry < 1 ? carry : 0);
            if (!(total > 0))
            {
                newCarry = 0;
                return 0;
            }

            if (total >= 9.2e18)
            {
                newCarry = 0;
                return long.MaxValue;
            }

            var whole = Math.Floor(total);
            newCarry = total - whole;
            return (long)whole;
        }

        private static long SafeAdd(long a, long b) => b > long.MaxValue - a ? long.MaxValue : a + b;

        /// <summary>Units of the fixed modes (×1, ×10, ×100). "Máx" depends on the Moedas and is solved apart.</summary>
        private static long AmountFor(CrewBuyMode mode)
        {
            switch (mode)
            {
                case CrewBuyMode.Ten: return 10;
                case CrewBuyMode.Hundred: return 100;
                default: return 1;
            }
        }

        private static int PreviousMilestone(CrewMilestonesConfig milestones, long units)
        {
            var reached = CrewRules.MilestonesReached(milestones, units);
            return reached > 0 ? milestones.Counts[reached - 1] : 0;
        }

        private void Persist()
        {
            _session.Persist();
            _lastPersistAtMs = Now;
        }
    }
}
