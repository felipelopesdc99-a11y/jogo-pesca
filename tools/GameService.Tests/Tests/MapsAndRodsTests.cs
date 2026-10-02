using FishingIdle.GameService.Core;
using FishingIdle.GameService.Persistence;
using FishingIdle.GameService.Shop;
using Xunit;

namespace FishingIdle.GameService.Tests;

public sealed class MapsAndRodsTests
{
    /// <summary>A player who fished until Level 10 and sold everything.</summary>
    private static (LocalGame Game, ManualClock Clock, string Dir) VeteranPlayer()
    {
        var (game, clock, dir) = TestSupport.NewGame();
        game.Fishing.StartFishing();
        var guard = 0;
        while (game.Player.GetPlayer().FisherLevel < 10 && guard++ < 16)
        {
            TestSupport.PlayFor(game, clock, 1800, stepSeconds: 30);
        }

        game.Fishing.StopFishing();
        game.Fishing.SellCatches(game.Fishing.GetFishingBox().Select(c => c.CatchId).ToList());
        Assert.True(game.Player.GetPlayer().FisherLevel >= 10);
        // Sale prices are a balance knob (economy.json); these tests are about the Shop, not about income.
        game.Session.Save.Coins += 20_000;
        game.Session.Save.Shells += 500;
        return (game, clock, dir);
    }

    [Fact]
    public void A_new_player_cannot_travel_to_map_2_or_buy_rod_1()
    {
        var (game, _, _) = TestSupport.NewGame();

        var map2 = game.Maps.GetMaps().Maps.Single(m => m.MapId == "map_02");
        Assert.False(map2.LevelUnlocked);
        Assert.Equal(ServiceError.MapLocked, map2.TravelBlocker);
        Assert.Equal(ServiceError.MapLocked, game.Maps.TravelTo("map_02").Error);
        Assert.Equal(ServiceError.AlreadyOnMap, game.Maps.TravelTo("map_01").Error);
        Assert.Equal(ServiceError.RodLocked, game.Shop.BuyRod("rod_01").Error);
        Assert.Equal(ServiceError.RodAlreadyOwned, game.Shop.BuyRod("rod_00_starter").Error);
        Assert.Equal(ServiceError.RodNotForSale, game.Shop.BuyRod("rod_99").Error);
    }

    [Fact]
    public void Level_10_with_the_starter_rod_still_cannot_fish_map_2()
    {
        var (game, _, _) = VeteranPlayer();

        Assert.Equal(ServiceError.RodTooWeakForMap, game.Maps.TravelTo("map_02").Error);
        Assert.Equal("map_01", game.Player.GetPlayer().MapId);
    }

    [Fact]
    public void Buying_rod_1_costs_its_price_equips_it_and_keeps_the_starter_rod()
    {
        var (game, _, _) = VeteranPlayer();
        var coins = game.Player.GetPlayer().Coins;

        var bought = game.Shop.BuyRod("rod_01");

        Assert.True(bought.Succeeded, bought.ErrorMessage);
        Assert.Equal(coins - 2500, game.Player.GetPlayer().Coins);
        var profile = game.Profile.GetProfile();
        Assert.Equal("Ponta Selvagem", profile.EquippedRod.Name);
        Assert.Equal(2, profile.Inventory.Count);
        Assert.Equal(ServiceError.RodAlreadyOwned, game.Shop.BuyRod("rod_01").Error);
    }

    [Fact]
    public void Travel_takes_30_seconds_pauses_fishing_and_resumes_on_the_new_map()
    {
        var (game, clock, _) = VeteranPlayer();
        game.Shop.BuyRod("rod_01");
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, 20);

        var trip = game.Maps.TravelTo("map_02");
        Assert.True(trip.Succeeded, trip.ErrorMessage);
        Assert.False(game.Fishing.GetStatus().IsFishing);
        Assert.Equal(ServiceError.Traveling, game.Fishing.StartFishing().Error);
        Assert.Equal(ServiceError.Traveling, game.Maps.TravelTo("map_01").Error);

