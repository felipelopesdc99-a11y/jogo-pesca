using FishingIdle.GameService.Aquarium;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Persistence;
using FishingIdle.GameService.Tutorial;
using Xunit;

namespace FishingIdle.GameService.Tests;

public sealed class TutorialTests
{
    private static (LocalGame Game, ManualClock Clock, string Dir) Newcomer() => TestSupport.NewGame(skipTutorial: false);

    [Fact]
    public void A_new_player_has_no_rod_and_must_claim_the_free_starter_rod_in_the_shop()
    {
        var (game, _, _) = Newcomer();

        Assert.Equal(TutorialSteps.Welcome, game.Tutorial.Get().Step);
        Assert.Empty(game.Profile.GetProfile().Inventory);
        Assert.Equal(ServiceError.NoRod, game.Fishing.StartFishing().Error);
        var offer = game.Shop.GetShop().Rods.Single(r => r.IsFree);
        Assert.Equal(0, offer.PriceCoins);
        Assert.Equal(ServiceError.None, offer.BuyBlocker);

        var claimed = game.Shop.BuyRod(offer.RodId);

        Assert.True(claimed.Succeeded, claimed.ErrorMessage);
        Assert.Equal(offer.RodId, game.Profile.GetProfile().EquippedRod.RodId);
        Assert.Equal(0, game.Player.GetPlayer().Coins);
        Assert.DoesNotContain(game.Shop.GetShop().Rods, r => r.IsFree);
        Assert.Equal(ServiceError.RodAlreadyOwned, game.Shop.BuyRod(offer.RodId).Error);
        Assert.True(game.Fishing.StartFishing().Succeeded);
    }

    [Fact]
    public void The_steps_follow_what_the_player_actually_does()
    {
        var (game, clock, _) = Newcomer();
        var t = game.Tutorial;

        Assert.Equal(ServiceError.TutorialStepMismatch, t.Acknowledge(TutorialSteps.OpenBox).Error);
        Assert.Equal(TutorialSteps.ClaimRod, t.Acknowledge(TutorialSteps.Welcome).Value.Step);
        game.Shop.BuyRod("rod_00_starter");
        Assert.Equal(TutorialSteps.StartFishing, t.Get().Step);
        game.Fishing.StartFishing();
        Assert.Equal(TutorialSteps.FirstCatch, t.Get().Step);
        TestSupport.PlayFor(game, clock, 95);
        Assert.Equal(TutorialSteps.OpenBox, t.Get().Step);
        Assert.True(t.Get().NeedsAcknowledge);
        Assert.Equal(TutorialSteps.SellFish, t.Acknowledge(TutorialSteps.OpenBox).Value.Step);

        var box = game.Fishing.GetFishingBox().Select(c => c.CatchId).ToList();
        game.Fishing.SellCatches(box.Take(1).ToList());
        Assert.Equal(TutorialSteps.KeepFish, t.Get().Step);
        game.Aquarium.KeepCatches(box.Skip(1).Take(1).ToList());
        Assert.Equal(TutorialSteps.Cardume, t.Get().Step);
        game.Cardume.SetSlot(1, game.Aquarium.GetAquarium(AquariumSort.Size).Fish[0].FishId);
        Assert.Equal(TutorialSteps.Expedition, t.Get().Step);

        var end = t.Acknowledge(TutorialSteps.Expedition);

        Assert.True(end.Succeeded);
        Assert.False(end.Value.Active);
        Assert.False(t.Get().Active);
    }

    [Fact]
    public void Skipping_ends_the_tutorial_and_never_leaves_the_player_without_a_rod()
    {
        var (game, _, dir) = Newcomer();

        var view = game.Tutorial.Skip();

        Assert.False(view.Active);
        Assert.NotNull(game.Profile.GetProfile().EquippedRod);
        Assert.True(game.Fishing.StartFishing().Succeeded);
        var reopened = TestSupport.NewGame(null, dir, skipTutorial: false).Game;
        Assert.False(reopened.Tutorial.Get().Active);
    }

    [Fact]
    public void A_newcomer_without_a_rod_survives_a_restart()
    {
        var (_, clock, dir) = Newcomer();

        var reopened = TestSupport.NewGame(null, dir, clock, skipTutorial: false).Game;

        Assert.True(reopened.Tutorial.Get().Active);
        Assert.Empty(reopened.Profile.GetProfile().Inventory);
        Assert.Equal(PlayerSave.CurrentVersion, reopened.Session.Save.SaveVersion);
    }
}
