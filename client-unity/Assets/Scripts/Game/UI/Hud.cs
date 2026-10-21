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
    /// The game interface on top of the scene: the top menu, fishing controls, Fishing Box button,
    /// notifications, and the windows.
    /// </summary>
    /// <remarks>
    /// Layout follows GDD section 8 with the top menu chosen by the owner on 12/10/2026 (M24-T43, A-169): a thin
    /// strip with the player card on the left (Moedas, Conchas, Dólares; a click opens the Perfil), four big coloured
    /// tabs in the centre (Pesca, Aventura, Comércio, Perfil; the Tripulação left the game in M24-T46, A-172) and the bell
    /// and options on the right; the round shortcuts (Mapa, Aquário, Passe, Missões, Eventos, Ranking) in a column on the right
    /// with their names on hover, at most two toasts left of them; the tutorial hint on the left. Since A-190 the Moedas
    /// show only on the player card (the plaque that repeated them left) and the fishing panel is one compact line. Everything is laid out on a 1080-pixel-tall
    /// virtual canvas and scaled.
    /// </remarks>
    public sealed class Hud : MonoBehaviour
    {

        // The top strip (M24-T43, A-169). Windows start at y 120 or lower (WindowFrame.Panel centres at most 880 px of
        // height on the 1080 canvas); since A-170 they are logbook pages whose label and seal sit just under the strip's
        // wooden trim (80..86).
        private const float BarHeight = 80f;

        // The four big tabs: 64 px tall (y 8..72; the active one grows 7%), 160 px wide or as wide as the longest label.
        private const float TabTop = 8f, TabHeight = 64f, TabWidth = 160f, TabGap = 14f, TabIconOnlyWidth = 76f;

        // The bell and the options at the right end of the strip: 20 margin + 2 × 52 + 8.
        private const float SideButton = 52f, SideButtonY = 12f;
        private const float TopRightExtent = 20f + SideButton * 2f + 8f;

        // The player card at the left of the strip.
        private const float PlayerCardHeight = 64f, PortraitSize = 50f;

        // The sub-tabs of Aventura and Comércio, under the active tab (only with a window of the group open).
        private const float SubTabY = BarHeight + 10f, SubTabHeight = 32f, SubTabGap = 6f;

        // The round shortcuts in a column on the right: 64 px discs every 80 px with no label under them (the name shows on
        // hover, A-190; were 72 every 116 with a label).
        private const float ShortcutColumnWidth = 96f, ShortcutSize = 64f, ShortcutStep = 80f, ShortcutTop = BarHeight + 32f;

        // The compact fishing panel at the bottom centre (A-190: one status line, the bar and an icon button; was 620 × 152).
        private const float FishingPanelWidth = 480f, FishingPanelHeight = 96f, BottomMargin = 24f;

        // The Caixa button at the bottom right: icon + "128/300" (A-190; was 270 × 92 with the title).
        private const float BoxButtonWidth = 184f, BoxButtonHeight = 64f;

        // At most two toasts over the scene at a time (A-190); the others wait and still expire on time.
        private const int MaxToastsShown = 2;

        // The tutorial hint on the left and the toasts on the right, under the strip.
        private const float UnderBar = BarHeight + 24f;

        // 480 since A-149 (was 440): two lines of the 15 px text hold what two lines of 13 px held.
        private const float ToastWidth = 480f, ToastHeight = 62f;

        // The celebration banner: under the strip, or under a window's label (130..186) and the sub-tabs (90..122)
        // when one is open. While it shows over the scene, the toasts start below it.
        private const float CelebrationTop = 96f, CelebrationTopWindow = 196f, CelebrationBottom = CelebrationTop + Celebrations.Height + 12f;

        // With a window open, the latest toast and the tutorial hint share the strip under the window: the toast left of
        // the Fishing Box button, the hint left of the toast, never narrower than this (else the toast waits).
        private const float BottomHintMinWidth = 600f, BottomHintMaxWidth = 880f;

        // The clock note at the bottom left, above the test tools' button.
        private const float ClockNoteWidth = 480f, ClockNoteHeight = 96f;

        // The test tools' button at the bottom left (Editor and development builds only): 12 + 130.
        private const float DevButtonRight = 142f;

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

        // Fragments and clothing levels can arrive many at a time: one notice per kind every few seconds (A-163, A-164).
        private readonly FragmentNotices _notices = new FragmentNotices();
        private ArenaWindow _arena;
        private MarketWindow _market;
        private RankingWindow _ranking;
        private PassWindow _pass;
        private MissionsWindow _missions;
        private WorldBossWindow _boss;
        private EventsWindow _events;
        private ClanWindow _clan;
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

        // The tabs of the last frame (the sub-tabs and the tutorial point at them), and the plaque's text styles.
        private Rect[] _tabRects;

        // The sub-tabs' labels and widths, kept between frames (no arrays built every frame).
        private static readonly string[] AdventureSubTabs = { GameTexts.Navigation.Expedition, GameTexts.Navigation.Arena, GameTexts.Navigation.Events };
        private static readonly string[] TradeSubTabs = { GameTexts.Navigation.Market, GameTexts.Navigation.Shop };
        private static readonly string[] ProfileSubTabs = { GameTexts.Navigation.Profile, GameTexts.Navigation.Clan };
        private readonly float[] _subTabWidths = new float[3];
        private readonly GUIContent _measure = new GUIContent();

        // The chances line of the fishing panel, rebuilt only when the service hands a new gear view.
        private FishingIdle.GameService.Shop.GearView _chanceGear;
        private string _chanceText = string.Empty;

        private void Start()
        {
            _root = GetComponent<GameRoot>();
            _scene = GetComponent<FishingScene>();
            try
            {
                CreateWindows();
            }
            catch (Exception exception)
            {
                // Without its windows the HUD can only show the error screen (M24-T38).
                _root.ReportFailure(exception);
            }
        }

        private void CreateWindows()
        {
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
            _pass = new PassWindow(_root);
            _missions = new MissionsWindow(_root);
            _boss = new WorldBossWindow(_root);
            _events = new EventsWindow(_root);
            _clan = new ClanWindow(_root);
            _events.OpenBoss = () =>
            {
                CloseAllWindows();
                _boss.Open();
            };
            _boss.BackToEvents = () =>
            {
                CloseAllWindows();
                _events.Open();
            };
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
                // Genes (M24-T52, A-176): a 5-star fish or a perfect specimen gets its own warning; a perfect gene only a
                // note at the end of the catch's line (about 1 fish in 8 has one: a toast of its own would be noise).
                if (c.Genes != null && c.Genes.IsCelebrated)
                {
                    _root.Toasts.Push(c.Genes.IsPerfectSpecimen ? GameTexts.Toasts.PerfectSpecimen(c.SpeciesName) : GameTexts.Toasts.TopGenes(c.SpeciesName, c.Genes.Stars),
                        ToastKind.Important, icon, notify: true, sound: SoundCue.RareCatch);
                    continue;
                }

                var perfectGenes = GeneUi.PerfectAttributes(c.Genes);
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
                    _root.Toasts.Push(GameTexts.Toasts.Catch(c.SpeciesName, size, c.SizeCategoryName) + (perfectGenes != null ? GameTexts.Toasts.PerfectGenes(perfectGenes) : string.Empty),
                        ToastKind.Catch, icon);
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

            _root.PushPassMilestones(update.LevelsReached);

            // Boat fragments (M24-T16, A-157), rod fragments (M24-T32, A-163) and Fragmentos de Roupa (M24-T33, A-164): small
            // notices grouped by kind (FragmentNotices); a big one when a boat's or a rod's fragments are complete (the player
            // then assembles it with "Montar", M24-T58, A-180).
            var now = Time.unscaledTime;
            foreach (var fragment in update.Fragments)
            {
                var f = fragment;
                if (f.Ready)
                {
                    _root.Toasts.Push(f.NeedsBoatName == null ? GameTexts.Gear.BoatReadyToast(f.BoatName) : GameTexts.Gear.BoatReadyNeedsToast(f.BoatName, f.NeedsBoatName), ToastKind.Important, notify: true);
                }
                else
                {
                    _notices.Add("boat:" + f.BoatId, f.Amount, n => f.Owned
                        ? (n == 1 ? GameTexts.Gear.FragmentExtraToast(f.BoatName) : GameTexts.Gear.BoatFragmentsExtraGroupToast(Format.Number(n), f.BoatName))
                        : n == 1 ? GameTexts.Gear.FragmentToast(f.BoatName, Format.Number(f.Have), Format.Number(f.Required))
                            : GameTexts.Gear.BoatFragmentsGroupToast(Format.Number(n), f.BoatName, Format.Number(f.Have), Format.Number(f.Required)),
                        ToastKind.Info, now, _root.Toasts);
                }
            }

            foreach (var fragment in update.RodFragments)
            {
                var f = fragment;
                if (f.Ready)
                {
                    _root.Toasts.Push(f.NeedsRodName == null ? GameTexts.Gear.RodReadyToast(f.RodName) : GameTexts.Gear.RodReadyNeedsToast(f.RodName, f.NeedsRodName), ToastKind.Important, notify: true);
                }
                else
                {
                    _notices.Add("rod:" + f.RodId, f.Amount, n => f.Owned
                        ? GameTexts.Gear.RodFragmentExtraToast(Format.Number(n), f.RodName)
                        : n == 1 ? GameTexts.Gear.RodFragmentToast(f.RodName, Format.Number(f.Have), Format.Number(f.Required))
                            : GameTexts.Gear.RodFragmentsToast(Format.Number(n), f.RodName, Format.Number(f.Have), Format.Number(f.Required)),
                        ToastKind.Info, now, _root.Toasts);
                }
            }

            if (update.ClothingFragments > 0)
            {
                var clothing = _root.Clothing;
                var cost = clothing != null ? clothing.NextCost : 0;
                var have = update.ClothingFragmentsHave;
                _notices.Add("clothing", update.ClothingFragments, n => cost <= 0
                    ? GameTexts.Gear.ClothingFragmentsExtraToast(Format.Number(n))
                    : n == 1 ? GameTexts.Gear.ClothingFragmentToast(Format.Number(have), Format.Number(cost))
                        : GameTexts.Gear.ClothingFragmentsToast(Format.Number(n), Format.Number(have), Format.Number(cost)),
                    ToastKind.Info, now, _root.Toasts);

                // The moment the fragments reach the next piece: the service says so once (update.ClothingReady).
                if (update.ClothingReady && cost > 0)
                {
                    _root.Toasts.Push(GameTexts.Gear.ClothingReadyToast(Format.Number(cost)), ToastKind.Important, notify: true);
                }
            }

            foreach (var level in update.ClothingLevels)
            {
                var l = level;
                _notices.Add("clothing-level:" + l.PieceId, 1, _ => GameTexts.Gear.ClothingLevelToast(l.PieceName, l.Level), ToastKind.LevelUp, now, _root.Toasts);
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

            // Genes (M24-T52, A-176): the best-genes catch of the batch (5 stars, or the perfect specimen) gets its own banner.
            CatchView topGenes = null;
            foreach (var c in update.NewCatches)
            {
                if (c.Genes != null && c.Genes.IsCelebrated && (topGenes == null || c.Genes.Total > topGenes.Genes.Total))
                {
                    topGenes = c;
                }
            }

            if (topGenes != null)
            {
                var genes = topGenes.Genes;
                _root.Celebrations.Show(genes.IsPerfectSpecimen ? GameTexts.Celebration.PerfectSpecimen : GameTexts.Celebration.TopGenes(genes.Stars),
                    GameTexts.Celebration.GenesLine(topGenes.SpeciesName, Format.Percent(genes.Quality, 0)), theme.Reward, Art.FishTexture(topGenes.SpeciesId));
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

            // Snapped to a clean step where it can be, so the pixel art keeps whole pixels (UiSkin.CanvasScale, A-190); the
            // text is drawn from the font's distance field and stays sharp at any scale (TD-067). The canvas is 1080 to
            // 1280 tall at a clean step, and every window lays itself out on _height.
            var scale = UiSkin.CanvasScale(Screen.height);
            _width = Screen.width / scale;
            _height = Screen.height / scale;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

            UiSkin skin;
            try
            {
                skin = UiSkin.Get();
                _tipPending = false;
                _notices.Tick(Time.unscaledTime, _root.Toasts);
                if (!_root.IsRunning || _boss == null)
                {
                    DrawStartupError(skin);
                    return;
                }
            }
            catch (ExitGUIException)
            {
                throw;
            }
            catch (Exception exception)
            {
                // Even the interface skin failed: the reason in Unity's plain style, so the screen is never empty (M24-T38).
                if (_root.IsRunning)
                {
                    _root.ReportFailure(exception);
                }

                DrawPlainError(exception);
                return;
            }

            try
            {
                DrawRunning(skin);
            }
            catch (ExitGUIException)
            {
                throw;
            }
            catch (Exception exception)
            {
                // An exception in a frame of the HUD would repeat every frame and leave the screen half drawn: the game
                // stops with the error screen and the reason instead (M24-T38).
                _root.ReportFailure(exception);
            }
        }

        private void DrawRunning(UiSkin skin)
        {
            if (_compact)
            {
                DrawCompact(skin);
                return;
            }

            // The owner's test tools (A-123): F2 opens and closes them, only in the Editor or a development build.
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.F2 && _root.DevToolsEnabled)
            {
                _admin.Toggle();
                Event.current.Use();
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                if (_admin.IsOpen)
                {
                    // The test tools (F2) sit above everything: Esc closes them first.
                    _admin.Toggle();
                    Event.current.Use();
                }
                else if (_showNotifications || _showSettings)
                {
                    _showNotifications = false;
                    _showSettings = false;
                    Event.current.Use();
                }
                else if (WindowFrame.CloseInfoBalloon())
                {
                    Event.current.Use();
                }
                else if (WelcomeBackPending)
                {
                    // The "Bem-vindo de volta" report sits over everything: Esc closes it like "Continuar" (A-175).
                    _root.WelcomeBack = null;
                    Event.current.Use();
                }
                else if (_boss.IsOpen)
                {
                    _boss.Close();
                    Event.current.Use();
                }
                else if (_events.IsOpen)
                {
                    _events.Close();
                    Event.current.Use();
                }
                else if (_pass.IsOpen)
                {
                    _pass.Close();
                    Event.current.Use();
                }
                else if (_missions.IsOpen)
                {
                    _missions.Close();
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
                else if (_clan.IsOpen)
                {
                    _clan.Close();
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
            var windowOpen = _box.IsOpen || _aquarium.IsOpen || _profile.IsOpen || _map.IsOpen || _shop.IsOpen || _expedition.IsOpen || _arena.IsOpen || _market.IsOpen || _ranking.IsOpen || _pass.IsOpen || _missions.IsOpen || _boss.IsOpen || _events.IsOpen || _clan.IsOpen || WelcomeBackPending || welcomeDialog;
            _root.WindowOpen = windowOpen;
            GUI.enabled = !windowOpen;
            if (!windowOpen)
            {
                // With a window open, the strip is drawn above it instead (below).
                DrawStrip(skin, false);
            }

            // The bell and options panels are drawn last, over the shortcuts: while one is open, nothing under it
            // takes a click (a click on the panel used to open the Mapa or the Aquário underneath).
            var panelOpen = _showNotifications || _showSettings;
            GUI.enabled = !windowOpen && !panelOpen;
            if (!windowOpen)
            {
                // With a window open, the shortcuts are drawn above it instead (below), still clickable.
                DrawShortcuts(skin);
            }

            DrawFishingControls(skin);
            DrawBoxButton(skin);
            if (!windowOpen)
            {
                DrawEscapeMessage(skin);
                DrawClockFrozen(skin);
            }

            GUI.enabled = true;
            if (windowOpen)
            {
                _showNotifications = false;
                _showSettings = false;
            }

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
            _clan.Draw(skin, _width, _height);
            _map.Draw(skin, _width, _height);
            _shop.Draw(skin, _width, _height);
            _expedition.Draw(skin, _width, _height);
            _arena.Draw(skin, _width, _height);
            _market.Draw(skin, _width, _height);
            _ranking.Draw(skin, _width, _height);
            _pass.Draw(skin, _width, _height);
            _missions.Draw(skin, _width, _height);
            _boss.Draw(skin, _width, _height);
            _events.Draw(skin, _width, _height);
            // The "Como funciona" balloon of a window header, over the window body (A-148).
            WindowFrame.DrawInfoBalloon(skin, AnyWindowDialog());
            GUI.color = previousColor;
            GUI.enabled = true;
            if (windowOpen)
            {
                // The strip and the sub-tabs stay usable above the windows (not above the welcome-back summary).
                // Never while a window's own confirmation dialog is open: the dialog owns the input.
                GUI.enabled = !WelcomeBackPending && !welcomeDialog && !AnyWindowDialog();
                DrawStrip(skin, true);
                DrawSubTabs(skin);
                // The round shortcuts stay beside the window (WindowFrame keeps the frame clear of their column):
                // a click closes the window and opens the shortcut's one, like the tabs.
                DrawShortcuts(skin);
                GUI.enabled = true;
            }

            _arrival.Draw(skin, _width, _height);
            DrawToasts(skin);
            // With a window open, the banner goes below the window's label and the sub-tabs.
            _root.Celebrations.Draw(skin, _width, windowOpen ? CelebrationTopWindow : CelebrationTop);
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

        /// <summary>The "Bem-vindo de volta" summary of the offline fishing is waiting.</summary>
        private bool WelcomeBackPending => _root.WelcomeBack != null;

        /// <summary>True when any window shows its own confirmation or profile dialog over itself.</summary>
        private bool AnyWindowDialog()
        {
            return _box.HasDialog || _aquarium.HasDialog || _profile.HasDialog || _map.HasDialog || _shop.HasDialog
                || _expedition.HasDialog || _arena.HasDialog || _market.HasDialog || _clan.HasDialog;
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

        // ------------------------------------------------------------------ top menu (M24-T43, A-169)

        // The four tabs and the menus each one groups (owner's choice of 12/10/2026, example 2 of menu_mesclas; the
        // Tripulação tab left with the Crew, M24-T46, A-172).
        private const int GroupFishing = 0, GroupAdventure = 1, GroupTrade = 2, GroupProfile = 3, GroupCount = 4, NoGroup = -1;

        private static string GroupLabel(int group)
        {
            switch (group)
            {
                case GroupAdventure: return GameTexts.Navigation.Adventure;
                case GroupTrade: return GameTexts.Navigation.Trade;
                case GroupProfile: return GameTexts.Navigation.Profile;
                default: return GameTexts.Navigation.Fishing;
            }
        }

        /// <summary>The tab icon: the painted ico_aba_* when it exists (ASSET_PENDENTE), otherwise the game's current icon.</summary>
        private static string GroupIcon(int group)
        {
            switch (group)
            {
                case GroupAdventure: return PendingIcon("aba_aventura", Icons.Expedition);
                case GroupTrade: return PendingIcon("aba_comercio", Icons.Market);
                case GroupProfile: return PendingIcon("aba_perfil", Icons.Profile);
                default: return PendingIcon("aba_pesca", Icons.Fishing);
            }
        }

        private static string PendingIcon(string art, string fallback) => ArtAssets.Icon(art) != null ? art : fallback;

        /// <summary>
        /// The tab whose window is open; Pesca when no window is; none for a window outside the tabs (Caixa, Mapa,
        /// Aquário, Passe, Ranking), so Pesca is not lit over it.
        /// </summary>
        private int ActiveGroup()
        {
            if (_expedition.IsOpen || _arena.IsOpen || _events.IsOpen || _boss.IsOpen) return GroupAdventure;
            if (_market.IsOpen || _shop.IsOpen) return GroupTrade;
            if (_profile.IsOpen || _clan.IsOpen) return GroupProfile;
            if (_box.IsOpen || _aquarium.IsOpen || _map.IsOpen || _pass.IsOpen || _missions.IsOpen || _ranking.IsOpen) return NoGroup;
            return GroupFishing;
        }

        /// <summary>
        /// The strip across the top: the player card on the left (a click opens the Perfil), the four tabs in the centre,
        /// the bell and the options on the right; with no window open, the plaque with the Moedas hangs under it.
        /// </summary>
        private void DrawStrip(UiSkin skin, bool windowOpen)
        {
            TopMenuArt.Strip(skin, new Rect(0, 0, _width, BarHeight));
            var player = _root.Player;
            var navEnabled = GUI.enabled;
            var cardRight = DrawPlayerCard(skin, player);
            _tabRects = TabLayout(skin, cardRight, out var iconsOnly);
            var active = ActiveGroup();
            for (var g = 0; g < GroupCount; g++)
            {
                var rect = _tabRects[g];
                if (TopMenuArt.Tab(skin, rect, g, GroupIcon(g), iconsOnly ? null : GroupLabel(g), g == active))
                {
                    OnTab(g, active, windowOpen);
                }

                if (g == GroupAdventure && _root.ExpeditionResult != null && !_expedition.IsOpen)
                {
                    // The Cardume is back: a red dot until the player opens the Expedition and reads the report.
                    TopMenuArt.Badge(skin, rect, 0, UiSkin.Danger);
                }
                else if (g == GroupAdventure && _root.WorldBoss != null && _root.WorldBoss.Open)
                {
                    // An event is happening (the Boss Mundial): a gold dot, like the round shortcut.
                    TopMenuArt.Badge(skin, rect, 0, UiSkin.Gold);
                }
            }

            if (player == null)
            {
                return;
            }

            // The bell and the options, icons only at the right end of the strip; inert while a window is open (as before).
            GUI.enabled = navEnabled && !windowOpen;
            var settings = new Rect(_width - 20f - SideButton, SideButtonY, SideButton, SideButton);
            if (skin.NavGameButton(settings, Icons.Settings, null, _showSettings))
            {
                _showSettings = !_showSettings;
                _showNotifications = false;
            }

            var bell = new Rect(settings.x - 8f - SideButton, SideButtonY, SideButton, SideButton);
            if (skin.NavGameButton(bell, Icons.Bell, null, _showNotifications))
            {
                _showNotifications = !_showNotifications;
                _showSettings = false;
                _root.Toasts.MarkAllRead();
            }

            if (_root.Toasts.Unread > 0)
            {
                TopMenuArt.Badge(skin, bell, _root.Toasts.Unread, UiSkin.Danger);
            }

            GUI.enabled = navEnabled;
        }

        private void OnTab(int group, int active, bool windowOpen)
        {
            if (group == GroupFishing)
            {
                // Pesca: back to the scene.
                if (windowOpen)
                {
                    CloseAllWindows();
                }

                return;
            }

            if (group == active)
            {
                // From inside an event (the Boss window), Aventura goes back to the list of events.
                if (_boss.IsOpen)
                {
                    CloseAllWindows();
                    _events.Open();
                }

                return;
            }

            CloseAllWindows();
            switch (group)
            {
                case GroupAdventure: _expedition.Open(); break;
                case GroupTrade:
                    // The first window of the group (Mercado); during the tutorial step of the first rod, the Loja.
                    if (_root.Tutorial != null && _root.Tutorial.Active && _root.Tutorial.Step == TutorialSteps.ClaimRod)
                    {
                        _shop.Open();
                    }
                    else
                    {
                        _market.Open();
                    }

                    break;
                case GroupProfile: _profile.Open(); break;
            }
        }

        /// <summary>
        /// Where the four tabs go: centred on the screen when that fits, otherwise centred between the player card and
        /// the bell; narrower tabs when needed, and icons only when even the narrowest labels do not fit.
        /// </summary>
        private Rect[] TabLayout(UiSkin skin, float cardRight, out bool iconsOnly)
        {
            var roomLeft = cardRight + 16f;
            var roomRight = _width - TopRightExtent - 16f;
            var room = roomRight - roomLeft;
            var need = 0f;
            for (var g = 0; g < GroupCount; g++)
            {
                need = Mathf.Max(need, TopMenuArt.TabLabelWidth(skin, GroupLabel(g)) + 24f);
            }

            var width = Mathf.Max(TabWidth, need);
            var total = GroupCount * width + (GroupCount - 1) * TabGap;
            if (total > room)
            {
                width = (room - (GroupCount - 1) * TabGap) / GroupCount;
            }

            iconsOnly = width < need;
            if (iconsOnly)
            {
                width = TabIconOnlyWidth;
            }

            total = GroupCount * width + (GroupCount - 1) * TabGap;
            var x = _width / 2f - total / 2f;
            if (x < roomLeft || x + total > roomRight)
            {
                x = Mathf.Max(roomLeft, (roomLeft + roomRight) / 2f - total / 2f);
            }

            var rects = new Rect[GroupCount];
            for (var g = 0; g < GroupCount; g++)
            {
                rects[g] = new Rect(x, TabTop, width, TabHeight);
                x += width + TabGap;
            }

            return rects;
        }

        /// <summary>
        /// With a window of Aventura or Comércio open, its sister windows as small sub-tabs hanging under the active tab
        /// (Expedição | Arena | Eventos, Mercado | Loja).
        /// </summary>
        private void DrawSubTabs(UiSkin skin)
        {
            var group = ActiveGroup();
            string[] labels;
            int active;
            if (group == GroupAdventure)
            {
                labels = AdventureSubTabs;
                active = _expedition.IsOpen ? 0 : _arena.IsOpen ? 1 : 2;
            }
            else if (group == GroupTrade)
            {
                labels = TradeSubTabs;
                active = _market.IsOpen ? 0 : 1;
            }
            else if (group == GroupProfile)
            {
                // Perfil | Clã (M24-T49, A-174).
                labels = ProfileSubTabs;
                active = _profile.IsOpen ? 0 : 1;
            }
            else
            {
                return;
            }

            var widths = _subTabWidths;
            var total = (labels.Length - 1) * SubTabGap;
            for (var i = 0; i < labels.Length; i++)
            {
                _measure.text = labels[i];
                widths[i] = skin.Chip.CalcSize(_measure).x + 32f;
                total += widths[i];
            }

            var tab = _tabRects != null ? _tabRects[group] : new Rect(_width / 2f, 0f, 0f, 0f);
            var x = Mathf.Clamp(tab.center.x - total / 2f, 20f, _width - 20f - total);
            var holder = new Rect(x - 6f, SubTabY - 6f, total + 12f, SubTabHeight + 12f);
            // ARC (A-184): the sub-tabs sit on a small night panel with the dark-gold line.
            skin.LineBox(holder, new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.94f), UiSkin.FrameDark, 2);
            for (var i = 0; i < labels.Length; i++)
            {
                var rect = new Rect(x, SubTabY, widths[i], SubTabHeight);
                // Eventos is also the way back from the Boss window to the list.
                if (TopMenuArt.SubTab(skin, rect, labels[i], i == active) && (i != active || _boss.IsOpen))
                {
                    CloseAllWindows();
                    if (group == GroupAdventure)
                    {
                        if (i == 0) _expedition.Open();
                        if (i == 1) _arena.Open();
                        if (i == 2) _events.Open();
                    }
                    else if (group == GroupTrade)
                    {
                        if (i == 0) _market.Open();
                        if (i == 1) _shop.Open();
                    }
                    else
                    {
                        if (i == 0) _profile.Open();
                        if (i == 1) _clan.Open();
                    }
                }

                if (group == GroupAdventure && i == 0 && _root.ExpeditionResult != null && !_expedition.IsOpen)
                {
                    TopMenuArt.Badge(skin, rect, 0, UiSkin.Danger);
                }

                x += widths[i] + SubTabGap;
            }
        }

        /// <summary>
        /// The round shortcuts in a column on the right: Mapa, Aquário, Passe do Pescador, Missões, Eventos (the Boss Mundial, gold and
        /// breathing with the time left, while it happens) and Ranking. With a window open they stay beside it, clickable:
        /// a click closes the window and opens the shortcut's one (inert only under a window's own dialog).
        /// </summary>
        private void DrawShortcuts(UiSkin skin)
        {
            var player = _root.Player;
            if (player == null)
            {
                return;
            }

            var cx = _width - 20f - ShortcutColumnWidth / 2f;
            var y = ShortcutTop;

            // The discs have no label under them (A-190): the name shows left of the disc under the mouse.
            var hovered = Rect.zero;
            string hoveredName = null;
            void Name(Rect r, string label)
            {
                if (GUI.enabled && r.Contains(Event.current.mousePosition))
                {
                    hovered = r;
                    hoveredName = label;
                }
            }

            Rect Next()
            {
                var r = new Rect(cx - ShortcutSize / 2f, y, ShortcutSize, ShortcutSize);
                y += ShortcutStep;
                return r;
            }

            var map = Next();
            Name(map, GameTexts.Navigation.Map);
            if (TopMenuArt.RoundButton(skin, map, PendingIcon("atalho_mapa", Icons.Map), null, TopMenuArt.Round.Blue, _map.IsOpen))
            {
                CloseAllWindows();
                _map.Open();
            }

            var aquarium = Next();
            Name(aquarium, GameTexts.Navigation.Aquarium);
            if (TopMenuArt.RoundButton(skin, aquarium, PendingIcon("atalho_aquario", Icons.Aquarium), null, TopMenuArt.Round.Blue, _aquarium.IsOpen))
            {
                CloseAllWindows();
                _aquarium.Open();
            }

            TopMenuArt.BottomPill(skin, aquarium, player.AquariumCount + "/" + player.AquariumCapacity);

            var pass = Next();
            Name(pass, GameTexts.Navigation.PassShort);
            if (TopMenuArt.RoundButton(skin, pass, PendingIcon("atalho_passe", PassWindow.NavIcon), null, TopMenuArt.Round.Pass, _pass.IsOpen))
            {
                CloseAllWindows();
                _pass.Open();
            }

            var ready = _root.Pass?.ClaimableCount ?? 0;
            if (ready > 0)
            {
                TopMenuArt.Badge(skin, pass, ready, UiSkin.Danger);
            }

            // Missões (M24-T54, A-177): with the count of missions ready to claim, like the Passe.
            var missions = Next();
            Name(missions, GameTexts.Navigation.Missions);
            if (TopMenuArt.RoundButton(skin, missions, MissionsWindow.NavIcon, null, TopMenuArt.Round.Blue, _missions.IsOpen))
            {
                CloseAllWindows();
                _missions.Open();
            }

            if (_root.MissionsReady > 0)
            {
                TopMenuArt.Badge(skin, missions, _root.MissionsReady, UiSkin.Danger);
            }

            var events = Next();
            var boss = _root.WorldBoss;
            Name(events, boss != null && boss.Open ? GameTexts.WorldBoss.Title : GameTexts.Navigation.Events);
            if (boss != null && boss.Open)
            {
                if (TopMenuArt.RoundButton(skin, events, PendingIcon("atalho_boss", WorldBossWindow.NavIcon), null, TopMenuArt.Round.Gold, _boss.IsOpen))
                {
                    CloseAllWindows();
                    _boss.Open();
                }

                TopMenuArt.TimePill(skin, events, Format.Countdown(boss.SecondsLeft));
            }
            else if (TopMenuArt.RoundButton(skin, events, PendingIcon("atalho_eventos", EventsWindow.NavIcon), null, TopMenuArt.Round.Blue, _events.IsOpen))
            {
                CloseAllWindows();
                _events.Open();
            }

            var ranking = Next();
            Name(ranking, GameTexts.Navigation.Ranking);
            if (TopMenuArt.RoundButton(skin, ranking, PendingIcon("atalho_ranking", Icons.Ranking), null, TopMenuArt.Round.Blue, _ranking.IsOpen))
            {
                CloseAllWindows();
                _ranking.Open();
            }

            if (hoveredName != null)
            {
                HoverName(skin, hovered, hoveredName, false);
            }
        }

        /// <summary>
        /// The player card at the left of the strip: portrait, name and level, and Moedas, Conchas and Dólares. A click opens
        /// the Perfil. Returns where the card ends.
        /// </summary>
        private float DrawPlayerCard(UiSkin skin, PlayerView player)
        {
            if (player == null)
            {
                var titleWidth = skin.Title.CalcSize(new GUIContent(GameTexts.GameTitle)).x;
                GUI.Label(new Rect(24f, 22f, titleWidth + 8f, 36f), GameTexts.GameTitle, skin.Title);
                return 24f + titleWidth + 8f;
            }

            var name = FishCard.Fit(player.PlayerName, skin.BodyBold, 180f);
            var level = GameTexts.Player.LevelShort + " " + player.FisherLevel;
            var line1 = skin.BodyBold.CalcSize(new GUIContent(name)).x + 10f + skin.SmallGold.CalcSize(new GUIContent(level)).x;
            var line2 = CurrencyWidth(skin, player.Coins) + CurrencyWidth(skin, player.Shells) + CurrencyWidth(skin, player.Dollars) + 2f * 14f;
            var width = Mathf.Clamp(12f + PortraitSize + 12f + Mathf.Max(line1, line2) + 14f, 220f, 400f);
            var card = new Rect(16f, (BarHeight - PlayerCardHeight) / 2f, width, PlayerCardHeight);
            var hover = GUI.enabled && card.Contains(Event.current.mousePosition) && !_showNotifications && !_showSettings;
            // ARC (A-184): the card is a field with the dark-gold line (gold on hover).
            skin.LineBox(card, new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.9f), hover ? UiSkin.FrameGold : UiSkin.FrameDark, 2);

            // Portrait: the fisherman's head in a small square frame with the gold line (the painted ring of the old skin
            // is no longer used).
            var avatar = new Rect(card.x + 8f, card.y + (card.height - PortraitSize) / 2f, PortraitSize, PortraitSize);
            skin.LineBox(avatar, UiSkin.FieldColor, UiSkin.FrameGold, 1);
            AvatarArt.Draw(skin, new Rect(avatar.x + 4, avatar.y + 4, avatar.width - 8, avatar.height - 8), player.AvatarId, 0f);

            var x = avatar.xMax + 12f;
            var nameWidth = skin.BodyBold.CalcSize(new GUIContent(name)).x + 2f;
            GUI.Label(new Rect(x, card.y + 6f, nameWidth, 24f), name, skin.BodyBold);
            GUI.Label(new Rect(x + nameWidth + 8f, card.y + 8f, 90f, 22f), level, skin.SmallGold);

            // The Moedas count up to a new total here (the plaque that repeated them under the tabs left in A-190), and
            // the "+N" of a gain shows just under the card.
            var coinsShown = CountCoins(player.Coins);
            var end = DrawCurrency(skin, x, card.y + 44f, Icons.Coin, GameTexts.Player.Coins, coinsShown, false);
            DrawCoinGain(skin, new Rect(x, card.yMax + 4f, 0f, 26f), _width / 2f);
            end = DrawCurrency(skin, end + 14f, card.y + 44f, Icons.Shell, GameTexts.Player.Shells, player.Shells, false);
            DrawCurrency(skin, end + 14f, card.y + 44f, Icons.Dollar, GameTexts.Player.Dollars, player.Dollars, false);

            if (GUI.Button(card, new GUIContent(string.Empty, GameTexts.Navigation.OpenProfile), GUIStyle.none) && !_profile.IsOpen && !_showNotifications && !_showSettings)
            {
                CloseAllWindows();
                _profile.Open();
            }

            return card.xMax;
        }

        private static float CurrencyWidth(UiSkin skin, long value) => 24f + skin.SmallBold.CalcSize(new GUIContent(Format.Short(value))).x + 2f;

        /// <summary>
        /// A small amount with its icon (Moedas, Conchas, Dólares), vertically centred on <paramref name="centreY"/>,
        /// optionally followed by the currency name. Returns the x where it ends.
        /// </summary>
        private float DrawCurrency(UiSkin skin, float x, float centreY, string icon, string name, long value, bool showName)
        {
            var text = Format.Short(value);
            var textWidth = skin.SmallBold.CalcSize(new GUIContent(text)).x + 2f;
            // The coloured pixel icons (ico_moeda / concha / dolar), never tinted; the white icon when one is missing.
            skin.CurrencyIcon(new Rect(x - 1f, centreY - 10f, 20, 20), icon);
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
        /// The "+N" of a coin gain, at the right of the coin line: right-aligned at <paramref name="right"/> at most (never
        /// over the coin number), on a small dark pill so it reads over anything under it.
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
            var x = Mathf.Min(coinLine.xMax + 10f, right - w - 8f);
            x = Mathf.Max(coinLine.xMax + 10f, x);
            // Rises at most ~8 px.
            var y = coinLine.center.y - 11f - since * 6f;
            var previous = GUI.color;
            GUI.color = previous * new Color(1f, 1f, 1f, Mathf.Clamp01(1.4f - since));
            skin.PixelPill(new Rect(x - 6f, y - 1f, w + 12f, 26f), new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.88f), Color.clear);
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
            var tip = new Rect(area.xMax - w, area.yMax + 4, w, 30);
            skin.TipBox(tip);
            GUI.Label(new Rect(tip.x + 10, tip.y + 3, w - 12, UiSkin.SmallLine), text, skin.SmallBold);
        }

        /// <summary>
        /// The Moedas shown: they count up to a new total in <see cref="VisualTheme.CoinCountSeconds"/> and remember the
        /// gain for <see cref="DrawCoinGain"/>. Presentation only: the number comes from the service.
        /// </summary>
        private long CountCoins(long coins)
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
            // In double, and exact at the end: a float cannot hold Moedas in the billions to the unit.
            _coinsShown = k >= 1f ? _coinsTarget : _coinsFrom + (long)((_coinsTarget - _coinsFrom) * (double)(1f - (1f - k) * (1f - k)));
            return _coinsShown;
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
            CloseFully(() => _pass.IsOpen, _pass.Close);
            CloseFully(() => _missions.IsOpen, _missions.Close);
            CloseFully(() => _boss.IsOpen, _boss.Close);
            CloseFully(() => _events.IsOpen, _events.Close);
            CloseFully(() => _clan.IsOpen, _clan.Close);

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

        /// <summary>The fishing panel at the bottom centre (the toasts column keeps clear of it).</summary>
        private Rect FishingPanelRect => new Rect(_width / 2f - FishingPanelWidth / 2f, _height - BottomMargin - FishingPanelHeight, FishingPanelWidth, FishingPanelHeight);

        /// <summary>The Iniciar pesca button of the stopped panel (the tutorial points at it).</summary>
        private Rect StartButtonRect
        {
            get
            {
                var panel = FishingPanelRect;
                return new Rect(panel.center.x - 128f, panel.yMax - 16f - 40f, 256f, 40f);
            }
        }

        /// <summary>
        /// The compact fishing panel (A-190): one status line ("Esperando o peixe morder… · 0:07") with the bait and its
        /// tentativas at the right, the cycle bar and an icon button to stop; stopped, the line and the Iniciar pesca
        /// button. The cycle and the chances show in a note while the mouse is over the panel; a click on the bait opens
        /// the Loja, as before.
        /// </summary>
        private void DrawFishingControls(UiSkin skin)
        {
            var status = _root.Status;
            if (status == null)
            {
                return;
            }

            var panel = FishingPanelRect;
            skin.FloatingPanel(panel);
            var line = new Rect(panel.x + 16f, panel.y + 12f, panel.width - 32f, 32f);

            var travel = _root.Travel;
            if (travel != null && travel.Active)
            {
                // Travelling: fishing is paused; the panel shows the trip instead (GDD section 18).
                GUI.Label(line, FishCard.Fit(GameTexts.Map.Traveling(travel.ToName) + " · " + Format.Countdown(travel.SecondsLeft), skin.BodyBold, line.width), skin.BodyBold);
                skin.Bar(new Rect(panel.x + 16f, panel.y + 60f, panel.width - 32f, 12f), (float)travel.Progress);
                return;
            }

            var boxFull = _root.Player != null && _root.Player.FishingBoxFull;
            if (!status.IsFishing)
            {
                GUI.Label(new Rect(line.x, line.y, line.width, line.height), GameTexts.Fishing.IdleShort, skin.BodyBold);
                var start = StartButtonRect;
                // A soft breathing ring invites the first click without shouting.
                skin.DrawGlow(start, UiSkin.Accent, 0.22f + 0.14f * Mathf.Sin(Time.unscaledTime * 2.2f));
                if (skin.IconButton(start, Icons.Play, GameTexts.Fishing.Start, skin.ButtonPrimary))
                {
                    _root.StartFishing();
                }

                return;
            }

            // The bait and its tentativas at the right of the line (icon + number); a click opens the Loja.
            var baitRight = DrawBaitCharges(skin, new Rect(line.xMax - 160f, line.y + 4f, 160f, 24f));
            var phase = _scene != null && _scene.Fisherman != null ? _scene.Fisherman.PhaseText : string.Empty;
            var remaining = (status.NextCatchAtMs - status.NowMs) / 1000.0;
            var text = boxFull ? GameTexts.Box.FullShort : GameTexts.Fishing.StatusLine(phase, Format.Countdown(remaining));
            var textRect = new Rect(line.x, line.y, Mathf.Max(0f, baitRight - 16f - line.x), line.height);
            GUI.Label(textRect, FishCard.Fit(text, boxFull ? skin.SmallGold : skin.BodyBold, textRect.width), boxFull ? skin.SmallGold : skin.BodyBold);

            var stop = new Rect(panel.xMax - 16f - 40f, panel.yMax - 12f - 40f, 40f, 40f);
            skin.Bar(new Rect(panel.x + 16f, stop.center.y - 4f, stop.x - 16f - panel.x - 16f, 8f), (float)status.CycleProgress);
            if (skin.IconButton(stop, Icons.Stop, null, skin.Button))
            {
                _root.StopFishing();
            }

            if (GUI.enabled && panel.Contains(Event.current.mousePosition) && !stop.Contains(Event.current.mousePosition))
            {
                DrawFishingNote(skin, panel, status.CycleSeconds);
            }
        }

        /// <summary>
        /// The bait in use and its tentativas, right-aligned so it ends at <paramref name="rect"/>'s right edge; a click
        /// opens the Loja (docs/SISTEMA_SUCESSO_PESCA.md, section 12). Returns where it starts.
        /// </summary>
        private float DrawBaitCharges(UiSkin skin, Rect rect)
        {
            var gear = _root.Gear;
            if (gear == null || gear.BaitName == null)
            {
                return rect.xMax;
            }

            var text = Format.Number(gear.BaitChargesLeft);
            var w = skin.SmallGoldRight.CalcSize(new GUIContent(text)).x + 4f;
            var x = rect.xMax - w - 8f - 24f;
            skin.DrawIcon(new Rect(x, rect.y, 24f, 24f), Icons.Bait, UiSkin.Gold);
            GUI.Label(new Rect(rect.xMax - w, rect.y, w, rect.height), text, skin.SmallGoldRight);
            var hit = new Rect(x - 4f, rect.y - 4f, rect.xMax - x + 8f, rect.height + 8f);
            if (GUI.Button(hit, new GUIContent(string.Empty, GameTexts.Gear.OpenDetails), GUIStyle.none))
            {
                CloseAllWindows();
                _shop.Open();
            }

            return x;
        }

        /// <summary>
        /// The note over the fishing panel while the mouse is on it: the cycle ("1 tentativa a cada 12 s") and the chance
        /// of pulling out each rarity that bites here, as the service gives them.
        /// </summary>
        private void DrawFishingNote(UiSkin skin, Rect panel, double cycleSeconds)
        {
            var gear = _root.Gear;
            if (gear != null && !ReferenceEquals(gear, _chanceGear))
            {
                // A new gear view from the service: build the line once, not every frame.
                var parts = new System.Collections.Generic.List<string>();
                foreach (var chance in gear.Chances)
                {
                    if (chance.BitesHere)
                    {
                        parts.Add(GameTexts.Fishing.ChanceShort(chance.RarityName, Format.Percent(chance.Chance, 0)));
                    }
                }

                _chanceGear = gear;
                _chanceText = string.Join(" · ", parts);
            }

            var cycle = GameTexts.Fishing.CycleInfo(Format.Duration(cycleSeconds));
            var width = panel.width;
            var chanceHeight = string.IsNullOrEmpty(_chanceText) ? 0f : skin.Small.CalcHeight(new GUIContent(_chanceText), width - 32f);
            var tip = new Rect(panel.x, panel.y - 16f - (16f + UiSkin.SmallLine + chanceHeight + 16f), width, 16f + UiSkin.SmallLine + chanceHeight + 16f);
            skin.TipBox(tip);
            GUI.Label(new Rect(tip.x + 16f, tip.y + 14f, width - 32f, UiSkin.SmallLine), cycle, skin.SmallMuted);
            if (chanceHeight > 0f)
            {
                GUI.Label(new Rect(tip.x + 16f, tip.y + 14f + UiSkin.SmallLine, width - 32f, chanceHeight), _chanceText, skin.Small);
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

            var scale = UiSkin.CanvasScale(Screen.height);
            var point = camera.WorldToScreenPoint(rig.LastEscapePoint);
            var alpha = Mathf.Clamp01(age / 0.2f) * Mathf.Clamp01((duration - age) / 0.6f);
            var cx = point.x / scale;
            var cy = (Screen.height - point.y) / scale - 70f - age * 14f;
            var rare = rig.LastEscapeRarityId != null && rig.LastEscapeRarityId != "common";

            var previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            // Wide enough for the rare-escape line (about 610 px at the 15 px size, A-149) on one line.
            var panel = new Rect(cx - 340, cy - 8, 680, 64);
            skin.FloatingPanel(panel);
            if (rare)
            {
                GUI.DrawTexture(UiSkin.Snap(new Rect(panel.x + 10, panel.y + 12, 4, panel.height - 24)), skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.RarityColor(rig.LastEscapeRarityId), 0, 0);
            }

            GUI.Label(new Rect(panel.x, panel.y + 6, panel.width, 26), GameTexts.Fishing.EscapeMessage, skin.CenterBold);
            GUI.Label(new Rect(panel.x + 16, panel.y + 34, panel.width - 32, 22),
                rare && !string.IsNullOrEmpty(rig.LastEscapeRarityName) ? GameTexts.Fishing.EscapeRare(rig.LastEscapeRarityName) : GameTexts.Fishing.EscapeHint, skin.SmallMutedCenter);
            GUI.color = previous;
        }

        private Rect BoxButtonRect => new Rect(_width - BottomMargin - BoxButtonWidth, _height - BottomMargin - BoxButtonHeight, BoxButtonWidth, BoxButtonHeight);

        /// <summary>
        /// The Caixa de Pesca button (A-190): the box icon and "128/300" (or "Caixa cheia" in gold), on the same bottom
        /// line as the fishing panel; its name shows on hover.
        /// </summary>
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
            if (GUI.Button(rect, new GUIContent(string.Empty, GameTexts.Box.Open), skin.Button))
            {
                _box.Open();
            }

            // The 24 px icon at 2× (48), so it never lands between two scales.
            skin.DrawIcon(new Rect(rect.x + 12f, rect.center.y - 24f, 48f, 48f), Icons.Box, UiSkin.Gold);
            var textRect = new Rect(rect.x + 12f + 48f + 12f, rect.y, rect.width - 12f - 48f - 12f - 12f, rect.height);
            if (player.FishingBoxFull)
            {
                GUI.Label(new Rect(textRect.x, rect.center.y - 14f, textRect.width, 28f), FishCard.Fit(GameTexts.Box.FullTiny, skin.BodyBold, textRect.width), skin.SmallGold);
            }
            else
            {
                var count = player.FishingBoxCapacity > 0 ? GameTexts.Box.Ratio(player.FishingBoxCount, player.FishingBoxCapacity) : Format.Number(player.FishingBoxCount);
                GUI.Label(new Rect(textRect.x, rect.center.y - 14f, textRect.width, 28f), FishCard.Fit(count, skin.BodyBold, textRect.width), skin.BodyBold);
            }

            if (GUI.enabled && rect.Contains(Event.current.mousePosition))
            {
                HoverName(skin, rect, player.FishingBoxFull ? GameTexts.Box.FullShort : GameTexts.Box.Open, true);
            }
        }

        /// <summary>
        /// The name of a control without a label, in a small note while the mouse is over it (A-190): above
        /// <paramref name="target"/>, or left of it when <paramref name="above"/> is false.
        /// </summary>
        private static void HoverName(UiSkin skin, Rect target, string name, bool above)
        {
            var w = skin.SmallBold.CalcSize(new GUIContent(name)).x + 24f;
            var tip = above
                ? new Rect(target.xMax - w, target.y - 8f - 32f, w, 32f)
                : new Rect(target.x - 8f - w, target.center.y - 16f, w, 32f);
            skin.TipBox(tip);
            GUI.Label(new Rect(tip.x + 12f, tip.y + 4f, w - 16f, UiSkin.SmallLine), name, skin.SmallBold);
        }

        /// <summary>Level 2 of the visual intensity system: quick, non-blocking toasts with a coloured accent.</summary>
        private void DrawToasts(UiSkin skin)
        {
            var items = _root.Toasts.Items;
            // On the right under the strip, left of the round shortcuts (A-169); under the plaque when a narrow screen
            // puts them in line.
            var y = UnderBar;
            var left = _width - 20f - ShortcutColumnWidth - 16f - ToastWidth;

            if (_root.Celebrations.Active)
            {
                // The celebration banner spans the top centre: the column starts below it while it shows.
                y = Mathf.Max(y, CelebrationBottom);
            }

            // The column stops above the Fishing Box button and, when a narrow screen puts them in line,
            // above the fishing panel; the oldest toasts wait (they still expire on time).
            var bottom = BoxButtonRect.y - 12f;
            if (left < FishingPanelRect.xMax + 12f)
            {
                bottom = Mathf.Min(bottom, FishingPanelRect.y - 12f);
            }

            // With a window open, only the latest toast shows, in the strip under the window, left of the Fishing Box
            // button (never over it) and right of the tutorial hint; when both cannot fit, the toast waits (it stays in
            // the bell's history).
            var windowOpen = _root.WindowOpen;
            var last = windowOpen ? items.Count - 1 : 0;
            if (windowOpen)
            {
                y = _height - 12f - ToastHeight;
                left = BottomBandRight - ToastWidth;
                if (TutorialStripShown(true) && left - 12f - BottomBandLeft < BottomHintMinWidth)
                {
                    return;
                }
            }

            var shown = 0;
            for (var i = items.Count - 1; i >= last && i >= 0; i--)
            {
                if (!windowOpen && (y + ToastHeight > bottom || shown >= MaxToastsShown))
                {
                    break;
                }

                shown++;

                var toast = items[i];
                var age = Time.unscaledTime - toast.CreatedAt;
                var fadeIn = Mathf.Clamp01(age / 0.25f);
                var fadeOut = Mathf.Clamp01((toast.Duration - age) / 0.6f);
                var alpha = Mathf.Min(fadeIn, fadeOut);
                var accent = ToastAccent(toast.Kind);

                // Slides in from the right over the scene; from the left under a window (the Caixa button is at its right).
                var rect = new Rect(left + (1f - fadeIn) * (windowOpen ? -40f : 40f), y, ToastWidth, ToastHeight);
                var previous = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, alpha);
                skin.FloatingPanel(rect);
                GUI.DrawTexture(UiSkin.Snap(new Rect(rect.x + 10, rect.y + 12, 4, rect.height - 24)), skin.White, ScaleMode.StretchToFill, true, 0, accent, 0, 0);

                var textX = rect.x + 22;
                if (toast.Icon != null)
                {
                    PixelDraw.Fit(new Rect(rect.x + 16, rect.y + 9, 80, 44), toast.Icon);
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
            if (tutorial == null || !tutorial.Active || WelcomeBackPending)
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
            // strip (the round shortcuts and the toasts are on the right).
            var rect = windowOpen ? BottomHintRect() : new Rect(20, UnderBar, 340, 200);
            skin.DrawShadow(rect);
            GUI.Box(rect, GUIContent.none, skin.PanelSolid);
            skin.DrawOutline(rect, UiSkin.Accent);
            if (windowOpen)
            {
                GUI.Label(new Rect(rect.x + 18, rect.y + 8, rect.width - 324, 28), GameTexts.Tutorial.Title(tutorial.Step), skin.BodyBold);
                GUI.Label(new Rect(rect.x + 18, rect.y + 36, rect.width - 324, 48), GameTexts.Tutorial.Body(tutorial.Step), skin.Small);
            }
            else
            {
                GUI.contentColor = UiSkin.Accent;
                GUI.Label(new Rect(rect.x + 16, rect.y + 11, rect.width - 32, UiSkin.SmallLine), GameTexts.Tutorial.StepOf(tutorial.StepNumber, tutorial.StepCount), skin.SmallBold);
                GUI.contentColor = Color.white;
                GUI.Label(new Rect(rect.x + 16, rect.y + 32, rect.width - 32, 26), GameTexts.Tutorial.Title(tutorial.Step), skin.BodyBold);
                GUI.Label(new Rect(rect.x + 16, rect.y + 60, rect.width - 32, 130), GameTexts.Tutorial.Body(tutorial.Step), skin.Small);
            }

            // With a window open: "Entendi" (112) + 8 + "Pular tutorial" (152), ending 18 px before the edge.
            var buttons = windowOpen ? new Rect(rect.xMax - 290, rect.y + 20, 272, 36) : new Rect(rect.x + 16, rect.yMax - 46, 140, 32);
            if (tutorial.NeedsAcknowledge && GUI.Button(new Rect(buttons.x, buttons.y, 112, buttons.height), GameTexts.Tutorial.GotIt, skin.ButtonPrimary))
            {
                _root.AcknowledgeTutorial(tutorial.Step);
            }

            var skipRect = windowOpen ? new Rect(buttons.x + (tutorial.NeedsAcknowledge ? 120 : 0), rect.y + 20, tutorial.NeedsAcknowledge ? 152 : 272, 36)
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

        /// <summary>
        /// A quiet note at the bottom left while the PC clock is behind the saved game time (the service's signal,
        /// TD-030/TD-052): the game stays still until the clock catches up, so the player knows why nothing moves.
        /// </summary>
        private void DrawClockFrozen(UiSkin skin)
        {
            var session = _root.Game?.Session;
            if (session == null || !session.ClockFrozen)
            {
                return;
            }

            var rect = new Rect(20f, _height - 24f - 30f - 20f - ClockNoteHeight, ClockNoteWidth, ClockNoteHeight);
            if (rect.xMax > FishingPanelRect.x - 12f)
            {
                rect.width = Mathf.Max(300f, FishingPanelRect.x - 12f - rect.x);
            }

            skin.FloatingPanel(rect);
            GUI.DrawTexture(UiSkin.Snap(new Rect(rect.x + 10, rect.y + 12, 4, rect.height - 24)), skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.Gold, 0, 0);
            skin.DrawIcon(new Rect(rect.x + 20, rect.y + 14, 22, 22), Icons.Clock, UiSkin.Gold);
            GUI.Label(new Rect(rect.x + 52, rect.y + 10, rect.width - 64, 22), GameTexts.Hud.ClockFrozenTitle, skin.SmallBold);
            GUI.Label(new Rect(rect.x + 52, rect.y + 32, rect.width - 64, rect.height - 38), GameTexts.Hud.ClockFrozenBody(Format.Duration(session.ClockFrozenSeconds)), skin.Small);
        }

        /// <summary>Free strip under an open window: from the test tools' button (or the edge) to the Fishing Box button.</summary>
        private float BottomBandLeft => _root.DevToolsEnabled ? DevButtonRight + 12f : 20f;

        private float BottomBandRight => BoxButtonRect.x - 12f;

        /// <summary>True when the tutorial hint shows as a strip under an open window this frame.</summary>
        private bool TutorialStripShown(bool windowOpen)
        {
            var tutorial = _root.Tutorial;
            return windowOpen && tutorial != null && tutorial.Active && !WelcomeBackPending && tutorial.Step != TutorialSteps.Welcome;
        }

        /// <summary>
        /// The tutorial hint under an open window: centred when it fits, inside the free strip and left of the latest
        /// toast when one shows (DrawToasts uses the same rule).
        /// </summary>
        private Rect BottomHintRect()
        {
            var left = BottomBandLeft;
            var right = BottomBandRight;
            if (_root.Toasts.Items.Count > 0 && right - ToastWidth - 12f - left >= BottomHintMinWidth)
            {
                right -= ToastWidth + 12f;
            }

            var width = Mathf.Min(BottomHintMaxWidth, right - left);
            var x = Mathf.Clamp(_width / 2f - width / 2f, left, right - width);
            return new Rect(x, _height - 88f, width, 76f);
        }

        /// <summary>A pulsing gold outline around what the current step asks the player to use.</summary>
        private void DrawTutorialHighlight(string step)
        {
            Rect target;
            switch (step)
            {
                // The tabs of the top menu (A-169): Comércio (it opens the Loja during this step), Perfil, Aventura.
                case TutorialSteps.ClaimRod: target = TabRect(GroupTrade); break;
                case TutorialSteps.Cardume: target = TabRect(GroupProfile); break;
                case TutorialSteps.Expedition: target = TabRect(GroupAdventure); break;
                case TutorialSteps.StartFishing: target = StartButtonRect; break;
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

        private Rect TabRect(int group)
        {
            return _tabRects != null ? _tabRects[group] : new Rect(_width / 2f, TabTop, 0f, TabHeight);
        }

        // ------------------------------------------------------------------ notifications and settings

        private Rect NotificationsPanelRect => new Rect(_width - 24 - 470, BarHeight + 8f, 470, 560);

        private Rect SettingsPanelRect => new Rect(_width - 24 - 380, BarHeight + 8f, 380, 340);

        private void DrawPanels(UiSkin skin)
        {
            // A click on an open panel stays on it (and a click outside closes it), so nothing under it reacts.
            var e = Event.current;
            if ((_showNotifications || _showSettings) && e.type == EventType.MouseDown)
            {
                var over = _showNotifications ? NotificationsPanelRect : SettingsPanelRect;
                var bar = new Rect(0, 0, _width, BarHeight);
                if (!over.Contains(e.mousePosition) && !bar.Contains(e.mousePosition))
                {
                    _showNotifications = false;
                    _showSettings = false;
                    e.Use();
                    return;
                }
            }

            if (_showNotifications)
            {
                var panel = NotificationsPanelRect;
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
                        PixelDraw.Fit(new Rect(panel.x + 16, y + 6, 60, 34), entry.Icon);
                        textX = panel.x + 86;
                    }

                    GUI.Label(new Rect(textX, y, 60, UiSkin.SmallLine), Format.Time(entry.At), skin.SmallMuted);
                    GUI.Label(new Rect(textX, y + 18, panel.xMax - textX - 16, 42), entry.Text, entry.Kind == ToastKind.Warning ? skin.SmallGold : skin.Small);
                    y += 58;
                }
            }

            if (_showSettings)
            {
                var panel = SettingsPanelRect;
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

                GUI.Label(new Rect(x, y + 46, w, 66), GameTexts.Hud.CompactNote, skin.SmallMuted);
            }

            // Whatever the panel's own controls did not take (a click on its empty paper) ends here.
            if (_showNotifications && e.type == EventType.MouseDown && NotificationsPanelRect.Contains(e.mousePosition))
            {
                e.Use();
            }
            else if (_showSettings && e.type == EventType.MouseDown && SettingsPanelRect.Contains(e.mousePosition))
            {
                e.Use();
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

            if (GUI.Button(new Rect(width - 120, 8, 112, 30), GameTexts.Hud.Expand, skin.Chip))
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
            skin.LineBox(bar, new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.85f), UiSkin.FrameDark, 2);
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

        /// <summary>The error screen without the game's skin (when the skin itself could not be built).</summary>
        private void DrawPlainError(Exception exception)
        {
            GUILayout.BeginArea(new Rect(40, 40, Mathf.Max(200f, _width - 80), _height - 80));
            GUILayout.Label(_root.StartupErrorTitle ?? GameTexts.Startup.UnexpectedErrorTitle);
            GUILayout.Label(GameTexts.Startup.UnexpectedErrorHint);
            foreach (var error in _root.StartupErrors)
            {
                GUILayout.Label("•  " + error);
            }

            if (_root.StartupErrors.Count == 0)
            {
                GUILayout.Label("•  " + GameTexts.Startup.ErrorLine(exception.GetType().Name, exception.Message));
            }

            GUILayout.EndArea();
        }
    }
}
