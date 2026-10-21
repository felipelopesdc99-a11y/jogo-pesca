using System;
using System.Collections.Generic;
using System.Linq;
using FishingIdle.GameService.Arena;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;

namespace FishingIdle.GameService.WorldBoss
{
    /// <summary>
    /// Pure rules of the Boss Mundial (M24-T29, A-165): the daily window in the event's fixed time zone, the Cardume's damage
    /// (the Arena's hit rule, BattleEngine.HitDamage) and the reward bands. No state, no clock: the service passes "now".
    /// </summary>
    public static class WorldBossRules
    {
        public const long DayMs = 86_400_000L;

        private static long OffsetMs(WorldBossConfig boss) => boss.Schedule.UtcOffsetMinutes * 60_000L;

        /// <summary>The event day of a moment: whole days since 01/01/1970 in the event's time zone.</summary>
        public static long DayOf(WorldBossConfig boss, long utcMs)
        {
            var local = utcMs + OffsetMs(boss);
            var day = local / DayMs;
            return local < 0 && local % DayMs != 0 ? day - 1 : day;
        }

        /// <summary>The calendar date of an event day (for "DD/MM/AAAA").</summary>
        public static DateTime DateOf(long day) => new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Unspecified).AddDays(day);

        public static long WindowStartMs(WorldBossConfig boss, long day) => day * DayMs - OffsetMs(boss) + boss.Schedule.StartMinuteOfDay * 60_000L;

        public static long WindowEndMs(WorldBossConfig boss, long day) => WindowStartMs(boss, day) + boss.Schedule.DurationMinutes * 60_000L;

        public static long IntervalMs(WorldBossConfig boss) => Math.Max(1000L, (long)Math.Round(boss.Attack.IntervalSeconds * 1000.0));

        /// <summary>Attack k (1 to this) happens k intervals after the window opens: 200 in 10 minutes at 3 s.</summary>
        public static int AttacksPerWindow(WorldBossConfig boss) => (int)(boss.Schedule.DurationMinutes * 60_000L / IntervalMs(boss));

        public static bool IsOpen(WorldBossConfig boss, long utcMs)
        {
            var day = DayOf(boss, utcMs);
            return utcMs >= WindowStartMs(boss, day) && utcMs < WindowEndMs(boss, day);
        }

        /// <summary>The next window start after <paramref name="utcMs"/> (today's if it has not opened yet, otherwise tomorrow's).</summary>
        public static long NextStartMs(WorldBossConfig boss, long utcMs)
        {
            var day = DayOf(boss, utcMs);
            var start = WindowStartMs(boss, day);
            return utcMs < start ? start : WindowStartMs(boss, day + 1);
        }

        /// <summary>The last attack slot due at <paramref name="utcMs"/> in a day's window (0 at the opening, at most AttacksPerWindow).</summary>
        public static long AttackIndexAt(WorldBossConfig boss, long day, long utcMs)
        {
            var elapsed = Math.Min(utcMs, WindowEndMs(boss, day)) - WindowStartMs(boss, day);
            return elapsed <= 0 ? 0 : Math.Min(AttacksPerWindow(boss), elapsed / IntervalMs(boss));
        }

        /// <summary>
        /// One fish's share of an attack: the Arena hit (attack × roll against the boss's defense) times the hits its Speed gives
        /// in one interval (interval ÷ the Arena's attack interval for that Speed).
        /// </summary>
        public static double FishDamage(GameConfig config, FishStats stats, double roll)
        {
            var combat = config.Arena.Combat;
            var hits = config.WorldBoss.Attack.IntervalSeconds / BattleEngine.Interval(combat, stats.Speed);
            return BattleEngine.HitDamage(combat, stats.Attack, config.WorldBoss.Attack.BossDefense, roll) * hits;
        }

