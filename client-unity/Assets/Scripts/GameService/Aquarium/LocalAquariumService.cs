using System;
using System.Collections.Generic;
using System.Linq;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;
using FishingIdle.GameService.Persistence;

namespace FishingIdle.GameService.Aquarium
{
    /// <summary>
    /// The Aquarium as the player can ask for it: keep catches, look at fish, feed, sell.
    /// </summary>
    public interface IAquariumService
    {
        AquariumView GetAquarium(AquariumSort sort);

        FishView GetFish(long fishId);

        /// <summary>Moves catches from the Fishing Box into the Aquarium. All-or-nothing.</summary>
        ServiceResult<KeepResult> KeepCatches(IReadOnlyCollection<long> catchIds);

        ServiceResult<FeedPreview> PreviewFeed(long targetFishId, IReadOnlyCollection<long> boxCatchIds, IReadOnlyCollection<long> aquariumFishIds);

        /// <summary>Consumes the food (Fishing Box catches and/or other Aquarium fish) into the target.</summary>
        ServiceResult<FeedPreview> Feed(long targetFishId, IReadOnlyCollection<long> boxCatchIds, IReadOnlyCollection<long> aquariumFishIds);

        SalePreview PreviewSale(IReadOnlyCollection<long> fishIds);

        /// <summary>Sells Aquarium fish to the game. Invested XP is not reimbursed (GDD section 36).</summary>
        ServiceResult<SaleResult> SellFish(IReadOnlyCollection<long> fishIds);
    }

    public sealed class LocalAquariumService : IAquariumService
    {
        private readonly GameSession _session;

