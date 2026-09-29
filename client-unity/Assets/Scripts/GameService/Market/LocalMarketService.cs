using System;
using System.Collections.Generic;
using System.Linq;
using FishingIdle.GameService.Aquarium;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;
using FishingIdle.GameService.Persistence;

namespace FishingIdle.GameService.Market
{
    /// <summary>The fixed-price Market (GDD sections 33–34): Buy, Sell, My Listings, Items to Withdraw.</summary>
    public interface IMarketService
    {
        MarketView GetMarket();

        /// <summary>Buy tab: other sellers' active listings matching the filters.</summary>
        List<ListingView> Search(MarketQuery query);

        MarketFilterOptions GetFilterOptions();

        /// <summary>Sell tab: what the player owns and whether each one can be listed now.</summary>
        List<SellCandidateView> GetSellCandidates();

        ServiceResult<ListingView> ListFish(long fishId, long priceCoins);

        ServiceResult<ListingView> ListRod(long itemId, long priceCoins);

        /// <summary>Buys a listing at its price. The goods go to Items to Withdraw, never straight to the Aquarium.</summary>
        ServiceResult<MarketPurchase> Buy(long listingId);

        /// <summary>Takes one of the player's listings down; the goods go to Items to Withdraw.</summary>
        ServiceResult<WithdrawalView> CancelListing(long listingId);

        /// <summary>Moves one entry from Items to Withdraw into the Aquarium or Inventory.</summary>
        ServiceResult<WithdrawalView> Withdraw(long withdrawalId);

        /// <summary>Withdraws everything that fits; fish stay when the Aquarium is full.</summary>
        ServiceResult<WithdrawAllResult> WithdrawAll();

        /// <summary>Runs the simulated traders up to now (also time the game was closed) and returns news to show.</summary>
        List<MarketEventView> Update();
    }

    public sealed partial class LocalMarketService : IMarketService, IAuctionService
    {
        private const int EventLimit = 30;
        private const ulong SupplySalt = 0x5A1E_5EEDUL;
        private const ulong DemandSalt = 0xDE3A_0D00UL;

        private readonly GameSession _session;
        private readonly LocalAquariumService _aquarium;

        public LocalMarketService(GameSession session, LocalAquariumService aquarium)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _aquarium = aquarium ?? throw new ArgumentNullException(nameof(aquarium));
        }

        private PlayerSave Save => _session.Save;
        private GameConfig Config => _session.Config;
        private MarketState State => Save.Market;
        private long Now => _session.Clock.UtcNowMs;

        public MarketView GetMarket()
        {
            Settle();
            var view = new MarketView
            {
                Coins = Save.Coins,
                MaxListings = Config.Market.MaxActiveListingsPerPlayer,
                ListingDurationDays = Config.Market.MaxListingDurationDays,
                SaleFeeRatio = Config.Market.CompletedSaleFeeRatio,
                MinimumPriceCoins = Config.Market.MinimumListingPriceCoins,
                AquariumCount = Save.Aquarium.Count,
                AquariumCapacity = Config.AquariumCapacity,
            };

            view.MyListings.AddRange(State.MyListings.OrderBy(l => l.ExpiresAtMs).Select(l => ToView(l, true)));
            view.Withdrawals.AddRange(State.Withdrawals.OrderByDescending(w => w.AtMs).ThenByDescending(w => w.Id).Select(ToView));
            view.Events.AddRange(State.Events.Select(ToView));
            return view;
        }

        public List<ListingView> Search(MarketQuery query)
        {
            Settle();
            query = query ?? new MarketQuery();
            var results = State.BotListings.Select(l => ToView(l, false)).Where(v => Matches(v, query));
            switch (query.Sort)
            {
                case MarketSort.PriceDescending:
                    results = results.OrderByDescending(v => v.PriceCoins).ThenBy(v => v.ListingId);
                    break;
                case MarketSort.SizeDescending:
                    results = results.OrderByDescending(v => v.Goods.Fish?.SizePercentile ?? -1).ThenBy(v => v.PriceCoins);
                    break;
                case MarketSort.SizeAscending:
                    results = results.OrderBy(v => v.Goods.Fish?.SizePercentile ?? 2).ThenBy(v => v.PriceCoins);
                    break;
                case MarketSort.Newest:
                    results = results.OrderByDescending(v => v.ListedAtMs).ThenByDescending(v => v.ListingId);
                    break;
                default:
                    results = results.OrderBy(v => v.PriceCoins).ThenBy(v => v.ListingId);
                    break;
            }

            return results.ToList();
        }

