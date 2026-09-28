using System;
using FishingIdle.GameService.Fishing;

namespace FishingIdle.GameService
{
    /// <summary>Read access to the player's own summary (level, coins, map, rod).</summary>
    public interface IPlayerService
    {
        PlayerView GetPlayer();
    }

    public sealed class LocalPlayerService : IPlayerService
    {
        private readonly GameSession _session;

        public LocalPlayerService(GameSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public PlayerView GetPlayer()
        {
            var save = _session.Save;
            var config = _session.Config;
            config.TryGetMap(save.CurrentMapId, out var map);
            config.TryGetRod(save.EquippedRod.RodId, out var rod);

            return new PlayerView
            {
                PlayerName = save.PlayerName,
                FisherLevel = save.FisherLevel,
                FisherMaxLevel = config.Progression.Fisher.MaxLevel,
                FisherXp = save.FisherXp,
                FisherXpToNext = config.FisherXpToNextLevel(save.FisherLevel),
                Coins = save.Coins,
                Shells = save.Shells,
                MapId = save.CurrentMapId,
                MapName = map?.DisplayName ?? save.CurrentMapId,
                RodName = rod?.DisplayName ?? save.EquippedRod.RodId,
                RodLevel = save.EquippedRod.Level,
                RodHasLevels = rod != null && rod.HasInternalLevels,
                TotalCatches = save.Stats.TotalCatches,
                FishingBoxCount = save.FishingBox.Count,
                SpeciesDiscovered = save.SpeciesRecords.Count,
            };
        }
    }
}
