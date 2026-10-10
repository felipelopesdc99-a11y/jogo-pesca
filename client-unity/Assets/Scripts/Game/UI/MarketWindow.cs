using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FishingIdle.Game.Bootstrap;
using FishingIdle.Game.Scene;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Market;
using FishingIdle.Game.Visual;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The Market (GDD sections 33–34): Buy with combined filters, Sell, My Listings and Items to
    /// Withdraw. Prices, fees, availability and custody are decided by the game service; this only
    /// shows its views and sends the player's requests.
    /// </summary>
    public sealed class MarketWindow
    {
        private const float CardWidth = 190f;
        private const float CardHeight = 190f;
        private const float Gap = 12f;
        private const float SidePanelWidth = 330f;

        private enum Tab
        {
            Buy,
            Sell,
            Mine,
            Auction,
            Withdraw,
            Currency,
        }

        private static readonly MarketSort[] Sorts = { MarketSort.PriceAscending, MarketSort.PriceDescending, MarketSort.SizeDescending, MarketSort.SizeAscending, MarketSort.Newest };

        private readonly GameRoot _root;
        private Tab _tab;
        private float _nextRefresh;
        private string _search = string.Empty;
        private Vector2 _scroll;
        private Vector2 _sideScroll;

        private MarketView _market;
        private AuctionsView _auctions;
        private long _selectedAuction;
        private bool _creatingAuction;
        private string _bidText = string.Empty;
        private MarketFilterOptions _options;
        private List<ListingView> _results = new List<ListingView>();
        private List<SellCandidateView> _candidates = new List<SellCandidateView>();
        private long _selectedListing;
        private long _selectedCandidate;
        private bool _selectedCandidateIsFish;
        private string _priceText = string.Empty;
        private CurrencyTradeView _currency;
        private CurrencyKind _currencyKind = CurrencyKind.Shells;
        private string _currencyAmountText = string.Empty;
        private string _currencyPriceText = string.Empty;

        // Buy filters.
        private MarketKindFilter _kind;
        private int _species;
        private int _rarity;
        private int _sizeCategory;
        private int _sort;
        private string _minSize = string.Empty, _maxSize = string.Empty;
        private string _minLevel = string.Empty, _maxLevel = string.Empty;
        private string _minPrice = string.Empty, _maxPrice = string.Empty;

        public MarketWindow(GameRoot root)
        {
            _root = root;
            _root.AquariumChanged += () => _nextRefresh = 0f;
        }

        public bool IsOpen { get; private set; }

        /// <summary>The Market has no confirmation dialog of its own (selections live in its side panel).</summary>
        public bool HasDialog => false;

        public void Open()
        {
            IsOpen = true;
            _nextRefresh = 0f;
            _options = null;
            _search = string.Empty;
        }

        public void Close()
        {
            if (_selectedListing != 0 || _selectedCandidate != 0 || _selectedAuction != 0 || _creatingAuction)
            {
                _selectedListing = 0;
                _selectedCandidate = 0;
                _selectedAuction = 0;
                _creatingAuction = false;
                return;
            }

            IsOpen = false;
        }

        public void Draw(UiSkin skin, float screenWidth, float screenHeight)
        {
            if (!IsOpen)
            {
                return;
            }

            if (Time.unscaledTime >= _nextRefresh)
            {
                Reload();
            }

            if (_market == null)
            {
                return;
            }

            var area = WindowFrame.Draw(skin, screenWidth, screenHeight, GameTexts.Market.Title, GameTexts.Market.Note, out var closed, 1500f, 880f, Icons.Market);
            if (closed)
            {
                _selectedListing = 0;
                _selectedCandidate = 0;
                _selectedAuction = 0;
                _creatingAuction = false;
                IsOpen = false;
                return;
            }

            DrawTabs(skin, area);
            var content = new Rect(area.x, area.y + 52, area.width, area.height - 52);
            switch (_tab)
            {
                case Tab.Sell: DrawSell(skin, content); break;
                case Tab.Mine: DrawMine(skin, content); break;
                case Tab.Auction: DrawAuction(skin, content); break;
                case Tab.Withdraw: DrawWithdraw(skin, content); break;
                case Tab.Currency: DrawCurrency(skin, content); break;
                default: DrawBuy(skin, content); break;
            }
        }

        private void Reload()
        {
            _nextRefresh = Time.unscaledTime + 0.5f;
            _options = _options ?? _root.GetMarketFilters();
            _market = _root.GetMarket();
            if (_tab == Tab.Buy)
            {
                _results = _root.SearchMarket(BuildQuery());
            }
            else if (_tab == Tab.Sell || (_tab == Tab.Auction && _creatingAuction))
            {
                _candidates = _root.GetSellCandidates();
            }

            if (_tab == Tab.Auction)
            {
                _auctions = _root.GetAuctions();
            }

            if (_tab == Tab.Currency)
            {
                _currency = _root.GetCurrencyTrade();
            }
        }

        private void DrawTabs(UiSkin skin, Rect area)
        {
            var tabs = new[]
            {
                (Tab.Buy, GameTexts.Market.TabBuy),
                (Tab.Sell, GameTexts.Market.TabSell),
                (Tab.Mine, GameTexts.Market.TabMineCount(_market.MyListings.Count, _market.MaxListings)),
                (Tab.Auction, GameTexts.Market.TabAuction),
                (Tab.Withdraw, GameTexts.Market.TabWithdrawCount(_market.Withdrawals.Count)),
                (Tab.Currency, GameTexts.Market.TabCurrency),
            };

            var x = area.x;
            foreach (var (tab, label) in tabs)
            {
                var w = skin.Chip.CalcSize(new GUIContent(label)).x + 12;
                if (GUI.Button(new Rect(x, area.y, w, 34), label, _tab == tab ? skin.ChipActive : skin.Chip) && _tab != tab)
                {
                    _tab = tab;
                    _scroll = Vector2.zero;
                    _search = string.Empty;
                    _selectedListing = 0;
                    _selectedCandidate = 0;
                    _selectedAuction = 0;
                    _creatingAuction = false;
                    _nextRefresh = 0f;
                }

                x += w + 8;
            }

            var coins = Format.Number(_market.Coins);
            var cw = skin.Number.CalcSize(new GUIContent(coins)).x + 4f;
            GUI.Label(new Rect(area.xMax - cw, area.y + 2, cw, 30), coins, skin.Number);
            skin.CoinIcon(new Rect(area.xMax - cw - 32, area.y + 4, 24, 24));
            GUI.Label(new Rect(area.xMax - cw - 250, area.y + 8, 200, 20), GameTexts.Market.AquariumSlots(_market.AquariumCount, _market.AquariumCapacity), skin.SmallMutedRight);
        }

        // ------------------------------------------------------------------ Buy

        private void DrawBuy(UiSkin skin, Rect area)
        {
            var filters = new Rect(area.x, area.y, 270, area.height);
            DrawFilters(skin, filters);

            var grid = new Rect(area.x + 290, area.y + 46, area.width - 290 - SidePanelWidth - 16, area.height - 46);
            DrawSearch(skin, grid.xMax, area.y, "busca_mercado");
            var shownResults = _results.Where(l => NameSearch.Matches(l.Goods.Name, _search)).ToList();
            if (shownResults.Count == 0)
            {
                GUI.Label(new Rect(grid.x, grid.y + 8, grid.width, 40), _results.Count > 0 ? GameTexts.Search.NoMatch : GameTexts.Market.NoResults, skin.Body);
            }
            else
            {
                DrawGrid(grid, shownResults, ref _scroll, (rect, listing) =>
                {
                    if (GoodsCard(skin, rect, listing.Goods, null, listing.ListingId == _selectedListing, Format.Number(listing.PriceCoins)))
                    {
                        _selectedListing = listing.ListingId;
                        _sideScroll = Vector2.zero;
                    }
                });
            }

            var side = new Rect(area.xMax - SidePanelWidth, area.y, SidePanelWidth, area.height);
            var selected = _results.FirstOrDefault(l => l.ListingId == _selectedListing);
            if (selected == null)
            {
                GUI.Box(side, GUIContent.none, skin.Card);
                GUI.Label(new Rect(side.x + 20, side.y + 20, side.width - 40, 60), GameTexts.Market.SelectHint, skin.SmallMuted);
                return;
            }

            GUI.Box(side, GUIContent.none, skin.Card);
            var x = side.x + 20;
            var w = side.width - 40;
            var y = DrawGoodsDetail(skin, side, selected.Goods);
            InfoRow(skin, x, ref y, w, GameTexts.Market.Seller, selected.SellerName);
            InfoRow(skin, x, ref y, w, GameTexts.Market.EndsLabel, Format.TimeLeft(selected.RemainingSeconds));
            InfoRow(skin, x, ref y, w, GameTexts.Market.NpcValue, Format.Number(selected.Goods.NpcValueCoins));

            GUI.Label(new Rect(x, side.yMax - 150, w, 60), GameTexts.Market.BuyNote, skin.SmallMuted);
            GUI.enabled = _market.Coins >= selected.PriceCoins;
            if (skin.IconButton(new Rect(x, side.yMax - 62, w, 44), Icons.Buy, GameTexts.Market.BuyFor(Format.Number(selected.PriceCoins)), skin.ButtonPrimary))
            {
                if (_root.BuyListing(selected.ListingId))
                {
                    _selectedListing = 0;
                    _nextRefresh = 0f;
                }
            }

            GUI.enabled = true;
            if (_market.Coins < selected.PriceCoins)
            {
                GUI.Label(new Rect(x, side.yMax - 84, w, 20), GameTexts.ServiceErrorMessage(ServiceError.NotEnoughCoins.ToString()), skin.SmallGold);
            }
        }

        private void DrawFilters(UiSkin skin, Rect area)
        {
            GUI.Box(area, GUIContent.none, skin.Card);
            var x = area.x + 14;
            var w = area.width - 28;
            var y = area.y + 12;
            var changed = false;

            GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), GameTexts.Market.FilterKind, skin.SmallMuted);
            y += 20;
            var kinds = new[] { (MarketKindFilter.All, GameTexts.Market.KindAll), (MarketKindFilter.Fish, GameTexts.Market.KindFish), (MarketKindFilter.Rods, GameTexts.Market.KindRods) };
            var kw = (w - 12) / 3f;
            for (var i = 0; i < kinds.Length; i++)
            {
                if (GUI.Button(new Rect(x + i * (kw + 6), y, kw, 30), kinds[i].Item2, _kind == kinds[i].Item1 ? skin.ChipActive : skin.Chip) && _kind != kinds[i].Item1)
                {
                    _kind = kinds[i].Item1;
                    changed = true;
                }
            }

            y += 42;
            changed |= Cycle(skin, x, ref y, w, GameTexts.Market.FilterSpecies, GameTexts.Market.AnyFemale, _options.Species, ref _species);
            changed |= Cycle(skin, x, ref y, w, GameTexts.Market.FilterRarity, GameTexts.Market.AnyFemale, _options.Rarities, ref _rarity, UiSkin.RarityColor);
            changed |= Cycle(skin, x, ref y, w, GameTexts.Market.FilterSize, GameTexts.Market.AnyFemale, _options.SizeCategories, ref _sizeCategory, UiSkin.SizeColor);
            changed |= Range(skin, x, ref y, w, GameTexts.Market.SizeRange, ref _minSize, ref _maxSize);
            changed |= Range(skin, x, ref y, w, GameTexts.Market.LevelRange, ref _minLevel, ref _maxLevel);
            changed |= Range(skin, x, ref y, w, GameTexts.Market.PriceRange, ref _minPrice, ref _maxPrice);

            GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), GameTexts.Market.SortLabel, skin.SmallMuted);
            y += 20;
            if (GUI.Button(new Rect(x, y, w, 32), "« " + SortName(Sorts[_sort]) + " »", skin.Chip))
            {
                _sort = (_sort + 1) % Sorts.Length;
                changed = true;
            }

            y += 48;
            if (skin.IconButton(new Rect(x, y, w, 34), Icons.Swap, GameTexts.Market.ClearFilters, skin.Button))
            {
                _kind = MarketKindFilter.All;
                _species = _rarity = _sizeCategory = _sort = 0;
                _minSize = _maxSize = _minLevel = _maxLevel = _minPrice = _maxPrice = string.Empty;
                changed = true;
            }

            if (changed)
            {
                _scroll = Vector2.zero;
                _nextRefresh = 0f;
            }
        }

        private MarketQuery BuildQuery()
        {
            return new MarketQuery
            {
                Kind = _kind,
                SpeciesId = _species > 0 && _options != null ? _options.Species[_species - 1].Id : null,
                RarityId = _rarity > 0 && _options != null ? _options.Rarities[_rarity - 1].Id : null,
                SizeCategoryId = _sizeCategory > 0 && _options != null ? _options.SizeCategories[_sizeCategory - 1].Id : null,
                MinSizeCm = ParseDouble(_minSize),
                MaxSizeCm = ParseDouble(_maxSize),
                MinLevel = (int?)ParseLong(_minLevel),
                MaxLevel = (int?)ParseLong(_maxLevel),
                MinPrice = ParseLong(_minPrice),
                MaxPrice = ParseLong(_maxPrice),
                Sort = Sorts[_sort],
            };
        }

        /// <summary>A filter that cycles through "any" and the options. Returns true when it changed.</summary>
        private static bool Cycle(UiSkin skin, float x, ref float y, float w, string label, string any, List<FilterOption> options, ref int index, System.Func<string, Color> colorOf = null)
        {
            GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), label, skin.SmallMuted);
            y += 20;
            var count = options.Count + 1;
            index = Mathf.Clamp(index, 0, count - 1);
            var changed = false;
            if (GUI.Button(new Rect(x, y, 34, 30), "«", skin.Chip))
            {
                index = (index + count - 1) % count;
                changed = true;
            }

            if (index > 0 && colorOf != null)
            {
                // A chosen rarity or size shows in its colour, like the Fishing Box filters.
                var c = colorOf(options[index - 1].Id);
                GUI.DrawTexture(new Rect(x + 40, y, w - 80, 30), skin.White, ScaleMode.StretchToFill, true, 0, new Color(c.r, c.g, c.b, 0.18f), 0, 8);
                var previous = GUI.contentColor;
                GUI.contentColor = Color.Lerp(c, Color.white, 0.4f);
                GUI.Label(new Rect(x + 40, y + 6, w - 80, 22), FishCard.Fit(options[index - 1].Name, skin.Center, w - 80), skin.Center);
                GUI.contentColor = previous;
            }
            else
            {
                GUI.Label(new Rect(x + 40, y + 6, w - 80, 22), FishCard.Fit(index == 0 ? any : options[index - 1].Name, skin.Center, w - 80), skin.Center);
            }
            if (GUI.Button(new Rect(x + w - 34, y, 34, 30), "»", skin.Chip))
            {
                index = (index + 1) % count;
                changed = true;
            }

            y += 42;
            return changed;
        }

        private static bool Range(UiSkin skin, float x, ref float y, float w, string label, ref string min, ref string max)
        {
            GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), label, skin.SmallMuted);
            y += 20;
            var half = (w - 10) / 2f;
            var newMin = Digits(GUI.TextField(new Rect(x, y, half, 28), min, 9), true);
            var newMax = Digits(GUI.TextField(new Rect(x + half + 10, y, half, 28), max, 9), true);
            var changed = newMin != min || newMax != max;
            min = newMin;
            max = newMax;
            y += 40;
            return changed;
        }

        private string AuctionHours()
        {
            return _auctions.DurationHours == 1 ? "1 hora" : Format.Decimal(_auctions.DurationHours, 0) + " horas";
        }

        /// <summary>The "search by fish name" box at the top right of a tab (A-084); <paramref name="right"/> is its right edge.</summary>
        private void DrawSearch(UiSkin skin, float right, float y, string controlName)
        {
            var search = NameSearch.Field(skin, new Rect(right - 300, y, 300, 36), _search, controlName);
            if (search != _search)
            {
                _search = search;
                _scroll = Vector2.zero;
            }
        }

        // ------------------------------------------------------------------ Conchas e Dólares (A-101)

        private void DrawCurrency(UiSkin skin, Rect area)
        {
            if (_currency == null)
            {
                return;
            }

            // Left: the listing form.
            var form = new Rect(area.x, area.y, 420, area.height);
            GUI.Box(form, GUIContent.none, skin.Card);
            var x = form.x + 22;
            var w = form.width - 44;
            var y = form.y + 18;
            GUI.Label(new Rect(x, y, w, 28), GameTexts.Market.CurrencySellTitle, skin.Heading);
            y += 44;

            GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), GameTexts.Market.CurrencyWhat, skin.SmallMuted);
            y += 22;
            var half = (w - 10) / 2f;
            foreach (var (kind, label, icon, i) in new[] { (CurrencyKind.Shells, GameTexts.Player.Shells, Icons.Shell, 0), (CurrencyKind.Dollars, GameTexts.Player.Dollars, Icons.Dollar, 1) })
            {
                if (skin.IconButton(new Rect(x + i * (half + 10), y, half, 36), icon, label, _currencyKind == kind ? skin.ChipActive : skin.Chip) && _currencyKind != kind)
                {
                    _currencyKind = kind;
                }
            }

            y += 48;
            var have = _currencyKind == CurrencyKind.Dollars ? _currency.Dollars : _currency.Shells;
            InfoRow(skin, x, ref y, w, GameTexts.Market.CurrencyYouHave, Format.Number(have));
            y += 6;

            GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), GameTexts.Market.CurrencyAmount, skin.SmallMuted);
            y += 20;
            _currencyAmountText = Digits(GUI.TextField(new Rect(x, y, w, 32), _currencyAmountText, 9), false);
            y += 44;
            GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), GameTexts.Market.CurrencyTotalPrice, skin.SmallMuted);
            y += 20;
            _currencyPriceText = Digits(GUI.TextField(new Rect(x, y, w, 32), _currencyPriceText, 12), false);
            y += 42;

            var amount = ParseLong(_currencyAmountText) ?? 0;
            var price = ParseLong(_currencyPriceText) ?? 0;
            if (amount > 0 && price > 0)
            {
                GUI.Label(new Rect(x, y, w, 20), GameTexts.Market.CurrencyPerUnit(Format.Decimal(price / (double)amount, 1)), skin.SmallMuted);
                y += 22;
                var fee = (long)System.Math.Round(price * _currency.SaleFeeRatio, System.MidpointRounding.AwayFromZero);
                GUI.Label(new Rect(x, y, w, 42), GameTexts.Market.YouReceive + ": " + Format.Number(price - fee) + " (" + GameTexts.Market.Fee(_currency.SaleFeeRatio) + ")", skin.SmallMuted);
            }

            GUI.Label(new Rect(x, form.yMax - 130, w, 60), GameTexts.Market.CurrencyHoldNote, skin.SmallMuted);
            GUI.enabled = amount >= 1 && amount <= have && price >= _currency.MinimumPriceCoins && _currency.MyListings.Count < _currency.MaxListings;
            if (skin.IconButton(new Rect(x, form.yMax - 62, w, 44), Icons.Sell, GameTexts.Market.CurrencyListButton, skin.ButtonPrimary))
            {
                if (_root.ListCurrency(_currencyKind, amount, price))
                {
                    _currencyAmountText = string.Empty;
                    _currencyPriceText = string.Empty;
                    _nextRefresh = 0f;
                }
            }

            GUI.enabled = true;

            // Right, top: other players' offers (none until the game is online — no simulated traders here).
            var right = new Rect(form.xMax + 20, area.y, area.width - form.width - 20, area.height);
            var offers = new Rect(right.x, right.y, right.width, 150);
            GUI.Box(offers, GUIContent.none, skin.Card);
            GUI.Label(new Rect(offers.x + 20, offers.y + 16, offers.width - 40, 26), GameTexts.Market.CurrencyOffersTitle, skin.Heading);
            if (_currency.Offers.Count == 0)
            {
                skin.DrawIcon(new Rect(offers.x + 20, offers.y + 58, 18, 18), Icons.Info, UiSkin.Muted);
                GUI.Label(new Rect(offers.x + 46, offers.y + 56, offers.width - 66, 80), GameTexts.Market.CurrencyNoOffers, skin.SmallMuted);
            }

            // Right, bottom: the player's own listings.
            var mine = new Rect(right.x, offers.yMax + 16, right.width, right.yMax - offers.yMax - 16);
            GUI.Label(new Rect(mine.x, mine.y, mine.width, 26), GameTexts.Market.CurrencyMineCount(_currency.MyListings.Count, _currency.MaxListings), skin.Heading);
            if (_currency.MyListings.Count == 0)
            {
                GUI.Label(new Rect(mine.x, mine.y + 36, mine.width, 24), GameTexts.Market.CurrencyNoListings, skin.Body);
                return;
            }

            var list = new Rect(mine.x, mine.y + 36, mine.width, mine.height - 36);
            var content = new Rect(0, 0, list.width - 20, _currency.MyListings.Count * 82);
            _scroll = GUI.BeginScrollView(list, _scroll, content);
            var ry = 0f;
            foreach (var l in _currency.MyListings)
            {
                var row = new Rect(0, ry, content.width, 74);
                GUI.Box(row, GUIContent.none, skin.Card);
                var isDollars = l.Kind == CurrencyKind.Dollars;
                skin.DrawIcon(new Rect(row.x + 18, row.y + 22, 30, 30), isDollars ? Icons.Dollar : Icons.Shell, Color.white);
                GUI.Label(new Rect(row.x + 62, row.y + 12, row.width - 240, 26),
                    GameTexts.Market.CurrencyLine(Format.Number(l.Amount), isDollars ? GameTexts.Player.Dollars : GameTexts.Player.Shells, Format.Number(l.PriceCoins)), skin.BodyBold);
                GUI.Label(new Rect(row.x + 62, row.y + 40, row.width - 240, 20), GameTexts.Market.YouReceive + ": " + Format.Number(l.NetCoins), skin.SmallMuted);
                if (GUI.Button(new Rect(row.xMax - 150, row.y + 18, 132, 38), GameTexts.Market.CurrencyCancel, skin.Button))
                {
                    _root.CancelCurrencyListing(l.ListingId);
                    _nextRefresh = 0f;
                }

                ry += 82;
            }

            GUI.EndScrollView();
        }

        // ------------------------------------------------------------------ Sell

        private void DrawSell(UiSkin skin, Rect area)
        {
            var grid = new Rect(area.x, area.y + 46, area.width - SidePanelWidth - 16, area.height - 46);
            GUI.Label(new Rect(area.x, area.y + 8, grid.width - 320, 22), GameTexts.Market.SellHint, skin.SmallMuted);
            DrawSearch(skin, grid.xMax, area.y, "busca_mercado_venda");

            var shownCandidates = _candidates.Where(c => NameSearch.Matches(c.Goods.Name, _search)).ToList();
            if (shownCandidates.Count == 0)
            {
                GUI.Label(new Rect(grid.x, grid.y + 8, grid.width, 40), _candidates.Count > 0 ? GameTexts.Search.NoMatch : GameTexts.Market.NothingToSell, skin.Body);
            }
            else
            {
                DrawGrid(grid, shownCandidates, ref _scroll, (rect, c) =>
                {
                    var selected = c.SourceId == _selectedCandidate && c.IsFish == _selectedCandidateIsFish;
                    var previous = GUI.color;
                    if (c.Blocker != ServiceError.None)
                    {
                        GUI.color = previous * new Color(1f, 1f, 1f, 0.55f);
                    }

                    if (GoodsCard(skin, rect, c.Goods, GameTexts.Market.NpcValue + " " + Format.Number(c.Goods.NpcValueCoins), selected))
                    {
                        _selectedCandidate = c.SourceId;
                        _selectedCandidateIsFish = c.IsFish;
                        _priceText = c.Goods.ReferenceCoins.ToString(CultureInfo.InvariantCulture);
                    }

                    GUI.color = previous;
                });
            }

            var side = new Rect(area.xMax - SidePanelWidth, area.y, SidePanelWidth, area.height);
            GUI.Box(side, GUIContent.none, skin.Card);
            var candidate = _candidates.FirstOrDefault(c => c.SourceId == _selectedCandidate && c.IsFish == _selectedCandidateIsFish);
            if (candidate == null)
            {
                GUI.Label(new Rect(side.x + 20, side.y + 20, side.width - 40, 60), GameTexts.Market.SelectHint, skin.SmallMuted);
                return;
            }

            var x = side.x + 20;
            var w = side.width - 40;
            var y = DrawGoodsDetail(skin, side, candidate.Goods);
            InfoRow(skin, x, ref y, w, GameTexts.Market.NpcValue, Format.Number(candidate.Goods.NpcValueCoins));
            InfoRow(skin, x, ref y, w, GameTexts.Market.Reference, Format.Number(candidate.Goods.ReferenceCoins));

            var blocker = candidate.Blocker;
            if (blocker == ServiceError.None && _market.MyListings.Count >= _market.MaxListings)
            {
                blocker = ServiceError.ListingLimitReached;
            }

            var bottom = side.yMax - 20;
            if (blocker != ServiceError.None)
            {
                GUI.Label(new Rect(x, bottom - 60, w, 60), GameTexts.ServiceErrorMessage(blocker.ToString()), skin.SmallGold);
                return;
            }

            GUI.Label(new Rect(x, bottom - 230, w, 20), GameTexts.Market.YourPrice, skin.SmallMuted);
            _priceText = Digits(GUI.TextField(new Rect(x, bottom - 208, w, 32), _priceText, 12), false);
            var price = ParseLong(_priceText) ?? 0;
            var fee = (long)Math.Round(price * _market.SaleFeeRatio, MidpointRounding.AwayFromZero);
            var ry = bottom - 166;
            InfoRow(skin, x, ref ry, w, GameTexts.Market.Fee(_market.SaleFeeRatio), Format.Number(fee));
            InfoRow(skin, x, ref ry, w, GameTexts.Market.YouReceive, Format.Number(Math.Max(0, price - fee)));
            GUI.Label(new Rect(x, bottom - 114, w, 64), GameTexts.Market.ListingNote, skin.SmallMuted);

            GUI.enabled = price >= _market.MinimumPriceCoins;
            if (skin.IconButton(new Rect(x, bottom - 44, w, 44), Icons.Sell, GameTexts.Market.ListFor(_market.ListingDurationDays), skin.ButtonPrimary))
            {
                if (_root.CreateListing(candidate.IsFish, candidate.SourceId, price))
                {
                    _selectedCandidate = 0;
                    _nextRefresh = 0f;
                }
            }

            GUI.enabled = true;
        }

        // ------------------------------------------------------------------ My Listings

        private void DrawMine(UiSkin skin, Rect area)
        {
            var list = new Rect(area.x, area.y, area.width - SidePanelWidth - 16, area.height);
            if (_market.MyListings.Count == 0)
            {
                GUI.Label(new Rect(list.x, list.y + 8, list.width, 40), GameTexts.Market.NoListings, skin.Body);
            }

            // Scrolls when the listings do not fit (rows are laid out inside the scroll view).
            var listHeight = _market.MyListings.Count * 102f;
            var listScrolls = listHeight > list.height;
            var rowWidth = list.width - (listScrolls ? 20f : 0f);
            _scroll = GUI.BeginScrollView(list, _scroll, new Rect(0, 0, rowWidth, listHeight), false, listScrolls);
            var y = 0f;
            foreach (var l in _market.MyListings)
            {
                var row = new Rect(0, y, rowWidth, 92);
                GUI.Box(row, GUIContent.none, skin.Card);
                GoodsIcon(skin, new Rect(row.x + 14, row.y + 12, 110, 60), l.Goods);
                GUI.Label(new Rect(row.x + 140, row.y + 14, 320, 24), l.Goods.Name, skin.BodyBold);
                GoodsLabel(skin, new Rect(row.x + 140, row.y + 40, 320, 20), l.Goods, skin.Small);
                GUI.Label(new Rect(row.x + 140, row.y + 62, 320, 20), GameTexts.Market.EndsIn(Format.TimeLeft(l.RemainingSeconds)), skin.SmallMuted);

                GUI.Label(new Rect(row.x + 480, row.y + 14, 200, 20), GameTexts.Market.Price, skin.SmallMuted);
                GUI.Label(new Rect(row.x + 480, row.y + 34, 200, 28), Format.Number(l.PriceCoins), skin.Number);
                GUI.Label(new Rect(row.x + 480, row.y + 64, 280, 20), GameTexts.Market.YouReceive + ": " + Format.Number(l.NetCoins), skin.SmallMuted);

                if (skin.IconButton(new Rect(row.xMax - 200, row.y + 24, 184, 42), Icons.Close, GameTexts.Market.Cancel, skin.Button))
                {
                    _root.CancelListing(l.ListingId);
                    _nextRefresh = 0f;
                }

                y += 102;
            }

            GUI.EndScrollView();

            var side = new Rect(area.xMax - SidePanelWidth, area.y, SidePanelWidth, area.height);
            GUI.Box(side, GUIContent.none, skin.Card);
            GUI.Label(new Rect(side.x + 20, side.y + 16, side.width - 40, 24), GameTexts.Market.RecentSales, skin.Heading);
            var sales = _market.Events.Where(e => e.Sold).ToList();
            if (sales.Count == 0)
            {
                GUI.Label(new Rect(side.x + 20, side.y + 52, side.width - 40, 40), GameTexts.Market.NoSales, skin.SmallMuted);
                return;
            }

            var inner = new Rect(side.x + 12, side.y + 50, side.width - 24, side.height - 62);
            _sideScroll = GUI.BeginScrollView(inner, _sideScroll, new Rect(0, 0, inner.width - 20, sales.Count * 64));
            var sy = 0f;
            foreach (var e in sales)
            {
                GUI.Label(new Rect(8, sy, inner.width - 36, 20), Format.DateTimeFromUnixMs(e.AtMs), skin.SmallMuted);
                GUI.Label(new Rect(8, sy + 20, inner.width - 36, 40), GameTexts.Market.SaleLine(e.GoodsName, e.BuyerName, Format.Number(e.PriceCoins), Format.Number(e.FeeCoins)), skin.Small);
                sy += 64;
            }

            GUI.EndScrollView();
        }

        // ------------------------------------------------------------------ Auction

        private void DrawAuction(UiSkin skin, Rect area)
        {
            if (_auctions == null)
            {
                return;
            }

            // Strip: the player's own auction (or the button to create one) and the coins locked in bids.
            var strip = new Rect(area.x, area.y, area.width, 78);
            GUI.Box(strip, GUIContent.none, skin.Card);
            GUI.Label(new Rect(strip.x + 18, strip.y + 10, 200, 20), GameTexts.Market.MyAuction, skin.SmallMuted);
            var mine = _auctions.Mine;
            if (mine == null)
            {
                GUI.Label(new Rect(strip.x + 18, strip.y + 34, 400, 24), GameTexts.Market.NoMyAuction, skin.Body);
                if (!_creatingAuction && skin.IconButton(new Rect(strip.x + 440, strip.y + 20, 200, 40), Icons.Add, GameTexts.Market.CreateAuction, skin.ButtonPrimary))
                {
                    _creatingAuction = true;
                    _search = string.Empty;
                    _selectedCandidate = 0;
                    _selectedAuction = 0;
                    _scroll = Vector2.zero;
                    _nextRefresh = 0f;
                }
            }
            else
            {
                GoodsIcon(skin, new Rect(strip.x + 18, strip.y + 30, 70, 40), mine.Goods);
                GUI.Label(new Rect(strip.x + 100, strip.y + 32, 260, 22), mine.Goods.Name, skin.BodyBold);
                var bid = mine.BidCount == 0 ? GameTexts.Market.NoBids : GameTexts.Market.BidAt(Format.Number(mine.HighestBidCoins)) + " · " + GameTexts.Market.BidCount(mine.BidCount);
                GUI.Label(new Rect(strip.x + 370, strip.y + 14, 300, 22), bid, skin.SmallGold);
                GUI.Label(new Rect(strip.x + 370, strip.y + 40, 300, 22), GameTexts.Market.EndsIn(Format.TimeLeft(mine.RemainingSeconds)), skin.SmallMuted);
                if (mine.CanEndNow)
                {
                    if (GUI.Button(new Rect(strip.x + 690, strip.y + 18, 300, 42), GameTexts.Market.EndNow(Format.Number(mine.EndNowNetCoins)), skin.Button))
                    {
                        _root.EndAuctionNow(mine.AuctionId);
                        _nextRefresh = 0f;
                    }
                }
                else
                {
                    GUI.Label(new Rect(strip.x + 690, strip.y + 18, 320, 44), GameTexts.ServiceErrorMessage(ServiceError.AuctionHasNoBids.ToString()), skin.SmallMuted);
                }
            }

            if (_auctions.ReservedCoins > 0)
            {
                GUI.Label(new Rect(strip.xMax - 300, strip.y + 30, 280, 22), GameTexts.Market.Reserved(Format.Number(_auctions.ReservedCoins)), skin.SmallGoldRight);
            }

            var body = new Rect(area.x, area.y + 92, area.width, area.height - 92);
            if (_creatingAuction)
            {
                DrawCreateAuction(skin, body);
                return;
            }

            // The note shares its row with the search box, so it gets room for four lines of the small text (A-149).
            GUI.Label(new Rect(body.x, body.y, body.width - SidePanelWidth - 16 - 320, 84), GameTexts.Market.AuctionNote(AuctionHours(), Format.Percent(_auctions.MinIncrementRatio, 0), Format.Percent(_auctions.BidFeeRatio, 0), Format.Duration(_auctions.AntiSnipeResetSeconds)), skin.SmallMuted);
            var grid = new Rect(body.x, body.y + 90, body.width - SidePanelWidth - 16, body.height - 90);
            DrawSearch(skin, grid.xMax, body.y, "busca_leilao");
            var shownAuctions = _auctions.Open.Where(a => NameSearch.Matches(a.Goods.Name, _search)).ToList();
            if (shownAuctions.Count == 0)
            {
                GUI.Label(new Rect(grid.x, grid.y + 8, grid.width, 40), _auctions.Open.Count > 0 ? GameTexts.Search.NoMatch : GameTexts.Market.NoAuctions, skin.Body);
            }
            else
            {
                DrawGrid(grid, shownAuctions, ref _scroll, (rect, a) =>
                {
                    var corner = (a.BidCount == 0 ? GameTexts.Market.StartingAt(Format.Number(a.StartingBidCoins)) : GameTexts.Market.BidAt(Format.Number(a.HighestBidCoins)))
                                 + " · " + Format.TimeLeft(a.RemainingSeconds);
                    // "Ganhando" is the card's own top-right badge, so it never covers the rarity seal or the check.
                    if (GoodsCard(skin, rect, a.Goods, corner, a.AuctionId == _selectedAuction, null, a.PlayerIsHighest ? GameTexts.Market.Winning : null, UiSkin.Accent))
                    {
                        _selectedAuction = a.AuctionId;
                        _bidText = a.MinNextBidCoins.ToString(CultureInfo.InvariantCulture);
                    }
                });
            }

            var side = new Rect(body.xMax - SidePanelWidth, body.y, SidePanelWidth, body.height);
            GUI.Box(side, GUIContent.none, skin.Card);
            var selected = _auctions.Open.FirstOrDefault(a => a.AuctionId == _selectedAuction);
            if (selected == null)
            {
                GUI.Label(new Rect(side.x + 20, side.y + 20, side.width - 40, 60), GameTexts.Market.SelectHint, skin.SmallMuted);
                return;
            }

            var x = side.x + 20;
            var w = side.width - 40;
            var y = DrawGoodsDetail(skin, side, selected.Goods);
            InfoRow(skin, x, ref y, w, GameTexts.Market.Seller, selected.SellerName);
            InfoRow(skin, x, ref y, w, GameTexts.Market.CurrentBid, selected.BidCount == 0 ? GameTexts.Market.NoBids : Format.Number(selected.HighestBidCoins) + " · " + selected.HighestBidderName);
            InfoRow(skin, x, ref y, w, GameTexts.Market.EndsLabel, Format.TimeLeft(selected.RemainingSeconds));

            var bottom = side.yMax - 20;
            if (selected.PlayerIsHighest)
            {
                GUI.Label(new Rect(x, bottom - 40, w, 40), GameTexts.Market.YouAreWinning, skin.SmallGold);
                return;
            }

            GUI.Label(new Rect(x, bottom - 170, w, 20), GameTexts.Market.MinBid(Format.Number(selected.MinNextBidCoins)), skin.SmallMuted);
            GUI.Label(new Rect(x, bottom - 146, w, 20), GameTexts.Market.YourBid, skin.SmallMuted);
            _bidText = Digits(GUI.TextField(new Rect(x, bottom - 124, w, 32), _bidText, 12), false);
            var amount = ParseLong(_bidText) ?? 0;
            var fee = (long)Math.Round(amount * _auctions.BidFeeRatio, MidpointRounding.AwayFromZero);
            GUI.Label(new Rect(x, bottom - 84, w, 20), GameTexts.Market.BidFee(_auctions.BidFeeRatio, Format.Number(fee)), skin.SmallMuted);
            GUI.enabled = amount >= selected.MinNextBidCoins && _auctions.Coins >= amount + fee;
            if (skin.IconButton(new Rect(x, bottom - 44, w, 44), Icons.Honor, GameTexts.Market.PlaceBid, skin.ButtonPrimary))
            {
                _root.PlaceBid(selected.AuctionId, amount);
                _nextRefresh = 0f;
            }

            GUI.enabled = true;
        }

        private void DrawCreateAuction(UiSkin skin, Rect area)
        {
            if (GUI.Button(new Rect(area.x, area.y, 220, 34), "« " + GameTexts.Market.BackToAuctions, skin.Button))
            {
                _creatingAuction = false;
                _search = string.Empty;
                _selectedCandidate = 0;
                return;
            }

            // Room for three lines of the small text between "Voltar" and the search box (A-149).
            GUI.Label(new Rect(area.x + 240, area.y, area.width - SidePanelWidth - 260 - 320, 64), GameTexts.Market.StartAuctionNote(AuctionHours()), skin.SmallMuted);
            var grid = new Rect(area.x, area.y + 70, area.width - SidePanelWidth - 16, area.height - 70);
            DrawSearch(skin, grid.xMax, area.y, "busca_leilao_criar");
            var shownCandidates = _candidates.Where(c => NameSearch.Matches(c.Goods.Name, _search)).ToList();
            if (shownCandidates.Count == 0 && _candidates.Count > 0)
            {
                GUI.Label(new Rect(grid.x, grid.y + 8, grid.width, 40), GameTexts.Search.NoMatch, skin.Body);
            }

            DrawGrid(grid, shownCandidates, ref _scroll, (rect, c) =>
            {
                var previous = GUI.color;
                if (c.Blocker != ServiceError.None)
                {
                    GUI.color = previous * new Color(1f, 1f, 1f, 0.55f);
                }

                if (GoodsCard(skin, rect, c.Goods, GameTexts.Market.NpcValue + " " + Format.Number(c.Goods.NpcValueCoins), c.SourceId == _selectedCandidate && c.IsFish == _selectedCandidateIsFish))
                {
                    _selectedCandidate = c.SourceId;
                    _selectedCandidateIsFish = c.IsFish;
                    _priceText = c.Goods.NpcValueCoins.ToString(CultureInfo.InvariantCulture);
                }

                GUI.color = previous;
            });

            var side = new Rect(area.xMax - SidePanelWidth, area.y, SidePanelWidth, area.height);
            GUI.Box(side, GUIContent.none, skin.Card);
            var candidate = _candidates.FirstOrDefault(c => c.SourceId == _selectedCandidate && c.IsFish == _selectedCandidateIsFish);
            if (candidate == null)
            {
                GUI.Label(new Rect(side.x + 20, side.y + 20, side.width - 40, 60), GameTexts.Market.SellHint, skin.SmallMuted);
                return;
            }

            var x = side.x + 20;
            var w = side.width - 40;
            var y = DrawGoodsDetail(skin, side, candidate.Goods);
            InfoRow(skin, x, ref y, w, GameTexts.Market.NpcValue, Format.Number(candidate.Goods.NpcValueCoins));
            InfoRow(skin, x, ref y, w, GameTexts.Market.Reference, Format.Number(candidate.Goods.ReferenceCoins));

            var bottom = side.yMax - 20;
            if (candidate.Blocker != ServiceError.None)
            {
                GUI.Label(new Rect(x, bottom - 60, w, 60), GameTexts.ServiceErrorMessage(candidate.Blocker.ToString()), skin.SmallGold);
                return;
            }

            GUI.Label(new Rect(x, bottom - 110, w, 20), GameTexts.Market.StartingBid, skin.SmallMuted);
            _priceText = Digits(GUI.TextField(new Rect(x, bottom - 88, w, 32), _priceText, 12), false);
            var start = ParseLong(_priceText) ?? 0;
            GUI.enabled = start >= _market.MinimumPriceCoins;
            if (skin.IconButton(new Rect(x, bottom - 44, w, 44), Icons.Hourglass, GameTexts.Market.StartAuctionFor(_auctions.DurationHours), skin.ButtonPrimary))
            {
                if (_root.StartAuction(candidate.IsFish, candidate.SourceId, start))
                {
                    _creatingAuction = false;
                    _selectedCandidate = 0;
                    _nextRefresh = 0f;
                }
            }

            GUI.enabled = true;
        }

        // ------------------------------------------------------------------ Items to Withdraw

        private void DrawWithdraw(UiSkin skin, Rect area)
        {
            GUI.Label(new Rect(area.x, area.y, area.width - 240, 40), GameTexts.Market.WithdrawNote, skin.SmallMuted);
            GUI.enabled = _market.Withdrawals.Count > 0;
            if (skin.IconButton(new Rect(area.xMax - 220, area.y - 4, 220, 38), Icons.Box, GameTexts.Market.WithdrawAll, skin.ButtonPrimary))
            {
                _root.WithdrawAll();
                _nextRefresh = 0f;
            }

            GUI.enabled = true;
            var list = new Rect(area.x, area.y + 48, area.width, area.height - 48);
            if (_market.Withdrawals.Count == 0)
            {
                GUI.Label(new Rect(list.x, list.y + 8, list.width, 40), GameTexts.Market.NothingToWithdraw, skin.Body);
                return;
            }

            var content = new Rect(0, 0, list.width - 20, _market.Withdrawals.Count * 96);
            _scroll = GUI.BeginScrollView(list, _scroll, content);
            var y = 0f;
            foreach (var w in _market.Withdrawals)
            {
                var row = new Rect(0, y, content.width, 86);
                GUI.Box(row, GUIContent.none, skin.Card);
                GoodsIcon(skin, new Rect(row.x + 14, row.y + 12, 110, 60), w.Goods);
                GUI.Label(new Rect(row.x + 140, row.y + 12, 360, 24), w.Goods.Name, skin.BodyBold);
                GoodsLabel(skin, new Rect(row.x + 140, row.y + 38, 360, 20), w.Goods, skin.Small);
                GUI.Label(new Rect(row.x + 520, row.y + 16, 300, 20), GameTexts.Market.Reason(w.Reason), skin.SmallGold);
                GUI.Label(new Rect(row.x + 520, row.y + 38, 300, 20), Format.DateTimeFromUnixMs(w.AtMs), skin.SmallMuted);

                if (w.Blocker != ServiceError.None)
                {
                    GUI.Label(new Rect(row.xMax - 330, row.y + 22, 314, 44), GameTexts.ServiceErrorMessage(w.Blocker.ToString()), skin.SmallGold);
                }
                else if (skin.IconButton(new Rect(row.xMax - 180, row.y + 22, 164, 42), Icons.Box, GameTexts.Market.Withdraw, skin.Button))
                {
                    _root.Withdraw(w.WithdrawalId);
                    _nextRefresh = 0f;
                }

                y += 96;
            }

            GUI.EndScrollView();
        }

        // ------------------------------------------------------------------ pieces

        /// <summary>The detail block at the top of a side panel. Returns the y below it.</summary>
        private static float DrawGoodsDetail(UiSkin skin, Rect side, GoodsView goods)
        {
            var x = side.x + 20;
            var w = side.width - 40;
            var y = side.y + 16;
            GoodsIcon(skin, new Rect(x, y, w, 110), goods);
            y += 120;
            GUI.Label(new Rect(x, y, w, 26), goods.Name, skin.Heading);
            y += 28;
            GoodsLabel(skin, new Rect(x, y, w, 20), goods, skin.Small);
            y += 30;

            if (goods.IsFish)
            {
                var f = goods.Fish;
                InfoRow(skin, x, ref y, w, GameTexts.Aquarium.Level, GameTexts.Aquarium.LevelOf(f.Level, f.MaxLevel));
                InfoRow(skin, x, ref y, w, GameTexts.Aquarium.Hp, Format.Decimal(f.Stats.Hp, 0));
                InfoRow(skin, x, ref y, w, GameTexts.Aquarium.Attack, Format.Decimal(f.Stats.Attack, 1));
                InfoRow(skin, x, ref y, w, GameTexts.Aquarium.Defense, Format.Decimal(f.Stats.Defense, 1));
                InfoRow(skin, x, ref y, w, GameTexts.Aquarium.Speed, Format.Decimal(f.Stats.Speed, 0));
            }
            else if (goods.Rod != null)
            {
                var r = goods.Rod;
                InfoRow(skin, x, ref y, w, GameTexts.Profile.RarityBonus, "+" + Format.Percent(r.RarityBonus, 0));
                InfoRow(skin, x, ref y, w, GameTexts.Profile.SizeBonus, "+" + Format.Percent(r.SizeBonus, 0));
                InfoRow(skin, x, ref y, w, GameTexts.Profile.ShellBonus, "+" + Format.Percent(r.ShellBonus, 0));
            }

            return y + 6;
        }

        /// <summary>The goods line with a fish's size category in its colour (addendum A-079).</summary>
        private static void GoodsLabel(UiSkin skin, Rect rect, GoodsView goods, GUIStyle style)
        {
            var fish = goods.IsFish ? goods.Fish : null;
            skin.SizeLine(rect, GoodsLine(goods), fish?.SizeCategoryName, fish?.SizeCategoryId, style);
        }

        private static string GoodsLine(GoodsView goods)
        {
            if (goods.IsFish)
            {
                var line = GameTexts.Market.FishLine(Format.SizeCm(goods.Fish.SizeCm), goods.Fish.SizeCategoryName);
                return line + " · " + GameTexts.Player.LevelShort + " " + goods.Fish.Level;
            }

            return goods.Rod == null ? GameTexts.Market.Rod : GameTexts.Market.RodLine(GameTexts.Profile.Tier(goods.Rod.Tier), goods.Rod.Level, goods.Rod.HasLevels);
        }

        private static void GoodsIcon(UiSkin skin, Rect rect, GoodsView goods)
        {
            var art = GoodsArt(goods);
            if (art != null)
            {
                GUI.DrawTexture(rect, art, ScaleMode.ScaleToFit, true);
            }
        }

        /// <summary>The picture of a fish or a rod (Resources/Arte/Varas/&lt;rod id&gt;.png).</summary>
        private static Texture2D GoodsArt(GoodsView goods)
        {
            if (goods.IsFish)
            {
                return Art.FishTexture(goods.Fish.SpeciesId);
            }

            return goods.Rod != null ? Visual.ArtAssets.Texture("Varas/" + goods.Rod.RodId) : null;
        }

        /// <summary>One Market card (the official card: picture first, essentials after). Returns true when clicked.</summary>
        private static bool GoodsCard(UiSkin skin, Rect rect, GoodsView goods, string corner, bool selected, string coins = null, string badge = null, Color? badgeColor = null)
        {
            var fish = goods.IsFish ? goods.Fish : null;
            return FishCard.Draw(skin, rect, new FishCardModel
            {
                SpeciesId = fish?.SpeciesId,
                Name = goods.Name,
                Line = GoodsLine(goods),
                RarityId = fish?.RarityId,
                RarityName = fish?.RarityName,
                SizeCategoryId = fish?.SizeCategoryId,
                SizeCategoryName = fish?.SizeCategoryName,
                Footer = corner,
                Coins = coins,
                Selected = selected,
                Badge = badge,
                BadgeColor = badgeColor ?? Color.white,
                Art = fish == null ? GoodsArt(goods) ?? skin.White : null,
            });
        }

        private static void InfoRow(UiSkin skin, float x, ref float y, float w, string label, string value)
        {
            // Label and value side by side, never overlapping. The value (a short number) keeps its width, up to
            // 55% of the line; the label gets the rest and is shortened with "…" only when it still does not fit.
            var vw = Mathf.Min(w * 0.55f, skin.SmallRight.CalcSize(new GUIContent(value)).x + 4f);
            var lw = Mathf.Max(0f, w - vw - 8f);
            GUI.Label(new Rect(x, y, lw, 20), FishCard.Fit(label, skin.SmallMuted, lw), skin.SmallMuted);
            GUI.Label(new Rect(x + w - vw, y, vw, 20), FishCard.Fit(value, skin.SmallRight, vw), skin.SmallRight);
            y += 24;
        }

        /// <summary>A scrolling card grid that only draws the rows on screen.</summary>
        private static void DrawGrid<T>(Rect area, IReadOnlyList<T> items, ref Vector2 scroll, Action<Rect, T> drawCard)
        {
            var columns = Mathf.Max(1, Mathf.FloorToInt((area.width - 20 + Gap) / (CardWidth + Gap)));
            var rows = Mathf.CeilToInt(items.Count / (float)columns);
            var content = new Rect(0, 0, area.width - 20, rows * (CardHeight + Gap));

            scroll = GUI.BeginScrollView(area, scroll, content);
            var firstRow = Mathf.Max(0, Mathf.FloorToInt(scroll.y / (CardHeight + Gap)));
            var lastRow = Mathf.Min(rows - 1, Mathf.CeilToInt((scroll.y + area.height) / (CardHeight + Gap)));
            for (var row = firstRow; row <= lastRow; row++)
            {
                for (var col = 0; col < columns; col++)
                {
                    var index = row * columns + col;
                    if (index >= items.Count)
                    {
                        break;
                    }

                    drawCard(new Rect(col * (CardWidth + Gap), row * (CardHeight + Gap), CardWidth, CardHeight), items[index]);
                }
            }

            GUI.EndScrollView();
        }

        private static string SortName(MarketSort sort)
        {
            switch (sort)
            {
                case MarketSort.PriceDescending: return GameTexts.Market.SortPriceDesc;
                case MarketSort.SizeDescending: return GameTexts.Market.SortSizeDesc;
                case MarketSort.SizeAscending: return GameTexts.Market.SortSizeAsc;
                case MarketSort.Newest: return GameTexts.Market.SortNewest;
                default: return GameTexts.Market.SortPriceAsc;
            }
        }

        /// <summary>Keeps only digits (and one decimal comma when allowed).</summary>
        private static string Digits(string text, bool allowDecimal)
        {
            var result = new System.Text.StringBuilder();
            var comma = false;
            foreach (var ch in text ?? string.Empty)
            {
                if (char.IsDigit(ch))
                {
                    result.Append(ch);
                }
                else if (allowDecimal && !comma && (ch == ',' || ch == '.'))
                {
                    result.Append(',');
                    comma = true;
                }
            }

            return result.ToString();
        }

        private static long? ParseLong(string text)
        {
            var digits = (text ?? string.Empty).Split(',')[0];
            return long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : (long?)null;
        }

        private static double? ParseDouble(string text)
        {
            return double.TryParse((text ?? string.Empty).Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value) ? value : (double?)null;
        }
    }
}
