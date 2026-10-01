using System;
using System.Collections.Generic;
using System.Linq;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;
using FishingIdle.GameService.Persistence;

namespace FishingIdle.GameService.Shop
{
    public sealed class BoatOfferView
    {
        public string BoatId { get; internal set; }
        public string Name { get; internal set; }
        public int Tier { get; internal set; }
        public double Bonus { get; internal set; }
        public long CostCoins { get; internal set; }
        public long CostShells { get; internal set; }
        public int UnlockFisherLevel { get; internal set; }
        public bool Owned { get; internal set; }
        public bool InUse { get; internal set; }

        /// <summary>Why buying is refused right now; None when it is possible.</summary>
        public ServiceError BuyBlocker { get; internal set; }
    }

    public sealed class BaitOfferView
    {
        public string BaitId { get; internal set; }
        public string Name { get; internal set; }
        public int Tier { get; internal set; }
        public double Bonus { get; internal set; }

        /// <summary>Attempts one purchase lasts.</summary>
        public int ChargesPerPurchase { get; internal set; }
        public int ChargesLeft { get; internal set; }
        public long CostCoins { get; internal set; }
        public long CostShells { get; internal set; }
        public int UnlockFisherLevel { get; internal set; }
        public bool InUse { get; internal set; }
        public ServiceError BuyBlocker { get; internal set; }
    }

    /// <summary>The chance of pulling out a fish of one rarity with the gear in use.</summary>
    public sealed class RarityChanceView
    {
        public string RarityId { get; internal set; }
        public string RarityName { get; internal set; }
        public double Chance { get; internal set; }

        /// <summary>Whether this rarity bites here at all: the current map has it and the rod can catch it.</summary>
        public bool BitesHere { get; internal set; }
    }

    /// <summary>Rod, boat and bait in use, what each adds, and the resulting chances (docs/SISTEMA_SUCESSO_PESCA.md).</summary>
    public sealed class GearView
    {
        public long Coins { get; internal set; }
        public long Shells { get; internal set; }
        public string RodName { get; internal set; }
        public double RodBonus { get; internal set; }
        public string BoatId { get; internal set; }
        public string BoatName { get; internal set; }
        public double BoatBonus { get; internal set; }

        /// <summary>The bait in use, or null.</summary>
        public string BaitId { get; internal set; }
        public string BaitName { get; internal set; }
        public double BaitBonus { get; internal set; }
        public int BaitChargesLeft { get; internal set; }
        public double TotalBonus => RodBonus + BoatBonus + BaitBonus;

        /// <summary>Floor and ceiling of the chance (config).</summary>
        public double ChanceMin { get; internal set; }
        public double ChanceMax { get; internal set; }
        public List<RarityChanceView> Chances { get; } = new List<RarityChanceView>();
        public List<BoatOfferView> Boats { get; } = new List<BoatOfferView>();
        public List<BaitOfferView> Baits { get; } = new List<BaitOfferView>();
    }

    /// <summary>Boats and baits (docs/SISTEMA_SUCESSO_PESCA.md): bought with Coins and Shells, they raise the Catch Success.</summary>
    public interface IGearService
    {
        GearView GetGear();

        /// <summary>Buys a boat and starts using it.</summary>
        ServiceResult<GearView> BuyBoat(string boatId);

        /// <summary>Switches to a boat already owned.</summary>
        ServiceResult<GearView> UseBoat(string boatId);

        /// <summary>Buys one batch of a bait (its charges add up); it is put in use when no bait is.</summary>
        ServiceResult<GearView> BuyBait(string baitId);

        /// <summary>Puts a bait with charges in use; null stops using bait (the charges are kept).</summary>
        ServiceResult<GearView> UseBait(string baitId);
    }

    public sealed class LocalGearService : IGearService
    {
        private readonly GameSession _session;
        private readonly IFishingService _fishing;

        public LocalGearService(GameSession session, IFishingService fishing)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _fishing = fishing ?? throw new ArgumentNullException(nameof(fishing));
        }

        private PlayerSave Save => _session.Save;
        private GameConfig Config => _session.Config;

        public GearView GetGear()
        {
            var config = Config;
            var save = Save;
            var rod = config.TryGetRod(save.EquippedRodItem()?.RodId, out var equipped) ? equipped : config.StarterRod;
            var rodLevel = save.EquippedRodItem()?.Level ?? 1;
            var boat = GearRules.ActiveBoat(config, save);
            var bait = GearRules.ActiveBait(config, save);
            var map = config.TryGetMap(save.CurrentMapId, out var current) ? current : config.StartingMap;

            var view = new GearView
            {
                Coins = save.Coins,
                Shells = save.Shells,
                RodName = rod.DisplayName,
                RodBonus = config.RodBonusesAt(rod, rodLevel).CatchSuccess,
                BoatId = boat.Id,
                BoatName = boat.DisplayName,
                BoatBonus = boat.CatchSuccessBonus,
                BaitId = bait?.Id,
                BaitName = bait?.DisplayName,
                BaitBonus = bait?.CatchSuccessBonus ?? 0.0,
                BaitChargesLeft = bait != null ? GearRules.Charges(save, bait.Id) : 0,
                ChanceMin = config.Fishing.CatchSuccessMin,
                ChanceMax = config.Fishing.CatchSuccessMax,
            };

            foreach (var tier in config.Progression.Rarity.Tiers)
            {
                view.Chances.Add(new RarityChanceView
                {
                    RarityId = tier.Id,
                    RarityName = tier.DisplayName,
                    Chance = CatchRules.SuccessChance(config, tier.Id, view.TotalBonus),
                    BitesHere = map.AvailableRarities != null && map.AvailableRarities.Contains(tier.Id)
                        && rod.CanCatchRarities != null && rod.CanCatchRarities.Contains(tier.Id),
                });
            }

            foreach (var b in config.Equipment.Boats.OrderBy(b => b.Tier))
            {
                view.Boats.Add(new BoatOfferView
                {
                    BoatId = b.Id,
                    Name = b.DisplayName,
                    Tier = b.Tier,
                    Bonus = b.CatchSuccessBonus,
                    CostCoins = b.CostCoins,
                    CostShells = b.CostShells,
                    UnlockFisherLevel = b.UnlockFisherLevel,
                    Owned = OwnsBoat(b),
                    InUse = b.Id == boat.Id,
                    BuyBlocker = BoatBlocker(b),
                });
            }

            foreach (var b in config.Equipment.Baits.OrderBy(b => b.Tier))
            {
                view.Baits.Add(new BaitOfferView
                {
                    BaitId = b.Id,
                    Name = b.DisplayName,
                    Tier = b.Tier,
                    Bonus = b.CatchSuccessBonus,
                    ChargesPerPurchase = b.Charges,
                    ChargesLeft = GearRules.Charges(save, b.Id),
                    CostCoins = b.CostCoins,
                    CostShells = b.CostShells,
                    UnlockFisherLevel = b.UnlockFisherLevel,
                    InUse = bait != null && b.Id == bait.Id,
                    BuyBlocker = BaitBlocker(b),
                });
            }

            return view;
        }

