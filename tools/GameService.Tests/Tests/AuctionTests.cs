using FishingIdle.GameService.Aquarium;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Market;
using FishingIdle.GameService.Persistence;
using Xunit;

namespace FishingIdle.GameService.Tests;

public sealed class AuctionTests
{
    private const long Budget = 10_000_000;

    /// <summary>Simulated bidders that never bid (0) or always bid, with no price ceiling (1).</summary>
    private static GameConfig Bidders(double chance)
    {
        return TestSupport.ConfigWith(GameConfigLoader.MarketBotsFile, j => j
            .Replace("\"bid_chance_per_check\": 0.3", "\"bid_chance_per_check\": " + chance.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Replace("\"max_bid_ratio\": 1.4", "\"max_bid_ratio\": 1000.0")).Config;
    }

    private static (LocalGame Game, ManualClock Clock, string Dir) Bidder(GameConfig config = null, string dir = null, ManualClock clock = null)
    {
        var (game, c, saveDir) = TestSupport.NewGame(config, dir, clock);
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, c, 600, stepSeconds: 30);
        game.Fishing.StopFishing();
        game.Aquarium.KeepCatches(game.Fishing.GetFishingBox().Select(x => x.CatchId).Take(4).ToList());
        game.Session.Save.Coins = Budget;
        return (game, c, saveDir);
    }

    private static long Fee(long bid) => (long)Math.Round(bid * 0.01, MidpointRounding.AwayFromZero);

    private static long AnyFish(LocalGame game) => game.Aquarium.GetAquarium(AquariumSort.Newest).Fish.Last().FishId;

    [Fact]
    public void Simulated_sellers_run_auctions_that_start_above_what_the_game_pays()
    {
        var (game, _, _) = Bidder(Bidders(0));

        var view = game.Auctions.GetAuctions();

        Assert.Null(view.Mine);
        Assert.Equal(TestSupport.RealConfig().MarketBots.Auctions.TargetAuctionCount, view.Open.Count);
        Assert.All(view.Open, a =>
        {
            Assert.True(a.StartingBidCoins > a.Goods.NpcValueCoins);
            Assert.Equal(a.StartingBidCoins, a.MinNextBidCoins);
            Assert.True(a.RemainingSeconds > 0 && a.RemainingSeconds <= 6 * 3600);
        });
        Assert.Equal(view.Open.Select(a => a.EndsAtMs).OrderBy(t => t), view.Open.Select(a => a.EndsAtMs));
    }

    [Fact]
    public void A_bid_pays_a_1_percent_fee_locks_the_principal_and_a_retry_charges_nothing()
    {
        var (game, _, _) = Bidder(Bidders(0));
        var auction = game.Auctions.GetAuctions().Open.First();

        Assert.Equal(ServiceError.BidTooLow, game.Auctions.PlaceBid(auction.AuctionId, auction.MinNextBidCoins - 1).Error);
        var bid = game.Auctions.PlaceBid(auction.AuctionId, auction.MinNextBidCoins);
        var retry = game.Auctions.PlaceBid(auction.AuctionId, auction.MinNextBidCoins);

        Assert.True(bid.Succeeded, bid.ErrorMessage);
        Assert.Equal(Fee(auction.MinNextBidCoins), bid.Value.FeeCoins);
        Assert.Equal(ServiceError.AlreadyHighestBidder, retry.Error);
        Assert.Equal(Budget - auction.MinNextBidCoins - Fee(auction.MinNextBidCoins), game.Player.GetPlayer().Coins);
        var after = game.Auctions.GetAuctions();
        Assert.Equal(auction.MinNextBidCoins, after.ReservedCoins);
        var mine = after.Open.Single(a => a.AuctionId == auction.AuctionId);
        Assert.True(mine.PlayerIsHighest);
        Assert.Equal((long)Math.Ceiling(auction.MinNextBidCoins * 1.03), mine.MinNextBidCoins);
    }

