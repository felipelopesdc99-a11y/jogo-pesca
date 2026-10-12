using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>
/// M24-T27 (A-160), the owner's decision of 12/10/2026: fishing every 10 s online and 20 s offline, 15 s offline with
/// the VIP; the rarer fish per hour stay what they were with the 15 s cycle (each rarity bites 2/3 as often per fish);
/// the Fishing Box holds one day of online fishing; baits and boat fragments keep the same pace per hour.
/// "Before" here is A-159 (15 s online, 30 s offline, fishing_bite_multiplier 0,35).
/// </summary>
public sealed class TenSecondCycleTests
{
    private const long Day = 86_400_000L;
    private static readonly GameConfig Config = TestSupport.RealConfig();

    private const double OnlineCycleBefore = 15;
    private const double OfflineCycleBefore = 30;
    private const double BiteMultiplierBefore = 0.35;

    // A-159's fragment chances (config/equipment.json before M24-T27).
    private static readonly Dictionary<string, double> FragmentChanceBefore = new Dictionary<string, double>
    {
        ["boat_01"] = 0.0064, ["boat_02"] = 0.0065, ["boat_03"] = 0.0072, ["boat_04"] = 0.0077, ["boat_05"] = 0.0078,
        ["boat_06"] = 0.0081, ["boat_07"] = 0.0088, ["boat_08"] = 0.0093, ["boat_09"] = 0.0102, ["boat_10"] = 0.0103,
    };

    private static GameConfig ConfigBefore()
    {
        var result = TestSupport.ConfigWith(GameConfigLoader.ProgressionFile, json =>
        {
            var root = JObject.Parse(json);
            root["fishing"]!["online_cycle_seconds"] = OnlineCycleBefore;
            root["fishing"]!["offline_cycle_seconds"] = OfflineCycleBefore;
            foreach (var tier in root["rarity"]!["tiers"]!)
            {
                if ((string)tier["id"]! != "common")
                {
                    tier["fishing_bite_multiplier"] = BiteMultiplierBefore;
                }
            }

            return root.ToString();
        });
        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        return result.Config;
    }

    /// <summary>Expected fish of each rarity pulled out per fishing attempt, computed without the service.</summary>
    private static Dictionary<string, double> PulledPerAttempt(GameConfig config, string mapId, string rodId, int level, double gear)
    {
        config.TryGetMap(mapId, out var map);
        config.TryGetRod(rodId, out var rod);
        var bonuses = config.RodBonusesAt(rod, level);
        var pool = CatchRules.EligiblePool(config, map, rod)
            .Select(p => new KeyValuePair<SpeciesConfig, double>(p.Key, CatchRules.IsCommon(p.Key) ? p.Value : p.Value * (1 + bonuses.RarityEfficiency)))
            .ToList();
        pool = CatchRules.FishingWeights(config, pool);
        var total = pool.Sum(p => p.Value);
        var result = new Dictionary<string, double>();
        foreach (var pair in pool)
        {
            var pulled = pair.Value / total * CatchRules.SuccessChance(config, pair.Key.Rarity, bonuses.CatchSuccess + gear);
            result[pair.Key.Rarity] = (result.TryGetValue(pair.Key.Rarity, out var sum) ? sum : 0) + pulled;
        }

        return result;
    }

    private static double Gear(string boatId, int boatLevel, string baitId)
    {
        var boat = 0.0;
        if (boatId != null && Config.TryGetBoat(boatId, out var b))
        {
            boat = Config.BoatBonusAt(b, boatLevel);
        }

        var bait = 0.0;
        if (baitId != null && Config.TryGetBait(baitId, out var x))
        {
            bait = x.CatchSuccessBonus;
        }

        return boat + bait;
    }

    // ------------------------------------------------------------------ the cycles

    [Fact]
    public void Fishing_is_every_10_seconds_online_20_offline_and_15_offline_with_the_vip()
    {
        Assert.Equal(10, Config.Fishing.OnlineCycleSeconds);
        Assert.Equal(20, Config.Fishing.OfflineCycleSeconds);
        Assert.Equal(15, Config.Economy.Vip.OfflineCycleSeconds);
        Assert.Equal(24, Config.Fishing.OfflineAccumulationCapHours);
    }

    [Fact]
    public void One_hour_online_is_360_attempts_with_or_without_the_vip()
    {
        foreach (var vip in new[] { false, true })
        {
            var (game, clock, _) = TestSupport.NewGame();
            game.Session.Save.VipUntilMs = vip ? clock.UtcNowMs + 30 * Day : 0;
            game.Fishing.StartFishing();
            var catches = TestSupport.PlayFor(game, clock, 3600);
            Assert.Equal(360, catches.Count);
        }
    }

