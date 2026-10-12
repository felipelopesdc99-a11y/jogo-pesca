using System.Collections.Generic;
using System.IO;
using System.Linq;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;
using FishingIdle.GameService.Persistence;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>
/// M24-T30 (A-161), the owner's decision of 12/10/2026: exactly three baits, +5%, +10% and +15% of chance to pull the fish
/// out, 999 attempts per purchase, a little more expensive per attempt than before; old baits in a save become the new ones
/// by value; every level of a rod or a boat costs Moedas and Conchas.
/// </summary>
public sealed class ThreeBaitsTests
{
    private static readonly GameConfig Config = TestSupport.RealConfig();

    private static List<BaitConfig> Legacy => Config.Equipment.BaitMigration.LegacyBaits.OrderBy(b => b.CatchSuccessBonus).ToList();

    [Fact]
    public void There_are_exactly_three_baits_of_5_10_and_15_percent_with_999_attempts()
    {
        var baits = Config.Equipment.Baits.OrderBy(b => b.Tier).ToList();
        Assert.Equal(new[] { "bait_01", "bait_02", "bait_03" }, baits.Select(b => b.Id));
        Assert.Equal(new[] { 0.05, 0.10, 0.15 }, baits.Select(b => b.CatchSuccessBonus));
        Assert.All(baits, b => Assert.Equal(999, b.Charges));
        Assert.All(baits, b => Assert.True(b.CostCoins > 0 && b.CostShells > 0));

        // The names are the ones of before, in the same order of category.
        Assert.Equal(Legacy.Select(b => b.DisplayName), baits.Select(b => b.DisplayName));

        var gear = TestSupport.NewGame(TestSupport.RealConfig()).Game.Gear.GetGear();
        Assert.Equal(3, gear.Baits.Count);
        Assert.All(gear.Baits, b => Assert.Equal(999, b.ChargesPerPurchase));
    }

    /// <summary>What the old baits charged per attempt for a bonus: the line through them (extended past the last one).</summary>
    private static double OldCoinsPerAttempt(double bonus, System.Func<BaitConfig, double> price)
    {
        var points = Legacy.Select(b => (x: b.CatchSuccessBonus, y: price(b) / b.Charges)).ToList();
        for (var i = 1; i < points.Count; i++)
        {
            if (bonus <= points[i].x || i == points.Count - 1)
            {
                var (x0, y0) = points[i - 1];
                var (x1, y1) = points[i];
                return y0 + (y1 - y0) * (bonus - x0) / (x1 - x0);
            }
        }

        return points[0].y;
    }

    [Fact]
    public void Each_attempt_costs_a_little_more_than_before_for_the_same_bonus_in_coins_and_shells()
    {
        foreach (var bait in Config.Equipment.Baits)
        {
            var coins = bait.CostCoins / (double)bait.Charges;
            var shells = bait.CostShells / (double)bait.Charges;
            var oldCoins = OldCoinsPerAttempt(bait.CatchSuccessBonus, b => b.CostCoins);
            var oldShells = OldCoinsPerAttempt(bait.CatchSuccessBonus, b => b.CostShells);

            Assert.InRange(coins / oldCoins, 1.05, 1.25);
            Assert.InRange(shells / oldShells, 1.05, 1.25);
        }

        // A better bait costs more per attempt.
        var perAttempt = Config.Equipment.Baits.OrderBy(b => b.CatchSuccessBonus).Select(b => b.CostCoins / (double)b.Charges).ToList();
        Assert.True(perAttempt.Zip(perAttempt.Skip(1), (a, b) => b > a).All(x => x));
    }

