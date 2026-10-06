using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>Maps 5 and 6, the Lendário rarity and Vara 3 (docs/PROGRESSAO_MAPAS_5_A_10_ADAPTADA.md).</summary>
public sealed class MapsFiveSixTests
{
    private static readonly GameConfig Config = TestSupport.RealConfig();

    private static MapConfig Map(string id) { Config.TryGetMap(id, out var map); return map; }
    private static RodConfig Rod(string id) { Config.TryGetRod(id, out var rod); return rod; }

    /// <summary>A player at a given level with plenty of coins and the rods asked for (the last one equipped).</summary>
    private static LocalGame Player(int level, params string[] rods)
    {
        var (game, _, _) = TestSupport.NewGame();
        game.Session.Save.FisherLevel = level;
        game.Session.Save.Coins = 10_000_000;
        game.Session.Save.Shells = 1_000_000;
        foreach (var rod in rods)
        {
            Assert.True(game.Shop.BuyRod(rod).Succeeded);
        }

        return game;
    }

    [Fact]
    public void Map_5_opens_at_level_40_for_rod_2()
    {
        Assert.Equal(ServiceError.MapLocked, Player(39, "rod_01", "rod_02").Maps.TravelTo("map_05").Error);
        Assert.Equal(ServiceError.RodTooWeakForMap, Player(40, "rod_01").Maps.TravelTo("map_05").Error);
        Assert.True(Player(40, "rod_01", "rod_02").Maps.TravelTo("map_05").Succeeded);
    }

    [Fact]
    public void Map_6_needs_level_50_and_rod_3()
    {
        Assert.Equal(ServiceError.MapLocked, Player(49, "rod_01", "rod_02").Maps.TravelTo("map_06").Error);
        Assert.Equal(ServiceError.RodTooWeakForMap, Player(50, "rod_01", "rod_02").Maps.TravelTo("map_06").Error);
        Assert.Equal(ServiceError.RodLocked, Player(49, "rod_01", "rod_02").Shop.BuyRod("rod_03").Error);

        var game = Player(50, "rod_01", "rod_02", "rod_03");
        Assert.Equal(10_000_000 - 2_500 - 90_000 - 730_000, game.Session.Save.Coins);
        Assert.True(game.Maps.TravelTo("map_06").Succeeded);
    }

    [Fact]
    public void Rod_3_pulls_the_veleiro_out_of_map_6_and_rod_2_never_catches_a_legendary()
    {
        var map6 = Enumerable.Range(0, 60_000).Select(i => CatchRules.Roll(Config, Map("map_06"), Rod("rod_03"), 10, Rng.For(6, 6, i)).Species).ToList();
        Assert.Contains(map6, s => s.Id == "veleiro");
        Assert.Contains(map6, s => s.Rarity == "epic");
        Assert.All(Enumerable.Range(0, 20_000), i => Assert.NotEqual("legendary", CatchRules.Roll(Config, Map("map_06"), Rod("rod_02"), 10, Rng.For(7, 7, i)).Species.Rarity));
        Assert.Contains(Enumerable.Range(0, 30_000).Select(i => CatchRules.Roll(Config, Map("map_05"), Rod("rod_02"), 1, Rng.For(8, 8, i)).Species), s => s.Rarity == "epic");
    }

    [Fact]
    public void Legendary_is_above_epic_and_counts_as_valuable_everywhere()
    {
        Assert.True(Config.RarityRank("legendary") > Config.RarityRank("epic"));
        Assert.Contains("legendary", Config.Economy.FishingBox.BulkSaleProtection.Rarities);
        Assert.Equal(0.14, CatchRules.SuccessChance(Config, "legendary", 0), 6);
    }

    [Fact]
    public void The_encyclopedia_lists_every_species()
    {
        var game = Player(50, "rod_01");
        Assert.Equal(100, game.Profile.GetProfile().Encyclopedia.Count);
    }
}
