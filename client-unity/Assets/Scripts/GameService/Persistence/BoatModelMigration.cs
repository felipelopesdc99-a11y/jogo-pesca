using System;
using System.Collections.Generic;
using System.Linq;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Shop;

namespace FishingIdle.GameService.Persistence
{
    /// <summary>What moving a save to today's boats changed (for the log and the tests).</summary>
    public sealed class BoatModelChange
    {
        /// <summary>Removed boats the player had assembled (each became Moedas, Conchas and fragments).</summary>
        public int BoatsRefunded { get; internal set; }

        /// <summary>Fragments of removed boats (held, plus what the assembled ones cost) that became fragments of today's boats.</summary>
        public long FragmentsMoved { get; internal set; }
        public long CoinsRefunded { get; internal set; }
        public long ShellsRefunded { get; internal set; }

        /// <summary>Today's boats completed by the moved fragments.</summary>
        public List<string> BoatsAssembled { get; } = new List<string>();
    }

    /// <summary>
    /// Moves a save from the 10 boats of M24-T16 (one per map) to today's 5 (M24-T42, A-168), with the removed boats listed in
    /// equipment.json → boat_migration. The fragments of a removed boat become fragments of the boat its map drops now
    /// (fragments_go_to). A removed boat already assembled becomes the Moedas and Conchas its levels cost (from Nv.1 to its
    /// level, by its old upgrade_cost and the boat_levels curve) plus its fragments_required as fragments of fragments_go_to.
    /// When it was in use, the player uses the best boat they have. Runs once per model change, when the session loads the
    /// save; it depends on the config, so it lives here and not in <see cref="SaveMigrations"/> (as <see cref="BaitModelMigration"/>).
    /// </summary>
    public static class BoatModelMigration
    {
        public static BoatModelChange MoveToCurrentModel(GameConfig config, PlayerSave save)
        {
            var change = new BoatModelChange();
            var current = config.BoatModelVersion;
            if (save.BoatModelVersion >= current)
            {
                return change;
            }

            var removed = config.Equipment.BoatMigration?.RemovedBoats ?? new List<RemovedBoatConfig>();
            save.OwnedBoatIds = save.OwnedBoatIds ?? new List<string>();
            save.BoatLevels = save.BoatLevels ?? new Dictionary<string, int>();
            save.BoatFragments = save.BoatFragments ?? new Dictionary<string, long>();
            var wasInUse = false;
            var moved = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (var boat in removed)
            {
                if (boat == null || boat.Id == null || config.TryGetBoat(boat.Id, out _))
                {
                    continue;
                }

                long fragments = save.BoatFragments.TryGetValue(boat.Id, out var held) ? Math.Max(0, held) : 0;
                save.BoatFragments.Remove(boat.Id);
                if (save.OwnedBoatIds.Contains(boat.Id))
                {
                    var level = save.BoatLevels.TryGetValue(boat.Id, out var l) ? l : 1;
                    var coins = LevelsCost(config, boat, level, true);
                    var shells = LevelsCost(config, boat, level, false);
                    save.Coins = Add(save.Coins, coins);
                    save.Shells = Add(save.Shells, shells);
                    change.CoinsRefunded = Add(change.CoinsRefunded, coins);
                    change.ShellsRefunded = Add(change.ShellsRefunded, shells);
                    fragments = Add(fragments, Math.Max(0, boat.FragmentsRequired));
                    change.BoatsRefunded++;
                    save.OwnedBoatIds.RemoveAll(id => id == boat.Id);
                }

                save.BoatLevels.Remove(boat.Id);
                if (save.BoatId == boat.Id)
                {
                    wasInUse = true;
                }

                if (fragments > 0 && boat.FragmentsGoTo != null)
                {
                    moved.TryGetValue(boat.FragmentsGoTo, out var sum);
                    moved[boat.FragmentsGoTo] = Add(sum, fragments);
                }
            }

            // A removed boat in use: the best boat the player has (the starter boat when none).
            if (wasInUse || (save.BoatId != null && !config.TryGetBoat(save.BoatId, out _)))
            {
                save.BoatId = BestOwned(config, save).Id;
            }

            // The fragments go to today's boats; enough of them assemble one, which goes in use when it is better (the usual rule).
            foreach (var pair in moved)
            {
                if (!config.TryGetBoat(pair.Key, out var target))
                {
                    continue;
                }

                var gain = BoatFragmentRules.Add(config, save, target, pair.Value);
                if (gain != null)
                {
                    change.FragmentsMoved = Add(change.FragmentsMoved, pair.Value);
                    if (gain.Assembled)
                    {
                        change.BoatsAssembled.Add(target.Id);
                    }
                }
            }

            save.BoatModelVersion = current;
            return change;
        }

        /// <summary>Moedas (or Conchas) a removed boat cost to go from Nv.1 to <paramref name="level"/> (boat_levels curve, TD-041).</summary>
        public static long LevelsCost(GameConfig config, RemovedBoatConfig boat, int level, bool coins)
        {
            var cost = boat.UpgradeCost;
            if (cost == null)
            {
                return 0;
            }

            var max = Math.Max(1, config.Equipment.BoatLevels?.MaxLevel ?? 1);
            var top = Math.Max(1, Math.Min(max, level));
            long total = 0;
            for (var n = 1; n < top; n++)
            {
                var price = coins ? LevelCurve.Cost(cost.CoinsFirst, cost.CoinsGrowth, n) : LevelCurve.Cost(cost.ShellsFirst, cost.ShellsGrowth, n);
                total = Add(total, price);
            }

            return total;
        }

        private static BoatConfig BestOwned(GameConfig config, PlayerSave save)
        {
            return config.Equipment.Boats
                .Where(b => BoatFragmentRules.Owns(config, save, b))
                .OrderByDescending(b => GearRules.BoatBonus(config, save, b))
                .ThenByDescending(b => b.Tier)
                .FirstOrDefault() ?? config.StarterBoat;
        }

        private static long Add(long a, long b)
        {
            return b > 0 && a > long.MaxValue - b ? long.MaxValue : a + b;
        }
    }
}
