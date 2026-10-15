using System;
using System.Collections.Generic;
using System.Linq;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;

namespace FishingIdle.GameService.Persistence
{
    /// <summary>What giving back the removed Crew and Upgrades changed (for the log and the tests).</summary>
    public sealed class CrewRemovalChange
    {
        /// <summary>Whether the save still had Crew or Upgrades data (a save from before v21).</summary>
        public bool HadData { get; internal set; }

        /// <summary>Crew units refunded, summed over every member.</summary>
        public long UnitsRefunded { get; internal set; }

        /// <summary>Upgrade levels refunded (a Crew member's upgrade counts as 1).</summary>
        public long UpgradeLevelsRefunded { get; internal set; }

        /// <summary>Moedas given back for the Crew.</summary>
        public long CrewCoins { get; internal set; }

        /// <summary>Moedas given back for the Upgrades.</summary>
        public long UpgradeCoins { get; internal set; }

        /// <summary>Everything given back (both parts, capped at long.MaxValue).</summary>
        public long CoinsRefunded => SafeMath.Add(CrewCoins, UpgradeCoins);

        /// <summary>Ids the price list does not know (an edited save): kept out of the refund.</summary>
        public int UnknownIds { get; internal set; }
    }

    /// <summary>
    /// The Crew (M24-T05) and the Upgrades (M24-T06) left the game in M24-T46 (A-172, TD-053). A save from before (versions 13
    /// to 20) still carries what the player had of them; this gives back the Moedas they cost and clears them. Prices come
    /// from economy.json → crew_removal_refund (the crew.json and upgrades.json of before), with the same formulas the game
    /// charged: a member's n units cost b·(rⁿ − 1)/(r − 1); a member's upgrade, cost_factor × b × r^unlock_count; a general
    /// upgrade's levels 1 to L, Σ base × growth^(n−1) (L capped at max_level). Only Moedas: neither system cost Conchas.
    /// Every sum saturates at long.MaxValue (<see cref="SafeMath"/>). Runs when the session loads the save, because it needs
    /// the config; once cleared, the data is gone, so it never refunds twice.
    /// </summary>
    public static class CrewRemovalMigration
    {
        private const double LongLimit = 9.2e18;

        public static CrewRemovalChange Refund(GameConfig config, PlayerSave save)
        {
            var change = new CrewRemovalChange();
            var crew = save.RetiredCrew;
            var upgrades = save.RetiredUpgrades;
            if (crew == null && upgrades == null)
            {
                return change;
            }

            change.HadData = true;
            var prices = config.Economy.CrewRemovalRefund ?? new CrewRemovalRefundConfig();
            var members = (prices.CrewMembers ?? new List<RetiredCrewMemberConfig>()).Where(m => m != null && m.Id != null)
                .ToDictionary(m => m.Id, StringComparer.Ordinal);
            var tiers = prices.CrewUpgradeTiers ?? new List<RetiredCrewUpgradeTierConfig>();
            var general = (prices.GeneralUpgrades ?? new List<RetiredGeneralUpgradeConfig>()).Where(g => g != null && g.Id != null)
                .ToDictionary(g => g.Id, StringComparer.Ordinal);

            foreach (var pair in crew?.Units ?? new Dictionary<string, long>())
            {
                if (pair.Value <= 0)
                {
                    continue;
                }

                if (pair.Key == null || !members.TryGetValue(pair.Key, out var member))
                {
                    change.UnknownIds++;
                    continue;
                }

                change.UnitsRefunded = SafeMath.Add(change.UnitsRefunded, pair.Value);
                change.CrewCoins = SafeMath.Add(change.CrewCoins, UnitsCost(member, pair.Value));
            }

            foreach (var pair in upgrades?.Levels ?? new Dictionary<string, int>())
            {
                if (pair.Value <= 0)
                {
                    continue;
                }

                if (pair.Key != null && general.TryGetValue(pair.Key, out var upgrade))
                {
                    var levels = Math.Min(pair.Value, Math.Max(0, upgrade.MaxLevel));
                    change.UpgradeLevelsRefunded += levels;
                    change.UpgradeCoins = SafeMath.Add(change.UpgradeCoins, GeneralCost(upgrade, levels));
                }
                else if (TryCrewUpgrade(pair.Key, members, tiers, out var member, out var tier))
                {
                    change.UpgradeLevelsRefunded++;
                    change.UpgradeCoins = SafeMath.Add(change.UpgradeCoins, ToCoins(tier.CostFactor * member.BaseCost * Math.Pow(member.CostGrowth, tier.UnlockCount)));
                }
                else
                {
                    change.UnknownIds++;
                }
            }

            save.Coins = SafeMath.Add(Math.Max(0, save.Coins), change.CoinsRefunded);
            save.RetiredCrew = null;
            save.RetiredUpgrades = null;
            return change;
        }

        /// <summary>Moedas of a member's first <paramref name="units"/> units: b·(rⁿ − 1)/(r − 1).</summary>
        internal static long UnitsCost(RetiredCrewMemberConfig member, long units)
        {
            var r = member.CostGrowth;
            if (units <= 0 || !(r > 1))
            {
                return 0;
            }

            return ToCoins(member.BaseCost * (Math.Pow(r, units) - 1.0) / (r - 1.0));
        }

        /// <summary>Moedas of a general upgrade's levels 1 to <paramref name="levels"/>: Σ base × growth^(n−1).</summary>
        internal static long GeneralCost(RetiredGeneralUpgradeConfig upgrade, int levels)
        {
            long total = 0;
            for (var n = 1; n <= levels; n++)
            {
                total = SafeMath.Add(total, ToCoins(upgrade.BaseCost * Math.Pow(upgrade.CostGrowth, n - 1)));
            }

            return total;
        }

        /// <summary>"crew_02_up_3": the Canoeiro's third upgrade (tier index 2), as the old UpgradeRules.CrewUpgradeId.</summary>
        private static bool TryCrewUpgrade(string id, Dictionary<string, RetiredCrewMemberConfig> members, List<RetiredCrewUpgradeTierConfig> tiers,
            out RetiredCrewMemberConfig member, out RetiredCrewUpgradeTierConfig tier)
        {
            member = null;
            tier = null;
            var cut = id?.LastIndexOf("_up_", StringComparison.Ordinal) ?? -1;
            if (cut <= 0 || !members.TryGetValue(id.Substring(0, cut), out member)
                || !int.TryParse(id.Substring(cut + 4), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var number)
                || number < 1 || number > tiers.Count || tiers[number - 1] == null)
            {
                member = null;
                return false;
            }

            tier = tiers[number - 1];
            return true;
        }

        /// <summary>A price in Moedas, rounded as the game charged it; long.MaxValue when it does not fit (never wraps).</summary>
        private static long ToCoins(double value)
        {
            if (double.IsNaN(value) || value <= 0)
            {
                return 0;
            }

            if (double.IsInfinity(value) || value >= LongLimit)
            {
                return long.MaxValue;
            }

            return (long)Math.Round(value, MidpointRounding.AwayFromZero);
        }
    }
}
