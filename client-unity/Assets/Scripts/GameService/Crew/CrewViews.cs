using System.Collections.Generic;
using FishingIdle.GameService.Core;

namespace FishingIdle.GameService.Crew
{
    /// <summary>How many units a Hire asks for: 1, 10, 100 or as many as the Moedas pay ("Máx").</summary>
    public enum CrewBuyMode
    {
        One,
        Ten,
        Hundred,
        Max,
    }

    /// <summary>The Crew window: totals, the fleet milestone and every member with the price for the chosen mode.</summary>
    public sealed class CrewView
    {
        /// <summary>Moedas per second of the whole Crew (all multipliers included).</summary>
        public double CoinsPerSecond { get; internal set; }

        /// <summary>Fisher XP per second of the whole Crew.</summary>
        public double XpPerSecond { get; internal set; }

        /// <summary>Every Moeda the Crew ever earned.</summary>
        public long CoinsEarned { get; internal set; }

        public long Coins { get; internal set; }
        public CrewBuyMode Mode { get; internal set; }

        /// <summary>Fleet milestones reached and the multiplier they give every member (1 when none).</summary>
        public int FleetMilestonesReached { get; internal set; }
        public double FleetMultiplier { get; internal set; }

        /// <summary>The next fleet milestone (units every member needs), or 0 when all are reached.</summary>
        public int FleetNextMilestone { get; internal set; }

        /// <summary>The previous fleet milestone (0 before the first), where the progress bar starts.</summary>
        public int FleetPreviousMilestone { get; internal set; }

        /// <summary>Members that already have <see cref="FleetNextMilestone"/> units, out of <see cref="MemberCount"/>.</summary>
        public int FleetMembersReady { get; internal set; }

        /// <summary>The fewest units any member has (the fleet milestone waits for this one).</summary>
        public long FleetFewestUnits { get; internal set; }

        /// <summary>What each milestone multiplies (crew.json; 2 = doubles).</summary>
        public double MilestoneMultiplier { get; internal set; }
        public double FleetMilestoneMultiplier { get; internal set; }

        public int MemberCount { get; internal set; }

        /// <summary>Units needed to unlock the next member, and the quantity milestones (crew.json), for the "Como funciona" text.</summary>
        public int UnlockPreviousCount { get; internal set; }
        public List<int> MilestoneCounts { get; } = new List<int>();

        /// <summary>The offline rules, for the "Como funciona" text.</summary>
        public double OfflineFullRateHours { get; internal set; }
        public double OfflineReducedRate { get; internal set; }
        public double OfflineMaxHours { get; internal set; }

        /// <summary>Every member, in hiring order (the window shows the unlocked ones and the next locked one).</summary>
        public List<CrewMemberView> Members { get; } = new List<CrewMemberView>();
    }

    public sealed class CrewMemberView
    {
        public string MemberId { get; internal set; }
        public string Name { get; internal set; }
        public string NamePlural { get; internal set; }

        /// <summary>1-based position in hiring order.</summary>
        public int Position { get; internal set; }

        public long Units { get; internal set; }
        public bool Unlocked { get; internal set; }

        /// <summary>While locked: the member to have and how many of it (and how many the player has).</summary>
        public string UnlockAfterNamePlural { get; internal set; }
        public int UnlockAfterCount { get; internal set; }
        public long UnlockAfterHave { get; internal set; }

        /// <summary>Moedas per second of all units of this member, every multiplier included.</summary>
        public double CoinsPerSecond { get; internal set; }

        /// <summary>Its own milestone multiplier (×2 per milestone reached; 1 when none).</summary>
        public double Multiplier { get; internal set; }

        /// <summary>The next quantity milestone, or 0 when all are reached; and the previous one (0 before the first).</summary>
        public int NextMilestone { get; internal set; }
        public int PreviousMilestone { get; internal set; }

        /// <summary>Units the chosen mode buys now (for "Máx", what the Moedas pay; at least 1 to show a price).</summary>
        public long BuyAmount { get; internal set; }

        /// <summary>Moedas for <see cref="BuyAmount"/>; <see cref="CrewRules.Unaffordable"/> when it does not fit a number.</summary>
        public long BuyCost { get; internal set; }

        /// <summary>Why Contratar is refused now (locked, not enough Moedas), or None.</summary>
        public ServiceError BuyBlocker { get; internal set; }
    }

    /// <summary>What one hire did.</summary>
    public sealed class CrewHireResult
    {
        public string MemberId { get; internal set; }
        public string Name { get; internal set; }
        public string NamePlural { get; internal set; }
        public long Bought { get; internal set; }
        public long CoinsSpent { get; internal set; }
        public long Units { get; internal set; }

        /// <summary>The quantity milestone just reached (0 when none) and the fleet milestone just reached (0 when none).</summary>
        public int MilestoneReached { get; internal set; }
        public int FleetMilestoneReached { get; internal set; }

        /// <summary>What a quantity milestone and a fleet milestone multiply (crew.json).</summary>
        public double MilestoneMultiplier { get; internal set; }
        public double FleetMilestoneMultiplier { get; internal set; }

        /// <summary>The member this hire unlocked, or null.</summary>
        public string UnlockedName { get; internal set; }

        /// <summary>The whole Crew's Moedas per second after the hire.</summary>
        public double CoinsPerSecond { get; internal set; }
    }

    /// <summary>Income credited while the game is open (since the last sync).</summary>
    public sealed class CrewUpdate
    {
        public long CoinsGained { get; internal set; }
        public long XpGained { get; internal set; }
        public List<int> LevelsReached { get; } = new List<int>();

        /// <summary>Dólares of the level milestones the Crew's XP reached (A-111).</summary>
        public long DollarsGained { get; internal set; }

        public bool HasChanges => CoinsGained > 0 || XpGained > 0;
    }

    /// <summary>What the Crew earned while the game was closed (the "Bem-vindo de volta" summary).</summary>
    public sealed class CrewOfflineReport
    {
        public long AwayMs { get; internal set; }

        /// <summary>Time at the full rate and at the reduced rate; past the cap it earned nothing.</summary>
        public long FullRateMs { get; internal set; }
        public long ReducedRateMs { get; internal set; }
        public bool Capped { get; internal set; }

        public double FullRateHours { get; internal set; }
        public double ReducedRate { get; internal set; }
        public double MaxHours { get; internal set; }

        public long CoinsGained { get; internal set; }
        public long XpGained { get; internal set; }
        public List<int> LevelsReached { get; } = new List<int>();
        public long DollarsGained { get; internal set; }
    }
}