        public MarketFilterOptions GetFilterOptions()
        {
            var options = new MarketFilterOptions();
            options.Species.AddRange(Config.FishCatalog.Species.OrderBy(s => s.DisplayName, StringComparer.CurrentCulture).Select(s => new FilterOption { Id = s.Id, Name = s.DisplayName }));
            options.Rarities.AddRange(Config.Progression.Rarity.Tiers.Select(t => new FilterOption { Id = t.Id, Name = t.DisplayName }));
            options.SizeCategories.AddRange(Config.SizeCategories.Select(c => new FilterOption { Id = c.Id, Name = c.DisplayName }));
            return options;
        }

        public List<SellCandidateView> GetSellCandidates()
        {
            var list = new List<SellCandidateView>();
            foreach (var fish in Save.Aquarium.OrderByDescending(f => f.Level).ThenBy(f => f.Id))
            {
                list.Add(new SellCandidateView
                {
                    IsFish = true,
                    SourceId = fish.Id,
                    Goods = ToView(new MarketGoods { Kind = MarketGoods.KindFish, Fish = fish }),
                    Blocker = FishBlocker(fish.Id),
                });
            }

            foreach (var item in Save.Inventory.Where(i => i.Kind == InventoryItem.KindRod).OrderBy(i => i.Id))
            {
                if (!Config.TryGetRod(item.RodId, out _))
                {
                    continue;
                }

                list.Add(new SellCandidateView
                {
                    IsFish = false,
                    SourceId = item.Id,
                    Goods = ToView(new MarketGoods { Kind = MarketGoods.KindRod, Rod = item }),
                    Blocker = RodBlocker(item),
                });
            }

            return list;
        }

        public ServiceResult<ListingView> ListFish(long fishId, long priceCoins)
        {
            Settle();
            var fish = Save.Aquarium.FirstOrDefault(f => f.Id == fishId);
            if (fish == null)
            {
                return ServiceResult<ListingView>.Fail(ServiceError.FishNotFound);
            }

            var blocker = FishBlocker(fishId);
            if (blocker == ServiceError.None) blocker = ListingBlocker(priceCoins);
            if (blocker != ServiceError.None)
            {
                return ServiceResult<ListingView>.Fail(blocker);
            }

            // The fish leaves the Aquarium: listed goods cannot be used anywhere else (GDD section 33).
            Save.Aquarium.Remove(fish);
            var listing = NewPlayerListing(new MarketGoods { Kind = MarketGoods.KindFish, Fish = fish }, priceCoins);
            _session.Persist();
            _session.Log("Listed fish " + fish.Id + " for " + priceCoins + " coins (listing " + listing.Id + ").");
            return ServiceResult<ListingView>.Ok(ToView(listing, true));
        }

        public ServiceResult<ListingView> ListRod(long itemId, long priceCoins)
        {
            Settle();
            var item = Save.Inventory.FirstOrDefault(i => i.Id == itemId && i.Kind == InventoryItem.KindRod);
            if (item == null || !Config.TryGetRod(item.RodId, out _))
            {
                return ServiceResult<ListingView>.Fail(ServiceError.ItemNotFound);
            }

            var blocker = RodBlocker(item);
            if (blocker == ServiceError.None) blocker = ListingBlocker(priceCoins);
            if (blocker != ServiceError.None)
            {
                return ServiceResult<ListingView>.Fail(blocker);
            }

            Save.Inventory.Remove(item);
            var listing = NewPlayerListing(new MarketGoods { Kind = MarketGoods.KindRod, Rod = item }, priceCoins);
            _session.Persist();
            _session.Log("Listed rod item " + item.Id + " for " + priceCoins + " coins (listing " + listing.Id + ").");
            return ServiceResult<ListingView>.Ok(ToView(listing, true));
        }

