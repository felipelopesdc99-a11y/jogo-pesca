using System;
using System.Linq;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Persistence;

namespace FishingIdle.GameService
{
    /// <summary>
    /// The live, authoritative game state for one player: config + save + clock + storage.
    /// </summary>
    /// <remarks>
    /// Shared by the Local...Service classes. It is the only object that touches the save, and it
    /// writes the save after every mutation, so closing the game at any moment loses at most the
    /// cycle in progress.
    /// </remarks>
    public sealed class GameSession
    {
        private readonly IPlayerRepository _repository;
        private readonly Action<string> _log;

        public GameSession(GameConfig config, IPlayerRepository repository, IClock clock, Action<string> log)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            if (clock == null)
            {
                throw new ArgumentNullException(nameof(clock));
            }

            // Rules never see time going backwards (TD-030).
            Clock = new SteadyClock(new OffsetClock(clock, () => Save?.DevTimeOffsetMs ?? 0), () => Save?.ClockHighWaterMs ?? 0, ms =>
            {
                if (Save != null)
                {
                    Save.ClockHighWaterMs = ms;
                }
            });
            _log = log ?? (_ => { });
            LoadOrCreate();
        }

        public GameConfig Config { get; private set; }

        public IClock Clock { get; }

        /// <summary>
        /// The owner's test tools (A-123). Set by the game client only in the Unity Editor or a development
        /// build; off in a release build and never on a server.
        /// </summary>
        public bool DevToolsEnabled { get; set; }

        internal PlayerSave Save { get; private set; }

        public SaveLoadStatus LoadStatus { get; private set; }

        public string SaveLocation => _repository.Location;

        /// <summary>Raised after the config is swapped, so services can re-anchor running timers.</summary>
        internal event Action<GameConfig, GameConfig> ConfigReplaced;

        /// <summary>Raised after the save is reset, so services can drop anything they cached.</summary>
        internal event Action SaveReset;

        internal void Log(string message) => _log(message);

        /// <summary>Writes the current state. Called by services after every mutation.</summary>
        internal void Persist()
        {
            if (LoadStatus == SaveLoadStatus.TooNew)
            {
                // Never overwrite a save made by a newer game version.
                return;
            }

            Save.UpdatedAtMs = Clock.UtcNowMs;
            _repository.Save(Save);
        }

        /// <summary>
        /// Swaps in a new validated config (Dev Panel "Salvar e aplicar"). Services settle anything
        /// owed under the old config first.
        /// </summary>
        public void ReplaceConfig(GameConfig newConfig)
        {
            if (newConfig == null)
            {
                throw new ArgumentNullException(nameof(newConfig));
            }

            var old = Config;
            ConfigReplaced?.Invoke(old, newConfig);
            Config = newConfig;
            RepairAgainstConfig();
            Persist();
            _log("Config replaced: " + old.Version + " -> " + newConfig.Version);
        }

        /// <summary>Starts over with a brand-new player. The old save is kept aside, not deleted.</summary>
        public string ResetSave()
        {
            var keptCopy = _repository.Reset();
            Save = NewPlayer();
            LoadStatus = SaveLoadStatus.NotFound;
            Persist();
            SaveReset?.Invoke();
            _log("Save reset. Previous save kept at: " + (keptCopy ?? "(none)"));
            return keptCopy;
        }

        /// <summary>Equips the Starter Rod, adding it to the Inventory if the player does not own one.</summary>
        internal void EquipStarterRod() => EquipStarterRod(Save, Clock.UtcNowMs);

        private void EquipStarterRod(PlayerSave save, long now)
        {
            var starter = save.Inventory.Find(i => i.Kind == InventoryItem.KindRod && i.RodId == Config.StarterRod.Id);
            if (starter == null)
            {
                starter = new InventoryItem { Id = save.NextItemId++, Kind = InventoryItem.KindRod, RodId = Config.StarterRod.Id, Level = 1, AcquiredAtMs = now };
                save.Inventory.Add(starter);
            }

            save.EquippedRodItemId = starter.Id;
        }

