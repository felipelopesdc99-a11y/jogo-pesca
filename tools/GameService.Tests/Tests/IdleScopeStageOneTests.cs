using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;
using FishingIdle.GameService.Persistence;
using FishingIdle.Texts;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>
/// First stage of the new idle scope (M24, A-152/A-153): items with no minimum level (M24-T09), the Fisher up to
/// Nv.1000 with a new XP curve (M24-T03), saves moved to that curve without ever losing a level (TD-038), and big
/// numbers.
/// </summary>
public sealed class IdleScopeStageOneTests
{
    // ------------------------------------------------------------------ M24-T09: items with no minimum level

    [Fact]
    public void A_level_1_player_with_the_money_can_buy_the_best_bait_and_rods_come_from_fragments()
    {
        // Boats (M24-T16, A-157) and rods (M24-T32, A-163) are not sold: they come from fragments.
        var (game, _, _) = TestSupport.NewGame(TestSupport.RealConfig());
        var save = game.Session.Save;
        Assert.Equal(1, save.FisherLevel);
        save.Coins = 1_000_000_000;
        save.Shells = 1_000_000;

        Assert.All(game.Shop.GetShop().Rods.Where(r => !r.Owned), r => Assert.Equal(ServiceError.RodNotForSale, r.BuyBlocker));
        Assert.All(game.Gear.GetGear().Baits, b => Assert.Equal(ServiceError.None, b.BuyBlocker));

        TestSupport.GiveRod(game, "rod_05");
        Assert.True(game.Gear.BuyBait("bait_03").Succeeded);
        Assert.Equal(1, game.Player.GetPlayer().FisherLevel);
        Assert.Equal(1_000_000_000 - game.Session.Config.Equipment.Baits.Single(b => b.Id == "bait_03").CostCoins, save.Coins);
    }

    [Fact]
    public void Without_the_money_the_only_blocker_is_the_money()
    {
        var (game, _, _) = TestSupport.NewGame(TestSupport.RealConfig());
        var save = game.Session.Save;
        save.Coins = 0;
        save.Shells = 1_000_000;
        Assert.Equal(ServiceError.RodNotForSale, game.Shop.BuyRod("rod_05").Error);
        Assert.Equal(ServiceError.NotEnoughCoins, game.Gear.BuyBait("bait_03").Error);

        save.Coins = 1_000_000_000;
        save.Shells = 0;
        Assert.Equal(ServiceError.RodNotForSale, game.Shop.BuyRod("rod_05").Error);
        Assert.Equal(ServiceError.NotEnoughShells, game.Gear.BuyBait("bait_03").Error);
    }

    [Fact]
    public void Maps_still_ask_for_the_fisher_level()
    {
        var (game, _, _) = TestSupport.NewGame(TestSupport.RealConfig());
        game.Session.Save.Coins = 1_000_000_000;
        game.Session.Save.Shells = 1_000_000;
        TestSupport.GiveRod(game, "rod_05");

        Assert.Equal(ServiceError.MapLocked, game.Maps.TravelTo("map_02").Error);
        Assert.Equal(ServiceError.MapLocked, game.Maps.TravelTo("map_10").Error);
    }

    // ------------------------------------------------------------------ M24-T03: Fisher up to Nv.1000

    [Fact]
    public void The_fisher_goes_up_to_level_1000_on_a_growing_curve()
    {
        var config = TestSupport.RealConfig();
        Assert.Equal(1000, config.Progression.Fisher.MaxLevel);
        Assert.Equal(999, config.Progression.Fisher.XpTable.Count);

        long total = 0;
        for (var level = 1; level < 1000; level++)
        {
            var xp = config.FisherXpToNextLevel(level);
            Assert.True(xp > 0, "level " + level);
            if (level > 1)
            {
                Assert.True(xp >= config.FisherXpToNextLevel(level - 1), "level " + level + " asks less than the one before");
            }

            total += xp;
        }

        Assert.Equal(0, config.FisherXpToNextLevel(1000));
        Assert.True(total > 100_000_000L, "the whole curve asks hundreds of millions of XP");
    }

    [Fact]
    public void The_first_levels_come_several_per_session()
    {
        // A-153: Nv.10 in about 1 hour of online fishing; 30 minutes since M24-T22 (A-159) and 20 minutes since
        // M24-T27 (A-160), with the same XP and 1,5× the fish per hour (FishingServiceTests checks the real pace).
        var config = TestSupport.RealConfig();
        var toLevel10 = Enumerable.Range(1, 9).Sum(l => config.FisherXpToNextLevel(l));
        Assert.InRange(toLevel10, 500L, 1500L);
    }

