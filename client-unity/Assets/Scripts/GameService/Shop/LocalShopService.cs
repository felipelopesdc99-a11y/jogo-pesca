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

        /// <summary>
        /// Rods are not sold (M24-T32, A-163): the fragments the player has (extras once owned), how many assemble it (0 for
        /// the Starter Rod) and the chance per fish pulled out, the same on every map since M24-T47 (A-173).
        /// </summary>
        public long Fragments { get; internal set; }
        public int FragmentsRequired { get; internal set; }
        public double FragmentChance { get; internal set; }
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

        /// <summary>Why claiming is refused right now (only the free Starter Rod can be claimed); None when it is possible.</summary>
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

    /// <summary>
    /// The Shop's rods (GDD section 7). Since M24-T32 (A-163) no rod is sold: the free Starter Rod is claimed here in the
    /// tutorial and the others are assembled from fragments (RodFragmentRules); the Shop shows where each one drops.
    /// </summary>
    public interface IShopService
    {
        ShopView GetShop();

        /// <summary>Claims the free Starter Rod, adds it to the Inventory and equips it. Any other rod: RodNotForSale.</summary>
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
            foreach (var rod in Config.Rods.Rods.Where(r => GameConfig.IsFragmentRod(r) || (IsClaimable(r) && !Owns(r))).OrderBy(r => r.Tier))
            {
                view.Rods.Add(Offer(rod));
            }

            return view;
        }

        public ServiceResult<RodOfferView> BuyRod(string rodId)
        {
            if (!Config.TryGetRod(rodId, out var rod) || !IsClaimable(rod))
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
            var item = new InventoryItem
            {
                Id = Save.NextItemId++,
                Kind = InventoryItem.KindRod,
                RodId = rod.Id,
                Level = 1,
                AcquiredAtMs = _session.Clock.UtcNowMs,
            };
            Save.Inventory.Add(item);
            Save.EquippedRodItemId = item.Id;
            _session.Persist();
            _session.Log("Claimed the free rod " + rod.Id + " (item " + item.Id + ").");
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
                Fragments = RodFragmentRules.Have(Save, rod.Id),
                FragmentsRequired = GameConfig.IsFragmentRod(rod) ? rod.FragmentsRequired : 0,
                FragmentChance = GameConfig.IsFragmentRod(rod) ? rod.FragmentDropChance : 0,
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

        private static bool IsClaimable(RodConfig rod) => GameConfig.IsFreeClaimRod(rod);

        /// <summary>Whether the player already has this rod (Inventory, Market, auctions or Items to Withdraw).</summary>
        private bool Owns(RodConfig rod) => RodFragmentRules.Owns(Save, rod.Id);

        /// <summary>Only the free Starter Rod is claimed here; the others come from fragments (M24-T32).</summary>
        private ServiceError Blocker(RodConfig rod)
        {
            if (Owns(rod)) return ServiceError.RodAlreadyOwned;
            if (!IsClaimable(rod)) return ServiceError.RodNotForSale;
            return ServiceError.None;
        }
    }
}
