using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>Catch Success, boats and baits (docs/SISTEMA_SUCESSO_PESCA.md) on the real config.</summary>
public sealed class CatchSuccessTests
{
    private static readonly GameConfig Config = TestSupport.RealConfig();

    private static MapConfig Map(string id) { Config.TryGetMap(id, out var map); return map; }
    private static RodConfig Rod(string id) { Config.TryGetRod(id, out var rod); return rod; }

    [Fact]
    public void Base_chance_comes_from_the_rarity_and_stays_between_5_and_95_percent()
    {
        Assert.Equal(0.50, CatchRules.SuccessChance(Config, "common", 0), 6);
        Assert.Equal(0.38, CatchRules.SuccessChance(Config, "rare", 0), 6);
        Assert.Equal(0.70, CatchRules.SuccessChance(Config, "common", 0.20), 6);
        Assert.Equal(0.95, CatchRules.SuccessChance(Config, "common", 0.80), 6);
        Assert.Equal(0.05, CatchRules.SuccessChance(Config, "rare", -1.0), 6);
    }

    [Fact]
    public void The_starter_gear_pulls_out_about_half_of_the_common_fish()
    {
        var result = CatchSimulator.Run(Config, Map("map_01"), Config.StarterRod, 1, Config.StarterBoat, null, 20_000, 99);

        Assert.Equal(20_000, result.Attempts);
        Assert.InRange(result.SuccessRate, 0.48, 0.52);
        Assert.All(result.ByRarity, r => Assert.Equal("common", r.RarityId));
    }

    [Fact]
    public void Rod_boat_and_bait_add_up_and_rare_fish_escape_more()
    {
        Config.TryGetBoat("boat_02", out var boat);
        Config.TryGetBait("bait_02", out var bait);
        var rod = Rod("rod_01");
        var result = CatchSimulator.Run(Config, Map("map_02"), rod, 10, boat, bait, 40_000, 7);

        var expectedCommon = 0.50 + Config.RodBonusesAt(rod, 10).CatchSuccess + boat.CatchSuccessBonus + bait.CatchSuccessBonus;
        var common = result.ByRarity.Single(r => r.RarityId == "common");
        var rare = result.ByRarity.Single(r => r.RarityId == "rare");
        Assert.Equal(expectedCommon, common.Chance, 6);
        Assert.Equal(expectedCommon - 0.12, rare.Chance, 6);
        Assert.InRange(common.Caught / (double)common.Bites, common.Chance - 0.02, common.Chance + 0.02);
        Assert.InRange(rare.Caught / (double)rare.Bites, rare.Chance - 0.05, rare.Chance + 0.05);
        Assert.True(result.BaitCoinsPerHour > 0);
    }

    [Fact]
    public void An_escaped_fish_gives_nothing()
    {
        var (game, clock, _) = TestSupport.NewGame(TestSupport.RealConfig());
        game.Fishing.StartFishing();
        var escapes = 0;
        var catches = 0;
        long xp = 0, shells = 0;
        for (var i = 0; i < 400; i++)
        {
            clock.AdvanceSeconds(30);
            var update = game.Fishing.Sync();
            escapes += update.Escapes.Count;
            catches += update.NewCatches.Count;
            xp += update.XpGained;
            shells += update.ShellsGained;
            Assert.All(update.Escapes, e => Assert.InRange(e.Chance, 0.05, 0.95));
        }

        var save = game.Session.Save;
        Assert.Equal(400, escapes + catches);
        Assert.InRange(escapes, 150, 250);
        Assert.Equal(catches, game.Fishing.GetFishingBox().Count);
        Assert.Equal(catches, save.Stats.TotalCatches);
        Assert.Equal(escapes, save.Stats.Escapes);
        Assert.Equal(save.FisherXpTotal, xp);
        Assert.Equal(save.SpeciesRecords.Values.Sum(r => r.TimesCaught), catches);
    }

    [Fact]
    public void Offline_fishing_uses_the_same_chance_and_spends_the_bait()
    {
        var dir = TestSupport.NewTempDirectory();
        var clock = new ManualClock(TestSupport.StartMs);
        var (game, _, _) = TestSupport.NewGame(TestSupport.RealConfig(), dir, clock);
        game.Session.Save.Coins = 10_000;
        game.Session.Save.Shells = 1_000; // every item asks for Conchas (A-099)
        Assert.True(game.Gear.BuyBait("bait_01").Succeeded);
        game.Fishing.StartFishing();

        // Closed for 3 hours: 180 offline attempts; the bait's charges run out on the way.
        clock.AdvanceSeconds(3 * 3600);
        var (back, _, _) = TestSupport.NewGame(TestSupport.RealConfig(), dir, clock, skipTutorial: false);
        var report = back.Fishing.TakeOfflineReport();

        Assert.NotNull(report);
        var update = report.Update;
        Assert.Equal(180, update.NewCatches.Count + update.Escapes.Count);
        Assert.InRange(update.Escapes.Count, 50, 110);
        Assert.Equal("Terra Viva", update.BaitRanOut);
        Assert.Equal(0, back.Session.Save.BaitCharges["bait_01"]);
        Assert.Null(back.Gear.GetGear().BaitId);
    }

