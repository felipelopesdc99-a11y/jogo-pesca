using System;
using System.Collections.Generic;
using System.Linq;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Persistence;

namespace FishingIdle.GameService.Market
{
    /// <summary>The Auction tab of the Market (GDD section 35).</summary>
    public interface IAuctionService
    {
        AuctionsView GetAuctions();

        /// <summary>Puts an Aquarium fish or an Inventory rod up for 6 hours. It cannot be cancelled.</summary>
        ServiceResult<AuctionView> StartAuction(bool isFish, long sourceId, long startingBidCoins);

        /// <summary>
        /// Bids on another seller's auction: at least the minimum, pays the 1% fee now (never
        /// returned) and locks the bid until someone outbids you.
        /// </summary>
        ServiceResult<BidResult> PlaceBid(long auctionId, long amountCoins);

        /// <summary>The seller accepts the current highest bid early, paying 3%. Only with a bid.</summary>
        ServiceResult<AuctionView> EndAuctionNow(long auctionId);
    }

    public sealed partial class LocalMarketService
    {
        private const string PlayerBidder = "player";
        private const ulong AuctionSupplySalt = 0xA0C7_1015UL;
        private const ulong AuctionBidSalt = 0xB1D5_0B07UL;

        public AuctionsView GetAuctions()
        {
            SettleAndPersist();
            var rules = Config.Auction;
            var view = new AuctionsView
            {
                Coins = Save.Coins,
                ReservedCoins = ReservedCoins(),
                MaxActive = rules.MaxActiveAuctionsPerSeller,
                DurationHours = rules.DurationHours,
                MinIncrementRatio = rules.MinBidIncrementRatio,
                BidFeeRatio = rules.BidFeeRatio,
                EarlyCloseFeeRatio = rules.SellerEarlyCloseFeeRatio,
            };

            var mine = State.Auctions.FirstOrDefault(a => a.SellerName == null);
            view.Mine = mine == null ? null : ToView(mine);
            view.Open.AddRange(State.Auctions.Where(a => a.SellerName != null).OrderBy(a => a.EndsAtMs).ThenBy(a => a.Id).Select(ToView));
            return view;
        }

        public ServiceResult<AuctionView> StartAuction(bool isFish, long sourceId, long startingBidCoins)
        {
            Settle();
            if (State.Auctions.Count(a => a.SellerName == null) >= Config.Auction.MaxActiveAuctionsPerSeller)
            {
                return ServiceResult<AuctionView>.Fail(ServiceError.AuctionLimitReached);
            }

            if (startingBidCoins < Config.Market.MinimumListingPriceCoins)
            {
                return ServiceResult<AuctionView>.Fail(ServiceError.InvalidPrice);
            }

            MarketGoods goods;
            if (isFish)
            {
                var fish = Save.Aquarium.FirstOrDefault(f => f.Id == sourceId);
                if (fish == null) return ServiceResult<AuctionView>.Fail(ServiceError.FishNotFound);
                var blocker = FishBlocker(fish.Id);
                if (blocker != ServiceError.None) return ServiceResult<AuctionView>.Fail(blocker);
                Save.Aquarium.Remove(fish);
                goods = new MarketGoods { Kind = MarketGoods.KindFish, Fish = fish };
            }
            else
            {
                var item = Save.Inventory.FirstOrDefault(i => i.Id == sourceId && i.Kind == InventoryItem.KindRod);
                if (item == null || !Config.TryGetRod(item.RodId, out _)) return ServiceResult<AuctionView>.Fail(ServiceError.ItemNotFound);
                var blocker = RodBlocker(item);
                if (blocker != ServiceError.None) return ServiceResult<AuctionView>.Fail(blocker);
                Save.Inventory.Remove(item);
                goods = new MarketGoods { Kind = MarketGoods.KindRod, Rod = item };
            }

            var auction = new Auction
            {
                Id = State.NextAuctionId++,
                Goods = goods,
                StartingBidCoins = startingBidCoins,
                StartedAtMs = Now,
                EndsAtMs = Now + DurationMs,
            };
            State.Auctions.Add(auction);
            _session.Persist();
            _session.Log("Started auction " + auction.Id + " at " + startingBidCoins + " coins.");
            return ServiceResult<AuctionView>.Ok(ToView(auction));
        }