    [Fact]
    public void Reaching_level_1000_stops_the_xp_inside_the_level()
    {
        var (game, clock, _) = TestSupport.NewGame();
        var save = game.Session.Save;
        save.FisherLevel = 999;
        save.FisherXp = game.Session.Config.FisherXpToNextLevel(999) - 1;
        game.Fishing.StartFishing();
        clock.AdvanceSeconds(120);

        var update = game.Fishing.Sync();
        Assert.Contains(1000, update.LevelsReached);
        Assert.Equal(1000, save.FisherLevel);
        Assert.Equal(0, save.FisherXp);
        Assert.Equal(0, game.Player.GetPlayer().FisherXpToNext);
    }

    [Fact]
    public void Level_ups_pay_no_dollars_by_themselves_the_pass_has_them()
    {
        // M24-T17 (A-158): the 10 Dólares every 10 levels up to Nv.100 (A-111) are in the pass's free track.
        var (game, clock, _) = TestSupport.NewGame();
        var save = game.Session.Save;
        var config = game.Session.Config;
        Assert.Equal(100, config.Pass.Milestones.Where(m => m.Level <= 100).Sum(m => m.Free.Dollars));

        save.FisherLevel = 99;
        save.FisherXp = config.FisherXpToNextLevel(99) - 1;
        game.Fishing.StartFishing();
        clock.AdvanceSeconds(60);
        Assert.Contains(100, game.Fishing.Sync().LevelsReached);
        Assert.Equal(0, save.Dollars);
    }

    // ------------------------------------------------------------------ TD-038: saves move to the new curve

    private static PlayerSave OldSave(int level, long xpInLevel, long total, long dollars)
    {
        return new PlayerSave { FisherLevel = level, FisherXp = xpInLevel, FisherXpTotal = total, Dollars = dollars, FisherXpCurveVersion = 1 };
    }

    [Fact]
    public void A_save_from_the_old_curve_goes_up_to_the_level_its_total_xp_reaches()
    {
        var config = TestSupport.RealConfig();
        // An old Nv.50 player (50 Dólares already earned) with 2 million XP earned in all.
        var save = OldSave(50, 1234, 2_000_000, 50);
        var (expected, rest) = FisherLevelRules.LevelForTotalXp(config, 2_000_000);
        Assert.True(expected > 100);

        var change = FisherLevelRules.MoveToCurrentCurve(config, save);

        Assert.NotNull(change);
        Assert.Equal(50, change.LevelBefore);
        Assert.Equal(expected, save.FisherLevel);
        Assert.Equal(rest, save.FisherXp);
        Assert.Equal(2_000_000, save.FisherXpTotal);
        Assert.Equal(50, save.Dollars); // the milestones crossed pay in the Passe do Pescador (A-158), not here
        Assert.Equal(config.Progression.Fisher.XpCurveVersion, save.FisherXpCurveVersion);
    }

    [Fact]
    public void A_save_from_the_old_curve_never_goes_down_a_level()
    {
        var config = TestSupport.RealConfig();
        // Set by the test tools: a high level with almost no XP earned.
        var save = OldSave(80, long.MaxValue / 2, 10, 80);

        var change = FisherLevelRules.MoveToCurrentCurve(config, save);

        Assert.Equal(80, save.FisherLevel);
        Assert.Equal(80, change.LevelAfter);
        Assert.Equal(config.FisherXpToNextLevel(80) - 1, save.FisherXp);
        Assert.Equal(80, save.Dollars);
    }

    [Fact]
    public void Every_old_level_lands_at_the_same_level_or_higher()
    {
        var config = TestSupport.RealConfig();
        // Even with no XP recorded (the test tools set the level directly) every old level is kept; the old
        // maximum (Nv.100 with the 11,5 million XP the old curve asked for) lands well above Nv.100.
        for (var level = 1; level <= 100; level++)
        {
            var save = OldSave(level, 0, 0, 0);
            FisherLevelRules.MoveToCurrentCurve(config, save);
            Assert.True(save.FisherLevel >= level);
        }

        var veteran = OldSave(100, 0, 11_530_255, 100);
        FisherLevelRules.MoveToCurrentCurve(config, veteran);
        Assert.True(veteran.FisherLevel > 100);
        Assert.Equal(100, veteran.Dollars);
    }

    [Fact]
    public void More_xp_than_the_whole_curve_is_level_1000()
    {
        var config = TestSupport.RealConfig();
        var save = OldSave(100, 0, long.MaxValue / 4, 100);
        FisherLevelRules.MoveToCurrentCurve(config, save);
        Assert.Equal(1000, save.FisherLevel);
        Assert.Equal(0, save.FisherXp);
    }

