using FishingIdle.GameService.Core;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>A-122: the owner can call the Cardume back early; it brings nothing.</summary>
public sealed class ExpeditionCancelTests
{
    [Fact]
    public void Cancelling_brings_the_cardume_back_with_nothing()
    {
        var (game, clock, _) = ExpeditionTests.GameWithCardume(3);
        var coins = game.Session.Save.Coins;
        Assert.Equal(ServiceError.ExpeditionNotActive, game.Expeditions.Cancel().Error);

        Assert.True(game.Expeditions.Start("exp_30m").Succeeded);
        clock.AdvanceSeconds(600);
        Assert.True(game.Expeditions.Cancel().Succeeded);

        Assert.Null(game.Expeditions.GetExpeditions().Active);
        clock.AdvanceSeconds(3600);
        Assert.Null(game.Expeditions.Update());
        Assert.Equal(coins, game.Session.Save.Coins);
        Assert.False(game.Expeditions.CardumeLocked);
    }
}
