using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Persistence;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>Problems found in the integrity review of the rules, config and save (12/10/2026), each pinned by a test.</summary>
public sealed class IntegrityReviewTests
{
    [Fact]
    public void A_pc_clock_set_back_freezes_the_game_time_and_the_service_says_so_after_5_minutes()
    {
        var (game, clock, _) = TestSupport.NewGame();
        clock.AdvanceSeconds(3600);
        _ = game.Session.Clock.UtcNowMs; // the rules saw this time
        Assert.Equal(0, game.Session.ClockFrozenSeconds);
        Assert.False(game.Session.ClockFrozen);

        clock.AdvanceSeconds(-120);
        Assert.Equal(120, game.Session.ClockFrozenSeconds);
        Assert.False(game.Session.ClockFrozen);

        clock.AdvanceSeconds(-600);
        Assert.Equal(720, game.Session.ClockFrozenSeconds);
        Assert.True(game.Session.ClockFrozen);

        // TD-030 still holds: the game time does not go back, and the warning goes away once the PC clock catches up.
        clock.AdvanceSeconds(720);
        Assert.Equal(0, game.Session.ClockFrozenSeconds);
        Assert.False(game.Session.ClockFrozen);
    }

    [Fact]
    public void The_service_says_when_the_clothing_fragments_reach_the_next_piece()
    {
        var config = TestSupport.ConfigWith(GameConfigLoader.EquipmentFile, json =>
        {
            var root = JObject.Parse(json);
            root["clothing"]!["fragment_drop_chance"] = 1.0;
            return root.ToString();
        }, certainCatch: true).Config;
        var (game, clock, _) = TestSupport.NewGame(config);
        var cost = game.Clothing.GetClothing().NextCost;
        Assert.True(cost > 1);
        game.Session.Save.Clothing.Fragments = cost - 1;
        Assert.False(game.Clothing.GetClothing().CanAssemble);

        game.Fishing.StartFishing();
        clock.AdvanceSeconds(config.Fishing.OnlineCycleSeconds);
        var reached = game.Fishing.Sync();
        clock.AdvanceSeconds(config.Fishing.OnlineCycleSeconds);
        var after = game.Fishing.Sync();

        Assert.Equal(1, reached.ClothingFragments);
        Assert.True(reached.ClothingReady);
        Assert.Equal(1, after.ClothingFragments);
        Assert.False(after.ClothingReady);
        Assert.True(game.Clothing.GetClothing().CanAssemble);
    }

    [Fact]
    public void Additions_stop_at_the_top_instead_of_turning_negative()
    {
        Assert.Equal(long.MaxValue, SafeMath.Add(long.MaxValue - 1, 5));
        Assert.Equal(7, SafeMath.Add(2, 5));
        Assert.Equal(int.MaxValue, SafeMath.AddCount(int.MaxValue - 1, 999));

        // A bait bought on top of almost int.MaxValue charges keeps the save valid (it used to wrap to negative).
        var (game, _, dir) = TestSupport.NewGame();
        var bait = game.Session.Config.Equipment.Baits.First();
        game.Session.Save.Coins = bait.CostCoins;
        game.Session.Save.Shells = bait.CostShells;
        game.Session.Save.BaitCharges[bait.Id] = int.MaxValue - 1;
        Assert.True(game.Gear.BuyBait(bait.Id).Succeeded);
        Assert.Equal(int.MaxValue, game.Session.Save.BaitCharges[bait.Id]);
        Assert.Empty(SaveValidator.Validate(game.Session.Save));
        var reopened = TestSupport.NewGame(saveDir: dir).Game;
        Assert.Equal(SaveLoadStatus.Loaded, reopened.Session.LoadStatus);
    }
}
