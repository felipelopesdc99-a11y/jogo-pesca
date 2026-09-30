using FishingIdle.GameService.Config;
using FishingIdle.GameService.Persistence;

namespace FishingIdle.GameService.Fishing
{
    /// <summary>Builds the read-only view of a Fishing Box catch. Shared by the fishing and Aquarium services.</summary>
    internal static class CatchViews
    {
        public static CatchView Create(GameConfig config, BoxCatch entry)
        {
            var view = new CatchView
            {
                CatchId = entry.Id,
                SpeciesId = entry.SpeciesId,
                SpeciesName = entry.SpeciesId,
                SizeCm = entry.SizeMm / 10.0,
                SizeCategoryId = entry.SizeCategoryId,
                SizeCategoryName = entry.SizeCategoryId,
                CaughtAtMs = entry.CaughtAtMs,
                IsNewSpecies = (entry.Flags & BoxCatchFlags.NewSpecies) != 0,
                IsPersonalRecord = (entry.Flags & BoxCatchFlags.PersonalRecord) != 0,
            };

            if (config.TryGetSizeCategory(entry.SizeCategoryId, out var category))
            {
                view.SizeCategoryName = category.DisplayName;
            }

            if (config.TryGetSpecies(entry.SpeciesId, out var species))
            {
                view.SpeciesName = species.DisplayName;
                view.RarityId = species.Rarity;
                view.RarityName = config.TryGetRarity(species.Rarity, out var rarity) ? rarity.DisplayName : species.Rarity;
                view.SpeciesMinCm = species.SizeCm.Min;
                view.SpeciesMaxCm = species.SizeCm.Max;
                view.SizePercentile = CatchRules.Percentile(species, entry.SizeMm);
                view.SalePriceCoins = CatchRules.SalePrice(config, species, entry.SizeMm);
                view.FeedXp = FishRules.FeedValue(config, species, entry.SizeMm, 0);
            }

            var protection = config.Economy.FishingBox.BulkSaleProtection;
            view.IsProtected =
                (protection.Rarities != null && view.RarityId != null && protection.Rarities.Contains(view.RarityId)) ||
                (protection.SizeCategories != null && protection.SizeCategories.Contains(entry.SizeCategoryId));

            view.IsValuableFood = FishRules.IsValuableFood(config, view.RarityId, entry.SizeCategoryId, 1);
            view.IsImportant = view.IsProtected || view.IsNewSpecies || config.IsSpecialSize(entry.SizeCategoryId);
            return view;
        }
    }
}
