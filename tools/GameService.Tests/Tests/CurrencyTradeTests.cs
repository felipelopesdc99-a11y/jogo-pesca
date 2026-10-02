using FishingIdle.GameService.Core;
using FishingIdle.GameService.Market;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>Selling Conchas and Dólares (addendum A-101): locally, list and cancel only — no simulated buyers.</summary>
public sealed class CurrencyTradeTests
{
    [Fact]
    public void Listing_takes_the_amount_out_of_the_wallet_and_cancelling_gives_it_back()
    {
        var (game, _, _) = TestSupport.NewGame();
        var save = game.Session.Save;
        save.Shells = 50;
        save.Dollars = 20;

        var shells = game.CurrencyTrade.ListCurrency(CurrencyKind.Shells, 30, 6000);
        Assert.True(shells.Succeeded, shells.ErrorMessage);
        Assert.Equal(20, save.Shells);
        Assert.True(game.CurrencyTrade.ListCurrency(CurrencyKind.Dollars, 20, 900).Succeeded);
        Assert.Equal(0, save.Dollars);

        var view = game.CurrencyTrade.GetCurrencyTrade();
        Assert.True(view.LocalOnly);
        Assert.Empty(view.Offers); // no simulated traders, ever
        Assert.Equal(2, view.MyListings.Count);
        Assert.True(shells.Value.NetCoins < shells.Value.PriceCoins);

        Assert.True(game.CurrencyTrade.CancelCurrencyListing(shells.Value.ListingId).Succeeded);
        Assert.Equal(50, save.Shells);
        Assert.Equal(ServiceError.ListingNotFound, game.CurrencyTrade.CancelCurrencyListing(shells.Value.ListingId).Error);
    }

    [Fact]
    public void Listing_refuses_what_the_player_does_not_have_or_a_bad_amount_or_price()
    {
        var (game, _, _) = TestSupport.NewGame();
        game.Session.Save.Shells = 5;

        Assert.Equal(ServiceError.NotEnoughShells, game.CurrencyTrade.ListCurrency(CurrencyKind.Shells, 6, 100).Error);
        Assert.Equal(ServiceError.NotEnoughDollars, game.CurrencyTrade.ListCurrency(CurrencyKind.Dollars, 1, 100).Error);
        Assert.Equal(ServiceError.InvalidAmount, game.CurrencyTrade.ListCurrency(CurrencyKind.Shells, 0, 100).Error);
        Assert.Equal(ServiceError.InvalidPrice, game.CurrencyTrade.ListCurrency(CurrencyKind.Shells, 1, 0).Error);
        Assert.Equal(5, game.Session.Save.Shells);
    }

    [Fact]
    public void A_listing_survives_closing_and_reopening_the_game()
    {
        var dir = TestSupport.NewTempDirectory();
        var clock = new ManualClock(TestSupport.StartMs);
        var (game, _, _) = TestSupport.NewGame(TestSupport.RealConfig(), dir, clock);
        game.Session.Save.Shells = 10;
        Assert.True(game.CurrencyTrade.ListCurrency(CurrencyKind.Shells, 4, 800).Succeeded);

        var (reopened, _, _) = TestSupport.NewGame(TestSupport.RealConfig(), dir, clock);
        Assert.Equal(6, reopened.Session.Save.Shells);
        var listing = Assert.Single(reopened.CurrencyTrade.GetCurrencyTrade().MyListings);
        Assert.Equal(4, listing.Amount);
        Assert.Equal(800, listing.PriceCoins);
    }
}
