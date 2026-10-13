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
    }

    /// <summary>
    /// Fisher level rules shared by fishing and loading a save: adding XP and the move of an older save to the
    /// current XP curve (M24-T03, TD-038). Since M24-T17 (A-158) the level milestones pay nothing by themselves: their
    /// Dólares (A-111) are in the free track of the Passe do Pescador, claimed there.
    /// </summary>
    public static class FisherLevelRules
    {
        /// <summary>
        /// Adds Fisher XP (since M24-T46 the fishing is its only source): counts it in the total (TD-038) and climbs the levels it
        /// reaches (each one added to <paramref name="levelsReached"/>). At the max level the XP still counts in the total.
        /// </summary>
        internal static void AddXp(GameConfig config, PlayerSave save, long xp, List<int> levelsReached)
        {
            if (xp <= 0)
            {
                return;
            }

            var maxLevel = config.Progression.Fisher.MaxLevel;
            save.FisherXpTotal = xp > long.MaxValue - save.FisherXpTotal ? long.MaxValue : save.FisherXpTotal + xp;
            if (save.FisherLevel >= maxLevel)
            {
                save.FisherXp = 0;
                return;
            }

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
            }

            if (save.FisherLevel >= maxLevel)
            {
                save.FisherXp = 0;
            }
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
        /// the next level). The level milestones crossed on the way become claimable in the Passe do Pescador (A-158).
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
                save.FisherLevel = level;
                save.FisherXp = xpInLevel;
            }
            else
            {
                var needed = config.FisherXpToNextLevel(before);
                save.FisherLevel = before;
                save.FisherXp = needed > 0 ? Math.Max(0, Math.Min(save.FisherXp, needed - 1)) : 0;
            }

            save.FisherXpCurveVersion = curve;
            change.LevelAfter = save.FisherLevel;
            return change;
        }
    }
}
