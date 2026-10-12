using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>
/// M24-T22 (A-159), the owner's decision of 11/10/2026: twice the fish per hour (the fishing cycle cut in half), the
/// rarer fish per hour at ~70% of before (each rarity bites 35% as often per fish), and the boats' fragments per hour
/// about the same (the chance per fish cut in half and recalibrated).
/// M24-T27 (A-160, 12/10/2026) went further: the cycle is a third of the original (10 s online, 20 s offline) and each
/// rarity bites 0,35 × 2/3 as often per fish, so the rarer fish per hour are still 70% of the original (the same as
/// A-159) and the fragments per hour still about the same. TenSecondCycleTests compares with A-159.
/// </summary>
public sealed class MoreFishFewerRaresTests
{
    private static readonly GameConfig Config = TestSupport.RealConfig();

    // What the game had before M24-T22 (A-157, A-159): 30 s online, 60 s offline and these fragment chances.
    private const double OnlineCycleBefore = 30;
    private const double OfflineCycleBefore = 60;

    private static readonly Dictionary<string, double> FragmentChanceBefore = new Dictionary<string, double>
    {
        ["boat_01"] = 0.013, ["boat_02"] = 0.013, ["boat_03"] = 0.015, ["boat_04"] = 0.015, ["boat_05"] = 0.016,
        ["boat_06"] = 0.017, ["boat_07"] = 0.019, ["boat_08"] = 0.02, ["boat_09"] = 0.023, ["boat_10"] = 0.024,
    };

    private static MapConfig Map(string id) { Config.TryGetMap(id, out var map); return map; }
    private static RodConfig Rod(string id) { Config.TryGetRod(id, out var rod); return rod; }
    private static BoatConfig Boat(string id) { Config.TryGetBoat(id, out var boat); return boat; }
    private static BaitConfig Bait(string id) { Config.TryGetBait(id, out var bait); return bait; }

    /// <summary>The typical moments of the game: start (map 2), middle (map 5) and end (map 10, everything at Nv.100).</summary>
    public static IEnumerable<object[]> Scenarios() => new[]
    {
        new object[] { "map_02", "rod_01", 10, "boat_01", 1, null },
        new object[] { "map_05", "rod_03", 30, "boat_05", 10, "bait_02" },
        new object[] { "map_10", "rod_05", 100, "boat_05", 100, "bait_03" },
        new object[] { "map_10", "rod_05", 1, null, 1, null },
    };

    /// <summary>The config as it was before M24-T22: the old cycle and no fishing_bite_multiplier.</summary>
    private static GameConfig ConfigBefore()
    {
        var result = TestSupport.ConfigWith(GameConfigLoader.ProgressionFile, json =>
        {
            var root = JObject.Parse(json);
            root["fishing"]!["online_cycle_seconds"] = OnlineCycleBefore;
            root["fishing"]!["offline_cycle_seconds"] = OfflineCycleBefore;
            foreach (var tier in root["rarity"]!["tiers"]!)
            {
                tier["fishing_bite_multiplier"] = 1.0;
            }

            return root.ToString();
        });
        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        return result.Config;
    }

    /// <summary>
    /// Expected fish of each rarity pulled out per attempt, computed without the service: the map's weights with the
    /// rod's rarity bonus, then (when <paramref name="fishing"/>) the rarities' fishing_bite_multiplier, times the
    /// Catch Success chance.
    /// </summary>
    private static Dictionary<string, double> PulledPerAttempt(MapConfig map, RodConfig rod, int level, double gear, bool fishing)
    {
        var bonuses = Config.RodBonusesAt(rod, level);
        var pool = CatchRules.EligiblePool(Config, map, rod)
            .Select(p => new KeyValuePair<SpeciesConfig, double>(p.Key, CatchRules.IsCommon(p.Key) ? p.Value : p.Value * (1 + bonuses.RarityEfficiency)))
            .ToList();
        if (fishing)
        {
            pool = CatchRules.FishingWeights(Config, pool);
        }

        var total = pool.Sum(p => p.Value);
        var result = new Dictionary<string, double>();
        foreach (var pair in pool)
        {
            var pulled = pair.Value / total * CatchRules.SuccessChance(Config, pair.Key.Rarity, bonuses.CatchSuccess + gear);
            result[pair.Key.Rarity] = (result.TryGetValue(pair.Key.Rarity, out var sum) ? sum : 0) + pulled;
        }

        return result;
    }

    private static double Gear(string boatId, int boatLevel, string baitId) =>
        (boatId != null ? Config.BoatBonusAt(Boat(boatId), boatLevel) : 0) + (baitId != null ? Bait(baitId).CatchSuccessBonus : 0);

    [Fact]
    public void The_fishing_cycle_is_a_third_of_what_it_was_online_and_offline_with_the_same_cap()
    {
        Assert.Equal(OnlineCycleBefore / 3, Config.Fishing.OnlineCycleSeconds);
        Assert.Equal(OfflineCycleBefore / 3, Config.Fishing.OfflineCycleSeconds);
        Assert.Equal(24, Config.Fishing.OfflineAccumulationCapHours);
    }