    [Fact]
    public void Three_hours_closed_is_540_attempts_without_the_vip_and_720_with_it()
    {
        var plain = Offline(vipHours: 0, closedHours: 3);
        Assert.Equal(540, plain.Update.NewCatches.Count);
        Assert.Equal(20, plain.CycleSeconds);
        Assert.Equal(0, plain.VipCycleSeconds);
        Assert.Equal(0, plain.VipAttempts);

        var vip = Offline(vipHours: 48, closedHours: 3);
        Assert.Equal(720, vip.Update.NewCatches.Count);
        Assert.Equal(20, vip.CycleSeconds);
        Assert.Equal(15, vip.VipCycleSeconds);
        Assert.Equal(720, vip.VipAttempts);
    }

    [Fact]
    public void The_vip_keeps_its_offline_xp_bonus_together_with_the_faster_cycle()
    {
        // The owner said "mantendo o buff": the +50% Fisher XP offline stays (OD-065 asks whether it should).
        var vip = Offline(vipHours: 48, closedHours: 3);
        var baseXp = vip.Update.XpGained - vip.Update.VipXpGained;
        Assert.InRange(vip.Update.VipXpGained / (double)baseXp, 0.4, 0.5);
    }

    [Fact]
    public void A_vip_that_ends_while_away_counts_15_seconds_only_until_it_ends()
    {
        // VIP for 1 h of 3 h away: 239 attempts at 15 s (the one that would end exactly when the VIP ends is no longer
        // VIP, like the XP bonus), then the rest at 20 s: (3 h − 239 × 15 s) ÷ 20 s = 360.
        var report = Offline(vipHours: 1, closedHours: 3);
        Assert.Equal(239, report.VipAttempts);
        Assert.Equal(239 + 360, report.Update.NewCatches.Count);
        Assert.Equal(15, report.VipCycleSeconds);

        // Between the 540 of no VIP and the 720 of a VIP for the whole time.
        Assert.InRange(report.Update.NewCatches.Count, 541, 719);
    }

    [Fact]
    public void A_whole_day_away_with_the_vip_is_5760_attempts_and_fits_the_box()
    {
        var report = Offline(vipHours: 48, closedHours: 30);
        Assert.True(report.Capped);
        Assert.Equal(5760, report.Update.NewCatches.Count);
        Assert.Equal(0, report.Update.SkippedBoxFull);
        Assert.True(Config.Economy.FishingBox.Capacity >= 5760);
    }

    [Fact]
    public void Without_a_vip_cycle_in_the_config_the_vip_does_not_change_the_offline_cycle()
    {
        var missing = TestSupport.ConfigWith(GameConfigLoader.EconomyFile, json =>
        {
            var root = JObject.Parse(json);
            ((JObject)root["vip"]!).Remove("offline_cycle_seconds");
            return root.ToString();
        }, certainCatch: true);
        Assert.True(missing.Succeeded, string.Join("\n", missing.Errors));
        Assert.Equal(0, missing.Config.Economy.Vip.OfflineCycleSeconds);

        var report = Offline(vipHours: 48, closedHours: 3, missing.Config);
        Assert.Equal(540, report.Update.NewCatches.Count);
        Assert.Equal(0, report.VipAttempts);
    }

    [Fact]
    public void A_vip_cycle_below_one_second_is_rejected()
    {
        var bad = TestSupport.ConfigWith(GameConfigLoader.EconomyFile, json =>
        {
            var root = JObject.Parse(json);
            root["vip"]!["offline_cycle_seconds"] = 0.5;
            return root.ToString();
        });
        Assert.False(bad.Succeeded);
        Assert.Contains(bad.Errors, e => e.Contains("vip.offline_cycle_seconds"));
    }

    [Fact]
    public void The_vip_screen_shows_both_offline_cycles()
    {
        var (game, _, _) = TestSupport.NewGame();
        var view = game.Vip.GetVip();
        Assert.Equal(15, view.OfflineCycleSeconds);
        Assert.Equal(20, view.NormalOfflineCycleSeconds);
        Assert.Equal(0.5, view.OfflineFisherXpBonus, 6);
    }

    // ------------------------------------------------------------------ rarities

    [Fact]
    public void Every_rarity_above_common_bites_two_thirds_as_often_per_fish_as_with_the_15_second_cycle()
    {
        foreach (var tier in Config.Progression.Rarity.Tiers.Where(t => t.Id != "common"))
        {
            Assert.Equal(BiteMultiplierBefore * Config.Fishing.OnlineCycleSeconds / OnlineCycleBefore, tier.FishingBiteMultiplier, 5);
        }
    }