        /// <summary>
        /// One Cardume attack: each fish rolls once with <paramref name="rng"/> (positions 1 to 6, empty ones skipped). Fills
        /// <paramref name="bySlot"/> (6 entries, 0 for an empty position) when given. At least 1 when any fish attacks.
        /// </summary>
        public static long Attack(GameConfig config, IReadOnlyList<FishStats> team, Rng rng, long[] bySlot = null)
        {
            var combat = config.Arena.Combat;
            var total = 0.0;
            var any = false;
            for (var i = 0; i < team.Count; i++)
            {
                var stats = team[i];
                if (stats == null)
                {
                    if (bySlot != null && i < bySlot.Length) bySlot[i] = 0;
                    continue;
                }

                any = true;
                var damage = FishDamage(config, stats, BattleEngine.RollDamage(combat, rng));
                total += damage;
                if (bySlot != null && i < bySlot.Length) bySlot[i] = Math.Max(1L, (long)Math.Round(damage));
            }

            return any ? Math.Max(1L, (long)Math.Round(total)) : 0L;
        }

        /// <summary>The damage of the whole window with this Cardume and the average roll: the day's maximum (100% of the bands).</summary>
        public static double FullWindowDamage(GameConfig config, IReadOnlyList<FishStats> team)
        {
            var roll = BattleEngine.AverageRoll(config.Arena.Combat);
            var perAttack = team.Where(s => s != null).Sum(s => FishDamage(config, s, roll));
            return perAttack * AttacksPerWindow(config.WorldBoss);
        }

        /// <summary>The damage a band asks for (1-based), from the day's maximum.</summary>
        public static long Threshold(WorldBossConfig boss, int band, double referenceDamage)
        {
            var percent = boss.Bands[band - 1].DamagePercent;
            return Math.Max(1L, (long)Math.Ceiling(referenceDamage * percent / 100.0 - 1e-9));
        }

        /// <summary>The highest band reached (0 = none).</summary>
        public static int BandFor(WorldBossConfig boss, long damage, double referenceDamage)
        {
            if (damage <= 0 || referenceDamage <= 0)
            {
                return 0;
            }

            var band = 0;
            for (var b = 1; b <= boss.Bands.Count; b++)
            {
                if (damage >= Threshold(boss, b, referenceDamage))
                {
                    band = b;
                }
            }

            return band;
        }

        /// <summary>The reference income row of a Fisher level (the highest level not above it).</summary>
        public static WorldBossIncomeConfig IncomeAt(WorldBossConfig boss, int fisherLevel)
        {
            var row = boss.ReferenceIncome[0];
            foreach (var r in boss.ReferenceIncome)
            {
                if (r.Level <= fisherLevel)
                {
                    row = r;
                }
            }

            return row;
        }

        public static long Coins(WorldBossConfig boss, WorldBossBandConfig band, int fisherLevel) => Minutes(IncomeAt(boss, fisherLevel).CoinsPerHour, band.IncomeMinutes);

        public static long Shells(WorldBossConfig boss, WorldBossBandConfig band, int fisherLevel) => Minutes(IncomeAt(boss, fisherLevel).ShellsPerHour, band.ShellMinutes);

        internal static long Minutes(long perHour, double minutes)
        {
            var value = Math.Round(perHour * minutes / 60.0, MidpointRounding.AwayFromZero);
            return value >= long.MaxValue ? long.MaxValue : Math.Max(0L, (long)value);
        }

        /// <summary>The boat of the highest map the Fisher level unlocks (whose fragments the bands give), or null.</summary>
        public static BoatConfig BoatFor(GameConfig config, int fisherLevel)
        {
            foreach (var map in config.Maps.Maps.Where(m => m.UnlockFisherLevel <= fisherLevel).OrderByDescending(m => m.UnlockFisherLevel))
            {
                if (config.TryGetBoatForMap(map.Id, out var boat) && boat.Id != config.StarterBoat.Id)
                {
                    return boat;
                }
            }

            return null;
        }

        /// <summary>Dólares of a rank of the day; 0 when the online rewards are off or the rank has none.</summary>
        public static long DollarsFor(WorldBossConfig boss, int rank)
        {
            var list = boss.Ranking.DollarsByRank;
            return boss.Ranking.OnlineRewardsEnabled && rank >= 1 && rank <= list.Count ? list[rank - 1] : 0;
        }
    }
}
