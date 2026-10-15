using FishingIdle.GameService.Core;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>OD-009: the Arena Shop sells Conchas and Dólares for Honor; Dólares have a weekly limit.</summary>
public sealed class ArenaShopTests
{
    [Fact]
    public void Honor_buys_shells()
    {
        var (game, _, _) = TestSupport.NewGame();
        var save = game.Session.Save;
        save.Arena.Honor = 250;
        var shells = save.Shells;

        Assert.True(game.Arena.BuyShopItem("arena_shells_20").Succeeded);
        Assert.Equal(150, save.Arena.Honor);
        Assert.Equal(shells + 20, save.Shells);
    }

    [Fact]
    public void Not_enough_honor_is_refused()
    {
        var (game, _, _) = TestSupport.NewGame();
        game.Session.Save.Arena.Honor = 50;
        Assert.Equal(ServiceError.NotEnoughHonor, game.Arena.BuyShopItem("arena_shells_20").Error);
        Assert.Equal(50, game.Session.Save.Arena.Honor);
    }

    [Fact]
    public void Dollars_have_a_weekly_limit_that_resets_after_7_days()
    {
        var (game, clock, _) = TestSupport.NewGame();
        var save = game.Session.Save;
        save.Arena.Honor = 2000;

        Assert.True(game.Arena.BuyShopItem("arena_dollars_5").Succeeded);
        Assert.True(game.Arena.BuyShopItem("arena_dollars_5").Succeeded);
        Assert.Equal(ServiceError.ArenaWeeklyLimit, game.Arena.BuyShopItem("arena_dollars_5").Error);
        Assert.Equal(10, save.Dollars);

        clock.AdvanceSeconds(7 * 86400 + 1);
        Assert.True(game.Arena.BuyShopItem("arena_dollars_5").Succeeded);
        Assert.Equal(15, save.Dollars);
    }
}