    [Fact]
    public void A_level_1_player_buys_any_bait_with_the_money_and_each_purchase_adds_999_attempts()
    {
        var (game, _, _) = TestSupport.NewGame(TestSupport.RealConfig());
        var save = game.Session.Save;
        Assert.Equal(1, save.FisherLevel);
        save.Coins = 10_000_000;
        save.Shells = 10_000;

        foreach (var bait in Config.Equipment.Baits)
        {
            var coins = save.Coins;
            var shells = save.Shells;
            Assert.True(game.Gear.BuyBait(bait.Id).Succeeded);
            Assert.Equal(coins - bait.CostCoins, save.Coins);
            Assert.Equal(shells - bait.CostShells, save.Shells);
            Assert.Equal(999, save.BaitCharges[bait.Id]);
        }

        Assert.True(game.Gear.BuyBait("bait_02").Succeeded);
        Assert.Equal(1998, game.Gear.GetGear().Baits.Single(b => b.BaitId == "bait_02").ChargesLeft);
        Assert.Equal("bait_01", game.Gear.GetGear().BaitId); // the first one bought went in use

        save.Coins = 0;
        Assert.Equal(ServiceError.NotEnoughCoins, game.Gear.BuyBait("bait_01").Error);
        save.Coins = 10_000_000;
        save.Shells = 0;
        Assert.Equal(ServiceError.NotEnoughShells, game.Gear.BuyBait("bait_01").Error);
    }

    [Fact]
    public void The_bait_adds_to_the_rod_and_the_boat_and_never_passes_95_percent()
    {
        var (game, _, _) = TestSupport.NewGame(TestSupport.RealConfig());
        var save = game.Session.Save;
        save.Coins = 10_000_000;
        save.Shells = 10_000;
        TestSupport.GiveBoat(game, "boat_01");
        Assert.True(game.Gear.BuyBait("bait_03").Succeeded);
        Assert.True(game.Gear.UseBait("bait_03").Succeeded);

        var gear = game.Gear.GetGear();
        Assert.Equal(0.15, gear.BaitBonus, 6);
        Assert.Equal(gear.RodBonus + gear.BoatBonus + 0.15, gear.TotalBonus, 6);
        Assert.Equal(System.Math.Min(0.95, 0.50 + gear.TotalBonus), gear.Chances.Single(c => c.RarityId == "common").Chance, 6);
        Assert.Equal(System.Math.Min(0.95, 0.38 + gear.TotalBonus), gear.Chances.Single(c => c.RarityId == "rare").Chance, 6);

        // With the best boat at its top level, everything stops at the ceiling.
        TestSupport.GiveBoat(game, "boat_10");
        save.BoatLevels["boat_10"] = Config.BoatMaxLevel(Config.Equipment.Boats.Single(b => b.Id == "boat_10"));
        gear = game.Gear.GetGear();
        Assert.Equal(gear.RodBonus + gear.BoatBonus + 0.15, gear.TotalBonus, 6);
        Assert.True(0.50 + gear.TotalBonus > 0.95);
        Assert.All(gear.Chances, c => Assert.True(c.Chance <= Config.Fishing.CatchSuccessMax + 1e-9));
        Assert.Equal(0.95, gear.Chances.Single(c => c.RarityId == "common").Chance, 6);
    }

    [Fact]
    public void Online_fishing_spends_one_of_the_999_attempts_per_attempt()
    {
        var (game, clock, _) = TestSupport.NewGame(TestSupport.RealConfig());
        game.Session.Save.Coins = 1_000_000;
        game.Session.Save.Shells = 1_000;
        Assert.True(game.Gear.BuyBait("bait_02").Succeeded);
        game.Fishing.StartFishing();

        TestSupport.PlayFor(game, clock, Config.Fishing.OnlineCycleSeconds * 25);
        Assert.Equal(999 - 25, game.Gear.GetGear().BaitChargesLeft);
    }

