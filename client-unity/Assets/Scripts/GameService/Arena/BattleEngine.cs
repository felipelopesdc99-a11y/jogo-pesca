using System;
using System.Collections.Generic;
using System.Linq;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;

namespace FishingIdle.GameService.Arena
{
    /// <summary>One fish entering a battle, with the attributes it fights with (modifiers applied).</summary>
    public sealed class Fighter
    {
        public string SpeciesId { get; set; }
        public string SpeciesName { get; set; }
        public string RarityId { get; set; }
        public string RarityName { get; set; }
        public int Level { get; set; }
        public FishStats Stats { get; set; }
    }

    /// <summary>A hit in the replay. Time is in battle seconds at 1x.</summary>
    public sealed class BattleEvent
    {
        public double Time { get; internal set; }

        /// <summary>0 = side A (the attacker), 1 = side B (the defender).</summary>
        public int AttackerSide { get; internal set; }

        /// <summary>Formation positions, 1–6.</summary>
        public int AttackerPosition { get; internal set; }
        public int TargetPosition { get; internal set; }
        public double Damage { get; internal set; }
        public double TargetHpAfter { get; internal set; }
        public bool TargetDefeated { get; internal set; }
    }

    public sealed class BattleOutcome
    {
        /// <summary>0 when side A (the attacker) won, 1 when side B won.</summary>
        public int Winner { get; internal set; }

        public double DurationSeconds { get; internal set; }
        public List<BattleEvent> Events { get; } = new List<BattleEvent>();
    }

    /// <summary>
    /// Automatic Cardume battle (GDD sections 24–27). Resolved instantly and completely; the client only
    /// replays the events. Four stats only, fixed target order 1→6, a defeated slot stays empty, and
    /// the only randomness is the small damage roll (arena.json → combat.damage_roll).
    /// </summary>
    public static class BattleEngine
    {
        /// <summary>Safety net only: with the minimum damage floor every battle ends long before this.</summary>
        private const int MaxEvents = 50000;

        public static BattleOutcome Resolve(GameConfig config, IReadOnlyList<Fighter> sideA, IReadOnlyList<Fighter> sideB, Rng rng)
        {
            var combat = config.Arena.Combat;
            var priority = config.Arena.Formation?.TargetPriority ?? new List<int> { 1, 2, 3, 4, 5, 6 };
            var hp = new[] { Hp(sideA), Hp(sideB) };
            var teams = new[] { sideA, sideB };
            var next = new[] { FirstTimes(combat, sideA), FirstTimes(combat, sideB) };
            var outcome = new BattleOutcome();

            if (!Alive(hp[0]) || !Alive(hp[1]))
            {
                outcome.Winner = Alive(hp[0]) ? 0 : 1;
                return outcome;
            }

            var time = 0.0;
            for (var guard = 0; guard < MaxEvents; guard++)
            {
                // The next attacker is whoever is due first; ties go to side A, then to the lower position.
                var side = -1;
                var index = -1;
                var best = double.MaxValue;
                for (var s = 0; s < 2; s++)
                {
                    for (var i = 0; i < 6; i++)
                    {
                        if (hp[s][i] > 0 && next[s][i] < best - 1e-9)
                        {
                            best = next[s][i];
                            side = s;
                            index = i;
                        }
                    }
                }

                time = best;
                var enemy = 1 - side;
                var target = priority.Select(p => p - 1).FirstOrDefault(i => i >= 0 && i < 6 && hp[enemy][i] > 0);
                var attacker = teams[side][index].Stats;
                var defender = teams[enemy][target].Stats;

                var roll = combat.DamageRoll.Min + rng.NextDouble() * (combat.DamageRoll.Max - combat.DamageRoll.Min);
                var mitigated = attacker.Attack * roll * (1.0 - defender.Defense / (defender.Defense + combat.DefenseMitigation.Constant));
                var damage = Math.Max(mitigated, attacker.Attack * combat.DefenseMitigation.MinimumDamageRatioOfAttack);

                hp[enemy][target] = Math.Max(0, hp[enemy][target] - damage);
                outcome.Events.Add(new BattleEvent
                {
                    Time = time,
                    AttackerSide = side,
                    AttackerPosition = index + 1,
                    TargetPosition = target + 1,
                    Damage = damage,
                    TargetHpAfter = hp[enemy][target],
                    TargetDefeated = hp[enemy][target] <= 0,
                });

                next[side][index] += Interval(combat, attacker.Speed);
                if (!Alive(hp[enemy]))
                {
                    outcome.Winner = side;
                    outcome.DurationSeconds = time;
                    return outcome;
                }
            }

            // Unreachable in practice; decide by remaining health so the result is still defined.
            outcome.Winner = hp[0].Sum() / Math.Max(1, MaxHp(sideA)) >= hp[1].Sum() / Math.Max(1, MaxHp(sideB)) ? 0 : 1;
            outcome.DurationSeconds = time;
            return outcome;
        }

        /// <summary>interval = base × (reference speed ÷ speed): faster fish attack a little more often.</summary>
        public static double Interval(CombatConfig combat, double speed)
        {
            return combat.Speed.BaseIntervalSeconds * (combat.Speed.ReferenceSpeed / Math.Max(1, speed));
        }

        private static double[] Hp(IReadOnlyList<Fighter> side)
        {
            var hp = new double[6];
            for (var i = 0; i < 6; i++)
            {
                hp[i] = i < side.Count && side[i] != null ? side[i].Stats.Hp : 0;
            }

            return hp;
        }

        private static double MaxHp(IReadOnlyList<Fighter> side) => side.Where(f => f != null).Sum(f => f.Stats.Hp);

        private static double[] FirstTimes(CombatConfig combat, IReadOnlyList<Fighter> side)
        {
            // The first attack comes one interval after t = 0 (arena.json: first_attack_uses_same_interval_from_t0).
            var times = new double[6];
            for (var i = 0; i < 6; i++)
            {
                times[i] = i < side.Count && side[i] != null ? Interval(combat, side[i].Stats.Speed) : double.MaxValue;
            }

            return times;
        }

        private static bool Alive(double[] hp) => hp.Any(h => h > 0);
    }
}
