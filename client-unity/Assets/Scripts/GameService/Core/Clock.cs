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
