using System;
using System.Collections.Generic;
using System.Linq;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Fishing;

namespace FishingIdle.GameService.Persistence
{
    /// <summary>What moving a save to the current rod and fish level curves changed (for the log and the tests).</summary>
    public sealed class LevelCurveChange
    {
        public int RodsMoved { get; internal set; }
        public int FishMoved { get; internal set; }
    }

    /// <summary>
    /// Moves rods and fish from the old Nv.1–Nv.10 levels to the current curve (M24-T13, TD-041). Each old level goes to
    /// the level the config names in legacy_levels_v1: the lowest one whose bonus is at least the old bonus, so nothing
    /// gets weaker. Runs once per curve change, when the session loads the save; it depends on the config (the map), so
    /// it lives here and not in <see cref="SaveMigrations"/>, which only changes the shape of the file (as TD-038).
    /// </summary>
    public static class LevelCurveMigration
    {
        public static LevelCurveChange MoveToCurrentCurves(GameConfig config, PlayerSave save)
        {
            var change = new LevelCurveChange();
            var rodCurve = config.Rods.UpgradeRules?.LevelCurveVersion ?? 0;
            if (save.RodLevelCurveVersion < rodCurve)
            {
                if (save.RodLevelCurveVersion == 1)
                {
                    foreach (var item in AllRods(save))
                    {
                        if (!config.TryGetRod(item.RodId, out var rod) || !rod.HasInternalLevels || rod.LegacyLevelsV1 == null || rod.LegacyLevelsV1.Count == 0)
                        {
                            continue;
                        }

                        var moved = Map(rod.LegacyLevelsV1, item.Level);
                        if (moved > item.Level)
                        {
                            item.Level = moved;
                            change.RodsMoved++;
                        }
                    }
                }

                // 0 = a save made on the current curve (a new player): nothing to move.
                save.RodLevelCurveVersion = rodCurve;
            }

            var fishLevel = config.Progression.FishLevel;
            if (save.FishLevelCurveVersion < fishLevel.LevelCurveVersion)
            {
                if (save.FishLevelCurveVersion == 1 && fishLevel.LegacyLevelsV1 != null && fishLevel.LegacyLevelsV1.Count > 0)
                {
                    foreach (var fish in AllFish(save))
                    {
                        var moved = Map(fishLevel.LegacyLevelsV1, fish.Level);
                        if (moved <= fish.Level)
                        {
                            continue;
                        }

                        // The XP the fish had inside its old level is applied again on the new curve (it may level up).
                        var level = moved;
                        long xp = 0;
                        FishRules.ApplyXp(config, ref level, ref xp, Math.Max(0, fish.Xp));
                        fish.Level = level;
                        fish.Xp = xp;
                        change.FishMoved++;
                    }
                }

                save.FishLevelCurveVersion = fishLevel.LevelCurveVersion;
            }

            return change;
        }

        /// <summary>Old level (1–10) → new level; above the map's last entry uses the last one. Never lower than before.</summary>
        private static int Map(List<int> legacy, int oldLevel)
        {
            var index = Math.Max(1, Math.Min(legacy.Count, oldLevel)) - 1;
            return Math.Max(oldLevel, legacy[index]);
        }

        private static IEnumerable<InventoryItem> AllRods(PlayerSave save)
        {
            var rods = (save.Inventory ?? new List<InventoryItem>()).Where(i => i != null && i.Kind == InventoryItem.KindRod);
            return rods.Concat(AllGoods(save).Where(g => g.Kind == MarketGoods.KindRod && g.Rod != null).Select(g => g.Rod));
        }

        private static IEnumerable<FishInstance> AllFish(PlayerSave save)
        {
            var fish = (save.Aquarium ?? new List<FishInstance>()).Where(f => f != null);
            return fish.Concat(AllGoods(save).Where(g => g.Kind == MarketGoods.KindFish && g.Fish != null).Select(g => g.Fish));
        }

        private static IEnumerable<MarketGoods> AllGoods(PlayerSave save)
        {
            var market = save.Market;
            if (market == null)
            {
                return Enumerable.Empty<MarketGoods>();
            }

            return (market.MyListings ?? new List<MarketListing>()).Select(l => l?.Goods)
                .Concat((market.BotListings ?? new List<MarketListing>()).Select(l => l?.Goods))
                .Concat((market.Withdrawals ?? new List<WithdrawalItem>()).Select(w => w?.Goods))
                .Concat((market.Auctions ?? new List<Auction>()).Select(a => a?.Goods))
                .Where(g => g != null);
        }
    }
}
