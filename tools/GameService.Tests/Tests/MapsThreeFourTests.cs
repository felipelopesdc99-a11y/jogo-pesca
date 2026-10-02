using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>Maps 3 and 4, the Épico rarity and Vara 2 (docs/PROGRESSAO_MAPAS_3_4.md, section 21).</summary>
public sealed class MapsThreeFourTests
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
    public void Map_3_opens_at_level_20_for_rod_1_and_never_for_the_starter_rod()
    {
        Assert.Equal(ServiceError.MapLocked, Player(19, "rod_01").Maps.TravelTo("map_03").Error);
        Assert.Equal(ServiceError.RodTooWeakForMap, Player(25).Maps.TravelTo("map_03").Error);

        var game = Player(20, "rod_01");
        Assert.True(game.Maps.GetMaps().Maps.Single(m => m.MapId == "map_03").LevelUnlocked);
        Assert.True(game.Maps.TravelTo("map_03").Succeeded);
    }

    [Fact]
    public void Map_4_needs_level_30_and_rod_2()
    {
        Assert.Equal(ServiceError.MapLocked, Player(29, "rod_01").Maps.TravelTo("map_04").Error);
        Assert.Equal(ServiceError.RodTooWeakForMap, Player(30, "rod_01").Maps.TravelTo("map_04").Error);
        Assert.Equal(ServiceError.RodLocked, Player(29).Shop.BuyRod("rod_02").Error);

        var game = Player(30, "rod_01", "rod_02");
        Assert.Equal(10_000_000 - 2_500 - 90_000, game.Session.Save.Coins);
        Assert.True(game.Maps.TravelTo("map_04").Succeeded);
    }

    [Fact]
    public void Rod_1_pulls_the_barbado_out_of_map_3_but_map_2_still_has_no_epic()
    {
        var rod = Rod("rod_01");
        var map3 = Enumerable.Range(0, 20_000).Select(i => CatchRules.Roll(Config, Map("map_03"), rod, 10, Rng.For(3, 3, i))).ToList();
        Assert.Contains(map3, c => c.Species.Id == "barbado");
        Assert.All(Enumerable.Range(0, 20_000), i => Assert.NotEqual("epic", CatchRules.Roll(Config, Map("map_02"), rod, 10, Rng.For(2, 2, i)).Species.Rarity));
        Assert.All(Enumerable.Range(0, 2_000), i => Assert.Equal("common", CatchRules.Roll(Config, Map("map_03"), Config.StarterRod, 1, Rng.For(4, 4, i)).Species.Rarity));
    }

    [Fact]
    public void Rod_2_catches_common_rare_and_both_epics_on_map_4()
    {
        var rolls = Enumerable.Range(0, 30_000).Select(i => CatchRules.Roll(Config, Map("map_04"), Rod("rod_02"), 1, Rng.For(5, 5, i)).Species).ToList();
        Assert.Contains(rolls, s => s.Rarity == "common");
        Assert.Contains(rolls, s => s.Rarity == "rare");
        Assert.Contains(rolls, s => s.Id == "camurupim");
        Assert.Contains(rolls, s => s.Id == "mero");
    }

    [Theory]
    [InlineData("map_03", 0.952, 0.045, 0.003, 47.9, 464, 1.75)]
    [InlineData("map_04", 0.934, 0.060, 0.006, 75.7, 1008, 1.55)]
    public void Bites_without_rod_bonus_hit_the_targets_of_the_document(string mapId, double common, double rare, double epic, double xpTarget, double coinsTarget, double scale)
    {
        // The document's XP and coin targets are for its own base values; the catalog scales them for
        // Catch Success (A-093): 1.75 on map 3, 1.55 on map 4.
        const int n = 200_000;
        var rng = new Rng(2026);
        var rolls = Enumerable.Range(0, n).Select(_ => CatchRules.RollFound(Config, Map(mapId), rng)).ToList();
        double Share(string r) => rolls.Count(c => c.Species.Rarity == r) / (double)n;

        Assert.InRange(Share("common"), common - 0.004, common + 0.004);
        Assert.InRange(Share("rare"), rare - 0.003, rare + 0.003);
        Assert.InRange(Share("epic"), epic * 0.7, epic * 1.3);
        Assert.InRange(rolls.Average(c => c.FisherXp) / scale, xpTarget * 0.95, xpTarget * 1.05);
        var saleMultiplier = Config.Economy.NpcFishSale.PriceMultiplier; // general sale knob, applied after the document's targets
        Assert.InRange(rolls.Average(c => CatchRules.SalePrice(Config, c.Species, c.SizeMm)) / scale / saleMultiplier, coinsTarget * 0.95, coinsTarget * 1.05);
    }

    [Fact]
    public void Epic_is_the_top_rarity_and_counts_as_valuable_everywhere()
    {
        Assert.True(Config.RarityRank("epic") > Config.RarityRank("rare"));
        Assert.Contains("epic", Config.Economy.FishingBox.BulkSaleProtection.Rarities);
        Assert.Equal(0.24, CatchRules.SuccessChance(Config, "epic", 0), 6);

        Config.TryGetSpecies("barbado", out var barbado);
        Config.TryGetRarity("epic", out var epic);
        var stats = FishRules.Stats(Config, barbado, (int)((barbado.SizeCm.Min + barbado.SizeCm.Max) * 5), 1);
        Assert.Equal(barbado.BaseStats.Attack * epic.StatMultiplier, stats.Attack, 6);
    }

    [Fact]
    public void An_epic_fish_can_be_kept_fed_and_sold_like_any_other()
    {
        var game = Player(30, "rod_01", "rod_02");
        var save = game.Session.Save;
        foreach (var species in new[] { "mero", "camurupim", "tainha" })
        {
            save.FishingBox.Add(new FishingIdle.GameService.Persistence.BoxCatch
            {
                Id = save.NextCatchId++, SpeciesId = species, SizeMm = 1500, SizeCategoryId = "adult", CaughtAtMs = TestSupport.StartMs,
            });
        }

        var box = game.Fishing.GetFishingBox();
        Assert.Equal(2, game.Fishing.PreviewSale(box.Select(c => c.CatchId).ToList()).ProtectedCatches.Count);
        Assert.True(game.Aquarium.KeepCatches(box.Where(c => c.SpeciesId != "tainha").Select(c => c.CatchId).ToList()).Succeeded);
        var aquarium = game.Aquarium.GetAquarium(FishingIdle.GameService.Aquarium.AquariumSort.Newest).Fish;
        var mero = aquarium.Single(f => f.SpeciesId == "mero");
        var camurupim = aquarium.Single(f => f.SpeciesId == "camurupim");
        Assert.True(game.Aquarium.Feed(mero.FishId, Array.Empty<long>(), new[] { camurupim.FishId }).Succeeded);
        Assert.True(game.Fishing.SellCatches(game.Fishing.GetFishingBox().Select(c => c.CatchId).ToList()).Succeeded);
    }

    [Fact]
    public void An_old_save_at_level_35_sees_both_maps_unlocked_without_moving()
    {
        var game = Player(35, "rod_01");
        var maps = game.Maps.GetMaps().Maps;
        Assert.True(maps.Single(m => m.MapId == "map_03").LevelUnlocked);
        Assert.True(maps.Single(m => m.MapId == "map_04").LevelUnlocked);
        Assert.Equal("map_01", game.Player.GetPlayer().MapId);
        Assert.Equal(40, game.Profile.GetProfile().Encyclopedia.Count);
    }
}