        public ServiceResult<BidResult> PlaceBid(long auctionId, long amountCoins)
        {
            Settle();
            var auction = State.Auctions.FirstOrDefault(a => a.Id == auctionId);
            if (auction == null)
            {
                return ServiceResult<BidResult>.Fail(ServiceError.AuctionNotFound);
            }

            if (auction.SellerName == null)
            {
                return ServiceResult<BidResult>.Fail(ServiceError.OwnListing);
            }

            if (auction.EndsAtMs <= Now)
            {
                return ServiceResult<BidResult>.Fail(ServiceError.AuctionEnded);
            }

            // A repeated request after a successful bid lands here: no second fee, no second lock.
            if (auction.HighestBidder == PlayerBidder)
            {
                return ServiceResult<BidResult>.Fail(ServiceError.AlreadyHighestBidder);
            }

            if (amountCoins < MinNextBid(auction))
            {
                return ServiceResult<BidResult>.Fail(ServiceError.BidTooLow);
            }

            var fee = BidFee(amountCoins);
            if (Save.Coins < amountCoins + fee)
            {
                return ServiceResult<BidResult>.Fail(ServiceError.NotEnoughCoins);
            }

            // The fee is burned; the principal is locked (taken from the balance) while you lead.
            Save.Coins -= amountCoins + fee;
            ApplyBid(auction, PlayerBidder, amountCoins, Now);
            auction.PlayerBid = true;
            _session.Persist();
            _session.Log("Bid " + amountCoins + " (fee " + fee + ") on auction " + auction.Id + ".");
            return ServiceResult<BidResult>.Ok(new BidResult { Auction = ToView(auction), FeeCoins = fee, NewBalance = Save.Coins });
        }

        public ServiceResult<AuctionView> EndAuctionNow(long auctionId)
        {
            Settle();
            var auction = State.Auctions.FirstOrDefault(a => a.Id == auctionId && a.SellerName == null);
            if (auction == null)
            {
                return ServiceResult<AuctionView>.Fail(ServiceError.AuctionNotFound);
            }

            if (auction.BidCount == 0)
            {
                return ServiceResult<AuctionView>.Fail(ServiceError.AuctionHasNoBids);
            }

            var view = ToView(auction);
            Finish(auction, Now, Config.Auction.SellerEarlyCloseFeeRatio);

            // The player asked for this and gets the answer now; no second notice later.
            State.Events[0].Notified = true;
            _session.Persist();
            return ServiceResult<AuctionView>.Ok(view);
        }

        // ------------------------------------------------------------------ simulation

        private long DurationMs => (long)Math.Round(Config.Auction.DurationHours * 3600000.0);

        private bool RunAuctions()
        {
            var changed = RunAuctionSupply();
            changed |= RunBotBids();
            foreach (var auction in State.Auctions.Where(a => a.EndsAtMs <= Now).OrderBy(a => a.EndsAtMs).ToList())
            {
                Finish(auction, auction.EndsAtMs, Config.Auction.TimedCompletionFeeRatio);
                changed = true;
            }

            return changed;
        }

        private bool RunAuctionSupply()
        {
            var bots = Config.MarketBots.Auctions;
            var intervalMs = Math.Max(1L, (long)Math.Round(bots.RefreshIntervalMinutes * 60000.0));
            var nowTick = Now / intervalMs;
            if (State.AuctionSupplyTick == 0)
            {
                // First visit: auctions already running, at different stages.
                for (var i = 0; i < bots.TargetAuctionCount; i++)
                {
                    var rng = NextAuctionRng();
                    State.Auctions.Add(NewBotAuction(rng, Now - (long)(rng.NextDouble() * DurationMs * 0.8)));
                }

                State.AuctionSupplyTick = nowTick;
                return true;
            }

            if (nowTick <= State.AuctionSupplyTick)
            {
                return false;
            }

            var maxTicks = DurationMs / intervalMs + 1;
            for (var t = Math.Max(State.AuctionSupplyTick + 1, nowTick - maxTicks + 1); t <= nowTick; t++)
            {
                var tickTime = t * intervalMs;
                var active = State.Auctions.Count(a => a.SellerName != null && a.EndsAtMs > tickTime);
                var room = Math.Min(bots.NewAuctionsPerRefresh, bots.TargetAuctionCount - active);
                for (var i = 0; i < room; i++)
                {
                    State.Auctions.Add(NewBotAuction(NextAuctionRng(), tickTime));
                }
            }

            State.AuctionSupplyTick = nowTick;
            return true;
        }