        public ServiceResult<MarketPurchase> Buy(long listingId)
        {
            Settle();
            if (State.MyListings.Any(l => l.Id == listingId))
            {
                return ServiceResult<MarketPurchase>.Fail(ServiceError.OwnListing);
            }

            var listing = State.BotListings.FirstOrDefault(l => l.Id == listingId);
            if (listing == null)
            {
                return ServiceResult<MarketPurchase>.Fail(ServiceError.ListingNotFound);
            }

            if (Save.Coins < listing.PriceCoins)
            {
                return ServiceResult<MarketPurchase>.Fail(ServiceError.NotEnoughCoins);
            }

            // One removal, then the payment: a second request for the same listing finds nothing.
            State.BotListings.Remove(listing);
            Save.Coins -= listing.PriceCoins;
            if (listing.Goods.Kind == MarketGoods.KindRod)
            {
                // A resale refund is based on what this owner paid, so a bargain cannot be resold to the game at a profit.
                listing.Goods.Rod.PurchasePriceCoins = listing.PriceCoins;
                listing.Goods.Rod.UpgradeCoinsInvested = 0;
            }

            var entry = AddWithdrawal(listing.Goods, WithdrawalItem.ReasonBought, Now);
            _session.Persist();
            _session.Log("Bought listing " + listing.Id + " for " + listing.PriceCoins + " coins.");
            return ServiceResult<MarketPurchase>.Ok(new MarketPurchase { Item = ToView(entry), PricePaid = listing.PriceCoins, NewBalance = Save.Coins });
        }

        public ServiceResult<WithdrawalView> CancelListing(long listingId)
        {
            Settle();
            var listing = State.MyListings.FirstOrDefault(l => l.Id == listingId);
            if (listing == null)
            {
                return ServiceResult<WithdrawalView>.Fail(ServiceError.ListingNotFound);
            }

            State.MyListings.Remove(listing);
            var entry = AddWithdrawal(listing.Goods, WithdrawalItem.ReasonCancelled, Now);
            _session.Persist();
            _session.Log("Cancelled listing " + listing.Id + ".");
            return ServiceResult<WithdrawalView>.Ok(ToView(entry));
        }

        public ServiceResult<WithdrawalView> Withdraw(long withdrawalId)
        {
            Settle();
            var entry = State.Withdrawals.FirstOrDefault(w => w.Id == withdrawalId);
            if (entry == null)
            {
                return ServiceResult<WithdrawalView>.Fail(ServiceError.WithdrawalNotFound);
            }

            var error = MoveOut(entry);
            if (error != ServiceError.None)
            {
                return ServiceResult<WithdrawalView>.Fail(error);
            }

            _session.Persist();
            return ServiceResult<WithdrawalView>.Ok(ToView(entry));
        }

        public ServiceResult<WithdrawAllResult> WithdrawAll()
        {
            Settle();
            var result = new WithdrawAllResult();
            foreach (var entry in State.Withdrawals.OrderBy(w => w.Id).ToList())
            {
                if (MoveOut(entry) == ServiceError.None)
                {
                    result.Withdrawn++;
                }
                else
                {
                    result.LeftBehind++;
                }
            }

            if (result.Withdrawn == 0 && result.LeftBehind == 0)
            {
                return ServiceResult<WithdrawAllResult>.Fail(ServiceError.WithdrawalNotFound);
            }

            _session.Persist();
            return ServiceResult<WithdrawAllResult>.Ok(result);
        }

        public List<MarketEventView> Update()
        {
            var changed = Settle();
            var news = State.Events.Where(e => !e.Notified).OrderBy(e => e.AtMs).ToList();
            foreach (var e in news)
            {
                e.Notified = true;
            }

            if (changed || news.Count > 0)
            {
                _session.Persist();
            }

            return news.Select(ToView).ToList();
        }

        // ------------------------------------------------------------------ simulation

        /// <summary>Brings the simulated traders up to the present. Returns whether anything changed.</summary>
        private bool Settle()
        {
            var changed = RunSupply();
            changed |= RunDemand();
            changed |= ExpireMyListings();
            changed |= RunAuctions();
            return changed;
        }

