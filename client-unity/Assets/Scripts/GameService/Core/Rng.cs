using System;

namespace FishingIdle.GameService.Core
{
    /// <summary>
    /// Small deterministic random generator (SplitMix64).
    /// </summary>
    /// <remarks>
    /// Every gameplay roll comes from here, never from UnityEngine.Random or System.Random, so the
    /// same save seed and cycle index always produce the same catch. That makes a fishing cycle
    /// idempotent by construction: processing cycle N twice cannot yield two different fish, and
    /// the cycle cursor in the save guarantees it is only processed once.
    /// </remarks>
    public sealed class Rng
    {
        private ulong _state;

        public Rng(ulong seed)
        {
            _state = seed;
        }

        /// <summary>A generator for one specific event, derived from a base seed and two indices.</summary>
        public static Rng For(ulong seed, long streamA, long streamB)
        {
            var mixed = Mix(seed ^ Mix((ulong)streamA + 0x632BE59BD9B4E019UL) ^ Mix((ulong)streamB * 0x9E3779B97F4A7C15UL + 1));
            return new Rng(mixed);
        }

        /// <summary>A fresh random seed for a brand-new save.</summary>
        public static ulong NewSeed()
        {
            var bytes = Guid.NewGuid().ToByteArray();
            return BitConverter.ToUInt64(bytes, 0) ^ BitConverter.ToUInt64(bytes, 8);
        }

        public ulong NextULong()
        {
            _state += 0x9E3779B97F4A7C15UL;
            return Mix(_state);
        }

        /// <summary>Uniform double in [0, 1).</summary>
        public double NextDouble()
        {
            return (NextULong() >> 11) * (1.0 / (1UL << 53));
        }

        /// <summary>Uniform integer in [minInclusive, maxInclusive].</summary>
        public int NextIntInclusive(int minInclusive, int maxInclusive)
        {
            if (maxInclusive <= minInclusive)
            {
                return minInclusive;
            }

            var span = (ulong)((long)maxInclusive - minInclusive + 1);
            return (int)(minInclusive + (long)(NextULong() % span));
        }

        private static ulong Mix(ulong z)
        {
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }
}
