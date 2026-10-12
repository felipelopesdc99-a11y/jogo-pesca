namespace FishingIdle.GameService.Core
{
    /// <summary>
    /// Additions that stop at the top instead of wrapping around. A balance that overflows would turn negative and
    /// the save validator would refuse to write it, so every reward that can pile up adds through here.
    /// </summary>
    public static class SafeMath
    {
        /// <summary>a + b, capped at long.MaxValue (and at long.MinValue going down).</summary>
        public static long Add(long a, long b)
        {
            if (b > 0 && a > long.MaxValue - b)
            {
                return long.MaxValue;
            }

            if (b < 0 && a < long.MinValue - b)
            {
                return long.MinValue;
            }

            return a + b;
        }

        /// <summary>a + b for an int counter (bait charges), capped at int.MaxValue and never below 0.</summary>
        public static int AddCount(int a, int b)
        {
            var sum = (long)a + b;
            if (sum > int.MaxValue)
            {
                return int.MaxValue;
            }

            return sum < 0 ? 0 : (int)sum;
        }
    }
}
