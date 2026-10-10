using System;
using System.Collections.Generic;
using System.Linq;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Crew;
using FishingIdle.GameService.Persistence;

namespace FishingIdle.GameService.Upgrades
{
    /// <summary>
    /// Upgrades bought with Moedas (M24-T06, A-155): each Crew member's ×2s, unlocked by its units and bought once, and
    /// the general upgrades with levels (Rádio do Porto, Freguesia na Feira, Caixa Térmica, Sonar de Cardume, Maré Boa).
    /// None asks for a Fisher level.
    /// </summary>
    /// <remarks>
    /// The client only asks: look at the Upgrades, buy one. Prices, locks and effects come from <see cref="UpgradeRules"/>;
    /// the effects are applied where the income is computed (CrewRules, the NPC fish sale, the Crew's offline cap). The
    /// Crew's income owed is credited before a purchase, so what it earned until then stays at the old rate.
    /// </remarks>
    public interface IUpgradeService
    {
        UpgradesView GetUpgrades();

        /// <summary>Buys a Crew member's upgrade or the next level of a general one.</summary>
        ServiceResult<UpgradeBuyResult> Buy(string upgradeId);
    }

    public sealed class LocalUpgradeService : IUpgradeService
    {
        /// <summary>The window shows only this many of the Crew members' upgrades, so it does not fill up (A-155).</summary>
        public const int CrewOffersShown = 3;

        /// <summary>The effect key of a Crew member's ×2 (the general ones use upgrades.json → effect).</summary>
        public const string CrewMemberEffect = "crew_member";

        private readonly GameSession _session;
        private readonly LocalCrewService _crew;

        public LocalUpgradeService(GameSession session, LocalCrewService crew)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _crew = crew ?? throw new ArgumentNullException(nameof(crew));
        }

        private PlayerSave Save => _session.Save;
        private GameConfig Config => _session.Config;

        public UpgradesView GetUpgrades()
        {
            var config = Config;
            var upgrades = Save.Upgrades;
            var view = new UpgradesView
            {
                Coins = Save.Coins,
                CrewMultiplier = config.Upgrades.CrewUpgrades.Multiplier,
            };
            view.CrewUnlockCounts.AddRange(config.Upgrades.CrewUpgrades.Tiers.Select(t => t.UnlockCount));

            // Crew members' upgrades: bought ones to the list of bought; the rest are offers, unlocked first.
            var unlocked = new List<UpgradeOfferView>();
            var locked = new List<UpgradeOfferView>();
            foreach (var member in config.Crew.Members)
            {
                for (var i = 0; i < config.Upgrades.CrewUpgrades.Tiers.Count; i++)
                {
                    view.CrewTotalCount++;
                    var offer = CrewOffer(config, member, i);
                    if (offer.Level > 0)
                    {
                        view.CrewBoughtCount++;
                        view.Bought.Add(offer);
                    }
                    else if (offer.BuyBlocker == ServiceError.UpgradeLocked)
                    {
                        locked.Add(offer);
                    }
                    else
                    {
                        unlocked.Add(offer);
                    }
                }
            }

            // The cheapest first; the order of crew.json breaks ties (OrderBy is stable).
            var next = unlocked.OrderBy(o => o.Cost).Concat(locked.OrderBy(o => o.Cost)).Take(CrewOffersShown).ToList();
            view.NextCrew.AddRange(next);
            view.CrewAvailableCount = Math.Max(0, unlocked.Count - next.Count(o => o.BuyBlocker != ServiceError.UpgradeLocked));

            foreach (var upgrade in config.Upgrades.GeneralUpgrades)
            {
                var offer = GeneralOffer(upgrade, upgrades);
                view.General.Add(offer);
                if (offer.Level > 0)
                {
                    view.Bought.Add(offer);
                }
            }

            return view;
        }

        public ServiceResult<UpgradeBuyResult> Buy(string upgradeId)
        {
            var config = Config;
            if (config.TryGetGeneralUpgrade(upgradeId, out var general))
            {
                return BuyGeneral(config, general);
            }

            if (UpgradeRules.TryGetCrewUpgrade(config, upgradeId, out var member, out var tier))
            {
                return BuyCrew(config, upgradeId, member, tier);
            }

            return ServiceResult<UpgradeBuyResult>.Fail(ServiceError.UpgradeNotFound);
        }

        // ------------------------------------------------------------------ internals

