using System;
using System.Collections.Generic;
using System.Linq;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;
using FishingIdle.GameService.Persistence;

namespace FishingIdle.GameService.Maps
{
    public sealed class MapView
    {
        public string MapId { get; internal set; }
        public string Name { get; internal set; }
        public string Summary { get; internal set; }
        /// <summary>The Fisher level that opens the map: the only requirement since M24-T58 (A-180).</summary>
        public int UnlockFisherLevel { get; internal set; }

        public bool IsCurrent { get; internal set; }
        public bool LevelUnlocked { get; internal set; }

        /// <summary>
        /// Information, not a lock (M24-T58, A-180): names of the rarities of this map the rod in use cannot catch (rods.json
        /// → can_catch_rarities), lowest first. Empty when the rod catches everything here or there is no rod yet.
        /// </summary>
        public List<string> RaritiesRodMisses { get; } = new List<string>();
        public int SpeciesTotal { get; internal set; }
        public int SpeciesDiscovered { get; internal set; }

        /// <summary>Why travelling there is refused right now; None when it is possible.</summary>
        public ServiceError TravelBlocker { get; internal set; }
    }

    public sealed class TravelView
    {
        public bool Active { get; internal set; }
        public string FromName { get; internal set; }
        public string ToMapId { get; internal set; }
        public string ToName { get; internal set; }
        public long StartedAtMs { get; internal set; }
        public long ArrivesAtMs { get; internal set; }
        public long NowMs { get; internal set; }

        public double Progress => !Active || ArrivesAtMs <= StartedAtMs ? 0 : Math.Min(1.0, Math.Max(0.0, (double)(NowMs - StartedAtMs) / (ArrivesAtMs - StartedAtMs)));
        public double SecondsLeft => Active ? Math.Max(0, (ArrivesAtMs - NowMs) / 1000.0) : 0;
    }

    public sealed class MapsView
    {
        public string CurrentMapId { get; internal set; }
        public TravelView Travel { get; internal set; }
        public double TravelSeconds { get; internal set; }
        public List<MapView> Maps { get; } = new List<MapView>();
    }

    /// <summary>What the last update changed, so the presentation can react (arrival rebuilds the scene).</summary>
    public sealed class MapUpdate
    {
        public bool Arrived { get; internal set; }
        public string MapId { get; internal set; }
    }

    /// <summary>Maps and travel (GDD section 18): manual only, 30 s, fishing paused on the way.</summary>
    public interface IMapService
    {
        MapsView GetMaps();

        TravelView GetTravel();

        ServiceResult<TravelView> TravelTo(string mapId);

        /// <summary>Completes a trip whose time is up. Safe to call as often as you like.</summary>
        MapUpdate Update();
    }

    public sealed class LocalMapService : IMapService
    {
        private readonly GameSession _session;
        private readonly LocalFishingService _fishing;

        public LocalMapService(GameSession session, LocalFishingService fishing)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _fishing = fishing ?? throw new ArgumentNullException(nameof(fishing));
            // A trip that ended while the game was closed is completed on start.
            Update();
        }

        private PlayerSave Save => _session.Save;
        private GameConfig Config => _session.Config;

        public MapsView GetMaps()
        {
            var view = new MapsView
            {
                CurrentMapId = Save.CurrentMapId,
                Travel = GetTravel(),
                TravelSeconds = Config.Maps.Travel.DurationSeconds,
            };

            var rod = EquippedRod();
            foreach (var map in Config.Maps.Maps)
            {
                var speciesIds = map.FishPool.Select(e => e.SpeciesId).Distinct().ToList();
                var entry = new MapView
                {
                    MapId = map.Id,
                    Name = map.DisplayName,
                    Summary = map.VisualTheme?.Summary,
                    UnlockFisherLevel = map.UnlockFisherLevel,
                    IsCurrent = map.Id == Save.CurrentMapId,
                    LevelUnlocked = Save.FisherLevel >= map.UnlockFisherLevel,
                    SpeciesTotal = speciesIds.Count,
                    SpeciesDiscovered = speciesIds.Count(Save.SpeciesRecords.ContainsKey),
                    TravelBlocker = Blocker(map),
                };
                if (rod?.CanCatchRarities != null && map.AvailableRarities != null)
                {
                    foreach (var tier in Config.Progression.Rarity.Tiers)
                    {
                        if (map.AvailableRarities.Contains(tier.Id) && !rod.CanCatchRarities.Contains(tier.Id))
                        {
                            entry.RaritiesRodMisses.Add(tier.DisplayName);
                        }
                    }
                }

                view.Maps.Add(entry);
            }

            return view;
        }

