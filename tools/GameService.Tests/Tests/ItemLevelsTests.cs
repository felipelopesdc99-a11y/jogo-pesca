using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;
using FishingIdle.GameService.Persistence;
using FishingIdle.GameService.Shop;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>Rods, boats and fish up to Nv.100 on a curve, and the move of old saves (M24-T13, A-156, TD-041).</summary>
public class ItemLevelsTests
{
    private static GameConfig Config => TestSupport.RealConfig();

    // ------------------------------------------------------------------ the curve

    [Fact]
    public void Prices_round_to_whole_numbers_and_three_digits()
    {
        Assert.Equal(1, LevelCurve.RoundPrice(0.3));
        Assert.Equal(999, LevelCurve.RoundPrice(999.4));
        Assert.Equal(12_300, LevelCurve.RoundPrice(12_345));
        Assert.Equal(1_240_000, LevelCurve.RoundPrice(1_235_000));
        Assert.Equal(LevelCurve.Unaffordable, LevelCurve.RoundPrice(1e19));
        Assert.Equal(0, LevelCurve.Cost(0, 2.0, 50));
        Assert.Equal(500, LevelCurve.Cost(500, 1.14, 1));
        Assert.Equal(570, LevelCurve.Cost(500, 1.14, 2));
    }

    [Fact]
    public void The_curve_starts_at_level_1_ends_at_the_max_and_each_step_is_a_little_bigger()
    {
        Assert.Equal(0.10, LevelCurve.Value(0.10, 0.40, 1, 100, 1.2), 9);
        Assert.Equal(0.40, LevelCurve.Value(0.10, 0.40, 100, 100, 1.2), 9);
        Assert.Equal(0.40, LevelCurve.Value(0.10, 0.40, 500, 100, 1.2), 9);
        var first = LevelCurve.Value(0.10, 0.40, 2, 100, 1.2) - LevelCurve.Value(0.10, 0.40, 1, 100, 1.2);
        var last = LevelCurve.Value(0.10, 0.40, 100, 100, 1.2) - LevelCurve.Value(0.10, 0.40, 99, 100, 1.2);
        Assert.True(first > 0 && last > first);
    }

    [Fact]
    public void Rods_go_to_level_100_from_today_s_bonus_with_small_steps()
    {
        var config = Config;
        foreach (var rod in config.Rods.Rods.Where(r => r.HasInternalLevels))
        {
            Assert.Equal(100, config.RodMaxLevel(rod));
            var at1 = config.RodBonusesAt(rod, 1);
            Assert.Equal(rod.BonusesAtLevel1.CatchSuccess, at1.CatchSuccess, 9);
            Assert.Equal(rod.BonusesAtMaxLevel.RarityEfficiency, config.RodBonusesAt(rod, 100).RarityEfficiency, 9);

            // Small steps: the first level adds well under 1 percentage point of Catch Success.
            Assert.InRange(config.RodBonusesAt(rod, 2).CatchSuccess - at1.CatchSuccess, 0.0, 0.01);
            for (var level = 1; level < 100; level++)
            {
                Assert.True(config.RodBonusesAt(rod, level + 1).CatchSuccess > config.RodBonusesAt(rod, level).CatchSuccess);
                Assert.True(config.RodUpgradeCost(rod, level + 1) >= config.RodUpgradeCost(rod, level) || level + 1 == 100);
                Assert.True(config.RodUpgradeCost(rod, level) > 0);
                Assert.True(config.RodUpgradeShellCost(rod, level) > 0);
            }

            Assert.Equal(0, config.RodUpgradeCost(rod, 100));
        }
    }

    [Fact]
    public void Even_the_best_gear_never_passes_the_catch_success_ceiling()
    {
        var config = Config;
        config.TryGetRod("rod_05", out var rod);
        config.TryGetBoat("boat_05", out var boat);
        config.TryGetBait("bait_03", out var bait);
        var bonus = config.RodBonusesAt(rod, 100).CatchSuccess + config.BoatBonusAt(boat, 100) + bait.CatchSuccessBonus;

        Assert.True(bonus > 1.0);
        foreach (var tier in config.Progression.Rarity.Tiers)
        {
            Assert.True(CatchRules.SuccessChance(config, tier.Id, bonus) <= config.Fishing.CatchSuccessMax);
        }
    }