        private void LoadOrCreate()
        {
            var result = _repository.Load();
            LoadStatus = result.Status;
            _log("Save load: " + result.Status + " (" + result.Detail + ")");

            switch (result.Status)
            {
                case SaveLoadStatus.Loaded:
                case SaveLoadStatus.RecoveredFromBackup:
                    Save = result.Save;
                    RepairAgainstConfig();
                    Persist();
                    break;
                case SaveLoadStatus.TooNew:
                    // Play on a throwaway in-memory player; Persist() refuses to write.
                    Save = NewPlayer();
                    break;
                default:
                    Save = NewPlayer();
                    Persist();
                    break;
            }
        }

        private PlayerSave NewPlayer()
        {
            var now = Clock.UtcNowMs;
            var save = new PlayerSave
            {
                PlayerId = Guid.NewGuid().ToString("N"),
                PlayerName = FishingIdle.Texts.GameTexts.Player.DefaultName,
                CreatedAtMs = now,
                UpdatedAtMs = now,
                RngSeed = Rng.NewSeed(),
                CurrentMapId = Config.StartingMap.Id,
            };
            // No rod yet: the tutorial has the player claim the free Starter Rod in the Shop (GDD section 40).
            for (var i = 0; i < Config.CardumeSize; i++)
            {
                save.CardumeSlots.Add(0);
            }

            return save;
        }

        /// <summary>
        /// Keeps a save usable after a config edit removed something it points at. Catches of a
        /// species that no longer exists stay in the box untouched (they come back if the species
        /// returns); only the map and rod fall back to the defaults.
        /// </summary>
        private void RepairAgainstConfig()
        {
            if (!Config.TryGetMap(Save.CurrentMapId, out _))
            {
                _log("Map '" + Save.CurrentMapId + "' no longer exists; moving player to " + Config.StartingMap.Id);
                Save.CurrentMapId = Config.StartingMap.Id;
            }

            var equipped = Save.EquippedRodItem();
            var waitingForTutorialRod = Save.EquippedRodItemId == 0 && !Save.Tutorial.Completed;
            if (!waitingForTutorialRod && (equipped == null || !Config.TryGetRod(equipped.RodId, out _)))
            {
                _log("Equipped rod is missing from the config; equipping " + Config.StarterRod.Id);
                EquipStarterRod(Save, Clock.UtcNowMs);
            }

            // The Cardume always has exactly one entry per position.
            while (Save.CardumeSlots.Count < Config.CardumeSize) Save.CardumeSlots.Add(0);
            while (Save.CardumeSlots.Count > Config.CardumeSize) Save.CardumeSlots.RemoveAt(Save.CardumeSlots.Count - 1);

            var maxLevel = Config.Progression.Fisher.MaxLevel;
            if (Save.FisherLevel > maxLevel)
            {
                Save.FisherLevel = maxLevel;
                Save.FisherXp = 0;
            }

            // Levels and charges stay inside what the config allows (an edited save, or a config that
            // lowered a maximum, cannot leave a fish or a rod above the top level).
            var fishMax = Config.Progression.FishLevel.MaxLevel;
            foreach (var fish in Save.Aquarium)
            {
                fish.Level = Math.Max(1, Math.Min(fishMax, fish.Level));
            }

            var market = Save.Market;
            var rodsOnMarket = market == null
                ? Enumerable.Empty<InventoryItem>()
                : market.MyListings.Select(l => l.Goods?.Rod)
                    .Concat(market.Withdrawals.Select(w => w.Goods?.Rod))
                    .Concat(market.Auctions.Where(a => a.SellerName == null).Select(a => a.Goods?.Rod))
                    .Where(r => r != null);
            foreach (var item in Save.Inventory.Where(i => i.Kind == InventoryItem.KindRod).Concat(rodsOnMarket))
            {
                if (Config.TryGetRod(item.RodId, out var rod))
                {
                    item.Level = Math.Max(1, Math.Min(Config.RodMaxLevel(rod), item.Level));
                }
            }

            if (Save.BaitCharges != null)
            {
                foreach (var bait in Save.BaitCharges.Keys.ToList())
                {
                    Save.BaitCharges[bait] = Math.Max(0, Save.BaitCharges[bait]);
                }
            }

            var unknown = Save.FishingBox.Count(c => !Config.TryGetSpecies(c.SpeciesId, out _));
            if (unknown > 0)
            {
                _log(unknown + " Fishing Box catches refer to species missing from the config; they are kept but cannot be sold until it returns.");
            }
        }
    }
}
