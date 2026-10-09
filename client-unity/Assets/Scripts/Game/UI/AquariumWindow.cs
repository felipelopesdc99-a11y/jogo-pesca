using System;
using System.Collections.Generic;
using System.Linq;
using FishingIdle.Game.Bootstrap;
using FishingIdle.Game.Scene;
using FishingIdle.Game.Visual;
using FishingIdle.GameService.Aquarium;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The Aquarium (GDD sections 12, 13, 22) as a living tank (addendum A-144): the kept fish swim in
    /// the water, a drawer below holds the miniatures of every fish, and the fish sheet (or the
    /// several-fish sale) sits on a glass panel on the right. Feeding and selling are requests the
    /// service validates; everything shown comes from the game service's views. The swimming is only
    /// drawing: positions follow the clock and a path fixed by each fish's id. A switch in the header turns
    /// it into the card mode (addendum A-145): a list of compact cards and the hero sheet of one fish.
    /// </summary>
    public sealed class AquariumWindow
    {
        // Food picker grid (feeding keeps the card grid).
        private const float CardWidth = 196f;
        private const float CardHeight = 196f;
        private const float Gap = 12f;

        // The tank and its drawer.
        private const int MaxSwimming = 24;
        private const float TankRadius = 14f;
        private const float MinSwimHeight = 220f;
        private const float DrawerHeader = 38f;
        private const float TileWidth = 92f;
        private const float TileHeight = 104f;
        private const float TileGap = 8f;

        // Final art, when it exists (Resources/Arte/UI). ASSET_PENDENTE: until then the tank, the sand,
        // the bubbles and the glass are drawn here (docs/ASSETS_PENDENTES.md, A-144).
        private const string TankBackgroundArt = "UI/ui_aquarium_bg";
        private const string TankSandArt = "UI/ui_aquarium_sand";
        private const string BubbleArt = "UI/ui_aquarium_bubble";
        private const string GlassArt = "UI/ui_aquarium_glass";
        private const float GlassArtBorder = 32f;

        private static readonly AquariumSort[] Sorts = { AquariumSort.Size, AquariumSort.Level, AquariumSort.Species, AquariumSort.Newest };

        // Plants that already exist (Arte/Vivos/Plantas): x across the tank, height as a share of the water.
        private static readonly (string Path, float X, float Height)[] Plants =
        {
            ("Vivos/Plantas/junco_01", 0.02f, 0.50f),
            ("Vivos/Plantas/junco_03", 0.09f, 0.34f),
            ("Vivos/Plantas/sargaco_02", 0.33f, 0.15f),
            ("Vivos/Plantas/junco_02", 0.60f, 0.42f),
            ("Vivos/Plantas/sargaco_04", 0.74f, 0.12f),
            ("Vivos/Plantas/junco_04", 0.93f, 0.56f),
            ("Vivos/Plantas/junco_06", 0.99f, 0.36f),
        };

        // Provisional pebbles on the sand: x across the tank, width, height, shade.
        private static readonly Vector4[] Pebbles =
        {
            new Vector4(0.06f, 30f, 13f, 0.2f), new Vector4(0.15f, 18f, 9f, 0.6f), new Vector4(0.27f, 24f, 11f, 0.4f),
            new Vector4(0.44f, 34f, 14f, 0.1f), new Vector4(0.52f, 16f, 8f, 0.7f), new Vector4(0.68f, 26f, 12f, 0.3f),
            new Vector4(0.81f, 20f, 10f, 0.5f), new Vector4(0.90f, 32f, 14f, 0.25f),
        };

        // Bubble columns: x across the water, rise speed (px/s).
        private static readonly Vector2[] BubbleColumns = { new Vector2(0.17f, 38f), new Vector2(0.55f, 30f), new Vector2(0.84f, 44f) };

        private static Texture2D _waterTexture;
        private static Texture2D _raysTexture;
        private static Texture2D _sandTexture;

        private readonly GameRoot _root;
        private AquariumView _aquarium;
        private AquariumSort _sort = AquariumSort.Size;
        private long _selectedId;
        private FishView _selected;
        private Vector2 _scroll;
        private bool _dirty = true;

        // The living tank (A-144)
        private bool _drawerOpen = true;
        private readonly Dictionary<long, float> _swimPhase = new Dictionary<long, float>();
        private readonly List<SwimSlot> _slots = new List<SwimSlot>();
        private readonly Dictionary<long, (string Name, string Line)> _tileText = new Dictionary<long, (string Name, string Line)>();
        private float _lastSwimTime = -1f;
        private float _logMinCm;
        private float _logMaxCm;

        private struct SwimSlot
        {
            public FishView Fish;
            public Rect Rect;
            public bool Flip;
            public float Tilt;
        }

        // Feeding
        private bool _feeding;
        private bool _feedFromAquarium;
        private string _search = string.Empty;
        private readonly HashSet<long> _foodBox = new HashSet<long>();
        private readonly HashSet<long> _foodFish = new HashSet<long>();
        private IReadOnlyList<CatchView> _box = new List<CatchView>();
        private ServiceResult<FeedPreview> _feedPreview;
        private Vector2 _feedScroll;

        // Selling several fish at once (A-130)
        private bool _multi;
        private readonly HashSet<long> _sellSet = new HashSet<long>();
        private SalePreview _salePreview;
        private List<FishView> _shown = new List<FishView>();
        private Vector2 _sellScroll;

        // Card mode, the hero sheet (A-145): the choice is kept on this PC (pure presentation).
        private const string VisualPrefsKey = "fishingidle.aquario.visual";
        private const float CardRowHeight = 66f;
        private const float CardRowGap = 6f;
        private bool _cardsMode;
        private Vector2 _cardsScroll;
        private bool _cardsFollow;
        private long _heroShownId;
        private float _heroChangedAt = -10f;
        private readonly Dictionary<long, (float Width, string Name, string Line)> _cardText = new Dictionary<long, (float Width, string Name, string Line)>();

        // The strongest value of each attribute among the kept fish (Vida, Ataque, Defesa, Velocidade): only the scale
        // of the hero sheet's bars; the numbers shown are the service's.
        private readonly double[] _statMax = new double[4];

        // Confirmations
        private bool _confirmSell;
        private bool _confirmSellMany;
        private bool _confirmFeed;

        public AquariumWindow(GameRoot root)
        {
            _root = root;
            _root.AquariumChanged += () => _dirty = true;
            _root.BoxChanged += () => _dirty = true;
            _cardsMode = PlayerPrefs.GetInt(VisualPrefsKey, 0) == 1;
        }

        public bool IsOpen { get; private set; }

        private bool DialogOpen => _confirmSell || _confirmSellMany || _confirmFeed;

        /// <summary>True while a confirmation (sell one, sell several, feed) is open over the window.</summary>
        public bool HasDialog => DialogOpen;

        public void Open()
        {
            IsOpen = true;
            _dirty = true;
            _search = string.Empty;
        }

        /// <summary>Closes the innermost thing first: a dialog, then feeding, then the window.</summary>
        public void Close()
        {
            if (DialogOpen)
            {
                _confirmSell = false;
                _confirmSellMany = false;
                _confirmFeed = false;
                return;
            }

            if (_multi)
            {
                StopMulti();
                return;
            }

            if (_feeding)
            {
                StopFeeding();
                return;
            }

            IsOpen = false;
        }

        public void Draw(UiSkin skin, float screenWidth, float screenHeight)
        {
            if (!IsOpen)
            {
                return;
            }

            if (_dirty)
            {
                Reload();
            }

            GUI.enabled = !DialogOpen;
            var panel = WindowFrame.Panel(skin, screenWidth, screenHeight, 1320f, 840f);
            // A-148: the fish count is a counter, next to the sign; the search (300 + 16) and, outside feeding, the
            // Tanque | Cartas toggle (~232 + 12) keep their place.
            if (WindowFrame.Header(skin, panel, GameTexts.Aquarium.Title, null, Icons.Aquarium,
                    GameTexts.Aquarium.Count(_aquarium.Count, _aquarium.Capacity), _feeding ? 316f : 560f))
            {
                Close();
            }

            // Search by name, next to Fechar (the same place in the Fishing Box).
            var search = NameSearch.Field(skin, new Rect(panel.xMax - 156 - 16 - 300, panel.y + 24, 300, 38), _search, "busca_aquario");
            if (search != _search)
            {
                _search = search;
                _scroll = Vector2.zero;
                _cardsScroll = Vector2.zero;
                _feedScroll = Vector2.zero;
                RefreshShown();
            }

            // Tanque | Cartas, just left of the search (A-145).
            if (!_feeding)
            {
                DrawVisualToggle(skin, panel.xMax - 156 - 16 - 300 - 12, panel.y + 24);
            }

            var detailWidth = 420f;
            var left = new Rect(panel.x + 28, panel.y + 80, panel.width - detailWidth - 72, panel.height - 100);
            var right = new Rect(panel.xMax - detailWidth - 24, panel.y + 80, detailWidth, panel.height - 100);

            if (_feeding)
            {
                DrawFoodPicker(skin, left);
                DrawFeedPanel(skin, right);
            }
            else
            {
                DrawBrowser(skin, left, right);
            }

            GUI.enabled = true;
            if (_confirmSell)
            {
                DrawSellDialog(skin, screenWidth, screenHeight);
            }
            else if (_confirmSellMany)
            {
                DrawSellManyDialog(skin, screenWidth, screenHeight);
            }
            else if (_confirmFeed)
            {
                DrawFeedDialog(skin, screenWidth, screenHeight);
            }
        }

        // ------------------------------------------------------------------ browsing

        private void DrawBrowser(UiSkin skin, Rect area, Rect right)
        {
            // "Selecionar vários" sits on the right of the sort row. Measured first: on a narrow panel it
            // becomes icon-only, and if even that would touch the sort chips it goes to the next line.
            var multiLabel = new GUIContent(GameTexts.Aquarium.MultiSelect);
            var multiWidth = skin.Chip.CalcSize(multiLabel).x + 34;
            var chipsEnd = area.x + 100;
            foreach (var sort in Sorts)
            {
                chipsEnd += skin.Chip.CalcSize(new GUIContent(SortName(sort))).x + 8 + 8;
            }

            var multiIconOnly = chipsEnd > area.xMax - multiWidth - 8;
            if (multiIconOnly)
            {
                multiWidth = 40f;
            }

            var multiOwnLine = chipsEnd > area.xMax - multiWidth - 8;
            var multiRect = new Rect(area.xMax - multiWidth, multiOwnLine ? area.y + 40 : area.y, multiWidth, 32);
            var tankTop = multiOwnLine ? 86f : 46f;

            // Sort chips
            var x = area.x;
            skin.DrawIcon(new Rect(x, area.y + 7, 18, 18), Icons.Sort, UiSkin.Muted);
            GUI.Label(new Rect(x + 24, area.y + 7, 80, 24), GameTexts.Aquarium.SortLabel, skin.SmallMuted);
            x += 100;
            foreach (var sort in Sorts)
            {
                var label = new GUIContent(SortName(sort));
                var w = skin.Chip.CalcSize(label).x + 8;
                if (GUI.Button(new Rect(x, area.y, w, 32), label, sort == _sort ? skin.ChipActive : skin.Chip))
                {
                    _sort = sort;
                    _dirty = true;
                }

                x += w + 8;
            }

            // "Selecionar vários": turns clicks into a selection to sell.
            if (_aquarium.Count > 0 && skin.IconButton(multiRect, Icons.Check, multiIconOnly ? null : GameTexts.Aquarium.MultiSelect, _multi ? skin.ChipActive : skin.Chip))
            {
                if (_multi)
                {
                    StopMulti();
                }
                else
                {
                    StartMulti();
                }
            }

            // The tank fills the rest of the window; the sheet is a glass panel inside it, on the right.
            var tank = new Rect(area.x, area.y + tankTop, right.xMax - area.x, area.height - tankTop);
            if (_cardsMode)
            {
                DrawCards(skin, tank);
                return;
            }

            var glass = new Rect(right.x, tank.y + 10f, right.width - 10f, tank.height - 20f);
            DrawTank(skin, tank, glass.x);

            if (_multi)
            {
                DrawSelectionPanel(skin, glass);
            }
            else
            {
                DrawSheet(skin, glass);
            }
        }

        // ------------------------------------------------------------------ the living tank (A-144)

        private void DrawTank(UiSkin skin, Rect tank, float glassX)
        {
            var lx = tank.x + 12f;
            var lw = glassX - 12f - lx;
            var drawerHeight = DrawerHeight(tank.height);
            var drawer = new Rect(lx, tank.yMax - 12f - drawerHeight, lw, drawerHeight);
            var floorY = drawer.y - 6f;
            var swim = new Rect(lx, tank.y + 14f, lw, Mathf.Max(40f, floorY - 24f - tank.y));
            var t = Time.unscaledTime;

            DrawWater(skin, tank, floorY, t);
            DrawPlants(swim, floorY, t);
            DrawBubbles(skin, swim, floorY, t);

            if (_aquarium.Count == 0)
            {
                TankMessage(skin, swim, GameTexts.Aquarium.Empty);
            }
            else if (_shown.Count == 0)
            {
                TankMessage(skin, swim, GameTexts.Search.NoMatch);
            }
            else
            {
                DrawSwimmers(skin, swim, t);
            }

            // The glass rim of the tank.
            GUI.DrawTexture(tank, skin.White, ScaleMode.StretchToFill, true, 0, new Color(UiSkin.Border.r, UiSkin.Border.g, UiSkin.Border.b, 0.95f), 1.5f, TankRadius);
            GUI.DrawTexture(new Rect(tank.x + TankRadius, tank.y + 2f, tank.width - TankRadius * 2f, 1f), skin.White, ScaleMode.StretchToFill, true, 0, new Color(1f, 1f, 1f, 0.16f), 0, 0);

            DrawDrawer(skin, drawer);
        }

        /// <summary>The drawer: only its bar when closed; one row of miniatures, or up to three while selecting several.</summary>
        private float DrawerHeight(float tankHeight)
        {
            if (!_drawerOpen)
            {
                return DrawerHeader;
            }

            var rowHeight = TileHeight + TileGap;
            var room = tankHeight - 24f - MinSwimHeight - DrawerHeader - 8f;
            var rows = Mathf.Clamp(Mathf.FloorToInt(room / rowHeight), 1, _multi ? 3 : 1);
            return DrawerHeader + rows * rowHeight + 4f;
        }

        private static void DrawWater(UiSkin skin, Rect tank, float floorY, float t)
        {
            var background = ArtAssets.Texture(TankBackgroundArt);
            if (background != null)
            {
                GUI.DrawTexture(tank, background, ScaleMode.ScaleAndCrop, true, 0, Color.white, 0, TankRadius);
            }
            else
            {
                // ASSET_PENDENTE: ui_aquarium_bg.png (water and light rays, 1920×1080) replaces this gradient and the rays.
                GUI.DrawTexture(tank, WaterTexture, ScaleMode.StretchToFill, true, 0, Color.white, 0, TankRadius);
                var pulse = 0.55f + 0.15f * Mathf.Sin(t * 0.35f);
                GUI.DrawTexture(new Rect(tank.x, tank.y, tank.width, (floorY - tank.y) * 0.9f), RaysTexture, ScaleMode.StretchToFill, true, 0,
                    new Color(0.82f, 0.96f, 1f, pulse), 0, TankRadius);
            }

            var bottom = new Vector4(0f, 0f, TankRadius, TankRadius);
            var sandArt = ArtAssets.Texture(TankSandArt);
            if (sandArt != null)
            {
                GUI.DrawTexture(new Rect(tank.x, floorY - 48f, tank.width, tank.yMax - floorY + 48f), sandArt, ScaleMode.StretchToFill, true, 0, Color.white, Vector4.zero, bottom);
                return;
            }

            // ASSET_PENDENTE: ui_aquarium_sand.png (sand strip with stones) replaces the drawn sand, dunes and pebbles.
            var top = SandTop;
            for (var i = 0; i < 5; i++)
            {
                var dx = Mathf.Max(tank.x, tank.x + i * tank.width / 5f - 40f);
                var dw = Mathf.Min(tank.xMax, tank.x + (i + 1) * tank.width / 5f + 40f) - dx;
                GUI.DrawTexture(new Rect(dx, floorY - 36f + (i % 2) * 7f, dw, 30f), skin.White, ScaleMode.StretchToFill, true, 0, top, 0, 15f);
            }

            GUI.DrawTexture(new Rect(tank.x, floorY - 26f, tank.width, tank.yMax - floorY + 26f), SandTexture, ScaleMode.StretchToFill, true, 0, Color.white, Vector4.zero, bottom);
            foreach (var p in Pebbles)
            {
                var px = Mathf.Clamp(tank.x + p.x * tank.width, tank.x + 8f, tank.xMax - 8f - p.y);
                var colour = Color.Lerp(new Color(0.36f, 0.40f, 0.44f), new Color(0.55f, 0.50f, 0.42f), p.w);
                GUI.DrawTexture(new Rect(px, floorY - p.z * 0.5f - 6f, p.y, p.z), skin.White, ScaleMode.StretchToFill, true, 0, colour, 0, p.z * 0.5f);
            }
        }

        private static void DrawPlants(Rect swim, float floorY, float t)
        {
            for (var i = 0; i < Plants.Length; i++)
            {
                var plant = Plants[i];
                var tex = ArtAssets.Texture(plant.Path);
                if (tex == null)
                {
                    continue;
                }

                var h = swim.height * plant.Height;
                var w = h * tex.width / tex.height;
                var px = Mathf.Clamp(swim.x + plant.X * swim.width - w / 2f, swim.x, swim.xMax - w);
                var rect = new Rect(px, floorY + 4f - h, w, h);
                var sway = Mathf.Sin(t * 0.6f + i * 1.7f) * 2f;
                DrawTransformed(rect, tex, new Vector2(rect.center.x, rect.yMax), sway, false, Color.white);
            }
        }

        private static void DrawBubbles(UiSkin skin, Rect swim, float floorY, float t)
        {
            const int perColumn = 6;
            var bubble = ArtAssets.Texture(BubbleArt);
            var start = floorY - 8f;
            var rise = start - swim.y;
            if (rise <= 20f)
            {
                return;
            }

            for (var c = 0; c < BubbleColumns.Length; c++)
            {
                var column = BubbleColumns[c];
                var cx = swim.x + column.x * swim.width;
                for (var k = 0; k < perColumn; k++)
                {
                    var p = Mathf.Repeat(t * column.y / rise + k / (float)perColumn + c * 0.37f, 1f);
                    var fade = Mathf.Clamp01(p * 8f) * Mathf.Clamp01((1f - p) * 6f);
                    var size = (5f + 4f * Mathf.Repeat(k * 0.618f + c * 0.3f, 1f)) * (0.7f + 0.3f * p);
                    var bx = cx + Mathf.Sin(t * 1.6f + k * 1.3f + c) * 5f;
                    var r = new Rect(bx - size / 2f, start - p * rise - size / 2f, size, size);
                    if (bubble != null)
                    {
                        GUI.DrawTexture(r, bubble, ScaleMode.ScaleToFit, true, 0, new Color(1f, 1f, 1f, 0.8f * fade), 0, 0);
                        continue;
                    }

                    // ASSET_PENDENTE: ui_aquarium_bubble.png replaces this drawn ring.
                    GUI.DrawTexture(r, skin.White, ScaleMode.StretchToFill, true, 0, new Color(0.85f, 0.97f, 1f, 0.45f * fade), 1.2f, size / 2f);
                    GUI.DrawTexture(new Rect(r.x + size * 0.25f, r.y + size * 0.2f, size * 0.25f, size * 0.25f), skin.White, ScaleMode.StretchToFill, true, 0,
                        new Color(1f, 1f, 1f, 0.55f * fade), 0, size * 0.125f);
                }
            }
        }

        private static void TankMessage(UiSkin skin, Rect swim, string text)
        {
            var w = Mathf.Min(swim.width - 40f, 520f);
            var box = new Rect(swim.center.x - w / 2f, swim.center.y - 44f, w, 88f);
            Glass(skin, box);
            GUI.Label(new Rect(box.x + 20f, box.y + 8f, box.width - 40f, box.height - 16f), text, skin.Center);
        }

        /// <summary>
        /// The first fish of the current order and search swim (at most <see cref="MaxSwimming"/>). Each one
        /// goes back and forth on a path fixed by its id; the clock only moves it along that path.
        /// </summary>
        private void DrawSwimmers(UiSkin skin, Rect swim, float t)
        {
            var count = Mathf.Min(MaxSwimming, _shown.Count);
            var e = Event.current;

            // Advance along the path once per frame (the repaint); the selected fish goes slower.
            if (e.type == EventType.Repaint)
            {
                var dt = _lastSwimTime < 0f ? 0f : Mathf.Clamp(t - _lastSwimTime, 0f, 0.1f);
                _lastSwimTime = t;
                for (var i = 0; i < count; i++)
                {
                    var id = _shown[i].FishId;
                    var rate = 1f / Mathf.Lerp(22f, 40f, Hash01(id, 2));
                    if (id == _selectedId && !_multi)
                    {
                        rate *= 0.3f;
                    }

                    _swimPhase[id] = Mathf.Repeat(Phase(id) + dt * rate, 1f);
                }
            }

            _slots.Clear();
            var scale = Mathf.Clamp(swim.height / 460f, 0.65f, 1.05f);
            var selectedIndex = -1;
            for (var i = 0; i < count; i++)
            {
                var f = _shown[i];
                var id = f.FishId;
                var sizeT = _logMaxCm > _logMinCm ? Mathf.InverseLerp(_logMinCm, _logMaxCm, LogCm(f)) : 0.5f;
                var w = Mathf.Min(Mathf.Lerp(74f, 170f, sizeT) * scale, swim.width * 0.3f);
                var h = w * 0.5f;
                var span = Mathf.Max(0f, swim.width - w);
                var own = span * Mathf.Lerp(0.55f, 1f, Hash01(id, 4));
                var x0 = swim.x + (span - own) * Hash01(id, 5);
                var angle = Phase(id) * Mathf.PI * 2f;
                var x = x0 + (0.5f - 0.5f * Mathf.Cos(angle)) * own;
                var laneRoom = Mathf.Max(0f, swim.height - h - 20f);
                var wave = t * Mathf.Lerp(0.5f, 0.9f, Hash01(id, 6)) + Hash01(id, 7) * 6.283f;
                var drift = Mathf.Sin(angle * 2f + Hash01(id, 8) * 6.283f) * Mathf.Min(14f, laneRoom * 0.1f);
                var y = Mathf.Clamp(swim.y + 10f + Hash01(id, 1) * laneRoom + Mathf.Sin(wave) * 7f + drift, swim.y, swim.yMax - h);
                if (id == _selectedId)
                {
                    selectedIndex = _slots.Count;
                }

                _slots.Add(new SwimSlot { Fish = f, Rect = new Rect(x, y, w, h), Flip = Mathf.Sin(angle) < 0f, Tilt = Mathf.Cos(wave) * 3f });
            }

            // A click picks the fish on top (the selected one is drawn last, so it is on top).
            if (e.type == EventType.MouseDown && e.button == 0 && GUI.enabled && swim.Contains(e.mousePosition))
            {
                var hit = -1;
                if (selectedIndex >= 0 && HitArea(_slots[selectedIndex].Rect).Contains(e.mousePosition))
                {
                    hit = selectedIndex;
                }

                for (var i = _slots.Count - 1; i >= 0 && hit < 0; i--)
                {
                    if (i != selectedIndex && HitArea(_slots[i].Rect).Contains(e.mousePosition))
                    {
                        hit = i;
                    }
                }

                if (hit >= 0)
                {
                    OnFishClicked(_slots[hit].Fish, e.control || e.command);
                    e.Use();
                }
            }

            for (var i = 0; i < _slots.Count; i++)
            {
                if (i != selectedIndex)
                {
                    DrawSwimmer(skin, _slots[i], swim, t, false);
                }
            }

            if (selectedIndex >= 0)
            {
                DrawSwimmer(skin, _slots[selectedIndex], swim, t, !_multi);
            }
        }

        private void DrawSwimmer(UiSkin skin, SwimSlot slot, Rect swim, float t, bool selected)
        {
            var f = slot.Fish;
            var r = slot.Rect;
            var core = new Rect(r.x + r.width * 0.25f, r.y + r.height * 0.25f, r.width * 0.5f, r.height * 0.5f);

            // Discreet glow (Art Bible): the slow gold one for Excepcional/Perfeição, a faint one in the rarity colour otherwise.
            var breath = Mathf.Sin(t * 0.9f + (f.FishId % 7));
            if (VisualTheme.IsSpecialSize(f.SizeCategoryId))
            {
                skin.DrawGlow(core, UiSkin.SizeColor(f.SizeCategoryId), 0.24f + 0.08f * breath);
            }
            else if (f.IsImportant)
            {
                skin.DrawGlow(core, UiSkin.RarityColor(f.RarityId), 0.12f + 0.04f * breath);
            }

            if (selected)
            {
                var ring = new Rect(r.x - 8f, r.y - 4f, r.width + 16f, r.height + 8f);
                GUI.DrawTexture(ring, skin.White, ScaleMode.StretchToFill, true, 0, new Color(UiSkin.Accent.r, UiSkin.Accent.g, UiSkin.Accent.b, 0.10f), 0, ring.height / 2f);
                GUI.DrawTexture(ring, skin.White, ScaleMode.StretchToFill, true, 0, new Color(UiSkin.Accent.r, UiSkin.Accent.g, UiSkin.Accent.b, 0.9f), 2f, ring.height / 2f);
            }

            DrawTransformed(r, Art.FishTexture(f.SpeciesId), r.center, slot.Tilt, slot.Flip, Color.white);

            if (_multi && _sellSet.Contains(f.FishId))
            {
                CheckBadge(skin, new Rect(r.xMax - 14f, r.y - 6f, 22f, 22f));
            }

            if (!selected)
            {
                return;
            }

            // The name tag that swims with the selected fish (above it, or below near the surface).
            var tag = TileText(skin, f).Name + " · " + GameTexts.Player.LevelShort + " " + f.Level;
            var tw = Mathf.Min(260f, skin.ChipText.CalcSize(new GUIContent(tag)).x + 20f);
            var ty = r.y - 34f < swim.y ? r.yMax + 8f : r.y - 34f;
            var tagRect = new Rect(Mathf.Clamp(r.center.x - tw / 2f, swim.x, swim.xMax - tw), ty, tw, 24f);
            GUI.DrawTexture(tagRect, skin.White, ScaleMode.StretchToFill, true, 0, new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.86f), 0, 12f);
            GUI.DrawTexture(tagRect, skin.White, ScaleMode.StretchToFill, true, 0, new Color(UiSkin.Accent.r, UiSkin.Accent.g, UiSkin.Accent.b, 0.6f), 1f, 12f);
            GUI.Label(tagRect, UI.FishCard.Fit(tag, skin.ChipText, tw - 14f), skin.ChipText);
        }

        private static Rect HitArea(Rect r) => new Rect(r.x + r.width * 0.1f, r.y + r.height * 0.15f, r.width * 0.8f, r.height * 0.7f);

        private void DrawDrawer(UiSkin skin, Rect drawer)
        {
            Glass(skin, drawer);

            // Bar: title, how many swim, and Recolher / Mostrar todos.
            var toggle = new GUIContent(_drawerOpen ? GameTexts.Aquarium.DrawerHide : GameTexts.Aquarium.DrawerShow);
            var bw = skin.Chip.CalcSize(toggle).x + 12f;
            var toggleRect = new Rect(drawer.xMax - 10f - bw, drawer.y + 5f, bw, 28f);
            if (GUI.Button(toggleRect, toggle, skin.Chip))
            {
                _drawerOpen = !_drawerOpen;
            }

            var tx = drawer.x + 16f;
            var room = toggleRect.x - 12f - tx;
            var title = GameTexts.Aquarium.DrawerTitle(_shown.Count);
            var titleWidth = Mathf.Min(room, skin.BodyBold.CalcSize(new GUIContent(title)).x + 4f);
            GUI.Label(new Rect(tx, drawer.y + 9f, titleWidth, 22f), UI.FishCard.Fit(title, skin.BodyBold, titleWidth), skin.BodyBold);
            if (_shown.Count > MaxSwimming)
            {
                var noteX = tx + titleWidth + 14f;
                var noteWidth = toggleRect.x - 12f - noteX;
                if (noteWidth > 40f)
                {
                    GUI.Label(new Rect(noteX, drawer.y + 11f, noteWidth, 20f),
                        UI.FishCard.Fit(GameTexts.Aquarium.SwimmingNote(MaxSwimming, _shown.Count), skin.SmallMuted, noteWidth), skin.SmallMuted);
                }
            }

            if (!_drawerOpen || _shown.Count == 0)
            {
                return;
            }

            var grid = new Rect(drawer.x + 10f, drawer.y + DrawerHeader, drawer.width - 14f, drawer.height - DrawerHeader - 4f);
            var contentWidth = grid.width - 18f;
            var columns = Mathf.Max(1, Mathf.FloorToInt((contentWidth + TileGap) / (TileWidth + TileGap)));
            var rows = (_shown.Count + columns - 1) / columns;
            var rowHeight = TileHeight + TileGap;

            _scroll = GUI.BeginScrollView(grid, _scroll, new Rect(0, 0, contentWidth, rows * rowHeight));
            var firstRow = Mathf.Max(0, Mathf.FloorToInt(_scroll.y / rowHeight));
            var lastRow = Mathf.Min(rows - 1, Mathf.CeilToInt((_scroll.y + grid.height) / rowHeight));
            for (var row = firstRow; row <= lastRow; row++)
            {
                for (var col = 0; col < columns; col++)
                {
                    var index = row * columns + col;
                    if (index >= _shown.Count)
                    {
                        break;
                    }

                    DrawTile(skin, new Rect(col * (TileWidth + TileGap), row * rowHeight, TileWidth, TileHeight), _shown[index]);
                }
            }

            GUI.EndScrollView();
        }

        /// <summary>A round miniature in the drawer: rarity ring, the fish, name, level and Cardume position.</summary>
        private void DrawTile(UiSkin skin, Rect tile, FishView f)
        {
            var marked = _multi && _sellSet.Contains(f.FishId);
            var selected = !_multi && f.FishId == _selectedId;
            var hover = GUI.enabled && tile.Contains(Event.current.mousePosition);
            var night = UiSkin.Night;
            GUI.DrawTexture(tile, skin.White, ScaleMode.StretchToFill, true, 0, new Color(night.r, night.g, night.b, hover ? 0.78f : 0.5f), 0, 12f);
            if (selected || marked)
            {
                GUI.DrawTexture(tile, skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.Accent, 2f, 12f);
            }

            const float d = 56f;
            var disc = new Rect(tile.center.x - d / 2f, tile.y + 6f, d, d);
            var panel = VisualTheme.Current.Panel;
            GUI.DrawTexture(disc, skin.White, ScaleMode.StretchToFill, true, 0, new Color(panel.r, panel.g, panel.b, 0.95f), 0, d / 2f);
            GUI.DrawTexture(disc, skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.RarityColor(f.RarityId), 2f, d / 2f);
            if (VisualTheme.IsSpecialSize(f.SizeCategoryId))
            {
                var outer = new Rect(disc.x - 3f, disc.y - 3f, d + 6f, d + 6f);
                GUI.DrawTexture(outer, skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.SizeColor(f.SizeCategoryId), 1.5f, outer.width / 2f);
            }

            GUI.DrawTexture(new Rect(disc.x + 5f, disc.y + 13f, d - 10f, d - 26f), Art.FishTexture(f.SpeciesId), ScaleMode.ScaleToFit, true);

            var text = TileText(skin, f);
            GUI.Label(new Rect(tile.x + 4f, tile.y + 64f, tile.width - 8f, 18f), text.Name, skin.ChipText);
            GUI.Label(new Rect(tile.x + 4f, tile.y + 83f, tile.width - 8f, 18f), text.Line, skin.SmallMutedCenter);

            if (marked)
            {
                CheckBadge(skin, new Rect(tile.xMax - 24f, tile.y + 4f, 20f, 20f));
            }

            if (GUI.Button(tile, GUIContent.none, GUIStyle.none))
            {
                OnFishClicked(f, Event.current != null && (Event.current.control || Event.current.command));
            }
        }

        /// <summary>The tile's name (shortened to fit) and "Nv. X · C1" line, built once per fish.</summary>
        private (string Name, string Line) TileText(UiSkin skin, FishView f)
        {
            if (!_tileText.TryGetValue(f.FishId, out var text))
            {
                var line = GameTexts.Player.LevelShort + " " + f.Level + (f.CardumePosition > 0 ? " · " + GameTexts.Cardume.Badge(f.CardumePosition) : string.Empty);
                text = (UI.FishCard.Fit(f.SpeciesName, skin.ChipText, TileWidth - 8f), line);
                _tileText[f.FishId] = text;
            }

            return text;
        }

        private static void CheckBadge(UiSkin skin, Rect badge)
        {
            GUI.DrawTexture(badge, skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.Accent, 0, badge.width / 2f);
            skin.DrawIcon(new Rect(badge.x + 4f, badge.y + 4f, badge.width - 8f, badge.height - 8f), Icons.Check, UiSkin.Night);
        }

        /// <summary>A click on a fish (tank or drawer): opens its sheet, or marks it while selecting several.</summary>
        private void OnFishClicked(FishView fish, bool ctrl)
        {
            // Ctrl + click starts a selection (with the fish already open, if any). Ctrl + click on the
            // open fish itself starts the selection with it, instead of adding and removing it at once.
            var started = false;
            if (!_multi && ctrl)
            {
                StartMulti();
                started = true;
            }

            if (_multi)
            {
                if (!(started && _sellSet.Contains(fish.FishId)))
                {
                    ToggleSell(fish.FishId);
                }
            }
            else
            {
                _selectedId = fish.FishId;
                _selected = fish;
            }
        }

        /// <summary>A translucent glass panel over the water (the sheet, the sale, the drawer).</summary>
        private static void Glass(UiSkin skin, Rect r)
        {
            var art = ArtAssets.Texture(GlassArt);
            if (art != null)
            {
                UiSkin.NineSlice(r, art, GlassArtBorder, GlassArtBorder, GlassArtBorder, GlassArtBorder, 1f);
                return;
            }

            // ASSET_PENDENTE: ui_aquarium_glass.png (9-slice glass panel) replaces this drawn glass.
            var night = UiSkin.Night;
            GUI.DrawTexture(r, skin.White, ScaleMode.StretchToFill, true, 0, new Color(night.r, night.g, night.b, 0.86f), 0, 14f);
            GUI.DrawTexture(r, skin.White, ScaleMode.StretchToFill, true, 0, new Color(0.75f, 0.92f, 1f, 0.22f), 1f, 14f);
            GUI.DrawTexture(new Rect(r.x + 16f, r.y + 1f, r.width - 32f, 1f), skin.White, ScaleMode.StretchToFill, true, 0, new Color(1f, 1f, 1f, 0.18f), 0, 0);
        }

        /// <summary>
        /// Draws a texture turned by <paramref name="angle"/> degrees around <paramref name="pivot"/> and, when
        /// flipped, mirrored; composed with the HUD's GUI.matrix in the virtual canvas (as ArenaWindow.DrawFish).
        /// </summary>
        private static void DrawTransformed(Rect rect, Texture tex, Vector2 pivot, float angle, bool flip, Color color)
        {
            var matrix = GUI.matrix;
            var p = new Vector3(pivot.x, pivot.y, 0f);
            GUI.matrix = matrix * Matrix4x4.TRS(p, Quaternion.Euler(0f, 0f, angle), new Vector3(flip ? -1f : 1f, 1f, 1f)) * Matrix4x4.TRS(-p, Quaternion.identity, Vector3.one);
            GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit, true, 0, color, 0, 0);
            GUI.matrix = matrix;
        }

        private float Phase(long id)
        {
            if (!_swimPhase.TryGetValue(id, out var phase))
            {
                phase = Hash01(id, 3);
                _swimPhase[id] = phase;
            }

            return phase;
        }

        /// <summary>A fixed number in [0, 1) from a fish id (its path never changes; no random draw per frame).</summary>
        private static float Hash01(long id, int salt)
        {
            unchecked
            {
                var x = (ulong)id * 0x9E3779B97F4A7C15UL + (ulong)salt * 0xBF58476D1CE4E5B9UL;
                x ^= x >> 31;
                x *= 0x94D049BB133111EBUL;
                x ^= x >> 29;
                return (x >> 40) / 16777216f;
            }
        }

        private static float LogCm(FishView f) => Mathf.Log((float)Math.Max(f.SizeCm, 0.1));

        // Provisional tank textures, made once (ASSET_PENDENTE: replaced by the files above when they exist).
        private static Color SandTop => new Color(0.66f, 0.58f, 0.43f);

        private static Texture2D WaterTexture => _waterTexture != null ? _waterTexture
            : (_waterTexture = GradientTexture(64, new Color(0.20f, 0.54f, 0.62f), new Color(0.08f, 0.31f, 0.44f), new Color(0.05f, 0.16f, 0.27f)));

        private static Texture2D SandTexture => _sandTexture != null ? _sandTexture
            : (_sandTexture = GradientTexture(32, SandTop, new Color(0.40f, 0.35f, 0.27f)));

        private static Texture2D RaysTexture
        {
            get
            {
                if (_raysTexture != null)
                {
                    return _raysTexture;
                }

                const int size = 128;
                var tex = NewTexture(size, size);
                for (var y = 0; y < size; y++)
                {
                    var v = (y + 0.5f) / size; // 1 at the top
                    for (var x = 0; x < size; x++)
                    {
                        var s = (x + 0.5f) / size + (1f - v) * 0.3f;
                        var beam = Ray(s, 0.22f, 0.05f) + Ray(s, 0.52f, 0.08f) * 0.8f + Ray(s, 0.80f, 0.04f);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(beam) * Mathf.Pow(v, 1.6f) * 0.16f));
                    }
                }

                tex.Apply();
                _raysTexture = tex;
                return tex;
            }
        }

        private static float Ray(float s, float centre, float width)
        {
            var d = (s - centre) / width;
            return Mathf.Exp(-d * d);
        }

        private static Texture2D GradientTexture(int height, params Color[] topToBottom)
        {
            var tex = NewTexture(1, height);
            for (var y = 0; y < height; y++)
            {
                var s = (1f - y / (float)(height - 1)) * (topToBottom.Length - 1); // 0 at the top
                var i = Mathf.Min(Mathf.FloorToInt(s), topToBottom.Length - 2);
                tex.SetPixel(0, y, Color.Lerp(topToBottom[i], topToBottom[i + 1], s - i));
            }

            tex.Apply();
            return tex;
        }

        private static Texture2D NewTexture(int w, int h)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };
        }

        // ------------------------------------------------------------------ the sheet

        private void DrawSheet(UiSkin skin, Rect area)
        {
            Glass(skin, area);
            var fish = _selected;
            if (fish == null)
            {
                GUI.Label(new Rect(area.x + 20, area.y + area.height / 2f - 20, area.width - 40, 40), GameTexts.Aquarium.PickFish, skin.Center);
                return;
            }

            var x = area.x + 22;
            var w = area.width - 44;
            var y = area.y + 18;

            // On a short panel the art shrinks and the four stats go in two columns, so the sheet always
            // ends above Alimentar / Vender (the content under the art is ~354 px, ~310 px in two columns;
            // the numbers below keep ~18 px of margin).
            const float bottomRoom = 66f + 18f + 16f;
            var fixedHeight = bottomRoom + 372f;
            var twoColumns = area.height - fixedHeight < 124f;
            if (twoColumns)
            {
                fixedHeight -= 44f;
            }

            var artHeight = Mathf.Clamp(area.height - fixedHeight, 48f, 124f);

            var accent = UiSkin.RarityColor(fish.RarityId);
            skin.DrawOutline(area, new Color(accent.r, accent.g, accent.b, 0.8f));
            var art = new Rect(x, y + 6, w, artHeight);
            var exceptional = VisualTheme.IsSpecialSize(fish.SizeCategoryId);
            if (fish.IsImportant)
            {
                skin.DrawGlow(new Rect(art.x + art.width * 0.25f, art.y + art.height * 0.3f, art.width * 0.5f, art.height * 0.4f),
                    exceptional ? UiSkin.SizeColor(fish.SizeCategoryId) : accent, 0.4f);
            }

            GUI.DrawTexture(art, Art.FishTexture(fish.SpeciesId), ScaleMode.ScaleToFit, true);
            y += artHeight + 16;

            GUI.Label(new Rect(x, y, w, 30), UI.FishCard.Fit(fish.SpeciesName, skin.Heading, w), skin.Heading);
            y += 32;
            var rarityLabel = fish.RarityName.ToUpperInvariant();
            var pillWidth = skin.PillWidth(rarityLabel, true);
            skin.RarityPill(new Rect(x, y, pillWidth, 22), fish.RarityId, rarityLabel);
            if (exceptional)
            {
                UI.FishCard.ExceptionalSeal(skin, new Rect(x + pillWidth + 8, y + 1, 110, 20), fish.SizeCategoryName.ToUpperInvariant(), UiSkin.SizeColor(fish.SizeCategoryId));
            }
            else
            {
                // The size seal next to the rarity, in the size colour (addendum A-079).
                var sizeLabel = fish.SizeCategoryName.ToUpperInvariant();
                skin.AccentPill(new Rect(x + pillWidth + 8, y, skin.PillWidth(sizeLabel, false), 22), sizeLabel, UiSkin.SizeColor(fish.SizeCategoryId));
            }

            y += 32;

            // Size: number and a bar inside the species range (GDD section 14).
            GUI.Label(new Rect(x, y, w, 22), GameTexts.Aquarium.Size + ": " + Format.SizeCm(fish.SizeCm) + "  (" + Format.SizeCm(fish.SpeciesMinCm) + " – " + Format.SizeCm(fish.SpeciesMaxCm) + ")", skin.Small);
            y += 22;
            skin.Bar(new Rect(x, y, w, 8), (float)fish.SizePercentile, exceptional ? UiSkin.SizeColor(fish.SizeCategoryId) : accent);
            y += 20;

            GUI.Label(new Rect(x, y, w, 22), GameTexts.Aquarium.LevelOf(fish.Level, fish.MaxLevel), skin.BodyBold);
            y += 24;
            if (fish.XpToNext > 0)
            {
                skin.Bar(new Rect(x, y, w, 8), fish.Xp / (float)fish.XpToNext, accent);
                GUI.Label(new Rect(x, y + 10, w, 20), GameTexts.Aquarium.Xp(Format.Number(fish.Xp), Format.Number(fish.XpToNext)), skin.SmallMuted);
            }
            else
            {
                skin.Bar(new Rect(x, y, w, 8), 1f, true);
                GUI.Label(new Rect(x, y + 10, w, 20), GameTexts.Aquarium.MaxLevelReached, skin.SmallMuted);
            }

            y += 38;
            GUI.Label(new Rect(x, y, w, 22), GameTexts.Aquarium.Stats, skin.BodyBold);
            y += 26;
            if (twoColumns)
            {
                var colW = (w - 20f) / 2f;
                var y2 = y;
                StatRow(skin, x, ref y, colW, GameTexts.Aquarium.Hp, fish.Stats.Hp);
                StatRow(skin, x, ref y, colW, GameTexts.Aquarium.Defense, fish.Stats.Defense);
                StatRow(skin, x + colW + 20f, ref y2, colW, GameTexts.Aquarium.Attack, fish.Stats.Attack);
                StatRow(skin, x + colW + 20f, ref y2, colW, GameTexts.Aquarium.Speed, fish.Stats.Speed);
            }
            else
            {
                StatRow(skin, x, ref y, w, GameTexts.Aquarium.Hp, fish.Stats.Hp);
                StatRow(skin, x, ref y, w, GameTexts.Aquarium.Attack, fish.Stats.Attack);
                StatRow(skin, x, ref y, w, GameTexts.Aquarium.Defense, fish.Stats.Defense);
                StatRow(skin, x, ref y, w, GameTexts.Aquarium.Speed, fish.Stats.Speed);
            }

            y += 6;

            InfoRow(skin, x, ref y, w, GameTexts.Aquarium.SaleValue, Format.Number(fish.SalePriceCoins));
            InfoRow(skin, x, ref y, w, GameTexts.Aquarium.FeedValue, GameTexts.Aquarium.FeedXp(Format.Number(fish.FeedXp)));
            InfoRow(skin, x, ref y, w, GameTexts.Aquarium.CaughtAt, Format.DateTimeFromUnixMs(fish.CaughtAtMs));

            var buttons = area.yMax - 60;
            var enabled = GUI.enabled;
            GUI.enabled = enabled && fish.XpToNext > 0;
            if (skin.IconButton(new Rect(x, buttons, w / 2f - 6, 42), Icons.Feed, GameTexts.Aquarium.Feed, skin.ButtonPrimary))
            {
                StartFeeding();
            }

            GUI.enabled = enabled;
            if (skin.IconButton(new Rect(x + w / 2f + 6, buttons, w / 2f - 6, 42), Icons.Sell, GameTexts.Aquarium.Sell, skin.Button))
            {
                _confirmSell = true;
            }
        }

        private static void StatRow(UiSkin skin, float x, ref float y, float w, string label, double value)
        {
            var text = Format.Number((long)Math.Round(value));
            var vw = Mathf.Min(w, skin.SmallRight.CalcSize(new GUIContent(text)).x + 4f);
            var lw = Mathf.Max(0f, w - vw - 6f);
            GUI.Label(new Rect(x, y, lw, 20), UI.FishCard.Fit(label, skin.SmallMuted, lw), skin.SmallMuted);
            GUI.Label(new Rect(x + w - vw, y, vw, 20), text, skin.SmallRight);
            y += 22;
        }

        private static void InfoRow(UiSkin skin, float x, ref float y, float w, string label, string value)
        {
            GUI.Label(new Rect(x, y, 170, 20), label, skin.SmallMuted);
            GUI.Label(new Rect(x + 170, y, w - 170, 20), value, skin.SmallRight);
            y += 22;
        }

        // ------------------------------------------------------------------ card mode, the hero sheet (A-145)

        /// <summary>The two-way "Tanque | Cartas" switch, ending at <paramref name="right"/>. The choice is kept in PlayerPrefs.</summary>
        private void DrawVisualToggle(UiSkin skin, float right, float y)
        {
            var tankWidth = skin.Chip.CalcSize(new GUIContent(GameTexts.Aquarium.ViewTank)).x + 34f;
            var cardsWidth = skin.Chip.CalcSize(new GUIContent(GameTexts.Aquarium.ViewCards)).x + 34f;
            var x = right - tankWidth - cardsWidth - 6f;
            if (skin.IconButton(new Rect(x, y, tankWidth, 38), Icons.Waves, GameTexts.Aquarium.ViewTank, _cardsMode ? skin.Chip : skin.ChipActive) && _cardsMode)
            {
                SetCardsMode(false);
            }

            if (skin.IconButton(new Rect(x + tankWidth + 6f, y, cardsWidth, 38), Icons.Book, GameTexts.Aquarium.ViewCards, _cardsMode ? skin.ChipActive : skin.Chip) && !_cardsMode)
            {
                SetCardsMode(true);
            }
        }

        /// <summary>Switches the view; the selected fish (and a selection to sell) stay as they are.</summary>
        private void SetCardsMode(bool cards)
        {
            _cardsMode = cards;
            _cardsFollow = true;
            PlayerPrefs.SetInt(VisualPrefsKey, cards ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Card mode: a compact list of cards on the left (filtered and ordered like the tank) and, beside it, the hero
        /// sheet of the selected fish; while selecting several, the several-fish sale takes the sheet's place.
        /// </summary>
        private void DrawCards(UiSkin skin, Rect body)
        {
            if (_aquarium.Count == 0 || _shown.Count == 0)
            {
                GUI.Box(body, GUIContent.none, skin.Card);
                GUI.Label(new Rect(body.x + 40f, body.center.y - 30f, body.width - 80f, 60f),
                    _aquarium.Count == 0 ? GameTexts.Aquarium.Empty : GameTexts.Search.NoMatch, skin.Center);
                return;
            }

            const float gap = 14f;
            var listWidth = Mathf.Clamp(body.width * 0.25f, 240f, 310f);
            var list = new Rect(body.x, body.y, listWidth, body.height);
            var rest = new Rect(list.xMax + gap, body.y, body.xMax - list.xMax - gap, body.height);

            if (!_multi)
            {
                // The sheet always shows a fish of the list: the first one when the chosen fish is not in it.
                var index = IndexOfSelected();
                if (index < 0)
                {
                    _selected = _shown[0];
                    _selectedId = _selected.FishId;
                    index = 0;
                    _cardsFollow = true;
                }

                // ← → walk the list like ‹ › (not while a dialog is open; a focused search field uses them first).
                var ev = Event.current;
                if (GUI.enabled && _shown.Count > 1 && ev.type == EventType.KeyDown && (ev.keyCode == KeyCode.LeftArrow || ev.keyCode == KeyCode.RightArrow))
                {
                    StepHero(index, ev.keyCode == KeyCode.LeftArrow ? -1 : 1);
                    ev.Use();
                }
            }

            DrawCardList(skin, list);

            if (_multi)
            {
                var w = Mathf.Min(rest.width, 560f);
                DrawSelectionPanel(skin, new Rect(rest.center.x - w / 2f, rest.y, w, rest.height));
                return;
            }

            if (_selected != null)
            {
                DrawHero(skin, rest);
            }
        }

        private int IndexOfSelected()
        {
            for (var i = 0; i < _shown.Count; i++)
            {
                if (_shown[i].FishId == _selectedId)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Moves to the previous or next fish of the list, wrapping around; the list scrolls to it.</summary>
        private void StepHero(int index, int delta)
        {
            var next = ((index + delta) % _shown.Count + _shown.Count) % _shown.Count;
            _selected = _shown[next];
            _selectedId = _selected.FishId;
            _cardsFollow = true;
        }

        /// <summary>The scrolling list of compact cards (only the rows on screen are drawn).</summary>
        private void DrawCardList(UiSkin skin, Rect list)
        {
            GUI.Box(list, GUIContent.none, skin.Card);
            var inner = new Rect(list.x + 8f, list.y + 8f, list.width - 16f, list.height - 16f);
            var contentWidth = inner.width - 18f;
            var rowHeight = CardRowHeight + CardRowGap;

            if (_cardsFollow)
            {
                _cardsFollow = false;
                var index = _multi ? -1 : IndexOfSelected();
                if (index >= 0)
                {
                    var top = index * rowHeight;
                    if (top < _cardsScroll.y)
                    {
                        _cardsScroll.y = top;
                    }
                    else if (top + CardRowHeight > _cardsScroll.y + inner.height)
                    {
                        _cardsScroll.y = top + CardRowHeight - inner.height;
                    }
                }
            }

            _cardsScroll = GUI.BeginScrollView(inner, _cardsScroll, new Rect(0, 0, contentWidth, Mathf.Max(0f, _shown.Count * rowHeight - CardRowGap)));
            var first = Mathf.Max(0, Mathf.FloorToInt(_cardsScroll.y / rowHeight));
            var last = Mathf.Min(_shown.Count - 1, Mathf.CeilToInt((_cardsScroll.y + inner.height) / rowHeight));
            for (var i = first; i <= last; i++)
            {
                DrawListCard(skin, new Rect(0, i * rowHeight, contentWidth, CardRowHeight), _shown[i]);
            }

            GUI.EndScrollView();
        }

        /// <summary>One compact card: frame and stripe in the rarity colour, the miniature, name, level and size, C1–C6.</summary>
        private void DrawListCard(UiSkin skin, Rect r, FishView f)
        {
            var marked = _multi && _sellSet.Contains(f.FishId);
            var selected = !_multi && f.FishId == _selectedId;
            var hover = GUI.enabled && r.Contains(Event.current.mousePosition);
            var theme = VisualTheme.Current;
            var rarity = UiSkin.RarityColor(f.RarityId);
            var low = f.RarityId == "common" || f.RarityId == "rare";

            var fill = selected ? theme.PanelElevated : Color.Lerp(theme.Panel, theme.PanelElevated, hover ? 0.6f : 0.2f);
            GUI.DrawTexture(r, skin.White, ScaleMode.StretchToFill, true, 0, fill, 0, 10f);
            GUI.DrawTexture(r, skin.White, ScaleMode.StretchToFill, true, 0, new Color(rarity.r, rarity.g, rarity.b, (low ? 0.45f : 0.85f) + (hover ? 0.15f : 0f)), 1.5f, 10f);
            GUI.DrawTexture(new Rect(r.x + 5f, r.y + 10f, 3f, r.height - 20f), skin.White, ScaleMode.StretchToFill, true, 0, rarity, 0, 1.5f);
            if (selected || marked)
            {
                GUI.DrawTexture(r, skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.Accent, 2f, 10f);
            }

            // The miniature, ringed in the size colour for Excepcional / Perfeição.
            var art = new Rect(r.x + 14f, r.y + 8f, 70f, r.height - 16f);
            GUI.DrawTexture(art, skin.White, ScaleMode.StretchToFill, true, 0, new Color(0.06f, 0.17f, 0.26f, 0.9f), 0, 8f);
            if (VisualTheme.IsSpecialSize(f.SizeCategoryId))
            {
                GUI.DrawTexture(art, skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.SizeColor(f.SizeCategoryId), 1.5f, 8f);
            }

            GUI.DrawTexture(new Rect(art.x + 4f, art.y + 4f, art.width - 8f, art.height - 8f), Art.FishTexture(f.SpeciesId), ScaleMode.ScaleToFit, true);

            // Right column: the Cardume position on top, the check below while selecting several.
            var badge = f.CardumePosition > 0 ? GameTexts.Cardume.Badge(f.CardumePosition) : null;
            var badgeWidth = badge != null ? skin.Badge.CalcSize(new GUIContent(badge)).x + 8f : 0f;
            var rightWidth = Mathf.Max(badgeWidth, marked ? 20f : 0f);
            if (badge != null)
            {
                skin.Tag(new Rect(r.xMax - 10f - badgeWidth, r.y + 10f, badgeWidth, 18f), badge, UiSkin.Accent);
            }

            if (marked)
            {
                CheckBadge(skin, new Rect(r.xMax - 30f, r.yMax - 28f, 20f, 20f));
            }

            var tx = art.xMax + 10f;
            var tw = r.xMax - 10f - (rightWidth > 0f ? rightWidth + 6f : 0f) - tx;
            var text = CardText(skin, f, tw);
            GUI.Label(new Rect(tx, r.y + 10f, tw, 22f), text.Name, skin.BodyBold);
            GUI.Label(new Rect(tx, r.y + 34f, tw, 20f), text.Line, skin.SmallMuted);

            if (GUI.Button(r, GUIContent.none, GUIStyle.none))
            {
                OnFishClicked(f, Event.current != null && (Event.current.control || Event.current.command));
            }
        }

        /// <summary>The card's name and "Nv. X · 48,6 cm" line, shortened to fit and kept until the width changes.</summary>
        private (string Name, string Line) CardText(UiSkin skin, FishView f, float width)
        {
            if (!_cardText.TryGetValue(f.FishId, out var text) || !Mathf.Approximately(text.Width, width))
            {
                var line = GameTexts.Aquarium.CardsLine(f.Level, Format.SizeCm(f.SizeCm));
                text = (width, UI.FishCard.Fit(f.SpeciesName, skin.BodyBold, width), UI.FishCard.Fit(line, skin.SmallMuted, width));
                _cardText[f.FishId] = text;
            }

            return (text.Name, text.Line);
        }

        /// <summary>The hero sheet: the fish on its pedestal with ‹ › on the left, the numbers and Alimentar / Vender on the right.</summary>
        private void DrawHero(UiSkin skin, Rect rest)
        {
            var fish = _selected;
            if (fish.FishId != _heroShownId)
            {
                _heroShownId = fish.FishId;
                _heroChangedAt = Time.unscaledTime;
            }

            const float gap = 14f;
            var infoWidth = Mathf.Clamp(rest.width * 0.44f, 300f, 400f);
            var stage = new Rect(rest.x, rest.y, rest.width - infoWidth - gap, rest.height);
            var info = new Rect(stage.xMax + gap, rest.y, infoWidth, rest.height);
            DrawHeroStage(skin, stage, fish);
            DrawHeroInfo(skin, info, fish);
        }

        private void DrawHeroStage(UiSkin skin, Rect rect, FishView fish)
        {
            var rarity = UiSkin.RarityColor(fish.RarityId);
            HeroSheet.Backdrop(skin, rect);
            var index = IndexOfSelected();
            GUI.Label(new Rect(rect.x + 12f, rect.y + 10f, rect.width - 24f, 20f), GameTexts.Aquarium.CardsPosition(index + 1, _shown.Count), skin.SmallMutedCenter);

            // The fish and its pedestal, centred in the stage.
            const float pedestalH = 34f;
            var artW = Mathf.Max(60f, rect.width - 130f);
            var artH = Mathf.Clamp(Mathf.Min(rect.height - 38f - 14f - pedestalH - 16f, artW * 0.6f), 40f, 320f);
            var groupBottom = Mathf.Min(rect.yMax - 14f, rect.center.y + 16f + (artH + 6f + pedestalH) / 2f);
            var zoneBottom = groupBottom - pedestalH;
            var centre = new Vector2(rect.center.x, zoneBottom - artH / 2f - 6f);

            var aura = Mathf.Max(0f, Mathf.Min(artW + 20f, 2f * (centre.y - rect.y - 8f)));
            HeroSheet.Aura(skin, centre, aura, rarity, HeroSheet.AuraStrength(fish.RarityId));
            if (VisualTheme.IsSpecialSize(fish.SizeCategoryId))
            {
                // Excepcional / Perfeição: the slow glow in the size colour (Art Bible 2.4).
                skin.DrawGlow(new Rect(centre.x - artW * 0.25f, centre.y - artH * 0.2f, artW * 0.5f, artH * 0.4f), UiSkin.SizeColor(fish.SizeCategoryId),
                    0.3f + 0.08f * Mathf.Sin(Time.unscaledTime * 0.9f));
            }

            HeroSheet.Pedestal(skin, rect.center.x, zoneBottom + 4f, Mathf.Min(artW * 0.8f, 340f), rarity, 0.7f);

            // The fish fades in when it changes and floats a little.
            var t = Mathf.Clamp01((Time.unscaledTime - _heroChangedAt) / 0.25f);
            var bob = Mathf.Sin(Time.unscaledTime * 1.6f) * 3f;
            var art = new Rect(centre.x - artW / 2f, centre.y - artH / 2f + bob + (1f - t) * 10f, artW, artH);
            GUI.DrawTexture(art, Art.FishTexture(fish.SpeciesId), ScaleMode.ScaleToFit, true, 0, new Color(1f, 1f, 1f, t), 0, 0);

            // ‹ › walk the current list.
            if (index >= 0 && _shown.Count > 1)
            {
                if (HeroSheet.Arrow(skin, new Rect(rect.x + 12f, centre.y - 24f, 48f, 48f), true))
                {
                    StepHero(index, -1);
                }

                if (HeroSheet.Arrow(skin, new Rect(rect.xMax - 60f, centre.y - 24f, 48f, 48f), false))
                {
                    StepHero(index, 1);
                }
            }
        }

        /// <summary>
        /// Name, rarity and size seals, the size, the level with its XP, the four attributes as bars, the sale and food
        /// values, the date and the Cardume position, then Alimentar and Vender (the same flows as the tank's sheet).
        /// </summary>
        private void DrawHeroInfo(UiSkin skin, Rect rect, FishView fish)
        {
            GUI.Box(rect, GUIContent.none, skin.Card);
            var accent = UiSkin.RarityColor(fish.RarityId);
            var low = fish.RarityId == "common" || fish.RarityId == "rare";
            skin.DrawOutline(rect, new Color(accent.r, accent.g, accent.b, low ? 0.45f : 0.8f));
            var x = rect.x + 20f;
            var w = rect.width - 40f;
            var y = rect.y + 16f;
            const float buttonH = 48f;
            var buttons = rect.yMax - 16f - buttonH;
            var exceptional = VisualTheme.IsSpecialSize(fish.SizeCategoryId);
            var sizeColor = UiSkin.SizeColor(fish.SizeCategoryId);

            // Eight rows (attributes and info) share what the fixed parts leave; the size bar goes first on a short sheet.
            const float fixedHeight = 36f + 30f + 22f + 62f + 26f + 4f + 12f;
            var withSizeBar = (buttons - y - fixedHeight - 16f) / 8f >= 20f;
            var rowH = Mathf.Clamp((buttons - y - fixedHeight - (withSizeBar ? 16f : 0f)) / 8f, 20f, 28f);

            GUI.Label(new Rect(x, y, w, 34f), UI.FishCard.Fit(fish.SpeciesName, skin.Title, w), skin.Title);
            y += 36f;

            // Rarity seal, then the size seal (gold Excepcional / diamond Perfeição, or the size pill).
            var rarityLabel = fish.RarityName.ToUpperInvariant();
            var pillWidth = Mathf.Min(skin.PillWidth(rarityLabel, true), w * 0.55f);
            skin.RarityPill(new Rect(x, y, pillWidth, 22f), fish.RarityId, rarityLabel);
            var sizeLabel = fish.SizeCategoryName.ToUpperInvariant();
            var sizeRoom = w - pillWidth - 8f;
            if (exceptional)
            {
                var sealWidth = Mathf.Min(skin.Badge.CalcSize(new GUIContent(sizeLabel)).x + 10f, sizeRoom);
                UI.FishCard.ExceptionalSeal(skin, new Rect(x + pillWidth + 8f, y + 1f, sealWidth, 20f), sizeLabel, sizeColor);
            }
            else
            {
                skin.AccentPill(new Rect(x + pillWidth + 8f, y, Mathf.Min(skin.PillWidth(sizeLabel, false), sizeRoom), 22f), sizeLabel, sizeColor);
            }

            y += 30f;

            // Size inside the species range (GDD section 14).
            var sizeText = GameTexts.Aquarium.Size + ": " + Format.SizeCm(fish.SizeCm) + "  (" + Format.SizeCm(fish.SpeciesMinCm) + " – " + Format.SizeCm(fish.SpeciesMaxCm) + ")";
            GUI.Label(new Rect(x, y, w, 20f), UI.FishCard.Fit(sizeText, skin.Small, w), skin.Small);
            y += 22f;
            if (withSizeBar)
            {
                skin.Bar(new Rect(x, y, w, 6f), (float)fish.SizePercentile, exceptional ? sizeColor : accent);
                y += 16f;
            }

            // The level, big: a disc with the number, "Nível X de 10" and the XP bar.
            const float disc = 52f;
            var discRect = new Rect(x, y + 2f, disc, disc);
            var night = UiSkin.Night;
            GUI.DrawTexture(discRect, skin.White, ScaleMode.StretchToFill, true, 0, new Color(night.r, night.g, night.b, 0.85f), 0, disc / 2f);
            GUI.DrawTexture(discRect, skin.White, ScaleMode.StretchToFill, true, 0, accent, 2.5f, disc / 2f);
            GUI.Label(discRect, fish.Level.ToString(), skin.TitleCenter);
            var lx = x + disc + 12f;
            var lw = w - disc - 12f;
            GUI.Label(new Rect(lx, y + 2f, lw, 22f), UI.FishCard.Fit(GameTexts.Aquarium.LevelOf(fish.Level, fish.MaxLevel), skin.BodyBold, lw), skin.BodyBold);
            if (fish.XpToNext > 0)
            {
                skin.Bar(new Rect(lx, y + 27f, lw, 10f), fish.Xp / (float)fish.XpToNext, accent);
                GUI.Label(new Rect(lx, y + 39f, lw, 20f), UI.FishCard.Fit(GameTexts.Aquarium.Xp(Format.Number(fish.Xp), Format.Number(fish.XpToNext)), skin.SmallMuted, lw), skin.SmallMuted);
            }
            else
            {
                skin.Bar(new Rect(lx, y + 27f, lw, 10f), 1f, true);
                GUI.Label(new Rect(lx, y + 39f, lw, 20f), GameTexts.Aquarium.MaxLevelReached, skin.SmallMuted);
            }

            y += 62f;

            // The four attributes as bars, on the scale of the strongest kept fish.
            var noteWidth = Mathf.Min(w * 0.5f, skin.SmallMutedRightLine.CalcSize(new GUIContent(GameTexts.Aquarium.CardsStatsNote)).x + 4f);
            GUI.Label(new Rect(x, y, w - noteWidth - 8f, 24f), UI.FishCard.Fit(GameTexts.Aquarium.Stats, skin.BodyBold, w - noteWidth - 8f), skin.BodyBold);
            GUI.Label(new Rect(x + w - noteWidth, y + 3f, noteWidth, 20f), UI.FishCard.Fit(GameTexts.Aquarium.CardsStatsNote, skin.SmallMutedRightLine, noteWidth), skin.SmallMutedRightLine);
            y += 26f;
            var stats = fish.Stats;
            HeroStat(skin, x, ref y, w, rowH, GameTexts.Aquarium.Hp, stats?.Hp ?? 0d, _statMax[0], accent);
            HeroStat(skin, x, ref y, w, rowH, GameTexts.Aquarium.Attack, stats?.Attack ?? 0d, _statMax[1], accent);
            HeroStat(skin, x, ref y, w, rowH, GameTexts.Aquarium.Defense, stats?.Defense ?? 0d, _statMax[2], accent);
            HeroStat(skin, x, ref y, w, rowH, GameTexts.Aquarium.Speed, stats?.Speed ?? 0d, _statMax[3], accent);
            y += 4f;

            // Sale value (with the coin), food value, date and the Cardume position.
            var ty = y + (rowH - 20f) / 2f;
            var coins = Format.Number(fish.SalePriceCoins);
            var coinsWidth = Mathf.Min(skin.CoinAmountWidth(coins, 20f), w * 0.5f);
            GUI.Label(new Rect(x, ty, w - coinsWidth - 8f, 20f), UI.FishCard.Fit(GameTexts.Aquarium.SaleValue, skin.SmallMuted, w - coinsWidth - 8f), skin.SmallMuted);
            skin.CoinAmount(new Rect(x + w - coinsWidth, ty, coinsWidth, 20f), coins);
            y += rowH;
            HeroInfoRow(skin, x, ref y, w, rowH, GameTexts.Aquarium.FeedValue, GameTexts.Aquarium.FeedXp(Format.Number(fish.FeedXp)));
            HeroInfoRow(skin, x, ref y, w, rowH, GameTexts.Aquarium.CaughtAt, Format.DateTimeFromUnixMs(fish.CaughtAtMs));
            HeroInfoRow(skin, x, ref y, w, rowH, GameTexts.Aquarium.CardumeLabel,
                fish.CardumePosition > 0 ? GameTexts.Aquarium.CardumeSlot(fish.CardumePosition) : GameTexts.Aquarium.CardumeOut);

            // Alimentar / Vender: the same flows as the tank's sheet (food picker, then the same dialogs).
            var enabled = GUI.enabled;
            GUI.enabled = enabled && fish.XpToNext > 0;
            if (skin.IconButton(new Rect(x, buttons, w / 2f - 6f, buttonH), Icons.Feed, GameTexts.Aquarium.Feed, skin.ButtonPrimary))
            {
                StartFeeding();
            }

            GUI.enabled = enabled;
            if (skin.IconButton(new Rect(x + w / 2f + 6f, buttons, w / 2f - 6f, buttonH), Icons.Sell, GameTexts.Aquarium.Sell, skin.Button))
            {
                _confirmSell = true;
            }
        }

        private static void HeroStat(UiSkin skin, float x, ref float y, float w, float rowH, string label, double value, double max, Color color)
        {
            var labelWidth = Mathf.Min(100f, w * 0.32f);
            const float valueWidth = 60f;
            var ty = y + (rowH - 20f) / 2f;
            GUI.Label(new Rect(x, ty, labelWidth, 20f), UI.FishCard.Fit(label, skin.SmallMuted, labelWidth), skin.SmallMuted);
            skin.Bar(new Rect(x + labelWidth + 6f, ty + 6f, w - labelWidth - valueWidth - 12f, 8f), max > 0d ? (float)(value / max) : 0f, color);
            GUI.Label(new Rect(x + w - valueWidth, ty, valueWidth, 20f), Format.Number((long)Math.Round(value)), skin.SmallRight);
            y += rowH;
        }

        private static void HeroInfoRow(UiSkin skin, float x, ref float y, float w, float rowH, string label, string value)
        {
            var ty = y + (rowH - 20f) / 2f;
            var labelWidth = Mathf.Min(skin.SmallMuted.CalcSize(new GUIContent(label)).x + 12f, w * 0.55f);
            GUI.Label(new Rect(x, ty, labelWidth, 20f), UI.FishCard.Fit(label, skin.SmallMuted, labelWidth), skin.SmallMuted);
            GUI.Label(new Rect(x + labelWidth, ty, w - labelWidth, 20f), UI.FishCard.Fit(value, skin.SmallRight, w - labelWidth), skin.SmallRight);
            y += rowH;
        }

        // ------------------------------------------------------------------ selling several

        private void StartMulti()
        {
            _multi = true;
            _sellSet.Clear();
            if (_selected != null)
            {
                _sellSet.Add(_selected.FishId);
            }

            _salePreview = null;
            _sellScroll = Vector2.zero;
            _drawerOpen = true;
        }

        private void StopMulti()
        {
            _multi = false;
            _sellSet.Clear();
            _salePreview = null;
        }

        private void ToggleSell(long id)
        {
            if (!_sellSet.Remove(id))
            {
                _sellSet.Add(id);
            }

            _salePreview = null;
        }

        private SalePreview CurrentSale()
        {
            return _salePreview ?? (_salePreview = _root.PreviewFishSale(_sellSet.ToList()) ?? new SalePreview());
        }

        private void DrawSelectionPanel(UiSkin skin, Rect area)
        {
            Glass(skin, area);
            skin.DrawOutline(area, new Color(UiSkin.Accent.r, UiSkin.Accent.g, UiSkin.Accent.b, 0.7f));
            var x = area.x + 22;
            var w = area.width - 44;
            var y = area.y + 18;

            skin.DrawIcon(new Rect(x, y + 4, 24, 24), Icons.Sell, UiSkin.Gold);
            GUI.Label(new Rect(x + 32, y, w - 32, 32), GameTexts.Aquarium.MultiSelectTitle, skin.Heading);
            y += 36;
            GUI.Label(new Rect(x, y, w, 40), GameTexts.Aquarium.MultiSelectHint, skin.SmallMuted);
            y += 46;

            if (skin.IconButton(new Rect(x, y, w / 2f - 6, 36), Icons.Check, GameTexts.Box.SelectAll, skin.Button))
            {
                foreach (var fish in _shown)
                {
                    _sellSet.Add(fish.FishId);
                }

                _salePreview = null;
            }

            var enabled = GUI.enabled;
            GUI.enabled = enabled && _sellSet.Count > 0;
            if (GUI.Button(new Rect(x + w / 2f + 6, y, w / 2f - 6, 36), GameTexts.Box.ClearSelection, skin.Button))
            {
                _sellSet.Clear();
                _salePreview = null;
            }

            GUI.enabled = enabled;
            y += 50;

            // Count and total, big: what the player gets.
            var preview = CurrentSale();
            GUI.Label(new Rect(x, y, w, 28), _sellSet.Count == 0 ? GameTexts.Aquarium.MultiSelectNone : GameTexts.Aquarium.SelectedCount(_sellSet.Count), skin.BodyBold);
            y += 30;
            if (_sellSet.Count > 0)
            {
                skin.DrawIcon(new Rect(x, y + 3, 22, 22), Icons.Coin, Color.white);
                GUI.Label(new Rect(x + 30, y, w - 30, 28), GameTexts.Aquarium.SelectedTotal(Format.Number(preview.TotalCoins)), skin.BodyBold);
                y += 34;
            }

            // The selected fish, one row each, newest choice order does not matter: by sale price.
            var chosen = _aquarium.Fish.Where(f => _sellSet.Contains(f.FishId)).OrderByDescending(f => f.SalePriceCoins).ToList();
            var listBottom = area.yMax - 74;
            var listRect = new Rect(x, y + 4, w, Mathf.Max(0, listBottom - y - 8));
            const float rowHeight = 40f;
            _sellScroll = GUI.BeginScrollView(listRect, _sellScroll, new Rect(0, 0, w - 18, chosen.Count * rowHeight));
            var first = Mathf.Max(0, Mathf.FloorToInt(_sellScroll.y / rowHeight));
            var last = Mathf.Min(chosen.Count - 1, Mathf.CeilToInt((_sellScroll.y + listRect.height) / rowHeight));
            for (var i = first; i <= last; i++)
            {
                var f = chosen[i];
                var row = new Rect(0, i * rowHeight, w - 18, rowHeight - 4);
                GUI.Box(row, GUIContent.none, skin.Card);
                GUI.DrawTexture(new Rect(row.x + 6, row.y + 4, 48, row.height - 8), Art.FishTexture(f.SpeciesId), ScaleMode.ScaleToFit, true);
                var color = UiSkin.RarityColor(f.RarityId);
                GUI.DrawTexture(new Rect(row.x + 60, row.y + 14, 8, 8), skin.White, ScaleMode.StretchToFill, true, 0, color, 0, 4);
                var priceText = Format.Number(f.SalePriceCoins);
                var priceWidth = skin.SmallRight.CalcSize(new GUIContent(priceText)).x + 4;
                var nameWidth = row.width - 74 - priceWidth - 34;
                var name = f.SpeciesName + " · " + GameTexts.Player.LevelShort + " " + f.Level + (f.CardumePosition > 0 ? " · " + GameTexts.Cardume.Badge(f.CardumePosition) : string.Empty);
                GUI.Label(new Rect(row.x + 74, row.y + 8, nameWidth, 20), UI.FishCard.Fit(name, skin.Small, nameWidth), skin.Small);
                GUI.Label(new Rect(row.xMax - priceWidth - 30, row.y + 8, priceWidth, 20), priceText, skin.SmallRight);
                // The remove button: an icon drawn over an empty chip (the chip's padding would hide a text "×").
                var remove = new Rect(row.xMax - 26, row.y + 7, 22, 22);
                if (GUI.Button(remove, GUIContent.none, skin.Chip))
                {
                    ToggleSell(f.FishId);
                }

                skin.DrawIcon(new Rect(remove.x + 5, remove.y + 5, 12, 12), Icons.Close, UiSkin.Muted);
            }

            GUI.EndScrollView();

            var buttons = area.yMax - 60;
            if (GUI.Button(new Rect(x, buttons, w / 2f - 6, 42), GameTexts.Aquarium.MultiSelectExit, skin.Button))
            {
                StopMulti();
                return;
            }

            GUI.enabled = enabled && _sellSet.Count > 0;
            if (skin.IconButton(new Rect(x + w / 2f + 6, buttons, w / 2f - 6, 42), Icons.Sell, GameTexts.Aquarium.SellMany(_sellSet.Count), skin.ButtonPrimary))
            {
                _confirmSellMany = true;
            }

            GUI.enabled = enabled;
        }

        private void DrawSellManyDialog(UiSkin skin, float screenWidth, float screenHeight)
        {
            if (_sellSet.Count == 0)
            {
                _confirmSellMany = false;
                return;
            }

            var preview = CurrentSale();
            var lines = preview.ProtectedFishNames.Take(6).ToList();
            var more = preview.ProtectedFishNames.Count - lines.Count;
            var extra = lines.Count == 0 ? 0f : 34f + lines.Count * 24f + (more > 0 ? 24f : 0f);
            var rect = Dialog(skin, screenWidth, screenHeight, 230f + extra);
            GUI.Label(new Rect(rect.x + 28, rect.y + 24, rect.width - 56, 30), GameTexts.Aquarium.SellManyTitle(_sellSet.Count), skin.Heading);
            GUI.Label(new Rect(rect.x + 28, rect.y + 64, rect.width - 56, 70), GameTexts.Aquarium.SellManyBody(Format.Number(preview.TotalCoins)), skin.Body);
            var y = rect.y + 134;
            if (lines.Count > 0)
            {
                GUI.Label(new Rect(rect.x + 28, y, rect.width - 56, 24), GameTexts.Aquarium.SellManyProtected, skin.SmallGold);
                y += 30;
                foreach (var line in lines)
                {
                    GUI.Label(new Rect(rect.x + 40, y, rect.width - 80, 22), "•  " + line, skin.Small);
                    y += 24;
                }

                if (more > 0)
                {
                    GUI.Label(new Rect(rect.x + 40, y, rect.width - 80, 22), GameTexts.Aquarium.AndMore(more), skin.SmallMuted);
                }
            }

            if (GUI.Button(new Rect(rect.x + 28, rect.yMax - 64, 150, 42), GameTexts.Dialogs.Cancel, skin.Button))
            {
                _confirmSellMany = false;
            }

            if (GUI.Button(new Rect(rect.xMax - 218, rect.yMax - 64, 190, 42), GameTexts.Aquarium.Sell, skin.ButtonPrimary))
            {
                _confirmSellMany = false;
                if (_root.SellFish(_sellSet.ToList()))
                {
                    if (_sellSet.Contains(_selectedId))
                    {
                        _selectedId = 0;
                        _selected = null;
                    }

                    StopMulti();
                }

                Reload();
            }
        }

        // ------------------------------------------------------------------ feeding

        private void StartFeeding()
        {
            _feeding = true;
            _feedFromAquarium = false;
            _foodBox.Clear();
            _foodFish.Clear();
            _feedPreview = null;
            _box = _root.GetFishingBox();
        }

        private void StopFeeding()
        {
            _feeding = false;
            _foodBox.Clear();
            _foodFish.Clear();
            _feedPreview = null;
        }

        private void DrawFoodPicker(UiSkin skin, Rect area)
        {
            GUI.Label(new Rect(area.x, area.y, area.width, 32), GameTexts.Aquarium.FeedTitle + ": " + _selected.SpeciesName, skin.Title);
            GUI.Label(new Rect(area.x + 2, area.y + 36, area.width, 22), GameTexts.Aquarium.FeedPickHint, skin.SmallMuted);

            var others = _aquarium.Fish.Where(f => f.FishId != _selectedId).ToList();
            var boxLabel = GameTexts.Aquarium.FeedFromBox + " (" + _box.Count + ")";
            var aquariumLabel = GameTexts.Aquarium.FeedFromAquarium + " (" + others.Count + ")";
            if (GUI.Button(new Rect(area.x, area.y + 70, 230, 32), boxLabel, _feedFromAquarium ? skin.Chip : skin.ChipActive))
            {
                _feedFromAquarium = false;
                _feedScroll = Vector2.zero;
            }

            if (GUI.Button(new Rect(area.x + 240, area.y + 70, 200, 32), aquariumLabel, _feedFromAquarium ? skin.ChipActive : skin.Chip))
            {
                _feedFromAquarium = true;
                _feedScroll = Vector2.zero;
            }

            var grid = new Rect(area.x - 4, area.y + 118, area.width + 8, area.height - 118);
            if (_feedFromAquarium)
            {
                DrawGrid(grid, others.Where(f => NameSearch.Matches(f.SpeciesName, _search)).ToList(), ref _feedScroll, (rect, fish) =>
                {
                    if (FishCard(skin, rect, fish.SpeciesId, fish.SpeciesName,
                        GameTexts.Player.LevelShort + " " + fish.Level + " · " + Format.SizeCm(fish.SizeCm) + " · " + fish.SizeCategoryName, (float)fish.SizePercentile,
                        GameTexts.Aquarium.FeedXp(Format.Number(fish.FeedXp)), fish.RarityId, fish.RarityName,
                        _foodFish.Contains(fish.FishId), fish.IsValuableFood, fish.SizeCategoryId, fish.SizeCategoryName))
                    {
                        Toggle(_foodFish, fish.FishId);
                    }
                });
            }
            else
            {
                DrawGrid(grid, _box.Where(c => NameSearch.Matches(c.SpeciesName, _search)).ToList(), ref _feedScroll, (rect, c) =>
                {
                    if (FishCard(skin, rect, c.SpeciesId, c.SpeciesName,
                        Format.SizeCm(c.SizeCm) + " · " + c.SizeCategoryName, (float)c.SizePercentile,
                        GameTexts.Aquarium.FeedXp(Format.Number(c.FeedXp)), c.RarityId, c.RarityName,
                        _foodBox.Contains(c.CatchId), c.IsValuableFood, c.SizeCategoryId, c.SizeCategoryName))
                    {
                        Toggle(_foodBox, c.CatchId);
                    }
                });
            }
        }

        private void DrawFeedPanel(UiSkin skin, Rect area)
        {
            GUI.Box(area, GUIContent.none, skin.Card);
            var x = area.x + 22;
            var w = area.width - 44;
            var y = area.y + 18;
            var fish = _selected;

            GUI.DrawTexture(new Rect(x, y, w, 100), Art.FishTexture(fish.SpeciesId), ScaleMode.ScaleToFit, true);
            y += 110;
            GUI.Label(new Rect(x, y, w, 28), fish.SpeciesName + " · " + GameTexts.Player.LevelShort + " " + fish.Level, skin.Heading);
            y += 34;
            skin.Bar(new Rect(x, y, w, 8), fish.XpToNext > 0 ? fish.Xp / (float)fish.XpToNext : 1f);
            GUI.Label(new Rect(x, y + 10, w, 20), GameTexts.Aquarium.Xp(Format.Number(fish.Xp), Format.Number(fish.XpToNext)), skin.SmallMuted);
            y += 44;

            var count = _foodBox.Count + _foodFish.Count;
            if (count == 0)
            {
                GUI.Label(new Rect(x, y, w, 24), GameTexts.Aquarium.FeedNothingSelected, skin.Body);
            }
            else
            {
                var preview = _feedPreview ?? (_feedPreview = _root.PreviewFeed(fish.FishId, _foodBox.ToList(), _foodFish.ToList()));
                if (!preview.Succeeded)
                {
                    GUI.Label(new Rect(x, y, w, 48), preview.ErrorMessage, skin.SmallGold);
                }
                else
                {
                    var p = preview.Value;
                    GUI.Label(new Rect(x, y, w, 24), GameTexts.Aquarium.FeedSummary(p.FoodCount, Format.Number(p.XpGained)), skin.BodyBold);
                    y += 28;
                    GUI.Label(new Rect(x, y, w, 24), GameTexts.Aquarium.FeedResult(p.LevelBefore, p.LevelAfter), skin.Body);
                    y += 28;
                    skin.Bar(new Rect(x, y, w, 8), p.XpToNextAfter > 0 ? p.XpAfter / (float)p.XpToNextAfter : 1f, p.XpToNextAfter == 0);
                    GUI.Label(new Rect(x, y + 10, w, 20), p.XpToNextAfter > 0
                        ? GameTexts.Aquarium.Xp(Format.Number(p.XpAfter), Format.Number(p.XpToNextAfter))
                        : GameTexts.Aquarium.MaxLevelReached, skin.SmallMuted);
                    y += 40;
                    if (p.WastedXp > 0)
                    {
                        GUI.Label(new Rect(x, y, w, 40), GameTexts.Aquarium.Wasted(Format.Number(p.WastedXp)), skin.SmallGold);
                    }
                }
            }

            var buttons = area.yMax - 60;
            if (GUI.Button(new Rect(x, buttons, w / 2f - 6, 42), GameTexts.Aquarium.Back, skin.Button))
            {
                StopFeeding();
                return;
            }

            var ready = _feedPreview != null && _feedPreview.Succeeded;
            var enabled = GUI.enabled;
            GUI.enabled = enabled && ready;
            if (GUI.Button(new Rect(x + w / 2f + 6, buttons, w / 2f - 6, 42), GameTexts.Aquarium.FeedConfirm, skin.ButtonPrimary))
            {
                if (_feedPreview.Value.NeedsConfirmation)
                {
                    _confirmFeed = true;
                }
                else
                {
                    DoFeed();
                }
            }

            GUI.enabled = enabled;
        }

        private void DoFeed()
        {
            if (_root.Feed(_selectedId, _foodBox.ToList(), _foodFish.ToList()))
            {
                StopFeeding();
            }

            Reload();
        }

        // ------------------------------------------------------------------ dialogs

        private void DrawSellDialog(UiSkin skin, float screenWidth, float screenHeight)
        {
            var fish = _selected;
            if (fish == null)
            {
                _confirmSell = false;
                return;
            }

            var rect = Dialog(skin, screenWidth, screenHeight, fish.CardumePosition > 0 ? 262f : 230f);
            GUI.Label(new Rect(rect.x + 28, rect.y + 24, rect.width - 56, 30), GameTexts.Aquarium.SellTitle(fish.SpeciesName), skin.Heading);
            GUI.Label(new Rect(rect.x + 28, rect.y + 64, rect.width - 56, 70), GameTexts.Aquarium.SellBody(Format.Number(fish.SalePriceCoins)), skin.Body);
            if (fish.CardumePosition > 0)
            {
                GUI.Label(new Rect(rect.x + 28, rect.y + 136, rect.width - 56, 24), GameTexts.Aquarium.LeavesCardume(fish.CardumePosition), skin.SmallGold);
            }

            if (GUI.Button(new Rect(rect.x + 28, rect.yMax - 64, 150, 42), GameTexts.Dialogs.Cancel, skin.Button))
            {
                _confirmSell = false;
            }

            if (GUI.Button(new Rect(rect.xMax - 218, rect.yMax - 64, 190, 42), GameTexts.Aquarium.Sell, skin.ButtonPrimary))
            {
                _confirmSell = false;
                if (_root.SellFish(new[] { fish.FishId }))
                {
                    _selectedId = 0;
                    _selected = null;
                }

                Reload();
            }
        }

        private void DrawFeedDialog(UiSkin skin, float screenWidth, float screenHeight)
        {
            // The preview is dropped whenever the box or the Aquarium changes (e.g. a new catch); ask again.
            _feedPreview = _feedPreview ?? _root.PreviewFeed(_selectedId, _foodBox.ToList(), _foodFish.ToList());
            if (!_feedPreview.Succeeded)
            {
                _confirmFeed = false;
                return;
            }

            var p = _feedPreview.Value;
            var lines = p.ValuableFood.Concat(p.CardumeFood).Take(6).ToList();
            var rect = Dialog(skin, screenWidth, screenHeight, 210f + lines.Count * 26f + (p.WastedXp > 0 ? 40f : 0f));
            GUI.Label(new Rect(rect.x + 28, rect.y + 24, rect.width - 56, 30), GameTexts.Aquarium.FeedValuableTitle, skin.Heading);

            var y = rect.y + 64;
            if (lines.Count > 0)
            {
                GUI.Label(new Rect(rect.x + 28, y, rect.width - 56, 24), p.CardumeFood.Count > 0 && p.ValuableFood.Count == 0 ? GameTexts.Aquarium.FeedCardumeBody : GameTexts.Aquarium.FeedValuableBody, skin.Body);
                y += 30;
                foreach (var line in lines)
                {
                    GUI.Label(new Rect(rect.x + 40, y, rect.width - 80, 24), "•  " + line, skin.Small);
                    y += 26;
                }
            }

            if (p.WastedXp > 0)
            {
                GUI.Label(new Rect(rect.x + 28, y + 4, rect.width - 56, 36), GameTexts.Aquarium.Wasted(Format.Number(p.WastedXp)), skin.SmallGold);
            }

            if (GUI.Button(new Rect(rect.x + 28, rect.yMax - 64, 150, 42), GameTexts.Dialogs.Cancel, skin.Button))
            {
                _confirmFeed = false;
            }

            if (GUI.Button(new Rect(rect.xMax - 218, rect.yMax - 64, 190, 42), GameTexts.Dialogs.ConfirmButton, skin.ButtonPrimary))
            {
                _confirmFeed = false;
                DoFeed();
            }
        }

        private static Rect Dialog(UiSkin skin, float screenWidth, float screenHeight, float height)
        {
            var rect = new Rect(screenWidth / 2f - 320, screenHeight / 2f - height / 2f, 640, height);
            GUI.DrawTexture(new Rect(0, 0, screenWidth, screenHeight), skin.Overlay);
            skin.DrawShadow(rect);
            GUI.Box(rect, GUIContent.none, skin.PanelSolid);
            return rect;
        }

        // ------------------------------------------------------------------ helpers

        private void Reload()
        {
            _dirty = false;
            _aquarium = _root.GetAquarium(_sort) ?? new AquariumView();
            var kept = new HashSet<long>(_aquarium.Fish.Select(f => f.FishId));
            if (_multi)
            {
                _sellSet.RemoveWhere(id => !kept.Contains(id));
                _salePreview = null;
            }

            // Fish that left stop swimming; names and levels may have changed (feeding).
            foreach (var gone in _swimPhase.Keys.Where(id => !kept.Contains(id)).ToList())
            {
                _swimPhase.Remove(gone);
            }

            _tileText.Clear();
            _cardText.Clear();
            for (var k = 0; k < _statMax.Length; k++)
            {
                _statMax[k] = 0d;
            }

            foreach (var f in _aquarium.Fish)
            {
                if (f.Stats == null)
                {
                    continue;
                }

                _statMax[0] = Math.Max(_statMax[0], f.Stats.Hp);
                _statMax[1] = Math.Max(_statMax[1], f.Stats.Attack);
                _statMax[2] = Math.Max(_statMax[2], f.Stats.Defense);
                _statMax[3] = Math.Max(_statMax[3], f.Stats.Speed);
            }

            RefreshShown();

            _selected = _aquarium.Fish.FirstOrDefault(f => f.FishId == _selectedId);
            if (_selected == null)
            {
                _selectedId = 0;
                if (_feeding)
                {
                    StopFeeding();
                }
            }

            if (_feeding)
            {
                _box = _root.GetFishingBox();
                var present = new HashSet<long>(_box.Select(c => c.CatchId));
                _foodBox.RemoveWhere(id => !present.Contains(id));
                var fish = new HashSet<long>(_aquarium.Fish.Select(f => f.FishId));
                _foodFish.RemoveWhere(id => !fish.Contains(id));
                _feedPreview = null;
            }
        }

        /// <summary>The fish that match the search, in the current order; the first ones swim.</summary>
        private void RefreshShown()
        {
            _shown = _aquarium == null ? new List<FishView>() : _aquarium.Fish.Where(f => NameSearch.Matches(f.SpeciesName, _search)).ToList();
            _logMinCm = float.MaxValue;
            _logMaxCm = float.MinValue;
            for (var i = 0; i < _shown.Count && i < MaxSwimming; i++)
            {
                var cm = LogCm(_shown[i]);
                _logMinCm = Mathf.Min(_logMinCm, cm);
                _logMaxCm = Mathf.Max(_logMaxCm, cm);
            }
        }

        private void Toggle(HashSet<long> set, long id)
        {
            if (!set.Remove(id))
            {
                set.Add(id);
            }

            _feedPreview = null;
        }

        private static string SortName(AquariumSort sort)
        {
            switch (sort)
            {
                case AquariumSort.Level: return GameTexts.Aquarium.SortLevel;
                case AquariumSort.Species: return GameTexts.Aquarium.SortSpecies;
                case AquariumSort.Newest: return GameTexts.Aquarium.SortNewest;
                default: return GameTexts.Aquarium.SortSize;
            }
        }

        /// <summary>A scrolling card grid that only draws the rows on screen.</summary>
        private static void DrawGrid<T>(Rect area, IReadOnlyList<T> items, ref Vector2 scroll, Action<Rect, T> drawCard)
        {
            var columns = Mathf.Max(1, Mathf.FloorToInt((area.width - 20 + Gap) / (CardWidth + Gap)));
            var rows = Mathf.CeilToInt(items.Count / (float)columns);
            var content = new Rect(0, 0, area.width - 20, rows * (CardHeight + Gap));

            scroll = GUI.BeginScrollView(area, scroll, content);
            var firstRow = Mathf.Max(0, Mathf.FloorToInt(scroll.y / (CardHeight + Gap)));
            var lastRow = Mathf.Min(rows - 1, Mathf.CeilToInt((scroll.y + area.height) / (CardHeight + Gap)));
            for (var row = firstRow; row <= lastRow; row++)
            {
                for (var col = 0; col < columns; col++)
                {
                    var index = row * columns + col;
                    if (index >= items.Count)
                    {
                        break;
                    }

                    drawCard(new Rect(col * (CardWidth + Gap), row * (CardHeight + Gap), CardWidth, CardHeight), items[index]);
                }
            }

            GUI.EndScrollView();
        }

        /// <summary>One fish card (the official template). Returns true when clicked.</summary>
        private static bool FishCard(UiSkin skin, Rect rect, string speciesId, string name, string line, float sizeFraction,
            string corner, string rarityId, string rarityName, bool selected, bool important, string sizeCategoryId, string sizeCategoryName)
        {
            return UI.FishCard.Draw(skin, rect, new FishCardModel
            {
                SpeciesId = speciesId,
                Name = name,
                Line = line,
                RarityId = rarityId,
                RarityName = rarityName,
                SizeCategoryId = sizeCategoryId,
                SizeCategoryName = sizeCategoryName,
                Bar = sizeFraction,
                Footer = corner,
                Selected = selected,
            });
        }
    }
}
