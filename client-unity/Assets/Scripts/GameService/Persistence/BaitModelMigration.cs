using System;
using System.Collections.Generic;
using System.Linq;
using FishingIdle.GameService.Config;

namespace FishingIdle.GameService.Persistence
{
    /// <summary>What moving a save to today's baits changed (for the log and the tests).</summary>
    public sealed class BaitModelChange
    {
        /// <summary>Old baits with charges left that became one of today's.</summary>
        public int BaitsConverted { get; internal set; }
    }

    /// <summary>
    /// Moves the baits a save holds from the old model (equipment.json → bait_migration.legacy_baits, the +3/+7/+10% baits)
    /// to today's three baits (M24-T30, A-161). Each old bait with charges left becomes today's bait with the same bonus or the
    /// closest one above (the biggest when none is above), and its charges keep their value in Coins: new charges = old charges
    /// × old Coins per charge ÷ new Coins per charge, rounded up. The bait in use stays in use (the new one). Runs once per
    /// model change, when the session loads the save; it depends on the config, so it lives here and not in
    /// <see cref="SaveMigrations"/> (as <see cref="LevelCurveMigration"/>, TD-038).
    /// </summary>
    public static class BaitModelMigration
    {
        public static BaitModelChange MoveToCurrentModel(GameConfig config, PlayerSave save)
        {
            var change = new BaitModelChange();
            var current = config.BaitModelVersion;
            if (save.BaitModelVersion >= current)
            {
                return change;
            }

            var legacy = config.Equipment.BaitMigration?.LegacyBaits;
            var today = config.Equipment.Baits ?? new List<BaitConfig>();
            if (save.BaitModelVersion == 1 && legacy != null && legacy.Count > 0 && today.Count > 0)
            {
                var old = save.BaitCharges ?? new Dictionary<string, int>();
                var converted = new Dictionary<string, int>(StringComparer.Ordinal);
                var active = save.ActiveBaitId;
                foreach (var pair in old)
                {
                    var before = legacy.FirstOrDefault(b => b != null && b.Id == pair.Key);
                    if (before == null)
                    {
                        // Not an old bait: kept as it is.
                        Add(converted, pair.Key, Math.Max(0, pair.Value));
                        continue;
                    }

                    var target = Target(today, before.CatchSuccessBonus);
                    if (pair.Key == save.ActiveBaitId)
                    {
                        active = target.Id;
                    }

                    if (pair.Value <= 0)
                    {
                        continue;
                    }

                    Add(converted, target.Id, Charges(before, target, pair.Value));
                    change.BaitsConverted++;
                }

                // An old bait in use with no entry at all still points at its new one.
                var activeOld = legacy.FirstOrDefault(b => b != null && b.Id == save.ActiveBaitId);
                if (activeOld != null && !old.ContainsKey(activeOld.Id))
                {
                    active = Target(today, activeOld.CatchSuccessBonus).Id;
                }

                save.BaitCharges = converted;
                save.ActiveBaitId = active;
            }

            // 0 = a save made on today's baits (a new player): nothing to convert.
            save.BaitModelVersion = current;
            return change;
        }

        /// <summary>Today's bait with the same bonus or the closest above; the biggest bonus when none is above.</summary>
        public static BaitConfig Target(List<BaitConfig> today, double oldBonus)
        {
            var ordered = today.OrderBy(b => b.CatchSuccessBonus).ThenBy(b => b.Tier).ToList();
            return ordered.FirstOrDefault(b => b.CatchSuccessBonus >= oldBonus - 1e-9) ?? ordered[ordered.Count - 1];
        }

        /// <summary>Old charges at their value in Coins, as charges of the new bait (rounded up, at least 1).</summary>
        public static int Charges(BaitConfig before, BaitConfig target, int oldCharges)
        {
            if (oldCharges <= 0)
            {
                return 0;
            }

            var oldPerCharge = before.Charges > 0 ? before.CostCoins / (double)before.Charges : 0;
            var newPerCharge = target.Charges > 0 ? target.CostCoins / (double)target.Charges : 0;
            if (oldPerCharge <= 0 || newPerCharge <= 0)
            {
                return oldCharges;
            }

            var charges = Math.Ceiling(oldCharges * oldPerCharge / newPerCharge - 1e-9);
            return (int)Math.Max(1, Math.Min(int.MaxValue, charges));
        }

        private static void Add(Dictionary<string, int> charges, string id, int amount)
        {
            charges.TryGetValue(id, out var have);
            charges[id] = (int)Math.Min(int.MaxValue, (long)have + amount);
        }
    }
}