    [Fact]
    public void Fish_attributes_grow_on_the_curve_to_three_times_the_old_level_10()
    {
        var config = Config;
        Assert.Equal(100, config.Progression.FishLevel.MaxLevel);
        Assert.Equal(0.0, config.FishStatBonus(1), 9);
        Assert.Equal(1.08, config.FishStatBonus(100), 9);
        Assert.InRange(config.FishStatBonus(2), 0.0, 0.01);

        config.TryGetSpecies("tilapia", out var tilapia);
        var level1 = FishRules.Stats(config, tilapia, 420, 1);
        var level100 = FishRules.Stats(config, tilapia, 420, 100);
        Assert.Equal(level1.Attack * 2.08, level100.Attack, 6);

        // The XP to level up starts small and grows.
        Assert.True(config.FishXpToNextLevel(1) <= 10);
        Assert.True(config.FishXpToNextLevel(99) > config.FishXpToNextLevel(50));
        Assert.Equal(0, config.FishXpToNextLevel(100));
    }

    // ------------------------------------------------------------------ boats

    [Fact]
    public void An_owned_boat_levels_up_for_coins_and_shells_and_fishes_with_the_new_bonus()
    {
        var (game, _, _) = TestSupport.NewGame();
        var save = game.Session.Save;
        var config = game.Session.Config;
        config.TryGetBoat("boat_01", out var boat);
        save.Coins = 10_000_000;
        save.Shells = 10_000;

        Assert.Equal(ServiceError.BoatNotOwned, game.Gear.UpgradeBoat(boat.Id).Error);
        Assert.Equal(ServiceError.BoatHasNoLevels, game.Gear.UpgradeBoat(config.StarterBoat.Id).Error);
        Assert.True(game.Gear.BuyBoat(boat.Id).Succeeded);

        var offer = game.Gear.GetGear().Boats.Single(b => b.BoatId == boat.Id);
        Assert.Equal(1, offer.Level);
        Assert.Equal(100, offer.MaxLevel);
        Assert.Equal(boat.CatchSuccessBonus, offer.Bonus, 9);
        Assert.Equal(config.BoatUpgradeCost(boat, 1), offer.NextUpgradeCoins);
        Assert.Equal(ServiceError.None, offer.UpgradeBlocker);

        var coins = save.Coins;
        var shells = save.Shells;
        var up = game.Gear.UpgradeBoat(boat.Id);

        Assert.True(up.Succeeded, up.ErrorMessage);
        Assert.Equal(coins - offer.NextUpgradeCoins, save.Coins);
        Assert.Equal(shells - offer.NextUpgradeShells, save.Shells);
        Assert.Equal(2, save.BoatLevels[boat.Id]);
        Assert.Equal(config.BoatBonusAt(boat, 2), up.Value.BoatBonus, 9);
        Assert.True(up.Value.BoatBonus > boat.CatchSuccessBonus);
        Assert.Equal(config.BoatBonusAt(boat, 2), GearRules.Bonus(config, save), 9);
    }

    [Fact]
    public void Boat_upgrades_need_shells_and_stop_at_the_max_level()
    {
        var (game, _, _) = TestSupport.NewGame();
        var save = game.Session.Save;
        var config = game.Session.Config;
        config.TryGetBoat("boat_01", out var boat);
        save.Coins = 10_000_000;
        save.Shells = 100;
        game.Gear.BuyBoat(boat.Id);

        save.Shells = 0;
        Assert.Equal(ServiceError.NotEnoughShells, game.Gear.UpgradeBoat(boat.Id).Error);
        save.Coins = 0;
        save.Shells = 100;
        Assert.Equal(ServiceError.NotEnoughCoins, game.Gear.UpgradeBoat(boat.Id).Error);

        save.BoatLevels[boat.Id] = config.BoatMaxLevel(boat);
        save.Coins = long.MaxValue / 2;
        Assert.Equal(ServiceError.BoatAtMaxLevel, game.Gear.UpgradeBoat(boat.Id).Error);
        Assert.Equal(config.BoatBonusAt(boat, 100), game.Gear.GetGear().BoatBonus, 9);
        Assert.Equal(boat.CatchSuccessBonusAtMaxLevel, config.BoatBonusAt(boat, 100), 9);
    }

