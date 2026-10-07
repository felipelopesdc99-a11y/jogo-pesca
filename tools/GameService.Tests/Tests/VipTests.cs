using FishingIdle.GameService.Core;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>The VIP (A-110): 100 Dólares, 30 days, +50% Fisher XP on offline fishing only.</summary>
public sealed class VipTests
{
    private const long Day = 86_400_000L;

    [Fact]
    public void Buying_needs_the_dollars()
    {
        var (game, _, _) = TestSupport.NewGame();
        var result = game.Vip.BuyVip();

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceError.NotEnoughDollars, result.Error);
        Assert.False(game.Vip.GetVip().Active);
    }

    [Fact]
    public void Buying_spends_100_dollars_for_30_days_and_buying_again_adds_to_the_end()
    {
        var (game, clock, _) = TestSupport.NewGame();
        game.Session.Save.Dollars = 250;

        Assert.True(game.Vip.BuyVip().Succeeded);
        Assert.Equal(150, game.Session.Save.Dollars);
        Assert.Equal(clock.UtcNowMs + 30 * Day, game.Session.Save.VipUntilMs);

        clock.AdvanceSeconds(10 * 86400);
        Assert.True(game.Vip.BuyVip().Succeeded);
        Assert.Equal(50, game.Session.Save.Dollars);
        Assert.Equal(TestSupport.StartMs + 60 * Day, game.Session.Save.VipUntilMs);
        Assert.True(game.Vip.GetVip().Active);
        Assert.Equal(ServiceError.NotEnoughDollars, game.Vip.BuyVip().Error);
    }

    [Fact]
    public void Offline_catches_give_half_more_fisher_xp_with_the_vip()
    {
        var report = Offline(vipHours: 48, closedHours: 3);

        var baseXp = report.Update.XpGained - report.Update.VipXpGained;
        Assert.True(report.Update.VipXpGained > 0);
        Assert.InRange(report.Update.VipXpGained / (double)baseXp, 0.4, 0.5);
    }

    [Fact]
    public void Without_the_vip_offline_xp_is_unchanged()
    {
        var report = Offline(vipHours: 0, closedHours: 3);
        Assert.Equal(0, report.Update.VipXpGained);
    }

    [Fact]
    public void A_vip_that_ends_while_away_counts_only_until_it_ends()
    {
        var report = Offline(vipHours: 1, closedHours: 3);

        var baseXp = report.Update.XpGained - report.Update.VipXpGained;
        Assert.True(report.Update.VipXpGained > 0);
        Assert.InRange(report.Update.VipXpGained / (double)baseXp, 0.08, 0.3);
    }

    [Fact]
    public void Online_catches_get_no_vip_bonus()
    {
        var (game, clock, _) = TestSupport.NewGame();
        game.Session.Save.VipUntilMs = clock.UtcNowMs + 30 * Day;
        game.Fishing.StartFishing();
        clock.AdvanceSeconds(600);

        var update = game.Fishing.Sync();
        Assert.NotEmpty(update.NewCatches);
        Assert.Equal(0, update.VipXpGained);
    }

    [Fact]
    public void Every_10_fisher_levels_give_10_dollars()
    {
        var (game, clock, _) = TestSupport.NewGame();
        var save = game.Session.Save;
        save.FisherLevel = 9;
        save.FisherXp = game.Session.Config.FisherXpToNextLevel(9) - 1;
        game.Fishing.StartFishing();
        clock.AdvanceSeconds(60);

        var update = game.Fishing.Sync();
        Assert.Contains(10, update.LevelsReached);
        Assert.Equal(10, update.DollarsGained);
        Assert.Equal(10, save.Dollars);

        save.FisherLevel = 10;
        save.FisherXp = 0;
        clock.AdvanceSeconds(60);
        Assert.Equal(0, game.Fishing.Sync().DollarsGained);
    }

    private static FishingIdle.GameService.Fishing.OfflineReport Offline(double vipHours, double closedHours)
    {
        var saveDir = TestSupport.NewTempDirectory();
        var clock = new ManualClock(TestSupport.StartMs);
        var (game, _, _) = TestSupport.NewGame(saveDir: saveDir, clock: clock);
        game.Session.Save.VipUntilMs = vipHours > 0 ? clock.UtcNowMs + (long)(vipHours * 3_600_000) : 0;
        game.Fishing.StartFishing();
        game.Fishing.MarkSeen();

        clock.AdvanceSeconds(closedHours * 3600);
        var (reopened, _, _) = TestSupport.NewGame(saveDir: saveDir, clock: clock);
        var report = reopened.Fishing.TakeOfflineReport();
        Assert.NotNull(report);
        Assert.NotEmpty(report.Update.NewCatches);
        return report;
    }
}
