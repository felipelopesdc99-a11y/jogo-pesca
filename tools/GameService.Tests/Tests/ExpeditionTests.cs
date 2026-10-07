using FishingIdle.GameService.Aquarium;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Expeditions;
using Xunit;

namespace FishingIdle.GameService.Tests;

public sealed class ExpeditionTests
{
    internal static (LocalGame Game, ManualClock Clock, string Dir) GameWithCardume(int fish)
    {
        var (game, clock, dir) = TestSupport.NewGame();
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, 1800, stepSeconds: 30);
        game.Fishing.StopFishing();
        game.Aquarium.KeepCatches(game.Fishing.GetFishingBox().Take(fish + 1).Select(c => c.CatchId).ToList());
        var ids = game.Aquarium.GetAquarium(AquariumSort.Newest).Fish.Select(f => f.FishId).OrderBy(i => i).ToList();
        for (var i = 0; i < fish; i++)
        {
            game.Cardume.SetSlot(i + 1, ids[i]);
        }

        return (game, clock, dir);
    }

    [Fact]
    public void Efficiency_follows_the_configured_curve()
    {
        var config = TestSupport.RealConfig();

        Assert.Equal(1.0, ExpeditionRules.Efficiency(config, 450, 450), 6);
        Assert.Equal(Math.Pow(0.5, 0.8), ExpeditionRules.Efficiency(config, 225, 450), 6);
        Assert.Equal(0.25, ExpeditionRules.Efficiency(config, 1, 450), 6); // floor
        Assert.Equal(1.35, ExpeditionRules.Efficiency(config, 900, 450), 6); // 1 + 0.35 × 1
        Assert.Equal(1.5, ExpeditionRules.Efficiency(config, 5000, 450), 6); // cap
    }

    [Fact]
    public void An_empty_cardume_cannot_leave()
    {
        var (game, _, _) = TestSupport.NewGame();

        Assert.Equal(ServiceError.CardumeEmpty, game.Expeditions.Start("exp_30m").Error);
        Assert.Equal(ServiceError.ExpeditionNotFound, game.Expeditions.Start("nope").Error);
    }

    [Fact]
    public void An_expedition_pays_coins_when_its_time_is_up_and_only_once()
    {
        var (game, clock, _) = GameWithCardume(3);
        var offer = game.Expeditions.GetExpeditions().Expeditions.Single(e => e.ExpeditionId == "exp_30m");
        var coins = game.Player.GetPlayer().Coins;

        Assert.True(game.Expeditions.Start("exp_30m").Succeeded);
        Assert.Equal(ServiceError.ExpeditionActive, game.Expeditions.Start("exp_1h").Error);

        clock.AdvanceSeconds(29 * 60);
        Assert.Null(game.Expeditions.Update());

        clock.AdvanceSeconds(60);
        var result = game.Expeditions.Update();
        Assert.NotNull(result);
        Assert.Equal(offer.ExpectedCoins, result.Coins);
        Assert.Equal(coins + result.Coins, game.Player.GetPlayer().Coins);
        Assert.Null(game.Expeditions.Update());
        Assert.Equal(coins + result.Coins, game.Player.GetPlayer().Coins);

        Assert.NotNull(game.Expeditions.PendingResult());
        game.Expeditions.AcknowledgeResult();
        Assert.Null(game.Expeditions.PendingResult());
    }

    [Fact]
    public void The_report_names_the_expedition_and_waits_until_it_is_read()
    {
        var (game, clock, dir) = GameWithCardume(3);
        game.Expeditions.Start("exp_30m");
        clock.AdvanceSeconds(31 * 60);
        game.Expeditions.Update();

        var report = game.Expeditions.PendingResult();
        Assert.Equal("exp_30m", report.ExpeditionId);
        Assert.Equal(report.FoundFish != null, report.FoundAFish);

        // Not read yet: still there after the game is closed and opened again.
        var (reopened, _, _) = TestSupport.NewGame(saveDir: dir, clock: clock);
        Assert.NotNull(reopened.Expeditions.PendingResult());
        reopened.Expeditions.AcknowledgeResult();
        Assert.Null(reopened.Expeditions.PendingResult());
    }

    [Fact]
    public void An_expedition_finished_while_closed_is_paid_on_start()
    {
        var (game, clock, dir) = GameWithCardume(2);
        game.Expeditions.Start("exp_6h");
        var coins = game.Player.GetPlayer().Coins;
        clock.AdvanceSeconds(8 * 3600);

        var (reopened, _, _) = TestSupport.NewGame(saveDir: dir, clock: clock);

        Assert.True(reopened.Player.GetPlayer().Coins > coins);
        Assert.NotNull(reopened.Expeditions.PendingResult());
        Assert.False(reopened.Expeditions.CardumeLocked);
    }

    [Fact]
    public void The_cardume_is_locked_while_away_but_fishing_continues()
    {
        var (game, clock, _) = GameWithCardume(2);
        var member = game.Cardume.GetCardume().Slots[0].Fish.FishId;
        var outsider = game.Aquarium.GetAquarium(AquariumSort.Newest).Fish.Single(f => f.CardumePosition == 0).FishId;
        game.Expeditions.Start("exp_1h");

        Assert.True(game.Expeditions.CardumeLocked);
        Assert.Equal(ServiceError.CardumeLocked, game.Cardume.SetSlot(6, outsider).Error);
        Assert.Equal(ServiceError.CardumeLocked, game.Cardume.ClearSlot(1).Error);
        Assert.Equal(ServiceError.CardumeLocked, game.Aquarium.SellFish(new[] { member }).Error);
        Assert.Equal(ServiceError.CardumeLocked, game.Aquarium.PreviewFeed(outsider, null, new[] { member }).Error);
        Assert.Equal(ServiceError.CardumeLocked, game.Aquarium.PreviewFeed(member, game.Fishing.GetFishingBox().Take(1).Select(c => c.CatchId).ToList(), null).Error);

        // Fish outside the Cardume and fishing itself are unaffected.
        Assert.True(game.Aquarium.SellFish(new[] { outsider }).Succeeded);
        Assert.True(game.Fishing.StartFishing().Succeeded);
        Assert.NotEmpty(TestSupport.PlayFor(game, clock, 60));
    }

    [Fact]
    public void Found_fish_go_to_the_fishing_box_without_xp()
    {
        var (game, clock, _) = GameWithCardume(6);
        var found = 0;
        for (var run = 0; run < 60 && found == 0; run++)
        {
            var xp = game.Player.GetPlayer().FisherXp;
            var level = game.Player.GetPlayer().FisherLevel;
            var box = game.Fishing.GetFishingBox().Count;
            game.Expeditions.Start("exp_6h");
            clock.AdvanceSeconds(6 * 3600);
            var result = game.Expeditions.Update();
            if (result.FoundFish != null)
            {
                found++;
                Assert.Equal(box + 1, game.Fishing.GetFishingBox().Count);
                Assert.Equal((level, xp), (game.Player.GetPlayer().FisherLevel, game.Player.GetPlayer().FisherXp));
            }
        }

        Assert.Equal(1, found);
    }
}
