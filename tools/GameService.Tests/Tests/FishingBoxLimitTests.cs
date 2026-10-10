using FishingIdle.GameService.Config;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>OD-025: the Fishing Box holds a limited number of fish; full, fishing pauses until the player sells.</summary>
public sealed class FishingBoxLimitTests
{
    private static GameConfig SmallBox() =>
        TestSupport.ConfigWith(GameConfigLoader.EconomyFile, j => j.Replace("\"capacity\": 1500", "\"capacity\": 3"), certainCatch: true).Config;

    [Fact]
    public void The_real_box_holds_1500_fish()
    {
        Assert.Equal(1500, TestSupport.RealConfig().Economy.FishingBox.Capacity);
    }

    [Fact]
    public void A_full_box_stops_the_catches_until_some_are_sold()
    {
        var (game, clock, _) = TestSupport.NewGame(SmallBox());
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, 300, stepSeconds: 30);

        Assert.Equal(3, game.Fishing.GetFishingBox().Count);
        Assert.True(game.Player.GetPlayer().FishingBoxFull);

        clock.AdvanceSeconds(60);
        var update = game.Fishing.Sync();
        Assert.Empty(update.NewCatches);
        Assert.True(update.SkippedBoxFull > 0);
    }
}
