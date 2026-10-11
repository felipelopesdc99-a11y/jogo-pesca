using FishingIdle.GameService.Config;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>OD-025: the Fishing Box holds a limited number of fish; full, fishing pauses until the player sells.</summary>
public sealed class FishingBoxLimitTests
{
    private static GameConfig SmallBox() =>
        TestSupport.ConfigWith(GameConfigLoader.EconomyFile, j => j.Replace("\"capacity\": 3000", "\"capacity\": 3"), certainCatch: true).Config;

    [Fact]
    public void The_real_box_holds_3000_fish()
    {
        // 1.500 until M24-T22 (A-159): doubled with the fishing cycle cut in half.
        Assert.Equal(3000, TestSupport.RealConfig().Economy.FishingBox.Capacity);
    }

    [Fact]
    public void The_real_box_still_fits_a_whole_day_of_offline_fishing()
    {
        var config = TestSupport.RealConfig();
        var offlineAttempts = config.Fishing.OfflineAccumulationCapHours * 3600 / config.Fishing.OfflineCycleSeconds;
        Assert.True(config.Economy.FishingBox.Capacity >= offlineAttempts);
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
