using FishingIdle.Game.Audio;
using FishingIdle.Game.Bootstrap;
using FishingIdle.Game.Scene;
using FishingIdle.Game.Visual;
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
        private readonly ArrivalTitle _arrival = new ArrivalTitle();
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
        private long _coinsTarget = -1;
        private long _coinsFrom;
        private long _coinsShown;
        private float _coinsChangedAt = -10f;
        private long _coinsGain;
        private float _coinsGainAt = -10f;
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
            _root.MapArrived += OnMapArrived;
        }

        private void OnDestroy()
        {
            if (_root != null)
            {
                _root.CatchesArrived -= OnCatchesArrived;
                _root.MapArrived -= OnMapArrived;
            }
        }

        /// <summary>A new map is a new chapter of the journey: its title card over the scene.</summary>
        private void OnMapArrived(string mapId)
        {
            var chapter = ArrivalTitle.ChapterOf(_root, mapId);
            _arrival.Show(chapter > 0 ? GameTexts.Map.Chapter(chapter) : null, _root.Player?.MapName, GameTexts.Map.Feeling(mapId));
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
                    _root.Toasts.Push(GameTexts.Toasts.NewSpecies(c.SpeciesName), ToastKind.Important, icon, notify: true, sound: SoundCue.RareCatch);
                }
                else if (c.SizeCategoryId == "perfect")
                {
                    _root.Toasts.Push(GameTexts.Toasts.Perfect(c.SpeciesName) + " " + size, ToastKind.Important, icon, notify: true, sound: SoundCue.RareCatch);
                }
                else if (c.SizeCategoryId == "exceptional")
                {
                    _root.Toasts.Push(GameTexts.Toasts.Exceptional(c.SpeciesName) + " " + size, ToastKind.Important, icon, notify: true, sound: SoundCue.RareCatch);
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

            Celebrate(update);

            foreach (var level in update.LevelsReached)
            {
                _root.Toasts.Push(GameTexts.Toasts.LevelUp(level), ToastKind.LevelUp, notify: true);
            }

            if (update.ShellsGained > 0)
            {
                _root.Toasts.Push(GameTexts.Toasts.Shells(Format.Number(update.ShellsGained)), ToastKind.Info);
            }

            // Escapes (docs/SISTEMA_SUCESSO_PESCA.md): a common one is told only near the bobber; a
            // rarer one also leaves a toast, so the player notices what was lost. No sound (addendum A-080).
            foreach (var escape in update.Escapes)
            {
                if (escape.RarityId != "common")
                {
                    _root.Toasts.Push(GameTexts.Fishing.RareEscaped(escape.RarityName, Format.Percent(escape.Chance, 0)), ToastKind.Warning);
                }
            }

            if (update.BaitRanOut != null)
            {
                _root.Toasts.Push(GameTexts.Fishing.BaitRanOut(update.BaitRanOut), ToastKind.Warning, notify: true);
            }

            if (update.NewCatches.Count > 0)
            {
                _boxPulseUntil = Time.unscaledTime + 1.2f;
            }
        }

        /// <summary>
        /// Level 3 moments (Art Bible, section 16): only the most remarkable catch of a batch gets a
        /// banner, then any level reached. The colour tells what happened before any text is read.
        /// </summary>
        private void Celebrate(FishingUpdate update)
        {
            var theme = VisualTheme.Current;
            CatchView best = null;
            foreach (var c in update.NewCatches)
            {
                if ((c.IsNewSpecies || c.IsPersonalRecord || VisualTheme.IsSpecialSize(c.SizeCategoryId) || (c.RarityId != null && c.RarityId != "common"))
                    && (best == null || c.SalePriceCoins > best.SalePriceCoins))
                {
                    best = c;
                }
            }

            if (best != null)
            {
                var art = Art.FishTexture(best.SpeciesId);
                var size = Format.SizeCm(best.SizeCm);
                if (best.IsNewSpecies)
                {
                    _root.Celebrations.Show(GameTexts.Celebration.NewSpecies, GameTexts.Celebration.NewSpeciesLine(best.SpeciesName), theme.Action, art, reveal: true);
                }
                else if (best.SizeCategoryId == "perfect")
                {
                    _root.Celebrations.Show(GameTexts.Celebration.Perfect, GameTexts.Celebration.CatchLine(best.SpeciesName, size), theme.Size("perfect"), art);
                }
                else if (best.SizeCategoryId == "exceptional")
                {
                    _root.Celebrations.Show(GameTexts.Celebration.Exceptional, GameTexts.Celebration.CatchLine(best.SpeciesName, size), theme.Exceptional, art);
                }
                else if (best.IsPersonalRecord && best.PreviousRecordCm > 0)
                {
                    _root.Celebrations.Show(GameTexts.Celebration.Record, GameTexts.Celebration.RecordLine(best.SpeciesName, Format.SizeCm(best.PreviousRecordCm), size), theme.Reward, art);
                }
                else if (best.RarityId != null && best.RarityId != "common")
                {
                    _root.Celebrations.Show(GameTexts.Celebration.RareCatch, GameTexts.Celebration.RareLine(best.SpeciesName, best.RarityName, size), theme.Rarity(best.RarityId), art);
                }
            }

            foreach (var level in update.LevelsReached)
            {
                _root.Celebrations.Show(GameTexts.Celebration.LevelUp(level), GameTexts.Celebration.LevelUpLine, theme.Reward);
            }
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
            var windowOpen = _box.IsOpen || _aquarium.IsOpen || _profile.IsOpen || _map.IsOpen || _shop.IsOpen || _expedition.IsOpen || _arena.IsOpen || _market.IsOpen || _root.WelcomeBack != null || welcomeDialog;
            _root.WindowOpen = windowOpen;
            GUI.enabled = !windowOpen;
            DrawTopBar(skin);
            DrawPlayerCard(skin);
            DrawFishingControls(skin);
            DrawBoxButton(skin);
            if (!windowOpen)
            {
                DrawEscapeMessage(skin);
            }

            GUI.enabled = true;

            // Menu transition: a window fades in when the first one opens.
            if (windowOpen && !_windowWasOpen)
            {
                _windowOpenedAt = Time.unscaledTime;
            }

            _windowWasOpen = windowOpen;
            var previousColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01((Time.unscaledTime - _windowOpenedAt) / VisualTheme.Current.WindowFadeSeconds));
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
                GUI.enabled = _root.WelcomeBack == null && !welcomeDialog;
                DrawNavigation(skin);
                GUI.enabled = true;
            }

            _arrival.Draw(skin, _width, _height);
            DrawToasts(skin);
            _root.Celebrations.Draw(skin, _width);
            DrawTutorial(skin, windowOpen);
            DrawPanels(skin);

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
            GUI.DrawTexture(bar, skin.White, ScaleMode.StretchToFill, true, 0, new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.82f), 0, 0);
            GUI.DrawTexture(new Rect(0, 64, _width, 1), skin.White, ScaleMode.StretchToFill, true, 0, new Color(UiSkin.Border.r, UiSkin.Border.g, UiSkin.Border.b, 0.8f), 0, 0);

            var player = _root.Player;
            skin.DrawIcon(new Rect(22, 17, 34, 30), Icons.Fish, UiSkin.Accent);
            GUI.Label(new Rect(64, 14, 240, 36), GameTexts.GameTitle, skin.Title);
            if (player != null)
            {
                var titleWidth = skin.Title.CalcSize(new GUIContent(GameTexts.GameTitle)).x;
                GUI.Label(new Rect(64 + titleWidth + 14, 23, 220, 24), "·  " + player.MapName, skin.SmallMuted);
            }

            // Primary navigation (GDD section 7).
            DrawNavigation(skin);

            if (player == null)
            {
                return;
            }

            // Secondary menu (GDD section 7): notifications bell and settings.
            var x = _width - 20;
            var settings = new Rect(x - 116, 12, 116, 40);
            if (skin.IconButton(settings, Icons.Settings, GameTexts.Hud.SettingsShort, _showSettings ? skin.NavActive : skin.Nav))
            {
                _showSettings = !_showSettings;
                _showNotifications = false;
            }

            x = settings.x - 8;
            var bell = new Rect(x - 112, 12, 112, 40);
            if (skin.IconButton(bell, Icons.Bell, GameTexts.Hud.Bell, _showNotifications ? skin.NavActive : skin.Nav))
            {
                _showNotifications = !_showNotifications;
                _showSettings = false;
                _root.Toasts.MarkAllRead();
            }

            if (_root.Toasts.Unread > 0)
            {
                // Red dot with the count, like a phone badge.
                var count = _root.Toasts.Unread > 9 ? "9+" : _root.Toasts.Unread.ToString();
                var dot = new Rect(bell.x + 22, bell.y + 2, 18, 18);
                GUI.DrawTexture(dot, skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.Danger, 0, 9);
                GUI.Label(dot, count, skin.PillText);
            }

            x = bell.x - 18;
            DrawCoins(skin, x, player.Coins);
            DrawWallet(skin, x, player);
        }

        /// <summary>
        /// Conchas and Dólares in a small strip under the coins: Conchas buy and upgrade every item, and
        /// both are traded between players (A-098).
        /// </summary>
        private void DrawWallet(UiSkin skin, float right, PlayerView player)
        {
            var x = right;
            foreach (var (icon, label, value) in new[]
                     {
                         (Icons.Dollar, GameTexts.Player.Dollars, player.Dollars),
                         (Icons.Shell, GameTexts.Player.Shells, player.Shells),
                     })
            {
                var text = Format.Number(value);
                var w = skin.SmallBold.CalcSize(new GUIContent(text)).x + 4f + 40f;
                var chip = new Rect(x - w, 70, w, 30);
                GUI.Box(chip, new GUIContent(string.Empty, label), skin.Chip);
                skin.DrawIcon(new Rect(chip.x + 10, chip.y + 6, 18, 18), icon, Color.white);
                GUI.Label(new Rect(chip.x + 34, chip.y + 6, w - 36, 20), text, skin.SmallBold);
                x = chip.x - 8;
            }
        }

        /// <summary>The coin counter: it counts up to the new total, with a "+N" that floats away.</summary>
        private void DrawCoins(UiSkin skin, float right, long coins)
        {
            if (_coinsTarget != coins)
            {
                if (coins > _coinsTarget && _coinsTarget >= 0)
                {
                    _coinsGain = coins - _coinsTarget;
                    _coinsGainAt = Time.unscaledTime;
                }

                _coinsFrom = _coinsTarget < 0 ? coins : _coinsShown;
                _coinsTarget = coins;
                _coinsChangedAt = Time.unscaledTime;
            }

            var k = Mathf.Clamp01((Time.unscaledTime - _coinsChangedAt) / Visual.VisualTheme.Current.CoinCountSeconds);
            _coinsShown = (long)Mathf.Lerp(_coinsFrom, _coinsTarget, 1f - (1f - k) * (1f - k));
            var text = Format.Number(_coinsShown);
            var textWidth = skin.Number.CalcSize(new GUIContent(text)).x;
            var box = new Rect(right - textWidth - 58, 12, textWidth + 58, 40);
            GUI.Box(box, GUIContent.none, skin.Chip);
            var pulse = k < 1f ? 1f + 0.12f * Mathf.Sin(k * Mathf.PI) : 1f;
            var iconSize = 24f * pulse;
            skin.CoinIcon(new Rect(box.x + 14 + (24f - iconSize) / 2f, box.y + 8 + (24f - iconSize) / 2f, iconSize, iconSize));
            GUI.Label(new Rect(box.x + 46, box.y + 7, textWidth + 4, 30), text, skin.Number);

            var since = Time.unscaledTime - _coinsGainAt;
            if (_coinsGain > 0 && since < 1.4f)
            {
                var previous = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1.4f - since));
                GUI.Label(new Rect(box.x + 46, box.yMax + 2 + since * 14f, 200, 26), GameTexts.Celebration.Coins(Format.Number(_coinsGain)), skin.SmallGold);
                GUI.color = previous;
            }
        }

        /// <summary>Where each main menu button goes: icon + label, centred; icons only when the screen is narrow.</summary>
        private Rect[] NavLayout(UiSkin skin, out bool iconsOnly)
        {
            var labels = NavLabels();
            var widths = new float[labels.Length];
            var total = 0f;
            for (var i = 0; i < labels.Length; i++)
            {
                widths[i] = skin.Nav.CalcSize(new GUIContent(labels[i])).x + 44f;
                total += widths[i] + 8f;
            }

            total -= 8f;
            var available = _width - 2f * 440f;
            iconsOnly = total > available;
            if (iconsOnly)
            {
                for (var i = 0; i < widths.Length; i++)
                {
                    widths[i] = 52f;
                }

                total = labels.Length * 60f - 8f;
            }

            var rects = new Rect[labels.Length];
            var x = _width / 2f - total / 2f;
            for (var i = 0; i < labels.Length; i++)
            {
                rects[i] = new Rect(x, 12, widths[i], 40);
                x += widths[i] + 8f;
            }

            return rects;
        }

        private string[] NavLabels()
        {
            return new[] { GameTexts.Navigation.Fishing, GameTexts.Navigation.Map, AquariumLabel(), GameTexts.Navigation.Arena, GameTexts.Navigation.Expedition, GameTexts.Navigation.Market, GameTexts.Navigation.Shop, GameTexts.Navigation.Profile };
        }

        private static readonly string[] NavIcons = { Icons.Fishing, Icons.Map, Icons.Aquarium, Icons.Arena, Icons.Expedition, Icons.Market, Icons.Shop, Icons.Profile };

        /// <summary>Main menus (GDD section 7). Pesca closes any window; the others open theirs.</summary>
        private void DrawNavigation(UiSkin skin)
        {
            var labels = NavLabels();
            var rects = NavLayout(skin, out var iconsOnly);
            var active = _map.IsOpen ? 1 : _aquarium.IsOpen ? 2 : _arena.IsOpen ? 3 : _expedition.IsOpen ? 4 : _market.IsOpen ? 5 : _shop.IsOpen ? 6 : _profile.IsOpen ? 7 : 0;

            for (var i = 0; i < labels.Length; i++)
            {
                var clicked = skin.IconButton(rects[i], NavIcons[i], iconsOnly ? null : labels[i], i == active ? skin.NavActive : skin.Nav) && i != active;
                if (i == 4 && _root.ExpeditionResult != null && !_expedition.IsOpen)
                {
                    // The Cardume is back: a dot until the player opens the Expedition and reads the report.
                    var dot = new Rect(rects[i].xMax - 14, rects[i].y + 3, 11, 11);
                    GUI.DrawTexture(dot, skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.Danger, 0, 5.5f);
                }

                if (clicked)
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
                if (skin.IconButton(new Rect(20, 84, 170, 44), Icons.Profile, GameTexts.Player.ExpandCard, skin.Button))
                {
                    _cardExpanded = true;
                }

                return;
            }

            var card = new Rect(20, 84, 340, player.Shells > 0 ? 318 : 286);
            skin.FloatingPanel(card);

            // Avatar: the painted fisherman's head in a tile (or the profile icon).
            var avatar = new Rect(card.x + 16, card.y + 16, 64, 64);
            GUI.Box(avatar, GUIContent.none, skin.IconTile);
            var portrait = Visual.ArtAssets.Texture("Cena/retrato");
            if (portrait != null)
            {
                GUI.DrawTexture(new Rect(avatar.x + 3, avatar.y + 3, avatar.width - 6, avatar.height - 6), portrait, ScaleMode.ScaleAndCrop, true, 0, Color.white, 0, 10);
            }
            else
            {
                skin.DrawIcon(new Rect(avatar.x + 14, avatar.y + 14, 36, 36), Icons.Profile, UiSkin.Accent);
            }

            GUI.Label(new Rect(avatar.xMax + 14, card.y + 16, 180, 28), player.PlayerName, skin.Heading);
            if (skin.IconButton(new Rect(card.xMax - 50, card.y + 14, 36, 32), Icons.Chevron, null, skin.Chip))
            {
                _cardExpanded = false;
            }

            GUI.Label(new Rect(avatar.xMax + 14, card.y + 44, 200, 22), GameTexts.Player.Level + " " + player.FisherLevel, skin.SmallMuted);
            var xpRect = new Rect(avatar.xMax + 14, card.y + 66, card.xMax - avatar.xMax - 30, 10);
            if (player.FisherXpToNext > 0)
            {
                skin.Bar(xpRect, player.FisherXp / (float)player.FisherXpToNext);
                GUI.Label(new Rect(xpRect.x, card.y + 80, xpRect.width, 20),
                    GameTexts.Player.Xp(Format.Number(player.FisherXp), Format.Number(player.FisherXpToNext)), skin.SmallMuted);
            }
            else
            {
                skin.Bar(xpRect, 1f, true);
                GUI.Label(new Rect(xpRect.x, card.y + 80, xpRect.width, 20), GameTexts.Player.MaxLevel, skin.SmallMuted);
            }

            skin.Divider(new Rect(card.x + 16, card.y + 112, card.width - 32, 1));
            var rod = player.RodName == null ? GameTexts.Player.NoRod
                : player.RodHasLevels ? player.RodName + " (" + GameTexts.Player.LevelShort + " " + player.RodLevel + ")" : player.RodName;
            var y = card.y + 126;
            Row(skin, card, ref y, Icons.Rod, GameTexts.Player.Rod, rod);
            Row(skin, card, ref y, Icons.Pin, GameTexts.Player.Map, player.MapName);
            Row(skin, card, ref y, Icons.Fish, GameTexts.Player.TotalCatches, Format.Number(player.TotalCatches));
            Row(skin, card, ref y, Icons.Book, GameTexts.Player.SpeciesDiscovered, player.SpeciesDiscovered.ToString());
            if (player.Shells > 0)
            {
                Row(skin, card, ref y, Icons.Shell, GameTexts.Player.Shells, Format.Number(player.Shells));
            }
        }

        private static void Row(UiSkin skin, Rect card, ref float y, string icon, string label, string value)
        {
            skin.DrawIcon(new Rect(card.x + 18, y + 1, 18, 18), icon, icon == Icons.Shell ? Color.white : UiSkin.Muted);
            GUI.Label(new Rect(card.x + 46, y, 160, 22), label, skin.SmallMuted);
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

            // Tall enough that the button (always at the bottom) sits clear of the progress bar.
            var panel = new Rect(_width / 2f - 310, _height - 176, 620, 152);
            skin.FloatingPanel(panel);
            var tile = new Rect(panel.x + 18, panel.y + 18, 56, 56);

            var travel = _root.Travel;
            if (travel != null && travel.Active)
            {
                // Travelling: fishing is paused; the panel shows the trip instead (GDD section 18).
                skin.IconBadge(tile, Icons.Map, UiSkin.Accent);
                GUI.Label(new Rect(tile.xMax + 16, panel.y + 18, panel.width - 120, 28), GameTexts.Map.Traveling(travel.ToName), skin.Heading);
                GUI.Label(new Rect(tile.xMax + 16, panel.y + 48, panel.width - 120, 22), GameTexts.Map.ArrivesIn(Format.Countdown(travel.SecondsLeft)), skin.SmallMuted);
                skin.Bar(new Rect(panel.x + 22, panel.y + 96, panel.width - 44, 12), (float)travel.Progress);
                return;
            }

            skin.IconBadge(tile, Icons.Fishing, status.IsFishing ? UiSkin.Accent : UiSkin.Muted);
            var phase = _scene != null && _scene.Fisherman != null ? _scene.Fisherman.PhaseText : string.Empty;
            GUI.Label(new Rect(tile.xMax + 16, panel.y + 16, 300, 30), status.IsFishing ? phase : GameTexts.Fishing.Idle, skin.Heading);

            var info = new Rect(panel.xMax - 250, panel.y + 20, 230, 40);
            GUI.Label(new Rect(info.x, info.y, info.width, 20), GameTexts.Fishing.CycleInfo(Format.Duration(status.CycleSeconds)), skin.SmallMutedRight);
            DrawChanceLine(skin, new Rect(info.x - 40, info.y + 20, info.width + 40, 22));

            if (status.IsFishing)
            {
                var remaining = (status.NextCatchAtMs - status.NowMs) / 1000.0;
                GUI.Label(new Rect(tile.xMax + 16, panel.y + 48, 300, 22), GameTexts.Fishing.NextCatchIn(Format.Countdown(remaining)), skin.SmallMuted);
                skin.Bar(new Rect(panel.x + 22, panel.y + 84, panel.width - 44, 8), (float)status.CycleProgress);
            }

            var button = new Rect(panel.x + panel.width / 2f - 130, panel.yMax - 46, 260, 38);
            if (status.IsFishing)
            {
                if (skin.IconButton(button, Icons.Stop, GameTexts.Fishing.Stop, skin.Button))
                {
                    _root.StopFishing();
                }
            }
            else
            {
                // A soft breathing glow invites the first click without shouting.
                skin.DrawGlow(button, UiSkin.Accent, 0.22f + 0.14f * Mathf.Sin(Time.unscaledTime * 2.2f));
            }

            if (!status.IsFishing && skin.IconButton(button, Icons.Play, GameTexts.Fishing.Start, skin.ButtonPrimary))
            {
                _root.StartFishing();
            }
        }

        /// <summary>
        /// The compact gear line of the fishing panel (docs/SISTEMA_SUCESSO_PESCA.md, section 12): the
        /// bait in use with its tentativas left, and the chance of pulling out each rarity that bites
        /// here. Clicking it opens the Shop with the full equipment and chances.
        /// </summary>
        private void DrawChanceLine(UiSkin skin, Rect rect)
        {
            var gear = _root.Gear;
            if (gear == null)
            {
                return;
            }

            var parts = new System.Collections.Generic.List<string>();
            foreach (var chance in gear.Chances)
            {
                if (chance.BitesHere)
                {
                    parts.Add(GameTexts.Fishing.ChanceShort(chance.RarityName, Format.Percent(chance.Chance, 0)));
                }
            }

            var text = string.Join(" · ", parts);
            var textWidth = skin.SmallMutedRight.CalcSize(new GUIContent(text)).x;
            var x = rect.xMax - textWidth - 26;
            skin.DrawIcon(new Rect(x, rect.y + 2, 18, 18), Icons.Fishing, UiSkin.Accent);
            if (gear.BaitName != null)
            {
                var left = Format.Number(gear.BaitChargesLeft);
                var leftWidth = skin.SmallGoldRight.CalcSize(new GUIContent(left)).x + 4f;
                GUI.Label(new Rect(x - leftWidth - 8, rect.y, leftWidth, 22), left, skin.SmallGoldRight);
                skin.DrawIcon(new Rect(x - leftWidth - 30, rect.y + 2, 18, 18), Icons.Bait, UiSkin.Gold);
            }

            GUI.Label(new Rect(rect.x, rect.y, rect.width - 4, 22), text, skin.SmallMutedRight);
            if (GUI.Button(new Rect(rect.x, rect.y - 2, rect.width, 26), new GUIContent(string.Empty, GameTexts.Gear.OpenDetails), GUIStyle.none))
            {
                CloseAllWindows();
                _shop.Open();
            }
        }

        /// <summary>
        /// "Você ainda não é bom o suficiente." rising over the bobber for a moment after a fish
        /// breaks free; the hint below it, smaller. Never a blocking popup (docs/SISTEMA_SUCESSO_PESCA.md, section 3).
        /// </summary>
        private void DrawEscapeMessage(UiSkin skin)
        {
            var rig = _scene != null ? _scene.Fisherman : null;
            var camera = Camera.main;
            if (rig == null || camera == null)
            {
                return;
            }

            var age = Time.time - rig.LastEscapeAt;
            const float duration = 2.8f;
            if (age < 0f || age > duration)
            {
                return;
            }

            var scale = Screen.height / VirtualHeight;
            var point = camera.WorldToScreenPoint(rig.LastEscapePoint);
            var alpha = Mathf.Clamp01(age / 0.2f) * Mathf.Clamp01((duration - age) / 0.6f);
            var cx = point.x / scale;
            var cy = (Screen.height - point.y) / scale - 70f - age * 14f;
            var rare = rig.LastEscapeRarityId != null && rig.LastEscapeRarityId != "common";

            var previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            var panel = new Rect(cx - 250, cy - 8, 500, 64);
            skin.FloatingPanel(panel);
            if (rare)
            {
                GUI.DrawTexture(new Rect(panel.x + 6, panel.y + 10, 4, panel.height - 20), skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.RarityColor(rig.LastEscapeRarityId), 0, 2);
            }

            GUI.Label(new Rect(panel.x, panel.y + 6, panel.width, 26), GameTexts.Fishing.EscapeMessage, skin.CenterBold);
            GUI.Label(new Rect(panel.x, panel.y + 34, panel.width, 22), GameTexts.Fishing.EscapeHint, skin.SmallMutedCenter);
            GUI.color = previous;
        }

        private Rect BoxButtonRect => new Rect(_width - 24 - 270, _height - 24 - 92, 270, 92);

        private void DrawBoxButton(UiSkin skin)
        {
            var player = _root.Player;
            if (player == null)
            {
                return;
            }

            var pulse = Time.unscaledTime < _boxPulseUntil ? 1f + Mathf.Sin((_boxPulseUntil - Time.unscaledTime) * 12f) * 0.04f : 1f;
            var baseRect = BoxButtonRect;
            var rect = new Rect(baseRect.xMax - baseRect.width * pulse, baseRect.yMax - baseRect.height * pulse, baseRect.width * pulse, baseRect.height * pulse);

            skin.DrawShadow(rect);
            if (GUI.Button(rect, GUIContent.none, skin.Button))
            {
                _box.Open();
            }

            skin.DrawIcon(new Rect(rect.x + 16, rect.y + 20, 52, 52), Icons.Box, UiSkin.Gold);
            GUI.Label(new Rect(rect.x + 82, rect.y + 20, rect.width - 120, 26), GameTexts.Box.Open, skin.BodyBold);
            GUI.Label(new Rect(rect.x + 82, rect.y + 48, rect.width - 120, 24), GameTexts.Box.Count(player.FishingBoxCount), skin.SmallMuted);
            skin.DrawIcon(new Rect(rect.xMax - 34, rect.center.y - 9, 18, 18), Icons.Chevron, UiSkin.Muted);
        }

        /// <summary>Level 2 of the visual intensity system: quick, non-blocking toasts with a coloured accent.</summary>
        private void DrawToasts(UiSkin skin)
        {
            var items = _root.Toasts.Items;
            var y = 112f; // below the Conchas / Dólares strip
            for (var i = items.Count - 1; i >= 0; i--)
            {
                var toast = items[i];
                var age = Time.unscaledTime - toast.CreatedAt;
                var fadeIn = Mathf.Clamp01(age / 0.25f);
                var fadeOut = Mathf.Clamp01((toast.Duration - age) / 0.6f);
                var alpha = Mathf.Min(fadeIn, fadeOut);
                var accent = ToastAccent(toast.Kind);

                var rect = new Rect(_width - 464 + (1f - fadeIn) * 40f, y, 440, 62);
                var previous = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, alpha);
                skin.FloatingPanel(rect);
                GUI.DrawTexture(new Rect(rect.x + 6, rect.y + 10, 4, rect.height - 20), skin.White, ScaleMode.StretchToFill, true, 0, accent, 0, 2);

                var textX = rect.x + 22;
                if (toast.Icon != null)
                {
                    GUI.DrawTexture(new Rect(rect.x + 16, rect.y + 9, 80, 44), toast.Icon, ScaleMode.ScaleToFit, true);
                    textX = rect.x + 104;
                }
                else
                {
                    skin.DrawIcon(new Rect(rect.x + 20, rect.y + 19, 24, 24), ToastIcon(toast.Kind), accent);
                    textX = rect.x + 56;
                }

                GUI.Label(new Rect(textX, rect.y + 11, rect.xMax - textX - 14, 44), toast.Text, toast.Kind == ToastKind.Warning ? skin.SmallBold : skin.Small);
                GUI.color = previous;
                y += 70;
            }
        }

        private static Color ToastAccent(ToastKind kind)
        {
            switch (kind)
            {
                case ToastKind.Important:
                case ToastKind.Coins:
                case ToastKind.LevelUp: return UiSkin.Gold;
                case ToastKind.Warning: return UiSkin.Danger;
                default: return UiSkin.Accent;
            }
        }

        private static string ToastIcon(ToastKind kind)
        {
            switch (kind)
            {
                case ToastKind.Coins: return Icons.Coin;
                case ToastKind.LevelUp: return Icons.Level;
                case ToastKind.Important: return Icons.Star;
                case ToastKind.Warning: return Icons.Warning;
                case ToastKind.Catch: return Icons.Fish;
                default: return Icons.Info;
            }
        }

        // ------------------------------------------------------------------ tutorial (GDD section 40)

        private void DrawTutorial(UiSkin skin, bool windowOpen)
        {
            var tutorial = _root.Tutorial;
            if (tutorial == null || !tutorial.Active || _root.WelcomeBack != null)
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
                : new Rect(20, _cardExpanded ? 424 : 144, 340, 200);
            skin.DrawShadow(rect);
            GUI.Box(rect, GUIContent.none, skin.PanelSolid);
            skin.DrawOutline(rect, UiSkin.Accent);
            if (windowOpen)
            {
                GUI.Label(new Rect(rect.x + 18, rect.y + 10, rect.width - 220, 22), GameTexts.Tutorial.Title(tutorial.Step), skin.BodyBold);
                GUI.Label(new Rect(rect.x + 18, rect.y + 34, rect.width - 220, 40), GameTexts.Tutorial.Body(tutorial.Step), skin.Small);
            }
            else
            {
                GUI.contentColor = UiSkin.Accent;
                GUI.Label(new Rect(rect.x + 16, rect.y + 12, rect.width - 32, 18), GameTexts.Tutorial.StepOf(tutorial.StepNumber, tutorial.StepCount), skin.SmallBold);
                GUI.contentColor = Color.white;
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
                case TutorialSteps.StartFishing: target = new Rect(_width / 2f - 130, _height - 70, 260, 38); break;
                case TutorialSteps.OpenBox:
                case TutorialSteps.SellFish:
                case TutorialSteps.KeepFish: target = BoxButtonRect; break;
                default: return;
            }

            if (_outline == null)
            {
                _outline = new GUIStyle { border = new RectOffset(12, 12, 12, 12) };
                _outline.normal.background = Art.RoundedRectTexture(new Color(0f, 0f, 0f, 0f), UiSkin.Accent, 12, 3f);
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
            return NavLayout(UiSkin.Get(), out _)[index];
        }

        // ------------------------------------------------------------------ notifications and settings

        private void DrawPanels(UiSkin skin)
        {
            if (_showNotifications)
            {
                var panel = new Rect(_width - 24 - 470, 70, 470, 560);
                skin.FloatingPanel(panel);
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
                skin.FloatingPanel(panel);
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
