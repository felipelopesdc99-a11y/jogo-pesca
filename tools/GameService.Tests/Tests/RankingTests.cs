using FishingIdle.GameService.Ranking;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>The Ranking (addendum A-100): real players only; locally, just the player on this PC.</summary>
public sealed class RankingTests
{
    [Theory]
    [InlineData(RankingCategory.Level)]
    [InlineData(RankingCategory.Coins)]
    [InlineData(RankingCategory.Shells)]
    [InlineData(RankingCategory.FishCaught)]
    public void The_local_ranking_lists_only_the_real_player(RankingCategory category)
    {
        var (game, _, _) = TestSupport.NewGame();
        var save = game.Session.Save;
        save.FisherLevel = 12;
        save.Coins = 4321;
        save.Shells = 17;
        save.Stats.TotalCatches = 250;

        var ranking = game.Ranking.GetRanking(category);

        Assert.True(ranking.LocalOnly);
        var entry = Assert.Single(ranking.Entries);
        Assert.True(entry.IsYou);
        Assert.Equal(1, entry.Position);
        Assert.Equal(save.PlayerName, entry.PlayerName);
        var expected = category switch
        {
            RankingCategory.Coins => 4321L,
            RankingCategory.Shells => 17L,
            RankingCategory.FishCaught => 250L,
            _ => 12L,
        };
        Assert.Equal(expected, entry.Value);
    }
}