    [Fact]
    public void Boat_levels_survive_closing_the_game()
    {
        var (game, clock, dir) = TestSupport.NewGame();
        game.Session.Save.Coins = 10_000_000;
        game.Session.Save.Shells = 1_000;
        game.Gear.BuyBoat("boat_01");
        game.Gear.UpgradeBoat("boat_01");
        game.Gear.UpgradeBoat("boat_01");

        var reopened = TestSupport.NewGame(saveDir: dir, clock: clock).Game;
        Assert.Equal(3, reopened.Gear.GetGear().Boats.Single(b => b.BoatId == "boat_01").Level);
    }

    // ------------------------------------------------------------------ the Shop shows the rod's level

    [Fact]
    public void The_shop_shows_the_level_of_your_rod_and_its_next_price()
    {
        var (game, _, _) = TestSupport.NewGame();
        var save = game.Session.Save;
        var config = game.Session.Config;
        config.TryGetRod("rod_01", out var rod);
        save.Coins = 10_000_000;
        save.Shells = 1_000;

        var before = game.Shop.GetShop().Rods.Single(r => r.RodId == rod.Id);
        Assert.Equal(0, before.OwnedLevel);
        Assert.Equal(ServiceError.ItemNotFound, before.UpgradeBlocker);

        game.Shop.BuyRod(rod.Id);
        var offer = game.Shop.GetShop().Rods.Single(r => r.RodId == rod.Id);
        Assert.Equal(1, offer.OwnedLevel);
        Assert.Equal(config.RodUpgradeCost(rod, 1), offer.NextUpgradeCost);
        Assert.Equal(config.RodUpgradeShellCost(rod, 1), offer.NextUpgradeShells);
        Assert.Equal(ServiceError.None, offer.UpgradeBlocker);

        Assert.True(game.Profile.UpgradeRod(offer.OwnedItemId).Succeeded);
        var after = game.Shop.GetShop().Rods.Single(r => r.RodId == rod.Id);
        Assert.Equal(2, after.OwnedLevel);
        Assert.Equal(config.RodBonusesAt(rod, 2).CatchSuccess, after.CatchBonusNow, 9);
    }

    // ------------------------------------------------------------------ save version 15

    [Fact]
    public void A_version_14_save_moves_rods_and_fish_up_without_losing_strength()
    {
        var (game, clock, dir) = TestSupport.NewGame();
        var save = game.Session.Save;
        var config = game.Session.Config;
        save.Coins = 10_000_000;
        save.Shells = 1_000;
        game.Shop.BuyRod("rod_01");
        game.Gear.BuyBoat("boat_01");
        save.EquippedRodItem().Level = 10;
        game.Session.DevToolsEnabled = true;
        game.DevTools.GiveFish("lambari", null, 1);
        var fishId = game.Aquarium.KeepCatches(game.Fishing.GetFishingBox().Select(c => c.CatchId).ToList()).Value.Kept[0].FishId;
        var fish = save.Aquarium.Single(f => f.Id == fishId);
        fish.Level = 10;
        fish.Xp = 0;
        game.Session.Persist();

        // The same player as a version 14 file: levels on the old Nv.1–Nv.10 curve, no boat levels.
        var path = Path.Combine(dir, JsonFilePlayerRepository.SaveFileName);
        var json = JObject.Parse(File.ReadAllText(path));
        json["save_version"] = 14;
        json.Remove("boat_levels");
        json.Remove("rod_level_curve_version");
        json.Remove("fish_level_curve_version");
        File.WriteAllText(path, json.ToString());

        var reopened = TestSupport.NewGame(saveDir: dir, clock: clock).Game;
        var moved = reopened.Session.Save;
        config.TryGetRod("rod_01", out var rod);

        Assert.Equal(PlayerSave.CurrentVersion, moved.SaveVersion);
        Assert.Equal(config.Rods.UpgradeRules.LevelCurveVersion, moved.RodLevelCurveVersion);
        Assert.Equal(config.Progression.FishLevel.LevelCurveVersion, moved.FishLevelCurveVersion);

        // The old rod_01 at Nv.10 (rods.json before M24-T13): rarity +22%, size +21%, Conchas +52%, success +8%.
        var rodLevel = moved.EquippedRodItem().Level;
        Assert.Equal(rod.LegacyLevelsV1[9], rodLevel);
        var bonuses = config.RodBonusesAt(rod, rodLevel);
        Assert.True(bonuses.RarityEfficiency >= 0.22 - 1e-9);
        Assert.True(bonuses.SizeQuality >= 0.21 - 1e-9);
        Assert.True(bonuses.ShellYield >= 0.52 - 1e-9);
        Assert.True(bonuses.CatchSuccess >= 0.08 - 1e-9);
        Assert.True(config.RodBonusesAt(rod, rodLevel - 1).RarityEfficiency < 0.22
                    || config.RodBonusesAt(rod, rodLevel - 1).SizeQuality < 0.21
                    || config.RodBonusesAt(rod, rodLevel - 1).ShellYield < 0.52
                    || config.RodBonusesAt(rod, rodLevel - 1).CatchSuccess < 0.08);

        // The old fish at Nv.10 had +36% on every attribute.
        var movedFish = moved.Aquarium.Single(f => f.Id == fishId);
        Assert.Equal(config.Progression.FishLevel.LegacyLevelsV1[9], movedFish.Level);
        Assert.True(config.FishStatBonus(movedFish.Level) >= 0.36 - 1e-9);
        Assert.True(config.FishStatBonus(movedFish.Level - 1) < 0.36);

        // Boats start at Nv.1.
        Assert.Empty(moved.BoatLevels);
        Assert.Equal(1, reopened.Gear.GetGear().Boats.Single(b => b.BoatId == "boat_01").Level);

        // Opening again does not move anything twice.
        var again = TestSupport.NewGame(saveDir: dir, clock: clock).Game.Session.Save;
        Assert.Equal(rodLevel, again.EquippedRodItem().Level);
        Assert.Equal(movedFish.Level, again.Aquarium.Single(f => f.Id == fishId).Level);
        Assert.Contains("\"save_version\": " + PlayerSave.CurrentVersion, File.ReadAllText(path));
    }

