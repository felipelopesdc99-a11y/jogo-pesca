using FishingIdle.GameService.Config;
using FishingIdle.GameService.Fishing;
using FishingIdle.GameService.Persistence;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>
/// M24-T31 (A-162), the owner's decision of 12/10/2026: "60 dias pro lvl 1000". Since M24-T46 (A-172) the Fisher's XP
/// curve is version 4: the Crew and the Upgrades left the game and the owner kept the 60 days, so levels 1 to 249 are the
/// ones of version 3 (and levels 1 to 99 the ones of version 2: Nv.10 in ~20 min, Nv.100 in ~0,5 day with the game open)
/// and the rest is recalibrated so Nv.1000 comes in ~60 days with the game always open (fishing only, no clothing, no
/// pass, no VIP). Saves on curves 1, 2 or 3 are moved by their total XP and never lose a level (TD-038).
/// </summary>
public sealed class FisherCurveSixtyDaysTests
{
    private static readonly GameConfig Config = TestSupport.RealConfig();

    // Total XP from Nv.1 to each level on curve 2 (config/progression.json before M24-T31, A-153).
    private static readonly Dictionary<int, long> CurveTwoTotals = new Dictionary<int, long>
    {
        [100] = 309_220, [101] = 318_860, [150] = 1_166_030, [250] = 6_534_030, [500] = 39_246_530,
        [750] = 99_300_530, [999] = 188_545_530, [1000] = 188_953_530,
    };

    // Total XP from Nv.1 to each level on curve 3 (config/progression.json before M24-T46, A-162).
    private static readonly Dictionary<int, long> CurveThreeTotals = new Dictionary<int, long>
    {
        [250] = 8_268_470, [300] = 13_531_770, [500] = 48_079_770, [750] = 120_337_770, [1000] = 229_596_770,
    };

    private static long TotalTo(int level)
    {
        long total = 0;
        for (var l = 1; l < level; l++)
        {
            total += Config.FisherXpToNextLevel(l);
        }

        return total;
    }

    private static PlayerSave SaveOnCurve(int curve, int level, long xpInLevel, long total)
    {
        return new PlayerSave { FisherLevel = level, FisherXp = xpInLevel, FisherXpTotal = total, FisherXpCurveVersion = curve };
    }

    [Fact]
    public void The_curve_is_version_4()
    {
        Assert.Equal(4, Config.Progression.Fisher.XpCurveVersion);
        Assert.Equal(1000, Config.Progression.Fisher.MaxLevel);
    }

    [Fact]
    public void The_whole_curve_asks_218_million_xp_and_the_start_did_not_change()
    {
        Assert.Equal(217_677_170L, TotalTo(1000));
        Assert.Equal(48_758_170L, TotalTo(500));
        // Levels 1 to 249 are the ones of curve 3 (A-172): nobody below Nv.250 sees a change.
        Assert.Equal(CurveThreeTotals[250], TotalTo(250));
        Assert.Equal(91_800, Config.FisherXpToNextLevel(249));
        Assert.Equal(94_500, Config.FisherXpToNextLevel(250));
        Assert.Equal(457_000, Config.FisherXpToNextLevel(999));
        // Levels 1 to 99 are the ones of curve 2: Nv.10 and Nv.100 come at the same time as before.
        Assert.Equal(990L, TotalTo(10));
        Assert.Equal(309_220L, TotalTo(100));
        Assert.Equal(10, Config.FisherXpToNextLevel(1));
        Assert.Equal(715, Config.FisherXpToNextLevel(10));
        Assert.Equal(2_200, Config.FisherXpToNextLevel(50));
        Assert.Equal(9_330, Config.FisherXpToNextLevel(99));
    }

    [Fact]
    public void After_level_100_every_level_asks_more_than_on_curve_2_and_the_steps_stay_small()
    {
        foreach (var pair in CurveTwoTotals.Where(p => p.Key > 100))
        {
            Assert.True(TotalTo(pair.Key) > pair.Value, "Nv." + pair.Key);
        }

        for (var level = 2; level < 1000; level++)
        {
            var now = Config.FisherXpToNextLevel(level);
            var before = Config.FisherXpToNextLevel(level - 1);
            Assert.True(now >= before, "Nv." + level);
            if (level > 10)
            {
                Assert.True(now <= before * 1.05, "Nv." + level + " jumps more than 5%");
            }
        }
    }

    [Fact]
    public void A_curve_2_save_never_goes_down_a_level()
    {
        foreach (var pair in CurveTwoTotals)
        {
            var level = Math.Min(pair.Key, 1000);
            var save = SaveOnCurve(2, level, 1_234, pair.Value + 1_234);

            var change = FisherLevelRules.MoveToCurrentCurve(Config, save);

            Assert.NotNull(change);
            Assert.True(save.FisherLevel >= level, "Nv." + level + " went down to Nv." + save.FisherLevel);
            Assert.Equal(4, save.FisherXpCurveVersion);
            Assert.True(level >= 1000 ? save.FisherXp == 0 : save.FisherXp < Config.FisherXpToNextLevel(save.FisherLevel));
        }
    }