        private Rng NextAuctionRng()
        {
            State.BotAuctionsCreated++;
            return Rng.For(Save.RngSeed ^ AuctionSupplySalt, State.BotAuctionsCreated, 0);
        }

        private Auction NewBotAuction(Rng rng, long startedAt)
        {
            var bots = Config.MarketBots.Auctions;
            var seller = RandomTraderName(rng);
            var goods = NewBotGoods(rng, bots.RodAuctionChance, startedAt);
            var ratio = bots.StartingBidRatio.Min + rng.NextDouble() * (bots.StartingBidRatio.Max - bots.StartingBidRatio.Min);

            // Never below what the game pays, so winning at the starting bid cannot be resold to the game at a profit.
            var start = Math.Max(MarketRules.NpcValue(Config, goods) + 1, (long)Math.Round(MarketRules.ReferenceValue(Config, goods) * ratio, MidpointRounding.AwayFromZero));
            return new Auction
            {
                Id = State.NextAuctionId++,
                SellerName = seller,
                Goods = goods,
                StartingBidCoins = start,
                StartedAtMs = startedAt,
                EndsAtMs = startedAt + DurationMs,
            };
        }

        /// <summary>Simulated bidders, on every auction (the player's included), in time order.</summary>
        private bool RunBotBids()
        {
            var bots = Config.MarketBots.Auctions;
            var intervalMs = Math.Max(1L, (long)Math.Round(bots.BidCheckIntervalMinutes * 60000.0));
            var nowTick = Now / intervalMs;
            if (State.AuctionBidTick == 0)
            {
                State.AuctionBidTick = nowTick;
                return true;
            }

            // Nothing new, or the PC clock went backwards: never run a check twice.
            if (nowTick <= State.AuctionBidTick)
            {
                return false;
            }

            for (var t = Math.Max(State.AuctionBidTick + 1, nowTick - bots.MaxChecksPerCatchUp + 1); t <= nowTick; t++)
            {
                var tickTime = t * intervalMs;
                foreach (var auction in State.Auctions.OrderBy(a => a.Id).ToList())
                {
                    if (auction.StartedAtMs >= tickTime || auction.EndsAtMs <= tickTime)
                    {
                        continue;
                    }

                    var rng = Rng.For(Save.RngSeed ^ AuctionBidSalt, t, auction.Id);
                    if (rng.NextDouble() >= bots.BidChancePerCheck)
                    {
                        continue;
                    }

                    var ceiling = (long)Math.Floor(MarketRules.ReferenceValue(Config, auction.Goods) * bots.MaxBidRatio);
                    var minimum = MinNextBid(auction);
                    if (minimum > ceiling)
                    {
                        continue;
                    }

                    var extra = bots.ExtraRaiseRatio.Min + rng.NextDouble() * (bots.ExtraRaiseRatio.Max - bots.ExtraRaiseRatio.Min);
                    var amount = Math.Min(ceiling, Math.Max(minimum, (long)Math.Ceiling(minimum * (1.0 + extra))));
                    var bidder = RandomTraderNameExcept(rng, auction.SellerName, auction.HighestBidder);
                    if (bidder == null)
                    {
                        continue;
                    }

                    ApplyBid(auction, bidder, amount, tickTime);
                }
            }

            State.AuctionBidTick = nowTick;
            return true;
        }

        /// <summary>
        /// Records a new highest bid: the previous leader, if it was the player, gets the principal back
        /// immediately; a bid in the last minute puts the clock back to one minute (GDD section 35).
        /// </summary>
        private void ApplyBid(Auction auction, string bidder, long amount, long at)
        {
            if (auction.HighestBidder == PlayerBidder)
            {
                Save.Coins += auction.HighestBidCoins;
                AddEvent(new MarketEvent { AtMs = at, Kind = MarketEvent.KindOutbid, Goods = auction.Goods, PriceCoins = amount, CounterpartName = bidder });
            }

            auction.HighestBidCoins = amount;
            auction.HighestBidder = bidder;
            auction.BidCount++;

            var rules = Config.Auction;
            var windowMs = (long)Math.Round(rules.AntiSnipeWindowSeconds * 1000.0);
            if (auction.EndsAtMs - at < windowMs)
            {
                auction.EndsAtMs = at + (long)Math.Round(rules.AntiSnipeResetToSeconds * 1000.0);
            }
        }

