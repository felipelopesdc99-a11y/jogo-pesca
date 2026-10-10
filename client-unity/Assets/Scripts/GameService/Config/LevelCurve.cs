using System;

namespace FishingIdle.GameService.Config
{
    /// <summary>
    /// The level curve of rods, boats and fish (M24-T13, TD-041): a value at Nv.1, a value at the max level and a
    /// curve in between, plus geometric level prices. tools/Progressao/curva_niveis.py does the same math.
    /// </summary>
    /// <remarks>
    /// value(N) = atLevel1 + (atMax − atLevel1) × ((N − 1) ÷ (max − 1))^exponent. With exponent &gt; 1 every level adds a
    /// little more than the one before, starting with small steps. A price is first × growth^(N − 1), rounded to a
    /// whole number below 1.000 (at least 1) and to 3 significant digits from 1.000 on (12.345 → 12.300).
    /// </remarks>
    public static class LevelCurve
    {
        /// <summary>A price that does not fit a long: never payable (as the Crew, TD-039).</summary>
        public const long Unaffordable = long.MaxValue;

        /// <summary>The value at <paramref name="level"/>, clamped to 1..<paramref name="maxLevel"/>.</summary>
        public static double Value(double atLevel1, double atMax, int level, int maxLevel, double exponent)
        {
            if (maxLevel <= 1)
            {
                return atLevel1;
            }

            var clamped = Math.Max(1, Math.Min(maxLevel, level));
            var t = (clamped - 1) / (double)(maxLevel - 1);
            var shaped = exponent > 0 ? Math.Pow(t, exponent) : t;
            return atLevel1 + (atMax - atLevel1) * shaped;
        }

        /// <summary>Price to go from <paramref name="level"/> to the next: first × growth^(level − 1), rounded; 0 when first is 0.</summary>
        public static long Cost(long first, double growth, int level)
        {
            if (first <= 0)
            {
                return 0;
            }

            return RoundPrice(first * Math.Pow(growth, Math.Max(0, level - 1)));
        }

        /// <summary>Whole number below 1.000 (at least 1); 3 significant digits from 1.000 on; <see cref="Unaffordable"/> past a long.</summary>
        public static long RoundPrice(double raw)
        {
            if (double.IsNaN(raw) || raw >= 9.2e18)
            {
                return Unaffordable;
            }

            if (raw < 1000)
            {
                return Math.Max(1, (long)Math.Floor(raw + 0.5));
            }

            var digits = (int)Math.Floor(Math.Log10(raw));
            var unit = Math.Pow(10, digits - 2);
            return (long)Math.Floor(raw / unit + 0.5) * (long)Math.Round(unit);
        }
    }
}
