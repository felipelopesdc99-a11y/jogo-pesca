using FishingIdle.GameService.Arena;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>Problems found in the full project review (after Milestone 11), each pinned by a test.</summary>
public sealed class ReviewFixTests
{
    /// <summary>A Level 10 player with Vara 1, ready to go to the second map.</summary>
    private static (LocalGame Game, ManualClock Clock, string Dir) Traveller()
    {
        var (game, clock, dir) = TestSupport.NewGame();
        game.Fishing.StartFishing();
        var guard = 0;
        while (game.Player.GetPlayer().FisherLevel < 10 && guard++ < 16)
        {
            TestSupport.PlayFor(game, clock, 1800, stepSeconds: 30);
        }

        game.Fishing.SellCatches(game.Fishing.GetFishingBox().Select(c => c.CatchId).ToList());
        // Level 10 now comes in about an hour (M24-T03): the Conchas of so few catches are a matter of luck.
        game.Session.Save.Shells += 50;
        Assert.True(game.Shop.BuyRod("rod_01").Succeeded);
        return (game, clock, dir);
    }

    [Fact]
    public void Time_closed_after_the_boat_arrived_counts_as_offline_fishing()
    {
        var (game, clock, dir) = Traveller();
        Assert.True(game.Maps.TravelTo("map_02").Succeeded);
        game.Fishing.MarkSeen();
        var before = game.Player.GetPlayer().TotalCatches;

        // Closed during the trip; reopened two hours after the boat arrived.
        clock.AdvanceSeconds(30 + 2 * 3600);
        var reopened = TestSupport.NewGame(null, dir, clock).Game;

        Assert.Equal("map_02", reopened.Player.GetPlayer().MapId);
        Assert.True(reopened.Fishing.GetStatus().IsFishing);
        var offline = reopened.Player.GetPlayer().TotalCatches - before;
        Assert.InRange(offline, 115, 120); // 2 h at one catch per 60 s
        Assert.NotNull(reopened.Fishing.TakeOfflineReport());
    }

    [Fact]
    public void Arriving_while_the_game_is_open_does_not_invent_offline_time()
    {
        var (game, clock, _) = Traveller();
        game.Maps.TravelTo("map_02");
        var before = game.Player.GetPlayer().TotalCatches;

        TestSupport.PlayFor(game, clock, 31, stepSeconds: 0.25);
        game.Maps.Update();

        Assert.Equal("map_02", game.Player.GetPlayer().MapId);
        Assert.Null(game.Fishing.TakeOfflineReport());
        Assert.Equal(before, game.Player.GetPlayer().TotalCatches);
    }

    [Fact]
    public void A_rod_too_weak_for_the_destination_cannot_be_equipped_during_the_trip()
    {
        var (game, _, _) = Traveller();
        var starter = game.Profile.GetProfile().Inventory.Single(r => r.Tier == 0).ItemId;
        Assert.True(game.Maps.TravelTo("map_02").Succeeded);

        Assert.Equal(ServiceError.RodNotAllowedOnMap, game.Profile.EquipRod(starter).Error);
        Assert.Equal("rod_01", game.Profile.GetProfile().EquippedRod.RodId);
    }

    [Fact]
    public void A_target_priority_that_skips_positions_is_rejected()
    {
        var result = TestSupport.ConfigWith(GameConfigLoader.ArenaFile, j =>
        {
            var json = Newtonsoft.Json.Linq.JObject.Parse(j);
            json["formation"]!["target_priority"] = new Newtonsoft.Json.Linq.JArray(1, 2, 3);
            return json.ToString();
        });

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("target_priority"));
    }

    [Fact]
    public void Battles_never_hit_an_empty_slot_even_with_an_incomplete_priority_list()
    {
        var config = TestSupport.RealConfig();
        config.Arena.Formation.TargetPriority = new List<int> { 1, 2, 3 }; // only possible if validation is bypassed
        try
        {
            static Fighter F(double hp, double atk) => new Fighter { SpeciesId = "x", Stats = new FishStats { Hp = hp, Attack = atk, Defense = 5, Speed = 100 } };
            var a = new List<Fighter> { F(500, 40), null, null, null, null, null };
            var b = new List<Fighter> { null, null, null, null, F(60, 5), null };

            var outcome = BattleEngine.Resolve(config, a, b, new Rng(3));

            Assert.Equal(0, outcome.Winner);
            Assert.All(outcome.Events.Where(e => e.AttackerSide == 0), e => Assert.Equal(5, e.TargetPosition));
            Assert.True(outcome.Events.Count < 100);
        }
        finally
        {
            config.Arena.Formation.TargetPriority = new List<int> { 1, 2, 3, 4, 5, 6 };
        }
    }

    [Fact]
    public void A_map_where_an_allowed_rod_would_catch_nothing_is_rejected()
    {
        // Lago Sereno with only the rare Aruanã: the Starter Rod (common only) would have nothing to catch.
        var result = TestSupport.ConfigWith(GameConfigLoader.MapsFile, j =>
        {
            var json = Newtonsoft.Json.Linq.JObject.Parse(j);
            var map = json["maps"]![0]!;
            map["available_rarities"] = new Newtonsoft.Json.Linq.JArray("common", "rare");
            var rare = TestSupport.RealConfig().FishCatalog.Species.First(s => s.Rarity == "rare").Id;
            map["fish_pool"] = new Newtonsoft.Json.Linq.JArray(new Newtonsoft.Json.Linq.JObject { ["species_id"] = rare, ["catch_weight"] = 1 });
            return json.ToString();
        });

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("nenhum peixe"));
    }
}