    [Fact]
    public void A_save_on_the_current_curve_is_left_alone()
    {
        var config = TestSupport.RealConfig();
        var save = OldSave(7, 3, 5_000_000, 0);
        save.FisherXpCurveVersion = config.Progression.Fisher.XpCurveVersion;

        Assert.Null(FisherLevelRules.MoveToCurrentCurve(config, save));
        Assert.Equal(7, save.FisherLevel);
        Assert.Equal(3, save.FisherXp);
    }

    [Fact]
    public void A_version_11_save_on_disk_is_moved_to_the_new_curve_once()
    {
        var (game, clock, dir) = TestSupport.NewGame();
        Assert.Equal(game.Session.Config.Progression.Fisher.XpCurveVersion, game.Session.Save.FisherXpCurveVersion);
        var save = game.Session.Save;
        save.FisherLevel = 40;
        save.FisherXp = 10;
        save.FisherXpTotal = 2_000_000;
        save.Dollars = 40;
        game.Session.Persist();

        // The same player as a version 11 file, from before the curve was recorded.
        var path = Path.Combine(dir, JsonFilePlayerRepository.SaveFileName);
        var json = JObject.Parse(File.ReadAllText(path));
        json["save_version"] = 11;
        json.Remove("fisher_xp_curve_version");
        File.WriteAllText(path, json.ToString());

        var reopened = TestSupport.NewGame(saveDir: dir, clock: clock).Game;
        var moved = reopened.Session.Save;
        var expected = FisherLevelRules.LevelForTotalXp(reopened.Session.Config, 2_000_000).Level;
        Assert.Equal(expected, reopened.Player.GetPlayer().FisherLevel);
        Assert.Equal(PlayerSave.CurrentVersion, moved.SaveVersion);
        Assert.Equal(reopened.Session.Config.Progression.Fisher.XpCurveVersion, moved.FisherXpCurveVersion);
        Assert.Equal(40, moved.Dollars); // nothing paid on load: the milestones Nv.50 to Nv.100 pay in the pass (A-158)
        Assert.Equal(40, moved.Pass.LegacyDollarsUpToLevel);
        Assert.Contains("\"save_version\": " + PlayerSave.CurrentVersion, File.ReadAllText(path));

        // Reopening again changes nothing: the move happens once.
        var again = TestSupport.NewGame(saveDir: dir, clock: clock).Game;
        Assert.Equal(expected, again.Player.GetPlayer().FisherLevel);
        Assert.Equal(40, again.Session.Save.Dollars);

        // The pass pays the Dólares of Nv.50 to Nv.100 (never paid) and not those of Nv.10 to Nv.40 (already paid).
        Assert.True(again.Pass.ClaimAll().Succeeded);
        Assert.Equal(100, again.Session.Save.Dollars);
    }

    // ------------------------------------------------------------------ big numbers

    [Theory]
    [InlineData(845_320L, "845.320")]
    [InlineData(1_000_000L, "1 mi")]
    [InlineData(3_450_000L, "3,4 mi")]
    [InlineData(999_999_999L, "999,9 mi")]
    [InlineData(1_250_000_000L, "1,2 bi")]
    [InlineData(12_000_000_000L, "12 bi")]
    [InlineData(1_500_000_000_000L, "1,5 tri")]
    [InlineData(-2_500_000L, "-2,5 mi")]
    [InlineData(long.MaxValue, "9.223.372 tri")]
    [InlineData(long.MinValue, "-9.223.372 tri")]
    public void Big_amounts_are_short_in_brazilian_portuguese(long value, string expected) => Assert.Equal(expected, Format.Short(value));

    [Fact]
    public void Coins_and_xp_in_the_billions_do_not_overflow()
    {
        var (game, clock, _) = TestSupport.NewGame();
        var save = game.Session.Save;
        save.Coins = 50_000_000_000L;
        save.FisherXpTotal = 9_000_000_000L;
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, 300, stepSeconds: 30);
        game.Fishing.SellCatches(game.Fishing.GetFishingBox().Select(c => c.CatchId).ToList());

        Assert.True(game.Player.GetPlayer().Coins > 50_000_000_000L);
        Assert.True(save.FisherXpTotal > 9_000_000_000L);
        Assert.Equal("50 bi", Format.Short(50_000_000_000L));
        Assert.Equal("229.596.770", Format.Number(229_596_770L));
    }
}