        /// <summary>Closes an auction: the highest bid wins; with none, the seller's goods go to Items to Withdraw.</summary>
        private void Finish(Auction auction, long at, double sellerFeeRatio)
        {
            State.Auctions.Remove(auction);
            var mine = auction.SellerName == null;
            var hasBid = auction.BidCount > 0;

            if (mine && hasBid)
            {
                var fee = (long)Math.Round(auction.HighestBidCoins * sellerFeeRatio, MidpointRounding.AwayFromZero);
                Save.Coins += auction.HighestBidCoins - fee;
                AddEvent(new MarketEvent { AtMs = at, Kind = MarketEvent.KindAuctionSold, Goods = auction.Goods, PriceCoins = auction.HighestBidCoins, FeeCoins = fee, CounterpartName = auction.HighestBidder });
            }
            else if (mine)
            {
                AddWithdrawal(auction.Goods, WithdrawalItem.ReasonAuctionUnsold, at);
                AddEvent(new MarketEvent { AtMs = at, Kind = MarketEvent.KindAuctionUnsold, Goods = auction.Goods, PriceCoins = auction.StartingBidCoins });
            }
            else if (auction.HighestBidder == PlayerBidder)
            {
                // The locked principal is the payment; the goods wait in Items to Withdraw.
                if (auction.Goods.Kind == MarketGoods.KindRod)
                {
                    auction.Goods.Rod.PurchasePriceCoins = auction.HighestBidCoins;
                    auction.Goods.Rod.UpgradeCoinsInvested = 0;
                }

                AddWithdrawal(auction.Goods, WithdrawalItem.ReasonAuctionWon, at);
                AddEvent(new MarketEvent { AtMs = at, Kind = MarketEvent.KindAuctionWon, Goods = auction.Goods, PriceCoins = auction.HighestBidCoins, CounterpartName = auction.SellerName });
            }
            else if (auction.PlayerBid)
            {
                AddEvent(new MarketEvent { AtMs = at, Kind = MarketEvent.KindAuctionLost, Goods = auction.Goods, PriceCoins = auction.HighestBidCoins, CounterpartName = auction.HighestBidder });
            }

            _session.Log("Auction " + auction.Id + " finished at " + auction.HighestBidCoins + " coins (" + (auction.HighestBidder ?? "no bids") + ").");
        }

        private long MinNextBid(Auction auction)
        {
            if (auction.BidCount == 0)
            {
                return auction.StartingBidCoins;
            }

            return Math.Max(auction.HighestBidCoins + 1, (long)Math.Ceiling(auction.HighestBidCoins * (1.0 + Config.Auction.MinBidIncrementRatio) - 1e-9));
        }

        private long BidFee(long amount)
        {
            return (long)Math.Round(amount * Config.Auction.BidFeeRatio, MidpointRounding.AwayFromZero);
        }

        private long ReservedCoins()
        {
            return State.Auctions.Where(a => a.HighestBidder == PlayerBidder).Sum(a => a.HighestBidCoins);
        }

        private AuctionView ToView(Auction a)
        {
            var min = MinNextBid(a);
            var earlyFee = (long)Math.Round(a.HighestBidCoins * Config.Auction.SellerEarlyCloseFeeRatio, MidpointRounding.AwayFromZero);
            return new AuctionView
            {
                AuctionId = a.Id,
                IsMine = a.SellerName == null,
                SellerName = a.SellerName ?? Save.PlayerName,
                Goods = ToView(a.Goods),
                StartingBidCoins = a.StartingBidCoins,
                HighestBidCoins = a.HighestBidCoins,
                HighestBidderName = a.HighestBidder == PlayerBidder ? Save.PlayerName : a.HighestBidder,
                PlayerIsHighest = a.HighestBidder == PlayerBidder,
                PlayerHasBid = a.PlayerBid,
                BidCount = a.BidCount,
                MinNextBidCoins = min,
                MinNextBidFeeCoins = BidFee(min),
                EndsAtMs = a.EndsAtMs,
                RemainingSeconds = Math.Max(0, (a.EndsAtMs - Now) / 1000.0),
                CanEndNow = a.SellerName == null && a.BidCount > 0,
                EndNowNetCoins = a.HighestBidCoins - earlyFee,
            };
        }
    }
}