    [Fact]
    public void Being_outbid_returns_the_principal_but_never_the_fee()
    {
        var (game, clock, _) = Bidder(Bidders(1));
        var auction = game.Auctions.GetAuctions().Open.OrderByDescending(a => a.RemainingSeconds).First();
        var bid = auction.MinNextBidCoins;
        Assert.True(game.Auctions.PlaceBid(auction.AuctionId, bid).Succeeded);

        clock.AdvanceSeconds(11 * 60);
        var news = game.Market.Update();

        Assert.Contains(news, n => n.Kind == MarketEvent.KindOutbid);
        Assert.Equal(Budget - Fee(bid), game.Player.GetPlayer().Coins);
        var now = game.Auctions.GetAuctions();
        Assert.Equal(0, now.ReservedCoins);
        Assert.False(now.Open.Single(a => a.AuctionId == auction.AuctionId).PlayerIsHighest);
    }

    [Fact]
    public void The_highest_bid_wins_at_the_end_and_the_goods_wait_in_withdrawal()
    {
        var (game, clock, _) = Bidder(Bidders(0));
        var auction = game.Auctions.GetAuctions().Open.First();
        var bid = auction.MinNextBidCoins;
        game.Auctions.PlaceBid(auction.AuctionId, bid);

        clock.AdvanceSeconds(auction.RemainingSeconds + 1);
        var news = game.Market.Update();

        Assert.Contains(news, n => n.Kind == MarketEvent.KindAuctionWon);
        Assert.DoesNotContain(game.Auctions.GetAuctions().Open, a => a.AuctionId == auction.AuctionId);
        Assert.Equal(Budget - bid - Fee(bid), game.Player.GetPlayer().Coins);
        var won = Assert.Single(game.Market.GetMarket().Withdrawals);
        Assert.Equal(WithdrawalItem.ReasonAuctionWon, won.Reason);
        Assert.Equal(auction.Goods.Name, won.Goods.Name);
    }

    [Fact]
    public void A_bid_in_the_last_minute_puts_the_clock_back_to_one_minute()
    {
        var (game, clock, _) = Bidder(Bidders(0));
        var auction = game.Auctions.GetAuctions().Open.First();
        clock.AdvanceSeconds(auction.RemainingSeconds - 30);

        Assert.True(game.Auctions.PlaceBid(auction.AuctionId, auction.MinNextBidCoins).Succeeded);

        var after = game.Auctions.GetAuctions().Open.Single(a => a.AuctionId == auction.AuctionId);
        Assert.Equal(60, after.RemainingSeconds, 3);
        clock.AdvanceSeconds(45);
        Assert.Contains(game.Auctions.GetAuctions().Open, a => a.AuctionId == auction.AuctionId);
    }

    [Fact]
    public void Own_auction_one_at_a_time_no_cancelling_no_self_bids_and_early_end_needs_a_bid()
    {
        var (game, _, _) = Bidder(Bidders(0));
        var ids = game.Aquarium.GetAquarium(AquariumSort.Newest).Fish.Select(f => f.FishId).ToList();

        var started = game.Auctions.StartAuction(true, ids[0], 100);

        Assert.True(started.Succeeded, started.ErrorMessage);
        Assert.Null(game.Aquarium.GetFish(ids[0]));
        Assert.Equal(6 * 3600, started.Value.RemainingSeconds, 3);
        Assert.Equal(ServiceError.AuctionLimitReached, game.Auctions.StartAuction(true, ids[1], 100).Error);
        Assert.Equal(ServiceError.FishNotFound, game.Market.ListFish(ids[0], 100).Error);
        Assert.Equal(ServiceError.OwnListing, game.Auctions.PlaceBid(started.Value.AuctionId, 1000).Error);
        Assert.Equal(ServiceError.AuctionHasNoBids, game.Auctions.EndAuctionNow(started.Value.AuctionId).Error);
        Assert.NotNull(game.Auctions.GetAuctions().Mine);
    }

