using System;
using System.Collections.Generic;
using System.Linq;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;
using FishingIdle.GameService.Profile;

namespace FishingIdle.GameService.Arena
{
    /// <summary>A simulated Arena opponent (local MVP). Always generated the same way from its number.</summary>
    public sealed class ArenaBot
    {
        public string Id { get; internal set; }
        public string Name { get; internal set; }
        public List<Fighter> Cardume { get; } = new List<Fighter>();
    }

    /// <summary>
    /// Builds the local opponents from arena_bots.json. Bot N (0 = strongest) keeps the same Cardume for
    /// as long as the config is the same; only its rank changes as battles happen.
    /// </summary>
    public static class ArenaBots
    {
        public const string PlayerId = "player";
        private const string Prefix = "bot:";
        private const ulong BotSeed = 0xB07_5EED_2026UL;

        public static string IdFor(int index) => Prefix + index;

        public static bool TryParse(string id, out int index)
        {
            index = -1;
            return id != null && id.StartsWith(Prefix, StringComparison.Ordinal) && int.TryParse(id.Substring(Prefix.Length), out index);
        }

        public static ArenaBot Build(GameConfig config, int index)
        {
            var bots = config.ArenaBots;
            var st = bots.StrengthByRank;
            var f = bots.BotCount <= 1 ? 0.0 : (double)index / (bots.BotCount - 1);
            var rng = Rng.For(BotSeed, index, 7);

            var names = bots.Names;
            var name = names[index % names.Count] + (index >= names.Count ? " " + (index / names.Count + 1) : string.Empty);
            var bot = new ArenaBot { Id = IdFor(index), Name = name };

            var size = (int)Math.Round(Lerp(st.TopCardumeSize, st.BottomCardumeSize, f));
            var level = (int)Math.Round(Lerp(st.TopFishLevel, st.BottomFishLevel, f));
            var percentile = Lerp(st.TopSizePercentile, st.BottomSizePercentile, f);
            var mapId = f < st.SecondMapRankFraction ? st.SecondMapId : st.FirstMapId;
            if (!config.TryGetMap(mapId, out var map))
            {
                map = config.StartingMap;
            }

            var pool = map.FishPool.Where(e => config.TryGetSpecies(e.SpeciesId, out _)).ToList();
            var fish = new List<Fighter>();
            for (var i = 0; i < Math.Max(1, Math.Min(6, size)); i++)
            {
                config.TryGetSpecies(pool[rng.NextIntInclusive(0, pool.Count - 1)].SpeciesId, out var species);
                var p = Math.Min(1, Math.Max(0, percentile + (rng.NextDouble() - 0.5) * 0.1));
                var mm = (int)Math.Round((species.SizeCm.Min + p * (species.SizeCm.Max - species.SizeCm.Min)) * 10);
                config.TryGetRarity(species.Rarity, out var rarity);
                fish.Add(new Fighter
                {
                    SpeciesId = species.Id,
                    SpeciesName = species.DisplayName,
                    RarityId = species.Rarity,
                    RarityName = rarity?.DisplayName ?? species.Rarity,
                    Level = Math.Max(1, Math.Min(config.Progression.FishLevel.MaxLevel, level)),
                    Stats = FishRules.Stats(config, species, mm, level),
                });
            }

            // Toughest fish in front, like a sensible player would.
            fish = fish.OrderByDescending(x => x.Stats.Hp + x.Stats.Defense * 4).ToList();
            var bonus = CardumeRules.CompleteBonusActive(config, fish.Count);
            foreach (var x in fish)
            {
                x.Stats = CardumeRules.Effective(config, x.Stats, bonus);
                bot.Cardume.Add(x);
            }

            while (bot.Cardume.Count < 6)
            {
                bot.Cardume.Add(null);
            }

            return bot;
        }

        private static double Lerp(double a, double b, double t) => a + (b - a) * t;
    }
}
