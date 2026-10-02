using System;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Fishing;

namespace FishingIdle.GameService.Profile
{
    /// <summary>Cardume modifiers and the private Strength metric (GDD sections 23 and 31).</summary>
    public static class CardumeRules
    {
        /// <summary>Whether the complete-Cardume bonus applies (exactly the required number of fish, 6).</summary>
        public static bool CompleteBonusActive(GameConfig config, int filled)
        {
            return filled >= config.Arena.Cardume.CompleteBonus.RequiresFilledSlots;
        }

        /// <summary>Attributes with the 6/6 bonus applied when active. Never written to the fish.</summary>
        public static FishStats Effective(GameConfig config, FishStats stats, bool bonusActive)
        {
            if (!bonusActive)
            {
                return stats;
            }

            var b = config.Arena.Cardume.CompleteBonus;
            return new FishStats
            {
                Hp = stats.Hp * (1 + b.HpPercent / 100.0),
                Attack = stats.Attack * (1 + b.AttackPercent / 100.0),
                Defense = stats.Defense * (1 + b.DefensePercent / 100.0),
                Speed = stats.Speed * (1 + b.SpeedPercent / 100.0),
            };
        }

        /// <summary>FishStrengthRaw = Attack×2 + Defense×1,5 + HP÷10 + Speed×0,5 (weights in arena.json).</summary>
        public static double RawStrength(GameConfig config, FishStats stats)
        {
            var w = config.Arena.CardumeStrength.Weights;
            return stats.Attack * w.Attack + stats.Defense * w.Defense + stats.Hp / w.HpDivisor + stats.Speed * w.Speed;
        }

        /// <summary>Raw strength scaled for display and rounded.</summary>
        public static long Display(GameConfig config, double raw)
        {
            return (long)Math.Round(raw * config.Arena.CardumeStrength.DisplayScale, MidpointRounding.AwayFromZero);
        }
    }
}
