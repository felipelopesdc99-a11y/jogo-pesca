using System;
using System.Collections.Generic;
using System.Linq;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;

namespace FishingIdle.GameService.Fishing
{
    /// <summary>The raw result of one fishing roll, before it is written to the save.</summary>
    public sealed class RolledCatch
    {
        public SpeciesConfig Species { get; internal set; }
        public SizeCategoryConfig SizeCategory { get; internal set; }
        public int SizeMm { get; internal set; }
        public long FisherXp { get; internal set; }
        public long Shells { get; internal set; }
    }

    /// <summary>
    /// One fishing attempt (docs/SISTEMA_SUCESSO_PESCA.md): the fish that bit, the chance of pulling
    /// it out, and the catch when it was pulled out (null when it escaped).
    /// </summary>
    public sealed class CatchAttempt
    {
        public SpeciesConfig Species { get; internal set; }
        public double Chance { get; internal set; }
        public bool Caught => Catch != null;
        public RolledCatch Catch { get; internal set; }
    }

    /// <summary>
    /// The fishing formulas: which species, which size, how much XP, how many Shells, what price.
    /// Pure functions of config + RNG, so they are easy to test and simulate.
    /// </summary>
    public static class CatchRules
    {
        /// <summary>Rolls one catch for a map with a given rod, always pulled out (Expeditions, bots).</summary>
        public static RolledCatch Roll(GameConfig config, MapConfig map, RodConfig rod, int rodLevel, Rng rng)
        {
            var bonuses = config.RodBonusesAt(rod, rodLevel);
            var species = RollSpecies(config, map, rod, bonuses, rng);
            return RollCaught(config, species, rod, bonuses, rng);
        }

        /// <summary>
        /// One fishing attempt, in the order of the design: the fish that bit (species), then the
        /// Catch Success roll with the rod's bonus plus <paramref name="gearBonus"/> (boat + bait),
        /// and only when it is pulled out, its size, XP and Shells.
        /// </summary>
        public static CatchAttempt Attempt(GameConfig config, MapConfig map, RodConfig rod, int rodLevel, double gearBonus, Rng rng)
        {
            var bonuses = config.RodBonusesAt(rod, rodLevel);
            var species = RollSpecies(config, map, rod, bonuses, rng);
            var chance = SuccessChance(config, species.Rarity, bonuses.CatchSuccess + gearBonus);
            var attempt = new CatchAttempt { Species = species, Chance = chance };
            if (rng.NextDouble() < chance)
            {
                attempt.Catch = RollCaught(config, species, rod, bonuses, rng);
            }

            return attempt;
        }

        /// <summary>
        /// Catch Success chance = the rarity's base + equipment bonus (rod + boat + bait), kept between
        /// the configured floor and ceiling (5% and 95%).
        /// </summary>
        public static double SuccessChance(GameConfig config, string rarityId, double equipmentBonus)
        {
            config.TryGetRarity(rarityId, out var rarity);
            var chance = (rarity?.CatchSuccessBase ?? 1.0) + equipmentBonus;
            var fishing = config.Fishing;
            return Math.Max(fishing.CatchSuccessMin, Math.Min(fishing.CatchSuccessMax, chance));
        }

        /// <summary>Size, XP and Shells of a fish that was pulled out.</summary>
        private static RolledCatch RollCaught(GameConfig config, SpeciesConfig species, RodConfig rod, RodBonusesConfig bonuses, Rng rng)
        {
            var category = RollSizeCategory(config, bonuses, rng, species.Rarity);

            // Exact size: uniform inside the category's percentile band, mapped onto the species range.
            var percentile = category.PercentileMin + rng.NextDouble() * (category.PercentileMax - category.PercentileMin);
            var sizeCm = species.SizeCm.Min + percentile * (species.SizeCm.Max - species.SizeCm.Min);
            var sizeMm = Math.Max(1, (int)Math.Round(sizeCm * 10.0, MidpointRounding.AwayFromZero));

            config.TryGetRarity(species.Rarity, out var rarity);
            var xp = species.BaseFisherXp * (rarity?.FisherXpMultiplier ?? 1.0) * category.FisherXpMultiplier;

            return new RolledCatch
            {
                Species = species,
                SizeCategory = category,
                SizeMm = sizeMm,
                FisherXp = Math.Max(1, (long)Math.Round(xp, MidpointRounding.AwayFromZero)),
                Shells = RollShells(config, rod, bonuses, rng),
            };
        }

        /// <summary>
        /// A fish found away from the rod (an Expedition): any rarity the map has, no rod bonuses.
        /// </summary>
        public static RolledCatch RollFound(GameConfig config, MapConfig map, Rng rng)
        {
            var anyRod = new RodConfig
            {
                Id = "expedition",
                CanCatchRarities = map.AvailableRarities,
                Bonuses = new RodBonusesConfig(),
            };
            return Roll(config, map, anyRod, 1, rng);
        }

