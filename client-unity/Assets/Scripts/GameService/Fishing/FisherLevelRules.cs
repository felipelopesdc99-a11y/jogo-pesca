using System;
using System.Collections.Generic;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Persistence;

namespace FishingIdle.GameService.Fishing
{
    /// <summary>What a save gets from moving to the current Fisher XP curve.</summary>
    public sealed class FisherCurveChange
    {
        public int LevelBefore { get; internal set; }
        public int LevelAfter { get; internal set; }
        public long DollarsGained { get; internal set; }
    }

    /// <summary>
    /// Fisher level rules shared by fishing and loading a save: the Dólares of a level milestone (A-111, capped at
    /// <c>up_to_level</c> since A-153) and the move of an older save to the current XP curve (M24-T03, TD-038).
    /// </summary>
    public static class FisherLevelRules
    {
        /// <summary>Dólares for reaching exactly <paramref name="level"/>: a multiple of every_levels, up to up_to_level.</summary>
        public static long DollarsForReaching(GameConfig config, int level)
        {
            var reward = config.Progression.Fisher.DollarsPerLevels;
            if (reward == null || reward.EveryLevels <= 0 || reward.Dollars <= 0 || level % reward.EveryLevels != 0)
            {
                return 0;
            }

            return reward.UpToLevel > 0 && level > reward.UpToLevel ? 0 : reward.Dollars;
        }

        /// <summary>
        /// Adds Fisher XP from any source (fishing, the Crew): counts it in the total (TD-038), climbs the levels it
        /// reaches (each one added to <paramref name="levelsReached"/>) and pays the Dólares of the level milestones
        /// (A-111). Returns the Dólares paid. At the max level the XP still counts in the total.
        /// </summary>
        internal static long AddXp(GameConfig config, PlayerSave save, long xp, List<int> levelsReached)
        {
            if (xp <= 0)
            {
                return 0;
            }

            var maxLevel = config.Progression.Fisher.MaxLevel;
            save.FisherXpTotal = xp > long.MaxValue - save.FisherXpTotal ? long.MaxValue : save.FisherXpTotal + xp;
            if (save.FisherLevel >= maxLevel)
            {
                save.FisherXp = 0;
                return 0;
            }

            long dollars = 0;
            save.FisherXp = xp > long.MaxValue - save.FisherXp ? long.MaxValue : save.FisherXp + xp;
            while (save.FisherLevel < maxLevel)
            {
                var needed = config.FisherXpToNextLevel(save.FisherLevel);
                if (needed <= 0 || save.FisherXp < needed)
                {
                    break;
                }

                save.FisherXp -= needed;
                save.FisherLevel++;
                levelsReached?.Add(save.FisherLevel);

                // A-111: a few Dólares at every level milestone, so a VIP can be saved up by playing (up to Nv.100, A-153).
                var reward = DollarsForReaching(config, save.FisherLevel);
                if (reward > 0)
                {
                    save.Dollars += reward;
                    dollars += reward;
                }
            }

            if (save.FisherLevel >= maxLevel)
            {
                save.FisherXp = 0;
            }

            return dollars;
        }

        /// <summary>The level and the XP inside it that a total XP reaches on the current curve.</summary>
        public static (int Level, long XpInLevel) LevelForTotalXp(GameConfig config, long totalXp)
        {
            var max = config.Progression.Fisher.MaxLevel;
            var level = 1;
            var rest = Math.Max(0, totalXp);
            while (level < max)
            {
                var needed = config.FisherXpToNextLevel(level);
                if (needed <= 0 || rest < needed)
                {
                    break;
                }

                rest -= needed;
                level++;
            }

            return (level, level >= max ? 0 : rest);
        }

        /// <summary>
        /// Moves a save made on an older XP curve to the current one: its total XP (every XP ever earned, kept in
        /// <see cref="PlayerSave.FisherXpTotal"/>) becomes the matching level on the new table. The level never goes
        /// down: when the new curve asks for more, the player keeps the level and the XP inside it (capped just below
        /// the next level). Level milestones crossed on the way give their Dólares, as fishing would.
        /// Returns null when the save is already on the current curve.
        /// </summary>
        public static FisherCurveChange MoveToCurrentCurve(GameConfig config, PlayerSave save)
        {
            var curve = config.Progression.Fisher.XpCurveVersion;
            if (save.FisherXpCurveVersion >= curve)
            {
                return null;
            }

            var max = config.Progression.Fisher.MaxLevel;
            var before = Math.Max(1, Math.Min(max, save.FisherLevel));
            var (level, xpInLevel) = LevelForTotalXp(config, save.FisherXpTotal);
            var change = new FisherCurveChange { LevelBefore = before };

            if (level >= before)
            {
                for (var reached = before + 1; reached <= level; reached++)
                {
                    change.DollarsGained += DollarsForReaching(config, reached);
                }

                save.FisherLevel = level;
                save.FisherXp = xpInLevel;
            }
            else
            {
                var needed = config.FisherXpToNextLevel(before);
                save.FisherLevel = before;
                save.FisherXp = needed > 0 ? Math.Max(0, Math.Min(save.FisherXp, needed - 1)) : 0;
            }

            save.Dollars += change.DollarsGained;
            save.FisherXpCurveVersion = curve;
            change.LevelAfter = save.FisherLevel;
            return change;
        }
    }
}
