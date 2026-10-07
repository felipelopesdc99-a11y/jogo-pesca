using System;
using System.Collections.Generic;
using System.Linq;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;
using FishingIdle.GameService.Persistence;

namespace FishingIdle.GameService.Dev
{
    public enum DevCurrency
    {
        Coins,
        Shells,
        Dollars,
        Honor,
    }

    /// <summary>One species the test tools can hand out.</summary>
    public sealed class DevSpeciesView
    {
        public string SpeciesId { get; internal set; }
        public string Name { get; internal set; }
        public string RarityId { get; internal set; }
        public string RarityName { get; internal set; }
        public string MapName { get; internal set; }
    }

    /// <summary>
    /// The owner's test tools (A-123): give currencies and fish, set the level, fill Energy and move time
    /// forward, so every system can be tested without waiting. They change the save through the same
    /// game service as everything else, and only while <see cref="GameSession.DevToolsEnabled"/> is on
    /// (Unity Editor or development build). A release build and a future server never enable them.
    /// </summary>
    public interface IDevToolsService
    {
        bool Enabled { get; }

        /// <summary>Every species, by map then rarity, for the "give fish" list.</summary>
        IReadOnlyList<DevSpeciesView> Species();

        ServiceResult<long> Give(DevCurrency currency, long amount);

        /// <summary>Puts <paramref name="count"/> fish of a species in the Fishing Box (no XP). Size: a category id or null for random.</summary>
        ServiceResult<int> GiveFish(string speciesId, string sizeCategoryId, int count);

        ServiceResult<int> SetFisherLevel(int level);

        ServiceResult<int> FillArenaEnergy();

        /// <summary>
        /// Moves the game clock forward. Online: the time counts as the game open (online fishing every 30 s).
        /// Offline: as the game closed (offline fishing, the "Bem-vindo de volta" summary). Expeditions,
        /// Energy and everything else on a timer move too.
        /// </summary>
        ServiceResult<long> AdvanceTime(double hours, bool online);
    }

    public sealed class LocalDevToolsService : IDevToolsService
    {
        private const double MaxHoursPerStep = 72;
        private readonly GameSession _session;
        private readonly LocalFishingService _fishing;

        public LocalDevToolsService(GameSession session, LocalFishingService fishing)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _fishing = fishing ?? throw new ArgumentNullException(nameof(fishing));
        }

        private PlayerSave Save => _session.Save;
        private GameConfig Config => _session.Config;

        public bool Enabled => _session.DevToolsEnabled;

        public IReadOnlyList<DevSpeciesView> Species()
        {
            var mapOf = new Dictionary<string, MapConfig>(StringComparer.Ordinal);
            foreach (var map in Config.Maps.Maps.OrderBy(m => m.UnlockFisherLevel))
            {
                foreach (var entry in map.FishPool)
                {
                    if (!mapOf.ContainsKey(entry.SpeciesId))
                    {
                        mapOf[entry.SpeciesId] = map;
                    }
                }
            }

            return Config.FishCatalog.Species
                .Select(s =>
                {
                    Config.TryGetRarity(s.Rarity, out var rarity);
                    mapOf.TryGetValue(s.Id, out var map);
                    return new DevSpeciesView
                    {
                        SpeciesId = s.Id,
                        Name = s.DisplayName,
                        RarityId = s.Rarity,
                        RarityName = rarity?.DisplayName ?? s.Rarity,
                        MapName = map?.DisplayName ?? string.Empty,
                    };
                })
                .OrderBy(v => mapOf.TryGetValue(v.SpeciesId, out var m) ? m.UnlockFisherLevel : int.MaxValue)
                .ThenBy(v => Config.RarityRank(v.RarityId))
                .ThenBy(v => v.Name, StringComparer.Ordinal)
                .ToList();
        }

