using System;
using System.Collections.Generic;
using FishingIdle.GameService.Aquarium;
using FishingIdle.GameService.Arena;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Expeditions;
using FishingIdle.GameService.Fishing;
using FishingIdle.GameService.Maps;
using FishingIdle.GameService.Market;
using FishingIdle.GameService.Persistence;
using FishingIdle.GameService.Profile;
using FishingIdle.GameService.Shop;
using FishingIdle.GameService.Tutorial;

namespace FishingIdle.GameService
{
    /// <summary>
    /// The local "server": loads the balance, opens the save and exposes the services.
    /// </summary>
    /// <remarks>
    /// This is the single place that decides the game runs locally. Swapping it for a remote
    /// implementation later means providing the same IFishingService / IPlayerService over HTTP.
    /// </remarks>
    public sealed class LocalGame
    {
        private LocalGame(GameSession session)
        {
            Session = session;
            var fishing = new LocalFishingService(session);
            Fishing = fishing;
            Maps = new LocalMapService(session, fishing);
            Shop = new LocalShopService(session, fishing);
            Player = new LocalPlayerService(session);
            var aquarium = new LocalAquariumService(session);
            Aquarium = aquarium;
            Cardume = new LocalCardumeService(session, aquarium);
            Profile = new LocalProfileService(session, Fishing, Cardume);
            Expeditions = new LocalExpeditionService(session, fishing, Cardume);
            Arena = new LocalArenaService(session, Cardume);
            var market = new LocalMarketService(session, aquarium);
            Market = market;
            Auctions = market;
            Tutorial = new LocalTutorialService(session);
        }

        public GameSession Session { get; }
        public IFishingService Fishing { get; }
        public IMapService Maps { get; }
        public IShopService Shop { get; }
        public IPlayerService Player { get; }
        public IAquariumService Aquarium { get; }
        public ICardumeService Cardume { get; }
        public IProfileService Profile { get; }
        public IExpeditionService Expeditions { get; }
        public IArenaService Arena { get; }
        public IMarketService Market { get; }
        public IAuctionService Auctions { get; }
        public ITutorialService Tutorial { get; }

        /// <summary>Starts the game service, or explains in PT-BR why it cannot.</summary>
        public static LocalGameStartResult Start(string configDirectory, string saveDirectory, IClock clock, Action<string> log)
        {
            var configResult = GameConfigLoader.LoadFromDirectory(configDirectory);
            if (!configResult.Succeeded)
            {
                return new LocalGameStartResult(null, configResult.Errors);
            }

            return Start(configResult.Config, new JsonFilePlayerRepository(saveDirectory), clock, log);
        }

        public static LocalGameStartResult Start(GameConfig config, IPlayerRepository repository, IClock clock, Action<string> log)
        {
            var session = new GameSession(config, repository, clock ?? new SystemClock(), log);
            return new LocalGameStartResult(new LocalGame(session), Array.Empty<string>());
        }
    }

    public sealed class LocalGameStartResult
    {
        public LocalGameStartResult(LocalGame game, IReadOnlyList<string> errors)
        {
            Game = game;
            Errors = errors;
        }

        public LocalGame Game { get; }

        /// <summary>PT-BR reasons the game could not start (usually invalid balance files).</summary>
        public IReadOnlyList<string> Errors { get; }

        public bool Succeeded => Game != null;
    }
}