    [Fact]
    public void A_bait_spends_one_charge_per_attempt_caught_or_not()
    {
        var (game, clock, _) = TestSupport.NewGame(TestSupport.RealConfig());
        game.Session.Save.Coins = 10_000;
        game.Session.Save.Shells = 1_000; // every item asks for Conchas (A-099)
        var bought = game.Gear.BuyBait("bait_01");
        Assert.True(bought.Succeeded);
        Assert.Equal("bait_01", bought.Value.BaitId);
        var pack = bought.Value.Baits.Single(b => b.BaitId == "bait_01").ChargesPerPurchase;
        Assert.Equal(pack, bought.Value.BaitChargesLeft);
        Assert.Equal(0.5 + bought.Value.BaitBonus, bought.Value.Chances.Single(c => c.RarityId == "common").Chance, 6);
        Assert.True(bought.Value.BaitBonus > 0);

        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, 30 * 10);
        Assert.Equal(pack - 10, game.Gear.GetGear().BaitChargesLeft);

        // A second batch adds up, and the bait can be put away and back without losing charges.
        Assert.True(game.Gear.BuyBait("bait_01").Succeeded);
        Assert.Equal(2 * pack - 10, game.Gear.GetGear().BaitChargesLeft);
        Assert.True(game.Gear.UseBait(null).Succeeded);
        TestSupport.PlayFor(game, clock, 30 * 5);
        Assert.Equal(2 * pack - 10, game.Session.Save.BaitCharges["bait_01"]);
        Assert.Equal(ServiceError.BaitNoCharges, game.Gear.UseBait("bait_02").Error);
        Assert.Equal(ServiceError.BaitLocked, game.Gear.BuyBait("bait_02").Error);
    }

    [Fact]
    public void Boats_cost_coins_and_shells_and_raise_the_chance()
    {
        var (game, _, _) = TestSupport.NewGame(TestSupport.RealConfig());
        var save = game.Session.Save;
        var gear = game.Gear.GetGear();
        Assert.Equal("boat_00", gear.BoatId);
        Assert.True(gear.Boats.First().Owned);
        Assert.Equal(ServiceError.BoatLocked, game.Gear.BuyBoat("boat_01").Error);

        var boat1 = gear.Boats.Single(b => b.BoatId == "boat_01");
        save.FisherLevel = 30;
        save.Coins = boat1.CostCoins;
        save.Shells = boat1.CostShells;
        var bought = game.Gear.BuyBoat("boat_01");
        Assert.True(bought.Succeeded);
        Assert.Equal(0, save.Coins);
        Assert.Equal(0, save.Shells);
        Assert.Equal("boat_01", bought.Value.BoatId);
        Assert.Equal(0.5 + bought.Value.BoatBonus, bought.Value.Chances.Single(c => c.RarityId == "common").Chance, 6);
        Assert.True(bought.Value.BoatBonus > 0);
        Assert.Equal(ServiceError.BoatAlreadyOwned, game.Gear.BuyBoat("boat_01").Error);

        save.Coins = 1_000_000;
        var boat3Shells = gear.Boats.Single(b => b.BoatId == "boat_03").CostShells;
        save.Shells = boat3Shells - 1;
        Assert.Equal(ServiceError.NotEnoughShells, game.Gear.BuyBoat("boat_03").Error);
        save.Shells = boat3Shells;
        Assert.True(game.Gear.BuyBoat("boat_03").Succeeded);
        Assert.Equal(0, save.Shells);
        Assert.Equal(1_000_000 - gear.Boats.Single(b => b.BoatId == "boat_03").CostCoins, save.Coins);

        // Going back to an owned boat is free; one never bought is refused.
        Assert.True(game.Gear.UseBoat("boat_00").Succeeded);
        Assert.Equal(ServiceError.BoatNotOwned, game.Gear.UseBoat("boat_02").Error);
        Assert.Equal(0.50, game.Gear.GetGear().Chances.Single(c => c.RarityId == "common").Chance, 6);
    }
}