        /// <summary>
        /// Species by catch weight, restricted to rarities both the map and the rod allow. The rod's
        /// rarity efficiency scales the weight of non-common species only; it never adds a rarity.
        /// </summary>
        public static SpeciesConfig RollSpecies(GameConfig config, MapConfig map, RodConfig rod, RodBonusesConfig bonuses, Rng rng)
        {
            var candidates = EligiblePool(config, map, rod)
                .Select(pair => new KeyValuePair<SpeciesConfig, double>(
                    pair.Key,
                    IsCommon(pair.Key) ? pair.Value : pair.Value * (1.0 + bonuses.RarityEfficiency)))
                .ToList();

            return PickWeighted(candidates, rng);
        }

        /// <summary>The species a rod can actually pull out of a map, with their base weights.</summary>
        public static List<KeyValuePair<SpeciesConfig, double>> EligiblePool(GameConfig config, MapConfig map, RodConfig rod)
        {
            var result = new List<KeyValuePair<SpeciesConfig, double>>();
            foreach (var entry in map.FishPool)
            {
                if (entry.CatchWeight <= 0 || !config.TryGetSpecies(entry.SpeciesId, out var species))
                {
                    continue;
                }

                var mapAllows = map.AvailableRarities != null && map.AvailableRarities.Contains(species.Rarity);
                var rodAllows = rod.CanCatchRarities != null && rod.CanCatchRarities.Contains(species.Rarity);
                if (mapAllows && rodAllows)
                {
                    result.Add(new KeyValuePair<SpeciesConfig, double>(species, entry.CatchWeight));
                }
            }

            return result;
        }

        /// <summary>
        /// Size category by draw weight. The rod's size quality multiplies the weight of the
        /// "large", "exceptional" and "perfect" categories, and the species' rarity multiplies each category by
        /// its size_weight_multipliers (rarer fish come big a little less often, addendum A-083);
        /// the draw renormalises over the new total.
        /// </summary>
        public static SizeCategoryConfig RollSizeCategory(GameConfig config, RodBonusesConfig bonuses, Rng rng, string rarityId = null)
        {
            RarityTierConfig rarity = null;
            if (rarityId != null)
            {
                config.TryGetRarity(rarityId, out rarity);
            }

            var candidates = config.SizeCategories
                .Select(c => new KeyValuePair<SizeCategoryConfig, double>(
                    c,
                    (IsBoostedBySizeQuality(c) ? c.DrawWeight * (1.0 + bonuses.SizeQuality) : c.DrawWeight)
                        * (rarity?.SizeWeightMultiplier(c.Id) ?? 1.0)))
                .ToList();

            return PickWeighted(candidates, rng);
        }

        private static long RollShells(GameConfig config, RodConfig rod, RodBonusesConfig bonuses, Rng rng)
        {
            // Roll unconditionally so a rod swap does not shift every later roll in the stream.
            var roll = rng.NextDouble();
            var shells = config.Economy.Shells;
            var amount = rng.NextIntInclusive(shells.AmountPerDrop.Min, shells.AmountPerDrop.Max);

            if (!rod.GeneratesShells)
            {
                return 0;
            }

            var chance = Math.Min(1.0, shells.BaseDropChancePerCatch * (1.0 + bonuses.ShellYield));
            return roll < chance ? amount : 0;
        }

        /// <summary>
        /// NPC sale price: base value × rarity multiplier × a continuous size factor
        /// (GDD section 36). Never below the configured minimum.
        /// </summary>
        public static long SalePrice(GameConfig config, SpeciesConfig species, int sizeMm)
        {
            config.TryGetRarity(species.Rarity, out var rarity);
            var influence = config.Progression.Size.SaleValueInfluence.Influence;
            var sizeFactor = 1.0 + influence * (Percentile(species, sizeMm) - 0.5);
            var price = species.BaseSaleValueCoins * (rarity?.SaleValueMultiplier ?? 1.0) * sizeFactor;
            return Math.Max(config.Economy.NpcFishSale.MinimumPriceCoins, (long)Math.Round(price, MidpointRounding.AwayFromZero));
        }

        /// <summary>Where a size sits inside the species range, clamped to 0–1.</summary>
        public static double Percentile(SpeciesConfig species, int sizeMm)
        {
            var span = species.SizeCm.Max - species.SizeCm.Min;
            if (span <= 0)
            {
                return 0.5;
            }

            var p = (sizeMm / 10.0 - species.SizeCm.Min) / span;
            return p < 0 ? 0 : p > 1 ? 1 : p;
        }

        public static bool IsCommon(SpeciesConfig species) => string.Equals(species.Rarity, "common", StringComparison.Ordinal);

        private static bool IsBoostedBySizeQuality(SizeCategoryConfig category)
        {
            return category.Id == "large" || category.Id == "exceptional" || category.Id == "perfect";
        }

        private static T PickWeighted<T>(IReadOnlyList<KeyValuePair<T, double>> candidates, Rng rng)
        {
            if (candidates.Count == 0)
            {
                throw new InvalidOperationException("Nothing to pick from: the pool is empty for this map and rod.");
            }

            var total = candidates.Sum(c => Math.Max(0, c.Value));
            var target = rng.NextDouble() * total;
            foreach (var candidate in candidates)
            {
                var weight = Math.Max(0, candidate.Value);
                if (target < weight)
                {
                    return candidate.Key;
                }

                target -= weight;
            }

            return candidates[candidates.Count - 1].Key;
        }
    }
}
