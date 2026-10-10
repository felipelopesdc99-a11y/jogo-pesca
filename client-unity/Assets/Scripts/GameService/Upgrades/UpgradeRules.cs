using System;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Crew;
using FishingIdle.GameService.Persistence;

namespace FishingIdle.GameService.Upgrades
{
    /// <summary>
    /// The Upgrades' arithmetic (M24-T06, A-155, TD-040): ids, names, prices and the multipliers they give the Crew,
    /// the fish sale and the offline cap. Pure functions of upgrades.json, crew.json and the levels bought; no clock,
    /// no roll, no save writes.
    /// </summary>
    /// <remarks>
    /// A Crew member's upgrade is level 1 once bought. A general upgrade's level is clamped to its max_level, so a
    /// balance change that lowers it never gives more than the file allows. Every effect is a double multiplier; the
    /// services round only the Moedas they credit. A null <see cref="UpgradesState"/> means nothing bought.
    /// </remarks>
    public static class UpgradeRules
    {
        /// <summary>The price of something no wallet can pay (it would not fit a long), as in the Crew.</summary>
        public const long Unaffordable = CrewRules.Unaffordable;

        /// <summary>The id of a Crew member's upgrade: "crew_02_up_3" is the Canoeiro's third (tier index 2).</summary>
        public static string CrewUpgradeId(string memberId, int tierIndex) => memberId + "_up_" + (tierIndex + 1);

