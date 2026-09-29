using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FishingIdle.GameService.Config;
using FishingIdle.Texts;
using Newtonsoft.Json;

namespace FishingIdle.GameService.Persistence
{
    /// <summary>How the save was obtained when the game started.</summary>
    public enum SaveLoadStatus
    {
        /// <summary>No save existed; the caller should create a new player.</summary>
        NotFound,

        /// <summary>The main save file was read and is valid.</summary>
        Loaded,

        /// <summary>The main file was damaged; the backup was valid and was used instead.</summary>
        RecoveredFromBackup,

        /// <summary>Both files were damaged. They were set aside and a new player starts.</summary>
        Unrecoverable,

        /// <summary>The save was written by a newer version of the game. Nothing may overwrite it.</summary>
        TooNew,
    }

    public sealed class SaveLoadResult
    {
        public SaveLoadResult(SaveLoadStatus status, PlayerSave save, string detail)
        {
            Status = status;
            Save = save;
            Detail = detail;
        }

        public SaveLoadStatus Status { get; }
        public PlayerSave Save { get; }

        /// <summary>Technical detail for logs (English).</summary>
        public string Detail { get; }
    }

    /// <summary>
    /// Where player state lives. Today a JSON file on the PC; later a remote server. The game
    /// service only talks to this interface.
    /// </summary>
    public interface IPlayerRepository
    {
        /// <summary>Human-readable location of the save, for the Dev Panel.</summary>
        string Location { get; }

        bool Exists { get; }

        SaveLoadResult Load();

        void Save(PlayerSave save);

        /// <summary>Removes the current save so the next start creates a new player. Returns where a copy was kept.</summary>
        string Reset();
    }

    /// <summary>
    /// Local JSON save with a version number, a rolling backup and basic validation.
    /// </summary>
    /// <remarks>
    /// Every write goes to a temporary file first; only a completely written file replaces the
    /// real save, and the previous save becomes the backup. A damaged file is never deleted: it is
    /// renamed aside with a timestamp so it can be inspected.
    /// </remarks>
    public sealed class JsonFilePlayerRepository : IPlayerRepository
    {
        public const string SaveFileName = "player_save.json";
        public const string BackupFileName = "player_save.backup.json";
        private const string TempFileName = "player_save.tmp";

        private readonly string _directory;
        private readonly IClockTimestamp _stamp;

        public JsonFilePlayerRepository(string directory)
            : this(directory, new UtcTimestamp())
        {
        }

        internal JsonFilePlayerRepository(string directory, IClockTimestamp stamp)
        {
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
            _stamp = stamp;
        }

        public string Location => SavePath;

        public string SavePath => Path.Combine(_directory, SaveFileName);

        public string BackupPath => Path.Combine(_directory, BackupFileName);

        public bool Exists => File.Exists(SavePath);

        public SaveLoadResult Load()
        {
            if (!File.Exists(SavePath) && !File.Exists(BackupPath))
            {
                return new SaveLoadResult(SaveLoadStatus.NotFound, null, "no save files");
            }

            var main = TryRead(SavePath);
            if (main.Save != null)
            {
                return new SaveLoadResult(SaveLoadStatus.Loaded, main.Save, SavePath);
            }

            if (main.TooNew)
            {
                return new SaveLoadResult(SaveLoadStatus.TooNew, null, main.Problem);
            }

            var backup = TryRead(BackupPath);
            if (backup.TooNew)
            {
                return new SaveLoadResult(SaveLoadStatus.TooNew, null, backup.Problem);
            }

            if (File.Exists(SavePath))
            {
                SetAside(SavePath, "corrupt");
            }

            if (backup.Save != null)
            {
                return new SaveLoadResult(
                    SaveLoadStatus.RecoveredFromBackup,
                    backup.Save,
                    "main save unusable (" + main.Problem + "); loaded backup");
            }

            if (File.Exists(BackupPath))
            {
                SetAside(BackupPath, "corrupt");
            }

            return new SaveLoadResult(
                SaveLoadStatus.Unrecoverable,
                null,
                "main: " + main.Problem + "; backup: " + backup.Problem);
        }

        public void Save(PlayerSave save)
        {
            var problems = SaveValidator.Validate(save);
            if (problems.Count > 0)
            {
                // Refusing to write keeps the last good save intact. This indicates a bug in the rules.
                throw new InvalidOperationException("Refusing to write an invalid save: " + string.Join("; ", problems));
            }

            Directory.CreateDirectory(_directory);
            var json = JsonConvert.SerializeObject(save, JsonSettings.Default);
            var temp = Path.Combine(_directory, TempFileName);

            File.WriteAllText(temp, json, new UTF8Encoding(false));

            if (File.Exists(SavePath))
            {
                File.Copy(SavePath, BackupPath, true);
            }

            File.Copy(temp, SavePath, true);
            File.Delete(temp);
        }