        private ServiceResult<UpgradeBuyResult> BuyCrew(GameConfig config, string upgradeId, CrewMemberConfig member, int tier)
        {
            if (Save.Upgrades.LevelOf(upgradeId) > 0)
            {
                return ServiceResult<UpgradeBuyResult>.Fail(ServiceError.UpgradeAlreadyBought);
            }

            if (!UpgradeRules.IsCrewUpgradeUnlocked(config, Save.Crew, member, tier))
            {
                return ServiceResult<UpgradeBuyResult>.Fail(ServiceError.UpgradeLocked);
            }

            // What the Crew earned up to now is at the old rate (and its Moedas can pay for this).
            _crew.SettleNow();
            var cost = UpgradeRules.CrewUpgradeCost(config, member, tier);
            if (!CrewRules.CanPay(cost, Save.Coins))
            {
                return ServiceResult<UpgradeBuyResult>.Fail(ServiceError.NotEnoughCoins);
            }

            Save.Coins -= cost;
            Save.Upgrades.Levels[upgradeId] = 1;
            var name = UpgradeRules.CrewUpgradeName(config, member, tier);
            return Done(new UpgradeBuyResult
            {
                UpgradeId = upgradeId,
                Name = name,
                IsCrewUpgrade = true,
                EffectKey = CrewMemberEffect,
                EffectValue = config.Upgrades.CrewUpgrades.Multiplier,
                MemberName = member.DisplayName,
                Level = 1,
                MaxLevel = 1,
                CoinsSpent = cost,
            });
        }

        private ServiceResult<UpgradeBuyResult> BuyGeneral(GameConfig config, GeneralUpgradeConfig upgrade)
        {
            var level = UpgradeRules.GeneralLevel(upgrade, Save.Upgrades);
            if (level >= upgrade.MaxLevel)
            {
                return ServiceResult<UpgradeBuyResult>.Fail(ServiceError.UpgradeMaxLevel);
            }

            _crew.SettleNow();
            var cost = UpgradeRules.GeneralCost(upgrade, level);
            if (!CrewRules.CanPay(cost, Save.Coins))
            {
                return ServiceResult<UpgradeBuyResult>.Fail(ServiceError.NotEnoughCoins);
            }

            Save.Coins -= cost;
            Save.Upgrades.Levels[upgrade.Id] = level + 1;
            return Done(new UpgradeBuyResult
            {
                UpgradeId = upgrade.Id,
                Name = upgrade.DisplayName,
                IsCrewUpgrade = false,
                EffectKey = upgrade.Effect,
                EffectValue = upgrade.ValuePerLevel,
                Level = level + 1,
                MaxLevel = upgrade.MaxLevel,
                CoinsSpent = cost,
            });
        }

        private ServiceResult<UpgradeBuyResult> Done(UpgradeBuyResult result)
        {
            result.CrewCoinsPerSecond = CrewRules.CoinsPerSecond(Config, Save.Crew, Save.Upgrades);
            _session.Persist();
            _session.Log("Upgrade: bought " + result.UpgradeId + " (level " + result.Level + ") for " + result.CoinsSpent + " coins.");
            return ServiceResult<UpgradeBuyResult>.Ok(result);
        }

        private UpgradeOfferView CrewOffer(GameConfig config, CrewMemberConfig member, int tier)
        {
            var id = UpgradeRules.CrewUpgradeId(member.Id, tier);
            var bought = Save.Upgrades.LevelOf(id) > 0;
            var isUnlocked = UpgradeRules.IsCrewUpgradeUnlocked(config, Save.Crew, member, tier);
            var cost = UpgradeRules.CrewUpgradeCost(config, member, tier);
            var multiplier = config.Upgrades.CrewUpgrades.Multiplier;
            return new UpgradeOfferView
            {
                UpgradeId = id,
                Name = UpgradeRules.CrewUpgradeName(config, member, tier),
                IsCrewUpgrade = true,
                EffectKey = CrewMemberEffect,
                EffectValue = multiplier,
                EffectTotal = bought ? multiplier : 0,
                MemberId = member.Id,
                MemberName = member.DisplayName,
                MemberNamePlural = member.PluralName,
                Level = bought ? 1 : 0,
                MaxLevel = 1,
                Cost = cost,
                UnlockCount = config.Upgrades.CrewUpgrades.Tiers[tier].UnlockCount,
                UnlockHave = Save.Crew.UnitsOf(member.Id),
                BuyBlocker = bought ? ServiceError.UpgradeAlreadyBought
                    : !isUnlocked ? ServiceError.UpgradeLocked
                    : !CrewRules.CanPay(cost, Save.Coins) ? ServiceError.NotEnoughCoins
                    : ServiceError.None,
            };
        }

        private UpgradeOfferView GeneralOffer(GeneralUpgradeConfig upgrade, UpgradesState upgrades)
        {
            var level = UpgradeRules.GeneralLevel(upgrade, upgrades);
            var cost = UpgradeRules.GeneralCost(upgrade, level);
            return new UpgradeOfferView
            {
                UpgradeId = upgrade.Id,
                Name = upgrade.DisplayName,
                IsCrewUpgrade = false,
                EffectKey = upgrade.Effect,
                EffectValue = upgrade.ValuePerLevel,
                EffectTotal = level * upgrade.ValuePerLevel,
                Level = level,
                MaxLevel = upgrade.MaxLevel,
                Cost = cost,
                BuyBlocker = level >= upgrade.MaxLevel ? ServiceError.UpgradeMaxLevel
                    : !CrewRules.CanPay(cost, Save.Coins) ? ServiceError.NotEnoughCoins
                    : ServiceError.None,
            };
        }
    }
}
