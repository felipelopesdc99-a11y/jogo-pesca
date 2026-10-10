using System.Collections.Generic;
using System.Linq;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Persistence;

namespace FishingIdle.GameService.Market
{
    /// <summary>Which currency a trade listing sells (addendum A-101).</summary>
    public enum CurrencyKind
    {
        Shells,
        Dollars,
    }

    public sealed class CurrencyListingView
    {
        public long ListingId { get; internal set; }
        public CurrencyKind Kind { get; internal set; }
        public long Amount { get; internal set; }

        /// <summary>Total asked for the whole amount, in coins.</summary>
        public long PriceCoins { get; internal set; }

        /// <summary>What the seller receives after the sale fee.</summary>
        public long NetCoins { get; internal set; }
        public long CreatedAtMs { get; internal set; }
        public string SellerName { get; internal set; }
    }

    public sealed class CurrencyTradeView
    {
        public long Coins { get; internal set; }
        public long Shells { get; internal set; }
        public long Dollars { get; internal set; }
        public int MaxListings { get; internal set; }
        public double SaleFeeRatio { get; internal set; }
        public long MinimumPriceCoins { get; internal set; }
        public List<CurrencyListingView> MyListings { get; } = new List<CurrencyListingView>();

        /// <summary>Other real players' listings. Always empty in the local MVP: there are no simulated traders here.</summary>
        public List<CurrencyListingView> Offers { get; } = new List<CurrencyListingView>();

        /// <summary>True while only this computer's player exists (no one can buy yet).</summary>
        public bool LocalOnly { get; internal set; }
    }

    /// <summary>Selling Conchas and Dólares to other players for coins (real players only).</summary>
    public interface ICurrencyTradeService
    {
        CurrencyTradeView GetCurrencyTrade();

        /// <summary>Lists an amount of Conchas or Dólares for a total price in coins. The amount leaves the wallet while listed.</summary>
        ServiceResult<CurrencyListingView> ListCurrency(CurrencyKind kind, long amount, long priceCoins);

        /// <summary>Takes a listing back; the amount returns to the wallet.</summary>
        ServiceResult<CurrencyTradeView> CancelCurrencyListing(long listingId);
    }

    /// <summary>
    /// The local MVP: the player can list and cancel, but nobody is on the other side until the game
    /// is online (owner's decision, 02/10/2026). The online version adds the offers and the purchase.
    /// </summary>
    public sealed class LocalCurrencyTradeService : ICurrencyTradeService
    {
        private readonly GameSession _session;

        public LocalCurrencyTradeService(GameSession session)
        {
            _session = session;
        }

        private PlayerSave Save => _session.Save;

        public CurrencyTradeView GetCurrencyTrade()
        {
            var rules = _session.Config.Economy.CurrencyTrade;
            var view = new CurrencyTradeView
            {
                Coins = Save.Coins,
                Shells = Save.Shells,
                Dollars = Save.Dollars,
                MaxListings = rules.MaxActiveListingsPerPlayer,
                SaleFeeRatio = rules.CompletedSaleFeeRatio,
                MinimumPriceCoins = rules.MinimumPriceCoins,
                LocalOnly = true,
            };
            view.MyListings.AddRange(Save.Market.CurrencyListings.OrderByDescending(l => l.CreatedAtMs).Select(ToView));
            return view;
        }

        public ServiceResult<CurrencyListingView> ListCurrency(CurrencyKind kind, long amount, long priceCoins)
        {
            var rules = _session.Config.Economy.CurrencyTrade;
            if (amount < 1)
            {
                return ServiceResult<CurrencyListingView>.Fail(ServiceError.InvalidAmount);
            }

            if (priceCoins < rules.MinimumPriceCoins)
            {
                return ServiceResult<CurrencyListingView>.Fail(ServiceError.InvalidPrice);
            }

            if (Save.Market.CurrencyListings.Count >= rules.MaxActiveListingsPerPlayer)
            {
                return ServiceResult<CurrencyListingView>.Fail(ServiceError.ListingLimitReached);
            }

            if (kind == CurrencyKind.Shells && Save.Shells < amount)
            {
                return ServiceResult<CurrencyListingView>.Fail(ServiceError.NotEnoughShells);
            }

            if (kind == CurrencyKind.Dollars && Save.Dollars < amount)
            {
                return ServiceResult<CurrencyListingView>.Fail(ServiceError.NotEnoughDollars);
            }

            if (kind == CurrencyKind.Shells)
            {
                Save.Shells -= amount;
            }
            else
            {
                Save.Dollars -= amount;
            }

            var listing = new CurrencyListing
            {
                Id = Save.Market.NextListingId++,
                Currency = KindKey(kind),
                Amount = amount,
                PriceCoins = priceCoins,
                CreatedAtMs = _session.Clock.UtcNowMs,
            };
            Save.Market.CurrencyListings.Add(listing);
            _session.Persist();
            _session.Log("Listed " + amount + " " + listing.Currency + " for " + priceCoins + " coins (currency listing " + listing.Id + ").");
            return ServiceResult<CurrencyListingView>.Ok(ToView(listing));
        }

        public ServiceResult<CurrencyTradeView> CancelCurrencyListing(long listingId)
        {
            var listing = Save.Market.CurrencyListings.FirstOrDefault(l => l.Id == listingId);
            if (listing == null)
            {
                return ServiceResult<CurrencyTradeView>.Fail(ServiceError.ListingNotFound);
            }

            Save.Market.CurrencyListings.Remove(listing);
            if (listing.Currency == KindKey(CurrencyKind.Dollars))
            {
                Save.Dollars += listing.Amount;
            }
            else
            {
                Save.Shells += listing.Amount;
            }

            _session.Persist();
            _session.Log("Cancelled currency listing " + listing.Id + "; " + listing.Amount + " " + listing.Currency + " back to the wallet.");
            return ServiceResult<CurrencyTradeView>.Ok(GetCurrencyTrade());
        }

        private CurrencyListingView ToView(CurrencyListing listing)
        {
            var fee = (long)System.Math.Round(listing.PriceCoins * _session.Config.Economy.CurrencyTrade.CompletedSaleFeeRatio, System.MidpointRounding.AwayFromZero);
            return new CurrencyListingView
            {
                ListingId = listing.Id,
                Kind = listing.Currency == KindKey(CurrencyKind.Dollars) ? CurrencyKind.Dollars : CurrencyKind.Shells,
                Amount = listing.Amount,
                PriceCoins = listing.PriceCoins,
                NetCoins = listing.PriceCoins - fee,
                CreatedAtMs = listing.CreatedAtMs,
                SellerName = Save.PlayerName,
            };
        }

        /// <summary>The key stored in the save ("shells" / "dollars").</summary>
        private static string KindKey(CurrencyKind kind) => kind == CurrencyKind.Dollars ? "dollars" : "shells";
    }
}