        public ServiceResult<long> Give(DevCurrency currency, long amount)
        {
            if (!Enabled) return ServiceResult<long>.Fail(ServiceError.DevToolsDisabled);
            if (amount <= 0) return ServiceResult<long>.Fail(ServiceError.InvalidAmount);

            long total;
            switch (currency)
            {
                case DevCurrency.Shells: total = Save.Shells += amount; break;
                case DevCurrency.Dollars: total = Save.Dollars += amount; break;
                case DevCurrency.Honor: total = Save.Arena.Honor += amount; break;
                default: total = Save.Coins += amount; break;
            }

            Done("gave " + amount + " " + currency);
            return ServiceResult<long>.Ok(total);
        }

        public ServiceResult<int> GiveFish(string speciesId, string sizeCategoryId, int count)
        {
            if (!Enabled) return ServiceResult<int>.Fail(ServiceError.DevToolsDisabled);
            if (count <= 0) return ServiceResult<int>.Fail(ServiceError.InvalidAmount);
            if (!Config.TryGetSpecies(speciesId, out var species)) return ServiceResult<int>.Fail(ServiceError.SpeciesMissingFromConfig);

            var fixedCategory = sizeCategoryId == null ? null : Config.SizeCategories.FirstOrDefault(c => c.Id == sizeCategoryId);
            var capacity = Config.Economy.FishingBox?.Capacity ?? 0;
            var update = new FishingUpdate();
            var given = 0;
            for (var i = 0; i < count; i++)
            {
                if (capacity > 0 && Save.FishingBox.Count >= capacity)
                {
                    break;
                }

                var rng = Rng.For(Save.RngSeed ^ 0xDE70_0150UL, Save.NextCatchId, i);
                var category = fixedCategory ?? CatchRules.RollSizeCategory(Config, new RodBonusesConfig(), rng, species.Rarity);
                var percentile = category.PercentileMin + rng.NextDouble() * (category.PercentileMax - category.PercentileMin);
                var sizeCm = species.SizeCm.Min + percentile * (species.SizeCm.Max - species.SizeCm.Min);
                _fishing.GrantCatch(new RolledCatch
                {
                    Species = species,
                    SizeCategory = category,
                    SizeMm = Math.Max(1, (int)Math.Round(sizeCm * 10.0, MidpointRounding.AwayFromZero)),
                    FisherXp = 0,
                    Shells = 0,
                }, update);
                given++;
            }

            Done("gave " + given + " x " + speciesId + " (" + (sizeCategoryId ?? "random") + ")");
            return ServiceResult<int>.Ok(given);
        }

        public ServiceResult<int> SetFisherLevel(int level)
        {
            if (!Enabled) return ServiceResult<int>.Fail(ServiceError.DevToolsDisabled);
            var max = Config.Progression.Fisher.MaxLevel;
            Save.FisherLevel = Math.Max(1, Math.Min(max, level));
            Save.FisherXp = 0;
            Done("fisher level set to " + Save.FisherLevel);
            return ServiceResult<int>.Ok(Save.FisherLevel);
        }

        public ServiceResult<int> FillArenaEnergy()
        {
            if (!Enabled) return ServiceResult<int>.Fail(ServiceError.DevToolsDisabled);
            Save.Arena.Energy = Config.Arena.Energy.Max;
            Save.Arena.EnergyUpdatedAtMs = _session.Clock.UtcNowMs;
            Done("arena energy filled");
            return ServiceResult<int>.Ok(Save.Arena.Energy);
        }

        public ServiceResult<long> AdvanceTime(double hours, bool online)
        {
            if (!Enabled) return ServiceResult<long>.Fail(ServiceError.DevToolsDisabled);
            if (hours <= 0 || hours > MaxHoursPerStep) return ServiceResult<long>.Fail(ServiceError.InvalidAmount);

            Save.DevTimeOffsetMs += (long)Math.Round(hours * 3600000.0);
            if (online)
            {
                // The jump counts as time the game was open: the next Sync settles it as online cycles.
                _fishing.MarkSeenNow();
            }

            Done("advanced " + hours + " h (" + (online ? "online" : "offline") + ")");
            return ServiceResult<long>.Ok(_session.Clock.UtcNowMs);
        }

        private void Done(string what)
        {
            _session.Persist();
            _session.Log("[test tools] " + what + ".");
        }
    }
}