    [Fact]
    public void A_fish_keeps_the_xp_it_had_inside_its_old_level()
    {
        var config = Config;
        var save = new PlayerSave { FishLevelCurveVersion = 1 };
        save.Aquarium.Add(new FishInstance { Id = 1, SpeciesId = "lambari", SizeMm = 100, Level = 3, Xp = 150 });

        LevelCurveMigration.MoveToCurrentCurves(config, save);

        var fish = save.Aquarium[0];
        var legacy = config.Progression.FishLevel.LegacyLevelsV1[2];
        Assert.True(fish.Level >= legacy);
        Assert.Equal(150, MarketXpFrom(config, legacy, fish.Level, fish.Xp));
    }

    [Fact]
    public void A_save_on_the_current_curve_is_not_moved()
    {
        var config = Config;
        var save = new PlayerSave
        {
            RodLevelCurveVersion = config.Rods.UpgradeRules.LevelCurveVersion,
            FishLevelCurveVersion = config.Progression.FishLevel.LevelCurveVersion,
        };
        save.Inventory.Add(new InventoryItem { Id = 1, Kind = InventoryItem.KindRod, RodId = "rod_01", Level = 5 });
        save.Aquarium.Add(new FishInstance { Id = 1, SpeciesId = "lambari", SizeMm = 100, Level = 5 });

        var change = LevelCurveMigration.MoveToCurrentCurves(config, save);

        Assert.Equal(0, change.RodsMoved + change.FishMoved);
        Assert.Equal(5, save.Inventory[0].Level);
        Assert.Equal(5, save.Aquarium[0].Level);
    }

    [Fact]
    public void A_save_with_a_broken_boat_level_is_not_trusted()
    {
        var save = new PlayerSave { PlayerId = "p", CurrentMapId = "map_01", NextItemId = 1 };
        save.BoatLevels["boat_01"] = 0;
        Assert.Contains("boat levels malformed", SaveValidator.Validate(save));
    }

    /// <summary>XP spent from <paramref name="fromLevel"/> (start) to the fish's level and XP now.</summary>
    private static long MarketXpFrom(GameConfig config, int fromLevel, int level, long xp)
    {
        long total = xp;
        for (var l = fromLevel; l < level; l++)
        {
            total += config.FishXpToNextLevel(l);
        }

        return total;
    }
}