    [Fact]
    public void Offline_fishing_spends_the_999_attempts_and_then_fishes_without_bait()
    {
        var dir = TestSupport.NewTempDirectory();
        var clock = new ManualClock(TestSupport.StartMs);
        var (game, _, _) = TestSupport.NewGame(TestSupport.RealConfig(), dir, clock);
        game.Session.Save.Coins = 1_000_000;
        game.Session.Save.Shells = 1_000;
        Assert.True(game.Gear.BuyBait("bait_03").Succeeded);
        game.Fishing.StartFishing();

        // Closed for 6 hours: 1.080 attempts offline (one every 20 s); the 999 run out on the way.
        clock.AdvanceSeconds(6 * 3600);
        var (back, _, _) = TestSupport.NewGame(TestSupport.RealConfig(), dir, clock, skipTutorial: false);
        var report = back.Fishing.TakeOfflineReport();

        Assert.NotNull(report);
        Assert.Equal(6 * 3600 / (int)Config.Fishing.OfflineCycleSeconds, report.Update.NewCatches.Count + report.Update.Escapes.Count);
        Assert.Equal("Ouro de Maré", report.Update.BaitRanOut);
        Assert.Equal(0, back.Session.Save.BaitCharges["bait_03"]);
        Assert.Null(back.Gear.GetGear().BaitId);
    }

    // ------------------------------------------------------------------ old baits in a save (save version 17)

    [Fact]
    public void Old_baits_go_to_the_new_bait_of_the_same_bonus_or_the_closest_above()
    {
        var today = Config.Equipment.Baits;
        Assert.Equal("bait_01", BaitModelMigration.Target(today, 0.03).Id);
        Assert.Equal("bait_02", BaitModelMigration.Target(today, 0.07).Id);
        Assert.Equal("bait_02", BaitModelMigration.Target(today, 0.10).Id);
        Assert.Equal("bait_03", BaitModelMigration.Target(today, 0.12).Id);
        Assert.Equal("bait_03", BaitModelMigration.Target(today, 0.50).Id);

        // The value in Coins is kept, rounded up: never less, and less than one attempt more.
        foreach (var before in Legacy)
        {
            var target = BaitModelMigration.Target(today, before.CatchSuccessBonus);
            var value = 180 * before.CostCoins / (double)before.Charges;
            var charges = BaitModelMigration.Charges(before, target, 180);
            var perCharge = target.CostCoins / (double)target.Charges;
            Assert.True(charges * perCharge >= value - 1e-6);
            Assert.True((charges - 1) * perCharge < value);
        }

        Assert.Equal(0, BaitModelMigration.Charges(Legacy[0], today[0], 0));
        Assert.Equal(1, BaitModelMigration.Charges(Legacy[0], today[0], 1));
    }

    [Fact]
    public void A_version_16_save_gets_the_new_baits_by_value_and_keeps_its_bait_in_use()
    {
        var (game, clock, dir) = TestSupport.NewGame(TestSupport.RealConfig());
        var save = game.Session.Save;
        save.BaitCharges = new Dictionary<string, int> { ["bait_01"] = 180, ["bait_02"] = 90, ["bait_03"] = 120 };
        save.ActiveBaitId = "bait_03";
        game.Session.Persist();

        // The same player as a version 16 file: the charges are of the old +3/+7/+10% baits.
        var path = Path.Combine(dir, JsonFilePlayerRepository.SaveFileName);
        var json = JObject.Parse(File.ReadAllText(path));
        json["save_version"] = 16;
        json.Remove("bait_model_version");
        File.WriteAllText(path, json.ToString());

        var reopened = TestSupport.NewGame(TestSupport.RealConfig(), dir, clock).Game;
        var moved = reopened.Session.Save;
        Assert.Equal(PlayerSave.CurrentVersion, moved.SaveVersion);
        Assert.Equal(Config.BaitModelVersion, moved.BaitModelVersion);

        var today = Config.Equipment.Baits.ToDictionary(b => b.Id);
        var old = Legacy.ToDictionary(b => b.Id);
        Assert.Equal(BaitModelMigration.Charges(old["bait_01"], today["bait_01"], 180), moved.BaitCharges["bait_01"]);
        Assert.Equal(BaitModelMigration.Charges(old["bait_02"], today["bait_02"], 90) + BaitModelMigration.Charges(old["bait_03"], today["bait_02"], 120),
                     moved.BaitCharges["bait_02"]);
        Assert.False(moved.BaitCharges.ContainsKey("bait_03"));

        // Terra Viva: 180 × 8,33 Moedas = 1.500 Moedas of value; Ouro de Maré (+10%) became Maré Viva (+10%), in use.
        Assert.InRange(moved.BaitCharges["bait_01"] * today["bait_01"].CostCoins / (double)today["bait_01"].Charges, 1500, 1500 + 24);
        var gear = reopened.Gear.GetGear();
        Assert.Equal("bait_02", gear.BaitId);
        Assert.Equal(0.10, gear.BaitBonus, 6);
        Assert.Contains("\"save_version\": " + PlayerSave.CurrentVersion, File.ReadAllText(path));

        // Converted once: opening again changes nothing.
        var charges = new Dictionary<string, int>(moved.BaitCharges);
        var again = TestSupport.NewGame(TestSupport.RealConfig(), dir, clock).Game;
        Assert.Equal(charges, again.Session.Save.BaitCharges);
    }

