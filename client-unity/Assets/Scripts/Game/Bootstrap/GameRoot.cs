using System;
using System.Collections.Generic;
using FishingIdle.Game.Scene;
using FishingIdle.Game.UI;
using FishingIdle.GameService;
using FishingIdle.GameService.Aquarium;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;
using FishingIdle.GameService.Persistence;
using FishingIdle.GameService.Profile;
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
                    var update = Game.Fishing.Sync();
                    if (update.HasChanges)
                    {
                        Publish(update);
                    }
                });
            }

            Status = Game.Fishing.GetStatus();
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
                BoxChanged?.Invoke();
                AquariumChanged?.Invoke();
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
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
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
