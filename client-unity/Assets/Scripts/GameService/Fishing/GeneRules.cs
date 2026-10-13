using System;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;

namespace FishingIdle.GameService.Fishing
{
    /// <summary>
    /// What the screens get about a fish's genes (M24-T52, A-176): the four values, the quality summary and the
    /// perfect marks. Every number is computed here, never on the screen.
    /// </summary>
    public sealed class GeneView
    {
        public int Hp { get; internal set; }
        public int Attack { get; internal set; }
        public int Defense { get; internal set; }
        public int Speed { get; internal set; }

        /// <summary>The highest a gene can be (progression.json → genes.max_value, 31).</summary>
        public int Max { get; internal set; }

        /// <summary>The sum of the four genes (0 to 4 × <see cref="Max"/>).</summary>
        public int Total { get; internal set; }

        /// <summary>The quality, 0–1: <see cref="Total"/> ÷ (4 × <see cref="Max"/>).</summary>
        public double Quality { get; internal set; }

        /// <summary>The quality stars (1 to the top band of genes.quality_stars).</summary>
        public int Stars { get; internal set; }

        /// <summary>The top possible star count, for drawing the empty stars.</summary>
        public int MaxStars { get; internal set; }

        public bool HpPerfect => Hp >= Max;
        public bool AttackPerfect => Attack >= Max;
        public bool DefensePerfect => Defense >= Max;
        public bool SpeedPerfect => Speed >= Max;

        /// <summary>How many of the four genes are perfect (equal to <see cref="Max"/>).</summary>
        public int PerfectCount { get; internal set; }

        /// <summary>All four genes perfect: the "Exemplar perfeito".</summary>
        public bool IsPerfectSpecimen => PerfectCount == 4;

        /// <summary>Deserves the catch celebration and the "Bem-vindo de volta" highlight (genes.celebrate_min_stars).</summary>
        public bool IsCelebrated { get; internal set; }
    }

    /// <summary>
    /// The genes of a fish (M24-T52, A-176, TD-056): four values, one per combat attribute, in the order Vida, Ataque,
    /// Defesa, Velocidade, each 0 to genes.max_value, rolled uniformly and independently at capture with the service's
    /// <see cref="Rng"/>, and kept in the fish for good. They scale the derived attributes; they never touch the NPC price.
    /// </summary>
    public static class GeneRules
    {
        /// <summary>Four genes, in the order Vida, Ataque, Defesa, Velocidade.</summary>
        public const int Count = 4;

        public const int Hp = 0, Attack = 1, Defense = 2, Speed = 3;

        /// <summary>Salt of the gene stream of a Fishing Box catch, kept apart from the catch's own rolls.</summary>
        public const ulong CatchSalt = 0x6E4E_5CA7_0001UL;

        /// <summary>Four genes, each uniform in 0..max_value, from <paramref name="rng"/>.</summary>
        public static int[] Roll(GameConfig config, Rng rng)
        {
            var max = MaxOf(config);
            var genes = new int[Count];
            for (var i = 0; i < Count; i++)
            {
                genes[i] = rng.NextIntInclusive(0, max);
            }

            return genes;
        }

        /// <summary>
        /// The genes of the Fishing Box catch number <paramref name="catchId"/>: a stream of its own (the save's seed and the
        /// catch id), so the catch, the fragments and every other roll of the cycle stay exactly as they were.
        /// </summary>
        public static int[] RollForCatch(GameConfig config, ulong saveSeed, long catchId)
        {
            return Roll(config, Rng.For(saveSeed ^ CatchSalt, catchId, 1));
        }

        /// <summary>The genes a fish from a save before version 23 gets: genes.migration_neutral_value in all four (TD-056).</summary>
        public static int[] Neutral(GameConfig config)
        {
            var value = config?.Progression?.Genes?.MigrationNeutralValue ?? 16;
            return new[] { value, value, value, value };
        }

        /// <summary>Whether <paramref name="genes"/> is a usable set (four values in range).</summary>
        public static bool IsValid(GameConfig config, int[] genes)
        {
            if (genes == null || genes.Length != Count)
            {
                return false;
            }

            var max = MaxOf(config);
            foreach (var g in genes)
            {
                if (g < 0 || g > max)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// The factor one gene puts on its attribute: min + (max − min) × gene ÷ max_value (0 → ×0,90, 31 → ×1,10).
        /// A gene out of range is clamped.
        /// </summary>
        public static double Multiplier(GameConfig config, int gene)
        {
            var g = config?.Progression?.Genes;
            var max = MaxOf(config);
            var min = g?.StatMultiplierMin ?? 0.9;
            var top = g?.StatMultiplierMax ?? 1.1;
            var clamped = Math.Max(0, Math.Min(max, gene));
            return min + (top - min) * clamped / max;
        }

        /// <summary>The quality, 0–1, and the stars of a set of genes (the view of an unusable set treats it as neutral).</summary>
        public static GeneView View(GameConfig config, int[] genes)
        {
            if (!IsValid(config, genes))
            {
                genes = Neutral(config);
            }

            var max = MaxOf(config);
            var total = genes[Hp] + genes[Attack] + genes[Defense] + genes[Speed];
            var perfect = 0;
            foreach (var g in genes)
            {
                if (g >= max)
                {
                    perfect++;
                }
            }

            var quality = total / (double)(Count * max);
            var stars = Stars(config, quality);
            var celebrate = config?.Progression?.Genes?.CelebrateMinStars ?? 0;
            return new GeneView
            {
                Hp = genes[Hp],
                Attack = genes[Attack],
                Defense = genes[Defense],
                Speed = genes[Speed],
                Max = max,
                Total = total,
                Quality = quality,
                Stars = stars,
                MaxStars = config?.Progression?.Genes?.QualityStars?.Count ?? 5,
                PerfectCount = perfect,
                IsCelebrated = perfect == Count || (celebrate > 0 && stars >= celebrate),
            };
        }

        /// <summary>The stars of a quality (0–1): the highest band of genes.quality_stars whose min_percent was reached.</summary>
        public static int Stars(GameConfig config, double quality)
        {
            var bands = config?.Progression?.Genes?.QualityStars;
            if (bands == null || bands.Count == 0)
            {
                return 1;
            }

            // A tiny tolerance so 90% computed as 0.8999999 still reaches the 90% band.
            var percent = quality * 100.0 + 1e-9;
            var stars = 1;
            foreach (var band in bands)
            {
                if (band != null && percent >= band.MinPercent)
                {
                    stars = Math.Max(stars, band.Stars);
                }
            }

            return stars;
        }

        /// <summary>The stars of a stored set of genes (an unusable set counts as neutral).</summary>
        public static int StarsOf(GameConfig config, int[] genes)
        {
            return View(config, genes).Stars;
        }

        private static int MaxOf(GameConfig config) => Math.Max(1, config?.Progression?.Genes?.MaxValue ?? 31);
    }
}
