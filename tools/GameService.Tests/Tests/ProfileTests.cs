using FishingIdle.GameService.Aquarium;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Profile;
using Xunit;

namespace FishingIdle.GameService.Tests;

public sealed class ProfileTests
{
    private static (LocalGame Game, ManualClock Clock, string Dir) GameWithAquarium(int keep)
    {
        var (game, clock, dir) = TestSupport.NewGame();
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, 1800, stepSeconds: 30);
        game.Fishing.StopFishing();
        game.Aquarium.KeepCatches(game.Fishing.GetFishingBox().Take(keep).Select(c => c.CatchId).ToList());
        return (game, clock, dir);
    }

    private static List<long> FishIds(LocalGame game) => game.Aquarium.GetAquarium(AquariumSort.Newest).Fish.Select(f => f.FishId).OrderBy(i => i).ToList();

    [Fact]
    public void A_new_player_owns_and_wears_the_starter_rod_and_has_an_empty_cardume()
    {
        var (game, _, _) = TestSupport.NewGame();

        var profile = game.Profile.GetProfile();
        var cardume = game.Cardume.GetCardume();

        Assert.Single(profile.Inventory);
        Assert.True(profile.EquippedRod.IsEquipped);
        Assert.Equal("Vara Inicial", profile.EquippedRod.Name);
        Assert.False(profile.EquippedRod.CanCatchRare);
        Assert.Equal(6, cardume.Slots.Count);
        Assert.Equal(0, cardume.Filled);
        Assert.Equal(new[] { true, true, true, false, false, false }, cardume.Slots.Select(s => s.IsFront));
    }

    [Fact]
    public void Fish_can_be_placed_moved_and_removed_and_each_fish_appears_once()
    {
        var (game, _, _) = GameWithAquarium(3);
        var ids = FishIds(game);

        game.Cardume.SetSlot(1, ids[0]);
        game.Cardume.SetSlot(5, ids[1]);
        var moved = game.Cardume.SetSlot(3, ids[0]).Value;

        Assert.Null(moved.Slots[0].Fish);
        Assert.Equal(ids[0], moved.Slots[2].Fish.FishId);
        Assert.Equal(ids[1], moved.Slots[4].Fish.FishId);
        Assert.Equal(2, moved.Filled);
        Assert.Equal(3, game.Aquarium.GetFish(ids[0]).CardumePosition);

        var cleared = game.Cardume.ClearSlot(3).Value;
        Assert.Equal(1, cleared.Filled);
        Assert.Equal(ServiceError.InvalidCardumePosition, game.Cardume.SetSlot(7, ids[2]).Error);
        Assert.Equal(ServiceError.FishNotFound, game.Cardume.SetSlot(2, 9999).Error);
    }

    [Fact]
    public void The_complete_cardume_bonus_needs_all_six_and_is_not_written_to_the_fish()
    {
        var (game, _, _) = GameWithAquarium(6);
        var ids = FishIds(game);
        for (var i = 0; i < 5; i++)
        {
            game.Cardume.SetSlot(i + 1, ids[i]);
        }

        var five = game.Cardume.GetCardume();
        Assert.False(five.CompleteBonusActive);
        Assert.Equal(five.Slots[0].Fish.Stats.Attack, five.Slots[0].EffectiveStats.Attack);

        var six = game.Cardume.SetSlot(6, ids[5]).Value;
        Assert.True(six.CompleteBonusActive);
        Assert.Equal(six.Slots[0].Fish.Stats.Attack * 1.03, six.Slots[0].EffectiveStats.Attack, 6);
        Assert.True(six.Strength > five.Strength);
        // The fish itself keeps its own stats.
        Assert.Equal(five.Slots[0].Fish.Stats.Attack, game.Aquarium.GetFish(ids[0]).Stats.Attack, 6);
    }

    [Fact]
    public void Strength_follows_the_gdd_formula()
    {
        var (game, _, _) = GameWithAquarium(1);
        var fish = game.Aquarium.GetAquarium(AquariumSort.Size).Fish[0];
        game.Cardume.SetSlot(1, fish.FishId);

        var s = fish.Stats;
        var expected = (long)Math.Round(s.Attack * 2 + s.Defense * 1.5 + s.Hp / 10 + s.Speed * 0.5, MidpointRounding.AwayFromZero);

        Assert.Equal(expected, game.Cardume.GetCardume().Strength);
        Assert.Equal(expected, game.Profile.GetProfile().CardumeStrength);
    }

    [Fact]
    public void Selling_or_eating_a_cardume_fish_leaves_its_position_empty_and_asks_first()
    {
        var (game, _, _) = GameWithAquarium(3);
        var ids = FishIds(game);
        game.Cardume.SetSlot(1, ids[0]);
        game.Cardume.SetSlot(2, ids[1]);

        var feedPreview = game.Aquarium.PreviewFeed(ids[2], null, new[] { ids[0] });
        Assert.Single(feedPreview.Value.CardumeFood);
        Assert.True(feedPreview.Value.NeedsConfirmation);
        Assert.True(game.Aquarium.PreviewSale(new[] { ids[1] }).NeedsConfirmation);

        game.Aquarium.Feed(ids[2], null, new[] { ids[0] });
        game.Aquarium.SellFish(new[] { ids[1] });

        Assert.Equal(0, game.Cardume.GetCardume().Filled);
    }

    [Fact]
    public void Cardume_and_inventory_survive_a_restart()
    {
        var (game, clock, dir) = GameWithAquarium(2);
        var ids = FishIds(game);
        game.Cardume.SetSlot(4, ids[1]);

        var (reopened, _, _) = TestSupport.NewGame(saveDir: dir, clock: clock);

        Assert.Equal(ids[1], reopened.Cardume.GetCardume().Slots[3].Fish.FishId);
        Assert.Single(reopened.Profile.GetProfile().Inventory);
    }

    [Fact]
    public void Equipping_checks_the_item_and_the_map()
    {
        var (game, _, _) = TestSupport.NewGame();
        var starter = game.Profile.GetProfile().EquippedRod;

        Assert.True(game.Profile.EquipRod(starter.ItemId).Succeeded);
        Assert.Equal(ServiceError.ItemNotFound, game.Profile.EquipRod(999).Error);
    }

    [Fact]
    public void The_encyclopedia_hides_species_until_first_caught_and_keeps_records()
    {
        var (game, _, _) = GameWithAquarium(0);
        var profile = game.Profile.GetProfile();
        var discovered = profile.Encyclopedia.Where(e => e.Discovered).ToList();

        Assert.Equal(40, profile.Encyclopedia.Count);
        Assert.NotEmpty(discovered);
        Assert.All(profile.Encyclopedia.Where(e => !e.Discovered), e => Assert.Null(e.Name));
        Assert.All(discovered, e => Assert.True(e.LargestCm > 0 && e.TimesCaught > 0));
        Assert.Contains(profile.Encyclopedia, e => e.SpeciesId == "aruana" && !e.Discovered);

        // Selling everything does not erase the discoveries.
        game.Fishing.SellCatches(game.Fishing.GetFishingBox().Select(c => c.CatchId).ToList());
        Assert.Equal(discovered.Count, game.Profile.GetProfile().Encyclopedia.Count(e => e.Discovered));
        Assert.Equal(discovered.Count, game.Profile.GetProfile().Records.SpeciesDiscovered);
        Assert.Equal(discovered.Max(e => e.LargestCm), game.Profile.GetProfile().Records.BiggestCm);
    }
}
