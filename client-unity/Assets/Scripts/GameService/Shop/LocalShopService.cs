using System;
using System.Collections.Generic;
using System.Linq;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;
using FishingIdle.GameService.Persistence;

namespace FishingIdle.GameService.Shop
{
    public sealed class RodOfferView
    {
        public string RodId { get; internal set; }
        public string Name { get; internal set; }
        public string Description { get; internal set; }
        public int Tier { get; internal set; }
        public long PriceCoins { get; internal set; }
        public long PriceShells { get; internal set; }
        public int MaxLevel { get; internal set; }
        public double RarityBonus { get; internal set; }
        public double SizeBonus { get; internal set; }
        public double ShellBonus { get; internal set; }
        public double RarityBonusAtMax { get; internal set; }
        public double SizeBonusAtMax { get; internal set; }
        public double ShellBonusAtMax { get; internal set; }

        /// <summary>Points added to the Catch Success chance (docs/SISTEMA_SUCESSO_PESCA.md).</summary>
        public double CatchBonus { get; internal set; }
        public double CatchBonusAtMax { get; internal set; }
        public bool CanCatchRare { get; internal set; }
        public bool CanCatchEpic { get; internal set; }
        public bool CanCatchLegendary { get; internal set; }
        public bool CanCatchMythic { get; internal set; }
        public bool GeneratesShells { get; internal set; }
        public bool Owned { get; internal set; }

        /// <summary>The free Starter Rod, claimed rather than bought (GDD section 40).</summary>
        public bool IsFree { get; internal set; }

        /// <summary>Why buying is refused right now; None when it is possible.</summary>
        public ServiceError BuyBlocker { get; internal set; }

        /// <summary>The player's copy in the Inventory (the highest level, if more than one); 0 when none (M24-T13).</summary>
        public long OwnedItemId { get; internal set; }

        /// <summary>Level of the player's copy; 0 when none.</summary>
        public int OwnedLevel { get; internal set; }

        /// <summary>Bonuses of the player's copy at its level (Nv.1 values when none).</summary>
        public double CatchBonusNow { get; internal set; }
        public double RarityBonusNow { get; internal set; }
        public double SizeBonusNow { get; internal set; }
        public double ShellBonusNow { get; internal set; }

        /// <summary>Price of the copy's next level; 0 at the max level, without levels or without a copy.</summary>
        public long NextUpgradeCost { get; internal set; }
        public long NextUpgradeShells { get; internal set; }

        /// <summary>Why the copy cannot go one level up right now; None when it can.</summary>
        public ServiceError UpgradeBlocker { get; internal set; }
    }

    public sealed class ShopView
    {
        public long Coins { get; internal set; }
        public List<RodOfferView> Rods { get; } = new List<RodOfferView>();
    }

    /// <summary>The Shop (GDD section 7). V0.1 sells only rods.</summary>
    public interface IShopService
    {
        ShopView GetShop();

        /// <summary>Buys a rod (or claims the free Starter Rod), adds it to the Inventory and equips it.</summary>
        ServiceResult<RodOfferView> BuyRod(string rodId);
    }

    public sealed class LocalShopService : IShopService
    {
        private readonly GameSession _session;
        private readonly IFishingService _fishing;

        public LocalShopService(GameSession session, IFishingService fishing)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _fishing = fishing ?? throw new ArgumentNullException(nameof(fishing));
        }

        private PlayerSave Save => _session.Save;
        private GameConfig Config => _session.Config;

        public ShopView GetShop()
        {
            var view = new ShopView { Coins = Save.Coins };
            foreach (var rod in Config.Rods.Rods.Where(r => Config.IsPurchasable(r) || (IsClaimable(r) && !Owns(r))).OrderBy(r => r.Tier))
            {
                view.Rods.Add(Offer(rod));
            }

            return view;
        }

        public ServiceResult<RodOfferView> BuyRod(string rodId)
        {
            if (!Config.TryGetRod(rodId, out var rod) || !(Config.IsPurchasable(rod) || IsClaimable(rod)))
            {
                return ServiceResult<RodOfferView>.Fail(ServiceError.RodNotForSale);
            }

            var blocker = Blocker(rod);
            if (blocker != ServiceError.None)
            {
                return ServiceResult<RodOfferView>.Fail(blocker);
            }

            // Catches completed with the old rod are settled before the new one takes over.
            _fishing.Sync();
            var price = rod.Acquisition.PurchaseCostCoins;
            var shells = rod.Acquisition.PurchaseCostShells;
            Save.Coins -= price;
            Save.Shells -= shells;
            var item = new InventoryItem
            {
                Id = Save.NextItemId++,
                Kind = InventoryItem.KindRod,
                RodId = rod.Id,
                Level = 1,
                AcquiredAtMs = _session.Clock.UtcNowMs,
                PurchasePriceCoins = price,
            };
            Save.Inventory.Add(item);
            Save.EquippedRodItemId = item.Id;
            _session.Persist();
            _session.Log("Bought rod " + rod.Id + " for " + price + " coins and " + shells + " shells (item " + item.Id + ").");
            return ServiceResult<RodOfferView>.Ok(Offer(rod));
        }

