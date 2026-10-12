using System.Collections.Generic;
using System.Linq;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Persistence;

namespace FishingIdle.GameService.Shop
{
    /// <summary>Fragments of one rod received at once (a catch, a pass reward).</summary>
    public sealed class RodFragmentGainView
    {
        public string RodId { get; internal set; }
        public string RodName { get; internal set; }
        public long Amount { get; internal set; }

        /// <summary>Fragments of this rod after the gain (extras when <see cref="Owned"/>).</summary>
        public long Have { get; internal set; }
        public int Required { get; internal set; }

        /// <summary>Whether the player owns the rod after this gain (assembled now or before).</summary>
        public bool Owned { get; internal set; }

        /// <summary>Whether this gain completed the rod.</summary>
        public bool Assembled { get; internal set; }

        /// <summary>Whether the assembled rod was put in use (it is of a higher tier than the one in use).</summary>
        public bool Equipped { get; internal set; }
    }

    /// <summary>
    /// Rods by fragments (M24-T32, A-163): every rod but the free Starter Rod drops on one map (rods.json →
    /// fragment_map_id), always a map the rod before it already reaches, so the progression never locks. A fish pulled out
    /// there may drop one of its fragments; fragments_required of them assemble the rod, which goes to the Inventory at
    /// Nv.1 and is put in use when its tier is higher than the rod in use. Fragments that arrive while the rod is owned are
    /// kept as extras (they assemble it again if it is sold). Rods are not sold in the Shop anymore.
    /// </summary>
    public static class RodFragmentRules
    {
        /// <summary>Fragments of a rod the player has now (extras when the rod is owned).</summary>
        public static long Have(PlayerSave save, string rodId)
        {
            return rodId != null && save.RodFragments != null && save.RodFragments.TryGetValue(rodId, out var n) ? n : 0;
        }

        /// <summary>
        /// Whether the player already has this rod: in the Inventory, but also on the Market, in one of their auctions or
        /// waiting in Items to Withdraw (a listed rod is still theirs).
        /// </summary>
        public static bool Owns(PlayerSave save, string rodId)
        {
            bool Same(MarketGoods g) => g != null && g.Kind == MarketGoods.KindRod && g.Rod != null && g.Rod.RodId == rodId;
            var market = save.Market;
            return save.Inventory.Any(i => i.Kind == InventoryItem.KindRod && i.RodId == rodId)
                || (market != null && (market.MyListings.Any(l => Same(l.Goods))
                    || market.Withdrawals.Any(w => Same(w.Goods))
                    || market.Auctions.Any(a => a.SellerName == null && Same(a.Goods))));
        }

        /// <summary>
        /// The roll after a fish is pulled out on <paramref name="map"/>: with the map rod's chance, 1 fragment of it.
        /// Draws from the cycle's own <see cref="Rng"/> after the catch and the boat fragment, so neither ever changes.
        /// Offline catches drop only when the config allows it (rods.json → fragment_rules). Returns the gain, or null.
        /// </summary>
        internal static RodFragmentGainView RollOnCatch(GameConfig config, PlayerSave save, MapConfig map, Rng rng, bool offline, long nowMs)
        {
            if (map == null || !config.TryGetRodForMap(map.Id, out var rod) || rod.FragmentDropChance <= 0)
            {
                return null;
            }

            if (offline && !config.RodFragmentsDropOffline)
            {
                return null;
            }

            return rng.NextDouble() < rod.FragmentDropChance ? Add(config, save, rod, 1, nowMs) : null;
        }

        /// <summary>
        /// Adds fragments of a rod and assembles it when they reach fragments_required (the Starter Rod takes none).
        /// Assembling spends the fragments, adds the rod to the Inventory at Nv.1 and puts it in use when its tier is
        /// higher than the rod in use.
        /// </summary>
        public static RodFragmentGainView Add(GameConfig config, PlayerSave save, RodConfig rod, long amount, long nowMs)
        {
            if (rod == null || amount <= 0 || !GameConfig.IsFragmentRod(rod))
            {
                return null;
            }

            save.RodFragments = save.RodFragments ?? new Dictionary<string, long>();
            var have = Have(save, rod.Id);
            have = amount > long.MaxValue - have ? long.MaxValue : have + amount;
            save.RodFragments[rod.Id] = have;
            var gain = new RodFragmentGainView
            {
                RodId = rod.Id,
                RodName = rod.DisplayName,
                Amount = amount,
                Required = rod.FragmentsRequired,
            };
            var before = save.EquippedRodItemId;
            gain.Assembled = TryAssemble(config, save, rod, nowMs);
            gain.Equipped = gain.Assembled && save.EquippedRodItemId != before;
            gain.Have = Have(save, rod.Id);
            gain.Owned = Owns(save, rod.Id);
            return gain;
        }

        /// <summary>Assembles every rod whose fragments are already enough (a config that lowered fragments_required). Returns their ids.</summary>
        internal static List<string> AssembleReady(GameConfig config, PlayerSave save, long nowMs)
        {
            var assembled = new List<string>();
            foreach (var rod in config.Rods.Rods.Where(GameConfig.IsFragmentRod).OrderBy(r => r.Tier))
            {
                if (TryAssemble(config, save, rod, nowMs))
                {
                    assembled.Add(rod.Id);
                }
            }

            return assembled;
        }

        private static bool TryAssemble(GameConfig config, PlayerSave save, RodConfig rod, long nowMs)
        {
            var required = System.Math.Max(1, rod.FragmentsRequired);
            if (Owns(save, rod.Id) || Have(save, rod.Id) < required)
            {
                return false;
            }

            save.RodFragments[rod.Id] = Have(save, rod.Id) - required;
            var item = new InventoryItem
            {
                Id = save.NextItemId++,
                Kind = InventoryItem.KindRod,
                RodId = rod.Id,
                Level = 1,
                AcquiredAtMs = nowMs,
                PurchasePriceCoins = 0,
            };
            save.Inventory.Add(item);

            // The new rod goes in use when it is of a higher tier than the rod in use (or when there is none yet).
            var equipped = save.EquippedRodItem();
            var current = equipped != null && config.TryGetRod(equipped.RodId, out var r) ? r : null;
            if (current == null || rod.Tier > current.Tier)
            {
                save.EquippedRodItemId = item.Id;
            }

            return true;
        }
    }
}
