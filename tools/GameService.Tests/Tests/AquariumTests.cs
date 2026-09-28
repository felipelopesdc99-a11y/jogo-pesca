using FishingIdle.GameService.Aquarium;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;
using FishingIdle.GameService.Persistence;
using Xunit;

namespace FishingIdle.GameService.Tests;

public sealed class AquariumTests
{
    /// <summary>A game with some catches already in the Fishing Box.</summary>
    private static (LocalGame Game, ManualClock Clock, string SaveDir) GameWithCatches(double seconds = 600, GameConfig config = null)
    {
        var (game, clock, dir) = TestSupport.NewGame(config);
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, seconds, stepSeconds: 30);
        game.Fishing.StopFishing();
        return (game, clock, dir);
    }

    private static List<long> BoxIds(LocalGame game, int count) => game.Fishing.GetFishingBox().Take(count).Select(c => c.CatchId).ToList();

    [Fact]
    public void Keeping_turns_catches_into_fish_and_removes_them_from_the_box()
    {
        var (game, _, _) = GameWithCatches();
        var boxBefore = game.Fishing.GetFishingBox();
        var ids = boxBefore.Take(3).Select(c => c.CatchId).ToList();

        var result = game.Aquarium.KeepCatches(ids);

        Assert.True(result.Succeeded);
        Assert.Equal(3, result.Value.Kept.Count);
        Assert.Equal(boxBefore.Count - 3, game.Fishing.GetFishingBox().Count);
        var aquarium = game.Aquarium.GetAquarium(AquariumSort.Newest);
        Assert.Equal(3, aquarium.Count);
        Assert.Equal(100, aquarium.Capacity);
        Assert.All(aquarium.Fish, f => Assert.Equal(1, f.Level));
        // Same species and exact size as the catch it came from.
        var kept = boxBefore.Take(3).OrderBy(c => c.CatchId).Select(c => (c.SpeciesId, c.SizeCm)).ToList();
        Assert.Equal(kept, aquarium.Fish.OrderBy(f => f.FishId).Select(f => (f.SpeciesId, f.SizeCm)).ToList());
    }

    [Fact]
    public void Keeping_the_same_catch_twice_is_refused()
    {
        var (game, _, _) = GameWithCatches();
        var ids = BoxIds(game, 1);

        Assert.True(game.Aquarium.KeepCatches(ids).Succeeded);
        var again = game.Aquarium.KeepCatches(ids);

        Assert.False(again.Succeeded);
        Assert.Equal(ServiceError.CatchNotFound, again.Error);
        Assert.Equal(1, game.Aquarium.GetAquarium(AquariumSort.Size).Count);
    }

    [Fact]
    public void The_aquarium_never_goes_past_its_capacity_and_fishing_continues()
    {
        var small = TestSupport.ConfigWith(GameConfigLoader.EconomyFile, j => j.Replace("\"hard_capacity\": 100", "\"hard_capacity\": 3")).Config;
        var (game, clock, _) = GameWithCatches(config: small);

        Assert.True(game.Aquarium.KeepCatches(BoxIds(game, 3)).Succeeded);
        var overflow = game.Aquarium.KeepCatches(BoxIds(game, 1));

        Assert.False(overflow.Succeeded);
        Assert.Equal(ServiceError.AquariumFull, overflow.Error);
        Assert.Equal(3, game.Aquarium.GetAquarium(AquariumSort.Size).Count);

        game.Fishing.StartFishing();
        Assert.NotEmpty(TestSupport.PlayFor(game, clock, 60));
    }

    [Fact]
    public void Keeping_more_than_the_free_slots_keeps_nothing()
    {
        var small = TestSupport.ConfigWith(GameConfigLoader.EconomyFile, j => j.Replace("\"hard_capacity\": 100", "\"hard_capacity\": 2")).Config;
        var (game, _, _) = GameWithCatches(config: small);
        var boxCount = game.Fishing.GetFishingBox().Count;

        var result = game.Aquarium.KeepCatches(BoxIds(game, 3));

        Assert.False(result.Succeeded);
        Assert.Equal(0, game.Aquarium.GetAquarium(AquariumSort.Size).Count);
        Assert.Equal(boxCount, game.Fishing.GetFishingBox().Count);
    }

    [Fact]
    public void Same_species_rarity_size_and_level_always_give_the_same_stats()
    {
        var config = TestSupport.RealConfig();
        config.TryGetSpecies("tilapia", out var tilapia);

        var a = FishRules.Stats(config, tilapia, 420, 3);
        var b = FishRules.Stats(config, tilapia, 420, 3);
        var bigger = FishRules.Stats(config, tilapia, 580, 3);
        var higher = FishRules.Stats(config, tilapia, 420, 10);

        Assert.Equal((a.Hp, a.Attack, a.Defense, a.Speed), (b.Hp, b.Attack, b.Defense, b.Speed));
        Assert.True(bigger.Attack > a.Attack);
        // Level 10 = +36% (4% per level) over level 1.
        var level1 = FishRules.Stats(config, tilapia, 420, 1);
        Assert.Equal(level1.Hp * 1.36, higher.Hp, 6);
    }

    [Fact]
    public void Rare_fish_are_stronger_than_a_common_fish_with_the_same_base()
    {
        var config = TestSupport.RealConfig();
        config.TryGetSpecies("aruana", out var aruana);
        config.TryGetRarity("rare", out var rare);

        var stats = FishRules.Stats(config, aruana, (int)((aruana.SizeCm.Min + aruana.SizeCm.Max) * 5), 1);

        Assert.Equal(aruana.BaseStats.Attack * rare.StatMultiplier, stats.Attack, 6);
    }

    [Fact]
    public void Feeding_levels_the_fish_up_and_consumes_the_food()
    {
        var (game, _, _) = GameWithCatches(1800);
        var target = game.Aquarium.KeepCatches(BoxIds(game, 1)).Value.Kept[0];
        var food = BoxIds(game, 6);
        var expectedXp = game.Fishing.GetFishingBox().Where(c => food.Contains(c.CatchId)).Sum(c => c.FeedXp);
        var boxBefore = game.Fishing.GetFishingBox().Count;

        var preview = game.Aquarium.PreviewFeed(target.FishId, food, null);
        var fed = game.Aquarium.Feed(target.FishId, food, null);

        Assert.True(fed.Succeeded);
        Assert.Equal(expectedXp, fed.Value.XpGained);
        Assert.Equal(preview.Value.LevelAfter, fed.Value.LevelAfter);
        var after = game.Aquarium.GetFish(target.FishId);
        Assert.Equal(fed.Value.LevelAfter, after.Level);
        Assert.True(after.Level > 1);
        Assert.Equal(expectedXp, after.InvestedXp);
        Assert.Equal(boxBefore - 6, game.Fishing.GetFishingBox().Count);
        Assert.False(game.Aquarium.Feed(target.FishId, food, null).Succeeded);
    }

    [Fact]
    public void Consuming_a_leveled_fish_returns_half_of_the_invested_xp()
    {
        var (game, _, _) = GameWithCatches(1800);
        var kept = game.Aquarium.KeepCatches(BoxIds(game, 2)).Value.Kept;
        var leveled = kept[0];
        var target = kept[1];
        game.Aquarium.Feed(leveled.FishId, BoxIds(game, 5), null);
        var invested = game.Aquarium.GetFish(leveled.FishId).InvestedXp;
        var config = TestSupport.RealConfig();
        config.TryGetSpecies(leveled.SpeciesId, out var species);
        var own = FishRules.FeedValue(config, species, (int)Math.Round(leveled.SizeCm * 10), 0);

        var preview = game.Aquarium.PreviewFeed(target.FishId, null, new[] { leveled.FishId });

        Assert.Equal(own + invested / 2, preview.Value.XpGained);
        Assert.True(preview.Value.XpGained < invested + own); // never lossless
    }

    [Fact]
    public void A_fish_cannot_eat_itself_and_a_max_level_fish_cannot_eat()
    {
        var (game, _, _) = GameWithCatches(3600);
        var target = game.Aquarium.KeepCatches(BoxIds(game, 1)).Value.Kept[0];

        var self = game.Aquarium.PreviewFeed(target.FishId, null, new[] { target.FishId });
        Assert.Equal(ServiceError.CannotFeedItself, self.Error);

        var all = game.Fishing.GetFishingBox().Select(c => c.CatchId).ToList();
        var feast = game.Aquarium.Feed(target.FishId, all, null);
        Assert.True(feast.Succeeded);
        Assert.Equal(10, feast.Value.LevelAfter);
        Assert.True(feast.Value.WastedXp > 0);
        Assert.True(feast.Value.NeedsConfirmation);

        game.Fishing.StartFishing();
        var more = game.Aquarium.PreviewFeed(target.FishId, BoxIds(game, 1), null);
        Assert.False(more.Succeeded);
    }

    [Fact]
    public void Leveled_or_exceptional_food_asks_for_confirmation()
    {
        var (game, _, _) = GameWithCatches(1800);
        var kept = game.Aquarium.KeepCatches(BoxIds(game, 2)).Value.Kept;
        game.Aquarium.Feed(kept[0].FishId, BoxIds(game, 5), null);

        var preview = game.Aquarium.PreviewFeed(kept[1].FishId, null, new[] { kept[0].FishId });

        Assert.True(game.Aquarium.GetFish(kept[0].FishId).Level >= 2);
        Assert.Single(preview.Value.ValuableFood);
        Assert.True(preview.Value.NeedsConfirmation);
    }

    [Fact]
    public void Selling_an_aquarium_fish_pays_the_normal_price_without_xp_refund()
    {
        var (game, _, _) = GameWithCatches(1800);
        var fish = game.Aquarium.KeepCatches(BoxIds(game, 1)).Value.Kept[0];
        var basePrice = fish.SalePriceCoins;
        game.Aquarium.Feed(fish.FishId, BoxIds(game, 5), null);
        var coins = game.Player.GetPlayer().Coins;

        var sale = game.Aquarium.SellFish(new[] { fish.FishId });

        Assert.True(sale.Succeeded);
        Assert.Equal(basePrice, sale.Value.CoinsGained);
        Assert.Equal(coins + basePrice, game.Player.GetPlayer().Coins);
        Assert.Null(game.Aquarium.GetFish(fish.FishId));
        Assert.False(game.Aquarium.SellFish(new[] { fish.FishId }).Succeeded);
    }

    [Fact]
    public void Default_order_is_exceptional_large_adult_small()
    {
        var (game, _, _) = GameWithCatches(6 * 3600);
        var ids = game.Fishing.GetFishingBox().Take(100).Select(c => c.CatchId).ToList();
        game.Aquarium.KeepCatches(ids);
        var order = new[] { "exceptional", "large", "adult", "small" };

        var ranks = game.Aquarium.GetAquarium(AquariumSort.Size).Fish.Select(f => Array.IndexOf(order, f.SizeCategoryId)).ToList();

        Assert.Equal(ranks.OrderBy(r => r), ranks);
    }

    [Fact]
    public void Aquarium_fish_survive_a_restart_and_an_old_save_is_upgraded()
    {
        var (game, clock, dir) = GameWithCatches();
        var kept = game.Aquarium.KeepCatches(BoxIds(game, 2)).Value.Kept;
        game.Aquarium.Feed(kept[0].FishId, null, new[] { kept[1].FishId });
        var before = game.Aquarium.GetFish(kept[0].FishId);

        var (reopened, _, _) = TestSupport.NewGame(saveDir: dir, clock: clock);
        var after = reopened.Aquarium.GetFish(kept[0].FishId);

        Assert.Equal((before.Level, before.Xp, before.InvestedXp, before.SizeCm), (after.Level, after.Xp, after.InvestedXp, after.SizeCm));

        // A save written before the Aquarium existed (version 1) loads with an empty Aquarium.
        var path = Path.Combine(dir, JsonFilePlayerRepository.SaveFileName);
        var json = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(path));
        json["save_version"] = 1;
        json.Remove("aquarium");
        json.Remove("next_fish_id");
        File.WriteAllText(path, json.ToString());
        var (upgraded, _, _) = TestSupport.NewGame(saveDir: dir, clock: clock);

        Assert.Equal(SaveLoadStatus.Loaded, upgraded.Session.LoadStatus);
        Assert.Equal(0, upgraded.Aquarium.GetAquarium(AquariumSort.Size).Count);
        Assert.Contains("\"save_version\": 2", File.ReadAllText(path));
    }
}
