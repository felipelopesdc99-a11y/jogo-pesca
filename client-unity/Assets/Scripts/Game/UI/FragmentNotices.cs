using System;
using System.Collections.Generic;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// Groups the small notices that can arrive many at a time (boat, rod and clothing fragments, clothing levels; M24-T32/T33,
    /// A-163/A-164): at most one notice per kind every <see cref="IntervalSeconds"/> seconds. The first one shows at once; what
    /// arrives in between is added up and shown in one notice when the interval ends, with the latest state.
    /// Presentation only: the game service already decided every fragment and level.
    /// </summary>
    public sealed class FragmentNotices
    {
        /// <summary>Seconds between two notices of the same kind.</summary>
        public const float IntervalSeconds = 8f;

        private sealed class Entry
        {
            public float LastShown = float.NegativeInfinity;
            public long Pending;
            public Func<long, string> Build;
            public ToastKind Kind;
        }

        private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>(StringComparer.Ordinal);

        /// <summary>
        /// Adds <paramref name="amount"/> to the kind <paramref name="key"/>. <paramref name="build"/> writes the notice for the
        /// amount gathered (it is called with the latest one, so it can show the latest state).
        /// </summary>
        public void Add(string key, long amount, Func<long, string> build, ToastKind kind, float now, ToastFeed toasts)
        {
            if (!_entries.TryGetValue(key, out var entry))
            {
                entry = new Entry();
                _entries[key] = entry;
            }

            entry.Pending += amount;
            entry.Build = build;
            entry.Kind = kind;
            if (now - entry.LastShown >= IntervalSeconds)
            {
                Flush(entry, now, toasts);
            }
        }

        /// <summary>Shows what waited for its interval to end. Call every frame.</summary>
        public void Tick(float now, ToastFeed toasts)
        {
            foreach (var entry in _entries.Values)
            {
                if (entry.Pending > 0 && now - entry.LastShown >= IntervalSeconds)
                {
                    Flush(entry, now, toasts);
                }
            }
        }

        private static void Flush(Entry entry, float now, ToastFeed toasts)
        {
            if (entry.Pending <= 0 || entry.Build == null)
            {
                return;
            }

            toasts.Push(entry.Build(entry.Pending), entry.Kind);
            entry.Pending = 0;
            entry.LastShown = now;
        }
    }
}