    [Fact]
    public void Every_rarity_above_common_bites_35_percent_times_two_thirds_as_often_while_fishing()
    {
        foreach (var tier in Config.Progression.Rarity.Tiers.Where(t => t.Id != "common"))
        {
            Assert.Equal(0.35 * 10 / 15, tier.FishingBiteMultiplier, 5);
        }
    }

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void Rarer_fish_per_hour_stay_at_70_percent_and_all_fish_per_hour_about_triple(string mapId, string rodId, int rodLevel, string boatId, int boatLevel, string baitId)
    {
        var gear = Gear(boatId, boatLevel, baitId);
        var before = PulledPerAttempt(Map(mapId), Rod(rodId), rodLevel, gear, fishing: false);
        var now = PulledPerAttempt(Map(mapId), Rod(rodId), rodLevel, gear, fishing: true);
        var perHourBefore = 3600 / OnlineCycleBefore;
        var perHourNow = 3600 / Config.Fishing.OnlineCycleSeconds;

        foreach (var rarity in before.Keys.Where(r => r != "common"))
        {
            var ratio = now[rarity] * perHourNow / (before[rarity] * perHourBefore);
            Assert.InRange(ratio, 0.699, 0.701);
        }

        // Commons fill what the rarer fish left, and they are pulled out more easily: a little more than triple.
        var allRatio = now.Values.Sum() * perHourNow / (before.Values.Sum() * perHourBefore);
        Assert.InRange(allRatio, 2.99, 3.6);
    }

    [Theory]
    [InlineData("map_02", "rod_01", 10, "boat_01", 1, null, 400_000, 0.58, 0.82)]
    [InlineData("map_10", "rod_05", 100, "boat_05", 100, "bait_03", 200_000, 0.66, 0.74)]
    public void Simulated_with_a_fixed_seed_rarer_fish_per_hour_fall_to_about_70_percent(string mapId, string rodId, int rodLevel, string boatId, int boatLevel, string baitId, int attempts, double min, double max)
    {
        var before = ConfigBefore();
        RodConfig RodOf(GameConfig c) { c.TryGetRod(rodId, out var r); return r; }
        MapConfig MapOf(GameConfig c) { c.TryGetMap(mapId, out var m); return m; }
        BoatConfig BoatOf(GameConfig c) { c.TryGetBoat(boatId, out var b); return b; }
        BaitConfig BaitOf(GameConfig c) { if (baitId == null) return null; c.TryGetBait(baitId, out var b); return b; }

        var old = CatchSimulator.Run(before, MapOf(before), RodOf(before), rodLevel, BoatOf(before), BaitOf(before), attempts, 2210, boatLevel);
        var now = CatchSimulator.Run(Config, MapOf(Config), RodOf(Config), rodLevel, BoatOf(Config), BaitOf(Config), attempts, 2210, boatLevel);

        double RarePerHour(CatchSimulationResult r) =>
            r.ByRarity.Where(x => x.RarityId != "common").Sum(x => x.Caught) / (double)r.Attempts * r.AttemptsPerHour;

        Assert.Equal(120, old.AttemptsPerHour, 6);
        Assert.Equal(360, now.AttemptsPerHour, 6);
        Assert.InRange(RarePerHour(now) / RarePerHour(old), min, max);
        Assert.InRange(now.CatchesPerHour / old.CatchesPerHour, 2.85, 3.75);
    }

    [Fact]
    public void One_hour_online_is_360_attempts_and_one_day_offline_is_4320()
    {
        var (game, clock, dir) = TestSupport.NewGame(TestSupport.RealConfig());
        game.Fishing.StartFishing();
        var attempts = 0;
        for (var i = 0; i < 3600; i++)
        {
            clock.AdvanceSeconds(1);
            var update = game.Fishing.Sync();
            attempts += update.NewCatches.Count + update.Escapes.Count;
        }

        Assert.Equal(360, attempts);

        game.Fishing.MarkSeen();
        clock.AdvanceSeconds(30 * 3600);
        var (reopened, _, _) = TestSupport.NewGame(TestSupport.RealConfig(), dir, clock);
        var report = reopened.Fishing.TakeOfflineReport();
        Assert.True(report.Capped);
        Assert.Equal(4320, report.Update.NewCatches.Count + report.Update.Escapes.Count);
    }

