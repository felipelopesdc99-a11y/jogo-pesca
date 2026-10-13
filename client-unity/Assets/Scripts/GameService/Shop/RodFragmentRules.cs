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

        /// <summary>Whether the player owns the rod (assembled before; extras then).</summary>
        public bool Owned { get; internal set; }

        /// <summary>
        /// Whether this gain completed the fragments the rod asks for (M24-T58, A-180): nothing is assembled by itself,
        /// the player assembles it with the "Montar" button (Loja → Varas).
        /// </summary>
        public bool Ready { get; internal set; }

        /// <summary>The rod the player still needs in the Inventory to assemble this one (the one before it), or null.</summary>
        public string NeedsRodName { get; internal set; }
    }

    /// <summary>
    /// Rods by fragments (M24-T32, A-163): since M24-T47 (A-173) every fish pulled out, on any map, may drop one fragment of
    /// each rod but the free Starter Rod (one roll per rod, with its own fragment_drop_chance). Since M24-T58 (A-180) the
    /// player assembles the rod with the "Montar" button: it takes fragments_required fragments AND the rod before it in
    /// the Inventory (rod 1 asks for the Starter Rod). Assembling spends only fragments_required; the rod goes to the
    /// Inventory at Nv.1 and is not equipped by itself. Fragments keep adding up without a limit; those of an owned rod are
    /// extras (they assemble it again if it is sold). Rods are not sold in the Shop.
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
        /// The rolls after a fish is pulled out (any map, M24-T47): one per rod assembled from fragments, in tier order,
        /// each with that rod's fragment_drop_chance of 1 fragment of it. Draws from the cycle's own <see cref="Rng"/> after
        /// the catch and the boat fragments, so none of them ever changes; a rod with chance 0 draws nothing. Offline
        /// catches drop only when the config allows it (rods.json → fragment_rules). Returns the gains (empty when none dropped).
        /// </summary>
        internal static List<RodFragmentGainView> RollOnCatch(GameConfig config, PlayerSave save, Rng rng, bool offline, long nowMs)
        {
            var gains = new List<RodFragmentGainView>();
            if (offline && !config.RodFragmentsDropOffline)
            {
                return gains;
            }

            foreach (var rod in config.FragmentRods)
            {
                if (rod.FragmentDropChance > 0 && rng.NextDouble() < rod.FragmentDropChance)
                {
                    var gain = Add(config, save, rod, 1, nowMs);
                    if (gain != null)
                    {
                        gains.Add(gain);
                    }
                }
            }

            return gains;
        }

        /// <summary>
        /// Adds fragments of a rod (the Starter Rod takes none). Nothing is assembled here (M24-T58): the gain says when
        /// the fragments became enough (<see cref="RodFragmentGainView.Ready"/>) and which rod is still missing before it.
        /// </summary>
        public static RodFragmentGainView Add(GameConfig config, PlayerSave save, RodConfig rod, long amount, long nowMs)
        {
            if (rod == null || amount <= 0 || !GameConfig.IsFragmentRod(rod))
            {
                return null;
            }

            save.RodFragments = save.RodFragments ?? new Dictionary<string, long>();
            var before = Have(save, rod.Id);
            var have = amount > long.MaxValue - before ? long.MaxValue : before + amount;
            save.RodFragments[rod.Id] = have;
            var required = System.Math.Max(1, rod.FragmentsRequired);
            var gain = new RodFragmentGainView
            {
                RodId = rod.Id,
                RodName = rod.DisplayName,
                Amount = amount,
                Required = rod.FragmentsRequired,
            };
            gain.Have = have;
            gain.Owned = Owns(save, rod.Id);
            gain.Ready = !gain.Owned && before < required && have >= required;
            var previous = Previous(config, rod);
            gain.NeedsRodName = previous != null && !InInventory(save, previous.Id) ? previous.DisplayName : null;
            return gain;
        }

        /// <summary>The rod right before this one in tier order (the Starter Rod for rod 1); null for the Starter Rod.</summary>
        public static RodConfig Previous(GameConfig config, RodConfig rod)
        {
            return config.Rods.Rods.Where(r => r.Tier < rod.Tier).OrderByDescending(r => r.Tier).FirstOrDefault();
        }

        /// <summary>Whether a copy of this rod is in the Inventory (a rod on the Market or in Items to Withdraw is not).</summary>
        public static bool InInventory(PlayerSave save, string rodId)
        {
            return save.Inventory.Any(i => i.Kind == InventoryItem.KindRod && i.RodId == rodId);
        }

        /// <summary>Why a rod cannot be assembled now (M24-T58, A-180); None when it can.</summary>
        public static ServiceError AssembleBlocker(GameConfig config, PlayerSave save, RodConfig rod)
        {
            if (rod == null) return ServiceError.ItemNotFound;
            if (!GameConfig.IsFragmentRod(rod)) return ServiceError.NotFromFragments;
            if (Owns(save, rod.Id)) return ServiceError.RodAlreadyOwned;
            var previous = Previous(config, rod);
            if (previous != null && !InInventory(save, previous.Id)) return ServiceError.RodNeedsPrevious;
            if (Have(save, rod.Id) < System.Math.Max(1, rod.FragmentsRequired)) return ServiceError.NotEnoughRodFragments;
            return ServiceError.None;
        }

        /// <summary>
        /// Assembles a rod (checked by <see cref="AssembleBlocker"/> first): spends fragments_required (the rest stays kept)
        /// and adds the rod to the Inventory at Nv.1. It is not equipped: the player chooses (M24-T58, A-180). Returns it.
        /// </summary>
        internal static InventoryItem Assemble(GameConfig config, PlayerSave save, RodConfig rod, long nowMs)
        {
            save.RodFragments[rod.Id] = Have(save, rod.Id) - System.Math.Max(1, rod.FragmentsRequired);
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
            return item;
        }
    }
}