        private RodOfferView Offer(RodConfig rod)
        {
            var at1 = Config.RodBonusesAt(rod, 1);
            var max = Config.RodMaxLevel(rod);
            var atMax = Config.RodBonusesAt(rod, max);
            var copy = Save.Inventory.Where(i => i.Kind == InventoryItem.KindRod && i.RodId == rod.Id).OrderByDescending(i => i.Level).FirstOrDefault();
            var now = Config.RodBonusesAt(rod, copy?.Level ?? 1);
            var nextCoins = copy != null && rod.HasInternalLevels ? Config.RodUpgradeCost(rod, copy.Level) : 0;
            var nextShells = copy != null && rod.HasInternalLevels ? Config.RodUpgradeShellCost(rod, copy.Level) : 0;
            return new RodOfferView
            {
                RodId = rod.Id,
                Name = rod.DisplayName,
                Description = rod.Description,
                Tier = rod.Tier,
                PriceCoins = rod.Acquisition.PurchaseCostCoins,
                PriceShells = rod.Acquisition.PurchaseCostShells,
                MaxLevel = max,
                RarityBonus = at1.RarityEfficiency,
                SizeBonus = at1.SizeQuality,
                ShellBonus = at1.ShellYield,
                RarityBonusAtMax = atMax.RarityEfficiency,
                SizeBonusAtMax = atMax.SizeQuality,
                ShellBonusAtMax = atMax.ShellYield,
                CatchBonus = at1.CatchSuccess,
                CatchBonusAtMax = atMax.CatchSuccess,
                CanCatchRare = rod.CanCatchRarities != null && rod.CanCatchRarities.Any(r => Config.RarityRank(r) > 0),
                CanCatchEpic = rod.CanCatchRarities != null && rod.CanCatchRarities.Any(r => Config.RarityRank(r) > 1),
                CanCatchLegendary = rod.CanCatchRarities != null && rod.CanCatchRarities.Any(r => Config.RarityRank(r) > 2),
                CanCatchMythic = rod.CanCatchRarities != null && rod.CanCatchRarities.Any(r => Config.RarityRank(r) > 3),
                GeneratesShells = rod.GeneratesShells,
                Owned = Owns(rod),
                IsFree = IsClaimable(rod),
                BuyBlocker = Blocker(rod),
                OwnedItemId = copy?.Id ?? 0,
                OwnedLevel = copy?.Level ?? 0,
                CatchBonusNow = now.CatchSuccess,
                RarityBonusNow = now.RarityEfficiency,
                SizeBonusNow = now.SizeQuality,
                ShellBonusNow = now.ShellYield,
                NextUpgradeCost = nextCoins,
                NextUpgradeShells = nextShells,
                UpgradeBlocker = CopyUpgradeBlocker(rod, copy, nextCoins, nextShells),
            };
        }

        /// <summary>The same checks as the upgrade itself (LocalProfileService.UpgradeRod), for the Shop's button.</summary>
        private ServiceError CopyUpgradeBlocker(RodConfig rod, InventoryItem copy, long coins, long shells)
        {
            if (copy == null) return ServiceError.ItemNotFound;
            if (!rod.HasInternalLevels) return ServiceError.RodHasNoLevels;
            if (coins <= 0) return ServiceError.RodAtMaxLevel;
            if (Save.Coins < coins) return ServiceError.NotEnoughCoins;
            if (Save.Shells < shells) return ServiceError.NotEnoughShells;
            return ServiceError.None;
        }

        private static bool IsClaimable(RodConfig rod) => rod.Acquisition != null && rod.Acquisition.Method == "free_claim_in_shop";

        /// <summary>
        /// Whether the player already has this rod: in the Inventory, but also on the Market, in one of
        /// their auctions or waiting in Items to Withdraw. Otherwise a listed rod could be bought again
        /// in the Shop, over and over.
        /// </summary>
        private bool Owns(RodConfig rod)
        {
            bool Same(MarketGoods g) => g != null && g.Kind == MarketGoods.KindRod && g.Rod != null && g.Rod.RodId == rod.Id;
            var market = Save.Market;
            return Save.Inventory.Any(i => i.Kind == InventoryItem.KindRod && i.RodId == rod.Id)
                || (market != null && (market.MyListings.Any(l => Same(l.Goods))
                    || market.Withdrawals.Any(w => Same(w.Goods))
                    || market.Auctions.Any(a => a.SellerName == null && Same(a.Goods))));
        }

        /// <summary>Items have no minimum Fisher level (M24-T09, A-153): only owning it or the money can block.</summary>
        private ServiceError Blocker(RodConfig rod)
        {
            if (Owns(rod)) return ServiceError.RodAlreadyOwned;
            if (Save.Coins < rod.Acquisition.PurchaseCostCoins) return ServiceError.NotEnoughCoins;
            if (Save.Shells < rod.Acquisition.PurchaseCostShells) return ServiceError.NotEnoughShells;
            return ServiceError.None;
        }
    }
}