        public string Reset()
        {
            string keptCopy = null;
            if (File.Exists(SavePath))
            {
                keptCopy = SetAside(SavePath, "reset");
            }

            if (File.Exists(BackupPath))
            {
                File.Delete(BackupPath);
            }

            return keptCopy;
        }

        private ReadAttempt TryRead(string path)
        {
            if (!File.Exists(path))
            {
                return ReadAttempt.Failed("missing");
            }

            string json;
            try
            {
                json = File.ReadAllText(path, Encoding.UTF8);
            }
            catch (IOException exception)
            {
                return ReadAttempt.Failed("unreadable: " + exception.Message);
            }

            PlayerSave save;
            try
            {
                save = JsonConvert.DeserializeObject<PlayerSave>(json, JsonSettings.Default);
            }
            catch (JsonException exception)
            {
                return ReadAttempt.Failed("invalid JSON: " + exception.Message);
            }

            if (save == null)
            {
                return ReadAttempt.Failed("empty");
            }

            if (save.SaveVersion > PlayerSave.CurrentVersion)
            {
                return new ReadAttempt(null, "save_version " + save.SaveVersion + " is newer than " + PlayerSave.CurrentVersion, true);
            }

            SaveMigrations.Upgrade(save);

            var problems = SaveValidator.Validate(save);
            return problems.Count == 0
                ? new ReadAttempt(save, null, false)
                : ReadAttempt.Failed("invalid: " + string.Join("; ", problems));
        }

        private string SetAside(string path, string reason)
        {
            var name = Path.GetFileNameWithoutExtension(path) + "." + reason + "-" + _stamp.Now() + ".json";
            var target = Path.Combine(_directory, name);
            File.Copy(path, target, true);
            File.Delete(path);
            return target;
        }

        private sealed class ReadAttempt
        {
            public ReadAttempt(PlayerSave save, string problem, bool tooNew)
            {
                Save = save;
                Problem = problem;
                TooNew = tooNew;
            }

            public PlayerSave Save { get; }
            public string Problem { get; }
            public bool TooNew { get; }

            public static ReadAttempt Failed(string problem) => new ReadAttempt(null, problem, false);
        }
    }

    /// <summary>File-name timestamps for set-aside copies.</summary>
    internal interface IClockTimestamp
    {
        string Now();
    }

    internal sealed class UtcTimestamp : IClockTimestamp
    {
        public string Now() => DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Upgrades older save formats in place. Each future format change adds one step here.</summary>
    internal static class SaveMigrations
    {
        public static void Upgrade(PlayerSave save)
        {
            if (save.SaveVersion == 1)
            {
                // v2 adds the Aquarium (Milestone 2).
                save.Aquarium = new List<FishInstance>();
                save.NextFishId = 1;
                save.SaveVersion = 2;
            }

            if (save.SaveVersion == 2)
            {
                // v3 moves the rod into the Inventory and adds the Cardume (Milestone 3).
                save.Inventory = new List<InventoryItem>();
                save.NextItemId = 1;
                if (save.EquippedRod != null && !string.IsNullOrWhiteSpace(save.EquippedRod.RodId))
                {
                    var item = new InventoryItem
                    {
                        Id = save.NextItemId++,
                        Kind = InventoryItem.KindRod,
                        RodId = save.EquippedRod.RodId,
                        Level = Math.Max(1, save.EquippedRod.Level),
                        AcquiredAtMs = save.CreatedAtMs,
                    };
                    save.Inventory.Add(item);
                    save.EquippedRodItemId = item.Id;
                }

                save.EquippedRod = null;
                save.CardumeSlots = new List<long>();
                save.SaveVersion = 3;
            }

            if (save.SaveVersion == 3)
            {
                // v4 adds map travel and splits rod spending into purchase and upgrades (Milestone 4).
                save.Travel = new TravelState();
                foreach (var item in save.Inventory ?? new List<InventoryItem>())
                {
                    item.CoinsInvested = null;
                }

                save.SaveVersion = 4;
            }

            if (save.SaveVersion == 4)
            {
                // v5 adds Expeditions (Milestone 6).
                save.Expedition = new ExpeditionState();
                save.SaveVersion = 5;
            }

            save.Expedition = save.Expedition ?? new ExpeditionState();
            save.Travel = save.Travel ?? new TravelState();
            save.Aquarium = save.Aquarium ?? new List<FishInstance>();
            save.Inventory = save.Inventory ?? new List<InventoryItem>();
            save.CardumeSlots = save.CardumeSlots ?? new List<long>();
            save.FishingBox = save.FishingBox ?? new List<BoxCatch>();
            save.SpeciesRecords = save.SpeciesRecords ?? new Dictionary<string, SpeciesRecord>();
            save.Stats = save.Stats ?? new PlayerStats();
            save.Fishing = save.Fishing ?? new FishingSessionState();
        }
    }

