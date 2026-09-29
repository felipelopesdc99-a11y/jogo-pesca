using System;
using System.Collections.Generic;
using FishingIdle.Game.Scene;
using FishingIdle.Game.UI;
using FishingIdle.GameService;
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
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.Bootstrap
{
    /// <summary>
    /// The one object that connects the Unity presentation to the game service.
    /// </summary>
    /// <remarks>
    /// Presentation components never touch the save or roll anything. They read snapshots from
    /// here (<see cref="Status"/>, <see cref="Player"/>) and send intents through the methods
    /// below; results come back from the service and are broadcast with <see cref="CatchesArrived"/>.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class GameRoot : MonoBehaviour
    {
        /// <summary>How often the client asks the service to settle finished cycles.</summary>
        private const float SyncIntervalSeconds = 0.25f;

        private float _nextSyncAt;

        public static GameRoot Instance { get; private set; }

        /// <summary>The running local game service. Null when startup failed.</summary>
        public LocalGame Game { get; private set; }

        /// <summary>PT-BR problems that prevented the game from starting (shown full screen).</summary>
        public IReadOnlyList<string> StartupErrors { get; private set; } = Array.Empty<string>();

        public string StartupErrorTitle { get; private set; }

        public FishingStatus Status { get; private set; }

        public PlayerView Player { get; private set; }

        /// <summary>The "while you were away" summary waiting to be shown, or null.</summary>
        public OfflineReport WelcomeBack { get; set; }

        /// <summary>A finished Expedition waiting to be shown, or null.</summary>
        public ExpeditionResultView ExpeditionResult { get; private set; }

        /// <summary>The tutorial step to show (GDD section 40); Active is false once it is over.</summary>
        public TutorialView Tutorial { get; private set; }

        /// <summary>The trip between maps in progress, refreshed every frame.</summary>
        public TravelView Travel { get; private set; }

        /// <summary>Raised when the player arrives on a new map (the scene rebuilds its scenery).</summary>
        public event Action<string> MapChanged;

        public ToastFeed Toasts { get; } = new ToastFeed();

        /// <summary>Raised whenever the service produced catches (sync, stop).</summary>
        public event Action<FishingUpdate> CatchesArrived;

        /// <summary>Raised after anything that may change what the Fishing Box shows.</summary>
        public event Action BoxChanged;

        /// <summary>Raised after anything that may change what the Aquarium shows.</summary>
        public event Action AquariumChanged;

        public bool IsRunning => Game != null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // An idle game keeps fishing when its window is not focused.
            Application.runInBackground = true;
            Application.targetFrameRate = 60;

            StartGameService();

            var scene = gameObject.AddComponent<FishingScene>();
            scene.Build(this);
            gameObject.AddComponent<Hud>();
            gameObject.AddComponent<FishingIdle.Game.Audio.GameAudio>();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (!IsRunning)
            {
                return;
            }

            if (Time.unscaledTime >= _nextSyncAt)
            {
                _nextSyncAt = Time.unscaledTime + SyncIntervalSeconds;
                Guard(() =>
                {
                    var arrival = Game.Maps.Update();
                    if (arrival.Arrived)
                    {
                        Refresh();
                        Toasts.Push(GameTexts.Map.Arrived(Player.MapName), ToastKind.Info);
                        MapChanged?.Invoke(arrival.MapId);
                        AquariumChanged?.Invoke();
                    }

                    var update = Game.Fishing.Sync();
                    if (update.HasChanges)
                    {
                        Publish(update);
                    }

                    TakeOfflineReport();
                    if (Game.Expeditions.Update() != null)
                    {
                        Toasts.Push(GameTexts.Expedition.CompletedToast, ToastKind.Important, notify: true);
                        Refresh();
                        BoxChanged?.Invoke();
                        AquariumChanged?.Invoke();
                    }

                    ExpeditionResult = Game.Expeditions.PendingResult();

                    foreach (var defense in Game.Arena.Update())
                    {
                        Toasts.Push(defense.PlayerWon
                            ? GameTexts.Arena.DefenseWon(defense.OpponentName, Format.Number(defense.HonorChange))
                            : GameTexts.Arena.DefenseLost(defense.OpponentName, defense.RankAfter), defense.PlayerWon ? ToastKind.Info : ToastKind.Warning, notify: true);
                    }

                    RefreshTutorial();

                    var marketNews = Game.Market.Update();
                    foreach (var news in marketNews)
                    {
                        PushMarketNews(news);
                    }

                    if (marketNews.Count > 0)
                    {
                        Refresh();
                    }
                });
            }

            // A failure inside the sync stops the game service; nothing more to read this frame.
            if (!IsRunning)
            {
                return;
            }

            Status = Game.Fishing.GetStatus();
            Travel = Game.Maps.GetTravel();
        }

        private void OnApplicationQuit()
        {
            if (IsRunning)
            {
                Guard(() => Game.Fishing.MarkSeen());
            }
        }

        // ------------------------------------------------------------------ intents

        public void StartFishing()
        {
            Guard(() =>
            {
                var result = Game.Fishing.StartFishing();
                if (!result.Succeeded)
                {
                    Toasts.Push(result.ErrorMessage, ToastKind.Warning);
                    return;
                }

                Toasts.Push(GameTexts.Toasts.FishingStarted, ToastKind.Info);
                Refresh();
            });
        }

        public void StopFishing()
        {
            Guard(() =>
            {
                var result = Game.Fishing.StopFishing();
                if (!result.Succeeded)
                {
                    Toasts.Push(result.ErrorMessage, ToastKind.Warning);
                    return;
                }

                if (result.Value.HasChanges)
                {
                    Publish(result.Value);
                }

                Toasts.Push(GameTexts.Toasts.FishingStopped, ToastKind.Info);
                Refresh();
            });
        }

        public IReadOnlyList<CatchView> GetFishingBox()
        {
            return IsRunning ? Game.Fishing.GetFishingBox() : Array.Empty<CatchView>();
        }

        public SalePreview PreviewSale(IReadOnlyCollection<long> ids)
        {
            return Game.Fishing.PreviewSale(ids);
        }

        /// <summary>Asks the service to sell. Returns true when the sale happened.</summary>
        public bool Sell(IReadOnlyCollection<long> ids)
        {
            var sold = false;
            Guard(() =>
            {
                var result = Game.Fishing.SellCatches(ids);
                if (!result.Succeeded)
                {
                    Toasts.Push(result.ErrorMessage, ToastKind.Warning);
                    return;
                }

                sold = true;
                Toasts.Push(
                    GameTexts.Box.Sold(result.Value.Count, Format.Number(result.Value.CoinsGained)),
                    ToastKind.Coins);
                Refresh();
                BoxChanged?.Invoke();
            });
            return sold;
        }

        // ------------------------------------------------------------------ Aquarium intents

        public AquariumView GetAquarium(AquariumSort sort)
        {
            return IsRunning ? Game.Aquarium.GetAquarium(sort) : null;
        }

        public FishView GetFish(long fishId)
        {
            return IsRunning ? Game.Aquarium.GetFish(fishId) : null;
        }

        /// <summary>Moves Fishing Box catches to the Aquarium. Returns true when it happened.</summary>
        public bool KeepCatches(IReadOnlyCollection<long> catchIds)
        {
            var kept = false;
            Guard(() =>
            {
                var result = Game.Aquarium.KeepCatches(catchIds);
                if (!result.Succeeded)
                {
                    Toasts.Push(result.ErrorMessage, ToastKind.Warning);
                    return;
                }

                kept = true;
                Toasts.Push(GameTexts.Aquarium.Kept(result.Value.Kept.Count, result.Value.AquariumCount, result.Value.Capacity), ToastKind.Info);
                Refresh();
                BoxChanged?.Invoke();
                AquariumChanged?.Invoke();
            });
            return kept;
        }

        public ServiceResult<FeedPreview> PreviewFeed(long targetId, IReadOnlyCollection<long> boxIds, IReadOnlyCollection<long> fishIds)
        {
            return Game.Aquarium.PreviewFeed(targetId, boxIds, fishIds);
        }

        public bool Feed(long targetId, IReadOnlyCollection<long> boxIds, IReadOnlyCollection<long> fishIds)
        {
            var fed = false;
            Guard(() =>
            {
                var result = Game.Aquarium.Feed(targetId, boxIds, fishIds);
                if (!result.Succeeded)
                {
                    Toasts.Push(result.ErrorMessage, ToastKind.Warning);
                    return;
                }

                fed = true;
                var p = result.Value;
                Toasts.Push(GameTexts.Aquarium.Fed(Format.Number(p.XpGained), p.LevelAfter), p.LevelAfter > p.LevelBefore ? ToastKind.LevelUp : ToastKind.Info);
                Refresh();
                BoxChanged?.Invoke();
                AquariumChanged?.Invoke();
            });
            return fed;
        }

        public SalePreview PreviewFishSale(IReadOnlyCollection<long> fishIds)
        {
            return Game.Aquarium.PreviewSale(fishIds);
        }

        public bool SellFish(IReadOnlyCollection<long> fishIds)
        {
            var sold = false;
            Guard(() =>
            {
                var result = Game.Aquarium.SellFish(fishIds);
                if (!result.Succeeded)
                {
                    Toasts.Push(result.ErrorMessage, ToastKind.Warning);
                    return;
                }

                sold = true;
                Toasts.Push(GameTexts.Box.Sold(result.Value.Count, Format.Number(result.Value.CoinsGained)), ToastKind.Coins);
                Refresh();
                AquariumChanged?.Invoke();
            });
            return sold;
        }

        // ------------------------------------------------------------------ Profile and Cardume intents

        public ProfileView GetProfile() => IsRunning ? Game.Profile.GetProfile() : null;

        public CardumeView GetCardume() => IsRunning ? Game.Cardume.GetCardume() : null;

        public void SetCardumeSlot(int position, long fishId)
        {
            Guard(() =>
            {
                var result = Game.Cardume.SetSlot(position, fishId);
                if (!result.Succeeded)
                {
                    Toasts.Push(result.ErrorMessage, ToastKind.Warning);
                }

                AquariumChanged?.Invoke();
            });
        }

        public void ClearCardumeSlot(int position)
        {
            Guard(() =>
            {
                var result = Game.Cardume.ClearSlot(position);
                if (!result.Succeeded)
                {
                    Toasts.Push(result.ErrorMessage, ToastKind.Warning);
                }

                AquariumChanged?.Invoke();
            });
        }

        public void EquipRod(long itemId)
        {
            Guard(() =>
            {
                var result = Game.Profile.EquipRod(itemId);
                if (!result.Succeeded)
                {
                    Toasts.Push(result.ErrorMessage, ToastKind.Warning);
                    return;
                }

                Refresh();
                AquariumChanged?.Invoke();
            });
        }

        // ------------------------------------------------------------------ Arena intents

        public ArenaView GetArena() => IsRunning ? Game.Arena.GetArena() : null;

        public void RerollOpponents()
        {
            Guard(() =>
            {
                var result = Game.Arena.Reroll();
                if (!result.Succeeded)
                {
                    Toasts.Push(result.ErrorMessage, ToastKind.Warning);
                }
            });
        }

        /// <summary>Asks for a battle. Returns the resolved report for the replay, or null when refused.</summary>
        public BattleReport Attack(int opponentIndex)
        {
            BattleReport report = null;
            Guard(() =>
            {
                var result = Game.Arena.Attack(opponentIndex);
                if (!result.Succeeded)
                {
                    Toasts.Push(result.ErrorMessage, ToastKind.Warning);
                    return;
                }

                report = result.Value;
            });
            return report;
        }

        // ------------------------------------------------------------------ Tutorial intents

        public void AcknowledgeTutorial(string step)
        {
            Guard(() =>
            {
                if (Game.Tutorial.Acknowledge(step).Succeeded)
                {
                    RefreshTutorial();
                }
            });
        }

        public void SkipTutorial()
        {
            Guard(() =>
            {
                Game.Tutorial.Skip();
                RefreshTutorial();
                Refresh();
            });
        }

        private void RefreshTutorial()
        {
            var wasActive = Tutorial != null && Tutorial.Active;
            Tutorial = Game.Tutorial.Get();
            if (wasActive && !Tutorial.Active)
            {
                Toasts.Push(GameTexts.Tutorial.Completed, ToastKind.Important);
            }
        }

        // ------------------------------------------------------------------ Market intents

        public MarketView GetMarket() => IsRunning ? Game.Market.GetMarket() : null;

        public List<ListingView> SearchMarket(MarketQuery query) => IsRunning ? Game.Market.Search(query) : new List<ListingView>();

        public MarketFilterOptions GetMarketFilters() => IsRunning ? Game.Market.GetFilterOptions() : new MarketFilterOptions();

        public List<SellCandidateView> GetSellCandidates() => IsRunning ? Game.Market.GetSellCandidates() : new List<SellCandidateView>();

        /// <summary>Buys a listing. Returns true when it happened.</summary>
        public bool BuyListing(long listingId)
        {
            var bought = false;
            Guard(() =>
            {
                var result = Game.Market.Buy(listingId);
                if (!result.Succeeded)
                {
                    Toasts.Push(result.ErrorMessage, ToastKind.Warning);
                    return;
                }

                bought = true;
                Toasts.Push(GameTexts.Market.Bought(result.Value.Item.Goods.Name), ToastKind.Info);
                Refresh();
            });
            return bought;
        }

        /// <summary>Lists an Aquarium fish (isFish) or an Inventory rod. Returns true when it happened.</summary>
        public bool CreateListing(bool isFish, long sourceId, long price)
        {
            var listed = false;
            Guard(() =>
            {
                var result = isFish ? Game.Market.ListFish(sourceId, price) : Game.Market.ListRod(sourceId, price);
                if (!result.Succeeded)
                {
                    Toasts.Push(result.ErrorMessage, ToastKind.Warning);
                    return;
                }

                listed = true;
                Toasts.Push(GameTexts.Market.Listed(result.Value.Goods.Name, Format.Number(result.Value.PriceCoins)), ToastKind.Info);
                Refresh();
                AquariumChanged?.Invoke();
            });
            return listed;
        }

        public void CancelListing(long listingId)
        {
            Guard(() =>
            {
                var result = Game.Market.CancelListing(listingId);
                if (!result.Succeeded)
                {
                    Toasts.Push(result.ErrorMessage, ToastKind.Warning);
                    return;
                }

                Toasts.Push(GameTexts.Market.Cancelled(result.Value.Goods.Name), ToastKind.Info);
            });
        }

        public void Withdraw(long withdrawalId)
        {
            Guard(() =>
            {
                var result = Game.Market.Withdraw(withdrawalId);
                if (!result.Succeeded)
                {
                    Toasts.Push(result.ErrorMessage, ToastKind.Warning);
                    return;
                }

                Toasts.Push(GameTexts.Market.Withdrawn(result.Value.Goods.Name), ToastKind.Info);
                Refresh();
                AquariumChanged?.Invoke();
            });
        }

        public void WithdrawAll()
        {
            Guard(() =>
            {
                var result = Game.Market.WithdrawAll();
                if (!result.Succeeded)
                {
                    Toasts.Push(result.ErrorMessage, ToastKind.Warning);
                    return;
                }

                Toasts.Push(GameTexts.Market.WithdrewAll(result.Value.Withdrawn, result.Value.LeftBehind), result.Value.LeftBehind > 0 ? ToastKind.Warning : ToastKind.Info);
                Refresh();
                AquariumChanged?.Invoke();
            });
        }

        private void PushMarketNews(MarketEventView news)
        {
            switch (news.Kind)
            {
                case MarketEvent.KindSold:
                    Toasts.Push(GameTexts.Market.Sold(news.BuyerName, news.GoodsName, Format.Number(news.NetCoins)), ToastKind.Coins, notify: true);
                    break;
                case MarketEvent.KindAuctionSold:
                    Toasts.Push(GameTexts.Market.AuctionSold(news.GoodsName, Format.Number(news.NetCoins)), ToastKind.Coins, notify: true);
                    break;
                case MarketEvent.KindAuctionUnsold:
                    Toasts.Push(GameTexts.Market.AuctionUnsold(news.GoodsName), ToastKind.Info, notify: true);
                    break;
                case MarketEvent.KindAuctionWon:
                    Toasts.Push(GameTexts.Market.AuctionWon(news.GoodsName), ToastKind.Important, notify: true);
                    break;
                case MarketEvent.KindAuctionLost:
                    Toasts.Push(GameTexts.Market.AuctionLost(news.GoodsName), ToastKind.Info, notify: true);
                    break;
                case MarketEvent.KindOutbid:
                    Toasts.Push(GameTexts.Market.Outbid(news.GoodsName, news.BuyerName), ToastKind.Warning, notify: true);
                    break;
                default:
                    Toasts.Push(GameTexts.Market.Expired(news.GoodsName), ToastKind.Info, notify: true);
                    break;
            }
        }

        public AuctionsView GetAuctions() => IsRunning ? Game.Auctions.GetAuctions() : null;

        public bool StartAuction(bool isFish, long sourceId, long startingBid)
        {
            var started = false;
            Guard(() =>
            {
                var result = Game.Auctions.StartAuction(isFish, sourceId, startingBid);
                if (!result.Succeeded)
                {
                    Toasts.Push(result.ErrorMessage, ToastKind.Warning);
                    return;
                }

                started = true;
                Toasts.Push(GameTexts.Market.AuctionStarted(result.Value.Goods.Name), ToastKind.Info);
                Refresh();
                AquariumChanged?.Invoke();
            });
            return started;
        }

        public void PlaceBid(long auctionId, long amount)
        {
            Guard(() =>
            {
                var result = Game.Auctions.PlaceBid(auctionId, amount);
                if (!result.Succeeded)
                {
                    Toasts.Push(result.ErrorMessage, ToastKind.Warning);
                    return;
                }

                Toasts.Push(GameTexts.Market.BidPlaced(result.Value.Auction.Goods.Name, Format.Number(amount), Format.Number(result.Value.FeeCoins)), ToastKind.Info);
                Refresh();
            });
        }

        public void EndAuctionNow(long auctionId)
        {
            Guard(() =>
            {
                var result = Game.Auctions.EndAuctionNow(auctionId);
                if (!result.Succeeded)
                {
                    Toasts.Push(result.ErrorMessage, ToastKind.Warning);
                    return;
                }

                Toasts.Push(GameTexts.Market.AuctionSold(result.Value.Goods.Name, Format.Number(result.Value.EndNowNetCoins)), ToastKind.Coins);
                Refresh();
            });
        }

        // ------------------------------------------------------------------ Expedition intents

        public ExpeditionsView GetExpeditions() => IsRunning ? Game.Expeditions.GetExpeditions() : null;

        public void StartExpedition(string expeditionId)
        {
            Guard(() =>
            {
                var result = Game.Expeditions.Start(expeditionId);
                if (!result.Succeeded)
                {
                    Toasts.Push(result.ErrorMessage, ToastKind.Warning);
                    return;
                }

                Toasts.Push(GameTexts.Expedition.Departed(result.Value.Name), ToastKind.Info);
                AquariumChanged?.Invoke();
            });
        }

        public void AcknowledgeExpedition()
        {
            Guard(() =>
            {
                Game.Expeditions.AcknowledgeResult();
                ExpeditionResult = null;
            });
        }

        // ------------------------------------------------------------------ Map, Shop and rod intents

        public MapsView GetMaps() => IsRunning ? Game.Maps.GetMaps() : null;

        public ShopView GetShop() => IsRunning ? Game.Shop.GetShop() : null;

        public void TravelTo(string mapId)
        {
            Guard(() =>
            {
                var result = Game.Maps.TravelTo(mapId);
                if (!result.Succeeded)
                {
                    Toasts.Push(result.ErrorMessage, ToastKind.Warning);
                    return;
                }

                Toasts.Push(GameTexts.Map.Departing(result.Value.ToName), ToastKind.Info);
                Refresh();
                Travel = result.Value;
            });
        }

        public void BuyRod(string rodId)
        {
            Guard(() =>
            {
                var result = Game.Shop.BuyRod(rodId);
                if (!result.Succeeded)
                {
                    Toasts.Push(result.ErrorMessage, ToastKind.Warning);
                    return;
                }

                Toasts.Push(GameTexts.Shop.Bought(result.Value.Name), ToastKind.Important);
                Refresh();
                AquariumChanged?.Invoke();
            });
        }

        public void UpgradeRod(long itemId)
        {
            Guard(() =>
            {
                var result = Game.Profile.UpgradeRod(itemId);
                if (!result.Succeeded)
                {
                    Toasts.Push(result.ErrorMessage, ToastKind.Warning);
                    return;
                }

                Toasts.Push(GameTexts.Shop.Upgraded(result.Value.Name, result.Value.Level), ToastKind.LevelUp);
                Refresh();
                AquariumChanged?.Invoke();
            });
        }

        public void SellRod(long itemId)
        {
            Guard(() =>
            {
                var result = Game.Profile.SellRod(itemId);
                if (!result.Succeeded)
                {
                    Toasts.Push(result.ErrorMessage, ToastKind.Warning);
                    return;
                }

                Toasts.Push(GameTexts.Shop.RodSold(Format.Number(result.Value.CoinsGained)), ToastKind.Coins);
                Refresh();
                AquariumChanged?.Invoke();
            });
        }

        public void DestroyRod(long itemId)
        {
            Guard(() =>
            {
                var result = Game.Profile.DestroyRod(itemId);
                if (!result.Succeeded)
                {
                    Toasts.Push(result.ErrorMessage, ToastKind.Warning);
                    return;
                }

                Toasts.Push(GameTexts.Shop.RodDestroyed(result.Value.Name), ToastKind.Info);
                Refresh();
                AquariumChanged?.Invoke();
            });
        }

        // ------------------------------------------------------------------ Dev Panel hooks

        /// <summary>Reloads /config into the running game (Dev Panel "Salvar e aplicar").</summary>
        public IReadOnlyList<string> ReloadConfig()
        {
            var result = GameConfigLoader.LoadFromDirectory(GamePaths.ConfigDirectory);
            if (!result.Succeeded)
            {
                return result.Errors;
            }

            Guard(() =>
            {
                Game.Session.ReplaceConfig(result.Config);
                Toasts.Push(GameTexts.Toasts.ConfigReloaded, ToastKind.Info);
                Refresh();
                BoxChanged?.Invoke();
                AquariumChanged?.Invoke();
            });
            return Array.Empty<string>();
        }

        /// <summary>Wipes the save while playing (Dev Panel). Returns where the old save was kept.</summary>
        public string ResetSave()
        {
            string kept = null;
            Guard(() =>
            {
                kept = Game.Session.ResetSave();
                Toasts.Push(GameTexts.Toasts.SaveReset, ToastKind.Info);
                Refresh();
                Tutorial = Game.Tutorial.Get();
                BoxChanged?.Invoke();
                AquariumChanged?.Invoke();
                MapChanged?.Invoke(Player.MapId);
            });
            return kept;
        }

        // ------------------------------------------------------------------ internals

        private void StartGameService()
        {
            try
            {
                var result = LocalGame.Start(
                    GamePaths.ConfigDirectory,
                    GamePaths.SaveDirectory,
                    new FishingIdle.GameService.Core.SystemClock(),
                    message => Debug.Log("[FishingIdle] " + message));

                if (!result.Succeeded)
                {
                    StartupErrorTitle = GameTexts.Startup.ConfigErrorTitle;
                    StartupErrors = result.Errors;
                    foreach (var error in result.Errors)
                    {
                        Debug.LogError("[FishingIdle] Config: " + error);
                    }

                    return;
                }

                Game = result.Game;
                Debug.Log("[FishingIdle] Game service started. Config " + Game.Session.Config.Version +
                          ", save at " + Game.Session.SaveLocation);
                AnnounceSaveStatus(Game.Session.LoadStatus);
                Refresh();
                Tutorial = Game.Tutorial.Get();
                TakeOfflineReport();
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        private void TakeOfflineReport()
        {
            var report = Game.Fishing.TakeOfflineReport();
            if (report == null)
            {
                return;
            }

            WelcomeBack = report;
            Refresh();
            BoxChanged?.Invoke();
        }

        private void AnnounceSaveStatus(SaveLoadStatus status)
        {
            switch (status)
            {
                case SaveLoadStatus.RecoveredFromBackup:
                    Toasts.Push(GameTexts.Startup.SaveRecovered, ToastKind.Warning);
                    break;
                case SaveLoadStatus.Unrecoverable:
                    Toasts.Push(GameTexts.Startup.SaveUnrecoverable, ToastKind.Warning);
                    break;
                case SaveLoadStatus.TooNew:
                    Toasts.Push(GameTexts.Startup.SaveTooNew, ToastKind.Warning);
                    break;
            }
        }

        private void Publish(FishingUpdate update)
        {
            Refresh();
            CatchesArrived?.Invoke(update);
            BoxChanged?.Invoke();
        }

        private void Refresh()
        {
            if (!IsRunning)
            {
                return;
            }

            Player = Game.Player.GetPlayer();
            Status = Game.Fishing.GetStatus();
            Travel = Game.Maps.GetTravel();
        }

        /// <summary>
        /// Runs a service call; an unexpected exception stops the game with a clear screen instead
        /// of leaving it half-working (and the save untouched after the failure).
        /// </summary>
        private void Guard(Action action)
        {
            if (!IsRunning)
            {
                return;
            }

            try
            {
                action();
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        private void Fail(Exception exception)
        {
            Debug.LogException(exception);
            Game = null;
            StartupErrorTitle = GameTexts.Startup.UnexpectedErrorTitle;
            StartupErrors = new[] { exception.Message };
        }
    }
}
