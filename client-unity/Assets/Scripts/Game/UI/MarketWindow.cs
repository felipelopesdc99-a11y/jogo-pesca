using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FishingIdle.Game.Bootstrap;
using FishingIdle.Game.Scene;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Market;
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
        private const float CardHeight = 170f;
        private const float Gap = 12f;
        private const float SidePanelWidth = 330f;

        private enum Tab
        {
            Buy,
            Sell,
            Mine,
            Auction,
            Withdraw,
        }

        private static readonly MarketSort[] Sorts = { MarketSort.PriceAscending, MarketSort.PriceDescending, MarketSort.SizeDescending, MarketSort.SizeAscending, MarketSort.Newest };

        private readonly GameRoot _root;
        private Tab _tab;
        private float _nextRefresh;
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

        public void Open()
        {
            IsOpen = true;
            _nextRefresh = 0f;
            _options = null;
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

            var area = WindowFrame.Draw(skin, screenWidth, screenHeight, GameTexts.Market.Title, GameTexts.Market.Note, out var closed, 1500f, 880f);
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
            };

            var x = area.x;
            foreach (var (tab, label) in tabs)
            {
                var w = skin.Chip.CalcSize(new GUIContent(label)).x + 12;
                if (GUI.Button(new Rect(x, area.y, w, 34), label, _tab == tab ? skin.ChipActive : skin.Chip) && _tab != tab)
                {
                    _tab = tab;
                    _scroll = Vector2.zero;
                    _selectedListing = 0;
                    _selectedCandidate = 0;
                    _selectedAuction = 0;
                    _creatingAuction = false;
                    _nextRefresh = 0f;
                }

                x += w + 8;
            }

            var coins = Format.Number(_market.Coins);
            var cw = skin.Number.CalcSize(new GUIContent(coins)).x;
            GUI.Label(new Rect(area.xMax - cw, area.y + 2, cw, 30), coins, skin.Number);
            skin.CoinIcon(new Rect(area.xMax - cw - 32, area.y + 4, 24, 24));
            GUI.Label(new Rect(area.xMax - cw - 250, area.y + 8, 200, 20), GameTexts.Market.AquariumSlots(_market.AquariumCount, _market.AquariumCapacity), skin.SmallMutedRight);
        }

        // ------------------------------------------------------------------ Buy

        private void DrawBuy(UiSkin skin, Rect area)
        {
            var filters = new Rect(area.x, area.y, 270, area.height);
            DrawFilters(skin, filters);

            var grid = new Rect(area.x + 290, area.y, area.width - 290 - SidePanelWidth - 16, area.height);
            if (_results.Count == 0)
            {
                GUI.Label(new Rect(grid.x, grid.y + 8, grid.width, 40), GameTexts.Market.NoResults, skin.Body);
            }
            else
            {
                DrawGrid(grid, _results, ref _scroll, (rect, listing) =>
                {
                    if (GoodsCard(skin, rect, listing.Goods, Format.Number(listing.PriceCoins), listing.ListingId == _selectedListing))
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
            if (GUI.Button(new Rect(x, side.yMax - 62, w, 44), GameTexts.Market.BuyFor(Format.Number(selected.PriceCoins)), skin.ButtonPrimary))
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

            GUI.Label(new Rect(x, y, w, 18), GameTexts.Market.FilterKind, skin.SmallMuted);
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
            changed |= Cycle(skin, x, ref y, w, GameTexts.Market.FilterRarity, GameTexts.Market.AnyFemale, _options.Rarities, ref _rarity);
            changed |= Cycle(skin, x, ref y, w, GameTexts.Market.FilterSize, GameTexts.Market.AnyFemale, _options.SizeCategories, ref _sizeCategory);
            changed |= Range(skin, x, ref y, w, GameTexts.Market.SizeRange, ref _minSize, ref _maxSize);
            changed |= Range(skin, x, ref y, w, GameTexts.Market.LevelRange, ref _minLevel, ref _maxLevel);
            changed |= Range(skin, x, ref y, w, GameTexts.Market.PriceRange, ref _minPrice, ref _maxPrice);

            GUI.Label(new Rect(x, y, w, 18), GameTexts.Market.SortLabel, skin.SmallMuted);
            y += 20;
            if (GUI.Button(new Rect(x, y, w, 32), "« " + SortName(Sorts[_sort]) + " »", skin.Chip))
            {
                _sort = (_sort + 1) % Sorts.Length;
                changed = true;
            }

            y += 48;
            if (GUI.Button(new Rect(x, y, w, 34), GameTexts.Market.ClearFilters, skin.Button))
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
        private static bool Cycle(UiSkin skin, float x, ref float y, float w, string label, string any, List<FilterOption> options, ref int index)
        {
            GUI.Label(new Rect(x, y, w, 18), label, skin.SmallMuted);
            y += 20;
            var count = options.Count + 1;
            index = Mathf.Clamp(index, 0, count - 1);
            var changed = false;
            if (GUI.Button(new Rect(x, y, 34, 30), "«", skin.Chip))
            {
                index = (index + count - 1) % count;
                changed = true;
            }

            GUI.Label(new Rect(x + 40, y + 6, w - 80, 22), index == 0 ? any : options[index - 1].Name, skin.Center);
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
            GUI.Label(new Rect(x, y, w, 18), label, skin.SmallMuted);
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

        // ------------------------------------------------------------------ Sell

        private void DrawSell(UiSkin skin, Rect area)
        {
            var grid = new Rect(area.x, area.y + 28, area.width - SidePanelWidth - 16, area.height - 28);
            GUI.Label(new Rect(area.x, area.y, grid.width, 22), GameTexts.Market.SellHint, skin.SmallMuted);
            if (_candidates.Count == 0)
            {
                GUI.Label(new Rect(grid.x, grid.y + 8, grid.width, 40), GameTexts.Market.NothingToSell, skin.Body);
            }
            else
            {
                DrawGrid(grid, _candidates, ref _scroll, (rect, c) =>
                {
                    var selected = c.SourceId == _selectedCandidate && c.IsFish == _selectedCandidateIsFish;
                    var previous = GUI.color;
                    if (c.Blocker != ServiceError.None)
                    {
                        GUI.color = new Color(1f, 1f, 1f, 0.55f);
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
            GUI.Label(new Rect(x, bottom - 110, w, 50), GameTexts.Market.ListingNote, skin.SmallMuted);

            GUI.enabled = price >= _market.MinimumPriceCoins;
            if (GUI.Button(new Rect(x, bottom - 44, w, 44), GameTexts.Market.ListFor(_market.ListingDurationDays), skin.ButtonPrimary))
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

            var y = list.y;
            foreach (var l in _market.MyListings)
            {
                var row = new Rect(list.x, y, list.width, 92);
                GUI.Box(row, GUIContent.none, skin.Card);
                GoodsIcon(skin, new Rect(row.x + 14, row.y + 12, 110, 60), l.Goods);
                GUI.Label(new Rect(row.x + 140, row.y + 14, 320, 24), l.Goods.Name, skin.BodyBold);
                GUI.Label(new Rect(row.x + 140, row.y + 40, 320, 20), GoodsLine(l.Goods), skin.Small);
                GUI.Label(new Rect(row.x + 140, row.y + 62, 320, 20), GameTexts.Market.EndsIn(Format.TimeLeft(l.RemainingSeconds)), skin.SmallMuted);

                GUI.Label(new Rect(row.x + 480, row.y + 14, 200, 20), GameTexts.Market.Price, skin.SmallMuted);
                GUI.Label(new Rect(row.x + 480, row.y + 34, 200, 28), Format.Number(l.PriceCoins), skin.Number);
                GUI.Label(new Rect(row.x + 480, row.y + 64, 280, 20), GameTexts.Market.YouReceive + ": " + Format.Number(l.NetCoins), skin.SmallMuted);

                if (GUI.Button(new Rect(row.xMax - 200, row.y + 24, 184, 42), GameTexts.Market.Cancel, skin.Button))
                {
                    _root.CancelListing(l.ListingId);
                    _nextRefresh = 0f;
                }

                y += 102;
            }

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
                if (!_creatingAuction && GUI.Button(new Rect(strip.x + 440, strip.y + 20, 200, 40), GameTexts.Market.CreateAuction, skin.ButtonPrimary))
                {
                    _creatingAuction = true;
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

            GUI.Label(new Rect(body.x, body.y, body.width - SidePanelWidth - 16, 40), GameTexts.Market.AuctionNote, skin.SmallMuted);
            var grid = new Rect(body.x, body.y + 46, body.width - SidePanelWidth - 16, body.height - 46);
            if (_auctions.Open.Count == 0)
            {
                GUI.Label(new Rect(grid.x, grid.y + 8, grid.width, 40), GameTexts.Market.NoAuctions, skin.Body);
            }
            else
            {
                DrawGrid(grid, _auctions.Open, ref _scroll, (rect, a) =>
                {
                    var corner = (a.BidCount == 0 ? GameTexts.Market.StartingAt(Format.Number(a.StartingBidCoins)) : GameTexts.Market.BidAt(Format.Number(a.HighestBidCoins)))
                                 + " · " + Format.TimeLeft(a.RemainingSeconds);
                    if (GoodsCard(skin, rect, a.Goods, corner, a.AuctionId == _selectedAuction))
                    {
                        _selectedAuction = a.AuctionId;
                        _bidText = a.MinNextBidCoins.ToString(CultureInfo.InvariantCulture);
                    }

                    if (a.PlayerIsHighest)
                    {
                        skin.Tag(new Rect(rect.x + 10, rect.y + 10, 84, 18), GameTexts.Market.Winning, UiSkin.Accent);
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
            if (GUI.Button(new Rect(x, bottom - 44, w, 44), GameTexts.Market.PlaceBid, skin.ButtonPrimary))
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
                _selectedCandidate = 0;
                return;
            }

            GUI.Label(new Rect(area.x + 240, area.y + 6, area.width - SidePanelWidth - 260, 40), GameTexts.Market.StartAuctionNote, skin.SmallMuted);
            var grid = new Rect(area.x, area.y + 48, area.width - SidePanelWidth - 16, area.height - 48);
            DrawGrid(grid, _candidates, ref _scroll, (rect, c) =>
            {
                var previous = GUI.color;
                if (c.Blocker != ServiceError.None)
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.55f);
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
            if (GUI.Button(new Rect(x, bottom - 44, w, 44), GameTexts.Market.StartAuctionFor(_auctions.DurationHours), skin.ButtonPrimary))
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
            if (GUI.Button(new Rect(area.xMax - 220, area.y - 4, 220, 38), GameTexts.Market.WithdrawAll, skin.ButtonPrimary))
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
                GUI.Label(new Rect(row.x + 140, row.y + 38, 360, 20), GoodsLine(w.Goods), skin.Small);
                GUI.Label(new Rect(row.x + 520, row.y + 16, 300, 20), GameTexts.Market.Reason(w.Reason), skin.SmallGold);
                GUI.Label(new Rect(row.x + 520, row.y + 38, 300, 20), Format.DateTimeFromUnixMs(w.AtMs), skin.SmallMuted);

                if (w.Blocker != ServiceError.None)
                {
                    GUI.Label(new Rect(row.xMax - 330, row.y + 22, 314, 44), GameTexts.ServiceErrorMessage(w.Blocker.ToString()), skin.SmallGold);
                }
                else if (GUI.Button(new Rect(row.xMax - 180, row.y + 22, 164, 42), GameTexts.Market.Withdraw, skin.Button))
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
            GUI.Label(new Rect(x, y, w, 20), GoodsLine(goods), skin.Small);
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
            if (goods.IsFish)
            {
                GUI.DrawTexture(rect, Art.FishTexture(goods.Fish.SpeciesId), ScaleMode.ScaleToFit, true);
                return;
            }

            // A simple rod: a long shaft with a reel.
            var cy = rect.center.y;
            GUI.DrawTexture(new Rect(rect.x + rect.width * 0.1f, cy - 3, rect.width * 0.8f, 6), skin.White, ScaleMode.StretchToFill, true, 0, new Color(0.62f, 0.45f, 0.28f), 0, 3);
            var reel = Mathf.Min(rect.height * 0.45f, 30f);
            var previous = GUI.color;
            GUI.color = new Color(0.78f, 0.82f, 0.88f);
            GUI.DrawTexture(new Rect(rect.x + rect.width * 0.22f, cy - reel / 2f, reel, reel), skin.Coin, ScaleMode.ScaleToFit, true);
            GUI.color = previous;
        }

        /// <summary>One Market card. Returns true when clicked.</summary>
        private static bool GoodsCard(UiSkin skin, Rect rect, GoodsView goods, string corner, bool selected)
        {
            var hovered = rect.Contains(Event.current.mousePosition);
            var important = goods.IsFish && goods.Fish.IsImportant;
            var style = selected ? skin.CardSelected : important ? skin.CardImportant : hovered ? skin.CardHovered : skin.Card;
            var clicked = GUI.Button(rect, GUIContent.none, style);

            GoodsIcon(skin, new Rect(rect.x + 14, rect.y + 12, rect.width - 28, 70), goods);
            GUI.Label(new Rect(rect.x + 12, rect.y + 88, rect.width - 24, 22), goods.Name, skin.BodyBold);
            GUI.Label(new Rect(rect.x + 12, rect.y + 110, rect.width - 24, 34), GoodsLine(goods), skin.Small);
            GUI.Label(new Rect(rect.x + 12, rect.y + 146, rect.width - 24, 20), corner, skin.SmallGold);

            if (goods.IsFish && goods.Fish.RarityId != null && goods.Fish.RarityId != "common")
            {
                skin.Tag(new Rect(rect.xMax - 66, rect.y + 10, 56, 18), goods.Fish.RarityName.ToUpperInvariant(), UiSkin.Rare);
            }

            return clicked;
        }

        private static void InfoRow(UiSkin skin, float x, ref float y, float w, string label, string value)
        {
            GUI.Label(new Rect(x, y, w * 0.6f, 20), label, skin.SmallMuted);
            GUI.Label(new Rect(x + w * 0.4f, y, w * 0.6f, 20), value, skin.SmallRight);
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
