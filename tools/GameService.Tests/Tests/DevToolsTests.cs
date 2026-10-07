using FishingIdle.GameService.Core;
using FishingIdle.GameService.Dev;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>A-123: the owner's test tools work only when enabled, and only through the game service.</summary>
public sealed class DevToolsTests
{
    [Fact]
    public void Everything_is_refused_while_the_tools_are_off()
    {
        var (game, _, _) = TestSupport.NewGame();
        Assert.Equal(ServiceError.DevToolsDisabled, game.DevTools.Give(DevCurrency.Coins, 1000).Error);
        Assert.Equal(ServiceError.DevToolsDisabled, game.DevTools.AdvanceTime(1, true).Error);
        Assert.Equal(0, game.Session.Save.DevTimeOffsetMs);
    }

    [Fact]
    public void Currencies_fish_and_level_are_given()
    {
        var (game, _, _) = TestSupport.NewGame();
        game.Session.DevToolsEnabled = true;
        var coins = game.Session.Save.Coins;

        Assert.True(game.DevTools.Give(DevCurrency.Coins, 1000).Succeeded);
        Assert.True(game.DevTools.Give(DevCurrency.Dollars, 100).Succeeded);
        Assert.Equal(coins + 1000, game.Session.Save.Coins);
        Assert.Equal(100, game.Session.Save.Dollars);

        Assert.Equal(3, game.DevTools.GiveFish("lambari", "exceptional", 3).Value);
        Assert.All(game.Fishing.GetFishingBox(), c => Assert.Equal("exceptional", c.SizeCategoryId));
        Assert.Equal(0, game.Session.Save.FisherXpTotal);

        Assert.Equal(50, game.DevTools.SetFisherLevel(50).Value);
        Assert.Equal(100, game.DevTools.SetFisherLevel(500).Value);
    }

    [Fact]
    public void One_hour_open_is_online_fishing_and_one_hour_closed_is_offline()
    {
        var (game, _, _) = TestSupport.NewGame();
        game.Session.DevToolsEnabled = true;
        game.Fishing.StartFishing();

        Assert.True(game.DevTools.AdvanceTime(1, online: true).Succeeded);
        var online = game.Fishing.Sync();
        Assert.Equal(120, online.NewCatches.Count); // one every 30 s
        Assert.Null(game.Fishing.TakeOfflineReport());

        Assert.True(game.DevTools.AdvanceTime(1, online: false).Succeeded);
        game.Fishing.Sync();
        var report = game.Fishing.TakeOfflineReport();
        Assert.NotNull(report);
        Assert.Equal(60, report.Update.NewCatches.Count); // one every 60 s
    }
}
