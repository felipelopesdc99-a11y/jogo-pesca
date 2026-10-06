using FishingIdle.GameService.Aquarium;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Market;
using FishingIdle.GameService.Persistence;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>
/// Milestone 11: things a player could try on their own PC to get more than the rules give —
/// clock tricks, repeated requests, using the same fish twice, editing the save.
/// </summary>
public sealed class AbuseTests
{
    private static (LocalGame Game, ManualClock Clock, string Dir) Player(GameConfig config = null, int keep = 6)
    {
        var (game, clock, dir) = TestSupport.NewGame(config);
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, 900, stepSeconds: 30);
        game.Fishing.StopFishing();
        game.Aquarium.KeepCatches(game.Fishing.GetFishingBox().Select(c => c.CatchId).Take(keep).ToList());
        game.Session.Save.Coins = 1_000_000;
        return (game, clock, dir);
    }

    [Fact]
    public void Turning_the_clock_back_and_forward_again_gives_no_extra_catches()
    {
        var (game, clock, _) = TestSupport.NewGame();
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, 300, stepSeconds: 5);
        var catches = game.Player.GetPlayer().TotalCatches;

        clock.AdvanceSeconds(-3600);
        Assert.Empty(game.Fishing.Sync().NewCatches);
        clock.AdvanceSeconds(3600);
        Assert.Empty(game.Fishing.Sync().NewCatches);

        Assert.Equal(catches, game.Player.GetPlayer().TotalCatches);
    }

    [Fact]
    public void Turning_the_clock_back_restarting_and_turning_it_forward_gives_nothing()
    {
        // TD-030: stopping and starting (or reopening the game) while the clock is turned back used to
        // re-anchor the run at the earlier time, so turning the clock forward again paid a full day.
        var (game, clock, _) = TestSupport.NewGame();
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, 300, stepSeconds: 5);
        var catches = game.Player.GetPlayer().TotalCatches;

        clock.AdvanceSeconds(-24 * 3600);
        game.Fishing.StopFishing();
        game.Fishing.StartFishing();
        clock.AdvanceSeconds(24 * 3600);
        game.Fishing.Sync();

        Assert.InRange(game.Player.GetPlayer().TotalCatches - catches, 0, 1);
    }

    [Fact]
    public void A_rod_on_the_market_still_counts_as_owned_and_bots_never_pay_more_than_a_new_one()
    {
        // Buying a rod in the Shop and reselling it to the simulated buyers above the Shop price used to
        // create coins, and a listed rod could be bought again right away.
        var (game, _, _) = Player();
        game.Session.Save.FisherLevel = 10;
        game.Session.Save.Shells = 1_000;
        Assert.True(game.Shop.BuyRod("rod_01").Succeeded);
        var inventory = game.Profile.GetProfile().Inventory;
        Assert.True(game.Profile.EquipRod(inventory.Single(r => r.RodId != "rod_01").ItemId).Succeeded);
        var rod = inventory.Single(r => r.RodId == "rod_01");

        var listed = game.Market.ListRod(rod.ItemId, 4_000);
        Assert.True(listed.Succeeded, listed.ErrorMessage);
        Assert.Equal(ServiceError.RodAlreadyOwned, game.Shop.BuyRod("rod_01").Error);

        var goods = game.Session.Save.Market.MyListings.Single().Goods;
        Assert.Equal(2_500, MarketRules.MaxBotPrice(game.Session.Config, goods));
    }

    [Fact]
    public void Jumping_the_clock_years_ahead_is_capped_everywhere()
    {
        var never = TestSupport.ConfigWith(GameConfigLoader.MarketBotsFile, j => j.Replace("\"chance_at_reference_price\": 0.25", "\"chance_at_reference_price\": 0.0")).Config;
        var (game, clock, dir) = Player(never);
        game.Fishing.StartFishing();
        game.Fishing.MarkSeen();
        var box = game.Fishing.GetFishingBox().Count;
        var catches = game.Player.GetPlayer().TotalCatches;

        clock.AdvanceSeconds(3 * 365 * 86400.0);
        var reopened = TestSupport.NewGame(never, dir, clock).Game;
        reopened.Market.Update();
        reopened.Arena.Update();

        var offline = reopened.Game_OfflineCatches(catches);
        var cap = (long)(TestSupport.RealConfig().Progression.Fishing.OfflineAccumulationCapHours * 3600 / TestSupport.RealConfig().Progression.Fishing.OfflineCycleSeconds);
        Assert.InRange(offline, 1, cap);
        Assert.True(reopened.Fishing.GetFishingBox().Count <= box + cap);
        Assert.True(reopened.Arena.GetArena().Energy <= TestSupport.RealConfig().Arena.Energy.Max);
        Assert.True(reopened.Arena.GetArena().History.Count <= 30);
    }

    [Fact]
    public void Market_checks_are_never_run_twice_after_the_clock_goes_back()
    {
        var (game, clock, _) = Player();
        var fish = game.Aquarium.GetAquarium(AquariumSort.Newest).Fish.Last();
        var listed = game.Market.ListFish(fish.FishId, fish.SalePriceCoins * 100); // far above what anyone pays
        Assert.True(listed.Succeeded);

        clock.AdvanceSeconds(2 * 3600);
        game.Market.Update();
        var coins = game.Player.GetPlayer().Coins;
        clock.AdvanceSeconds(-2 * 3600);
        game.Market.Update();
        clock.AdvanceSeconds(2 * 3600);
        game.Market.Update();

        Assert.Equal(coins, game.Player.GetPlayer().Coins);
        Assert.Single(game.Market.GetMarket().MyListings);
    }

    [Fact]
    public void A_fish_is_only_ever_in_one_place()
    {
        var (game, _, _) = Player();
        var ids = game.Aquarium.GetAquarium(AquariumSort.Newest).Fish.Select(f => f.FishId).ToList();
        var target = ids[0];
        var listed = ids[1];
        var auctioned = ids[2];

        Assert.True(game.Market.ListFish(listed, 500).Succeeded);
        Assert.True(game.Auctions.StartAuction(true, auctioned, 500).Succeeded);

        // Neither can be eaten, sold to the game, put in the Cardume or put on the Market again.
        foreach (var id in new[] { listed, auctioned })
        {
            Assert.Equal(ServiceError.FishNotFound, game.Aquarium.Feed(target, Array.Empty<long>(), new[] { id }).Error);
            Assert.Equal(ServiceError.FishNotFound, game.Aquarium.SellFish(new[] { id }).Error);
            Assert.Equal(ServiceError.FishNotFound, game.Cardume.SetSlot(1, id).Error);
            Assert.Equal(ServiceError.FishNotFound, game.Market.ListFish(id, 100).Error);
            Assert.False(game.Auctions.StartAuction(true, id, 100).Succeeded);
        }
    }

    [Fact]
    public void The_cardume_on_an_expedition_cannot_be_listed_or_auctioned()
    {
        var (game, _, _) = Player();
        var fishId = game.Aquarium.GetAquarium(AquariumSort.Newest).Fish.First().FishId;
        game.Cardume.SetSlot(1, fishId);
        var expedition = game.Expeditions.GetExpeditions().Expeditions.First();
        Assert.True(game.Expeditions.Start(expedition.ExpeditionId).Succeeded);

        Assert.Equal(ServiceError.CardumeLocked, game.Market.ListFish(fishId, 100).Error);
        Assert.Equal(ServiceError.CardumeLocked, game.Auctions.StartAuction(true, fishId, 100).Error);
        Assert.NotNull(game.Aquarium.GetFish(fishId));
    }

    [Fact]
    public void Repeated_requests_never_pay_or_charge_twice()
    {
        var (game, _, _) = Player();
        var box = game.Fishing.GetFishingBox().Select(c => c.CatchId).Take(2).ToList();
        var coins = game.Player.GetPlayer().Coins;

        var sold = game.Fishing.SellCatches(box);
        var again = game.Fishing.SellCatches(box);

        Assert.True(sold.Succeeded);
        Assert.False(again.Succeeded);
        Assert.Equal(coins + sold.Value.CoinsGained, game.Player.GetPlayer().Coins);

        var fish = game.Aquarium.GetAquarium(AquariumSort.Newest).Fish.Last().FishId;
        var first = game.Aquarium.SellFish(new[] { fish });
        Assert.True(first.Succeeded);
        Assert.Equal(ServiceError.FishNotFound, game.Aquarium.SellFish(new[] { fish }).Error);
        Assert.Equal(coins + sold.Value.CoinsGained + first.Value.CoinsGained, game.Player.GetPlayer().Coins);
    }

    [Fact]
    public void Negative_or_zero_prices_and_bids_are_refused()
    {
        var (game, _, _) = Player();
        var fish = game.Aquarium.GetAquarium(AquariumSort.Newest).Fish.First().FishId;
        var auction = game.Auctions.GetAuctions().Open.First();

        Assert.Equal(ServiceError.InvalidPrice, game.Market.ListFish(fish, -5).Error);
        Assert.Equal(ServiceError.InvalidPrice, game.Auctions.StartAuction(true, fish, 0).Error);
        Assert.Equal(ServiceError.BidTooLow, game.Auctions.PlaceBid(auction.AuctionId, -1_000_000).Error);
        Assert.Equal(1_000_000, game.Player.GetPlayer().Coins);
    }

    [Fact]
    public void A_save_with_every_system_in_use_reopens_identically()
    {
        var (game, clock, dir) = Player(keep: 8);
        var ids = game.Aquarium.GetAquarium(AquariumSort.Newest).Fish.Select(f => f.FishId).ToList();
        game.Cardume.SetSlot(1, ids[0]);
        game.Cardume.SetSlot(2, ids[1]);
        game.Aquarium.Feed(ids[0], Array.Empty<long>(), new[] { ids[2] });
        game.Arena.Attack(0);
        game.Market.ListFish(ids[3], 12345);
        game.Market.Buy(game.Market.Search(new MarketQuery()).First().ListingId);
        game.Auctions.StartAuction(true, ids[4], 50);
        var open = game.Auctions.GetAuctions().Open.First();
        game.Auctions.PlaceBid(open.AuctionId, open.MinNextBidCoins);
        game.Expeditions.Start(game.Expeditions.GetExpeditions().Expeditions.First().ExpeditionId);
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, 60, stepSeconds: 30);
        game.Fishing.MarkSeen();
        var path = Path.Combine(dir, JsonFilePlayerRepository.SaveFileName);
        var before = File.ReadAllText(path);

        var reopened = TestSupport.NewGame(null, dir, clock).Game;

        Assert.Equal(SaveLoadStatus.Loaded, reopened.Session.LoadStatus);

        // Reopening starts a new fishing run (by design, after any gap); everything else is unchanged.
        static string WithoutFishingRun(string text)
        {
            var json = JObject.Parse(text);
            json.Remove("fishing");
            return json.ToString();
        }

        Assert.Equal(WithoutFishingRun(before), WithoutFishingRun(File.ReadAllText(path)));
        Assert.Equal(game.Player.GetPlayer().Coins, reopened.Player.GetPlayer().Coins);
        Assert.Equal(game.Aquarium.GetAquarium(AquariumSort.Newest).Fish.Select(f => (f.FishId, f.Level, f.Xp)), reopened.Aquarium.GetAquarium(AquariumSort.Newest).Fish.Select(f => (f.FishId, f.Level, f.Xp)));
    }

    [Fact]
    public void A_hand_edited_save_with_negative_coins_is_not_trusted()
    {
        var (game, clock, dir) = Player();
        var path = Path.Combine(dir, JsonFilePlayerRepository.SaveFileName);
        var json = JObject.Parse(File.ReadAllText(path));
        json["coins"] = -999;
        File.WriteAllText(path, json.ToString());

        var reopened = TestSupport.NewGame(null, dir, clock).Game;

        Assert.True(reopened.Player.GetPlayer().Coins >= 0);
        Assert.NotEqual(SaveLoadStatus.Loaded, reopened.Session.LoadStatus);
    }
}

internal static class AbuseTestExtensions
{
    /// <summary>Catches credited since <paramref name="before"/> (total catches is monotonic).</summary>
    public static long Game_OfflineCatches(this LocalGame game, long before) => game.Player.GetPlayer().TotalCatches - before;
}
