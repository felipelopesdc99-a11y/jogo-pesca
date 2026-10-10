using FishingIdle.GameService.Aquarium;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Market;
using FishingIdle.GameService.Persistence;
using Xunit;

namespace FishingIdle.GameService.Tests;

public sealed class MarketTests
{
    /// <summary>A player with a few Aquarium fish and plenty of Coins.</summary>
    private static (LocalGame Game, ManualClock Clock, string Dir) Trader(GameConfig config = null, int keep = 5, string dir = null)
    {
        var (game, clock, saveDir) = TestSupport.NewGame(config, dir);
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, 600, stepSeconds: 30);
        game.Fishing.StopFishing();
        var box = game.Fishing.GetFishingBox().Select(c => c.CatchId).ToList();
        game.Aquarium.KeepCatches(box.Take(keep).ToList());
        game.Session.Save.Coins = 1_000_000;
        return (game, clock, saveDir);
    }

    private static GameConfig MarketConfig(Func<string, string> edit) => TestSupport.ConfigWith(GameConfigLoader.MarketBotsFile, edit).Config;

    private static long FirstFish(LocalGame game) => game.Aquarium.GetAquarium(AquariumSort.Newest).Fish.Last().FishId;

    [Fact]
    public void The_simulated_market_has_listings_from_others_priced_above_what_the_game_pays()
    {
        var (game, _, _) = Trader();

        var listings = game.Market.Search(new MarketQuery());

        Assert.Equal(TestSupport.RealConfig().MarketBots.Supply.TargetListingCount, listings.Count);
        Assert.All(listings, l =>
        {
            Assert.False(l.IsMine);
            Assert.False(string.IsNullOrWhiteSpace(l.SellerName));
            Assert.True(l.PriceCoins > l.Goods.NpcValueCoins);
            Assert.True(l.RemainingSeconds > 0);
        });
        Assert.Equal(listings.Select(l => l.PriceCoins).OrderBy(p => p), listings.Select(l => l.PriceCoins));
    }

    [Fact]
    public void Filters_combine_and_sorts_apply()
    {
        var (game, _, _) = Trader();
        var all = game.Market.Search(new MarketQuery { Kind = MarketKindFilter.Fish });
        var species = all.First().Goods.Fish.SpeciesId;

        var filtered = game.Market.Search(new MarketQuery { Kind = MarketKindFilter.Fish, SpeciesId = species, MaxPrice = 1_000_000, Sort = MarketSort.PriceDescending });

        Assert.NotEmpty(filtered);
        Assert.All(filtered, l => Assert.Equal(species, l.Goods.Fish.SpeciesId));
        Assert.Equal(filtered.Select(l => l.PriceCoins).OrderByDescending(p => p), filtered.Select(l => l.PriceCoins));
        Assert.Empty(game.Market.Search(new MarketQuery { MaxPrice = 0 }));
        Assert.All(game.Market.Search(new MarketQuery { Kind = MarketKindFilter.Rods }), l => Assert.False(l.Goods.IsFish));
    }

    [Fact]
    public void Buying_pays_the_price_puts_the_goods_in_withdrawal_and_cannot_happen_twice()
    {
        var (game, _, _) = Trader();
        var listing = game.Market.Search(new MarketQuery { Kind = MarketKindFilter.Fish }).First();
        var coins = game.Player.GetPlayer().Coins;
        var aquarium = game.Aquarium.GetAquarium(AquariumSort.Size).Count;

        var bought = game.Market.Buy(listing.ListingId);
        var again = game.Market.Buy(listing.ListingId);

        Assert.True(bought.Succeeded, bought.ErrorMessage);
        Assert.Equal(ServiceError.ListingNotFound, again.Error);
        Assert.Equal(coins - listing.PriceCoins, game.Player.GetPlayer().Coins);
        Assert.Equal(aquarium, game.Aquarium.GetAquarium(AquariumSort.Size).Count); // nothing goes straight to the Aquarium
        var entry = Assert.Single(game.Market.GetMarket().Withdrawals);
        Assert.Equal(WithdrawalItem.ReasonBought, entry.Reason);
        Assert.DoesNotContain(game.Market.Search(new MarketQuery()), l => l.ListingId == listing.ListingId);
    }

    [Fact]
    public void Buying_without_enough_coins_is_refused()
    {
        var (game, _, _) = Trader();
        game.Session.Save.Coins = 0;
        var listing = game.Market.Search(new MarketQuery()).First();

        Assert.Equal(ServiceError.NotEnoughCoins, game.Market.Buy(listing.ListingId).Error);
        Assert.Empty(game.Market.GetMarket().Withdrawals);
    }

    [Fact]
    public void A_full_aquarium_allows_buying_but_blocks_withdrawing_a_fish()
    {
        var small = TestSupport.ConfigWith(GameConfigLoader.EconomyFile, j => j.Replace("\"hard_capacity\": 100", "\"hard_capacity\": 3")).Config;
        var (game, _, _) = Trader(small, keep: 3);
        var listing = game.Market.Search(new MarketQuery { Kind = MarketKindFilter.Fish }).First();

        var bought = game.Market.Buy(listing.ListingId);
        Assert.True(bought.Succeeded, bought.ErrorMessage);
        Assert.Equal(ServiceError.AquariumFull, bought.Value.Item.Blocker);
        Assert.Equal(ServiceError.AquariumFull, game.Market.Withdraw(bought.Value.Item.WithdrawalId).Error);
        Assert.Equal(3, game.Aquarium.GetAquarium(AquariumSort.Size).Count);

        game.Aquarium.SellFish(new[] { FirstFish(game) });
        var withdrawn = game.Market.Withdraw(bought.Value.Item.WithdrawalId);

        Assert.True(withdrawn.Succeeded, withdrawn.ErrorMessage);
        Assert.Equal(3, game.Aquarium.GetAquarium(AquariumSort.Size).Count);
        var fish = game.Aquarium.GetAquarium(AquariumSort.Newest).Fish.First();
        Assert.Equal(listing.Goods.Fish.SpeciesId, fish.SpeciesId);
        Assert.Equal(listing.Goods.Fish.SizeCm, fish.SizeCm);
        Assert.Equal(listing.Goods.Fish.Level, fish.Level);
        Assert.Empty(game.Market.GetMarket().Withdrawals);
    }

    [Fact]
    public void Listing_a_fish_takes_it_out_of_the_aquarium_and_it_cannot_be_listed_twice()
    {
        var (game, _, _) = Trader();
        var fishId = FirstFish(game);
        var count = game.Aquarium.GetAquarium(AquariumSort.Size).Count;

        var listed = game.Market.ListFish(fishId, 500);
        var twice = game.Market.ListFish(fishId, 500);

        Assert.True(listed.Succeeded, listed.ErrorMessage);
        Assert.Equal(ServiceError.FishNotFound, twice.Error);
        Assert.Equal(count - 1, game.Aquarium.GetAquarium(AquariumSort.Size).Count);
        Assert.Null(game.Aquarium.GetFish(fishId));
        Assert.Equal(ServiceError.FishNotFound, game.Cardume.SetSlot(1, fishId).Error);
        Assert.Equal(ServiceError.FishNotFound, game.Aquarium.SellFish(new[] { fishId }).Error);
        var mine = Assert.Single(game.Market.GetMarket().MyListings);
        Assert.Equal(500, mine.PriceCoins);
        Assert.Equal(15, mine.FeeCoins);
        Assert.Equal(485, mine.NetCoins);
        Assert.DoesNotContain(game.Market.Search(new MarketQuery()), l => l.IsMine);
        Assert.Equal(ServiceError.OwnListing, game.Market.Buy(mine.ListingId).Error);
    }

    [Fact]
    public void Listing_rules_price_limit_and_cardume()
    {
        var (game, _, _) = Trader(keep: 7);
        var ids = game.Aquarium.GetAquarium(AquariumSort.Newest).Fish.Select(f => f.FishId).ToList();

        Assert.Equal(ServiceError.InvalidPrice, game.Market.ListFish(ids[0], 0).Error);
        game.Cardume.SetSlot(1, ids[0]);
        Assert.Equal(ServiceError.FishInCardume, game.Market.ListFish(ids[0], 100).Error);

        for (var i = 1; i <= 5; i++)
        {
            Assert.True(game.Market.ListFish(ids[i], 100 + i).Succeeded);
        }

        Assert.Equal(ServiceError.ListingLimitReached, game.Market.ListFish(ids[6], 100).Error);
        Assert.NotNull(game.Aquarium.GetFish(ids[6]));
    }

    [Fact]
    public void The_starter_rod_and_the_equipped_rod_cannot_be_listed()
    {
        var (game, _, _) = Trader();
        var starter = game.Profile.GetProfile().EquippedRod.ItemId;

        Assert.Equal(ServiceError.RodNotTradable, game.Market.ListRod(starter, 100).Error);
        Assert.Contains(game.Market.GetSellCandidates(), c => !c.IsFish && c.Blocker == ServiceError.RodNotTradable);
    }

    [Fact]
    public void Cancelling_sends_the_fish_to_withdrawal_with_all_its_data()
    {
        var (game, _, _) = Trader();
        var fish = game.Aquarium.GetAquarium(AquariumSort.Newest).Fish.Last();
        var listing = game.Market.ListFish(fish.FishId, 999).Value;

        var cancelled = game.Market.CancelListing(listing.ListingId);
        Assert.True(cancelled.Succeeded, cancelled.ErrorMessage);
        Assert.Equal(ServiceError.ListingNotFound, game.Market.CancelListing(listing.ListingId).Error);
        Assert.Null(game.Aquarium.GetFish(fish.FishId));
        Assert.Equal(WithdrawalItem.ReasonCancelled, cancelled.Value.Reason);

        Assert.True(game.Market.Withdraw(cancelled.Value.WithdrawalId).Succeeded);
        Assert.Equal(ServiceError.WithdrawalNotFound, game.Market.Withdraw(cancelled.Value.WithdrawalId).Error);
        var back = game.Aquarium.GetFish(fish.FishId);
        Assert.NotNull(back);
        Assert.Equal(fish.SizeCm, back.SizeCm);
        Assert.Equal(fish.Level, back.Level);
        Assert.Equal(fish.InvestedXp, back.InvestedXp);
    }

    [Fact]
    public void A_simulated_buyer_pays_the_price_minus_the_3_percent_fee()
    {
        var sure = MarketConfig(j => j.Replace("\"chance_at_reference_price\": 0.25", "\"chance_at_reference_price\": 1.0"));
        var (game, clock, _) = Trader(sure);
        var candidate = game.Market.GetSellCandidates().First(c => c.IsFish && c.Blocker == ServiceError.None);
        var price = candidate.Goods.ReferenceCoins;
        game.Market.ListFish(candidate.SourceId, price);
        var coins = game.Player.GetPlayer().Coins;

        clock.AdvanceSeconds(21 * 60);
        var news = game.Market.Update();

        var sale = Assert.Single(news);
        Assert.True(sale.Sold);
        var fee = (long)Math.Round(price * 0.03, MidpointRounding.AwayFromZero);
        Assert.Equal(fee, sale.FeeCoins);
        Assert.Equal(coins + price - fee, game.Player.GetPlayer().Coins);
        Assert.Empty(game.Market.GetMarket().MyListings);
        Assert.Empty(game.Market.GetMarket().Withdrawals); // the fish went to the buyer
        Assert.Empty(game.Market.Update()); // told once
    }

    [Fact]
    public void An_unsold_listing_expires_after_7_days_into_withdrawal_also_while_closed()
    {
        var never = MarketConfig(j => j.Replace("\"chance_at_reference_price\": 0.25", "\"chance_at_reference_price\": 0.0"));
        var (game, clock, dir) = Trader(never);
        var fishId = FirstFish(game);
        game.Market.ListFish(fishId, 50);

        clock.AdvanceSeconds(6.9 * 86400);
        Assert.Empty(game.Market.Update());
        Assert.Single(game.Market.GetMarket().MyListings);

        // Closed and reopened after the deadline.
        clock.AdvanceSeconds(0.2 * 86400);
        var reopened = TestSupport.NewGame(never, dir, clock).Game;
        var news = reopened.Market.Update();

        var expired = Assert.Single(news);
        Assert.False(expired.Sold);
        var market = reopened.Market.GetMarket();
        Assert.Empty(market.MyListings);
        Assert.Equal(WithdrawalItem.ReasonExpired, Assert.Single(market.Withdrawals).Reason);
        Assert.True(reopened.Market.WithdrawAll().Succeeded);
        Assert.NotNull(reopened.Aquarium.GetFish(fishId));
    }

    [Fact]
    public void A_bought_rod_keeps_its_level_and_cannot_be_resold_to_the_game_at_a_profit()
    {
        var rods = MarketConfig(j => j.Replace("\"rod_listing_chance\": 0.12", "\"rod_listing_chance\": 1.0"));
        var (game, _, _) = Trader(rods);
        var listing = game.Market.Search(new MarketQuery { Kind = MarketKindFilter.Rods, Sort = MarketSort.PriceAscending }).First();

        var bought = game.Market.Buy(listing.ListingId);
        Assert.True(bought.Succeeded, bought.ErrorMessage);
        Assert.True(game.Market.Withdraw(bought.Value.Item.WithdrawalId).Succeeded);

        var rod = game.Profile.GetProfile().Inventory.Single(r => r.RodId == listing.Goods.Rod.RodId);
        Assert.Equal(listing.Goods.Rod.Level, rod.Level);
        Assert.True(rod.ResaleValue < listing.PriceCoins);
        var ids = game.Session.Save.Inventory.Select(i => i.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void Listings_and_withdrawals_survive_a_restart()
    {
        var (game, clock, dir) = Trader();
        var fishId = FirstFish(game);
        game.Market.ListFish(fishId, 777);
        var bought = game.Market.Buy(game.Market.Search(new MarketQuery()).First().ListingId).Value;
        var offer = game.Market.Search(new MarketQuery()).Select(l => l.ListingId).ToList();

        var reopened = TestSupport.NewGame(null, dir, clock).Game;
        var market = reopened.Market.GetMarket();

        Assert.Equal(777, Assert.Single(market.MyListings).PriceCoins);
        Assert.Equal(bought.Item.WithdrawalId, Assert.Single(market.Withdrawals).WithdrawalId);
        Assert.Equal(offer, reopened.Market.Search(new MarketQuery()).Select(l => l.ListingId).ToList());
    }

    [Fact]
    public void The_bot_supply_refreshes_over_time_without_passing_the_target()
    {
        var (game, clock, _) = Trader();
        var first = game.Market.Search(new MarketQuery()).Select(l => l.ListingId).ToHashSet();

        clock.AdvanceSeconds(5 * 86400);
        var later = game.Market.Search(new MarketQuery());

        Assert.True(later.Count <= TestSupport.RealConfig().MarketBots.Supply.TargetListingCount);
        Assert.NotEmpty(later);
        Assert.DoesNotContain(later, l => first.Contains(l.ListingId)); // 48 h listings are long gone
    }
}
