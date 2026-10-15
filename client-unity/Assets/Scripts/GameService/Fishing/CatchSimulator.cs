using System;
using System.Collections.Generic;
using System.Linq;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;

namespace FishingIdle.GameService.Fishing
{
    /// <summary>Caught and escaped fish of one rarity in a simulation.</summary>
    public sealed class RaritySimulationRow
    {
        public string RarityId { get; internal set; }
        public string RarityName { get; internal set; }
        public int Bites { get; internal set; }
        public int Caught { get; internal set; }
        public double Chance { get; internal set; }
    }

    /// <summary>What a batch of fishing attempts produced (Dev Panel and tools/Simulador).</summary>
    public sealed class CatchSimulationResult
    {
        public int Attempts { get; internal set; }
        public int Catches { get; internal set; }
        public int Escapes => Attempts - Catches;
        public double SuccessRate => Attempts > 0 ? Catches / (double)Attempts : 0;
        public long FisherXp { get; internal set; }

        /// <summary>NPC sale value of everything caught.</summary>
        public long Coins { get; internal set; }
        public long Shells { get; internal set; }
        public List<RaritySimulationRow> ByRarity { get; } = new List<RaritySimulationRow>();

        /// <summary>Attempts per hour of online fishing (one per online cycle).</summary>
        public double AttemptsPerHour { get; internal set; }
        public double CatchesPerHour => AttemptsPerHour * SuccessRate;
        public double EscapesPerHour => AttemptsPerHour - CatchesPerHour;
        public double XpPerHour => Attempts > 0 ? FisherXp / (double)Attempts * AttemptsPerHour : 0;
        public double CoinsPerHour => Attempts > 0 ? Coins / (double)Attempts * AttemptsPerHour : 0;
        public double ShellsPerHour => Attempts > 0 ? Shells / (double)Attempts * AttemptsPerHour : 0;

        /// <summary>What keeping the bait on costs per hour (one charge per attempt).</summary>
        public double BaitCoinsPerHour { get; internal set; }
        public double BaitShellsPerHour { get; internal set; }
    }

    /// <summary>
    /// Runs the real Catch Success rules for a gear combination, without a save: map + rod + level +
    /// boat (+ its level, M24-T13) + bait + N attempts (docs/SISTEMA_SUCESSO_PESCA.md, section 22). The bait is assumed
    /// kept on for every attempt.
    /// </summary>
    public static class CatchSimulator
    {
        public static CatchSimulationResult Run(GameConfig config, MapConfig map, RodConfig rod, int rodLevel, BoatConfig boat, BaitConfig bait, int attempts, ulong seed, int boatLevel = 1)
        {
            var rng = new Rng(seed);
            var gear = (boat != null ? config.BoatBonusAt(boat, boatLevel) : 0) + (bait?.CatchSuccessBonus ?? 0);
            var rows = new Dictionary<string, RaritySimulationRow>(StringComparer.Ordinal);
            var result = new CatchSimulationResult { AttemptsPerHour = 3600.0 / Math.Max(1.0, config.Fishing.OnlineCycleSeconds) };

            for (var i = 0; i < attempts; i++)
            {
                var attempt = CatchRules.Attempt(config, map, rod, rodLevel, gear, rng);
                var rarity = attempt.Species.Rarity;
                if (!rows.TryGetValue(rarity, out var row))
                {
                    config.TryGetRarity(rarity, out var tier);
                    row = new RaritySimulationRow { RarityId = rarity, RarityName = tier?.DisplayName ?? rarity, Chance = attempt.Chance };
                    rows[rarity] = row;
                }

                row.Bites++;
                result.Attempts++;
                if (!attempt.Caught)
                {
                    continue;
                }

                row.Caught++;
                result.Catches++;
                result.FisherXp += attempt.Catch.FisherXp;
                result.Shells += attempt.Catch.Shells;
                result.Coins += CatchRules.SalePrice(config, attempt.Species, attempt.Catch.SizeMm);
            }

            result.ByRarity.AddRange(rows.Values.OrderBy(r => config.RarityRank(r.RarityId)));
            if (bait != null && bait.Charges > 0)
            {
                result.BaitCoinsPerHour = bait.CostCoins / (double)bait.Charges * result.AttemptsPerHour;
                result.BaitShellsPerHour = bait.CostShells / (double)bait.Charges * result.AttemptsPerHour;
            }

            return result;
        }
    }
}
