using System;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Persistence;

namespace FishingIdle.GameService.Crew
{
    /// <summary>
    /// The Crew's arithmetic (M24-T05, A-154, TD-039): prices, milestones, income per second and the offline share.
    /// Pure functions of crew.json and the units hired; no clock, no roll, no save writes.
    /// </summary>
    /// <remarks>
    /// Prices use the closed form of a geometric series, b·r^k·(r^n − 1)/(r − 1), in double and rounded to a long;
    /// anything that does not fit a long is "unaffordable" (<see cref="Unaffordable"/>), never a wrapped number.
    /// The income is a double per second (the first members earn fractions of a Moeda); the service credits whole
    /// Moedas and keeps the fraction for the next credit.
    /// </remarks>
    public static class CrewRules
    {
        /// <summary>The price of something no wallet can pay (it would not fit a long).</summary>
        public const long Unaffordable = long.MaxValue;

        /// <summary>Upper bound of units bought in one purchase, a guard for "Máx" with a huge wallet.</summary>
        public const long MaxUnitsPerPurchase = 1_000_000;

        /// <summary>
        /// Moedas for <paramref name="amount"/> units of <paramref name="member"/> when the player already has
        /// <paramref name="owned"/>: b·r^owned·(r^amount − 1)/(r − 1), rounded. <see cref="Unaffordable"/> when it
        /// does not fit a long.
        /// </summary>
        public static long Cost(CrewMemberConfig member, long owned, long amount)
        {
            if (member == null || amount <= 0 || owned < 0)
            {
                return amount == 0 ? 0 : Unaffordable;
            }

            var r = member.CostGrowth;
            var cost = member.BaseCost * Math.Pow(r, owned) * (Math.Pow(r, amount) - 1.0) / (r - 1.0);
            if (double.IsNaN(cost) || cost >= 9.2e18)
            {
                return Unaffordable;
            }

            return Math.Max(1L, (long)Math.Round(cost, MidpointRounding.AwayFromZero));
        }

        /// <summary>
        /// The most units of <paramref name="member"/> that <paramref name="coins"/> pay for, from
        /// <paramref name="owned"/> on (0 when not even one). Solved in closed form, then checked against
        /// <see cref="Cost"/> so the answer is exactly what a purchase would charge.
        /// </summary>
        public static long MaxAffordable(CrewMemberConfig member, long owned, long coins)
        {
            if (member == null || coins <= 0 || !CanPay(Cost(member, owned, 1), coins))
            {
                return 0;
            }

            var r = member.CostGrowth;
            var first = member.BaseCost * Math.Pow(r, owned);
            var estimate = Math.Floor(Math.Log(1.0 + coins * (r - 1.0) / first) / Math.Log(r));
            var units = double.IsNaN(estimate) || estimate < 1 ? 1L : (long)Math.Min(estimate, MaxUnitsPerPurchase);

            // Rounding can put the estimate one off either way: step to the exact answer.
            while (units > 1 && !CanPay(Cost(member, owned, units), coins))
            {
                units--;
            }

            while (units < MaxUnitsPerPurchase && CanPay(Cost(member, owned, units + 1), coins))
            {
                units++;
            }

            return units;
        }

        /// <summary>Whether <paramref name="coins"/> pay <paramref name="cost"/>; an <see cref="Unaffordable"/> price never is, not even with every coin.</summary>
        public static bool CanPay(long cost, long coins) => cost != Unaffordable && cost <= coins;

        /// <summary>Whether <paramref name="index"/> (0-based, in crew.json order) can be hired: the first always; the rest with enough of the previous one.</summary>
        public static bool IsUnlocked(GameConfig config, CrewState crew, int index)
        {
            var members = config.Crew.Members;
            if (index <= 0)
            {
                return index == 0 && members.Count > 0;
            }

            return index < members.Count && crew.UnitsOf(members[index - 1].Id) >= config.Crew.UnlockPreviousCount;
        }

        /// <summary>How many of <paramref name="counts"/> <paramref name="units"/> has reached.</summary>
        public static int MilestonesReached(CrewMilestonesConfig milestones, long units)
        {
            var reached = 0;
            if (milestones?.Counts == null)
            {
                return 0;
            }

            foreach (var count in milestones.Counts)
            {
                if (units >= count)
                {
                    reached++;
                }
            }

            return reached;
        }

