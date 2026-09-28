using System.Collections.Generic;
using Newtonsoft.Json;

namespace FishingIdle.GameService.Persistence
{
    /// <summary>
    /// Everything that belongs to the player, exactly as written to the local save file.
    /// </summary>
    /// <remarks>
    /// Only the game service reads or writes this. Presentation code receives read-only views
    /// built from it, never the object itself. Values derivable from /config (prices, XP,
    /// names) are not stored, so a balance change applies to existing catches.
    /// </remarks>
    public sealed class PlayerSave
    {
        /// <summary>Format version of this file. Bump when the shape changes; see SaveMigrations.</summary>
        public const int CurrentVersion = 3;

        public int SaveVersion { get; set; } = CurrentVersion;
        public string PlayerId { get; set; }
        public string PlayerName { get; set; }
        public long CreatedAtMs { get; set; }
        public long UpdatedAtMs { get; set; }

        /// <summary>Base seed for every roll this player gets. Never shown, never changed.</summary>
        public ulong RngSeed { get; set; }

        public long Coins { get; set; }
        public long Shells { get; set; }

        public int FisherLevel { get; set; } = 1;

        /// <summary>XP accumulated inside the current level.</summary>
        public long FisherXp { get; set; }

        public long FisherXpTotal { get; set; }

        public string CurrentMapId { get; set; }

        /// <summary>Pre-version-3 rod slot, read only to migrate old saves. Never written.</summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public EquippedRodState EquippedRod { get; set; }

        /// <summary>Everything the player owns that is not a fish (GDD section 20). V0.1: rods.</summary>
        public List<InventoryItem> Inventory { get; set; } = new List<InventoryItem>();

        public long NextItemId { get; set; } = 1;

        /// <summary>The Inventory item in the only equipment slot, the Fishing Rod.</summary>
        public long EquippedRodItemId { get; set; }

        /// <summary>
        /// The one Cardume: six positions (index 0 = position 1), each an Aquarium fish id or 0 when
        /// empty. Positions 1–3 are the front row, 4–6 the back row (GDD section 26).
        /// </summary>
        public List<long> CardumeSlots { get; set; } = new List<long>();

        public FishingSessionState Fishing { get; set; } = new FishingSessionState();

        /// <summary>Next id handed to a Fishing Box catch. Ids are never reused.</summary>
        public long NextCatchId { get; set; } = 1;

        public List<BoxCatch> FishingBox { get; set; } = new List<BoxCatch>();

        /// <summary>
        /// Per-species history: first catch and largest specimen. Kept from the very first catch
        /// because it cannot be reconstructed later; the Encyclopedia (Milestone 3) displays it.
        /// </summary>
        public Dictionary<string, SpeciesRecord> SpeciesRecords { get; set; } = new Dictionary<string, SpeciesRecord>();

        /// <summary>Next id handed to an Aquarium fish. Ids are never reused.</summary>
        public long NextFishId { get; set; } = 1;

        /// <summary>Fish kept in the Aquarium: full persistent entities (GDD sections 12–13). Added in save version 2.</summary>
        public List<FishInstance> Aquarium { get; set; } = new List<FishInstance>();

        public PlayerStats Stats { get; set; } = new PlayerStats();

        /// <summary>The equipped rod item, or null if the reference is broken.</summary>
        public InventoryItem EquippedRodItem()
        {
            return Inventory?.Find(i => i.Id == EquippedRodItemId && i.Kind == InventoryItem.KindRod);
        }
    }

    public sealed class InventoryItem
    {
        public const string KindRod = "rod";

        public long Id { get; set; }
        public string Kind { get; set; }
        public string RodId { get; set; }

        /// <summary>Internal rod level (1–10); rods without levels stay at 1.</summary>
        public int Level { get; set; } = 1;

        public long AcquiredAtMs { get; set; }

        /// <summary>Coins spent buying and upgrading it; part of it comes back on NPC resale (Milestone 4).</summary>
        public long CoinsInvested { get; set; }
    }

    public sealed class EquippedRodState
    {
        public string RodId { get; set; }
        public int Level { get; set; } = 1;
    }

    /// <summary>
    /// The authoritative fishing cursor. Catches are derived from timestamps and this cursor, never
    /// from per-frame timers, so a repeated or early request cannot produce extra fish.
    /// </summary>
    public sealed class FishingSessionState
    {
        public bool Active { get; set; }

        /// <summary>Counts every session ever started; part of the RNG stream so sessions never repeat.</summary>
        public long SessionIndex { get; set; }

        /// <summary>When the current run of cycles started (Unix ms). Cycle N completes at start + N × cycle.</summary>
        public long StartedAtMs { get; set; }

        /// <summary>Length of one cycle for this run, frozen at start so a config edit cannot rewrite history.</summary>
        public long CycleMs { get; set; }

        /// <summary>How many cycles of this run have already produced a catch.</summary>
        public long CyclesProcessed { get; set; }

        /// <summary>The last moment the game was seen running. Time after it is not online time.</summary>
        public long LastSeenAtMs { get; set; }
    }

    /// <summary>
    /// A catch waiting in the Fishing Box. Deliberately compact (GDD section 11): it is not a
    /// full fish entity, and anything derivable from the species config is not repeated here.
    /// </summary>
    public sealed class BoxCatch
    {
        [JsonProperty("id")] public long Id { get; set; }
        [JsonProperty("sp")] public string SpeciesId { get; set; }

        /// <summary>Exact size in millimetres.</summary>
        [JsonProperty("mm")] public int SizeMm { get; set; }

        /// <summary>
        /// Size category at catch time. Stored rather than derived so later threshold edits do not
        /// silently reclassify fish the player already has.
        /// </summary>
        [JsonProperty("cat")] public string SizeCategoryId { get; set; }

        [JsonProperty("t")] public long CaughtAtMs { get; set; }

        /// <summary>Bit flags, see <see cref="BoxCatchFlags"/>.</summary>
        [JsonProperty("f")] public int Flags { get; set; }
    }

    /// <summary>
    /// A fish the player kept. It exists only from the moment it is moved to the Aquarium and keeps
    /// its identity until consumed or sold. Combat stats are not stored: they are derived from
    /// species, rarity, size and level, so identical inputs always give identical stats.
    /// </summary>
    public sealed class FishInstance
    {
        public long Id { get; set; }
        public string SpeciesId { get; set; }
        public int SizeMm { get; set; }
        public string SizeCategoryId { get; set; }
        public int Level { get; set; } = 1;

        /// <summary>XP inside the current level.</summary>
        public long Xp { get; set; }

        /// <summary>All XP ever fed to this fish; half comes back when it is consumed (GDD section 22).</summary>
        public long InvestedXp { get; set; }

        public long CaughtAtMs { get; set; }
        public long KeptAtMs { get; set; }

        /// <summary>The Fishing Box catch it came from, for audit.</summary>
        public long SourceCatchId { get; set; }
    }

    public static class BoxCatchFlags
    {
        public const int NewSpecies = 1;
        public const int PersonalRecord = 2;
    }

    public sealed class SpeciesRecord
    {
        public long FirstCaughtAtMs { get; set; }
        public int LargestMm { get; set; }
        public long TimesCaught { get; set; }
    }

    public sealed class PlayerStats
    {
        public long TotalCatches { get; set; }
        public long FishSold { get; set; }
        public long CoinsFromSales { get; set; }

        /// <summary>Catches of Exceptional size (counted from save version 3 on).</summary>
        public long ExceptionalCatches { get; set; }

        /// <summary>Catches above common rarity (counted from save version 3 on).</summary>
        public long RareCatches { get; set; }
    }
}
