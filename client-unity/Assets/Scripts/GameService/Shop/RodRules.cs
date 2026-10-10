using System;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Persistence;

namespace FishingIdle.GameService.Shop
{
    /// <summary>Rod economy formulas (GDD section 19).</summary>
    public static class RodRules
    {
        /// <summary>
        /// NPC resale: base ratio × purchase price + upgrade ratio × coins spent on upgrades
        /// (rods.json → npc_resale). Rods without that rule use their flat npc_resale_value_coins.
        /// </summary>
        public static long ResaleValue(RodConfig rod, InventoryItem item)
        {
            if (rod.NpcResale == null)
            {
                return Math.Max(0, rod.NpcResaleValueCoins);
            }

            var value = item.PurchasePriceCoins * rod.NpcResale.BaseValueRatio + item.UpgradeCoinsInvested * rod.NpcResale.UpgradeInvestmentRatio;
            return Math.Max(0, (long)Math.Round(value, MidpointRounding.AwayFromZero));
        }

        /// <summary>Only rods bought in the Shop can be sold or destroyed; the free Starter Rod always stays.</summary>
        public static bool CanDispose(GameConfig config, RodConfig rod) => config.IsPurchasable(rod);
    }
}