        public ServiceResult<GearView> BuyBoat(string boatId)
        {
            if (!Config.TryGetBoat(boatId, out var boat))
            {
                return ServiceResult<GearView>.Fail(ServiceError.BoatNotFound);
            }

            var blocker = BoatBlocker(boat);
            if (blocker != ServiceError.None)
            {
                return ServiceResult<GearView>.Fail(blocker);
            }

            // Cycles completed with the old boat are settled before the new one takes over.
            _fishing.Sync();
            Save.Coins -= boat.CostCoins;
            Save.Shells -= boat.CostShells;
            Save.OwnedBoatIds.Add(boat.Id);
            Save.BoatId = boat.Id;
            _session.Persist();
            _session.Log("Bought boat " + boat.Id + " for " + boat.CostCoins + " coins and " + boat.CostShells + " shells.");
            return ServiceResult<GearView>.Ok(GetGear());
        }

        public ServiceResult<GearView> UseBoat(string boatId)
        {
            if (!Config.TryGetBoat(boatId, out var boat))
            {
                return ServiceResult<GearView>.Fail(ServiceError.BoatNotFound);
            }

            if (!OwnsBoat(boat))
            {
                return ServiceResult<GearView>.Fail(ServiceError.BoatNotOwned);
            }

            _fishing.Sync();
            Save.BoatId = boat.Id;
            _session.Persist();
            return ServiceResult<GearView>.Ok(GetGear());
        }

        public ServiceResult<GearView> BuyBait(string baitId)
        {
            if (!Config.TryGetBait(baitId, out var bait))
            {
                return ServiceResult<GearView>.Fail(ServiceError.BaitNotFound);
            }

            var blocker = BaitBlocker(bait);
            if (blocker != ServiceError.None)
            {
                return ServiceResult<GearView>.Fail(blocker);
            }

            _fishing.Sync();
            Save.Coins -= bait.CostCoins;
            Save.Shells -= bait.CostShells;
            Save.BaitCharges[bait.Id] = GearRules.Charges(Save, bait.Id) + bait.Charges;
            if (GearRules.ActiveBait(Config, Save) == null)
            {
                Save.ActiveBaitId = bait.Id;
            }

            _session.Persist();
            _session.Log("Bought bait " + bait.Id + " (" + bait.Charges + " charges) for " + bait.CostCoins + " coins and " + bait.CostShells + " shells.");
            return ServiceResult<GearView>.Ok(GetGear());
        }

        public ServiceResult<GearView> UseBait(string baitId)
        {
            if (baitId != null)
            {
                if (!Config.TryGetBait(baitId, out var bait))
                {
                    return ServiceResult<GearView>.Fail(ServiceError.BaitNotFound);
                }

                if (GearRules.Charges(Save, bait.Id) <= 0)
                {
                    return ServiceResult<GearView>.Fail(ServiceError.BaitNoCharges);
                }
            }

            _fishing.Sync();
            Save.ActiveBaitId = baitId;
            _session.Persist();
            return ServiceResult<GearView>.Ok(GetGear());
        }

        private bool OwnsBoat(BoatConfig boat) => boat.Id == Config.StarterBoat.Id || Save.OwnedBoatIds.Contains(boat.Id);

        private ServiceError BoatBlocker(BoatConfig boat)
        {
            if (OwnsBoat(boat)) return ServiceError.BoatAlreadyOwned;
            if (Save.FisherLevel < boat.UnlockFisherLevel) return ServiceError.BoatLocked;
            if (Save.Coins < boat.CostCoins) return ServiceError.NotEnoughCoins;
            if (Save.Shells < boat.CostShells) return ServiceError.NotEnoughShells;
            return ServiceError.None;
        }

        private ServiceError BaitBlocker(BaitConfig bait)
        {
            if (Save.FisherLevel < bait.UnlockFisherLevel) return ServiceError.BaitLocked;
            if (Save.Coins < bait.CostCoins) return ServiceError.NotEnoughCoins;
            if (Save.Shells < bait.CostShells) return ServiceError.NotEnoughShells;
            return ServiceError.None;
        }
    }
}