    [Fact]
    public void A_new_player_is_on_the_new_baits_and_nothing_is_converted()
    {
        var (game, clock, dir) = TestSupport.NewGame(TestSupport.RealConfig());
        var save = game.Session.Save;
        Assert.Equal(Config.BaitModelVersion, save.BaitModelVersion);
        save.Coins = 1_000_000;
        save.Shells = 1_000;
        Assert.True(game.Gear.BuyBait("bait_03").Succeeded);

        var reopened = TestSupport.NewGame(TestSupport.RealConfig(), dir, clock).Game;
        Assert.Equal(999, reopened.Session.Save.BaitCharges["bait_03"]);
        Assert.Equal("bait_03", reopened.Gear.GetGear().BaitId);
    }

    // ------------------------------------------------------------------ every level costs Moedas and Conchas

    [Fact]
    public void Every_level_of_every_rod_and_boat_costs_coins_and_shells()
    {
        foreach (var rod in Config.Rods.Rods.Where(r => r.HasInternalLevels))
        {
            for (var level = 1; level < Config.RodMaxLevel(rod); level++)
            {
                Assert.True(Config.RodUpgradeCost(rod, level) > 0, rod.Id + " Nv." + level);
                Assert.True(Config.RodUpgradeShellCost(rod, level) > 0, rod.Id + " Nv." + level);
            }
        }

        foreach (var boat in Config.Equipment.Boats.Where(b => b.HasLevels))
        {
            for (var level = 1; level < Config.BoatMaxLevel(boat); level++)
            {
                Assert.True(Config.BoatUpgradeCost(boat, level) > 0, boat.Id + " Nv." + level);
                Assert.True(Config.BoatUpgradeShellCost(boat, level) > 0, boat.Id + " Nv." + level);
            }
        }
    }

    [Theory]
    [InlineData(GameConfigLoader.RodsFile, "rods")]
    [InlineData(GameConfigLoader.EquipmentFile, "boats")]
    public void A_config_with_a_level_that_costs_no_shells_is_refused(string file, string section)
    {
        var result = TestSupport.ConfigWith(file, json =>
        {
            var root = JObject.Parse(json);
            var item = root[section]!.First(x => x["upgrade_cost"] != null && x["upgrade_cost"]!.Type == JTokenType.Object);
            item["upgrade_cost"]!["shells_first"] = 0;
            return root.ToString();
        });

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void Upgrading_a_boat_without_shells_is_refused_and_with_them_spends_both()
    {
        var (game, _, _) = TestSupport.NewGame(TestSupport.RealConfig());
        var save = game.Session.Save;
        TestSupport.GiveBoat(game, "boat_01");
        save.Coins = 1_000_000;
        save.Shells = 0;
        Assert.Equal(ServiceError.NotEnoughShells, game.Gear.UpgradeBoat("boat_01").Error);

        save.Shells = 100;
        var boat = Config.Equipment.Boats.Single(b => b.Id == "boat_01");
        Assert.True(game.Gear.UpgradeBoat("boat_01").Succeeded);
        Assert.Equal(1_000_000 - Config.BoatUpgradeCost(boat, 1), save.Coins);
        Assert.Equal(100 - Config.BoatUpgradeShellCost(boat, 1), save.Shells);
    }
}