        private bool RunSupply()
        {
            var supply = Config.MarketBots.Supply;
            var intervalMs = Math.Max(1L, (long)Math.Round(supply.RefreshIntervalMinutes * 60000.0));
            var durationMs = (long)Math.Round(supply.ListingDurationHours * 3600000.0);
            var nowTick = Now / intervalMs;
            var changed = false;

            if (State.SupplyTick == 0)
            {
                // First visit: a market that already has sellers, listed at different moments.
                for (var i = 0; i < supply.TargetListingCount; i++)
                {
                    var rng = NextSupplyRng();
                    var listedAt = Now - (long)(rng.NextDouble() * durationMs * 0.75);
                    State.BotListings.Add(NewBotListing(rng, listedAt, durationMs));
                }

                State.SupplyTick = nowTick;
                return true;
            }

            if (nowTick <= State.SupplyTick)
            {
                return RemoveExpiredBotListings(Now);
            }

            // Only the ticks whose listings could still be alive matter after a long absence.
            var maxTicks = durationMs / intervalMs + 1;
            var first = Math.Max(State.SupplyTick + 1, nowTick - maxTicks + 1);
            for (var t = first; t <= nowTick; t++)
            {
                var tickTime = t * intervalMs;
                RemoveExpiredBotListings(tickTime);
                var room = Math.Min(supply.NewListingsPerRefresh, supply.TargetListingCount - State.BotListings.Count);
                for (var i = 0; i < room; i++)
                {
                    State.BotListings.Add(NewBotListing(NextSupplyRng(), tickTime, durationMs));
                }

                changed = true;
            }

            State.SupplyTick = nowTick;
            RemoveExpiredBotListings(Now);
            return changed;
        }

        private bool RemoveExpiredBotListings(long at)
        {
            return State.BotListings.RemoveAll(l => l.ExpiresAtMs <= at) > 0;
        }

        private Rng NextSupplyRng()
        {
            State.BotListingsCreated++;
            return Rng.For(Save.RngSeed ^ SupplySalt, State.BotListingsCreated, 0);
        }

        private MarketListing NewBotListing(Rng rng, long listedAt, long durationMs)
        {
            var supply = Config.MarketBots.Supply;
            var seller = RandomTraderName(rng);
            var goods = NewBotGoods(rng, supply.RodListingChance, listedAt);

            // Never below what the game pays, so buying a simulated listing to resell it to the game never profits.
            var floor = MarketRules.NpcValue(Config, goods) + 1;
            var reference = MarketRules.ReferenceValue(Config, goods);
            var ratio = supply.PriceRatio.Min + rng.NextDouble() * (supply.PriceRatio.Max - supply.PriceRatio.Min);
            var price = Math.Max(floor, (long)Math.Round(reference * ratio, MidpointRounding.AwayFromZero));

            return new MarketListing
            {
                Id = State.NextListingId++,
                SellerName = seller,
                PriceCoins = price,
                ListedAtMs = listedAt,
                ExpiresAtMs = listedAt + durationMs,
                Goods = goods,
            };
        }

        private string RandomTraderName(Rng rng)
        {
            var names = Config.ArenaBots.Names;
            return names[rng.NextIntInclusive(0, names.Count - 1)];
        }

        /// <summary>A simulated trader other than the given ones (the next name in the list when the draw hits one); null if none.</summary>
        private string RandomTraderNameExcept(Rng rng, string a, string b)
        {
            var names = Config.ArenaBots.Names;
            var start = rng.NextIntInclusive(0, names.Count - 1);
            for (var i = 0; i < names.Count; i++)
            {
                var name = names[(start + i) % names.Count];
                if (name != a && name != b)
                {
                    return name;
                }
            }

            return null;
        }

