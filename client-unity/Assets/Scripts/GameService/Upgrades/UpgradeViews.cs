using System.Collections.Generic;
using FishingIdle.GameService.Core;

namespace FishingIdle.GameService.Upgrades
{
    /// <summary>The Upgrades tab: the next Crew upgrades, every general upgrade with its next level, and what was bought.</summary>
    public sealed class UpgradesView
    {
        public long Coins { get; internal set; }

        /// <summary>The next Crew members' upgrades: the cheapest unlocked ones first, then the cheapest locked ones (at most 3).</summary>
        public List<UpgradeOfferView> NextCrew { get; } = new List<UpgradeOfferView>();

        /// <summary>Every general upgrade, in upgrades.json order, with its next level (or at the max level).</summary>
        public List<UpgradeOfferView> General { get; } = new List<UpgradeOfferView>();

        /// <summary>What was bought: the Crew members' upgrades in hiring and tier order, then the general ones with a level.</summary>
        public List<UpgradeOfferView> Bought { get; } = new List<UpgradeOfferView>();

        /// <summary>Crew members' upgrades bought and how many exist.</summary>
        public int CrewBoughtCount { get; internal set; }
        public int CrewTotalCount { get; internal set; }

        /// <summary>Crew members' upgrades on sale now (unlocked and not bought), beyond the ones in <see cref="NextCrew"/>.</summary>
        public int CrewAvailableCount { get; internal set; }

        /// <summary>What a Crew member's upgrade multiplies (upgrades.json; 2 = doubles), for the "i" text.</summary>
        public double CrewMultiplier { get; internal set; }

        /// <summary>The units of a member that unlock each tier (upgrades.json), for the "i" text.</summary>
        public List<int> CrewUnlockCounts { get; } = new List<int>();
    }

    /// <summary>One upgrade, to buy or already bought.</summary>
    public sealed class UpgradeOfferView
    {
        public string UpgradeId { get; internal set; }
        public string Name { get; internal set; }

        /// <summary>Whether it is a Crew member's ×2 (bought once) or a general upgrade (with levels).</summary>
        public bool IsCrewUpgrade { get; internal set; }

        /// <summary>
        /// What it does, as a stored key: "crew_member" for a Crew member's ×2, or a general upgrade's effect
        /// (crew_coins, fish_sale, crew_offline_hours, crew_xp, fishing_coins). The window turns it into text.
        /// </summary>
        public string EffectKey { get; internal set; }

        /// <summary>
        /// The effect of the next level (or, when bought out, of one level): the multiplier of a Crew member's upgrade
        /// (2), a fraction for the "+%" ones (0.25 = +25%) or hours for the Caixa Térmica.
        /// </summary>
        public double EffectValue { get; internal set; }

        /// <summary>The total effect at the current level (0 when none): Σ level × value_per_level, or the multiplier.</summary>
        public double EffectTotal { get; internal set; }

        /// <summary>The member a Crew upgrade belongs to ("Canoeiro"), or null.</summary>
        public string MemberName { get; internal set; }
        public string MemberId { get; internal set; }

        /// <summary>Level now and the maximum (a Crew member's upgrade: 0 or 1, of 1).</summary>
        public int Level { get; internal set; }
        public int MaxLevel { get; internal set; }

        /// <summary>Moedas of the next level; <see cref="UpgradeRules.Unaffordable"/> at the max level or past a number.</summary>
        public long Cost { get; internal set; }

        /// <summary>While locked: units of the member needed, and how many the player has.</summary>
        public int UnlockCount { get; internal set; }
        public long UnlockHave { get; internal set; }
        public string MemberNamePlural { get; internal set; }

        /// <summary>Why Comprar is refused now (locked, at the max level, already bought, not enough Moedas), or None.</summary>
        public ServiceError BuyBlocker { get; internal set; }
    }

    /// <summary>What one purchase did.</summary>
    public sealed class UpgradeBuyResult
    {
        public string UpgradeId { get; internal set; }
        public string Name { get; internal set; }
        public bool IsCrewUpgrade { get; internal set; }
        public string EffectKey { get; internal set; }
        public double EffectValue { get; internal set; }
        public string MemberName { get; internal set; }

        /// <summary>The level reached and the maximum.</summary>
        public int Level { get; internal set; }
        public int MaxLevel { get; internal set; }

        public long CoinsSpent { get; internal set; }

        /// <summary>The whole Crew's Moedas per second after the purchase.</summary>
        public double CrewCoinsPerSecond { get; internal set; }
    }
}