    [Fact]
    public void An_auction_without_bids_returns_the_fish_through_withdrawal_after_6_hours()
    {
        var (game, clock, _) = Bidder(Bidders(0));
        var fishId = AnyFish(game);
        game.Auctions.StartAuction(true, fishId, 100);

        clock.AdvanceSeconds(6 * 3600 - 5);
        Assert.NotNull(game.Auctions.GetAuctions().Mine);
        clock.AdvanceSeconds(10);
        var news = game.Market.Update();

        Assert.Contains(news, n => n.Kind == MarketEvent.KindAuctionUnsold);
        Assert.Null(game.Auctions.GetAuctions().Mine);
        Assert.Equal(WithdrawalItem.ReasonAuctionUnsold, Assert.Single(game.Market.GetMarket().Withdrawals).Reason);
        Assert.True(game.Market.WithdrawAll().Succeeded);
        Assert.NotNull(game.Aquarium.GetFish(fishId));
    }

    [Fact]
    public void Ending_early_pays_the_seller_97_percent_of_the_highest_bid()
    {
        var (game, clock, _) = Bidder(Bidders(1));
        var started = game.Auctions.StartAuction(true, AnyFish(game), 100).Value;

        clock.AdvanceSeconds(11 * 60);
        var withBid = game.Auctions.GetAuctions().Mine;
        Assert.True(withBid.CanEndNow);
        var coins = game.Player.GetPlayer().Coins;

        var ended = game.Auctions.EndAuctionNow(started.AuctionId);

        Assert.True(ended.Succeeded, ended.ErrorMessage);
        var fee = (long)Math.Round(withBid.HighestBidCoins * 0.03, MidpointRounding.AwayFromZero);
        Assert.Equal(coins + withBid.HighestBidCoins - fee, game.Player.GetPlayer().Coins);
        Assert.Null(game.Auctions.GetAuctions().Mine);
        Assert.Empty(game.Market.GetMarket().Withdrawals);
    }

    [Fact]
    public void A_normal_timed_end_charges_the_seller_no_fee()
    {
        var (game, clock, _) = Bidder(Bidders(1));
        game.Auctions.StartAuction(true, AnyFish(game), 100);
        var coins = game.Player.GetPlayer().Coins;

        clock.AdvanceSeconds(6 * 3600 + 120);
        var news = game.Market.Update();

        var sold = Assert.Single(news, n => n.Kind == MarketEvent.KindAuctionSold);
        Assert.Equal(0, sold.FeeCoins);
        Assert.Equal(coins + sold.PriceCoins, game.Player.GetPlayer().Coins);
    }

    [Fact]
    public void Bids_and_reserved_coins_survive_a_restart_and_a_retried_bid_after_it()
    {
        var (game, clock, dir) = Bidder(Bidders(0));
        var auction = game.Auctions.GetAuctions().Open.First();
        game.Auctions.PlaceBid(auction.AuctionId, auction.MinNextBidCoins);
        var coins = game.Player.GetPlayer().Coins;

        var reopened = TestSupport.NewGame(Bidders(0), dir, clock).Game;

        Assert.Equal(coins, reopened.Player.GetPlayer().Coins);
        Assert.Equal(auction.MinNextBidCoins, reopened.Auctions.GetAuctions().ReservedCoins);
        Assert.Equal(ServiceError.AlreadyHighestBidder, reopened.Auctions.PlaceBid(auction.AuctionId, auction.MinNextBidCoins).Error);
        Assert.Equal(coins, reopened.Player.GetPlayer().Coins);
    }

    [Fact]
    public void Bidding_needs_the_bid_plus_the_fee()
    {
        var (game, _, _) = Bidder(Bidders(0));
        var auction = game.Auctions.GetAuctions().Open.First();
        var short1 = auction.MinNextBidCoins + Fee(auction.MinNextBidCoins) - 1;
        game.Session.Save.Coins = short1; // one coin short of bid + fee

        Assert.Equal(ServiceError.NotEnoughCoins, game.Auctions.PlaceBid(auction.AuctionId, auction.MinNextBidCoins).Error);
        Assert.Equal(short1, game.Player.GetPlayer().Coins);
    }
}