        /// <summary>A fish from any map (sometimes levelled) or, with <paramref name="rodChance"/>, a tradable rod.</summary>
        private MarketGoods NewBotGoods(Rng rng, double rodChance, long at)
        {
            var rods = Config.Rods.Rods.Where(r => r.TradableOnMarket).ToList();
            var rodRoll = rng.NextDouble();
            if (rods.Count > 0 && rodRoll < rodChance)
            {
                var rod = rods[rng.NextIntInclusive(0, rods.Count - 1)];
                var level = rod.HasInternalLevels ? LowBiasedLevel(rng, Config.RodMaxLevel(rod)) : 1;
                return new MarketGoods
                {
                    Kind = MarketGoods.KindRod,
                    Rod = new InventoryItem
                    {
                        Kind = InventoryItem.KindRod,
                        RodId = rod.Id,
                        Level = level,
                        AcquiredAtMs = at,
                        PurchasePriceCoins = rod.Acquisition?.PurchaseCostCoins ?? 0,
                        UpgradeCoinsInvested = MarketRules.RodUpgradeSpend(Config, rod, level),
                    },
                };
            }

            var map = Config.Maps.Maps[rng.NextIntInclusive(0, Config.Maps.Maps.Count - 1)];
            var rolled = CatchRules.RollFound(Config, map, rng);
            var fishLevel = LowBiasedLevel(rng, Math.Min(Config.MarketBots.Supply.MaxFishLevel, Config.Progression.FishLevel.MaxLevel));
            return new MarketGoods
            {
                Kind = MarketGoods.KindFish,
                Fish = new FishInstance
                {
                    SpeciesId = rolled.Species.Id,
                    SizeMm = rolled.SizeMm,
                    SizeCategoryId = rolled.SizeCategory.Id,
                    Level = fishLevel,
                    InvestedXp = MarketRules.FishXpToReach(Config, fishLevel),
                    CaughtAtMs = at,
                    KeptAtMs = at,
                },
            };
        }

        /// <summary>Most simulated goods are low level; a few reach the cap.</summary>
        private static int LowBiasedLevel(Rng rng, int max)
        {
            var r = rng.NextDouble();
            return Math.Max(1, Math.Min(max, 1 + (int)Math.Floor(r * r * max)));
        }

        private bool RunDemand()
        {
            var demand = Config.MarketBots.Demand;
            var intervalMs = Math.Max(1L, (long)Math.Round(demand.CheckIntervalMinutes * 60000.0));
            var nowTick = Now / intervalMs;
            if (State.DemandTick == 0)
            {
                State.DemandTick = nowTick;
                return true;
            }

            // Nothing new, or the PC clock went backwards: never run a check twice.
            if (nowTick <= State.DemandTick)
            {
                return false;
            }

            var first = Math.Max(State.DemandTick + 1, nowTick - demand.MaxChecksPerCatchUp + 1);
            for (var t = first; t <= nowTick && State.MyListings.Count > 0; t++)
            {
                var tickTime = t * intervalMs;
                foreach (var listing in State.MyListings.OrderBy(l => l.Id).ToList())
                {
                    if (listing.ListedAtMs >= tickTime || listing.ExpiresAtMs <= tickTime)
                    {
                        continue;
                    }

                    var rng = Rng.For(Save.RngSeed ^ DemandSalt, t, listing.Id);
                    var chance = MarketRules.PurchaseChance(Config, listing.PriceCoins, MarketRules.ReferenceValue(Config, listing.Goods));
                    if (rng.NextDouble() >= chance)
                    {
                        continue;
                    }

                    CompleteSale(listing, tickTime, RandomTraderName(rng));
                }
            }

            State.DemandTick = nowTick;
            return true;
        }

        /// <summary>A buyer took the listing: the seller gets the price minus 3% right away (GDD section 33).</summary>
        private void CompleteSale(MarketListing listing, long at, string buyer)
        {
            State.MyListings.Remove(listing);
            var fee = MarketRules.SaleFee(Config, listing.PriceCoins);
            Save.Coins += listing.PriceCoins - fee;
            AddEvent(new MarketEvent
            {
                AtMs = at,
                Kind = MarketEvent.KindSold,
                Goods = listing.Goods,
                PriceCoins = listing.PriceCoins,
                FeeCoins = fee,
                CounterpartName = buyer,
            });
            _session.Log("Listing " + listing.Id + " sold for " + listing.PriceCoins + " coins (fee " + fee + ").");
        }

        private bool ExpireMyListings()
        {
            var expired = State.MyListings.Where(l => l.ExpiresAtMs <= Now).OrderBy(l => l.ExpiresAtMs).ToList();
            foreach (var listing in expired)
            {
                State.MyListings.Remove(listing);
                AddWithdrawal(listing.Goods, WithdrawalItem.ReasonExpired, listing.ExpiresAtMs);
                AddEvent(new MarketEvent { AtMs = listing.ExpiresAtMs, Kind = MarketEvent.KindExpired, Goods = listing.Goods, PriceCoins = listing.PriceCoins });
            }

            return expired.Count > 0;
        }

