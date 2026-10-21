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
        private readonly IClock _rawClock;
        private readonly ulong? _fixedSeed;

        /// <summary>How far ahead of the PC clock the game time may be before the screen warns (5 minutes, TD-052).</summary>
        public const long ClockFrozenWarningSeconds = 300;

        public GameSession(GameConfig config, IPlayerRepository repository, IClock clock, Action<string> log, ulong? rngSeed = null)
        {
            _fixedSeed = rngSeed;
            Config = config ?? throw new ArgumentNullException(nameof(config));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            if (clock == null)
            {
                throw new ArgumentNullException(nameof(clock));
            }

            // Rules never see time going backwards (TD-030).
            _rawClock = new OffsetClock(clock, () => Save?.DevTimeOffsetMs ?? 0);
            Clock = new SteadyClock(_rawClock, () => Save?.ClockHighWaterMs ?? 0, ms =>
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
        /// Seconds the game time is ahead of the PC clock (TD-030 keeps it from going back, so the game time stays
        /// frozen until the PC clock catches up). 0 when the PC clock is at or past the game time (TD-052).
        /// </summary>
        public long ClockFrozenSeconds
        {
            get
            {
                var highest = Save?.ClockHighWaterMs ?? 0;
                var ahead = highest - _rawClock.UtcNowMs;
                return ahead > 0 ? ahead / 1000 : 0;
            }
        }

        /// <summary>True when the game time is frozen for more than 5 minutes: the screen shows a warning (TD-052).</summary>
        public bool ClockFrozen => ClockFrozenSeconds > ClockFrozenWarningSeconds;

        /// <summary>
        /// The owner's test tools (A-123). Set by the game client only in the Unity Editor or a development
        /// build; off in a release build and never on a server.
        /// </summary>
        public bool DevToolsEnabled { get; set; }

        internal PlayerSave Save { get; private set; }

        public SaveLoadStatus LoadStatus { get; private set; }

        public string SaveLocation => _repository.Location;

        /// <summary>
        /// What the save had of the removed Crew and Upgrades and the Moedas given back for it, when this start converted
        /// a save from before v21 (M24-T46, A-172); null otherwise. The screen shows it once, as a notice.
        /// </summary>
        public CrewRemovalChange CrewRefund { get; private set; }

        /// <summary>Raised after the config is swapped, so services can re-anchor running timers.</summary>
        internal event Action<GameConfig, GameConfig> ConfigReplaced;

        /// <summary>Raised after the save is reset, so services can drop anything they cached.</summary>
        internal event Action SaveReset;

        /// <summary>
        /// Raised by every <see cref="Persist"/> just before the save is written: the Missões (M24-T54) mark the goals the
        /// change reached, in the same write.
        /// </summary>
        internal event Action BeforePersist;

        internal void Log(string message) => _log(message);

        /// <summary>Writes the current state. Called by services after every mutation.</summary>
        internal void Persist()
        {
            if (LoadStatus == SaveLoadStatus.TooNew)
            {
                // Never overwrite a save made by a newer game version.
                return;
            }

            BeforePersist?.Invoke();
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
                RngSeed = _fixedSeed ?? Rng.NewSeed(),
                CurrentMapId = Config.StartingMap.Id,
                FisherXpCurveVersion = Config.Progression.Fisher.XpCurveVersion,
                RodLevelCurveVersion = Config.Rods.UpgradeRules?.LevelCurveVersion ?? 0,
                FishLevelCurveVersion = Config.Progression.FishLevel.LevelCurveVersion,
                BaitModelVersion = Config.BaitModelVersion,
                BoatModelVersion = Config.BoatModelVersion,
                Pass = new PassState { PassId = Config.Pass.PassId },
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

            // The Crew and the Upgrades left the game (M24-T46, A-172): a save from before gets back the Moedas they cost.
            var refund = Persistence.CrewRemovalMigration.Refund(Config, Save);
            if (refund.HadData)
            {
                CrewRefund = refund;
                _log("Crew and Upgrades removed: " + refund.UnitsRefunded + " crew units and " + refund.UpgradeLevelsRefunded + " upgrade levels refunded ("
                     + refund.CrewCoins + " + " + refund.UpgradeCoins + " coins; " + refund.UnknownIds + " unknown ids ignored).");
            }

            // A save from an older XP curve moves to the current one: total XP → level, never down (TD-038).
            var curve = Fishing.FisherLevelRules.MoveToCurrentCurve(Config, Save);
            if (curve != null)
            {
                _log("Fisher XP curve " + Save.FisherXpCurveVersion + ": level " + curve.LevelBefore + " -> " + curve.LevelAfter
                     + "; the pass milestones crossed are claimable in the Passe do Pescador.");
            }

            // Rods and fish from the old Nv.1–Nv.10 move to the current level curve, never down (M24-T13, TD-041).
            var levels = Persistence.LevelCurveMigration.MoveToCurrentCurves(Config, Save);
            if (levels.RodsMoved > 0 || levels.FishMoved > 0)
            {
                _log("Level curves: " + levels.RodsMoved + " rods and " + levels.FishMoved + " fish moved to the current curve.");
            }

            // Boats 6 to 10 from before M24-T42 become Moedas, Conchas and fragments of boats 1 to 5 (A-168), before the levels
            // below are clamped (an unknown boat would drop to Nv.1).
            var boats = Persistence.BoatModelMigration.MoveToCurrentModel(Config, Save);
            if (boats.BoatsRefunded > 0 || boats.FragmentsMoved > 0)
            {
                _log("Boat model " + Config.BoatModelVersion + ": " + boats.BoatsRefunded + " removed boats refunded (" + boats.CoinsRefunded + " coins, "
                     + boats.ShellsRefunded + " shells), " + boats.FragmentsMoved + " fragments moved to today's boats.");
            }

            // Fish from before the genes (M24-T52, A-176, TD-056) get the neutral 16/16/16/16; a broken set too.
            var neutralGenes = Persistence.GeneMigration.GiveMissingGenes(Config, Save);
            if (neutralGenes > 0)
            {
                _log("Genes: " + neutralGenes + " fish without genes got the neutral value " + Config.Progression.Genes.MigrationNeutralValue + ".");
            }

            // The Missões (M24-T54, A-177): a save from before version 24 fills the counters its state already shows, after the
            // genes above (the stars of the fish it has); the missions those prove reached show up ready to claim.
            if (Save.Missions != null && Save.Missions.SeedPending)
            {
                var seeded = Persistence.MissionMigration.SeedFromSave(Config, Save);
                _log("Missions: counters seeded from the old save (" + seeded + " counters).");
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

            if (Save.BoatLevels != null)
            {
                foreach (var boatId in Save.BoatLevels.Keys.ToList())
                {
                    var boatMax = Config.TryGetBoat(boatId, out var boat) ? Config.BoatMaxLevel(boat) : 1;
                    Save.BoatLevels[boatId] = Math.Max(1, Math.Min(boatMax, Save.BoatLevels[boatId]));
                }
            }

            // Boats by fragments and the pass (M24-T16/T17): no negative counts; the pass progress belongs to the pass in
            // the config (a save from before the pass gets its id).
            if (Save.BoatFragments != null)
            {
                foreach (var boatId in Save.BoatFragments.Keys.ToList())
                {
                    Save.BoatFragments[boatId] = Math.Max(0, Save.BoatFragments[boatId]);
                }
            }

            // Nothing is assembled on load (M24-T58, A-180): fragments already enough wait for the player's "Montar".

            // Rods and clothing by fragments (M24-T32/T33): no negative counts and levels inside the config's maximum.
            if (Save.RodFragments != null)
            {
                foreach (var rodId in Save.RodFragments.Keys.ToList())
                {
                    Save.RodFragments[rodId] = Math.Max(0, Save.RodFragments[rodId]);
                }
            }

            if (Save.Clothing != null)
            {
                Save.Clothing.Fragments = Math.Max(0, Save.Clothing.Fragments);
                foreach (var piece in Save.Clothing.Pieces.Values.Where(p => p != null))
                {
                    piece.Level = Math.Max(1, Math.Min(Config.ClothingMaxLevel, piece.Level));
                    piece.Xp = Math.Max(0, piece.Xp);
                }
            }

            // The Clãs (M24-T49): a clan reference that points nowhere is cleared, a missing fish leaves the clan's Cardume.
            if (Save.Clans != null && Clans.ClanRules.Repair(Save))
            {
                _log("Clan state repaired on load.");
            }

            if (Save.Pass != null && string.IsNullOrEmpty(Save.Pass.PassId))
            {
                Save.Pass.PassId = Config.Pass.PassId;
            }

            // Baits from before M24-T30 become today's, by value; the one in use stays in use (A-161).
            var baits = Persistence.BaitModelMigration.MoveToCurrentModel(Config, Save);
            if (baits.BaitsConverted > 0)
            {
                _log("Bait model " + Config.BaitModelVersion + ": " + baits.BaitsConverted + " old baits converted to today's.");
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
