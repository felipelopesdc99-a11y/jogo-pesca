using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;
using Xunit;

namespace FishingIdle.GameService.Tests;

public sealed class CatchRulesTests
{
    private static readonly GameConfig Config = TestSupport.RealConfig();

    private static MapConfig Map(string id) { Config.TryGetMap(id, out var map); return map; }
    private static RodConfig Rod(string id) { Config.TryGetRod(id, out var rod); return rod; }

    [Fact]
    public void Size_categories_follow_the_20_60_19_1_distribution()
    {
        var rng = new Rng(12345);
        var counts = new Dictionary<string, int>();
        const int draws = 200_000;
        for (var i = 0; i < draws; i++)
        {
            var category = CatchRules.RollSizeCategory(Config, new RodBonusesConfig(), rng).Id;
            counts[category] = counts.GetValueOrDefault(category) + 1;
        }

        Assert.InRange(counts["small"] / (double)draws, 0.19, 0.21);
        Assert.InRange(counts["adult"] / (double)draws, 0.59, 0.61);
        Assert.InRange(counts["large"] / (double)draws, 0.18, 0.20);
        Assert.InRange(counts["exceptional"] / (double)draws, 0.008, 0.012);
    }

    [Fact]
    public void Every_catch_on_map_1_comes_from_map_1_with_a_size_inside_its_range()
    {
        var map = Map("map_01");
        var rod = Rod("rod_00_starter");
        var poolIds = map.FishPool.Select(e => e.SpeciesId).ToHashSet();

        for (var i = 0; i < 5000; i++)
        {
            var rolled = CatchRules.Roll(Config, map, rod, 1, Rng.For(99, 1, i));
            Assert.Contains(rolled.Species.Id, poolIds);
            var cm = rolled.SizeMm / 10.0;
            Assert.InRange(cm, rolled.Species.SizeCm.Min - 0.05, rolled.Species.SizeCm.Max + 0.05);
            Assert.True(rolled.FisherXp >= 1);
        }
    }

    [Fact]
    public void The_starter_rod_never_catches_a_rare_fish_even_on_a_map_that_has_one()
    {
        var map = Map("map_02");
        var starter = Rod("rod_00_starter");

        for (var i = 0; i < 20_000; i++)
        {
            var rolled = CatchRules.Roll(Config, map, starter, 1, Rng.For(7, 3, i));
            Assert.Equal("common", rolled.Species.Rarity);
        }
    }

    [Fact]
    public void Rod_1_can_catch_the_rare_fish_and_its_rarity_bonus_stays_modest()
    {
        var map = Map("map_02");
        var rod = Rod("rod_01");
        const int draws = 200_000;

        int CountRare(int level) =>
            Enumerable.Range(0, draws).Count(i => CatchRules.Roll(Config, map, rod, level, Rng.For(5, level, i)).Species.Rarity == "rare");

        var level1 = CountRare(1) / (double)draws;
        var level10 = CountRare(10) / (double)draws;

        // Aruanã is 5 of 1000 weight: 0.5% at Lv.1; +22% efficiency at Lv.10 must stay near 0.6%, never 22%.
        Assert.InRange(level1, 0.004, 0.006);
        Assert.InRange(level10, 0.005, 0.0075);
    }

    [Fact]
    public void The_starter_rod_never_generates_shells()
    {
        var map = Map("map_01");
        var starter = Rod("rod_00_starter");
        var shells = Enumerable.Range(0, 10_000).Sum(i => CatchRules.Roll(Config, map, starter, 1, Rng.For(1, 1, i)).Shells);
        Assert.Equal(0, shells);
    }

    [Fact]
    public void Sale_price_grows_continuously_with_size()
    {
        Config.TryGetSpecies("tambaqui", out var tambaqui);
        var smallest = CatchRules.SalePrice(Config, tambaqui, 400);
        var middle = CatchRules.SalePrice(Config, tambaqui, 750);
        var largest = CatchRules.SalePrice(Config, tambaqui, 1100);

        Assert.True(smallest < middle && middle < largest);
        Assert.Equal(120, middle); // base value at the 50th percentile
    }

    [Fact]
    public void The_same_seed_and_cycle_always_produce_the_same_catch()
    {
        var map = Map("map_01");
        var rod = Rod("rod_00_starter");
        var a = CatchRules.Roll(Config, map, rod, 1, Rng.For(42, 7, 13));
        var b = CatchRules.Roll(Config, map, rod, 1, Rng.For(42, 7, 13));

        Assert.Equal(a.Species.Id, b.Species.Id);
        Assert.Equal(a.SizeMm, b.SizeMm);
    }
}
