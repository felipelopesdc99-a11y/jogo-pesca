using System.Collections.Generic;
using FishingIdle.GameService.Fishing;

namespace FishingIdle.GameService.Aquarium
{
    /// <summary>How the Aquarium list is ordered on screen.</summary>
    public enum AquariumSort
    {
        /// <summary>GDD section 12: Exceptional → Large → Adult → Small, biggest for its species first.</summary>
        Size,
        Level,
        Species,
        Newest,
    }

    /// <summary>Read-only view of a kept fish, with its derived attributes.</summary>
    public sealed class FishView
    {
        public long FishId { get; internal set; }
        public string SpeciesId { get; internal set; }
        public string SpeciesName { get; internal set; }
        public string RarityId { get; internal set; }
        public string RarityName { get; internal set; }
        public string SizeCategoryId { get; internal set; }
        public string SizeCategoryName { get; internal set; }
        public double SizeCm { get; internal set; }
        public double SpeciesMinCm { get; internal set; }
        public double SpeciesMaxCm { get; internal set; }
        public double SizePercentile { get; internal set; }
        public int Level { get; internal set; }
        public int MaxLevel { get; internal set; }
        public long Xp { get; internal set; }

        /// <summary>XP to the next level; 0 at the maximum level.</summary>
        public long XpToNext { get; internal set; }

        public long InvestedXp { get; internal set; }
        public FishStats Stats { get; internal set; }
        public long SalePriceCoins { get; internal set; }

        /// <summary>XP this fish gives if consumed: own value + 50% of invested XP.</summary>
        public long FeedXp { get; internal set; }

        public bool IsValuableFood { get; internal set; }

        /// <summary>Special look: Exceptional size or a rarity above common.</summary>
        public bool IsImportant { get; internal set; }

        public long CaughtAtMs { get; internal set; }
        public long KeptAtMs { get; internal set; }

        /// <summary>Cardume position (1–6), or 0 when not in the Cardume.</summary>
        public int CardumePosition { get; internal set; }
    }

    public sealed class AquariumView
    {
        public int Capacity { get; internal set; }
        public List<FishView> Fish { get; } = new List<FishView>();
        public int Count => Fish.Count;
        public int FreeSlots => Capacity - Count;
    }

    public sealed class KeepResult
    {
        public List<FishView> Kept { get; } = new List<FishView>();
        public int AquariumCount { get; internal set; }
        public int Capacity { get; internal set; }
    }

    /// <summary>What feeding would do (or did): XP, resulting level, anything wasted, what needs confirming.</summary>
    public sealed class FeedPreview
    {
        public long TargetFishId { get; internal set; }
        public int FoodCount { get; internal set; }
        public long XpGained { get; internal set; }
        public int LevelBefore { get; internal set; }
        public long XpBefore { get; internal set; }
        public long XpToNextBefore { get; internal set; }
        public int LevelAfter { get; internal set; }
        public long XpAfter { get; internal set; }
        public long XpToNextAfter { get; internal set; }
        public int MaxLevel { get; internal set; }

        /// <summary>XP beyond the maximum level, which would be lost.</summary>
        public long WastedXp { get; internal set; }

        /// <summary>Names of valuable fish in the food, for the confirmation.</summary>
        public List<string> ValuableFood { get; } = new List<string>();

        /// <summary>Food that is in the Cardume; feeding removes it from there (GDD section 22).</summary>
        public List<string> CardumeFood { get; } = new List<string>();

        public bool NeedsConfirmation => ValuableFood.Count > 0 || CardumeFood.Count > 0 || WastedXp > 0;
    }
}
