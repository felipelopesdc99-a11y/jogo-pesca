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
    /// Layout follows GDD section 8: navigation on top, the living scene in the centre with a minimal
    /// fishing overlay, quick Fishing Box access, toasts that never block. The player card, with the
    /// wallet (Moedas, Conchas, Dólares) inside it, sits on the right under the bar and the toasts
    /// below it; the tutorial hint uses the left side (owner request 07/10/2026). Everything is laid
    /// out on a 1080-pixel-tall virtual canvas and scaled.
    /// </remarks>
    public sealed class Hud : MonoBehaviour
    {
        private const float VirtualHeight = 1080f;

        // Main bar, ~25% bigger than the old 64 px one (owner request 07/10/2026). Windows start at
        // y 120 or lower (WindowFrame.Panel centres at most 880 px of height on the 1080 canvas), so the
        // bar (80) and its shade (92) never reach them.
        private const float BarHeight = 80f;
        private const float NavButtonY = 12f;       // 52 px face + 4 px lip: 12..68 inside the 80 px bar
        private const float NavButtonHeight = 52f;  // the old menu buttons were 40
        private const float NavSpacing = 8f;

        // Bell and options at the right of the bar: 20 margin + 56 + 8 + 56.
        private const float TopRightExtent = 140f;

        // The player card on the right, under the bar.
        private const float CardWidth = 360f;
        private const float CardTop = BarHeight + 16f;
        private const float CardExpandedHeight = 356f;
        private const float CardCollapsedHeight = 64f;
        private const float ToastWidth = 440f;

        private GameRoot _root;
        private FishingScene _scene;
        private FishingBoxWindow _box;
        private AquariumWindow _aquarium;
        private ProfileWindow _profile;
        private MapWindow _map;
        private ShopWindow _shop;
        private AdminWindow _admin;
        private ExpeditionWindow _expedition;
        private readonly ArrivalTitle _arrival = new ArrivalTitle();
        private ArenaWindow _arena;
        private MarketWindow _market;
        private RankingWindow _ranking;
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

        // The exact-value tooltip of the wallet (A-124) is drawn last, above the card and toasts.
        private bool _tipPending;
        private Rect _tipArea;
        private long _tipValue;

        // Measured each frame by the top bar (logo and map name), so the main menu knows how much room it has.
        private float _topLeftExtent = 300f;

        private void Start()
        {
            _root = GetComponent<GameRoot>();
            _scene = GetComponent<FishingScene>();
            _box = new FishingBoxWindow(_root);
            _aquarium = new AquariumWindow(_root);
            _profile = new ProfileWindow(_root);
            _map = new MapWindow(_root);
            _shop = new ShopWindow(_root);
            _admin = new AdminWindow(_root);
            _expedition = new ExpeditionWindow(_root);
            _arena = new ArenaWindow(_root);
            _market = new MarketWindow(_root);
            _ranking = new RankingWindow(_root);
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
                    _root.Toasts.Push(GameTexts.Toasts.PerfectWithSize(c.SpeciesName, size), ToastKind.Important, icon, notify: true, sound: SoundCue.RareCatch);
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

            if (update.DollarsGained > 0)
            {
                _root.Toasts.Push(GameTexts.Toasts.Dollars(Format.Number(update.DollarsGained)), ToastKind.Important);
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
                    _root.Celebrations.Show(GameTexts.Celebration.RarityCatch(best.RarityId), GameTexts.Celebration.RareLine(best.SpeciesName, best.RarityName, size), theme.Rarity(best.RarityId), art);
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
            _tipPending = false;

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

            // Mouse wheel over the scene: a gentle zoom towards the fisherman (A-102). Never while a window
            // or panel is open, where the wheel scrolls lists.
            if (Event.current.type == EventType.ScrollWheel && !_root.WindowOpen && !_showNotifications && !_showSettings)
            {
                GameSettings.Zoom -= Event.current.delta.y * 0.06f;
                Event.current.Use();
            }

            // The owner's test tools (A-123): F2 opens and closes them, only in the Editor or a development build.
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.F2 && _root.DevToolsEnabled)
            {
                _admin.Toggle();
                Event.current.Use();
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
                else if (_ranking.IsOpen)
                {
                    _ranking.Close();
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
            var windowOpen = _box.IsOpen || _aquarium.IsOpen || _profile.IsOpen || _map.IsOpen || _shop.IsOpen || _expedition.IsOpen || _arena.IsOpen || _market.IsOpen || _ranking.IsOpen || _root.WelcomeBack != null || welcomeDialog;
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
            _ranking.Draw(skin, _width, _height);
            GUI.color = previousColor;
            GUI.enabled = true;
            if (windowOpen)
            {
                // The navigation stays usable above the windows (not above the welcome-back summary).
                // Never while a window's own confirmation dialog is open: the dialog owns the input.
                GUI.enabled = _root.WelcomeBack == null && !welcomeDialog && !AnyWindowDialog();
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

            if (_root.DevToolsEnabled)
            {
                if (!_admin.IsOpen && GUI.Button(new Rect(12, _height - 42, 130, 30), GameTexts.Dev.Button, skin.Chip))
                {
                    _admin.Toggle();
                }

                _admin.Draw(skin, _width, _height);
            }

            if (_tipPending)
            {
                ExactOnHover(skin, _tipArea, _tipValue);
            }
        }

        /// <summary>True when any window shows its own confirmation or profile dialog over itself.</summary>
        private bool AnyWindowDialog()
        {
            return _box.HasDialog || _aquarium.HasDialog || _profile.HasDialog || _map.HasDialog || _shop.HasDialog
                || _expedition.HasDialog || _arena.HasDialog || _market.HasDialog;
        }

        /// <summary>Queues the exact-value tooltip of a wallet amount (player card); it is drawn at the end of OnGUI.</summary>
        private void QueueExactOnHover(Rect area, long value)
        {
            // Hidden under an open window or panel: no tooltip for something the player cannot see.
            if (_root.WindowOpen || _showNotifications || _showSettings || value < 1_000_000L || !area.Contains(Event.current.mousePosition))
            {
                return;
            }

            _tipPending = true;
            _tipArea = area;
            _tipValue = value;
        }

        // ------------------------------------------------------------------ pieces

        private void DrawTopBar(UiSkin skin)
        {
            skin.TopBarBackground(new Rect(0, 0, _width, BarHeight));

            // Logo block on two lines (title, then the map), so the bigger menu keeps its names at 16:9.
            // ASSET_PENDENTE: logo "Fishing Idle" (ui_logo_fishing_idle.png) replaces the fish icon and the title text.
            var player = _root.Player;
            var titleWidth = skin.Title.CalcSize(new GUIContent(GameTexts.GameTitle)).x;
            skin.DrawIcon(new Rect(22, 21, 40, 38), Icons.Fish, UiSkin.Accent);
            _topLeftExtent = 72f + titleWidth;
            if (player != null)
            {
                GUI.Label(new Rect(72, 8, titleWidth + 8, 36), GameTexts.GameTitle, skin.Title);
                var mapLabel = FishCard.Fit(player.MapName, skin.SmallMuted, 200f);
                skin.DrawIcon(new Rect(73, 49, 14, 14), Icons.Pin, UiSkin.Muted);
                GUI.Label(new Rect(91, 47, 204, 20), mapLabel, skin.SmallMuted);
                _topLeftExtent = Mathf.Max(_topLeftExtent, 91f + Mathf.Min(200f, skin.SmallMuted.CalcSize(new GUIContent(mapLabel)).x));
            }
            else
            {
                GUI.Label(new Rect(72, 22, titleWidth + 8, 36), GameTexts.GameTitle, skin.Title);
            }

            // Primary navigation (GDD section 7).
            DrawNavigation(skin);

            if (player == null)
            {
                return;
            }

            // Secondary menu (GDD section 7): notifications bell and settings, icons only, so the nine main
            // menus keep their names (A-100). The wallet lives in the player card (owner request 07/10/2026).
            var settings = new Rect(_width - 20f - 56f, NavButtonY, 56f, NavButtonHeight);
            if (skin.NavGameButton(settings, Icons.Settings, null, _showSettings))
            {
                _showSettings = !_showSettings;
                _showNotifications = false;
            }

            var bell = new Rect(settings.x - 8f - 56f, NavButtonY, 56f, NavButtonHeight);
            if (skin.NavGameButton(bell, Icons.Bell, null, _showNotifications))
            {
                _showNotifications = !_showNotifications;
                _showSettings = false;
                _root.Toasts.MarkAllRead();
            }

            if (_root.Toasts.Unread > 0)
            {
                // Red dot with the count, like a phone badge.
                var count = _root.Toasts.Unread > 9 ? "9+" : _root.Toasts.Unread.ToString();
                var dot = new Rect(bell.xMax - 20, bell.y + 2, 18, 18);
                GUI.DrawTexture(dot, skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.Danger, 0, 9);
                GUI.Label(dot, count, skin.PillText);
            }
        }

        /// <summary>
        /// The wallet inside the expanded player card: Moedas on the first line (counting up, with the
        /// "+N" of a gain at the right of that line), Conchas and Dólares side by side on the second.
        /// Conchas buy and upgrade every item, and both are traded between players (A-098).
        /// </summary>
        private void DrawWallet(UiSkin skin, Rect area, PlayerView player)
        {
            skin.Inset(area);
            var coinLine = DrawCoins(skin, area.x + 14f, area.y + 24f, 30f, player.Coins);
            skin.Divider(new Rect(area.x + 14f, area.y + 45f, area.width - 28f, 1f));
            var half = (area.width - 28f) / 2f;
            DrawCurrency(skin, area.x + 14f, area.y + 64f, Icons.Shell, GameTexts.Player.Shells, player.Shells, true);
            DrawCurrency(skin, area.x + 14f + half, area.y + 64f, Icons.Dollar, GameTexts.Player.Dollars, player.Dollars, true);
            DrawCoinGain(skin, coinLine, area.xMax - 10f);
        }

        /// <summary>
        /// A small amount with its icon (Conchas, Dólares), vertically centred on <paramref name="centreY"/>,
        /// optionally followed by the currency name. Returns the x where it ends.
        /// </summary>
        private float DrawCurrency(UiSkin skin, float x, float centreY, string icon, string name, long value, bool showName)
        {
            // ASSET_PENDENTE: ui_currency_shell.png / ui_currency_dollar.png (coloured, 64 px) replace the tinted icons.
            var text = Format.Short(value);
            var textWidth = skin.SmallBold.CalcSize(new GUIContent(text)).x + 2f;
            skin.DrawIcon(new Rect(x, centreY - 9f, 18, 18), icon, Color.white);
            GUI.Label(new Rect(x + 24f, centreY - 9f, textWidth + 2f, 20), text, skin.SmallBold);
            var end = x + 24f + textWidth;
            if (showName)
            {
                var nameWidth = skin.SmallMuted.CalcSize(new GUIContent(name)).x + 2f;
                GUI.Label(new Rect(end + 6f, centreY - 9f, nameWidth + 2f, 20), name, skin.SmallMuted);
                end += 6f + nameWidth;
            }

            QueueExactOnHover(new Rect(x - 4f, centreY - 13f, end - x + 8f, 26f), value);
            return end;
        }

        /// <summary>
        /// The "+N" of a coin gain, on the coin line of the card: right-aligned at <paramref name="right"/>
        /// (never over the coin number), on a small dark pill so it reads over anything under it.
        /// </summary>
        private void DrawCoinGain(UiSkin skin, Rect coinLine, float right)
        {
            var since = Time.unscaledTime - _coinsGainAt;
            if (_coinsGain <= 0 || since >= 1.4f)
            {
                return;
            }

            var text = GameTexts.Celebration.Coins(Format.Number(_coinsGain));
            var w = skin.SmallGoldLine.CalcSize(new GUIContent(text)).x + 4f;
            var x = Mathf.Max(coinLine.xMax + 10f, right - w - 8f);
            // Rises at most ~8 px, staying inside the coin line.
            var y = coinLine.center.y - 11f - since * 6f;
            var previous = GUI.color;
            GUI.color = previous * new Color(1f, 1f, 1f, Mathf.Clamp01(1.4f - since));
            GUI.DrawTexture(new Rect(x - 4f, y - 1f, w + 8f, 24f), skin.White, ScaleMode.StretchToFill, true, 0, new Color(0.04f, 0.08f, 0.14f, 0.85f), 0, 11);
            GUI.Label(new Rect(x, y + 2f, w, 22), text, skin.SmallGoldLine);
            GUI.color = previous;
        }

        /// <summary>A short amount ("12,4 mi") shows the full number while the mouse is over it (A-124).</summary>
        internal static void ExactOnHover(UiSkin skin, Rect area, long value)
        {
            if (value < 1_000_000L || !area.Contains(Event.current.mousePosition))
            {
                return;
            }

            var text = Format.Number(value);
            var w = skin.SmallBold.CalcSize(new GUIContent(text)).x + 20f;
            var tip = new Rect(area.xMax - w, area.yMax + 4, w, 26);
            GUI.DrawTexture(tip, skin.White, ScaleMode.StretchToFill, true, 0, new Color(0.04f, 0.08f, 0.14f, 0.95f), 0, 8);
            GUI.Label(new Rect(tip.x + 10, tip.y + 4, w - 12, 20), text, skin.SmallBold);
        }

        /// <summary>
        /// The coin counter: it counts up to the new total (the coin pulses meanwhile). Drawn from
        /// <paramref name="x"/>, centred on <paramref name="centreY"/>; returns the rect of icon and number.
        /// </summary>
        private Rect DrawCoins(UiSkin skin, float x, float centreY, float iconSize, long coins)
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
            var text = Format.Short(_coinsShown);
            var textWidth = skin.Number.CalcSize(new GUIContent(text)).x;
            var pulse = k < 1f ? 1f + 0.12f * Mathf.Sin(k * Mathf.PI) : 1f;
            var size = iconSize * pulse;
            // ASSET_PENDENTE: ui_currency_coin.png (moeda pintada, 64 px) in the final style replaces ico_moeda.
            skin.CoinIcon(new Rect(x + (iconSize - size) / 2f, centreY - size / 2f, size, size));
            GUI.Label(new Rect(x + iconSize + 8f, centreY - 13f, textWidth + 4f, 30f), text, skin.Number);
            var line = new Rect(x - 4f, centreY - 16f, iconSize + 8f + textWidth + 8f, 32f);
            QueueExactOnHover(line, _coinsShown);
            return line;
        }

        /// <summary>
        /// Where each main menu button goes: icon + label, centred on the screen when that fits, otherwise
        /// centred in the room between the logo block and the bell; icons only when even that is too narrow.
        /// </summary>
        private Rect[] NavLayout(UiSkin skin, out bool iconsOnly)
        {
            var labels = NavLabels();
            var widths = new float[labels.Length];
            var total = -NavSpacing;
            for (var i = 0; i < labels.Length; i++)
            {
                widths[i] = skin.NavGameWidth(labels[i]);
                total += widths[i] + NavSpacing;
            }

            var roomLeft = _topLeftExtent + 16f;
            var roomRight = _width - TopRightExtent - 16f;
            iconsOnly = total > roomRight - roomLeft;
            if (iconsOnly)
            {
                for (var i = 0; i < widths.Length; i++)
                {
                    widths[i] = UiSkin.NavGameIconWidth;
                }

                total = labels.Length * (UiSkin.NavGameIconWidth + NavSpacing) - NavSpacing;
            }

            var x = _width / 2f - total / 2f;
            if (x < roomLeft || x + total > roomRight)
            {
                x = Mathf.Max(roomLeft, (roomLeft + roomRight) / 2f - total / 2f);
            }

            var rects = new Rect[labels.Length];
            for (var i = 0; i < labels.Length; i++)
            {
                rects[i] = new Rect(x, NavButtonY, widths[i], NavButtonHeight);
                x += widths[i] + NavSpacing;
            }

            return rects;
        }

        private string[] NavLabels()
        {
            return new[] { GameTexts.Navigation.Fishing, GameTexts.Navigation.Map, AquariumLabel(), GameTexts.Navigation.Arena, GameTexts.Navigation.Ranking, GameTexts.Navigation.Expedition, GameTexts.Navigation.Market, GameTexts.Navigation.Shop, GameTexts.Navigation.Profile };
        }

        private static readonly string[] NavIcons = { Icons.Fishing, Icons.Map, Icons.Aquarium, Icons.Arena, Icons.Ranking, Icons.Expedition, Icons.Market, Icons.Shop, Icons.Profile };

        /// <summary>Main menus (GDD section 7). Pesca closes any window; the others open theirs.</summary>
        private void DrawNavigation(UiSkin skin)
        {
            var labels = NavLabels();
            var rects = NavLayout(skin, out var iconsOnly);
            var active = _map.IsOpen ? 1 : _aquarium.IsOpen ? 2 : _arena.IsOpen ? 3 : _ranking.IsOpen ? 4 : _expedition.IsOpen ? 5 : _market.IsOpen ? 6 : _shop.IsOpen ? 7 : _profile.IsOpen ? 8 : 0;

            for (var i = 0; i < labels.Length; i++)
            {
                // ASSET_PENDENTE: ico_nav_* (conjunto de 9 ícones do menu, 96 px) in the final style.
                var clicked = skin.NavGameButton(rects[i], NavIcons[i], iconsOnly ? null : labels[i], i == active) && i != active;
                if (i == 5 && _root.ExpeditionResult != null && !_expedition.IsOpen)
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
                    if (i == 4) _ranking.Open();
                    if (i == 5) _expedition.Open();
                    if (i == 6) _market.Open();
                    if (i == 7) _shop.Open();
                    if (i == 8) _profile.Open();
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
            // Close() first closes a window's inner dialog or selection: repeat until the window itself is closed.
            CloseFully(() => _aquarium.IsOpen, _aquarium.Close);
            CloseFully(() => _box.IsOpen, _box.Close);
            CloseFully(() => _profile.IsOpen, _profile.Close);
            CloseFully(() => _map.IsOpen, _map.Close);
            CloseFully(() => _shop.IsOpen, _shop.Close);
            CloseFully(() => _expedition.IsOpen, _expedition.Close);
            CloseFully(() => _arena.IsOpen, _arena.Close);
            CloseFully(() => _market.IsOpen, _market.Close);
            CloseFully(() => _ranking.IsOpen, _ranking.Close);

            // A panel left open would sit on top of the window and catch its clicks.
            _showNotifications = false;
            _showSettings = false;
        }

        /// <summary>Calls <paramref name="close"/> until the window reports closed (at most a few times, never forever).</summary>
        private static void CloseFully(Func<bool> isOpen, Action close)
        {
            for (var i = 0; i < 5 && isOpen(); i++)
            {
                close();
            }
        }

        /// <summary>The player card: on the right, under the bar; collapsed it keeps only the wallet line.</summary>
        private Rect CardRect => new Rect(_width - 20f - CardWidth, CardTop, CardWidth, _cardExpanded ? CardExpandedHeight : CardCollapsedHeight);

        /// <summary>
        /// The player card with the wallet inside it (owner request 07/10/2026): avatar, name, level and
        /// XP, then Moedas, Conchas and Dólares, then gear and progress. Collapsed: one wallet line.
        /// </summary>
        private void DrawPlayerCard(UiSkin skin)
        {
            var player = _root.Player;
            if (player == null)
            {
                return;
            }

            var card = CardRect;
            skin.FloatingPanel(card);

            // The notifications and options panels open over the card: its button must not catch their clicks.
            var enabled = GUI.enabled;
            var buttonEnabled = enabled && !_showNotifications && !_showSettings;

            if (!_cardExpanded)
            {
                var expand = new Rect(card.xMax - 14f - 44f, card.y + 12f, 44f, 40f);
                // Moedas big on the left; Conchas over Dólares in a small column beside them.
                var coinLine = DrawCoins(skin, card.x + 16f, card.center.y, 26f, player.Coins);
                DrawCurrency(skin, coinLine.xMax + 14f, card.y + 21f, Icons.Shell, GameTexts.Player.Shells, player.Shells, false);
                DrawCurrency(skin, coinLine.xMax + 14f, card.y + 43f, Icons.Dollar, GameTexts.Player.Dollars, player.Dollars, false);
                GUI.enabled = buttonEnabled;
                if (skin.IconButton(expand, Icons.Profile, null, skin.Chip))
                {
                    _cardExpanded = true;
                }

                GUI.enabled = enabled;
                DrawCoinGain(skin, coinLine, expand.x - 6f);
                return;
            }

            // Avatar: the painted fisherman's head in a tile (or the profile icon).
            // ASSET_PENDENTE: ui_avatar_frame.png (moldura do avatar, 96 px) replaces the plain tile.
            var avatar = new Rect(card.x + 16, card.y + 16, 64, 64);
            GUI.Box(avatar, GUIContent.none, skin.IconTile);
            AvatarArt.Draw(skin, new Rect(avatar.x + 3, avatar.y + 3, avatar.width - 6, avatar.height - 6), player.AvatarId);

            GUI.Label(new Rect(avatar.xMax + 14, card.y + 16, 200, 28), FishCard.Fit(player.PlayerName, skin.Heading, 200), skin.Heading);
            GUI.enabled = buttonEnabled;
            if (skin.IconButton(new Rect(card.xMax - 50, card.y + 14, 36, 32), Icons.Chevron, null, skin.Chip))
            {
                _cardExpanded = false;
            }

            GUI.enabled = enabled;
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

            // Wallet: y 110..194 of the card.
            DrawWallet(skin, new Rect(card.x + 16, card.y + 110, card.width - 32, 84), player);

            skin.Divider(new Rect(card.x + 16, card.y + 206, card.width - 32, 1));
            var rod = player.RodName == null ? GameTexts.Player.NoRod
                : player.RodHasLevels ? player.RodName + " (" + GameTexts.Player.LevelShort + " " + player.RodLevel + ")" : player.RodName;
            var y = card.y + 218;
            Row(skin, card, ref y, Icons.Rod, GameTexts.Player.Rod, rod);
            Row(skin, card, ref y, Icons.Pin, GameTexts.Player.Map, player.MapName);
            Row(skin, card, ref y, Icons.Fish, GameTexts.Player.TotalCatches, Format.Number(player.TotalCatches));
            Row(skin, card, ref y, Icons.Book, GameTexts.Player.SpeciesDiscovered, player.SpeciesDiscovered.ToString());
        }

        private static void Row(UiSkin skin, Rect card, ref float y, string icon, string label, string value)
        {
            skin.DrawIcon(new Rect(card.x + 18, y + 1, 18, 18), icon, UiSkin.Muted);
            GUI.Label(new Rect(card.x + 46, y, 160, 22), label, skin.SmallMuted);
            GUI.Label(new Rect(card.x + 150, y, card.width - 168, 22), value, skin.SmallRight);
            y += 32;
        }

        /// <summary>The fishing panel at the bottom centre (the toasts column keeps clear of it).</summary>
        private Rect FishingPanelRect => new Rect(_width / 2f - 310, _height - 176, 620, 152);

        private void DrawFishingControls(UiSkin skin)
        {
            var status = _root.Status;
            if (status == null)
            {
                return;
            }

            // Tall enough that the button (always at the bottom) sits clear of the progress bar.
            var panel = FishingPanelRect;
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
            var boxFull = _root.Player != null && _root.Player.FishingBoxFull;
            var info = new Rect(panel.xMax - 250, panel.y + 20, 230, 40);
            // The status text keeps clear of the cycle info on the right; stopped, it is a body text on two lines.
            var statusX = tile.xMax + 16;
            var statusW = info.x - 10 - statusX;
            if (!status.IsFishing)
            {
                GUI.Label(new Rect(statusX, panel.y + 16, statusW, 48), GameTexts.Fishing.Idle, skin.Body);
            }
            else
            {
                GUI.Label(new Rect(statusX, panel.y + 16, statusW, 30), boxFull ? GameTexts.Box.FullShort : phase, boxFull ? skin.SmallGold : skin.Heading);
            }

            GUI.Label(new Rect(info.x, info.y, info.width, 20), GameTexts.Fishing.CycleInfo(Format.Duration(status.CycleSeconds)), skin.SmallMutedRight);

            // Rows: status (16), next bite (46), gear and chances on their own line (68), progress bar (92), button (106).
            DrawChanceLine(skin, new Rect(statusX, panel.y + 68, panel.xMax - 20 - statusX, 20));

            if (status.IsFishing)
            {
                var remaining = (status.NextCatchAtMs - status.NowMs) / 1000.0;
                GUI.Label(new Rect(statusX, panel.y + 46, statusW, 22), GameTexts.Fishing.NextCatchIn(Format.Countdown(remaining)), skin.SmallMuted);
                skin.Bar(new Rect(panel.x + 22, panel.y + 92, panel.width - 44, 8), (float)status.CycleProgress);
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

            // One line, right-aligned, never wrapping: [bait icon][charges]  [rod icon] chances.
            // The pieces are measured first and the chances shortened with "…" if the line is too long,
            // so the icons always stay inside the rect.
            var style = skin.SmallMutedRightLine;
            var left = gear.BaitName != null ? Format.Number(gear.BaitChargesLeft) : null;
            var leftWidth = left != null ? skin.SmallGoldRight.CalcSize(new GUIContent(left)).x + 4f : 0f;
            var baitBlock = left != null ? 22f + leftWidth + 8f : 0f;
            var maxText = Mathf.Max(0f, rect.width - 26f - baitBlock - 4f);
            var text = FishCard.Fit(string.Join(" · ", parts), style, maxText);
            var textWidth = Mathf.Min(maxText, style.CalcSize(new GUIContent(text)).x);
            var x = rect.xMax - 4f - textWidth - 26f;
            skin.DrawIcon(new Rect(x, rect.y + 1, 18, 18), Icons.Fishing, UiSkin.Accent);
            if (left != null)
            {
                GUI.Label(new Rect(x - leftWidth - 8, rect.y, leftWidth, rect.height), left, skin.SmallGoldRight);
                skin.DrawIcon(new Rect(x - leftWidth - 30, rect.y + 1, 18, 18), Icons.Bait, UiSkin.Gold);
            }

            GUI.Label(new Rect(rect.xMax - 4f - textWidth - 2f, rect.y, textWidth + 2f, rect.height), text, style);
            if (GUI.Button(new Rect(rect.x, rect.y - 2, rect.width, rect.height + 4), new GUIContent(string.Empty, GameTexts.Gear.OpenDetails), GUIStyle.none))
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
            // Wide enough for the rare-escape line (about 520 px) on one line.
            var panel = new Rect(cx - 300, cy - 8, 600, 64);
            skin.FloatingPanel(panel);
            if (rare)
            {
                GUI.DrawTexture(new Rect(panel.x + 6, panel.y + 10, 4, panel.height - 20), skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.RarityColor(rig.LastEscapeRarityId), 0, 2);
            }

            GUI.Label(new Rect(panel.x, panel.y + 6, panel.width, 26), GameTexts.Fishing.EscapeMessage, skin.CenterBold);
            GUI.Label(new Rect(panel.x + 16, panel.y + 34, panel.width - 32, 22),
                rare && !string.IsNullOrEmpty(rig.LastEscapeRarityName) ? GameTexts.Fishing.EscapeRare(rig.LastEscapeRarityName) : GameTexts.Fishing.EscapeHint, skin.SmallMutedCenter);
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
            // Texts from x + 78 up to 4 px before the chevron (x + width - 30): about 158 px, enough for
            // "Caixa cheia: venda peixes" on one line.
            var textW = rect.width - 78 - 34;
            GUI.Label(new Rect(rect.x + 78, rect.y + 20, textW, 26), GameTexts.Box.Open, skin.BodyBold);
            if (player.FishingBoxFull)
            {
                GUI.Label(new Rect(rect.x + 78, rect.y + 48, textW, 24), FishCard.Fit(GameTexts.Box.FullShort, skin.SmallGoldLine, textW), skin.SmallGoldLine);
            }
            else
            {
                GUI.Label(new Rect(rect.x + 78, rect.y + 48, textW, 24), player.FishingBoxCapacity > 0 ? GameTexts.Box.CountOf(player.FishingBoxCount, player.FishingBoxCapacity) : GameTexts.Box.Count(player.FishingBoxCount), skin.SmallMuted);
            }
            skin.DrawIcon(new Rect(rect.xMax - 30, rect.center.y - 9, 18, 18), Icons.Chevron, UiSkin.Muted);
        }

        /// <summary>Level 2 of the visual intensity system: quick, non-blocking toasts with a coloured accent.</summary>
        private void DrawToasts(UiSkin skin)
        {
            var items = _root.Toasts.Items;
            // Under the player card, aligned with its right edge (owner request 07/10/2026).
            var y = _root.Player != null ? CardRect.yMax + 16f : CardTop;
            var left = _width - 20f - ToastWidth;
            // The column stops above the Fishing Box button and, when a narrow screen puts them in line,
            // above the fishing panel; the oldest toasts wait (they still expire on time).
            var bottom = BoxButtonRect.y - 12f;
            if (left < FishingPanelRect.xMax + 12f)
            {
                bottom = Mathf.Min(bottom, FishingPanelRect.y - 12f);
            }

            // With a window open, only the latest toast shows, at the bottom right under the window, so it
            // never covers the window's header ("Fechar") or the Market balance.
            var windowOpen = _root.WindowOpen;
            var last = windowOpen ? items.Count - 1 : 0;
            if (windowOpen)
            {
                y = _height - 74f;
            }

            for (var i = items.Count - 1; i >= last && i >= 0; i--)
            {
                if (!windowOpen && y + 62f > bottom)
                {
                    break;
                }

                var toast = items[i];
                var age = Time.unscaledTime - toast.CreatedAt;
                var fadeIn = Mathf.Clamp01(age / 0.25f);
                var fadeOut = Mathf.Clamp01((toast.Duration - age) / 0.6f);
                var alpha = Mathf.Min(fadeIn, fadeOut);
                var accent = ToastAccent(toast.Kind);

                var rect = new Rect(left + (1f - fadeIn) * 40f, y, ToastWidth, 62);
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

            // With a window open the hint moves to a strip under it; otherwise it sits on the left under the
            // bar (the player card and the toasts are on the right).
            var rect = windowOpen
                ? new Rect(_width / 2f - 440, _height - 88, 880, 76)
                : new Rect(20, CardTop, 340, 200);
            skin.DrawShadow(rect);
            GUI.Box(rect, GUIContent.none, skin.PanelSolid);
            skin.DrawOutline(rect, UiSkin.Accent);
            if (windowOpen)
            {
                GUI.Label(new Rect(rect.x + 18, rect.y + 10, rect.width - 260, 22), GameTexts.Tutorial.Title(tutorial.Step), skin.BodyBold);
                GUI.Label(new Rect(rect.x + 18, rect.y + 34, rect.width - 260, 40), GameTexts.Tutorial.Body(tutorial.Step), skin.Small);
            }
            else
            {
                GUI.contentColor = UiSkin.Accent;
                GUI.Label(new Rect(rect.x + 16, rect.y + 12, rect.width - 32, 18), GameTexts.Tutorial.StepOf(tutorial.StepNumber, tutorial.StepCount), skin.SmallBold);
                GUI.contentColor = Color.white;
                GUI.Label(new Rect(rect.x + 16, rect.y + 32, rect.width - 32, 26), GameTexts.Tutorial.Title(tutorial.Step), skin.BodyBold);
                GUI.Label(new Rect(rect.x + 16, rect.y + 60, rect.width - 32, 90), GameTexts.Tutorial.Body(tutorial.Step), skin.Small);
            }

            // With a window open: "Entendi" (84) + 8 + "Pular tutorial" (120), ending 18 px before the edge.
            var buttons = windowOpen ? new Rect(rect.xMax - 230, rect.y + 20, 212, 36) : new Rect(rect.x + 16, rect.yMax - 46, 140, 32);
            if (tutorial.NeedsAcknowledge && GUI.Button(new Rect(buttons.x, buttons.y, 84, buttons.height), GameTexts.Tutorial.GotIt, skin.ButtonPrimary))
            {
                _root.AcknowledgeTutorial(tutorial.Step);
            }

            var skipRect = windowOpen ? new Rect(buttons.x + (tutorial.NeedsAcknowledge ? 92 : 0), rect.y + 20, tutorial.NeedsAcknowledge ? 120 : 212, 36)
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
                case TutorialSteps.ClaimRod: target = NavRect(7); break;
                case TutorialSteps.Cardume: target = NavRect(8); break;
                case TutorialSteps.Expedition: target = NavRect(5); break;
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
                var panel = new Rect(_width - 24 - 470, BarHeight + 8f, 470, 560);
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
                var panel = new Rect(_width - 24 - 380, BarHeight + 8f, 380, 384);
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

                GUI.Label(new Rect(x, y + 4, 120, 22), GameTexts.Hud.Zoom, skin.Body);
                GameSettings.Zoom = GUI.HorizontalSlider(new Rect(x + 130, y + 10, w - 130, 20), GameSettings.Zoom, 0f, 1f);
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
