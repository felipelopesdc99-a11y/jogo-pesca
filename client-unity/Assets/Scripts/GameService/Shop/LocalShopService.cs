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
        public int UnlockFisherLevel { get; internal set; }
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
        public bool GeneratesShells { get; internal set; }
        public bool Owned { get; internal set; }

        /// <summary>The free Starter Rod, claimed rather than bought (GDD section 40).</summary>
        public bool IsFree { get; internal set; }

        /// <summary>Why buying is refused right now; None when it is possible.</summary>
        public ServiceError BuyBlocker { get; internal set; }
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
            return new RodOfferView
            {
                RodId = rod.Id,
                Name = rod.DisplayName,
                Description = rod.Description,
                Tier = rod.Tier,
                PriceCoins = rod.Acquisition.PurchaseCostCoins,
                PriceShells = rod.Acquisition.PurchaseCostShells,
                UnlockFisherLevel = rod.Acquisition.UnlockFisherLevel,
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
                GeneratesShells = rod.GeneratesShells,
                Owned = Owns(rod),
                IsFree = IsClaimable(rod),
                BuyBlocker = Blocker(rod),
            };
        }

        private static bool IsClaimable(RodConfig rod) => rod.Acquisition != null && rod.Acquisition.Method == "free_claim_in_shop";

        private bool Owns(RodConfig rod) => Save.Inventory.Any(i => i.Kind == InventoryItem.KindRod && i.RodId == rod.Id);

        private ServiceError Blocker(RodConfig rod)
        {
            if (Owns(rod)) return ServiceError.RodAlreadyOwned;
            if (Save.FisherLevel < rod.Acquisition.UnlockFisherLevel) return ServiceError.RodLocked;
            if (Save.Coins < rod.Acquisition.PurchaseCostCoins) return ServiceError.NotEnoughCoins;
            if (Save.Shells < rod.Acquisition.PurchaseCostShells) return ServiceError.NotEnoughShells;
            return ServiceError.None;
        }
    }
}
