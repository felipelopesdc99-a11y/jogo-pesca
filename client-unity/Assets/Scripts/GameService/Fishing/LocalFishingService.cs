using System;
using System.Collections.Generic;
using System.Linq;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Persistence;

namespace FishingIdle.GameService.Fishing
{
    /// <summary>
    /// Fishing as the player can ask for it: start, stop, sync, look at the box, sell.
    /// </summary>
    /// <remarks>
    /// The client only expresses intent through this interface. It never says "I caught X":
    /// catches come out of <see cref="Sync"/>, computed from timestamps and the saved cycle cursor.
    /// A future RemoteFishingService implements the same interface over HTTP.
    /// </remarks>
    public interface IFishingService
    {
        FishingStatus GetStatus();

        ServiceResult<FishingUpdate> StartFishing();

        ServiceResult<FishingUpdate> StopFishing();

        /// <summary>
        /// Settles every cycle that has completed by now. Safe to call as often as you like: a cycle
        /// is only ever processed once, and calling early produces nothing.
        /// </summary>
        FishingUpdate Sync();

        /// <summary>Records that the game is still running and writes the save (e.g. on quit).</summary>
        void MarkSeen();

        /// <summary>The Fishing Box, newest first.</summary>
        IReadOnlyList<CatchView> GetFishingBox();

        SalePreview PreviewSale(IReadOnlyCollection<long> catchIds);

        ServiceResult<SaleResult> SellCatches(IReadOnlyCollection<long> catchIds);
    }

    public sealed class LocalFishingService : IFishingService
    {
        /// <summary>Upper bound of cycles settled in one call; a guard against a runaway clock.</summary>
        private const int MaxCyclesPerSettle = 10000;

        private readonly GameSession _session;

        public LocalFishingService(GameSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _session.ConfigReplaced += OnConfigReplaced;
            ResumeAfterStartup();
        }

        private PlayerSave Save => _session.Save;
        private GameConfig Config => _session.Config;
        private long Now => _session.Clock.UtcNowMs;

        public FishingStatus GetStatus()
        {
            var fishing = Save.Fishing;
            var now = Now;
            var status = new FishingStatus
            {
                IsFishing = fishing.Active,
                NowMs = now,
                CycleSeconds = fishing.Active ? fishing.CycleMs / 1000.0 : Config.Fishing.OnlineCycleSeconds,
                MapName = CurrentMap().DisplayName,
            };

            if (fishing.Active)
            {
                status.CycleStartedAtMs = fishing.StartedAtMs + fishing.CyclesProcessed * fishing.CycleMs;
                status.NextCatchAtMs = status.CycleStartedAtMs + fishing.CycleMs;
            }

            return status;
        }

        public ServiceResult<FishingUpdate> StartFishing()
        {
            if (Save.Fishing.Active)
            {
                return ServiceResult<FishingUpdate>.Fail(ServiceError.AlreadyFishing);
            }

            StartNewRun(Now);
            _session.Persist();
            _session.Log("Fishing started on " + Save.CurrentMapId);
            return ServiceResult<FishingUpdate>.Ok(new FishingUpdate());
        }

        public ServiceResult<FishingUpdate> StopFishing()
        {
            if (!Save.Fishing.Active)
            {
                return ServiceResult<FishingUpdate>.Fail(ServiceError.NotFishing);
            }

            // Whatever completed before the stop is owed to the player.
            var update = Sync();
            Save.Fishing.Active = false;
            _session.Persist();
            _session.Log("Fishing stopped.");
            return ServiceResult<FishingUpdate>.Ok(update);
        }

        public FishingUpdate Sync()
        {
            var update = new FishingUpdate();
            var fishing = Save.Fishing;
            if (!fishing.Active)
            {
                return update;
            }

            var now = Now;
            var rebased = false;

            if (now - fishing.LastSeenAtMs > GapLimitMs(fishing.CycleMs))
            {
                // The game was not observed running (closed, asleep, clock jump). Only time it was
                // seen running counts as online fishing; offline catches are Milestone 5.
                Settle(fishing.LastSeenAtMs, update);
                StartNewRun(now);
                rebased = true;
                _session.Log("Gap in online time detected; fishing cycle restarted.");
            }
            else
            {
                Settle(now, update);
            }

            fishing.LastSeenAtMs = Math.Max(fishing.LastSeenAtMs, now);

            if (update.HasChanges || rebased)
            {
                _session.Persist();
            }

            return update;
        }

