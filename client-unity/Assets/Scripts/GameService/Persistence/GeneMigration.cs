using System.Collections.Generic;
using System.Linq;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Fishing;

namespace FishingIdle.GameService.Persistence
{
    /// <summary>
    /// Genes for the fish that existed before them (M24-T52, A-176, TD-056). A save before version 23 has fish without
    /// genes in the Fishing Box, the Aquarium (and so the Cardume and the clan's fish) and the Market (listings, items to
    /// withdraw, auctions, the news). Each gets progression.json → genes.migration_neutral_value (16) in all four: almost
    /// exactly the strength it had (×1,003), never a free 5★ nor a weaker fish. It needs the config, so GameSession runs
    /// it after loading; it only fills what is missing (or broken), so running it again changes nothing.
    /// </summary>
    internal static class GeneMigration
    {
        /// <summary>Gives neutral genes to every fish without a usable set. Returns how many fish got them.</summary>
        public static int GiveMissingGenes(GameConfig config, PlayerSave save)
        {
            if (config == null || save == null)
            {
                return 0;
            }

            var count = 0;
            foreach (var entry in save.FishingBox ?? Enumerable.Empty<BoxCatch>())
            {
                if (entry != null && !GeneRules.IsValid(config, entry.Genes))
                {
                    entry.Genes = GeneRules.Neutral(config);
                    count++;
                }
            }

            foreach (var fish in AllFish(save))
            {
                if (!GeneRules.IsValid(config, fish.Genes))
                {
                    fish.Genes = GeneRules.Neutral(config);
                    count++;
                }
            }

            return count;
        }

        /// <summary>Every kept fish the save holds: the Aquarium and everything on the Market.</summary>
        private static IEnumerable<FishInstance> AllFish(PlayerSave save)
        {
            var fish = new List<FishInstance>();
            if (save.Aquarium != null)
            {
                fish.AddRange(save.Aquarium);
            }

            var market = save.Market;
            if (market != null)
            {
                fish.AddRange((market.MyListings ?? new List<MarketListing>()).Select(l => l?.Goods?.Fish));
                fish.AddRange((market.BotListings ?? new List<MarketListing>()).Select(l => l?.Goods?.Fish));
                fish.AddRange((market.Withdrawals ?? new List<WithdrawalItem>()).Select(w => w?.Goods?.Fish));
                fish.AddRange((market.Auctions ?? new List<Auction>()).Select(a => a?.Goods?.Fish));
                fish.AddRange((market.Events ?? new List<MarketEvent>()).Select(e => e?.Goods?.Fish));
            }

            return fish.Where(f => f != null);
        }
    }
}
