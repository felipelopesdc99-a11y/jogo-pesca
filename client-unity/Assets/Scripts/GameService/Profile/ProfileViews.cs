using System.Collections.Generic;
using FishingIdle.GameService.Aquarium;
using FishingIdle.GameService.Fishing;

namespace FishingIdle.GameService.Profile
{
    public sealed class CardumeSlotView
    {
        /// <summary>1–6. Positions 1–3 are the front row, 4–6 the back row.</summary>
        public int Position { get; internal set; }
        public bool IsFront { get; internal set; }

        /// <summary>The fish in this position, or null when empty.</summary>
        public FishView Fish { get; internal set; }

        /// <summary>Attributes with the active Cardume modifiers (the 6/6 bonus).</summary>
        public FishStats EffectiveStats { get; internal set; }

        /// <summary>This fish's share of the private Cardume Strength.</summary>
        public long Strength { get; internal set; }
    }

    /// <summary>The player's own Cardume. Contains the private Strength: never build this for another player.</summary>
    public sealed class CardumeView
    {
        public List<CardumeSlotView> Slots { get; } = new List<CardumeSlotView>();
        public int Filled { get; internal set; }
        public int Size { get; internal set; }
        public bool CompleteBonusActive { get; internal set; }
        public int CompleteBonusSlots { get; internal set; }
        public double CompleteBonusPercent { get; internal set; }
        public long Strength { get; internal set; }
    }

    public sealed class RodItemView
    {
        public long ItemId { get; internal set; }
        public string RodId { get; internal set; }
        public string Name { get; internal set; }
        public int Tier { get; internal set; }
        public bool HasLevels { get; internal set; }
        public int Level { get; internal set; }
        public int MaxLevel { get; internal set; }
        public double RarityBonus { get; internal set; }
        public double SizeBonus { get; internal set; }
        public double ShellBonus { get; internal set; }
        public bool CanCatchRare { get; internal set; }
        public bool CanCatchEpic { get; internal set; }
        public bool GeneratesShells { get; internal set; }
        public bool IsEquipped { get; internal set; }

        /// <summary>Whether it can be used on the map the player is on (GDD section 19).</summary>
        public bool AllowedOnCurrentMap { get; internal set; }

        /// <summary>Coins for the next internal level; 0 at the max level or for rods without levels.</summary>
        public long NextUpgradeCost { get; internal set; }

        /// <summary>Conchas for the next internal level, on top of the coins.</summary>
        public long NextUpgradeShells { get; internal set; }

        /// <summary>What the game pays for it (NPC resale).</summary>
        public long ResaleValue { get; internal set; }

        /// <summary>Can be sold to the game or destroyed (never the equipped rod nor the Starter Rod).</summary>
        public bool CanDispose { get; internal set; }
    }

    public sealed class EncyclopediaEntryView
    {
        public string SpeciesId { get; internal set; }
        public bool Discovered { get; internal set; }

        // Filled only when discovered: before that the entry is a dark silhouette (GDD section 38).
        public string Name { get; internal set; }
        public string MapName { get; internal set; }
        public string RarityId { get; internal set; }
        public string RarityName { get; internal set; }
        public double LargestCm { get; internal set; }
        public double SpeciesMaxCm { get; internal set; }
        public long TimesCaught { get; internal set; }
        public long FirstCaughtAtMs { get; internal set; }
    }

    public sealed class RecordsView
    {
        public long TotalCatches { get; internal set; }
        public int SpeciesDiscovered { get; internal set; }
        public int SpeciesTotal { get; internal set; }
        public string BiggestSpeciesId { get; internal set; }
        public string BiggestSpeciesName { get; internal set; }
        public double BiggestCm { get; internal set; }
        public int HighestFishLevel { get; internal set; }
        public long ExceptionalCatches { get; internal set; }
        public long PerfectCatches { get; internal set; }
        public long RareCatches { get; internal set; }
        public long FishSold { get; internal set; }
        public long CoinsFromSales { get; internal set; }
    }

    /// <summary>The player's own Profile (GDD section 37, own view).</summary>
    public sealed class ProfileView
    {
        public string PlayerName { get; internal set; }
        public int FisherLevel { get; internal set; }
        public string MapName { get; internal set; }

        /// <summary>Private: shown only to the owner, never on a public profile or to opponents.</summary>
        public long CardumeStrength { get; internal set; }

        public RodItemView EquippedRod { get; internal set; }
        public List<RodItemView> Inventory { get; } = new List<RodItemView>();
        public List<EncyclopediaEntryView> Encyclopedia { get; } = new List<EncyclopediaEntryView>();
        public RecordsView Records { get; internal set; }
    }
}
