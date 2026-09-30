using System.Collections.Generic;

namespace FishingIdle.GameService.Fishing
{
    // Read-only answers the game service hands to the presentation layer. They carry display-ready
    // facts (names in PT-BR from /config, prices, sizes) so the client never needs to compute a
    // value with gameplay meaning. They are snapshots: changing one changes nothing in the game.

    public sealed class PlayerView
    {
        public string PlayerName { get; internal set; }
        public int FisherLevel { get; internal set; }
        public int FisherMaxLevel { get; internal set; }
        public long FisherXp { get; internal set; }

        /// <summary>XP needed for the next level; 0 at the maximum level.</summary>
        public long FisherXpToNext { get; internal set; }

        public long Coins { get; internal set; }
        public long Shells { get; internal set; }
        public string MapId { get; internal set; }
        public string MapName { get; internal set; }
        public string RodName { get; internal set; }
        public int RodLevel { get; internal set; }
        public bool RodHasLevels { get; internal set; }
        public long TotalCatches { get; internal set; }
        public int FishingBoxCount { get; internal set; }
        public int SpeciesDiscovered { get; internal set; }
        public int AquariumCount { get; internal set; }
        public int AquariumCapacity { get; internal set; }
    }

    public sealed class FishingStatus
    {
        public bool IsFishing { get; internal set; }

        /// <summary>Game-service time when this status was produced (Unix ms).</summary>
        public long NowMs { get; internal set; }

        /// <summary>When the cycle in progress began. Meaningful only while fishing.</summary>
        public long CycleStartedAtMs { get; internal set; }

        /// <summary>When the cycle in progress will produce its catch.</summary>
        public long NextCatchAtMs { get; internal set; }

        public double CycleSeconds { get; internal set; }
        public string MapName { get; internal set; }

        /// <summary>0–1 progress of the current cycle, for the progress bar and the animation.</summary>
        public double CycleProgress
        {
            get
            {
                if (!IsFishing || NextCatchAtMs <= CycleStartedAtMs)
                {
                    return 0;
                }

                var progress = (double)(NowMs - CycleStartedAtMs) / (NextCatchAtMs - CycleStartedAtMs);
                return progress < 0 ? 0 : progress > 1 ? 1 : progress;
            }
        }
    }

    public sealed class CatchView
    {
        public long CatchId { get; internal set; }
        public string SpeciesId { get; internal set; }
        public string SpeciesName { get; internal set; }
        public string RarityId { get; internal set; }
        public string RarityName { get; internal set; }
        public string SizeCategoryId { get; internal set; }
        public string SizeCategoryName { get; internal set; }
        public double SizeCm { get; internal set; }
        public double SpeciesMinCm { get; internal set; }
        public double SpeciesMaxCm { get; internal set; }

        /// <summary>Where the size sits inside the species range, 0–1 (for the size bar).</summary>
        public double SizePercentile { get; internal set; }

        public long SalePriceCoins { get; internal set; }

        /// <summary>XP this catch gives when used as food.</summary>
        public long FeedXp { get; internal set; }

        /// <summary>Asks for confirmation before being consumed as food.</summary>
        public bool IsValuableFood { get; internal set; }

        public long CaughtAtMs { get; internal set; }
        public bool IsNewSpecies { get; internal set; }
        public bool IsPersonalRecord { get; internal set; }

        /// <summary>
        /// For a personal record reported at catch time: the species' previous best size, so the
        /// celebration can show "antigo → novo". 0 otherwise (and in later views of the Fishing Box).
        /// </summary>
        public double PreviousRecordCm { get; internal set; }

        /// <summary>Catches the Fishing Box asks to confirm before a bulk sale (rare, Exceptional…).</summary>
        public bool IsProtected { get; internal set; }

        /// <summary>Deserves the special glow: Exceptional size, higher rarity or a first catch.</summary>
        public bool IsImportant { get; internal set; }
    }

    /// <summary>Everything that happened as a result of a sync, start or stop.</summary>
    public sealed class FishingUpdate
    {
        public List<CatchView> NewCatches { get; } = new List<CatchView>();
        public List<int> LevelsReached { get; } = new List<int>();
        public long XpGained { get; internal set; }
        public long ShellsGained { get; internal set; }

        /// <summary>Fish that bit and escaped (docs/SISTEMA_SUCESSO_PESCA.md): nothing of them was kept.</summary>
        public List<EscapeView> Escapes { get; } = new List<EscapeView>();

        /// <summary>The name of the bait whose last charge was used in this update, or null.</summary>
        public string BaitRanOut { get; internal set; }

        public bool HasChanges => NewCatches.Count > 0 || Escapes.Count > 0 || BaitRanOut != null;
    }

    /// <summary>
    /// A fish that bit and got away. Only its rarity is told (the species stays a mystery), so the
    /// screen can show how big the fight was.
    /// </summary>
    public sealed class EscapeView
    {
        public string RarityId { get; internal set; }
        public string RarityName { get; internal set; }

        /// <summary>The chance the player had of pulling it out (0 to 1).</summary>
        public double Chance { get; internal set; }
        public long AtMs { get; internal set; }
    }

    /// <summary>What was fished while the game was closed (GDD section 10, offline fishing).</summary>
    public sealed class OfflineReport
    {
        /// <summary>How long the game was away (closed, asleep).</summary>
        public long AwayMs { get; internal set; }

        /// <summary>The part of that time that counted: at most the offline cap (24 h).</summary>
        public long CountedMs { get; internal set; }

        public bool Capped { get; internal set; }
        public double CycleSeconds { get; internal set; }
        public FishingUpdate Update { get; internal set; }
    }

    public sealed class SalePreview
    {
        public int Count { get; internal set; }
        public long TotalCoins { get; internal set; }
        public List<CatchView> ProtectedCatches { get; } = new List<CatchView>();

        /// <summary>For Aquarium sales: valuable fish in the sale, already described for the dialog.</summary>
        public List<string> ProtectedFishNames { get; } = new List<string>();

        public bool NeedsConfirmation => ProtectedCatches.Count > 0 || ProtectedFishNames.Count > 0;
    }

    public sealed class SaleResult
    {
        public int Count { get; internal set; }
        public long CoinsGained { get; internal set; }
        public long NewBalance { get; internal set; }
    }
}