        public void MarkSeen()
        {
            var fishing = Save.Fishing;
            if (fishing.Active)
            {
                Sync();
                fishing.LastSeenAtMs = Math.Max(fishing.LastSeenAtMs, Now);
            }

            _session.Persist();
        }

        public IReadOnlyList<CatchView> GetFishingBox()
        {
            return Save.FishingBox
                .OrderByDescending(c => c.Id)
                .Select(c => CatchViews.Create(Config, c))
                .ToList();
        }

        public SalePreview PreviewSale(IReadOnlyCollection<long> catchIds)
        {
            var preview = new SalePreview();
            if (catchIds == null)
            {
                return preview;
            }

            var wanted = new HashSet<long>(catchIds);
            foreach (var entry in Save.FishingBox.Where(c => wanted.Contains(c.Id)))
            {
                var view = CatchViews.Create(Config, entry);
                preview.Count++;
                preview.TotalCoins += view.SalePriceCoins;
                if (view.IsProtected)
                {
                    preview.ProtectedCatches.Add(view);
                }
            }

            return preview;
        }

        public ServiceResult<SaleResult> SellCatches(IReadOnlyCollection<long> catchIds)
        {
            if (catchIds == null || catchIds.Count == 0)
            {
                return ServiceResult<SaleResult>.Fail(ServiceError.EmptySelection);
            }

            var wanted = new HashSet<long>(catchIds);
            if (wanted.Count != catchIds.Count)
            {
                return ServiceResult<SaleResult>.Fail(ServiceError.DuplicateCatchInRequest);
            }

            // Validate everything before changing anything: a sale is all-or-nothing.
            var entries = Save.FishingBox.Where(c => wanted.Contains(c.Id)).ToList();
            if (entries.Count != wanted.Count)
            {
                return ServiceResult<SaleResult>.Fail(ServiceError.CatchNotFound);
            }

            long total = 0;
            foreach (var entry in entries)
            {
                if (!Config.TryGetSpecies(entry.SpeciesId, out var species))
                {
                    return ServiceResult<SaleResult>.Fail(ServiceError.SpeciesMissingFromConfig);
                }

                total += CatchRules.SalePrice(Config, species, entry.SizeMm);
            }

            Save.FishingBox.RemoveAll(c => wanted.Contains(c.Id));
            Save.Coins += total;
            Save.Stats.FishSold += entries.Count;
            Save.Stats.CoinsFromSales += total;
            _session.Persist();
            _session.Log("Sold " + entries.Count + " catches for " + total + " coins.");

            return ServiceResult<SaleResult>.Ok(new SaleResult
            {
                Count = entries.Count,
                CoinsGained = total,
                NewBalance = Save.Coins,
            });
        }

        // ------------------------------------------------------------------ internals

        private void ResumeAfterStartup()
        {
            var fishing = Save.Fishing;
            if (!fishing.Active)
            {
                return;
            }

            // Time since the game was last seen running is not online time. Settle what was earned
            // while it ran, then keep fishing from now on.
            var update = new FishingUpdate();
            Settle(fishing.LastSeenAtMs, update);
            StartNewRun(Now);
            _session.Persist();
            _session.Log("Resumed fishing after startup (" + update.NewCatches.Count + " catches settled from the previous run).");
        }

        private void OnConfigReplaced(GameConfig oldConfig, GameConfig newConfig)
        {
            if (!Save.Fishing.Active)
            {
                return;
            }

            // Settle under the old rules, then run the next cycle with the new timing.
            Sync();
            StartNewRun(Now, newConfig);
        }

        private void StartNewRun(long now, GameConfig configForTiming = null)
        {
            var fishing = Save.Fishing;
            fishing.Active = true;
            fishing.SessionIndex++;
            fishing.StartedAtMs = now;
            fishing.CyclesProcessed = 0;
            fishing.CycleMs = (long)Math.Round((configForTiming ?? Config).Fishing.OnlineCycleSeconds * 1000.0);
            fishing.LastSeenAtMs = now;
        }

