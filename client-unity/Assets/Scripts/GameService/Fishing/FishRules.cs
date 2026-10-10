using System;
using FishingIdle.GameService.Config;

namespace FishingIdle.GameService.Fishing
{
    /// <summary>The four combat attributes. No hidden rolls: derived only from species, rarity, size and level.</summary>
    public sealed class FishStats
    {
        public double Hp { get; internal set; }
        public double Attack { get; internal set; }
        public double Defense { get; internal set; }
        public double Speed { get; internal set; }
    }

    /// <summary>
    /// Formulas for kept fish: attributes, level curve and feeding (GDD sections 13, 22, 24).
    /// Pure functions of config, so two fish with the same species, rarity, size and level always
    /// have the same attributes.
    /// </summary>
    public static class FishRules
    {
        /// <summary>
        /// attribute = base × rarity multiplier × (1 + size influence × (percentile − 0,5))
        ///           × (1 + level bonus) × size category multiplier (Perfeição: +5%). The level bonus is 0 at Nv.1
        ///           and grows on a curve up to Nv.100 (GameConfig.FishStatBonus, M24-T13).
        /// </summary>
        public static FishStats Stats(GameConfig config, SpeciesConfig species, int sizeMm, int level, string sizeCategoryId = null)
        {
            config.TryGetRarity(species.Rarity, out var rarity);
            var rarityFactor = rarity?.StatMultiplier ?? 1.0;
            var sizeFactor = 1.0 + config.Progression.Size.StatInfluence.Influence * (CatchRules.Percentile(species, sizeMm) - 0.5);
            var levelFactor = 1.0 + config.FishStatBonus(level);
            var categoryFactor = sizeCategoryId != null && config.TryGetSizeCategory(sizeCategoryId, out var category) ? category.StatMultiplier : 1.0;
            var factor = rarityFactor * sizeFactor * levelFactor * categoryFactor;
            var b = species.BaseStats;

            return new FishStats
            {
                Hp = b.Hp * factor,
                Attack = b.Attack * factor,
                Defense = b.Defense * factor,
                Speed = b.Speed * factor,
            };
        }

        /// <summary>
        /// XP a fish gives when consumed: its own feed value (species × rarity × size) plus the
        /// configured share (50%) of the XP that had been invested in it. Never lossless.
        /// </summary>
        public static long FeedValue(GameConfig config, SpeciesConfig species, int sizeMm, long investedXp)
        {
            config.TryGetRarity(species.Rarity, out var rarity);
            var sizeFactor = 1.0 + config.Progression.Size.FeedXpInfluence.Influence * (CatchRules.Percentile(species, sizeMm) - 0.5);
            var own = Math.Max(1, (long)Math.Round(species.BaseFeedXp * (rarity?.FeedXpMultiplier ?? 1.0) * sizeFactor, MidpointRounding.AwayFromZero));
            var recovered = (long)Math.Floor(investedXp * config.Progression.Feeding.InvestedXpRecoveryRatio);
            return own + recovered;
        }

        /// <summary>Whether consuming this fish should ask for confirmation (progression.json → feeding).</summary>
        public static bool IsValuableFood(GameConfig config, string rarityId, string sizeCategoryId, int level)
        {
            var feeding = config.Progression.Feeding;
            if (!feeding.WarnOnValuableFeed)
            {
                return false;
            }

            var rules = feeding.ValuableFeedRules;
            var minRank = config.RarityRank(rules.RarityAtOrAbove);
            if (minRank >= 0 && config.RarityRank(rarityId) >= minRank)
            {
                return true;
            }

            if (rules.SizeCategories != null && rules.SizeCategories.Contains(sizeCategoryId))
            {
                return true;
            }

            return rules.MinLevel > 0 && level >= rules.MinLevel;
        }

        /// <summary>Applies XP to a level/XP pair, following the fish XP table. Returns XP that could not be used (max level).</summary>
        public static long ApplyXp(GameConfig config, ref int level, ref long xp, long gained)
        {
            var max = config.Progression.FishLevel.MaxLevel;
            xp += gained;
            while (level < max)
            {
                var needed = config.FishXpToNextLevel(level);
                if (needed <= 0 || xp < needed)
                {
                    break;
                }

                xp -= needed;
                level++;
            }

            if (level >= max)
            {
                var wasted = xp;
                xp = 0;
                return wasted;
            }

            return 0;
        }
    }
}