    [Fact]
    public void Boat_fragments_per_hour_stay_about_the_same_on_every_map()
    {
        // With the minimum rod of the map at Nv.1 and no boat or bait: fragments per hour = attempts per hour ×
        // fish pulled out per attempt × chance per fish (A-157). The chance was halved and recalibrated (A-159), then
        // cut by 1,5 and recalibrated again (A-160): about a third of A-157.
        foreach (var boat in Config.Equipment.Boats.Where(b => b.MapId != null))
        {
            var map = Map(boat.MapId);
            var rod = Config.Rods.Rods.OrderBy(r => r.Tier).First(r => r.Tier >= map.MinimumRodTier);
            var pulledBefore = PulledPerAttempt(map, rod, 1, 0, fishing: false).Values.Sum();
            var pulledNow = PulledPerAttempt(map, rod, 1, 0, fishing: true).Values.Sum();
            var before = 3600 / OnlineCycleBefore * pulledBefore * FragmentChanceBefore[boat.Id];
            var now = 3600 / Config.Fishing.OnlineCycleSeconds * pulledNow * boat.FragmentDropChance;

            Assert.InRange(now / before, 0.9, 1.1);
            Assert.InRange(boat.FragmentDropChance, FragmentChanceBefore[boat.Id] * 0.25, FragmentChanceBefore[boat.Id] * 0.37);
        }
    }

    [Fact]
    public void Expeditions_and_the_market_sellers_keep_the_plain_draw()
    {
        // The fishing multiplier is only for fishing: a fish found on an Expedition (CatchRules.Roll / RollFound) is drawn
        // as before, so it is not rarer than it was.
        var map = Map("map_10");
        var rod = Rod("rod_05");
        var bonuses = Config.RodBonusesAt(rod, 1);
        const int draws = 50_000;
        var plain = Enumerable.Range(0, draws).Count(i => CatchRules.RollSpecies(Config, map, rod, bonuses, Rng.For(31, 7, i)).Rarity != "common");
        var fishing = Enumerable.Range(0, draws).Count(i => CatchRules.RollBite(Config, map, rod, bonuses, Rng.For(31, 7, i)).Rarity != "common");
        var expected = 1 - PulledShareOfCommons(map, rod);

        Assert.InRange(plain / (double)draws, expected - 0.02, expected + 0.02);
        Assert.InRange(fishing / (double)plain, 0.20, 0.27);
    }

    private static double PulledShareOfCommons(MapConfig map, RodConfig rod)
    {
        var bonuses = Config.RodBonusesAt(rod, 1);
        var pool = CatchRules.EligiblePool(Config, map, rod);
        var weights = pool.Select(p => CatchRules.IsCommon(p.Key) ? p.Value : p.Value * (1 + bonuses.RarityEfficiency)).ToList();
        return pool.Where(p => CatchRules.IsCommon(p.Key)).Sum(p => p.Value) / weights.Sum();
    }

    [Fact]
    public void The_encyclopedia_bite_share_uses_the_fishing_chance_and_adds_up_to_one_per_map()
    {
        foreach (var map in Config.Maps.Maps)
        {
            var shares = map.FishPool.Select(p => CatchRules.BiteShare(Config, map, p.SpeciesId)).ToList();
            Assert.Equal(1.0, shares.Sum(), 6);
        }

        // Map 2: the Aruanã (the only Rare, 10 of the pool's weight) bites 0,233333 × as often as its plain share (A-160).
        var map2 = Map("map_02");
        var plainShare = map2.FishPool.Single(p => p.SpeciesId == "aruana").CatchWeight / map2.FishPool.Sum(p => p.CatchWeight);
        Assert.Equal(plainShare * 0.233333, CatchRules.BiteShare(Config, map2, "aruana"), 6);
    }

    [Fact]
    public void A_pool_without_commons_and_a_negative_multiplier_are_handled()
    {
        var negative = TestSupport.ConfigWith(GameConfigLoader.ProgressionFile, json =>
        {
            var root = JObject.Parse(json);
            root["rarity"]!["tiers"]![1]!["fishing_bite_multiplier"] = -0.5;
            return root.ToString();
        });
        Assert.False(negative.Succeeded);
        Assert.Contains(negative.Errors, e => e.Contains("fishing_bite_multiplier"));

        // Without the field the rarity bites as it always did.
        var missing = TestSupport.ConfigWith(GameConfigLoader.ProgressionFile, json =>
        {
            var root = JObject.Parse(json);
            foreach (var tier in root["rarity"]!["tiers"]!)
            {
                ((JObject)tier).Remove("fishing_bite_multiplier");
            }

            return root.ToString();
        });
        Assert.True(missing.Succeeded, string.Join("\n", missing.Errors));
        Assert.All(missing.Config.Progression.Rarity.Tiers, t => Assert.Equal(1.0, t.FishingBiteMultiplier));

        // Only rarer fish in a pool: nothing to fill the rest, so the weights stay as they are.
        var rares = Config.FishCatalog.Species.Where(s => s.Rarity != "common").Take(3)
            .Select(s => new KeyValuePair<SpeciesConfig, double>(s, 10)).ToList();
        Assert.Equal(rares.Select(p => p.Value), CatchRules.FishingWeights(Config, rares).Select(p => p.Value));
    }
}