        /// <summary>The next count above <paramref name="units"/>, or 0 when every one is reached.</summary>
        public static int NextMilestone(CrewMilestonesConfig milestones, long units)
        {
            if (milestones?.Counts == null)
            {
                return 0;
            }

            foreach (var count in milestones.Counts)
            {
                if (units < count)
                {
                    return count;
                }
            }

            return 0;
        }

        /// <summary>The fewest units any member has (every member in crew.json counts, hired or not).</summary>
        public static long FewestUnits(GameConfig config, CrewState crew)
        {
            var fewest = long.MaxValue;
            foreach (var member in config.Crew.Members)
            {
                fewest = Math.Min(fewest, crew.UnitsOf(member.Id));
            }

            return fewest == long.MaxValue ? 0 : fewest;
        }

        /// <summary>Fleet milestones reached: every member with at least the count.</summary>
        public static int FleetMilestonesReached(GameConfig config, CrewState crew)
        {
            return MilestonesReached(config.Crew.FleetMilestones, FewestUnits(config, crew));
        }

        /// <summary>The fleet multiplier on every member's Moedas (×2 per fleet milestone by default).</summary>
        public static double FleetMultiplier(GameConfig config, CrewState crew)
        {
            return Math.Pow(config.Crew.FleetMilestones.Multiplier, FleetMilestonesReached(config, crew));
        }

        /// <summary>A member's own multiplier from its quantity milestones (×2 per milestone by default).</summary>
        public static double MemberMultiplier(GameConfig config, long units)
        {
            return Math.Pow(config.Crew.Milestones.Multiplier, MilestonesReached(config.Crew.Milestones, units));
        }

        /// <summary>Moedas per second of one member with <paramref name="units"/> hired: base × units × its milestones × the fleet.</summary>
        public static double MemberCoinsPerSecond(GameConfig config, CrewMemberConfig member, long units, double fleetMultiplier)
        {
            if (units <= 0)
            {
                return 0;
            }

            return member.CoinsPerSecond * units * MemberMultiplier(config, units) * fleetMultiplier;
        }

        /// <summary>Moedas per second of the whole Crew.</summary>
        public static double CoinsPerSecond(GameConfig config, CrewState crew)
        {
            var fleet = FleetMultiplier(config, crew);
            double total = 0;
            foreach (var member in config.Crew.Members)
            {
                total += MemberCoinsPerSecond(config, member, crew.UnitsOf(member.Id), fleet);
            }

            return total;
        }

        /// <summary>Fisher XP per second of the whole Crew (milestones do not multiply XP).</summary>
        public static double XpPerSecond(GameConfig config, CrewState crew)
        {
            double total = 0;
            foreach (var member in config.Crew.Members)
            {
                total += member.XpPerSecond * crew.UnitsOf(member.Id);
            }

            return total;
        }

        /// <summary>
        /// The seconds of income that <paramref name="awayMs"/> with the game closed are worth: the first
        /// full_rate_hours at 100%, then reduced_rate up to max_hours away, nothing after that.
        /// </summary>
        public static OfflineShare OfflineSeconds(CrewConfig crew, long awayMs)
        {
            var away = Math.Max(0L, awayMs) / 1000.0;
            var offline = crew.Offline;
            var full = Math.Min(away, offline.FullRateHours * 3600.0);
            var reduced = Math.Max(0.0, Math.Min(away, offline.MaxHours * 3600.0) - offline.FullRateHours * 3600.0);
            return new OfflineShare(full, reduced, full + reduced * offline.ReducedRate, away > offline.MaxHours * 3600.0);
        }
    }

    /// <summary>How an absence counts for the Crew's income.</summary>
    public readonly struct OfflineShare
    {
        public OfflineShare(double fullSeconds, double reducedSeconds, double effectiveSeconds, bool capped)
        {
            FullSeconds = fullSeconds;
            ReducedSeconds = reducedSeconds;
            EffectiveSeconds = effectiveSeconds;
            Capped = capped;
        }

        /// <summary>Seconds at the full rate.</summary>
        public double FullSeconds { get; }

        /// <summary>Seconds at the reduced rate.</summary>
        public double ReducedSeconds { get; }

        /// <summary>What the absence is worth in seconds of full income.</summary>
        public double EffectiveSeconds { get; }

        /// <summary>Whether part of the absence went past the cap and earned nothing.</summary>
        public bool Capped { get; }
    }
}