        public LocalAquariumService(GameSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        private PlayerSave Save => _session.Save;
        private GameConfig Config => _session.Config;

        public AquariumView GetAquarium(AquariumSort sort)
        {
            var view = new AquariumView { Capacity = Config.AquariumCapacity };
            view.Fish.AddRange(Sort(Save.Aquarium.Select(ToView), sort));
            return view;
        }

        public FishView GetFish(long fishId)
        {
            var fish = Save.Aquarium.FirstOrDefault(f => f.Id == fishId);
            return fish == null ? null : ToView(fish);
        }

        public ServiceResult<KeepResult> KeepCatches(IReadOnlyCollection<long> catchIds)
        {
            if (catchIds == null || catchIds.Count == 0)
            {
                return ServiceResult<KeepResult>.Fail(ServiceError.EmptySelection);
            }

            var wanted = new HashSet<long>(catchIds);
            if (wanted.Count != catchIds.Count)
            {
                return ServiceResult<KeepResult>.Fail(ServiceError.DuplicateCatchInRequest);
            }

            var entries = Save.FishingBox.Where(c => wanted.Contains(c.Id)).OrderBy(c => c.Id).ToList();
            if (entries.Count != wanted.Count)
            {
                return ServiceResult<KeepResult>.Fail(ServiceError.CatchNotFound);
            }

            if (entries.Any(c => !Config.TryGetSpecies(c.SpeciesId, out _)))
            {
                return ServiceResult<KeepResult>.Fail(ServiceError.SpeciesMissingFromConfig);
            }

            if (Save.Aquarium.Count + entries.Count > Config.AquariumCapacity)
            {
                return ServiceResult<KeepResult>.Fail(ServiceError.AquariumFull);
            }

            var now = _session.Clock.UtcNowMs;
            var result = new KeepResult();
            foreach (var entry in entries)
            {
                // The catch becomes a full persistent fish only now (GDD section 11).
                var fish = new FishInstance
                {
                    Id = Save.NextFishId++,
                    SpeciesId = entry.SpeciesId,
                    SizeMm = entry.SizeMm,
                    SizeCategoryId = entry.SizeCategoryId,
                    Level = 1,
                    CaughtAtMs = entry.CaughtAtMs,
                    KeptAtMs = now,
                    SourceCatchId = entry.Id,
                };
                Save.Aquarium.Add(fish);
                result.Kept.Add(ToView(fish));
            }

            Save.FishingBox.RemoveAll(c => wanted.Contains(c.Id));
            result.AquariumCount = Save.Aquarium.Count;
            result.Capacity = Config.AquariumCapacity;
            _session.Persist();
            _session.Log("Kept " + entries.Count + " catches in the Aquarium.");
            return ServiceResult<KeepResult>.Ok(result);
        }

        public ServiceResult<FeedPreview> PreviewFeed(long targetFishId, IReadOnlyCollection<long> boxCatchIds, IReadOnlyCollection<long> aquariumFishIds)
        {
            return Evaluate(targetFishId, boxCatchIds, aquariumFishIds, out _, out _, out _);
        }

        public ServiceResult<FeedPreview> Feed(long targetFishId, IReadOnlyCollection<long> boxCatchIds, IReadOnlyCollection<long> aquariumFishIds)
        {
            var check = Evaluate(targetFishId, boxCatchIds, aquariumFishIds, out var target, out var boxFood, out var fishFood);
            if (!check.Succeeded)
            {
                return check;
            }

            var preview = check.Value;
            target.Level = preview.LevelAfter;
            target.Xp = preview.XpAfter;
            target.InvestedXp += preview.XpGained - preview.WastedXp;

            var boxIds = new HashSet<long>(boxFood.Select(c => c.Id));
            var fishIds = new HashSet<long>(fishFood.Select(f => f.Id));
            Save.FishingBox.RemoveAll(c => boxIds.Contains(c.Id));
            Save.Aquarium.RemoveAll(f => fishIds.Contains(f.Id));
            _session.Persist();
            _session.Log("Fed fish " + target.Id + " with " + preview.FoodCount + " fish (+" + preview.XpGained + " XP).");
            return check;
        }

        public SalePreview PreviewSale(IReadOnlyCollection<long> fishIds)
        {
            var preview = new SalePreview();
            var wanted = new HashSet<long>(fishIds ?? Array.Empty<long>());
            foreach (var fish in Save.Aquarium.Where(f => wanted.Contains(f.Id)))
            {
                var view = ToView(fish);
                preview.Count++;
                preview.TotalCoins += view.SalePriceCoins;
                if (view.IsValuableFood)
                {
                    preview.ProtectedFishNames.Add(view.SpeciesName + " · " + FishingIdle.Texts.GameTexts.Player.LevelShort + " " + view.Level);
                }
            }

            return preview;
        }

        public ServiceResult<SaleResult> SellFish(IReadOnlyCollection<long> fishIds)
        {
            if (fishIds == null || fishIds.Count == 0)
            {
                return ServiceResult<SaleResult>.Fail(ServiceError.EmptySelection);
            }

            var wanted = new HashSet<long>(fishIds);
            if (wanted.Count != fishIds.Count)
            {
                return ServiceResult<SaleResult>.Fail(ServiceError.DuplicateCatchInRequest);
            }

            var fish = Save.Aquarium.Where(f => wanted.Contains(f.Id)).ToList();
            if (fish.Count != wanted.Count)
            {
                return ServiceResult<SaleResult>.Fail(ServiceError.FishNotFound);
            }

            long total = 0;
            foreach (var f in fish)
            {
                if (!Config.TryGetSpecies(f.SpeciesId, out var species))
                {
                    return ServiceResult<SaleResult>.Fail(ServiceError.SpeciesMissingFromConfig);
                }

                total += CatchRules.SalePrice(Config, species, f.SizeMm);
            }

            Save.Aquarium.RemoveAll(f => wanted.Contains(f.Id));
            Save.Coins += total;
            Save.Stats.FishSold += fish.Count;
            Save.Stats.CoinsFromSales += total;
            _session.Persist();
            _session.Log("Sold " + fish.Count + " Aquarium fish for " + total + " coins.");
            return ServiceResult<SaleResult>.Ok(new SaleResult { Count = fish.Count, CoinsGained = total, NewBalance = Save.Coins });
        }

        // ------------------------------------------------------------------ internals

        /// <summary>Validates a feeding request and computes its outcome without changing anything.</summary>
        private ServiceResult<FeedPreview> Evaluate(
            long targetFishId,
            IReadOnlyCollection<long> boxCatchIds,
            IReadOnlyCollection<long> aquariumFishIds,
            out FishInstance target,
            out List<BoxCatch> boxFood,
            out List<FishInstance> fishFood)
        {
            boxFood = new List<BoxCatch>();
            fishFood = new List<FishInstance>();
            target = Save.Aquarium.FirstOrDefault(f => f.Id == targetFishId);
            if (target == null)
            {
                return ServiceResult<FeedPreview>.Fail(ServiceError.FishNotFound);
            }

            var boxIds = new HashSet<long>(boxCatchIds ?? Array.Empty<long>());
            var fishIds = new HashSet<long>(aquariumFishIds ?? Array.Empty<long>());
            if (boxIds.Count + fishIds.Count == 0)
            {
                return ServiceResult<FeedPreview>.Fail(ServiceError.EmptySelection);
            }

            if (boxIds.Count != (boxCatchIds?.Count ?? 0) || fishIds.Count != (aquariumFishIds?.Count ?? 0))
            {
                return ServiceResult<FeedPreview>.Fail(ServiceError.DuplicateCatchInRequest);
            }

            if (fishIds.Contains(targetFishId))
            {
                return ServiceResult<FeedPreview>.Fail(ServiceError.CannotFeedItself);
            }

            if (target.Level >= Config.Progression.FishLevel.MaxLevel)
            {
                return ServiceResult<FeedPreview>.Fail(ServiceError.FishAtMaxLevel);
            }

            boxFood = Save.FishingBox.Where(c => boxIds.Contains(c.Id)).ToList();
            fishFood = Save.Aquarium.Where(f => fishIds.Contains(f.Id)).ToList();
            if (boxFood.Count != boxIds.Count)
            {
                return ServiceResult<FeedPreview>.Fail(ServiceError.CatchNotFound);
            }

            if (fishFood.Count != fishIds.Count)
            {
                return ServiceResult<FeedPreview>.Fail(ServiceError.FishNotFound);
            }

            var preview = new FeedPreview
            {
                TargetFishId = target.Id,
                FoodCount = boxFood.Count + fishFood.Count,
                LevelBefore = target.Level,
                XpBefore = target.Xp,
                XpToNextBefore = Config.FishXpToNextLevel(target.Level),
                MaxLevel = Config.Progression.FishLevel.MaxLevel,
            };

            foreach (var c in boxFood)
            {
                if (!Config.TryGetSpecies(c.SpeciesId, out var species))
                {
                    return ServiceResult<FeedPreview>.Fail(ServiceError.SpeciesMissingFromConfig);
                }

                preview.XpGained += FishRules.FeedValue(Config, species, c.SizeMm, 0);
                if (FishRules.IsValuableFood(Config, species.Rarity, c.SizeCategoryId, 1))
                {
                    preview.ValuableFood.Add(species.DisplayName + " · " + FishingIdle.Texts.Format.SizeCm(c.SizeMm / 10.0));
                }
            }

            foreach (var f in fishFood)
            {
                if (!Config.TryGetSpecies(f.SpeciesId, out var species))
                {
                    return ServiceResult<FeedPreview>.Fail(ServiceError.SpeciesMissingFromConfig);
                }

                preview.XpGained += FishRules.FeedValue(Config, species, f.SizeMm, f.InvestedXp);
                if (FishRules.IsValuableFood(Config, species.Rarity, f.SizeCategoryId, f.Level))
                {
                    preview.ValuableFood.Add(species.DisplayName + " · " + FishingIdle.Texts.GameTexts.Player.LevelShort + " " + f.Level);
                }
            }

            var level = target.Level;
            var xp = target.Xp;
            preview.WastedXp = FishRules.ApplyXp(Config, ref level, ref xp, preview.XpGained);
            preview.LevelAfter = level;
            preview.XpAfter = xp;
            preview.XpToNextAfter = Config.FishXpToNextLevel(level);
            return ServiceResult<FeedPreview>.Ok(preview);
        }

        private FishView ToView(FishInstance fish)
        {
            var view = new FishView
            {
                FishId = fish.Id,
                SpeciesId = fish.SpeciesId,
                SpeciesName = fish.SpeciesId,
                SizeCategoryId = fish.SizeCategoryId,
                SizeCategoryName = fish.SizeCategoryId,
                SizeCm = fish.SizeMm / 10.0,
                Level = fish.Level,
                MaxLevel = Config.Progression.FishLevel.MaxLevel,
                Xp = fish.Xp,
                XpToNext = Config.FishXpToNextLevel(fish.Level),
                InvestedXp = fish.InvestedXp,
                CaughtAtMs = fish.CaughtAtMs,
                KeptAtMs = fish.KeptAtMs,
                Stats = new FishStats(),
            };

            if (Config.TryGetSizeCategory(fish.SizeCategoryId, out var category))
            {
                view.SizeCategoryName = category.DisplayName;
            }

            if (Config.TryGetSpecies(fish.SpeciesId, out var species))
            {
                view.SpeciesName = species.DisplayName;
                view.RarityId = species.Rarity;
                view.RarityName = Config.TryGetRarity(species.Rarity, out var rarity) ? rarity.DisplayName : species.Rarity;
                view.SpeciesMinCm = species.SizeCm.Min;
                view.SpeciesMaxCm = species.SizeCm.Max;
                view.SizePercentile = CatchRules.Percentile(species, fish.SizeMm);
                view.Stats = FishRules.Stats(Config, species, fish.SizeMm, fish.Level);
                view.SalePriceCoins = CatchRules.SalePrice(Config, species, fish.SizeMm);
                view.FeedXp = FishRules.FeedValue(Config, species, fish.SizeMm, fish.InvestedXp);
                view.IsValuableFood = FishRules.IsValuableFood(Config, species.Rarity, fish.SizeCategoryId, fish.Level);
            }

            view.IsImportant = fish.SizeCategoryId == "exceptional" || (view.RarityId != null && Config.RarityRank(view.RarityId) > 0);
            return view;
        }

        private IEnumerable<FishView> Sort(IEnumerable<FishView> fish, AquariumSort sort)
        {
            switch (sort)
            {
                case AquariumSort.Level:
                    return fish.OrderByDescending(f => f.Level).ThenByDescending(f => f.Xp).ThenBy(f => f.FishId);
                case AquariumSort.Species:
                    return fish.OrderBy(f => f.SpeciesName, StringComparer.CurrentCulture).ThenByDescending(f => f.SizeCm);
                case AquariumSort.Newest:
                    return fish.OrderByDescending(f => f.FishId);
                default:
                    // Size categories are listed smallest → largest in config; show the largest first.
                    var order = Config.SizeCategories.Select((c, i) => new { c.Id, i }).ToDictionary(x => x.Id, x => x.i);
                    return fish
                        .OrderByDescending(f => order.TryGetValue(f.SizeCategoryId ?? string.Empty, out var i) ? i : -1)
                        .ThenByDescending(f => f.SizePercentile)
                        .ThenBy(f => f.FishId);
            }
        }
    }
}
