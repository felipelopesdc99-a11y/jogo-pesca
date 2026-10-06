using System;

namespace FishingIdle.GameService.Core
{
    /// <summary>
    /// The only source of "now" the game rules may use.
    /// </summary>
    /// <remarks>
    /// In the local MVP this is the PC's clock. When the rules move to a remote server the same
    /// interface is backed by the server's clock, and the presentation layer never notices.
    /// Presentation code must not read DateTime.Now to decide anything that matters.
    /// </remarks>
    public interface IClock
    {
        /// <summary>Current UTC time in Unix milliseconds.</summary>
        long UtcNowMs { get; }
    }

    public sealed class SystemClock : IClock
    {
        public long UtcNowMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// A clock that never goes backwards: "now" is the latest time this player has ever been seen at.
    /// </summary>
    /// <remarks>
    /// Every timer (offline fishing, Arena energy, Expeditions, Market checks) is anchored to "now".
    /// Turning the PC clock back and then forward again would otherwise open a fresh gap each time
    /// (TD-030). With this clock, turning it back simply freezes time until the real time catches up;
    /// turning it forward is still capped by each system's own limits (TD-019).
    /// </remarks>
    public sealed class SteadyClock : IClock
    {
        private readonly IClock _inner;
        private readonly Func<long> _read;
        private readonly Action<long> _write;

        public SteadyClock(IClock inner, Func<long> readHighWater, Action<long> writeHighWater)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _read = readHighWater;
            _write = writeHighWater;
        }

        public long UtcNowMs
        {
            get
            {
                var now = _inner.UtcNowMs;
                var highest = _read();
                if (now > highest)
                {
                    _write(now);
                    return now;
                }

                return highest;
            }
        }
    }

    /// <summary>A clock tests and simulations move by hand.</summary>
    public sealed class ManualClock : IClock
    {
        public ManualClock(long startUnixMs)
        {
            UtcNowMs = startUnixMs;
        }

        public long UtcNowMs { get; set; }

        public void AdvanceSeconds(double seconds)
        {
            UtcNowMs += (long)Math.Round(seconds * 1000.0);
        }
    }
}
