using FishingIdle.GameService.Config;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>OD-025: the Fishing Box holds a limited number of fish; full, fishing pauses until the player sells.</summary>
public sealed class FishingBoxLimitTests
{
    private static GameConfig SmallBox() =>
        TestSupport.ConfigWith(GameConfigLoader.EconomyFile, j => j.Replace("\"capacity\": 8640", "\"capacity\": 3"), certainCatch: true).Config;

    [Fact]
    public void The_real_box_holds_8640_fish_one_day_of_online_fishing()
    {
        // 1.500 until M24-T22 (A-159), then 3.000; M24-T27 (A-160): one whole day of online fishing (24 h every 10 s).
        var config = TestSupport.RealConfig();
        Assert.Equal(8640, config.Economy.FishingBox.Capacity);
        Assert.Equal(24 * 3600 / config.Fishing.OnlineCycleSeconds, config.Economy.FishingBox.Capacity, 6);
    }

    [Fact]
    public void The_real_box_still_fits_a_whole_day_of_offline_fishing()
    {
        var config = TestSupport.RealConfig();
        var offlineAttempts = config.Fishing.OfflineAccumulationCapHours * 3600 / config.Fishing.OfflineCycleSeconds;
        Assert.True(config.Economy.FishingBox.Capacity >= offlineAttempts);

        // With the VIP's faster offline cycle (A-160) too.
        var vipAttempts = config.Fishing.OfflineAccumulationCapHours * 3600 / config.Economy.Vip.OfflineCycleSeconds;
        Assert.True(config.Economy.FishingBox.Capacity >= vipAttempts);
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
