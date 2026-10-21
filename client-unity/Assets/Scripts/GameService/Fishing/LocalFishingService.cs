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

        /// <summary>The offline summary produced since it was last taken, or null. Taking it clears it.</summary>
        OfflineReport TakeOfflineReport();

        SalePreview PreviewSale(IReadOnlyCollection<long> catchIds);

        ServiceResult<SaleResult> SellCatches(IReadOnlyCollection<long> catchIds);
    }

    public sealed class LocalFishingService : IFishingService
    {
        /// <summary>Upper bound of cycles settled in one call; a guard against a runaway clock.</summary>
        private const int MaxCyclesPerSettle = 10000;

        private readonly GameSession _session;
        private OfflineReport _pendingOffline;

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

            if (Save.Travel.Active)
            {
                return ServiceResult<FishingUpdate>.Fail(ServiceError.Traveling);
            }

            if (Save.EquippedRodItem() == null)
            {
                return ServiceResult<FishingUpdate>.Fail(ServiceError.NoRod);
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
                // The game was not observed running (asleep, clock jump). Time it was seen running is
                // online fishing; the gap is offline fishing, at the offline rate and cap.
                Settle(fishing.LastSeenAtMs, update);
                CatchUpOffline(fishing.LastSeenAtMs, now);
                StartNewRun(now);
                rebased = true;
                _session.Log("Gap in online time detected; offline catch-up applied and cycle restarted.");
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

        public OfflineReport TakeOfflineReport()
        {
            var report = _pendingOffline;
            _pendingOffline = null;
            return report;
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

                total = SafeAdd(total, CatchRules.SalePrice(Config, species, entry.SizeMm));
            }

            Save.FishingBox.RemoveAll(c => wanted.Contains(c.Id));
            Save.Coins = SafeAdd(Save.Coins, total);
            Save.Stats.FishSold += entries.Count;
            Save.Stats.CoinsFromSales = SafeAdd(Save.Stats.CoinsFromSales, total);
            Missions.MissionTracker.OnBoxSold(Save, entries.Count);
            _session.Persist();
            _session.Log("Sold " + entries.Count + " catches for " + total + " coins.");

            return ServiceResult<SaleResult>.Ok(new SaleResult
            {
                Count = entries.Count,
                CoinsGained = total,
                NewBalance = Save.Coins,
            });
        }

        // ------------------------------------------------------------------ map travel hooks

        /// <summary>Settles what is owed and pauses fishing for a trip. Returns whether it was on.</summary>
        internal bool PauseForTravel(FishingUpdate update)
        {
            if (!Save.Fishing.Active)
            {
                return false;
            }

            var settled = Sync();
            update.NewCatches.AddRange(settled.NewCatches);
            update.LevelsReached.AddRange(settled.LevelsReached);
            update.XpGained += settled.XpGained;
            update.ShellsGained += settled.ShellsGained;
            update.Fragments.AddRange(settled.Fragments);
            update.RodFragments.AddRange(settled.RodFragments);
            update.ClothingFragments += settled.ClothingFragments;
            update.ClothingFragmentsHave = Math.Max(update.ClothingFragmentsHave, settled.ClothingFragmentsHave);
            update.ClothingReady |= settled.ClothingReady;
            update.ClothingLevels.AddRange(settled.ClothingLevels);
            Save.Fishing.Active = false;
            return true;
        }

        /// <summary>
        /// Puts a fish found on an Expedition in the Fishing Box. Same records and flags as a catch,
        /// but no Fisher XP and no Shells (GDD section 32).
        /// </summary>
        internal CatchView AddFoundCatch(RolledCatch rolled, long atMs)
        {
            rolled.FisherXp = 0;
            rolled.Shells = 0;
            var update = new FishingUpdate();
            ApplyCatch(rolled, atMs, update, false);
            return update.NewCatches[0];
        }

        /// <summary>Starts a fresh cycle on the new map after arriving.</summary>
        internal void ResumeAfterTravel(long now)
        {
            StartNewRun(now);
        }

        // ------------------------------------------------------------------ internals

        private void ResumeAfterStartup()
        {
            var fishing = Save.Fishing;
            if (!fishing.Active)
            {
                return;
            }

            // Time since the game was last seen running is not online time: settle what was earned
            // while it ran, credit the closed period as offline fishing, then keep fishing from now.
            var update = new FishingUpdate();
            var now = Now;
            Settle(fishing.LastSeenAtMs, update);
            CatchUpOffline(fishing.LastSeenAtMs, now);
            StartNewRun(now);
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

        /// <summary>
        /// Offline fishing (GDD section 10): one attempt per offline cycle (fishing.offline_cycle_seconds) for
        /// the time between <paramref name="fromMs"/> and <paramref name="toMs"/>, never more than the cap (24 h).
        /// While the VIP is active the cycle is vip.offline_cycle_seconds instead (M24-T27, A-160): a VIP cycle is
        /// used only when it ends before the VIP does, so a VIP that ends while the player is away counts only
        /// until it ends and the rest of the time runs at the ordinary cycle.
        /// Computed in one pass from timestamps; no timer ran while the player was away. The catches
        /// continue the current run's RNG stream, and the caller starts a new run right after, so
        /// the same offline period can never be credited twice.
        /// </summary>
        private void CatchUpOffline(long fromMs, long toMs)
        {
            var away = Math.Max(0, toMs - fromMs);
            var cycleMs = (long)Math.Round(Config.Fishing.OfflineCycleSeconds * 1000.0);
            var vipCycleMs = OfflineVipCycleMs();
            var capMs = (long)Math.Round(Config.Fishing.OfflineAccumulationCapHours * 3600000.0);
            var counted = Math.Min(away, capMs);
            var endMs = fromMs + counted;
            var fishing = Save.Fishing;
            var levelBefore = Save.FisherLevel;

            var update = new FishingUpdate();
            long cycles = 0;
            long vipCycles = 0;
            if (cycleMs > 0)
            {
                var map = CurrentMap();
                var t = fromMs;
                while (cycles < MaxCyclesPerSettle)
                {
                    var vip = vipCycleMs > 0 && Vip.VipRules.IsActive(Save, t + vipCycleMs);
                    var step = vip ? vipCycleMs : cycleMs;
                    if (t + step > endMs)
                    {
                        break;
                    }

                    t += step;
                    cycles++;
                    if (vip)
                    {
                        vipCycles++;
                    }

                    var rng = Rng.For(Save.RngSeed, fishing.SessionIndex, fishing.CyclesProcessed + cycles);
                    // The rod is read every attempt: one assembled from fragments (M24-T32) may take over in between.
                    Attempt(map, CurrentRod(), Save.EquippedRodItem()?.Level ?? 1, rng, t, update, offline: true);
                }

                fishing.CyclesProcessed += cycles;
            }

            var shortest = vipCycleMs > 0 && Vip.VipRules.IsActive(Save, fromMs + vipCycleMs) ? vipCycleMs : cycleMs;
            if (away >= shortest && shortest > 0)
            {
                _pendingOffline = new OfflineReport
                {
                    AwayMs = away,
                    CountedMs = counted,
                    Capped = away > capMs,
                    CycleSeconds = cycleMs / 1000.0,
                    VipCycleSeconds = vipCycles > 0 ? vipCycleMs / 1000.0 : 0,
                    VipAttempts = vipCycles,
                    CapHours = Config.Fishing.OfflineAccumulationCapHours,
                    Update = update,
                };
                FillOfflineSummary(_pendingOffline, levelBefore);
                _session.Log("Offline fishing: " + cycles + " attempts (" + vipCycles + " at the VIP cycle) for " + (counted / 1000) + " s away.");
            }
        }

        /// <summary>
        /// The numbers of the "Bem-vindo de volta" summary (M24-T51, A-175): the sale value of the catches, the Fisher's
        /// levels, the Fishing Box, the catches by rarity and the Fragmentos de Roupa. The screen only shows them.
        /// </summary>
        private void FillOfflineSummary(OfflineReport report, int levelBefore)
        {
            var catches = report.Update.NewCatches;
            long value = 0;
            foreach (var c in catches)
            {
                value = SafeAdd(value, c.SalePriceCoins);
            }

            report.SaleValueCoins = value;
            report.FisherLevelBefore = levelBefore;
            report.FisherLevelAfter = Save.FisherLevel;
            report.FishingBoxCount = Save.FishingBox.Count;
            report.FishingBoxCapacity = Config.Economy.FishingBox?.Capacity ?? 0;
            report.ClothingFragmentsHave = Save.Clothing?.Fragments ?? 0;
            report.ClothingFragmentsNeed = Shop.ClothingRules.NextCost(Config, Save);

            var tiers = Config.Progression.Rarity?.Tiers;
            if (tiers == null)
            {
                return;
            }

            foreach (var tier in tiers)
            {
                var count = 0;
                foreach (var c in catches)
                {
                    if (c.RarityId == tier.Id)
                    {
                        count++;
                    }
                }

                if (count > 0)
                {
                    report.Rarities.Add(new RarityCountView { RarityId = tier.Id, RarityName = tier.DisplayName, Count = count });
                }
            }
        }

        /// <summary>The VIP's offline cycle in ms (A-160), or 0 when the config gives none.</summary>
        private long OfflineVipCycleMs()
        {
            var vip = Config.Economy.Vip;
            return vip != null && vip.OfflineCycleSeconds >= 1 ? (long)Math.Round(vip.OfflineCycleSeconds * 1000.0) : 0;
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
            for (long i = 0; i < toProcess; i++)
            {
                var cycle = fishing.CyclesProcessed + 1;
                var completedAt = fishing.StartedAtMs + cycle * fishing.CycleMs;
                var rng = Rng.For(Save.RngSeed, fishing.SessionIndex, cycle);
                // The rod is read every attempt: one assembled from fragments (M24-T32) may take over in between.
                Attempt(map, CurrentRod(), Save.EquippedRodItem()?.Level ?? 1, rng, completedAt, update);
                fishing.CyclesProcessed = cycle;
            }
        }

        /// <summary>
        /// One fishing cycle (docs/SISTEMA_SUCESSO_PESCA.md): the boat and bait bonus (the bait spends
        /// a charge), the attempt, and either the catch or the escape. An escape gives nothing: no
        /// fish, XP, Shells, discovery or record.
        /// </summary>
        private void Attempt(MapConfig map, RodConfig rod, int rodLevel, Rng rng, long atMs, FishingUpdate update, bool offline = false)
        {
            // OD-025: a full Fishing Box pauses the catches (and spends no bait) until the player sells some.
            var capacity = Config.Economy.FishingBox?.Capacity ?? 0;
            if (capacity > 0 && Save.FishingBox.Count >= capacity)
            {
                update.SkippedBoxFull++;
                return;
            }

            var gear = Shop.GearRules.SpendAttempt(Config, Save, update);
            // Clothing (M24-T33): the Luvas add to the chance like the boat and bait; the rest adds to the rod's bonuses.
            var clothing = Shop.ClothingRules.Bonuses(Config, Save);
            var attempt = CatchRules.Attempt(Config, map, rod, rodLevel, gear + clothing.CatchSuccess, rng, clothing);
            if (attempt.Caught)
            {
                ApplyCatch(attempt.Catch, atMs, update, offline: offline, clothingXp: clothing.FisherXp);

                // Boats by fragments (M24-T16): since M24-T47 (A-173) every boat may drop a fragment on any map, each in
                // its own roll, from the same cycle's Rng, after the catch.
                update.Fragments.AddRange(Shop.BoatFragmentRules.RollOnCatch(Config, Save, rng, offline));

                // Rods by fragments (M24-T32, every rod on any map since M24-T47) and the Fragmento de Roupa (M24-T33):
                // the same Rng, after the boats' rolls.
                update.RodFragments.AddRange(Shop.RodFragmentRules.RollOnCatch(Config, Save, rng, offline, atMs));

                if (Shop.ClothingRules.RollOnCatch(Config, Save, rng, offline))
                {
                    update.ClothingFragments++;
                    update.ClothingFragmentsHave = Save.Clothing.Fragments;

                    // The moment the fragments reach the next piece: the screen says once that it can be assembled.
                    var clothingCost = Shop.ClothingRules.NextCost(Config, Save);
                    if (clothingCost > 0 && Save.Clothing.Fragments >= clothingCost && Save.Clothing.Fragments - 1 < clothingCost)
                    {
                        update.ClothingReady = true;
                    }
                }

                // Clothing levels up with use: every fish pulled out gives use XP to every assembled piece.
                Shop.ClothingRules.GainUse(Config, Save, Config.Clothing?.UseXpPerFish ?? 0, update.ClothingLevels);
                return;
            }

            Save.Stats.Escapes++;
            Config.TryGetRarity(attempt.Species.Rarity, out var rarity);
            update.Escapes.Add(new EscapeView
            {
                RarityId = attempt.Species.Rarity,
                RarityName = rarity?.DisplayName ?? attempt.Species.Rarity,
                Chance = attempt.Chance,
                AtMs = atMs,
            });
        }

        /// <summary>The owner's test tools (A-123): puts a catch in the Fishing Box without fishing or XP.</summary>
        internal void GrantCatch(RolledCatch rolled, FishingUpdate update)
        {
            ApplyCatch(rolled, Now, update, grantXp: false);
        }

        /// <summary>The owner's test tools (A-123): the time that just passed counts as time the game was open.</summary>
        internal void MarkSeenNow()
        {
            Save.Fishing.LastSeenAtMs = Math.Max(Save.Fishing.LastSeenAtMs, Now);
        }

        private void ApplyCatch(RolledCatch rolled, long caughtAtMs, FishingUpdate update, bool grantXp = true, bool offline = false, double clothingXp = 0)
        {
            var speciesId = rolled.Species.Id;
            var flags = 0;
            var previousRecordMm = 0;

            if (!Save.SpeciesRecords.TryGetValue(speciesId, out var record))
            {
                record = new SpeciesRecord { FirstCaughtAtMs = caughtAtMs, LargestMm = rolled.SizeMm };
                Save.SpeciesRecords[speciesId] = record;
                flags |= BoxCatchFlags.NewSpecies;
            }
            else if (rolled.SizeMm > record.LargestMm)
            {
                previousRecordMm = record.LargestMm;
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

            // The genes (M24-T52, A-176): their own stream (seed and catch id), so every other roll of the cycle is unchanged.
            entry.Genes = GeneRules.IsValid(Config, rolled.Genes) ? (int[])rolled.Genes.Clone() : GeneRules.RollForCatch(Config, Save.RngSeed, entry.Id);

            // M18-T07: notable catches remember which balance numbers produced them.
            if (!CatchRules.IsCommon(rolled.Species) || rolled.SizeCategory.Special)
            {
                entry.BalanceVersion = Config.Version;
            }
            Save.FishingBox.Add(entry);
            Save.Stats.TotalCatches++;
            if (rolled.SizeCategory.Id == "exceptional") Save.Stats.ExceptionalCatches++;
            if (rolled.SizeCategory.Id == "perfect") Save.Stats.PerfectCatches++;
            if (!CatchRules.IsCommon(rolled.Species)) Save.Stats.RareCatches++;
            // The Missões (M24-T54): size, gene stars, perfect genes and a beaten size record of every catch in the Box.
            Missions.MissionTracker.OnCatch(Config, Save, entry, (flags & BoxCatchFlags.PersonalRecord) != 0);
            Save.Shells = SafeAdd(Save.Shells, rolled.Shells);
            update.ShellsGained = SafeAdd(update.ShellsGained, rolled.Shells);

            if (grantXp)
            {
                // VIP (A-110): offline catches made while it was active give extra Fisher XP. Checked
                // per catch, at its own time, so a VIP that ends during the absence counts only until then.
                var vipXp = offline ? Vip.VipRules.OfflineBonusXp(Config, Save, rolled.FisherXp, caughtAtMs) : 0;
                update.VipXpGained += vipXp;
                // The Calça (M24-T33, A-164): +% of the fish's own XP, rounded to the nearest point.
                var clothesXp = clothingXp > 0 ? (long)Math.Round(rolled.FisherXp * clothingXp, MidpointRounding.AwayFromZero) : 0;
                // The Clãs (M24-T49, A-174): +1% of the fish's own XP for a member (the fraction carries to the next fish).
                var clanXp = Clans.ClanRules.TakeBonusXp(Config, Save, rolled.FisherXp);
                update.ClanXpGained += clanXp;
                AddFisherXp(SafeAdd(SafeAdd(rolled.FisherXp + vipXp, clothesXp), clanXp), update);
            }
            var view = CatchViews.Create(Config, entry);
            view.PreviousRecordCm = previousRecordMm / 10.0;
            update.NewCatches.Add(view);
        }

        private static long SafeAdd(long a, long b) => b > long.MaxValue - a ? long.MaxValue : a + b;

        private void AddFisherXp(long xp, FishingUpdate update)
        {
            // Total XP and levels (the milestones' rewards are in the Passe do Pescador).
            update.XpGained += xp;
            FisherLevelRules.AddXp(Config, Save, xp, update.LevelsReached);
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