        /// <summary>Produces the catch of every cycle completed by <paramref name="horizonMs"/>.</summary>
        private void Settle(long horizonMs, FishingUpdate update)
        {
            var fishing = Save.Fishing;
            if (fishing.CycleMs <= 0 || horizonMs <= fishing.StartedAtMs)
            {
                return;
            }

            var eligible = (horizonMs - fishing.StartedAtMs) / fishing.CycleMs;
            var toProcess = Math.Min(eligible - fishing.CyclesProcessed, MaxCyclesPerSettle);

            var map = CurrentMap();
            var rod = CurrentRod();
            for (long i = 0; i < toProcess; i++)
            {
                var cycle = fishing.CyclesProcessed + 1;
                var completedAt = fishing.StartedAtMs + cycle * fishing.CycleMs;
                var rng = Rng.For(Save.RngSeed, fishing.SessionIndex, cycle);
                var rolled = CatchRules.Roll(Config, map, rod, Save.EquippedRodItem()?.Level ?? 1, rng);
                ApplyCatch(rolled, completedAt, update);
                fishing.CyclesProcessed = cycle;
            }
        }

        private void ApplyCatch(RolledCatch rolled, long caughtAtMs, FishingUpdate update)
        {
            var speciesId = rolled.Species.Id;
            var flags = 0;

            if (!Save.SpeciesRecords.TryGetValue(speciesId, out var record))
            {
                record = new SpeciesRecord { FirstCaughtAtMs = caughtAtMs, LargestMm = rolled.SizeMm };
                Save.SpeciesRecords[speciesId] = record;
                flags |= BoxCatchFlags.NewSpecies;
            }
            else if (rolled.SizeMm > record.LargestMm)
            {
                record.LargestMm = rolled.SizeMm;
                flags |= BoxCatchFlags.PersonalRecord;
            }

            record.TimesCaught++;

            var entry = new BoxCatch
            {
                Id = Save.NextCatchId++,
                SpeciesId = speciesId,
                SizeMm = rolled.SizeMm,
                SizeCategoryId = rolled.SizeCategory.Id,
                CaughtAtMs = caughtAtMs,
                Flags = flags,
            };
            Save.FishingBox.Add(entry);
            Save.Stats.TotalCatches++;
            if (rolled.SizeCategory.Id == "exceptional") Save.Stats.ExceptionalCatches++;
            if (!CatchRules.IsCommon(rolled.Species)) Save.Stats.RareCatches++;
            Save.Shells += rolled.Shells;
            update.ShellsGained += rolled.Shells;

            AddFisherXp(rolled.FisherXp, update);
            update.NewCatches.Add(CatchViews.Create(Config, entry));
        }

        private void AddFisherXp(long xp, FishingUpdate update)
        {
            var maxLevel = Config.Progression.Fisher.MaxLevel;
            Save.FisherXpTotal += xp;
            update.XpGained += xp;

            if (Save.FisherLevel >= maxLevel)
            {
                Save.FisherXp = 0;
                return;
            }

            Save.FisherXp += xp;
            while (Save.FisherLevel < maxLevel)
            {
                var needed = Config.FisherXpToNextLevel(Save.FisherLevel);
                if (needed <= 0 || Save.FisherXp < needed)
                {
                    break;
                }

                Save.FisherXp -= needed;
                Save.FisherLevel++;
                update.LevelsReached.Add(Save.FisherLevel);
            }

            if (Save.FisherLevel >= maxLevel)
            {
                Save.FisherXp = 0;
            }
        }

        private MapConfig CurrentMap()
        {
            return Config.TryGetMap(Save.CurrentMapId, out var map) ? map : Config.StartingMap;
        }

        private RodConfig CurrentRod()
        {
            return Config.TryGetRod(Save.EquippedRodItem()?.RodId, out var rod) ? rod : Config.StarterRod;
        }

        /// <summary>
        /// How long without a sync still counts as "the game was running". Generous, so a slow frame
        /// or a dragged window never costs a catch, but a closed game or a sleeping PC does not
        /// count as online fishing.
        /// </summary>
        private static long GapLimitMs(long cycleMs)
        {
            return Math.Max(3 * cycleMs, 120000);
        }
    }
}