        /// <summary>Finds the member and the tier of a Crew upgrade id; false when the id is not one.</summary>
        public static bool TryGetCrewUpgrade(GameConfig config, string upgradeId, out CrewMemberConfig member, out int tierIndex)
        {
            member = null;
            tierIndex = -1;
            if (string.IsNullOrEmpty(upgradeId))
            {
                return false;
            }

            var tiers = config.Upgrades.CrewUpgrades.Tiers;
            foreach (var candidate in config.Crew.Members)
            {
                if (!upgradeId.StartsWith(candidate.Id + "_up_", StringComparison.Ordinal))
                {
                    continue;
                }

                for (var i = 0; i < tiers.Count; i++)
                {
                    if (string.Equals(CrewUpgradeId(candidate.Id, i), upgradeId, StringComparison.Ordinal))
                    {
                        member = candidate;
                        tierIndex = i;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>"Anzol Afiado do Canoeiro": the tier's name and the member's suffix (the member's name when it has none).</summary>
        public static string CrewUpgradeName(GameConfig config, CrewMemberConfig member, int tierIndex)
        {
            var crewUpgrades = config.Upgrades.CrewUpgrades;
            var tier = crewUpgrades.Tiers[tierIndex];
            if (crewUpgrades.MemberSuffixes != null && crewUpgrades.MemberSuffixes.TryGetValue(member.Id, out var suffix) && !string.IsNullOrWhiteSpace(suffix))
            {
                return tier.Name + " " + suffix;
            }

            return tier.Name + " (" + member.DisplayName + ")";
        }

        /// <summary>
        /// Moedas of a Crew member's upgrade: cost_factor × the price of that member's unit number unlock_count
        /// (base_cost × cost_growth^unlock_count). <see cref="Unaffordable"/> when it does not fit a long.
        /// </summary>
        public static long CrewUpgradeCost(GameConfig config, CrewMemberConfig member, int tierIndex)
        {
            var tier = config.Upgrades.CrewUpgrades.Tiers[tierIndex];
            return ToPrice(tier.CostFactor * member.BaseCost * Math.Pow(member.CostGrowth, tier.UnlockCount));
        }

        /// <summary>Whether a Crew member's upgrade can be bought: the player has at least its unlock_count of that member.</summary>
        public static bool IsCrewUpgradeUnlocked(GameConfig config, CrewState crew, CrewMemberConfig member, int tierIndex)
        {
            return crew != null && crew.UnitsOf(member.Id) >= config.Upgrades.CrewUpgrades.Tiers[tierIndex].UnlockCount;
        }

        /// <summary>How many of a member's upgrades were bought.</summary>
        public static int CrewUpgradesBought(GameConfig config, UpgradesState upgrades, string memberId)
        {
            if (upgrades == null)
            {
                return 0;
            }

            var bought = 0;
            for (var i = 0; i < config.Upgrades.CrewUpgrades.Tiers.Count; i++)
            {
                if (upgrades.LevelOf(CrewUpgradeId(memberId, i)) > 0)
                {
                    bought++;
                }
            }

            return bought;
        }

        /// <summary>What a member's bought upgrades multiply its Moedas by (×2 each by default; 1 when none).</summary>
        public static double CrewMemberMultiplier(GameConfig config, UpgradesState upgrades, string memberId)
        {
            var bought = CrewUpgradesBought(config, upgrades, memberId);
            return bought == 0 ? 1.0 : Math.Pow(config.Upgrades.CrewUpgrades.Multiplier, bought);
        }

        /// <summary>A general upgrade's level, clamped to 0 … max_level.</summary>
        public static int GeneralLevel(GeneralUpgradeConfig upgrade, UpgradesState upgrades)
        {
            var level = upgrades?.LevelOf(upgrade.Id) ?? 0;
            return Math.Max(0, Math.Min(level, upgrade.MaxLevel));
        }

        /// <summary>
        /// Moedas to go from <paramref name="level"/> to the next: base_cost × cost_growth^level (level 0 → 1 costs
        /// base_cost). <see cref="Unaffordable"/> at the max level or when it does not fit a long.
        /// </summary>
        public static long GeneralCost(GeneralUpgradeConfig upgrade, int level)
        {
            if (level < 0 || level >= upgrade.MaxLevel)
            {
                return Unaffordable;
            }

            return ToPrice(upgrade.BaseCost * Math.Pow(upgrade.CostGrowth, level));
        }

        /// <summary>What every general upgrade with <paramref name="effect"/> adds together: Σ level × value_per_level.</summary>
        public static double EffectTotal(GameConfig config, UpgradesState upgrades, string effect)
        {
            double total = 0;
            if (upgrades == null)
            {
                return 0;
            }

            foreach (var upgrade in config.Upgrades.GeneralUpgrades)
            {
                if (string.Equals(upgrade.Effect, effect, StringComparison.Ordinal))
                {
                    total += GeneralLevel(upgrade, upgrades) * upgrade.ValuePerLevel;
                }
            }

            return total;
        }

        /// <summary>The Rádio do Porto: what the whole Crew's Moedas are multiplied by (1 + Σ%).</summary>
        public static double CrewCoinsMultiplier(GameConfig config, UpgradesState upgrades) => 1.0 + EffectTotal(config, upgrades, UpgradeEffects.CrewCoins);

        /// <summary>The Sonar de Cardume: what the Crew's Fisher XP is multiplied by.</summary>
        public static double CrewXpMultiplier(GameConfig config, UpgradesState upgrades) => 1.0 + EffectTotal(config, upgrades, UpgradeEffects.CrewXp);

        /// <summary>The Freguesia na Feira: what the NPC sale price of every fish (Fishing Box and Aquarium) is multiplied by.</summary>
        public static double FishSaleMultiplier(GameConfig config, UpgradesState upgrades) => 1.0 + EffectTotal(config, upgrades, UpgradeEffects.FishSale);

        /// <summary>The Maré Boa: what the Moedas of selling the Fishing Box are multiplied by.</summary>
        public static double FishingCoinsMultiplier(GameConfig config, UpgradesState upgrades) => 1.0 + EffectTotal(config, upgrades, UpgradeEffects.FishingCoins);

        /// <summary>Everything that multiplies the NPC price of a Fishing Box catch: Freguesia na Feira × Maré Boa.</summary>
        public static double BoxSaleMultiplier(GameConfig config, UpgradesState upgrades) => FishSaleMultiplier(config, upgrades) * FishingCoinsMultiplier(config, upgrades);

        /// <summary>The Caixa Térmica: hours added to the Crew's offline cap (counted at the reduced rate).</summary>
        public static double OfflineExtraHours(GameConfig config, UpgradesState upgrades) => EffectTotal(config, upgrades, UpgradeEffects.CrewOfflineHours);

        /// <summary>Rounds a price to a long; <see cref="Unaffordable"/> when it is not a number or does not fit.</summary>
        private static long ToPrice(double cost)
        {
            if (double.IsNaN(cost) || double.IsInfinity(cost) || cost >= 9.2e18)
            {
                return Unaffordable;
            }

            return Math.Max(1L, (long)Math.Round(cost, MidpointRounding.AwayFromZero));
        }
    }
}
