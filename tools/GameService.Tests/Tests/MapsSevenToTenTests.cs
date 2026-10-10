using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>Maps 7 to 10, the Mítico rarity and Varas 4 and 5 (docs/PROGRESSAO_MAPAS_5_A_10_ADAPTADA.md).</summary>
public sealed class MapsSevenToTenTests
{
    private static readonly GameConfig Config = TestSupport.RealConfig();

    private static MapConfig Map(string id) { Config.TryGetMap(id, out var map); return map; }
    private static RodConfig Rod(string id) { Config.TryGetRod(id, out var rod); return rod; }

    /// <summary>A player at a given level with plenty of coins and the rods asked for (the last one equipped).</summary>
    private static LocalGame Player(int level, params string[] rods)
    {
        var (game, _, _) = TestSupport.NewGame();
        game.Session.Save.FisherLevel = level;
        game.Session.Save.Coins = 100_000_000;
        game.Session.Save.Shells = 1_000_000;
        foreach (var rod in rods)
        {
            Assert.True(game.Shop.BuyRod(rod).Succeeded);
        }

        return game;
    }

    [Theory]
    [InlineData("map_07", 60, "rod_03", "rod_02")]
    [InlineData("map_08", 70, "rod_04", "rod_03")]
    [InlineData("map_09", 80, "rod_04", "rod_03")]
    [InlineData("map_10", 90, "rod_05", "rod_04")]
    public void Each_map_needs_its_level_and_its_rod(string mapId, int level, string rod, string weaker)
    {
        var all = new[] { "rod_01", "rod_02", "rod_03", "rod_04", "rod_05" };
        var upToWeaker = all.Take(Array.IndexOf(all, weaker) + 1).ToArray();
        var upToRod = all.Take(Array.IndexOf(all, rod) + 1).ToArray();

        Assert.Equal(ServiceError.MapLocked, Player(level - 1, upToWeaker).Maps.TravelTo(mapId).Error);
        Assert.Equal(ServiceError.RodTooWeakForMap, Player(level, upToWeaker).Maps.TravelTo(mapId).Error);
        Assert.True(Player(level, upToRod).Maps.TravelTo(mapId).Succeeded);
    }

    [Fact]
    public void Rods_4_and_5_can_be_bought_at_any_level_with_the_money()
    {
        // M24-T09: no minimum level; the map still asks for its level (Each_map_needs_its_level_and_its_rod).
        Assert.True(Player(1, "rod_01", "rod_02", "rod_03").Shop.BuyRod("rod_04").Succeeded);
        Assert.True(Player(1, "rod_01", "rod_02", "rod_03", "rod_04").Shop.BuyRod("rod_05").Succeeded);
    }

    [Fact]
    public void Only_rod_5_pulls_the_mythic_out_of_map_10()
    {
        var withRod5 = Enumerable.Range(0, 200_000).Select(i => CatchRules.Roll(Config, Map("map_10"), Rod("rod_05"), 10, Rng.For(10, 10, i)).Species).ToList();
        Assert.Contains(withRod5, s => s.Id == "tubarao_boca_grande");
        Assert.All(Enumerable.Range(0, 50_000), i => Assert.NotEqual("mythic", CatchRules.Roll(Config, Map("map_10"), Rod("rod_04"), 10, Rng.For(11, 11, i)).Species.Rarity));
    }

    [Fact]
    public void Mythic_is_the_top_rarity_and_counts_as_valuable()
    {
        Assert.True(Config.RarityRank("mythic") > Config.RarityRank("legendary"));
        Assert.Contains("mythic", Config.Economy.FishingBox.BulkSaleProtection.Rarities);
        Assert.Equal(0.08, CatchRules.SuccessChance(Config, "mythic", 0), 6);
    }

    [Fact]
    public void Every_rod_catches_something_on_every_map_it_may_fish()
    {
        foreach (var map in Config.Maps.Maps)
        {
            foreach (var rod in Config.Rods.Rods.Where(r => r.Tier >= map.MinimumRodTier))
            {
                var species = CatchRules.Roll(Config, map, rod, 1, Rng.For(1, 2, 3)).Species;
                Assert.Contains(map.FishPool, e => e.SpeciesId == species.Id);
            }
        }
    }
}