    [Fact]
    public void A_curve_2_player_up_to_level_100_stays_exactly_where_it_was()
    {
        for (var level = 1; level <= 100; level++)
        {
            var xp = level < 100 ? Config.FisherXpToNextLevel(level) / 2 : 77;
            var save = SaveOnCurve(2, level, xp, TotalTo(level) + xp);
            FisherLevelRules.MoveToCurrentCurve(Config, save);
            Assert.Equal(level, save.FisherLevel);
            Assert.Equal(xp, save.FisherXp);
        }
    }

    [Fact]
    public void A_curve_2_level_500_keeps_level_500_and_its_xp()
    {
        // On curve 4, the 39,2 million XP of a curve-2 Nv.500 only reach Nv.455: the player keeps Nv.500.
        Assert.Equal(455, FisherLevelRules.LevelForTotalXp(Config, CurveTwoTotals[500]).Level);
        var save = SaveOnCurve(2, 500, 100_000, CurveTwoTotals[500] + 100_000);

        var change = FisherLevelRules.MoveToCurrentCurve(Config, save);

        Assert.Equal(500, change.LevelBefore);
        Assert.Equal(500, change.LevelAfter);
        Assert.Equal(500, save.FisherLevel);
        Assert.Equal(100_000, save.FisherXp);
        Assert.Equal(CurveTwoTotals[500] + 100_000, save.FisherXpTotal);
    }

    [Fact]
    public void A_curve_1_save_goes_straight_to_curve_4()
    {
        // The old Nv.100 (curve 1, 11,5 million XP) lands on Nv.281 (Nv.304 on curve 2, Nv.282 on curve 3).
        var save = SaveOnCurve(1, 100, 0, 11_530_255);
        var (expected, rest) = FisherLevelRules.LevelForTotalXp(Config, 11_530_255);
        Assert.Equal(281, expected);

        var change = FisherLevelRules.MoveToCurrentCurve(Config, save);

        Assert.Equal(100, change.LevelBefore);
        Assert.Equal(282, save.FisherLevel);
        Assert.Equal(rest, save.FisherXp);
        Assert.Equal(4, save.FisherXpCurveVersion);
        Assert.Null(FisherLevelRules.MoveToCurrentCurve(Config, save));
    }

    [Fact]
    public void A_curve_3_save_never_goes_down_and_climbs_where_curve_4_asks_less()
    {
        // Below Nv.250 the curves are the same: the level and the XP stay exactly as they were.
        var low = SaveOnCurve(3, 200, 1_000, TotalTo(200) + 1_000);
        FisherLevelRules.MoveToCurrentCurve(Config, low);
        Assert.Equal(200, low.FisherLevel);
        Assert.Equal(1_000, low.FisherXp);
        Assert.Equal(4, low.FisherXpCurveVersion);

        // Nv.300 and Nv.500 ask a little more on curve 4: the player keeps the level and the XP inside it.
        var mid = SaveOnCurve(3, 500, 100_000, CurveThreeTotals[500] + 100_000);
        var change = FisherLevelRules.MoveToCurrentCurve(Config, mid);
        Assert.Equal(500, change.LevelBefore);
        Assert.Equal(500, mid.FisherLevel);
        Assert.Equal(100_000, mid.FisherXp);

        // Nv.750 asks less on curve 4: the same total XP reaches Nv.757.
        var high = SaveOnCurve(3, 750, 5_000, CurveThreeTotals[750] + 5_000);
        FisherLevelRules.MoveToCurrentCurve(Config, high);
        Assert.Equal(757, high.FisherLevel);
        Assert.Equal(7_600, high.FisherXp);

        // The top stays at the top.
        var top = SaveOnCurve(3, 1000, 0, CurveThreeTotals[1000]);
        FisherLevelRules.MoveToCurrentCurve(Config, top);
        Assert.Equal(1000, top.FisherLevel);
        Assert.Equal(0, top.FisherXp);
    }

    [Fact]
    public void A_curve_2_save_on_disk_is_moved_once_and_keeps_its_level()
    {
        var (game, clock, dir) = TestSupport.NewGame();
        var save = game.Session.Save;
        save.FisherLevel = 750;
        save.FisherXp = 5_000;
        save.FisherXpTotal = CurveTwoTotals[750] + 5_000;
        game.Session.Persist();

        var path = Path.Combine(dir, JsonFilePlayerRepository.SaveFileName);
        var json = JObject.Parse(File.ReadAllText(path));
        json["fisher_xp_curve_version"] = 2;
        File.WriteAllText(path, json.ToString());

        var reopened = TestSupport.NewGame(saveDir: dir, clock: clock).Game;
        Assert.Equal(750, reopened.Player.GetPlayer().FisherLevel);
        Assert.Equal(5_000, reopened.Session.Save.FisherXp);
        Assert.Equal(4, reopened.Session.Save.FisherXpCurveVersion);
        reopened.Session.Persist();
        Assert.Equal(4, (int)JObject.Parse(File.ReadAllText(path))["fisher_xp_curve_version"]);

        var again = TestSupport.NewGame(saveDir: dir, clock: clock).Game;
        Assert.Equal(750, again.Player.GetPlayer().FisherLevel);
    }
}