        // ------------------------------------------------------------------ internals

        private ServiceError FishBlocker(long fishId)
        {
            if (Save.Expedition.Active && Save.CardumeSlots.Contains(fishId)) return ServiceError.CardumeLocked;
            if (Save.CardumeSlots.Contains(fishId)) return ServiceError.FishInCardume;
            return ServiceError.None;
        }

        private ServiceError RodBlocker(InventoryItem item)
        {
            if (!Config.TryGetRod(item.RodId, out var rod) || !rod.TradableOnMarket) return ServiceError.RodNotTradable;
            if (item.Id == Save.EquippedRodItemId) return ServiceError.RodEquipped;
            return ServiceError.None;
        }

        private ServiceError ListingBlocker(long price)
        {
            if (price < Config.Market.MinimumListingPriceCoins) return ServiceError.InvalidPrice;
            if (State.MyListings.Count >= Config.Market.MaxActiveListingsPerPlayer) return ServiceError.ListingLimitReached;
            if (Save.Coins < Config.Market.ListingFeeCoins) return ServiceError.NotEnoughCoins;
            return ServiceError.None;
        }

        private MarketListing NewPlayerListing(MarketGoods goods, long price)
        {
            Save.Coins -= Config.Market.ListingFeeCoins;
            var listing = new MarketListing
            {
                Id = State.NextListingId++,
                PriceCoins = price,
                ListedAtMs = Now,
                ExpiresAtMs = Now + (long)Math.Round(Config.Market.MaxListingDurationDays * 86400000.0),
                Goods = goods,
            };
            State.MyListings.Add(listing);
            return listing;
        }

        private WithdrawalItem AddWithdrawal(MarketGoods goods, string reason, long at)
        {
            var entry = new WithdrawalItem { Id = State.NextWithdrawalId++, Reason = reason, AtMs = at, Goods = goods };
            State.Withdrawals.Add(entry);
            return entry;
        }

        private void AddEvent(MarketEvent e)
        {
            State.Events.Insert(0, e);
            if (State.Events.Count > EventLimit)
            {
                State.Events.RemoveRange(EventLimit, State.Events.Count - EventLimit);
            }
        }

        /// <summary>Moves a custody entry into the Aquarium or Inventory, keeping its data.</summary>
        private ServiceError MoveOut(WithdrawalItem entry)
        {
            if (entry.Goods.Kind == MarketGoods.KindFish)
            {
                // The Market never bypasses the Aquarium's usable capacity (GDD section 33).
                if (Save.Aquarium.Count >= Config.AquariumCapacity)
                {
                    return ServiceError.AquariumFull;
                }

                var fish = entry.Goods.Fish;
                if (fish.Id <= 0 || fish.Id >= Save.NextFishId || Save.Aquarium.Any(f => f.Id == fish.Id))
                {
                    fish.Id = Save.NextFishId++;
                }

                fish.KeptAtMs = Now;
                Save.Aquarium.Add(fish);
            }
            else
            {
                var rod = entry.Goods.Rod;
                if (rod.Id <= 0 || rod.Id >= Save.NextItemId || Save.Inventory.Any(i => i.Id == rod.Id))
                {
                    rod.Id = Save.NextItemId++;
                }

                Save.Inventory.Add(rod);
            }

            State.Withdrawals.Remove(entry);
            _session.Log("Withdrew market entry " + entry.Id + " (" + entry.Goods.Kind + ").");
            return ServiceError.None;
        }