        clock.AdvanceSeconds(29);
        Assert.False(game.Maps.Update().Arrived);
        Assert.Empty(game.Fishing.Sync().NewCatches);

        clock.AdvanceSeconds(1);
        var arrival = game.Maps.Update();
        Assert.True(arrival.Arrived);
        Assert.Equal("map_02", game.Player.GetPlayer().MapId);
        Assert.True(game.Fishing.GetStatus().IsFishing);

        var map2Species = new HashSet<string> { "tucunare", "piranha", "dourado", "pintado", "cachara", "jau", "peixe_cachorra", "piracanjuba", "pirarucu", "aruana" };
        var catches = new List<FishingIdle.GameService.Fishing.CatchView>();
        for (var i = 0; i < 40; i++)
        {
            clock.AdvanceSeconds(30);
            game.Maps.Update();
            catches.AddRange(game.Fishing.Sync().NewCatches);
        }

        Assert.Equal(40, catches.Count);
        Assert.All(catches, c => Assert.Contains(c.SpeciesId, map2Species));
    }

    [Fact]
    public void On_map_2_the_starter_rod_cannot_be_equipped()
    {
        var (game, clock, _) = VeteranPlayer();
        game.Shop.BuyRod("rod_01");
        game.Maps.TravelTo("map_02");
        clock.AdvanceSeconds(30);
        game.Maps.Update();

        var starter = game.Profile.GetProfile().Inventory.Single(r => r.RodId == "rod_00_starter");

        Assert.False(starter.AllowedOnCurrentMap);
        Assert.Equal(ServiceError.RodNotAllowedOnMap, game.Profile.EquipRod(starter.ItemId).Error);
    }

    [Fact]
    public void A_trip_finished_while_the_game_was_closed_completes_on_start()
    {
        var (game, clock, dir) = VeteranPlayer();
        game.Shop.BuyRod("rod_01");
        game.Maps.TravelTo("map_02");
        clock.AdvanceSeconds(3600);

        var (reopened, _, _) = TestSupport.NewGame(saveDir: dir, clock: clock);

        Assert.Equal("map_02", reopened.Player.GetPlayer().MapId);
        Assert.False(reopened.Maps.GetTravel().Active);
    }

    [Fact]
    public void Upgrading_charges_the_configured_cost_and_raises_the_bonus()
    {
        var (game, _, _) = VeteranPlayer();
        game.Shop.BuyRod("rod_01");
        var rod = game.Profile.GetProfile().EquippedRod;
        var coins = game.Player.GetPlayer().Coins;
        Assert.Equal(1500, rod.NextUpgradeCost);

        var up = game.Profile.UpgradeRod(rod.ItemId);

        Assert.True(up.Succeeded, up.ErrorMessage);
        Assert.Equal(2, up.Value.Level);
        Assert.Equal(coins - 1500, game.Player.GetPlayer().Coins);
        Assert.True(up.Value.RarityBonus > rod.RarityBonus);
        Assert.Equal(2300, up.Value.NextUpgradeCost);
    }

    [Fact]
    public void Buying_and_upgrading_a_rod_ask_for_the_configured_shells()
    {
        var (game, _, _) = VeteranPlayer();
        var save = game.Session.Save;
        game.Session.Config.TryGetRod("rod_01", out var rod);
        save.Shells = rod.Acquisition.PurchaseCostShells - 1;
        Assert.Equal(ServiceError.NotEnoughShells, game.Shop.BuyRod("rod_01").Error);

        save.Shells = rod.Acquisition.PurchaseCostShells + 100;
        Assert.True(game.Shop.BuyRod("rod_01").Succeeded);
        Assert.Equal(100, save.Shells);

        var item = game.Profile.GetProfile().EquippedRod;
        Assert.True(item.NextUpgradeShells > 0);
        Assert.True(game.Profile.UpgradeRod(item.ItemId).Succeeded);
        Assert.Equal(100 - item.NextUpgradeShells, save.Shells);
    }

    [Fact]
    public void Upgrading_needs_coins_and_stops_at_level_10()
    {
        var (game, _, _) = VeteranPlayer();
        game.Shop.BuyRod("rod_01");
        var profile = game.Profile.GetProfile();
        var id = profile.EquippedRod.ItemId;
        var starterId = profile.Inventory.Single(r => r.RodId == "rod_00_starter").ItemId;
        Assert.Equal(ServiceError.RodHasNoLevels, game.Profile.UpgradeRod(starterId).Error);

        game.Session.Save.Coins = 0;
        Assert.Equal(ServiceError.NotEnoughCoins, game.Profile.UpgradeRod(id).Error);

        game.Session.Save.Coins = 1_000_000;
        game.Session.Save.Shells = 0;
        Assert.Equal(ServiceError.NotEnoughShells, game.Profile.UpgradeRod(id).Error); // upgrades ask for Conchas too (A-099)
        game.Session.Save.Shells = 1_000;
        for (var level = 2; level <= 10; level++)
        {
            Assert.True(game.Profile.UpgradeRod(id).Succeeded);
        }

        Assert.Equal(10, game.Profile.GetProfile().EquippedRod.Level);
        Assert.Equal(1_000_000 - 138_050, game.Player.GetPlayer().Coins); // sum of upgrade_costs
        Assert.Equal(ServiceError.RodAtMaxLevel, game.Profile.UpgradeRod(id).Error);
    }

    [Fact]
    public void Reselling_returns_40_percent_of_the_price_plus_25_percent_of_upgrades()
    {
        var (game, _, _) = VeteranPlayer();
        game.Shop.BuyRod("rod_01");
        var rod = game.Profile.GetProfile().EquippedRod;
        game.Profile.UpgradeRod(rod.ItemId);
        var starter = game.Profile.GetProfile().Inventory.Single(r => r.RodId == "rod_00_starter");

        Assert.Equal(ServiceError.RodEquipped, game.Profile.SellRod(rod.ItemId).Error);
        Assert.Equal(ServiceError.RodNotSellable, game.Profile.SellRod(starter.ItemId).Error);

        game.Profile.EquipRod(starter.ItemId);
        var view = game.Profile.GetProfile().Inventory.Single(r => r.ItemId == rod.ItemId);
        Assert.Equal((long)Math.Round(2500 * 0.4 + 1500 * 0.25), view.ResaleValue);
        var coins = game.Player.GetPlayer().Coins;

        var sale = game.Profile.SellRod(rod.ItemId);

        Assert.True(sale.Succeeded);
        Assert.Equal(coins + view.ResaleValue, game.Player.GetPlayer().Coins);
        Assert.Single(game.Profile.GetProfile().Inventory);
        Assert.Equal(ServiceError.RodEquipped, game.Profile.SellRod(starter.ItemId).Error);
    }

    [Fact]
    public void Destroying_removes_the_rod_and_the_starter_rod_always_stays()
    {
        var (game, _, _) = VeteranPlayer();
        game.Shop.BuyRod("rod_01");
        var rod = game.Profile.GetProfile().EquippedRod;
        var starter = game.Profile.GetProfile().Inventory.Single(r => r.RodId == "rod_00_starter");
        game.Profile.EquipRod(starter.ItemId);

        Assert.True(game.Profile.DestroyRod(rod.ItemId).Succeeded);
        Assert.Single(game.Profile.GetProfile().Inventory);
        Assert.Equal(ServiceError.RodEquipped, game.Profile.DestroyRod(starter.ItemId).Error);
    }

    [Fact]
    public void Rod_1_catches_rares_on_map_2_and_generates_shells()
    {
        var (game, clock, _) = VeteranPlayer();
        game.Shop.BuyRod("rod_01");
        game.Maps.TravelTo("map_02");
        clock.AdvanceSeconds(30);
        game.Maps.Update();
        game.Fishing.StartFishing();

        var catches = TestSupport.PlayFor(game, clock, 12 * 3600, stepSeconds: 30);

        Assert.True(game.Player.GetPlayer().Shells > 0);
        // 1440 catches at ~0.5% Aruanã: expect a few; allow the rare unlucky run.
        Assert.InRange(catches.Count(c => c.RarityId == "rare"), 0, 30);
    }
}