    /// <summary>Structural checks on a save. Rule-level checks belong to the service.</summary>
    public static class SaveValidator
    {
        public static List<string> Validate(PlayerSave save)
        {
            var problems = new List<string>();
            if (save == null)
            {
                problems.Add("save is null");
                return problems;
            }

            if (save.SaveVersion < 1 || save.SaveVersion > PlayerSave.CurrentVersion)
            {
                problems.Add("unsupported save_version " + save.SaveVersion);
            }

            if (string.IsNullOrWhiteSpace(save.PlayerId)) problems.Add("player_id missing");
            if (save.Coins < 0) problems.Add("coins negative");
            if (save.Shells < 0) problems.Add("shells negative");
            if (save.FisherLevel < 1) problems.Add("fisher_level below 1");
            if (save.FisherXp < 0 || save.FisherXpTotal < 0) problems.Add("fisher xp negative");
            if (string.IsNullOrWhiteSpace(save.CurrentMapId)) problems.Add("current_map_id missing");
            if (save.Inventory == null) problems.Add("inventory missing");
            else
            {
                var itemIds = save.Inventory.Where(i => i != null).Select(i => i.Id).ToList();
                if (save.Inventory.Any(i => i == null || string.IsNullOrWhiteSpace(i.Kind))) problems.Add("inventory has malformed entries");
                if (itemIds.Count != itemIds.Distinct().Count()) problems.Add("inventory has duplicate ids");
                if (itemIds.Any(id => id <= 0 || id >= save.NextItemId)) problems.Add("inventory id outside issued range");
                if (save.EquippedRodItem() == null) problems.Add("equipped rod is not a rod in the inventory");
            }

            if (save.Expedition == null) problems.Add("expedition missing");
            else if (save.Expedition.Active && (string.IsNullOrWhiteSpace(save.Expedition.ExpeditionId) || save.Expedition.EndsAtMs < save.Expedition.StartedAtMs)) problems.Add("expedition malformed");
            if (save.Travel == null) problems.Add("travel missing");
            else if (save.Travel.Active && (string.IsNullOrWhiteSpace(save.Travel.ToMapId) || save.Travel.ArrivesAtMs < save.Travel.StartedAtMs)) problems.Add("travel malformed");
            if (save.Inventory != null && save.Inventory.Any(i => i != null && (i.PurchasePriceCoins < 0 || i.UpgradeCoinsInvested < 0 || i.Level < 1))) problems.Add("inventory has negative values");
            if (save.CardumeSlots == null) problems.Add("cardume missing");
            else if (save.Aquarium != null)
            {
                var placed = save.CardumeSlots.Where(id => id != 0).ToList();
                if (placed.Count != placed.Distinct().Count()) problems.Add("cardume repeats a fish");
                if (placed.Any(id => save.Aquarium.All(f => f == null || f.Id != id))) problems.Add("cardume points to a fish not in the aquarium");
            }
            if (save.Fishing == null) problems.Add("fishing missing");
            else if (save.Fishing.CyclesProcessed < 0) problems.Add("fishing cycles negative");
            if (save.FishingBox == null) problems.Add("fishing_box missing");
            else
            {
                if (save.FishingBox.Any(c => c == null || string.IsNullOrWhiteSpace(c.SpeciesId) || c.SizeMm <= 0))
                {
                    problems.Add("fishing_box has malformed entries");
                }

                var ids = save.FishingBox.Where(c => c != null).Select(c => c.Id).ToList();
                if (ids.Count != ids.Distinct().Count()) problems.Add("fishing_box has duplicate ids");
                if (ids.Any(id => id <= 0 || id >= save.NextCatchId)) problems.Add("fishing_box id outside issued range");
            }

            if (save.SpeciesRecords == null) problems.Add("species_records missing");
            if (save.Aquarium == null) problems.Add("aquarium missing");
            else
            {
                if (save.Aquarium.Any(f => f == null || string.IsNullOrWhiteSpace(f.SpeciesId) || f.SizeMm <= 0 || f.Level < 1 || f.Xp < 0 || f.InvestedXp < 0))
                {
                    problems.Add("aquarium has malformed entries");
                }

                var fishIds = save.Aquarium.Where(f => f != null).Select(f => f.Id).ToList();
                if (fishIds.Count != fishIds.Distinct().Count()) problems.Add("aquarium has duplicate ids");
                if (fishIds.Any(id => id <= 0 || id >= save.NextFishId)) problems.Add("aquarium id outside issued range");
            }
            if (save.Stats == null) problems.Add("stats missing");

            return problems;
        }
    }

    /// <summary>PT-BR description of a load outcome, for the game and the Dev Panel.</summary>
    public static class SaveLoadStatusText
    {
        public static string Describe(SaveLoadStatus status) => GameTexts.Save.Status(status.ToString());
    }
}