        public TravelView GetTravel()
        {
            var t = Save.Travel;
            var view = new TravelView { Active = t.Active, NowMs = _session.Clock.UtcNowMs };
            if (!t.Active)
            {
                return view;
            }

            view.FromName = Config.TryGetMap(t.FromMapId, out var from) ? from.DisplayName : t.FromMapId;
            view.ToMapId = t.ToMapId;
            view.ToName = Config.TryGetMap(t.ToMapId, out var to) ? to.DisplayName : t.ToMapId;
            view.StartedAtMs = t.StartedAtMs;
            view.ArrivesAtMs = t.ArrivesAtMs;
            return view;
        }

        public ServiceResult<TravelView> TravelTo(string mapId)
        {
            if (!Config.TryGetMap(mapId, out var map))
            {
                return ServiceResult<TravelView>.Fail(ServiceError.MapNotFound);
            }

            var blocker = Blocker(map);
            if (blocker != ServiceError.None)
            {
                return ServiceResult<TravelView>.Fail(blocker);
            }

            var now = _session.Clock.UtcNowMs;
            var resume = _fishing.PauseForTravel(new FishingUpdate());
            Save.Travel = new TravelState
            {
                Active = true,
                FromMapId = Save.CurrentMapId,
                ToMapId = map.Id,
                StartedAtMs = now,
                ArrivesAtMs = now + (long)Math.Round(Config.Maps.Travel.DurationSeconds * 1000.0),
                ResumeFishing = resume,
            };
            _session.Persist();
            _session.Log("Travel started: " + Save.Travel.FromMapId + " -> " + map.Id);
            return ServiceResult<TravelView>.Ok(GetTravel());
        }

        public MapUpdate Update()
        {
            var t = Save.Travel;
            var now = _session.Clock.UtcNowMs;
            if (!t.Active || now < t.ArrivesAtMs)
            {
                return new MapUpdate();
            }

            // Maps are places, not level states: the player only moves by choosing to travel.
            Save.CurrentMapId = Config.TryGetMap(t.ToMapId, out _) ? t.ToMapId : Save.CurrentMapId;
            var resume = t.ResumeFishing;
            Save.Travel = new TravelState();
            if (resume)
            {
                // Fishing restarts at the moment of arrival, not now: if the game was closed after the
                // boat arrived, that time is credited as offline fishing by the next sync.
                _fishing.ResumeAfterTravel(Math.Min(now, t.ArrivesAtMs));
                _fishing.Sync();
            }

            _session.Persist();
            _session.Log("Arrived at " + Save.CurrentMapId);
            return new MapUpdate { Arrived = true, MapId = Save.CurrentMapId };
        }

        /// <summary>Only the Fisher level opens a map (M24-T58, A-180): the rod in use never blocks a trip.</summary>
        private ServiceError Blocker(MapConfig map)
        {
            if (Save.Travel.Active) return ServiceError.Traveling;
            if (map.Id == Save.CurrentMapId) return ServiceError.AlreadyOnMap;
            if (Save.FisherLevel < map.UnlockFisherLevel) return ServiceError.MapLocked;
            return ServiceError.None;
        }

        private RodConfig EquippedRod()
        {
            return Config.TryGetRod(Save.EquippedRodItem()?.RodId, out var rod) ? rod : null;
        }
    }
}
