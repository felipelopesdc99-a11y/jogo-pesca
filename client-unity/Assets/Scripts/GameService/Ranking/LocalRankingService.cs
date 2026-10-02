using System.Collections.Generic;
using FishingIdle.GameService.Core;

namespace FishingIdle.GameService.Ranking
{
    /// <summary>What a ranking is sorted by (addendum A-100).</summary>
    public enum RankingCategory
    {
        Level,
        Coins,
        Shells,
        FishCaught,
    }

    public sealed class RankingEntryView
    {
        public int Position { get; internal set; }
        public string PlayerName { get; internal set; }

        /// <summary>The number the ranking is sorted by (level, coins, Conchas or fish caught).</summary>
        public long Value { get; internal set; }
        public int FisherLevel { get; internal set; }
        public bool IsYou { get; internal set; }
    }

    public sealed class RankingView
    {
        public RankingCategory Category { get; internal set; }
        public List<RankingEntryView> Entries { get; } = new List<RankingEntryView>();

        /// <summary>
        /// True while the ranking only knows the players on this computer (the local MVP). The online
        /// version lists every real player; there are never simulated players in a ranking.
        /// </summary>
        public bool LocalOnly { get; internal set; }
    }

    /// <summary>Rankings of real players only (no simulated players).</summary>
    public interface IRankingService
    {
        RankingView GetRanking(RankingCategory category);
    }

    /// <summary>
    /// The local MVP has one real player — the one on this computer — so the ranking lists just them.
    /// The remote version (server) will implement the same interface over every player's account.
    /// </summary>
    public sealed class LocalRankingService : IRankingService
    {
        private readonly GameSession _session;

        public LocalRankingService(GameSession session)
        {
            _session = session;
        }

        public RankingView GetRanking(RankingCategory category)
        {
            var save = _session.Save;
            var view = new RankingView { Category = category, LocalOnly = true };
            view.Entries.Add(new RankingEntryView
            {
                Position = 1,
                PlayerName = save.PlayerName,
                Value = ValueOf(category, save),
                FisherLevel = save.FisherLevel,
                IsYou = true,
            });
            return view;
        }

        private static long ValueOf(RankingCategory category, Persistence.PlayerSave save)
        {
            switch (category)
            {
                case RankingCategory.Coins: return save.Coins;
                case RankingCategory.Shells: return save.Shells;
                case RankingCategory.FishCaught: return save.Stats.TotalCatches;
                default: return save.FisherLevel;
            }
        }
    }
}
