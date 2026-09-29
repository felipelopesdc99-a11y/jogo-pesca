using System;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Fishing;
using FishingIdle.GameService.Persistence;
using FishingIdle.GameService.Shop;

namespace FishingIdle.GameService.Market
{
    /// <summary>Market money formulas: fee, NPC value and the simulated market's reference value.</summary>
    public static class MarketRules
    {
        /// <summary>The 3% taken from a completed fixed-price sale (GDD section 33).</summary>
        public static long SaleFee(GameConfig config, long price)
        {
            return Math.Max(0, (long)Math.Round(price * config.Market.CompletedSaleFeeRatio, MidpointRounding.AwayFromZero));
        }

        /// <summary>What the game would pay for the goods right now.</summary>
        public static long NpcValue(GameConfig config, MarketGoods goods)
        {
            if (goods.Kind == MarketGoods.KindFish)
            {
                return config.TryGetSpecies(goods.Fish.SpeciesId, out var species) ? CatchRules.SalePrice(config, species, goods.Fish.SizeMm) : 0;
            }

            return config.TryGetRod(goods.Rod.RodId, out var rod) ? RodRules.ResaleValue(rod, goods.Rod) : 0;
        }

        /// <summary>
        /// What the local simulated market considers fair (market_bots.json → valuation). Only the
        /// simulation uses it; real players will set their own prices.
        /// </summary>
        public static long ReferenceValue(GameConfig config, MarketGoods goods)
        {
            var val = config.MarketBots.Valuation;
            double value;
            if (goods.Kind == MarketGoods.KindFish)
            {
                if (!config.TryGetSpecies(goods.Fish.SpeciesId, out var species))
                {
                    return 0;
                }

                value = CatchRules.SalePrice(config, species, goods.Fish.SizeMm) * val.FishReferenceRatio * (1.0 + val.FishLevelPremiumPerLevel * (goods.Fish.Level - 1));
            }
            else
            {
                if (!config.TryGetRod(goods.Rod.RodId, out var rod))
                {
                    return 0;
                }

                value = (rod.Acquisition?.PurchaseCostCoins ?? 0) + RodUpgradeSpend(config, rod, goods.Rod.Level);
                value *= val.RodReferenceRatio;
            }

            return Math.Max(1, (long)Math.Round(value, MidpointRounding.AwayFromZero));
        }

        /// <summary>
        /// Chance a simulated buyer takes a listing in one demand check: the configured chance at the
        /// reference price, up to double for a bargain, down to zero at max_price_ratio × reference.
        /// </summary>
        public static double PurchaseChance(GameConfig config, long price, long reference)
        {
            var demand = config.MarketBots.Demand;
            var ratio = price / (double)Math.Max(1, reference);
            double chance;
            if (ratio <= 1)
            {
                chance = demand.ChanceAtReferencePrice * (2.0 - ratio);
            }
            else
            {
                chance = demand.ChanceAtReferencePrice * (demand.MaxPriceRatio - ratio) / (demand.MaxPriceRatio - 1.0);
            }

            return chance < 0 ? 0 : chance > 1 ? 1 : chance;
        }

        /// <summary>Coins the upgrades from Lv.1 to <paramref name="level"/> cost.</summary>
        public static long RodUpgradeSpend(GameConfig config, RodConfig rod, int level)
        {
            long total = 0;
            for (var l = 1; l < level; l++)
            {
                total += config.RodUpgradeCost(rod, l);
            }

            return total;
        }

        /// <summary>XP a fish needs in total to go from Lv.1 to <paramref name="level"/>.</summary>
        public static long FishXpToReach(GameConfig config, int level)
        {
            long total = 0;
            for (var l = 1; l < level; l++)
            {
                total += config.FishXpToNextLevel(l);
            }

            return total;
        }
    }
}
