using FishingIdle.Game.Bootstrap;
using FishingIdle.Game.Scene;
using System;
using FishingIdle.GameService.Fishing;
using FishingIdle.GameService.Tutorial;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The game interface on top of the scene: top bar, retractable player card, fishing controls,
    /// Fishing Box button, notifications, and the Fishing Box window.
    /// </summary>
    /// <remarks>
    /// Layout follows GDD section 8: thin navigation on top, player card on the left, the living
    /// scene in the centre with a minimal fishing overlay, quick Fishing Box access, toasts that
    /// never block. Everything is laid out on a 1080-pixel-tall virtual canvas and scaled.
    /// </remarks>
    public sealed class Hud : MonoBehaviour
    {
        private const float VirtualHeight = 1080f;

        private GameRoot _root;
        private FishingScene _scene;
        private FishingBoxWindow _box;
        private AquariumWindow _aquarium;
        private ProfileWindow _profile;
        private MapWindow _map;
        private ShopWindow _shop;
        private ExpeditionWindow _expedition;
        private ArenaWindow _arena;
        private MarketWindow _market;
        private bool _cardExpanded = true;
        private bool _showNotifications;
        private bool _showSettings;
        private bool _compact;
        private int _restoreWidth;
        private int _restoreHeight;
        private FullScreenMode _restoreMode;
        private bool _windowWasOpen;
        private float _windowOpenedAt;
        private string _lastCatchText;
        private float _lastCatchAt;
        private GUIStyle _outline;
        private float _boxPulseUntil;
        private float _width;
        private float _height;

        private void Start()
        {
            _root = GetComponent<GameRoot>();
            _scene = GetComponent<FishingScene>();
            _box = new FishingBoxWindow(_root);
            _aquarium = new AquariumWindow(_root);
            _profile = new ProfileWindow(_root);
            _map = new MapWindow(_root);
            _shop = new ShopWindow(_root);
            _expedition = new ExpeditionWindow(_root);
            _arena = new ArenaWindow(_root);
            _market = new MarketWindow(_root);
            _root.CatchesArrived += OnCatchesArrived;
        }

        private void OnDestroy()
        {
            if (_root != null)
            {
                _root.CatchesArrived -= OnCatchesArrived;
            }
        }

        private void OnCatchesArrived(FishingUpdate update)
        {
            foreach (var c in update.NewCatches)
            {
                _lastCatchText = c.SpeciesName + " · " + Format.SizeCm(c.SizeCm);
                _lastCatchAt = Time.unscaledTime;
                var size = Format.SizeCm(c.SizeCm);
                var icon = Art.FishTexture(c.SpeciesId);
                if (c.IsNewSpecies)
                {
                    _root.Toasts.Push(GameTexts.Toasts.NewSpecies(c.SpeciesName), ToastKind.Important, icon, notify: true);
                }
                else if (c.SizeCategoryId == "exceptional")
                {
                    _root.Toasts.Push(GameTexts.Toasts.Exceptional(c.SpeciesName) + " " + size, ToastKind.Important, icon, notify: true);
                }
                else if (c.IsPersonalRecord)
                {
                    _root.Toasts.Push(GameTexts.Toasts.PersonalRecord(c.SpeciesName, size), ToastKind.Important, icon, notify: true);
                }
                else
                {
                    _root.Toasts.Push(GameTexts.Toasts.Catch(c.SpeciesName, size, c.SizeCategoryName), ToastKind.Catch, icon);
                }
            }

            foreach (var level in update.LevelsReached)
            {
                _root.Toasts.Push(GameTexts.Toasts.LevelUp(level), ToastKind.LevelUp, notify: true);
            }

            if (update.ShellsGained > 0)
            {
                _root.Toasts.Push(GameTexts.Toasts.Shells(Format.Number(update.ShellsGained)), ToastKind.Info);
            }

            _boxPulseUntil = Time.unscaledTime + 1.2f;
        }

        private void OnGUI()
        {
            if (_root == null)
            {
                return;
            }

            var scale = Screen.height / VirtualHeight;
            _width = Screen.width / scale;
            _height = VirtualHeight;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

            var skin = UiSkin.Get();

            if (!_root.IsRunning)
            {
                DrawStartupError(skin);
                return;
            }

            if (_compact)
            {
                DrawCompact(skin);
                return;
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                if (_showNotifications || _showSettings)
                {
                    _showNotifications = false;
                    _showSettings = false;
                    Event.current.Use();
                }
                else if (_arena.IsOpen)
                {
                    _arena.Close();
                    Event.current.Use();
                }
                else if (_market.IsOpen)
                {
                    _market.Close();
                    Event.current.Use();
                }
                else if (_map.IsOpen || _shop.IsOpen || _expedition.IsOpen)
                {
                    _map.Close();
                    _shop.Close();
                    _expedition.Close();
                    Event.current.Use();
                }
                else if (_profile.IsOpen)
                {
                    _profile.Close();
                    Event.current.Use();
                }
                else if (_aquarium.IsOpen)
                {
                    _aquarium.Close();
                    Event.current.Use();
                }
                else if (_box.IsOpen)
                {
                    _box.Close();
                    Event.current.Use();
                }
            }

            _root.Toasts.Prune();

            // End of a trip: the scene fades to dark and back while the new map is built.
            var fade = _scene != null ? _scene.TravelFade : 0f;
            if (fade > 0.01f)
            {
                GUI.DrawTexture(new Rect(0, 0, _width, _height), skin.White, ScaleMode.StretchToFill, true, 0, new Color(0.02f, 0.04f, 0.06f, fade * 0.9f), 0, 0);
            }

            // While a window is open, it owns the input; the HUD underneath is shown but inert.
            var welcomeDialog = _root.Tutorial != null && _root.Tutorial.Active && _root.Tutorial.Step == TutorialSteps.Welcome;
            var windowOpen = _box.IsOpen || _aquarium.IsOpen || _profile.IsOpen || _map.IsOpen || _shop.IsOpen || _expedition.IsOpen || _arena.IsOpen || _market.IsOpen || _root.WelcomeBack != null || _root.ExpeditionResult != null || welcomeDialog;
            GUI.enabled = !windowOpen;
            DrawTopBar(skin);
            DrawPlayerCard(skin);
            DrawFishingControls(skin);
            DrawBoxButton(skin);
            GUI.enabled = true;

            // Menu transition: a window fades in when the first one opens.
            if (windowOpen && !_windowWasOpen)
            {
                _windowOpenedAt = Time.unscaledTime;
            }

            _windowWasOpen = windowOpen;
            var previousColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01((Time.unscaledTime - _windowOpenedAt) / 0.18f));
            _box.Draw(skin, _width, _height);
            _aquarium.Draw(skin, _width, _height);
            _profile.Draw(skin, _width, _height);
            _map.Draw(skin, _width, _height);
            _shop.Draw(skin, _width, _height);
            _expedition.Draw(skin, _width, _height);
            _arena.Draw(skin, _width, _height);
            _market.Draw(skin, _width, _height);
            GUI.color = previousColor;
            GUI.enabled = true;
            if (windowOpen)
            {
                // The navigation stays usable above the windows (not above the welcome-back summary).
                GUI.enabled = _root.WelcomeBack == null && _root.ExpeditionResult == null && !welcomeDialog;
                DrawNavigation(skin);
                GUI.enabled = true;
            }

            DrawToasts(skin);
            DrawTutorial(skin, windowOpen);
            DrawPanels(skin);

            ExpeditionWindow.DrawResult(skin, _root, _width, _height);
            if (WelcomeBackDialog.Draw(skin, _root, _width, _height))
            {
                CloseAllWindows();
                _box.Open();
            }
        }

        // ------------------------------------------------------------------ pieces

        private void DrawTopBar(UiSkin skin)
        {
            var bar = new Rect(0, 0, _width, 64);
            GUI.DrawTexture(bar, skin.White, ScaleMode.StretchToFill, true, 0, new Color(0.04f, 0.07f, 0.11f, 0.78f), 0, 0);

            var player = _root.Player;
            GUI.Label(new Rect(24, 16, 240, 32), GameTexts.GameTitle, skin.Title);
            if (player != null)
            {
                GUI.Label(new Rect(200, 22, 300, 24), "·  " + player.MapName, skin.Body);
            }

            // Primary navigation (GDD section 7). Only menus that exist are shown; the others
            // appear as their milestones arrive, never as dead buttons.
            DrawNavigation(skin);

            if (player == null)
            {
                return;
            }

            // Secondary menu (GDD section 7): notifications bell and settings.
            var x = _width - 24;
            const float settingsWidth = 96f;
            x -= settingsWidth;
            if (GUI.Button(new Rect(x, 12, settingsWidth, 40), GameTexts.Hud.SettingsShort, _showSettings ? skin.ChipActive : skin.Chip))
            {
                _showSettings = !_showSettings;
                _showNotifications = false;
            }

            var bellLabel = _root.Toasts.Unread > 0 ? GameTexts.Hud.Bell + " · " + _root.Toasts.Unread : GameTexts.Hud.Bell;
            var bellWidth = skin.Chip.CalcSize(new GUIContent(bellLabel)).x + 16;
            x -= bellWidth + 8;
            if (GUI.Button(new Rect(x, 12, bellWidth, 40), bellLabel, _showNotifications ? skin.ChipActive : _root.Toasts.Unread > 0 ? skin.ButtonPrimary : skin.Chip))
            {
                _showNotifications = !_showNotifications;
                _showSettings = false;
                _root.Toasts.MarkAllRead();
            }

            x -= 18;

            var coins = Format.Number(player.Coins);
            var coinsWidth = skin.Number.CalcSize(new GUIContent(coins)).x;
            x -= coinsWidth;
            GUI.Label(new Rect(x, 18, coinsWidth, 30), coins, skin.Number);
            x -= 34;
            skin.CoinIcon(new Rect(x, 20, 26, 26));
        }

        /// <summary>Main menus (GDD section 7). Pesca closes any window; the others open theirs.</summary>
        private void DrawNavigation(UiSkin skin)
        {
            const float navWidth = 112f;
            var labels = new[] { GameTexts.Navigation.Fishing, GameTexts.Navigation.Map, AquariumLabel(), GameTexts.Navigation.Arena, GameTexts.Navigation.Expedition, GameTexts.Navigation.Market, GameTexts.Navigation.Shop, GameTexts.Navigation.Profile };
            var active = _map.IsOpen ? 1 : _aquarium.IsOpen ? 2 : _arena.IsOpen ? 3 : _expedition.IsOpen ? 4 : _market.IsOpen ? 5 : _shop.IsOpen ? 6 : _profile.IsOpen ? 7 : 0;
            var x = _width / 2f - (labels.Length * (navWidth + 10f) - 10f) / 2f;

            for (var i = 0; i < labels.Length; i++)
            {
                if (GUI.Button(new Rect(x + i * (navWidth + 10f), 12, navWidth, 40), labels[i], i == active ? skin.ChipActive : skin.Chip) && i != active)
                {
                    CloseAllWindows();
                    if (i == 1) _map.Open();
                    if (i == 2) _aquarium.Open();
                    if (i == 3) _arena.Open();
                    if (i == 4) _expedition.Open();
                    if (i == 5) _market.Open();
                    if (i == 6) _shop.Open();
                    if (i == 7) _profile.Open();
                }
            }
        }

        private string AquariumLabel()
        {
            var player = _root.Player;
            return player == null
                ? GameTexts.Navigation.Aquarium
                : GameTexts.Navigation.Aquarium + "  " + player.AquariumCount + "/" + player.AquariumCapacity;
        }

        private void CloseAllWindows()
        {
            while (_aquarium.IsOpen) _aquarium.Close();
            while (_box.IsOpen) _box.Close();
            while (_profile.IsOpen) _profile.Close();
            _map.Close();
            _shop.Close();
            _expedition.Close();
            while (_arena.IsOpen) _arena.Close();
            while (_market.IsOpen) _market.Close();

            // A panel left open would sit on top of the window and catch its clicks.
            _showNotifications = false;
            _showSettings = false;
        }

        private void DrawPlayerCard(UiSkin skin)
        {
            var player = _root.Player;
            if (player == null)
            {
                return;
            }

            if (!_cardExpanded)
            {
                if (GUI.Button(new Rect(20, 84, 170, 44), GameTexts.Player.ExpandCard + "  »", skin.Button))
                {
                    _cardExpanded = true;
                }

                return;
            }

            var card = new Rect(20, 84, 330, player.Shells > 0 ? 308 : 276);
            GUI.Box(card, GUIContent.none, skin.Panel);

            GUI.Label(new Rect(card.x + 18, card.y + 14, 200, 28), player.PlayerName, skin.Heading);
            if (GUI.Button(new Rect(card.xMax - 104, card.y + 12, 90, 30), "« " + GameTexts.Player.CollapseCard, skin.Chip))
            {
                _cardExpanded = false;
            }

            var levelText = GameTexts.Player.Level + " " + player.FisherLevel;
            GUI.Label(new Rect(card.x + 18, card.y + 50, 200, 24), levelText, skin.BodyBold);

            var xpRect = new Rect(card.x + 18, card.y + 80, card.width - 36, 12);
            if (player.FisherXpToNext > 0)
            {
                skin.Bar(xpRect, player.FisherXp / (float)player.FisherXpToNext);
                GUI.Label(new Rect(card.x + 18, card.y + 96, card.width - 36, 20),
                    GameTexts.Player.Xp(Format.Number(player.FisherXp), Format.Number(player.FisherXpToNext)), skin.SmallMuted);
            }
            else
            {
                skin.Bar(xpRect, 1f, true);
                GUI.Label(new Rect(card.x + 18, card.y + 96, card.width - 36, 20), GameTexts.Player.MaxLevel, skin.SmallMuted);
            }

            var rod = player.RodName == null ? GameTexts.Player.NoRod
                : player.RodHasLevels ? player.RodName + " (" + GameTexts.Player.LevelShort + " " + player.RodLevel + ")" : player.RodName;
            var y = card.y + 130;
            Row(skin, card, ref y, GameTexts.Player.Rod, rod);
            Row(skin, card, ref y, GameTexts.Player.Map, player.MapName);
            Row(skin, card, ref y, GameTexts.Player.TotalCatches, Format.Number(player.TotalCatches));
            Row(skin, card, ref y, GameTexts.Player.SpeciesDiscovered, player.SpeciesDiscovered.ToString());
            if (player.Shells > 0)
            {
                Row(skin, card, ref y, GameTexts.Player.Shells, Format.Number(player.Shells));
            }
        }

        private static void Row(UiSkin skin, Rect card, ref float y, string label, string value)
        {
            GUI.Label(new Rect(card.x + 18, y, 170, 22), label, skin.SmallMuted);
            GUI.Label(new Rect(card.x + 150, y, card.width - 168, 22), value, skin.SmallRight);
            y += 32;
        }

        private void DrawFishingControls(UiSkin skin)
        {
            var status = _root.Status;
            if (status == null)
            {
                return;
            }

            var panel = new Rect(_width / 2f - 290, _height - 168, 580, 144);
            GUI.Box(panel, GUIContent.none, skin.Panel);

            var travel = _root.Travel;
            if (travel != null && travel.Active)
            {
                // Travelling: fishing is paused; the panel shows the trip instead (GDD section 18).
                GUI.Label(new Rect(panel.x + 22, panel.y + 16, panel.width - 44, 26), GameTexts.Map.Traveling(travel.ToName), skin.BodyBold);
                skin.Bar(new Rect(panel.x + 22, panel.y + 52, panel.width - 44, 14), (float)travel.Progress);
                GUI.Label(new Rect(panel.x + 22, panel.y + 72, panel.width - 44, 22), GameTexts.Map.ArrivesIn(Format.Countdown(travel.SecondsLeft)), skin.SmallMuted);
                return;
            }

            var phase = _scene != null && _scene.Fisherman != null ? _scene.Fisherman.PhaseText : string.Empty;
            GUI.Label(new Rect(panel.x + 22, panel.y + 16, panel.width - 44, 26), status.IsFishing ? phase : GameTexts.Fishing.Idle, skin.BodyBold);

            if (status.IsFishing)
            {
                var remaining = (status.NextCatchAtMs - status.NowMs) / 1000.0;
                skin.Bar(new Rect(panel.x + 22, panel.y + 52, panel.width - 44, 14), (float)status.CycleProgress);
                GUI.Label(new Rect(panel.x + 22, panel.y + 72, 300, 22), GameTexts.Fishing.NextCatchIn(Format.Countdown(remaining)), skin.SmallMuted);
            }

            var cycleInfo = GameTexts.Fishing.CycleInfo(Format.Duration(status.CycleSeconds)) + " · " + status.MapName;
            GUI.Label(new Rect(panel.x + 22, panel.y + 72, panel.width - 44, 22), cycleInfo, skin.SmallMutedRight);

            var button = new Rect(panel.x + panel.width / 2f - 120, panel.y + 98, 240, 38);
            if (status.IsFishing)
            {
                if (GUI.Button(button, GameTexts.Fishing.Stop, skin.Button))
                {
                    _root.StopFishing();
                }
            }
            else if (GUI.Button(button, GameTexts.Fishing.Start, skin.ButtonPrimary))
            {
                _root.StartFishing();
            }
        }

        private void DrawBoxButton(UiSkin skin)
        {
            var player = _root.Player;
            if (player == null)
            {
                return;
            }

            var pulse = Time.unscaledTime < _boxPulseUntil ? 1f + Mathf.Sin((_boxPulseUntil - Time.unscaledTime) * 12f) * 0.04f : 1f;
            var w = 250f * pulse;
            var h = 96f * pulse;
            var rect = new Rect(_width - 24 - w, _height - 24 - h, w, h);

            if (GUI.Button(rect, GUIContent.none, skin.Button))
            {
                _box.Open();
            }

            GUI.DrawTexture(new Rect(rect.x + 12, rect.y + 18, 96, 56), Art.FishTexture("tilapia"), ScaleMode.ScaleToFit, true);
            GUI.Label(new Rect(rect.x + 114, rect.y + 20, rect.width - 120, 26), GameTexts.Box.Open, skin.BodyBold);
            GUI.Label(new Rect(rect.x + 114, rect.y + 50, rect.width - 120, 24), GameTexts.Box.Count(player.FishingBoxCount), skin.SmallMuted);
        }

        private void DrawToasts(UiSkin skin)
        {
            var items = _root.Toasts.Items;
            var y = 80f;
            for (var i = items.Count - 1; i >= 0; i--)
            {
                var toast = items[i];
                var age = Time.unscaledTime - toast.CreatedAt;
                var fadeIn = Mathf.Clamp01(age / 0.25f);
                var fadeOut = Mathf.Clamp01((toast.Duration - age) / 0.6f);
                var alpha = Mathf.Min(fadeIn, fadeOut);

                var rect = new Rect(_width - 460 + (1f - fadeIn) * 40f, y, 436, 58);
                var previous = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.Box(rect, GUIContent.none, toast.Kind == ToastKind.Important || toast.Kind == ToastKind.LevelUp ? skin.CardImportant : skin.Panel);

                var textX = rect.x + 16;
                if (toast.Icon != null)
                {
                    GUI.DrawTexture(new Rect(rect.x + 10, rect.y + 8, 78, 42), toast.Icon, ScaleMode.ScaleToFit, true);
                    textX = rect.x + 96;
                }

                var style = toast.Kind == ToastKind.Warning ? skin.SmallGold : skin.Small;
                GUI.Label(new Rect(textX, rect.y + 10, rect.xMax - textX - 12, 40), toast.Text, style);
                GUI.color = previous;
                y += 66;
            }
        }

        // ------------------------------------------------------------------ tutorial (GDD section 40)

        private void DrawTutorial(UiSkin skin, bool windowOpen)
        {
            var tutorial = _root.Tutorial;
            if (tutorial == null || !tutorial.Active || _root.WelcomeBack != null || _root.ExpeditionResult != null)
            {
                return;
            }

            // Steps that end by looking: the Fishing Box and the Expedition menu count as seen when opened.
            if (tutorial.Step == TutorialSteps.OpenBox && _box.IsOpen)
            {
                _root.AcknowledgeTutorial(TutorialSteps.OpenBox);
                return;
            }

            if (tutorial.Step == TutorialSteps.Expedition && _expedition.IsOpen)
            {
                _root.AcknowledgeTutorial(TutorialSteps.Expedition);
                return;
            }

            if (tutorial.Step == TutorialSteps.Welcome)
            {
                var dialog = WindowFrame.Dialog(skin, _width, _height, 300f, 680f);
                GUI.Label(new Rect(dialog.x + 28, dialog.y + 24, dialog.width - 56, 32), GameTexts.Tutorial.Title(tutorial.Step), skin.Title);
                GUI.Label(new Rect(dialog.x + 28, dialog.y + 70, dialog.width - 56, 120), GameTexts.Tutorial.Body(tutorial.Step), skin.Body);
                if (GUI.Button(new Rect(dialog.xMax - 228, dialog.yMax - 66, 200, 44), GameTexts.Tutorial.Begin, skin.ButtonPrimary))
                {
                    _root.AcknowledgeTutorial(TutorialSteps.Welcome);
                }

                if (GUI.Button(new Rect(dialog.x + 28, dialog.yMax - 66, 200, 44), GameTexts.Tutorial.Skip, skin.Button))
                {
                    _root.SkipTutorial();
                }

                return;
            }

            // With a window open the hint moves to a strip under it; otherwise it sits under the player card.
            var rect = windowOpen
                ? new Rect(_width / 2f - 440, _height - 88, 880, 76)
                : new Rect(20, _cardExpanded ? 412 : 144, 330, 200);
            GUI.Box(rect, GUIContent.none, skin.CardImportant);
            if (windowOpen)
            {
                GUI.Label(new Rect(rect.x + 18, rect.y + 10, rect.width - 220, 22), GameTexts.Tutorial.Title(tutorial.Step), skin.BodyBold);
                GUI.Label(new Rect(rect.x + 18, rect.y + 34, rect.width - 220, 40), GameTexts.Tutorial.Body(tutorial.Step), skin.Small);
            }
            else
            {
                GUI.Label(new Rect(rect.x + 16, rect.y + 12, rect.width - 32, 18), GameTexts.Tutorial.StepOf(tutorial.StepNumber, tutorial.StepCount), skin.SmallGold);
                GUI.Label(new Rect(rect.x + 16, rect.y + 32, rect.width - 32, 26), GameTexts.Tutorial.Title(tutorial.Step), skin.BodyBold);
                GUI.Label(new Rect(rect.x + 16, rect.y + 60, rect.width - 32, 90), GameTexts.Tutorial.Body(tutorial.Step), skin.Small);
            }

            var buttons = windowOpen ? new Rect(rect.xMax - 190, rect.y + 20, 172, 36) : new Rect(rect.x + 16, rect.yMax - 46, 140, 32);
            if (tutorial.NeedsAcknowledge && GUI.Button(new Rect(buttons.x, buttons.y, 84, buttons.height), GameTexts.Tutorial.GotIt, skin.ButtonPrimary))
            {
                _root.AcknowledgeTutorial(tutorial.Step);
            }

            var skipRect = windowOpen ? new Rect(rect.xMax - 190 + (tutorial.NeedsAcknowledge ? 92 : 0), rect.y + 20, tutorial.NeedsAcknowledge ? 80 : 172, 36)
                                      : new Rect(rect.xMax - 16 - 150, rect.yMax - 46, 150, 32);
            if (GUI.Button(skipRect, GameTexts.Tutorial.Skip, skin.Chip))
            {
                _root.SkipTutorial();
            }

            if (!windowOpen)
            {
                DrawTutorialHighlight(tutorial.Step);
            }
        }

        /// <summary>A pulsing gold outline around what the current step asks the player to use.</summary>
        private void DrawTutorialHighlight(string step)
        {
            Rect target;
            switch (step)
            {
                case TutorialSteps.ClaimRod: target = NavRect(6); break;
                case TutorialSteps.Cardume: target = NavRect(7); break;
                case TutorialSteps.Expedition: target = NavRect(4); break;
                case TutorialSteps.StartFishing: target = new Rect(_width / 2f - 120, _height - 70, 240, 38); break;
                case TutorialSteps.OpenBox:
                case TutorialSteps.SellFish:
                case TutorialSteps.KeepFish: target = new Rect(_width - 274, _height - 120, 250, 96); break;
                default: return;
            }

            if (_outline == null)
            {
                _outline = new GUIStyle { border = new RectOffset(12, 12, 12, 12) };
                _outline.normal.background = Art.RoundedRectTexture(new Color(0f, 0f, 0f, 0f), UiSkin.Gold, 12, 3f);
            }

            var pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 5f);
            var previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, pulse);
            var grow = 6f + 3f * pulse;
            GUI.Box(new Rect(target.x - grow, target.y - grow, target.width + grow * 2, target.height + grow * 2), GUIContent.none, _outline);
            GUI.color = previous;
        }

        private Rect NavRect(int index)
        {
            const float navWidth = 112f;
            var x = _width / 2f - (8 * (navWidth + 10f) - 10f) / 2f;
            return new Rect(x + index * (navWidth + 10f), 12, navWidth, 40);
        }

        // ------------------------------------------------------------------ notifications and settings

        private void DrawPanels(UiSkin skin)
        {
            if (_showNotifications)
            {
                var panel = new Rect(_width - 24 - 470, 70, 470, 560);
                GUI.Box(panel, GUIContent.none, skin.Panel);
                GUI.Label(new Rect(panel.x + 20, panel.y + 16, 300, 26), GameTexts.Hud.Notifications, skin.Heading);
                if (_root.Toasts.History.Count > 0 && GUI.Button(new Rect(panel.xMax - 110, panel.y + 14, 90, 30), GameTexts.Hud.ClearNotifications, skin.Chip))
                {
                    _root.Toasts.ClearHistory();
                }

                var y = panel.y + 58;
                if (_root.Toasts.History.Count == 0)
                {
                    GUI.Label(new Rect(panel.x + 20, y, panel.width - 40, 24), GameTexts.Hud.NoNotifications, skin.SmallMuted);
                }

                foreach (var entry in _root.Toasts.History)
                {
                    if (y + 56 > panel.yMax - 10)
                    {
                        break;
                    }

                    var textX = panel.x + 20;
                    if (entry.Icon != null)
                    {
                        GUI.DrawTexture(new Rect(panel.x + 16, y + 6, 60, 34), entry.Icon, ScaleMode.ScaleToFit, true);
                        textX = panel.x + 86;
                    }

                    GUI.Label(new Rect(textX, y, 60, 18), Format.Time(entry.At), skin.SmallMuted);
                    GUI.Label(new Rect(textX, y + 18, panel.xMax - textX - 16, 36), entry.Text, entry.Kind == ToastKind.Warning ? skin.SmallGold : skin.Small);
                    y += 58;
                }
            }

            if (_showSettings)
            {
                var panel = new Rect(_width - 24 - 380, 70, 380, 340);
                GUI.Box(panel, GUIContent.none, skin.Panel);
                var x = panel.x + 20;
                var w = panel.width - 40;
                var y = panel.y + 16;
                GUI.Label(new Rect(x, y, w, 26), GameTexts.Hud.Settings, skin.Heading);
                y += 42;

                GameSettingsToggle(skin, x, ref y, w, GameTexts.Hud.Sound, GameSettings.SoundOn, v => GameSettings.SoundOn = v);
                GameSettingsToggle(skin, x, ref y, w, GameTexts.Hud.Ambient, GameSettings.AmbientOn, v => GameSettings.AmbientOn = v);

                GUI.Label(new Rect(x, y + 4, 120, 22), GameTexts.Hud.Volume, skin.Body);
                GameSettings.Volume = GUI.HorizontalSlider(new Rect(x + 130, y + 10, w - 130, 20), GameSettings.Volume, 0f, 1f);
                y += 44;

                if (GUI.Button(new Rect(x, y, w, 40), GameTexts.Hud.CompactMode, skin.ButtonPrimary))
                {
                    EnterCompact();
                }

                GUI.Label(new Rect(x, y + 46, w, 60), GameTexts.Hud.CompactNote, skin.SmallMuted);
            }
        }

        private static void GameSettingsToggle(UiSkin skin, float x, ref float y, float w, string label, bool value, Action<bool> set)
        {
            GUI.Label(new Rect(x, y + 6, w - 150, 22), label, skin.Body);
            if (GUI.Button(new Rect(x + w - 140, y, 140, 34), value ? GameTexts.Hud.On : GameTexts.Hud.Off, value ? skin.ChipActive : skin.Chip))
            {
                set(!value);
            }

            y += 44;
        }

        // ------------------------------------------------------------------ compact / taskbar mode (GDD section 9)

        private void EnterCompact()
        {
            CloseAllWindows();
            _showSettings = false;
            _showNotifications = false;
            _restoreWidth = Screen.width;
            _restoreHeight = Screen.height;
            _restoreMode = Screen.fullScreenMode;
            _compact = true;

            // Pure presentation: the game service keeps fishing exactly the same.
            Screen.SetResolution(480, 270, FullScreenMode.Windowed);
        }

        private void ExitCompact()
        {
            _compact = false;
            if (_restoreWidth > 0 && _restoreHeight > 0)
            {
                Screen.SetResolution(_restoreWidth, _restoreHeight, _restoreMode);
            }
        }

        /// <summary>Only the scene, a status line and the last catch; everything else hidden.</summary>
        private void DrawCompact(UiSkin skin)
        {
            const float virtualHeight = 300f;
            var scale = Screen.height / virtualHeight;
            var width = Screen.width / scale;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

            if (GUI.Button(new Rect(width - 108, 8, 100, 30), GameTexts.Hud.Expand, skin.Chip))
            {
                ExitCompact();
                return;
            }

            var status = _root.Status;
            var player = _root.Player;
            var line = status != null && status.IsFishing
                ? GameTexts.Hud.Fishing + " · " + Format.Countdown((status.NextCatchAtMs - status.NowMs) / 1000.0)
                : GameTexts.Hud.Stopped;
            if (player != null)
            {
                line += "   " + GameTexts.Hud.BoxCount(player.FishingBoxCount);
            }

            var bar = new Rect(8, virtualHeight - 38, width - 16, 30);
            GUI.DrawTexture(bar, skin.White, ScaleMode.StretchToFill, true, 0, new Color(0.04f, 0.07f, 0.11f, 0.6f), 0, 8);
            GUI.Label(new Rect(bar.x + 10, bar.y + 7, bar.width - 20, 20), line, skin.Small);

            if (_lastCatchText != null && Time.unscaledTime - _lastCatchAt < 5f)
            {
                var alpha = Mathf.Clamp01(5f - (Time.unscaledTime - _lastCatchAt));
                var previous = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.Label(new Rect(12, 12, width - 130, 22), _lastCatchText, skin.SmallGold);
                GUI.color = previous;
            }
        }

        private void DrawStartupError(UiSkin skin)
        {
            GUI.DrawTexture(new Rect(0, 0, _width, _height), skin.White, ScaleMode.StretchToFill, true, 0, new Color(0.05f, 0.08f, 0.12f, 0.96f), 0, 0);
            var panel = new Rect(_width / 2f - 480, 120, 960, _height - 240);
            GUI.Box(panel, GUIContent.none, skin.Panel);
            GUILayout.BeginArea(new Rect(panel.x + 32, panel.y + 28, panel.width - 64, panel.height - 56));
            GUILayout.Label(_root.StartupErrorTitle ?? GameTexts.Startup.UnexpectedErrorTitle, skin.Title);
            GUILayout.Space(10);
            var hint = _root.StartupErrorTitle == GameTexts.Startup.ConfigErrorTitle ? GameTexts.Startup.ConfigErrorHint : GameTexts.Startup.UnexpectedErrorHint;
            GUILayout.Label(hint, skin.SmallMuted);
            GUILayout.Space(16);
            foreach (var error in _root.StartupErrors)
            {
                GUILayout.Label("•  " + error, skin.Body);
                GUILayout.Space(6);
            }

            GUILayout.EndArea();
        }
    }
}