    public static IEnumerable<object[]> Scenarios() => new[]
    {
        new object[] { "map_02", "rod_01", 10, "boat_01", 1, null },
        new object[] { "map_05", "rod_03", 30, "boat_05", 10, "bait_02" },
        new object[] { "map_10", "rod_05", 100, "boat_05", 100, "bait_03" },
        new object[] { "map_10", "rod_05", 1, null, 1, null },
    };

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void Rarer_fish_per_hour_stay_the_same_online_and_offline_and_all_fish_per_hour_go_up_about_half(string mapId, string rodId, int rodLevel, string boatId, int boatLevel, string baitId)
    {
        var gear = Gear(boatId, boatLevel, baitId);
        var before = PulledPerAttempt(ConfigBefore(), mapId, rodId, rodLevel, gear);
        var now = PulledPerAttempt(Config, mapId, rodId, rodLevel, gear);

        foreach (var cycles in new[] { (OnlineCycleBefore, Config.Fishing.OnlineCycleSeconds), (OfflineCycleBefore, Config.Fishing.OfflineCycleSeconds) })
        {
            var perHourBefore = 3600 / cycles.Item1;
            var perHourNow = 3600 / cycles.Item2;
            foreach (var rarity in before.Keys.Where(r => r != "common"))
            {
                var ratio = now[rarity] * perHourNow / (before[rarity] * perHourBefore);
                Assert.InRange(ratio, 0.999, 1.001);
            }

            // Commons fill what the rarer fish left: a little more than 1,5× the fish per hour.
            var allRatio = now.Values.Sum() * perHourNow / (before.Values.Sum() * perHourBefore);
            Assert.InRange(allRatio, 1.499, 1.6);

            // And the chance of a rarer fish per fish pulled out goes down.
            var rareShareBefore = before.Where(p => p.Key != "common").Sum(p => p.Value) / before.Values.Sum();
            var rareShareNow = now.Where(p => p.Key != "common").Sum(p => p.Value) / now.Values.Sum();
            Assert.True(rareShareNow < rareShareBefore || rareShareBefore == 0);
        }
    }

    [Fact]
    public void Simulated_with_a_fixed_seed_rarer_fish_per_hour_stay_about_the_same()
    {
        var before = ConfigBefore();
        GameConfig[] configs = { before, Config };
        var results = configs.Select(c =>
        {
            c.TryGetMap("map_10", out var map);
            c.TryGetRod("rod_05", out var rod);
            c.TryGetBoat("boat_05", out var boat);
            c.TryGetBait("bait_03", out var bait);
            return CatchSimulator.Run(c, map, rod, 100, boat, bait, 200_000, 2212, 100);
        }).ToList();

        double RarePerHour(CatchSimulationResult r) =>
            r.ByRarity.Where(x => x.RarityId != "common").Sum(x => x.Caught) / (double)r.Attempts * r.AttemptsPerHour;

        Assert.Equal(240, results[0].AttemptsPerHour, 6);
        Assert.Equal(360, results[1].AttemptsPerHour, 6);
        Assert.InRange(RarePerHour(results[1]) / RarePerHour(results[0]), 0.95, 1.05);
        Assert.InRange(results[1].CatchesPerHour / results[0].CatchesPerHour, 1.45, 1.75);
    }

    // ------------------------------------------------------------------ box, baits, fragments

    [Fact]
    public void The_box_holds_one_day_of_online_fishing()
    {
        Assert.Equal(8640, Config.Economy.FishingBox.Capacity);
        Assert.Equal(86400 / Config.Fishing.OnlineCycleSeconds, Config.Economy.FishingBox.Capacity, 6);
    }

    // The 180-charge bait packs of M24-T27 became the 999-attempt baits of M24-T30 (A-161): ThreeBaitsTests.

    [Fact]
    public void Boat_fragments_per_hour_stay_about_the_same_as_with_the_15_second_cycle()
    {
        var before = ConfigBefore();
        foreach (var boat in Config.Equipment.Boats.Where(b => b.MapId != null))
        {
            Config.TryGetMap(boat.MapId, out var map);
            var rod = Config.Rods.Rods.OrderBy(r => r.Tier).First(r => r.Tier >= map.MinimumRodTier);
            var pulledBefore = PulledPerAttempt(before, map.Id, rod.Id, 1, 0).Values.Sum();
            var pulledNow = PulledPerAttempt(Config, map.Id, rod.Id, 1, 0).Values.Sum();
            var perHourBefore = 3600 / OnlineCycleBefore * pulledBefore * FragmentChanceBefore[boat.Id];
            var perHourNow = 3600 / Config.Fishing.OnlineCycleSeconds * pulledNow * boat.FragmentDropChance;

            Assert.InRange(perHourNow / perHourBefore, 0.95, 1.05);
            Assert.InRange(boat.FragmentDropChance / FragmentChanceBefore[boat.Id], 0.6, 0.7);
        }
    }

    // ------------------------------------------------------------------ helpers

    private static OfflineReport Offline(double vipHours, double closedHours, GameConfig config = null)
    {
        var saveDir = TestSupport.NewTempDirectory();
        var clock = new ManualClock(TestSupport.StartMs);
        var (game, _, _) = TestSupport.NewGame(config, saveDir, clock);
        game.Session.Save.VipUntilMs = vipHours > 0 ? clock.UtcNowMs + (long)(vipHours * 3_600_000) : 0;
        game.Fishing.StartFishing();
        game.Fishing.MarkSeen();

        clock.AdvanceSeconds(closedHours * 3600);
        var (reopened, _, _) = TestSupport.NewGame(config, saveDir, clock);
        var report = reopened.Fishing.TakeOfflineReport();
        Assert.NotNull(report);
        return report;
    }
}