        private static bool Matches(ListingView v, MarketQuery q)
        {
            if (q.Kind == MarketKindFilter.Fish && !v.Goods.IsFish) return false;
            if (q.Kind == MarketKindFilter.Rods && v.Goods.IsFish) return false;
            if (q.MinPrice.HasValue && v.PriceCoins < q.MinPrice.Value) return false;
            if (q.MaxPrice.HasValue && v.PriceCoins > q.MaxPrice.Value) return false;

            var fish = v.Goods.Fish;
            var anyFishFilter = q.SpeciesId != null || q.RarityId != null || q.SizeCategoryId != null || q.MinSizeCm.HasValue || q.MaxSizeCm.HasValue;
            if (fish == null)
            {
                if (anyFishFilter) return false;
                var level = v.Goods.Rod?.Level ?? 1;
                if (q.MinLevel.HasValue && level < q.MinLevel.Value) return false;
                if (q.MaxLevel.HasValue && level > q.MaxLevel.Value) return false;
                return true;
            }

            if (q.SpeciesId != null && fish.SpeciesId != q.SpeciesId) return false;
            if (q.RarityId != null && fish.RarityId != q.RarityId) return false;
            if (q.SizeCategoryId != null && fish.SizeCategoryId != q.SizeCategoryId) return false;
            if (q.MinSizeCm.HasValue && fish.SizeCm < q.MinSizeCm.Value) return false;
            if (q.MaxSizeCm.HasValue && fish.SizeCm > q.MaxSizeCm.Value) return false;
            if (q.MinLevel.HasValue && fish.Level < q.MinLevel.Value) return false;
            if (q.MaxLevel.HasValue && fish.Level > q.MaxLevel.Value) return false;
            return true;
        }

        private ListingView ToView(MarketListing l, bool mine)
        {
            var fee = MarketRules.SaleFee(Config, l.PriceCoins);
            return new ListingView
            {
                ListingId = l.Id,
                IsMine = mine,
                SellerName = mine ? Save.PlayerName : l.SellerName,
                PriceCoins = l.PriceCoins,
                ListedAtMs = l.ListedAtMs,
                ExpiresAtMs = l.ExpiresAtMs,
                RemainingSeconds = Math.Max(0, (l.ExpiresAtMs - Now) / 1000.0),
                Goods = ToView(l.Goods),
                FeeCoins = mine ? fee : 0,
                NetCoins = mine ? l.PriceCoins - fee : 0,
            };
        }

        private WithdrawalView ToView(WithdrawalItem w)
        {
            return new WithdrawalView
            {
                WithdrawalId = w.Id,
                Reason = w.Reason,
                AtMs = w.AtMs,
                Goods = ToView(w.Goods),
                Blocker = w.Goods.Kind == MarketGoods.KindFish && Save.Aquarium.Count >= Config.AquariumCapacity ? ServiceError.AquariumFull : ServiceError.None,
            };
        }

        private MarketEventView ToView(MarketEvent e)
        {
            return new MarketEventView
            {
                AtMs = e.AtMs,
                Sold = e.Kind == MarketEvent.KindSold || e.Kind == MarketEvent.KindAuctionSold,
                Kind = e.Kind,
                GoodsName = ToView(e.Goods).Name,
                PriceCoins = e.PriceCoins,
                FeeCoins = e.FeeCoins,
                BuyerName = e.CounterpartName,
            };
        }

        internal GoodsView ToView(MarketGoods goods)
        {
            var view = new GoodsView
            {
                IsFish = goods.Kind == MarketGoods.KindFish,
                NpcValueCoins = MarketRules.NpcValue(Config, goods),
                ReferenceCoins = MarketRules.ReferenceValue(Config, goods),
            };

            if (view.IsFish)
            {
                view.Fish = _aquarium.ToView(goods.Fish);
                view.Name = view.Fish.SpeciesName;
                return view;
            }

            var item = goods.Rod;
            if (Config.TryGetRod(item.RodId, out var rod))
            {
                var bonuses = Config.RodBonusesAt(rod, item.Level);
                view.Rod = new MarketRodView
                {
                    RodId = rod.Id,
                    Name = rod.DisplayName,
                    Tier = rod.Tier,
                    HasLevels = rod.HasInternalLevels,
                    Level = item.Level,
                    MaxLevel = Config.RodMaxLevel(rod),
                    RarityBonus = bonuses.RarityEfficiency,
                    SizeBonus = bonuses.SizeQuality,
                    ShellBonus = bonuses.ShellYield,
                };
                view.Name = rod.DisplayName;
            }
            else
            {
                view.Rod = new MarketRodView { RodId = item.RodId, Name = item.RodId, Level = item.Level, MaxLevel = item.Level };
                view.Name = item.RodId;
            }

            return view;
        }
    }
}
