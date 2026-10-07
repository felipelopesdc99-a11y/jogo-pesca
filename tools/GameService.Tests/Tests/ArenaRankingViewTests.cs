using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>The Arena ranking screen (A-112): podium for the top 3, pages of 10, at most the top 100.</summary>
public sealed class ArenaRankingViewTests
{
    [Fact]
    public void The_ranking_shows_the_top_100_in_pages_of_10()
    {
        var (game, _, _) = TestSupport.NewGame();
        var view = game.Arena.GetArena();

        Assert.Equal(100, view.Ranking.Count);
        Assert.Equal(10, view.RankingPageSize);
        for (var i = 0; i < view.Ranking.Count; i++)
        {
            Assert.Equal(i + 1, view.Ranking[i].Rank);
        }
    }

    [Fact]
    public void The_podium_shows_the_best_fish_of_the_top_3_only()
    {
        var (game, _, _) = TestSupport.NewGame();
        var view = game.Arena.GetArena();

        for (var i = 0; i < 3; i++)
        {
            Assert.NotNull(view.Ranking[i].LeadSpeciesId);
            Assert.True(view.Ranking[i].LeadLevel >= 1);
        }

        Assert.Null(view.Ranking[3].LeadSpeciesId);
    }

    [Fact]
    public void A_new_player_is_below_the_top_100_and_still_has_a_rank()
    {
        var (game, _, _) = TestSupport.NewGame();
        var view = game.Arena.GetArena();

        Assert.DoesNotContain(view.Ranking, r => r.IsPlayer);
        Assert.True(view.Rank > 100);
    }
}
