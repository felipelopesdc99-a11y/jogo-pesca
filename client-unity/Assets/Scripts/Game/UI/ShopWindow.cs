using System.Collections.Generic;
using FishingIdle.Game.Bootstrap;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Shop;
using FishingIdle.Game.Visual;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The Shop (GDD section 7): rods, and since V0.2 boats and baits (docs/SISTEMA_SUCESSO_PESCA.md), drawn as the
    /// "Balcão do Píer" (addendum A-150). Prices, bonuses, requirements and blockers come from the Shop and Gear
    /// services (rods.json, equipment.json); nothing here decides anything.
    /// </summary>
    /// <remarks>
    /// Layout: a navy plank wall behind everything with a turquoise beam on top; the tabs are small plaques hanging
    /// from the beam. On the left, the hook wall: one shelf per item, lying on two brass hooks, with a name plate and a
    /// hanging tag (price, "JÁ É SUA", "EM USO" or a padlock with "Nv. X"). In the middle, the pier counter with a
    /// pedestal and a soft aura where the chosen item rests; on the right, its sheet in the rope-framed card, with the
    /// bonuses as bars and Comprar / Usar. At the bottom, "Seu equipamento" in an open tackle box and the chance of
    /// pulling each rarity as five bobbers on the water.
    /// Choosing an item makes it leave its hook (the hook says "no balcão") and fly in a short arc to the pedestal,
    /// 0.6 s, eased out, real UI time, once; the card slides in from the right. No loops.
    /// </remarks>
    public sealed class ShopWindow
    {
        public enum Tab
        {
            Rods,
            Boats,
            Baits,
            Vip,
        }

        private enum TagKind
        {
            Price,
            Free,
            Owned,
            InUse,
            Locked,
        }

        /// <summary>One item on the hook wall, whatever the tab: only what the wall and the counter draw.</summary>
        private struct WallItem
        {
            public string Id, Name, Sub, Art, Icon, OwnedText;
            public int Tier, Level;
            public long Coins, Shells;
            public TagKind Tag;
            public bool Square, Turned;
        }

        // Layout (virtual canvas). The window is the common 1320×840 frame: the content is 1264×736 on any screen of
        // 4:3 or wider (the HUD scales a 1080-tall canvas), a little narrower on 5:4.
        private const float BeamHeight = 14f, TabRope = 10f, TabHeight = 40f, TabGap = 10f, BodyTop = BeamHeight + TabRope + TabHeight + 12f;
        private const float Gap = 16f, DockHeight = 184f, LidHeight = 14f;
        private const float RowGap = 6f, MinRow = 92f, MaxRow = 124f, PlateWidth = 146f, ShelfBoard = 6f;
        private const float CardPad = 32f, CardPadTop = 34f;

        // Presentation timing (A-150): the flight, then the aura and the card.
        private const float FlightSeconds = 0.6f, TrailFade = 0.25f, CardDelay = 0.15f, CardSeconds = 0.35f, CardSlide = 24f;
        private const float AuraDelay = 0.35f, AuraSeconds = 0.3f, HookAngle = 28f;

        // Art (Resources/Arte/UI). Every piece falls back to the drawing in this file when its file is missing.
        // ASSET_PENDENTE: ui_loja_parede_noite (tile), ui_loja_gancho, ui_loja_balcao_pier (9-slice), ui_loja_caixa
        // (9-slice) and ui_loja_boia (two halves) — requests in docs/ASSETS_PENDENTES.md. The card frame already exists.
        private const string WallArt = "UI/ui_loja_parede_noite", HookArt = "UI/ui_loja_gancho", CounterArt = "UI/ui_loja_balcao_pier",
            BoxArt = "UI/ui_loja_caixa", BobberArt = "UI/ui_loja_boia", CardArt = "UI/ui_exp_moldura_carta";

        /// <summary>9-slice borders in texture pixels: the card frame (412×600; rope knots in the corners), the counter
        /// (1024×96, sides only) and the tackle box (512×288).</summary>
        private const float CardArtBorder = 84f, CardArtScale = 0.5f, CounterArtSide = 48f, BoxArtBorder = 40f, BoxArtScale = 0.35f;

        // Colours of the Mescla B page the owner approved (Art Bible palette: night blues and turquoise).
        private static readonly Color PlankA = Rgb(0x132A45), PlankB = Rgb(0x15304E), PlankSeam = Rgb(0x0B1D33);
        private static readonly Color BeamTop = Rgb(0x2A8B98), BeamBottom = Rgb(0x1B5D68), Rope = Rgb(0xC9A77A);
        private static readonly Color TabTop = Rgb(0x1D4266), TabBottom = Rgb(0x173757), TabSeam = Rgb(0x0D2440), TabEdge = Rgb(0x2A5480);
        private static readonly Color PlateTop = Rgb(0x20435F), PlateBottom = Rgb(0x152F47), PlateEdge = Rgb(0x2F5F86);
        private static readonly Color Shelf = Rgb(0x25476B), Brass = Rgb(0xC9A45A), TagString = Rgb(0xD8CDB6), TagHole = Rgb(0x173244);
        private static readonly Color TagDark = Rgb(0x0E3A40), TagLocked = Rgb(0x2A3646), TealLight = Rgb(0x3AD6D2), TealInk = Rgb(0x06313A);
        private static readonly Color CounterTop = Rgb(0x2A5A86), CounterMid = Rgb(0x183A5E), CounterBottom = Rgb(0x0E2442);
        private static readonly Color BoxTop = Rgb(0x2F6A7A), BoxBottom = Rgb(0x1F4F5E), BoxEdge = Rgb(0x133A46), LidTop = Rgb(0x3A7D8E), LidBottom = Rgb(0x2A6474);
        private static readonly Color Well = Rgb(0x0B2230), BoxInk = Rgb(0xE9FBFA), WaterTop = Rgb(0x0F2A47), WaterA = Rgb(0x0D3A5C), WaterB = Rgb(0x0A2A48);
        private static readonly Color WaterLine = Rgb(0x1B5D7A), BobberWhite = Rgb(0xF4F4F4), BobberBand = Rgb(0x2A3646), Antenna = Rgb(0xDDDDDD);
        private static readonly Color Silhouette = new Color(0.02f, 0.05f, 0.09f, 0.75f), Ghost = new Color(0.62f, 0.68f, 0.75f, 0.13f);
        private static readonly Color Lying = new Color(0.45f, 0.50f, 0.58f, 0.6f);

        private readonly GameRoot _root;
        private readonly List<WallItem> _items = new List<WallItem>();
        private readonly string[] _selected = new string[4];
        private ShopView _shop;
        private bool _dirty = true;
        private Tab _tab;
        private Vector2 _wallScroll;
        private int _scrollTo = -1;

        // The flight: when it started, where the item left from (its hook, window coordinates) and where it lands.
        private bool _animPending;
        private float _animStart = -10f;
        private Rect _flySource, _heroRect;
        private bool _hasSource;
        private float _flyAngle;

        private GUIStyle _plaque, _percent, _boldCenter, _mutedTop, _bonusBig;

        public ShopWindow(GameRoot root)
        {
            _root = root;
            _root.AquariumChanged += () => _dirty = true;
            _root.BoxChanged += () => _dirty = true;
        }

        public bool IsOpen { get; private set; }

        public void Open(Tab tab = Tab.Rods)
        {
            IsOpen = true;
            _dirty = true;
            _tab = tab;
            _animPending = true;
        }

        public void Close() => IsOpen = false;

        /// <summary>The Shop has no confirmation dialog of its own.</summary>
        public bool HasDialog => false;

        public void Draw(UiSkin skin, float screenWidth, float screenHeight)
        {
            if (!IsOpen)
            {
                return;
            }

            if (_dirty)
            {
                _dirty = false;
                _shop = _root.GetShop();
            }

            var gear = _root.Gear;
            if (_shop == null || gear == null)
            {
                return;
            }

            EnsureStyles(skin);

            // The long sentences go behind the "i": what the tab is about, then how the chance works (A-150).
            var chanceNote = GameTexts.Gear.ChanceNote(Format.Percent(gear.ChanceMin, 0), Format.Percent(gear.ChanceMax, 0));
            var area = WindowFrame.Draw(skin, screenWidth, screenHeight, GameTexts.Shop.Title, GameTexts.Shop.Info(TabNote(), chanceNote), out var closed, 1320f, 840f, Icons.Shop);
            if (closed)
            {
                Close();
                return;
            }

            if (_animPending)
            {
                _animPending = false;
                _animStart = Time.unscaledTime;
            }

            DrawWallBackground(skin, area);
            DrawBeam(skin, new Rect(area.x, area.y, area.width, BeamHeight));
            DrawTabs(skin, area.x + 14f, area.y + BeamHeight);

            var body = new Rect(area.x, area.y + BodyTop, area.width, area.height - BodyTop);
            if (_tab == Tab.Vip)
            {
                DrawVip(skin, new Rect(body.x, body.y, body.width, body.height - DockHeight - Gap));
                DrawDock(skin, new Rect(body.x, body.yMax - DockHeight, body.width, DockHeight), gear);
                return;
            }

            BuildItems(gear);
            var selected = SelectedIndex();

            var wallWidth = Mathf.Clamp(body.width * 0.33f, 340f, 430f);
            var wall = new Rect(body.x, body.y, wallWidth, body.height);
            var right = new Rect(wall.xMax + Gap, body.y, body.xMax - wall.xMax - Gap, body.height);
            var dock = new Rect(right.x, right.yMax - DockHeight, right.width, DockHeight);
            var stageHeight = dock.y - Gap - right.y;
            var cardWidth = Mathf.Clamp(right.width * 0.42f, 330f, 380f);
            var counter = new Rect(right.x, right.y, right.width - cardWidth - Gap, stageHeight);
            var card = new Rect(right.xMax - cardWidth, right.y, cardWidth, stageHeight);

            _hasSource = false;
            DrawWall(skin, wall, selected);
            selected = SelectedIndex();
            DrawCounter(skin, counter, selected);
            DrawCard(skin, card, gear, selected);
            DrawDock(skin, dock, gear);
            DrawFlight(skin, selected);
        }

        /// <summary>The sentence of the current tab for the "i".</summary>
        private string TabNote()
        {
            switch (_tab)
            {
                case Tab.Boats: return GameTexts.Gear.BoatsNote;
                case Tab.Baits: return GameTexts.Gear.BaitsNote;
                case Tab.Vip: return GameTexts.Vip.Note;
                default: return GameTexts.Shop.Note;
            }
        }

        // ------------------------------------------------------------------ items of the tab

        private void BuildItems(GearView gear)
        {
            _items.Clear();
            switch (_tab)
            {
                case Tab.Boats:
                    foreach (var boat in gear.Boats)
                    {
                        _items.Add(new WallItem
                        {
                            Id = boat.BoatId, Name = boat.Name, Sub = GameTexts.Gear.Bonus(Format.Percent(boat.Bonus, 0)),
                            Art = "Barcos/" + boat.BoatId, Icon = Icons.Boat, Tier = boat.Tier, Level = boat.UnlockFisherLevel,
                            Coins = boat.CostCoins, Shells = boat.CostShells, OwnedText = GameTexts.Shop.OwnedBoat,
                            Tag = boat.InUse ? TagKind.InUse : boat.Owned ? TagKind.Owned : boat.BuyBlocker == ServiceError.BoatLocked ? TagKind.Locked : TagKind.Price,
                        });
                    }

                    break;
                case Tab.Baits:
                    foreach (var bait in gear.Baits)
                    {
                        _items.Add(new WallItem
                        {
                            Id = bait.BaitId, Name = bait.Name, Sub = GameTexts.Gear.Bonus(Format.Percent(bait.Bonus, 0)),
                            Art = "Iscas/" + bait.BaitId, Icon = Icons.Bait, Tier = bait.Tier, Level = bait.UnlockFisherLevel,
                            Coins = bait.CostCoins, Shells = bait.CostShells, Square = true,
                            Tag = bait.InUse ? TagKind.InUse : bait.BuyBlocker == ServiceError.BaitLocked ? TagKind.Locked : TagKind.Price,
                        });
                    }

                    break;
                default:
                    foreach (var rod in _shop.Rods)
                    {
                        var inUse = rod.Owned && rod.RodId == gear.RodId;
                        _items.Add(new WallItem
                        {
                            Id = rod.RodId, Name = rod.Name, Sub = GameTexts.Profile.Tier(rod.Tier),
                            Art = "Varas/" + rod.RodId, Icon = Icons.Rod, Tier = rod.Tier, Level = rod.UnlockFisherLevel,
                            Coins = rod.PriceCoins, Shells = rod.PriceShells, OwnedText = GameTexts.Shop.Owned, Turned = true,
                            Tag = inUse ? TagKind.InUse : rod.Owned ? TagKind.Owned : rod.BuyBlocker == ServiceError.RodLocked ? TagKind.Locked : rod.IsFree ? TagKind.Free : TagKind.Price,
                        });
                    }

                    break;
            }
        }

        /// <summary>The chosen item of the tab; the one in use (or the first) when nothing is chosen yet.</summary>
        private int SelectedIndex()
        {
            var id = _selected[(int)_tab];
            var fallback = -1;
            for (var i = 0; i < _items.Count; i++)
            {
                if (_items[i].Id == id)
                {
                    return i;
                }

                if (fallback < 0 && _items[i].Tag == TagKind.InUse)
                {
                    fallback = i;
                }
            }

            if (_items.Count == 0)
            {
                return -1;
            }

            fallback = Mathf.Max(0, fallback);
            _selected[(int)_tab] = _items[fallback].Id;
            return fallback;
        }

        private void Select(int index, bool scrollToIt)
        {
            if (index < 0 || index >= _items.Count || _items[index].Id == _selected[(int)_tab])
            {
                return;
            }

            _selected[(int)_tab] = _items[index].Id;
            _animStart = Time.unscaledTime;
            if (scrollToIt)
            {
                _scrollTo = index;
            }
        }

        // ------------------------------------------------------------------ wall, beam and tab plaques

        /// <summary>The navy plank wall behind the whole content.</summary>
        private static void DrawWallBackground(UiSkin skin, Rect area)
        {
            var art = ArtAssets.Texture(WallArt);
            if (art != null)
            {
                // The tile repeats sideways at the content height (the art is loaded clamped, so one tile at a time).
                var tile = art.width * area.height / art.height;
                for (var x = area.x; x < area.xMax; x += tile)
                {
                    var w = Mathf.Min(tile, area.xMax - x);
                    GUI.DrawTextureWithTexCoords(new Rect(x, area.y, w, area.height), art, new Rect(0f, 0f, w / tile, 1f), true);
                }

                GUI.DrawTexture(area, skin.White, ScaleMode.StretchToFill, true, 0, new Color(UiSkin.Border.r, UiSkin.Border.g, UiSkin.Border.b, 0.6f), 1f, 0f);
                return;
            }

            // Vertical planks, alternating two night blues, with a dark seam between them.
            Fill(skin, area, PlankA, 10f);
            const float plank = 88f;
            var i = 0;
            for (var x = area.x; x < area.xMax - 1f; x += plank, i++)
            {
                var w = Mathf.Min(plank, area.xMax - x);
                if (i % 2 == 1)
                {
                    Fill(skin, new Rect(x, area.y, w, area.height), PlankB, 0f);
                }

                if (x > area.x)
                {
                    Fill(skin, new Rect(x - 1f, area.y, 2f, area.height), PlankSeam, 0f);
                }
            }

            GUI.DrawTexture(area, skin.White, ScaleMode.StretchToFill, true, 0, new Color(UiSkin.Border.r, UiSkin.Border.g, UiSkin.Border.b, 0.6f), 1f, 10f);
        }

        private static void DrawBeam(UiSkin skin, Rect r)
        {
            Fill(skin, new Rect(r.x, r.y + 4f, r.width, r.height), new Color(0f, 0f, 0f, 0.35f), 4f);
            Fill(skin, r, BeamBottom, 4f);
            Fill(skin, new Rect(r.x, r.y, r.width, r.height * 0.5f), BeamTop, 4f);
            Fill(skin, new Rect(r.x + 6f, r.y + 2f, r.width - 12f, 1f), new Color(1f, 1f, 1f, 0.15f), 0f);
        }

        /// <summary>The four tabs as small plaques hanging from the beam by two ropes; the active one with a turquoise edge.</summary>
        private void DrawTabs(UiSkin skin, float x, float beamBottom)
        {
            var tabs = new[] { (Tab.Rods, GameTexts.Gear.TabRods, Icons.Rod), (Tab.Boats, GameTexts.Gear.TabBoats, Icons.Boat), (Tab.Baits, GameTexts.Gear.TabBaits, Icons.Bait), (Tab.Vip, GameTexts.Vip.Tab, Icons.Dollar) };
            foreach (var (tab, label, icon) in tabs)
            {
                var active = _tab == tab;
                var width = 16f + 20f + 8f + _plaque.CalcSize(new GUIContent(label)).x + 18f;
                var r = new Rect(x, beamBottom + TabRope, width, TabHeight);
                var hover = GUI.enabled && r.Contains(Event.current.mousePosition);

                Fill(skin, new Rect(r.x + width * 0.22f - 1.5f, beamBottom - 2f, 3f, TabRope + 4f), Rope, 0f);
                Fill(skin, new Rect(r.x + width * 0.78f - 1.5f, beamBottom - 2f, 3f, TabRope + 4f), Rope, 0f);
                if (active)
                {
                    skin.DrawGlow(r, UiSkin.Accent, 0.3f);
                }

                Fill(skin, new Rect(r.x + 1f, r.y + 5f, r.width, r.height), new Color(0f, 0f, 0f, 0.35f), 6f);
                Fill(skin, r, TabBottom, 6f);
                GUI.DrawTexture(new Rect(r.x, r.y, r.width, r.height * 0.48f), skin.White, ScaleMode.StretchToFill, true, 0, TabTop, Vector4.zero, new Vector4(6f, 6f, 0f, 0f));
                Fill(skin, new Rect(r.x + 2f, r.y + r.height * 0.48f, r.width - 4f, 2f), TabSeam, 0f);
                GUI.DrawTexture(r, skin.White, ScaleMode.StretchToFill, true, 0, active ? UiSkin.Accent : hover ? UiSkin.AccentHover : TabEdge, 2f, 6f);

                skin.DrawIcon(new Rect(r.x + 16f, r.y + 10f, 20f, 20f), icon, active ? Color.white : UiSkin.Muted);
                var previous = GUI.contentColor;
                GUI.contentColor = active ? Color.white : UiSkin.Muted;
                GUI.Label(new Rect(r.x + 44f, r.y, r.width - 50f, r.height), label, _plaque);
                GUI.contentColor = previous;

                if (GUI.Button(r, GUIContent.none, GUIStyle.none) && !active)
                {
                    _tab = tab;
                    _animStart = Time.unscaledTime;
                }

                x += width + TabGap;
            }
        }

        // ------------------------------------------------------------------ the hook wall

        private void DrawWall(UiSkin skin, Rect wall, int selected)
        {
            var count = _items.Count;
            if (count == 0)
            {
                return;
            }

            // The tag column fits the widest tag of the tab.
            var tagWidth = 0f;
            foreach (var item in _items)
            {
                tagWidth = Mathf.Max(tagWidth, TagWidth(skin, item));
            }

            var rowHeight = Mathf.Clamp((wall.height - (count - 1) * RowGap) / count, MinRow, MaxRow);
            var total = count * rowHeight + (count - 1) * RowGap;
            var scrolls = total > wall.height + 0.5f;
            var width = scrolls ? wall.width - 18f : wall.width;
            var origin = wall.position;
            if (scrolls)
            {
                if (_scrollTo >= 0)
                {
                    _wallScroll.y = Mathf.Clamp(_scrollTo * (rowHeight + RowGap) - (wall.height - rowHeight) / 2f, 0f, total - wall.height);
                }

                _wallScroll = GUI.BeginScrollView(wall, _wallScroll, new Rect(0f, 0f, width, total));
                origin = Vector2.zero;
            }

            _scrollTo = -1;
            for (var i = 0; i < count; i++)
            {
                var row = new Rect(origin.x, origin.y + i * (rowHeight + RowGap), width, rowHeight);

                // Inside a scroll view the items are not turned (the GUI matrix would turn them around the wrong point).
                var itemRect = DrawShelf(skin, row, _items[i], i == selected, Mathf.Min(tagWidth, width * 0.36f), !scrolls);
                if (i == selected)
                {
                    _flySource = scrolls ? new Rect(wall.x + itemRect.x - _wallScroll.x, wall.y + itemRect.y - _wallScroll.y, itemRect.width, itemRect.height) : itemRect;
                    _flyAngle = _items[i].Turned && !scrolls ? HookAngle : 0f;
                    _hasSource = true;
                }

                if (GUI.Button(row, GUIContent.none, GUIStyle.none))
                {
                    Select(i, false);
                }
            }

            if (scrolls)
            {
                GUI.EndScrollView();
            }
        }

        /// <summary>One shelf: name plate, the item lying on two hooks and the hanging tag. Returns the item's rect.</summary>
        private static Rect DrawShelf(UiSkin skin, Rect row, WallItem item, bool chosen, float tagColumn, bool turn)
        {
            var hover = GUI.enabled && row.Contains(Event.current.mousePosition);
            if (chosen)
            {
                Fill(skin, row, new Color(UiSkin.Accent.r, UiSkin.Accent.g, UiSkin.Accent.b, 0.10f), 6f);
                GUI.DrawTexture(row, skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.Accent, 1.5f, 6f);
            }
            else if (hover)
            {
                Fill(skin, row, new Color(1f, 1f, 1f, 0.04f), 6f);
            }

            // The shelf board under the row.
            Fill(skin, new Rect(row.x, row.yMax - ShelfBoard, row.width, ShelfBoard), Shelf, 2f);
            Fill(skin, new Rect(row.x + 2f, row.yMax, row.width - 4f, 3f), new Color(0f, 0f, 0f, 0.35f), 0f);

            var midY = row.y + (row.height - ShelfBoard) / 2f;

            // Name plate with a stripe in the tier colour.
            var plate = new Rect(row.x + 8f, midY - 25f, PlateWidth, 50f);
            Fill(skin, plate, PlateBottom, 6f);
            GUI.DrawTexture(new Rect(plate.x, plate.y, plate.width, plate.height * 0.5f), skin.White, ScaleMode.StretchToFill, true, 0, PlateTop, Vector4.zero, new Vector4(6f, 6f, 0f, 0f));
            GUI.DrawTexture(plate, skin.White, ScaleMode.StretchToFill, true, 0, PlateEdge, 1.5f, 6f);
            Fill(skin, new Rect(plate.x + 4f, plate.y + 8f, 3f, plate.height - 16f), UiSkin.TierColor(item.Tier), 1.5f);
            GUI.Label(new Rect(plate.x + 12f, plate.y + 4f, plate.width - 18f, UiSkin.SmallLine), FishCard.Fit(item.Name, skin.SmallBold, plate.width - 18f), skin.SmallBold);
            GUI.Label(new Rect(plate.x + 12f, plate.y + 25f, plate.width - 18f, UiSkin.SmallLine), FishCard.Fit(item.Sub, skin.SmallMuted, plate.width - 18f), skin.SmallMuted);

            // The tag hangs by a string from the top of the row.
            var tagCol = new Rect(row.xMax - tagColumn - 8f, row.y, tagColumn, row.height - ShelfBoard);
            DrawTag(skin, tagCol, item, row.y);

            // Mount: two brass hooks and the item lying on them.
            var mx = plate.xMax + 6f;
            var mw = Mathf.Max(40f, tagCol.x - 6f - mx);
            Hook(skin, mx + mw * 0.28f, midY - 8f);
            Hook(skin, mx + mw * 0.64f, midY - 8f);

            Rect itemRect;
            if (item.Square)
            {
                var s = Mathf.Min(row.height - ShelfBoard - 16f, 72f);
                itemRect = new Rect(mx + mw / 2f - s / 2f, midY - s / 2f + 2f, s, s);
            }
            else
            {
                var w = Mathf.Min(mw + 24f, 156f);
                itemRect = new Rect(mx + mw / 2f - w / 2f, midY - w / 4f + 2f, w, w / 2f);
            }

            var angle = item.Turned && turn ? HookAngle : 0f;
            if (chosen)
            {
                // The item is on the counter: only a faint mark stays, with "no balcão".
                DrawItemArt(skin, itemRect, item, angle, Ghost);
                var text = GameTexts.Shop.OnCounter;
                var tw = skin.SmallMutedCenter.CalcSize(new GUIContent(text)).x + 20f;
                var pill = new Rect(mx + mw / 2f - tw / 2f, midY - 12f, tw, 24f);
                Fill(skin, pill, new Color(PlankSeam.r, PlankSeam.g, PlankSeam.b, 0.8f), 10f);
                skin.DashedFrame(pill, UiSkin.Accent, 5f, 4f, 1.5f);
                var previous = GUI.contentColor;
                GUI.contentColor = TealLight;
                GUI.Label(pill, text, skin.SmallMutedCenter);
                GUI.contentColor = previous;
            }
            else
            {
                DrawItemArt(skin, itemRect, item, angle, item.Tag == TagKind.Locked ? Silhouette : Color.white);
            }

            return itemRect;
        }

        private static void Hook(UiSkin skin, float cx, float top)
        {
            var art = ArtAssets.Texture(HookArt);
            if (art != null)
            {
                GUI.DrawTexture(new Rect(cx - 9f, top - 4f, 18f, 18f), art, ScaleMode.ScaleToFit, true);
                return;
            }

            Fill(skin, new Rect(cx - 1f, top - 6f, 2f, 8f), Brass, 0f);
            GUI.DrawTexture(new Rect(cx - 5f, top + 1f, 10f, 9f), skin.White, ScaleMode.StretchToFill, true, 0, Brass, new Vector4(2f, 0f, 2f, 2f), new Vector4(0f, 0f, 5f, 5f));
        }

        private static float TagWidth(UiSkin skin, WallItem item)
        {
            return TagContentWidth(skin, item) + 28f;
        }

        private static float TagContentWidth(UiSkin skin, WallItem item)
        {
            switch (item.Tag)
            {
                case TagKind.Price:
                    // Coins and Conchas on two lines, so the tag stays narrow.
                    var width = skin.CoinAmountWidth(Format.Short(item.Coins), 20f, skin.SmallGoldLine);
                    if (item.Shells > 0)
                    {
                        width = Mathf.Max(width, 24f + skin.SmallGoldLine.CalcSize(new GUIContent(Format.Number(item.Shells))).x + 4f);
                    }

                    return width;
                case TagKind.Free:
                    return skin.SmallGoldLine.CalcSize(new GUIContent(GameTexts.Shop.Free)).x;
                case TagKind.Locked:
                    return 18f + 4f + skin.SmallMuted.CalcSize(new GUIContent(GameTexts.Shop.LevelTag(item.Level))).x;
                default:
                    return 18f + 4f + skin.PillText.CalcSize(new GUIContent(TagText(item))).x;
            }
        }

        private static string TagText(WallItem item)
        {
            return (item.Tag == TagKind.InUse ? GameTexts.Gear.InUse : item.OwnedText ?? GameTexts.Shop.Owned).ToUpperInvariant();
        }

        /// <summary>The tag hanging from the shelf: price, "JÁ É SUA" / "JÁ É SEU", "EM USO" or a padlock with "Nv. X".</summary>
        private static void DrawTag(UiSkin skin, Rect col, WallItem item, float stringTop)
        {
            var content = TagContentWidth(skin, item);
            var w = Mathf.Min(col.width, content + 28f);
            var h = item.Tag == TagKind.Price && item.Shells > 0 ? 48f : 28f;
            var tag = new Rect(col.xMax - w, col.center.y - h / 2f, w, h);
            Fill(skin, new Rect(tag.center.x - 1f, stringTop + 4f, 2f, tag.y - stringTop - 4f), new Color(TagString.r, TagString.g, TagString.b, 0.8f), 0f);

            Color fill, edge, ink;
            switch (item.Tag)
            {
                case TagKind.InUse: fill = UiSkin.Accent; edge = UiSkin.Accent; ink = TealInk; break;
                case TagKind.Locked: fill = TagLocked; edge = UiSkin.Border; ink = UiSkin.Muted; break;
                case TagKind.Owned: fill = TagDark; edge = UiSkin.Accent; ink = TealLight; break;
                default: fill = TagDark; edge = UiSkin.Accent; ink = UiSkin.Gold; break;
            }

            Fill(skin, new Rect(tag.x + 1f, tag.y + 3f, tag.width, tag.height), new Color(0f, 0f, 0f, 0.35f), 5f);
            Fill(skin, tag, fill, 5f);
            GUI.DrawTexture(tag, skin.White, ScaleMode.StretchToFill, true, 0, edge, 1.5f, 5f);
            Fill(skin, new Rect(tag.x + 6f, tag.center.y - 3f, 6f, 6f), TagHole, 3f);

            var x = tag.x + 18f;
            var line = new Rect(x, tag.y + 4f, tag.xMax - x - 6f, UiSkin.SmallLine);
            var previous = GUI.contentColor;
            switch (item.Tag)
            {
                case TagKind.Price:
                    var coins = Format.Short(item.Coins);
                    var cw = skin.CoinAmountWidth(coins, 20f, skin.SmallGoldLine);
                    skin.CoinAmount(new Rect(x, line.y, cw, 20f), coins, skin.SmallGoldLine);
                    if (item.Shells > 0)
                    {
                        var sy = line.y + 20f;
                        skin.CurrencyIcon(new Rect(x, sy, 20f, 20f), Icons.Shell);
                        var shells = Format.Number(item.Shells);
                        GUI.Label(new Rect(x + 24f, sy, skin.SmallGoldLine.CalcSize(new GUIContent(shells)).x + 4f, 20f), shells, skin.SmallGoldLine);
                    }

                    break;
                case TagKind.Free:
                    GUI.Label(line, GameTexts.Shop.Free, skin.SmallGoldLine);
                    break;
                case TagKind.Locked:
                    skin.DrawIcon(new Rect(x, line.y + 1f, 18f, 18f), Icons.Lock, UiSkin.Gold);
                    GUI.Label(new Rect(x + 22f, line.y, line.width - 22f, line.height), GameTexts.Shop.LevelTag(item.Level), skin.SmallMuted);
                    break;
                default:
                    skin.DrawIcon(new Rect(x, line.y + 1f, 18f, 18f), Icons.Check, ink);
                    GUI.contentColor = ink;
                    GUI.Label(new Rect(x + 22f, tag.y, line.width - 22f, tag.height), TagText(item), skin.PillText);
                    break;
            }

            GUI.contentColor = previous;
        }

        // ------------------------------------------------------------------ the counter

        /// <summary>"‹ 4 de 6 ›", the aura, the pedestal and the counter plank; the chosen item rests on the pedestal.</summary>
        private void DrawCounter(UiSkin skin, Rect counter, int selected)
        {
            if (selected < 0)
            {
                return;
            }

            var item = _items[selected];
            var cx = counter.center.x;

            // Position in the tab and the arrows.
            var label = GameTexts.Shop.Position(selected + 1, _items.Count);
            var lw = Mathf.Max(80f, skin.CenterBold.CalcSize(new GUIContent(label)).x + 16f);
            GUI.Label(new Rect(cx - lw / 2f, counter.y + 2f, lw, 34f), label, skin.CenterBold);
            if (HeroSheet.Arrow(skin, new Rect(cx - lw / 2f - 40f, counter.y + 2f, 34f, 34f), true, selected > 0))
            {
                Select(selected - 1, true);
            }

            if (HeroSheet.Arrow(skin, new Rect(cx + lw / 2f + 6f, counter.y + 2f, 34f, 34f), false, selected < _items.Count - 1))
            {
                Select(selected + 1, true);
            }

            // The counter plank along the bottom (a little wider than the column).
            var plank = new Rect(counter.x - 8f, counter.yMax - 34f, counter.width + 16f, 34f);
            DrawCounterPlank(skin, plank);

            var tier = UiSkin.TierColor(item.Tier);
            var pedestalWidth = Mathf.Min(counter.width * 0.6f, 300f);
            var pedestalTop = plank.y - 32f;

            if (item.Square)
            {
                var s = Mathf.Min(counter.width * 0.45f, 190f);
                _heroRect = new Rect(cx - s / 2f, pedestalTop - s + 6f, s, s);
            }
            else
            {
                var w = Mathf.Min(counter.width * 0.86f, 440f);
                _heroRect = new Rect(cx - w / 2f, pedestalTop - w / 2f + 10f, w, w / 2f);
            }

            var t = Time.unscaledTime - _animStart;
            var aura = Mathf.Clamp01((t - AuraDelay) / AuraSeconds);
            if (aura > 0f)
            {
                HeroSheet.Aura(skin, _heroRect.center, Mathf.Min(_heroRect.height * 1.7f, counter.width * 0.9f), tier, 0.09f * aura);
            }

            HeroSheet.Pedestal(skin, cx, pedestalTop, pedestalWidth, tier, 0.6f);

            // While it flies, the item is drawn by DrawFlight instead.
            if (!_hasSource || t >= FlightSeconds)
            {
                DrawItemArt(skin, _heroRect, item, 0f, item.Tag == TagKind.Locked ? Silhouette : Color.white);
            }
        }

        private static void DrawCounterPlank(UiSkin skin, Rect plank)
        {
            Fill(skin, new Rect(plank.x + 2f, plank.y + 8f, plank.width, plank.height), new Color(0f, 0f, 0f, 0.4f), 6f);
            var art = ArtAssets.Texture(CounterArt);
            if (art != null)
            {
                UiSkin.NineSlice(plank, art, CounterArtSide, CounterArtSide, 0f, 0f, plank.height / art.height);
                return;
            }

            Fill(skin, plank, CounterBottom, 6f);
            Fill(skin, new Rect(plank.x, plank.y, plank.width, plank.height * 0.62f), CounterMid, 6f);
            Fill(skin, new Rect(plank.x, plank.y, plank.width, plank.height * 0.3f), CounterTop, 6f);
            Fill(skin, new Rect(plank.x + 2f, plank.y, plank.width - 4f, 3f), UiSkin.Accent, 1.5f);
        }

        /// <summary>The item leaving its hook in a short arc to the pedestal, with a dotted trail that fades after landing.</summary>
        private void DrawFlight(UiSkin skin, int selected)
        {
            if (selected < 0 || !_hasSource)
            {
                return;
            }

            var t = Time.unscaledTime - _animStart;
            if (t >= FlightSeconds + TrailFade)
            {
                return;
            }

            var from = _flySource.center;
            var to = _heroRect.center;
            var control = new Vector2((from.x + to.x) / 2f, Mathf.Min(from.y, to.y) - 90f);
            var u = Mathf.Clamp01(t / FlightSeconds);
            var e = 1f - (1f - u) * (1f - u) * (1f - u);

            // The trail: dots along the path already travelled.
            var trail = t < FlightSeconds ? 0.5f : 0.5f * (1f - (t - FlightSeconds) / TrailFade);
            for (var i = 0; i <= 14; i++)
            {
                var s = i / 14f;
                if (s > e)
                {
                    break;
                }

                var p = Bezier(from, control, to, s);
                Fill(skin, new Rect(p.x - 2.5f, p.y - 2.5f, 5f, 5f), new Color(TealLight.r, TealLight.g, TealLight.b, trail), 2.5f);
            }

            if (t >= FlightSeconds)
            {
                return;
            }

            var centre = Bezier(from, control, to, e);
            var size = Vector2.Lerp(_flySource.size, _heroRect.size, e);
            var item = _items[selected];
            var color = item.Tag == TagKind.Locked ? Silhouette : Color.white;
            color.a *= Mathf.Lerp(0.4f, 1f, Mathf.Clamp01(u * 3f));
            DrawItemArt(skin, new Rect(centre.x - size.x / 2f, centre.y - size.y / 2f, size.x, size.y), item, Mathf.Lerp(_flyAngle, 0f, e), color);
        }

        private static Vector2 Bezier(Vector2 a, Vector2 b, Vector2 c, float t)
        {
            var k = 1f - t;
            return k * k * a + 2f * k * t * b + t * t * c;
        }

        /// <summary>The item's picture (turned by <paramref name="angle"/>), or its icon when the art is missing.</summary>
        private static void DrawItemArt(UiSkin skin, Rect rect, WallItem item, float angle, Color color)
        {
            var tex = ArtAssets.Texture(item.Art);
            if (tex == null)
            {
                var s = Mathf.Min(rect.width, rect.height) * 0.8f;
                skin.DrawIcon(new Rect(rect.center.x - s / 2f, rect.center.y - s / 2f, s, s), item.Icon, new Color(UiSkin.Muted.r, UiSkin.Muted.g, UiSkin.Muted.b, color.a));
                return;
            }

            if (Mathf.Abs(angle) < 0.01f)
            {
                GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit, true, 0, color, 0, 0);
                return;
            }

            var matrix = GUI.matrix;
            var p = new Vector3(rect.center.x, rect.center.y, 0f);
            GUI.matrix = matrix * Matrix4x4.TRS(p, Quaternion.Euler(0f, 0f, angle), Vector3.one) * Matrix4x4.TRS(-p, Quaternion.identity, Vector3.one);
            GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit, true, 0, color, 0, 0);
            GUI.matrix = matrix;
        }

        // ------------------------------------------------------------------ the card

        private void DrawCard(UiSkin skin, Rect card, GearView gear, int selected)
        {
            if (selected < 0)
            {
                return;
            }

            // Slides in from the right after the item leaves its hook (ease-out, once).
            var s = Mathf.Clamp01((Time.unscaledTime - _animStart - CardDelay) / CardSeconds);
            card.x += (1f - s) * (1f - s) * CardSlide;

            var inner = CardFrame(skin, card);
            var id = _items[selected].Id;
            switch (_tab)
            {
                case Tab.Boats:
                    foreach (var boat in gear.Boats)
                    {
                        if (boat.BoatId == id)
                        {
                            DrawBoatCard(skin, inner, boat, gear);
                        }
                    }

                    break;
                case Tab.Baits:
                    foreach (var bait in gear.Baits)
                    {
                        if (bait.BaitId == id)
                        {
                            DrawBaitCard(skin, inner, bait, gear);
                        }
                    }

                    break;
                default:
                    foreach (var rod in _shop.Rods)
                    {
                        if (rod.RodId == id)
                        {
                            DrawRodCard(skin, inner, rod, rod.Owned && rod.RodId == gear.RodId);
                        }
                    }

                    break;
            }
        }

        /// <summary>The rope card frame (the Expedition's mission card, as a 9-slice); returns the area inside it.</summary>
        private static Rect CardFrame(UiSkin skin, Rect card)
        {
            var frame = ArtAssets.Texture(CardArt);
            if (frame == null)
            {
                GUI.Box(card, GUIContent.none, skin.Card);
                return new Rect(card.x + 18f, card.y + 16f, card.width - 36f, card.height - 32f);
            }

            Fill(skin, new Rect(card.x + 4f, card.y + 10f, card.width, card.height), new Color(0f, 0f, 0f, 0.35f), 18f);
            GUI.DrawTexture(new Rect(card.x + 10f, card.y + 10f, card.width - 20f, card.height - 20f), skin.White, ScaleMode.StretchToFill, true, 0,
                new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.95f), 0, 16f);
            UiSkin.NineSlice(card, frame, CardArtBorder, CardArtBorder, CardArtBorder, CardArtBorder, CardArtScale);
            return new Rect(card.x + CardPad, card.y + CardPadTop, card.width - CardPad * 2f, card.height - CardPadTop * 2f);
        }

        private void DrawRodCard(UiSkin skin, Rect inner, RodOfferView rod, bool inUse)
        {
            var x = inner.x;
            var w = inner.width;
            var y = inner.y;
            GUI.Label(new Rect(x, y, w, 30f), FishCard.Fit(rod.Name, skin.Heading, w), skin.Heading);
            y += 30f;
            GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), FishCard.Fit(GameTexts.Profile.Tier(rod.Tier) + " · " + GameTexts.Shop.MaxLevelOf(rod.MaxLevel), skin.SmallMuted, w), skin.SmallMuted);
            y += 24f;
            var catches = rod.CanCatchMythic ? GameTexts.Profile.CatchesUpToMythic : rod.CanCatchLegendary ? GameTexts.Profile.CatchesUpToLegendary : rod.CanCatchEpic ? GameTexts.Profile.CatchesRareAndEpic : rod.CanCatchRare ? GameTexts.Profile.CatchesRare : GameTexts.Profile.NoRare;
            y = WrappedLabel(new Rect(x, y, w, 0f), catches, skin.Small) + 4f;
            RequireLine(skin, x, ref y, w, rod.UnlockFisherLevel, rod.BuyBlocker == ServiceError.RodLocked);
            y += 8f;

            // Bottom: price, then the button row.
            var button = new Rect(x, inner.yMax - 44f, w, 44f);
            var price = new Rect(x, button.y - 32f, w, 24f);
            if (!rod.Owned)
            {
                if (rod.IsFree)
                {
                    GUI.Label(price, GameTexts.Shop.Free, skin.BodyBold);
                }
                else
                {
                    Cost(skin, new Rect(price.x, price.y + 1f, price.width, 22f), rod.PriceCoins, rod.PriceShells);
                }
            }

            // Bonuses at level 1 → at the maximum level, each bar scaled by the biggest of the tab (only the drawing).
            var bars = new Rect(x, y, w, price.y - 10f - y);
            var maxCatch = 0.0;
            var maxRarity = 0.0;
            var maxSize = 0.0;
            var maxShell = 0.0;
            foreach (var r in _shop.Rods)
            {
                maxCatch = System.Math.Max(maxCatch, System.Math.Max(r.CatchBonus, r.CatchBonusAtMax));
                maxRarity = System.Math.Max(maxRarity, System.Math.Max(r.RarityBonus, r.RarityBonusAtMax));
                maxSize = System.Math.Max(maxSize, System.Math.Max(r.SizeBonus, r.SizeBonusAtMax));
                maxShell = System.Math.Max(maxShell, System.Math.Max(r.ShellBonus, r.ShellBonusAtMax));
            }

            if (bars.height >= 120f)
            {
                Fill(skin, bars, new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.6f), 8f);
                GUI.DrawTexture(bars, skin.White, ScaleMode.StretchToFill, true, 0, new Color(UiSkin.Border.r, UiSkin.Border.g, UiSkin.Border.b, 0.7f), 1f, 8f);
                var bx = bars.x + 10f;
                var bw = bars.width - 20f;
                var by = bars.y + 6f;
                Legend(skin, bx, by, bw);
                by += 24f;
                var rowHeight = Mathf.Clamp((bars.yMax - 6f - by) / 4f, 30f, 40f);
                BonusBar(skin, new Rect(bx, by, bw, rowHeight), GameTexts.Shop.CatchBonus, rod.CatchBonus, rod.CatchBonusAtMax, maxCatch);
                BonusBar(skin, new Rect(bx, by + rowHeight, bw, rowHeight), GameTexts.Profile.RarityBonus, rod.RarityBonus, rod.RarityBonusAtMax, maxRarity);
                BonusBar(skin, new Rect(bx, by + rowHeight * 2f, bw, rowHeight), GameTexts.Profile.SizeBonus, rod.SizeBonus, rod.SizeBonusAtMax, maxSize);
                BonusBar(skin, new Rect(bx, by + rowHeight * 3f, bw, rowHeight), GameTexts.Profile.ShellBonus, rod.ShellBonus, rod.ShellBonusAtMax, maxShell);
            }

            if (inUse)
            {
                OwnedPill(skin, button, GameTexts.Gear.InUse);
            }
            else if (rod.Owned)
            {
                OwnedPill(skin, button, GameTexts.Shop.Owned);
            }
            else if (rod.BuyBlocker != ServiceError.None)
            {
                Blocked(skin, button, rod.BuyBlocker, rod.BuyBlocker == ServiceError.RodLocked ? GameTexts.Shop.Requires(rod.UnlockFisherLevel) : null);
            }
            else if (skin.IconButton(button, Icons.Buy, rod.IsFree ? GameTexts.Shop.ClaimFree : GameTexts.Shop.Buy, skin.ButtonPrimary))
            {
                _root.BuyRod(rod.RodId);
                _dirty = true;
            }
        }

        private void DrawBoatCard(UiSkin skin, Rect inner, BoatOfferView boat, GearView gear)
        {
            var x = inner.x;
            var w = inner.width;
            var y = inner.y;
            GUI.Label(new Rect(x, y, w, 30f), FishCard.Fit(boat.Name, skin.Heading, w), skin.Heading);
            y += 30f;
            GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), FishCard.Fit(GameTexts.Profile.BoatTier(boat.Tier), skin.SmallMuted, w), skin.SmallMuted);
            y += 24f;
            if (!string.IsNullOrEmpty(boat.Description))
            {
                y = WrappedLabel(new Rect(x, y, w, 0f), boat.Description, skin.SmallMuted) + 6f;
            }

            var max = 0.0;
            foreach (var b in gear.Boats)
            {
                max = System.Math.Max(max, b.Bonus);
            }

            BigBonus(skin, x, ref y, w, boat.Bonus, max);
            RequireLine(skin, x, ref y, w, boat.UnlockFisherLevel, boat.BuyBlocker == ServiceError.BoatLocked);

            var button = new Rect(x, inner.yMax - 44f, w, 44f);
            var price = new Rect(x, button.y - 32f, w, 24f);
            Note(skin, new Rect(x, y + 6f, w, (boat.Owned ? button.y : price.y) - 8f - y - 6f), GameTexts.Gear.BoatsNote);
            if (!boat.Owned)
            {
                Cost(skin, new Rect(price.x, price.y + 1f, price.width, 22f), boat.CostCoins, boat.CostShells);
            }

            if (boat.InUse)
            {
                OwnedPill(skin, button, GameTexts.Gear.InUse);
            }
            else if (boat.Owned)
            {
                if (skin.IconButton(button, Icons.Swap, GameTexts.Gear.Use, skin.Button))
                {
                    _root.UseBoat(boat.BoatId);
                }
            }
            else if (boat.BuyBlocker != ServiceError.None)
            {
                Blocked(skin, button, boat.BuyBlocker, boat.BuyBlocker == ServiceError.BoatLocked ? GameTexts.Shop.Requires(boat.UnlockFisherLevel) : null);
            }
            else if (skin.IconButton(button, Icons.Buy, GameTexts.Shop.Buy, skin.ButtonPrimary))
            {
                _root.BuyBoat(boat.BoatId);
            }
        }

        private void DrawBaitCard(UiSkin skin, Rect inner, BaitOfferView bait, GearView gear)
        {
            var x = inner.x;
            var w = inner.width;
            var y = inner.y;
            GUI.Label(new Rect(x, y, w, 30f), FishCard.Fit(bait.Name, skin.Heading, w), skin.Heading);
            y += 30f;
            GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), FishCard.Fit(GameTexts.Profile.BaitTier(bait.Tier), skin.SmallMuted, w), skin.SmallMuted);
            y += 24f;
            if (!string.IsNullOrEmpty(bait.Description))
            {
                y = WrappedLabel(new Rect(x, y, w, 0f), bait.Description, skin.SmallMuted) + 6f;
            }

            var max = 0.0;
            foreach (var b in gear.Baits)
            {
                max = System.Math.Max(max, b.Bonus);
            }

            BigBonus(skin, x, ref y, w, bait.Bonus, max);
            GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), FishCard.Fit(GameTexts.Gear.Charges(bait.ChargesPerPurchase), skin.Small, w), skin.Small);
            y += 22f;
            if (bait.ChargesLeft > 0)
            {
                var style = bait.InUse ? skin.SmallGold : skin.Small;
                GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), FishCard.Fit(GameTexts.Gear.ChargesLeft(bait.ChargesLeft), style, w), style);
                y += 22f;
            }

            RequireLine(skin, x, ref y, w, bait.UnlockFisherLevel, bait.BuyBlocker == ServiceError.BaitLocked);

            // Bottom: price, Comprar, and Usar / Guardar under it.
            var use = new Rect(x, inner.yMax - 40f, w, 40f);
            var buy = new Rect(x, use.y - 50f, w, 44f);
            var price = new Rect(x, buy.y - 32f, w, 24f);
            Note(skin, new Rect(x, y + 6f, w, price.y - 8f - y - 6f), GameTexts.Gear.BaitsNote);
            Cost(skin, new Rect(price.x, price.y + 1f, price.width, 22f), bait.CostCoins, bait.CostShells);

            if (bait.BuyBlocker != ServiceError.None)
            {
                Blocked(skin, buy, bait.BuyBlocker, bait.BuyBlocker == ServiceError.BaitLocked ? GameTexts.Shop.Requires(bait.UnlockFisherLevel) : null);
            }
            else if (skin.IconButton(buy, Icons.Buy, GameTexts.Shop.Buy, skin.ButtonPrimary))
            {
                _root.BuyBait(bait.BaitId);
            }

            if (bait.InUse)
            {
                if (skin.IconButton(use, Icons.Pause, GameTexts.Gear.PutAway, skin.Button))
                {
                    _root.UseBait(null);
                }
            }
            else if (bait.ChargesLeft > 0 && skin.IconButton(use, Icons.Swap, GameTexts.Gear.Use, skin.Button))
            {
                _root.UseBait(bait.BaitId);
            }
        }

        /// <summary>"No Nível 1" and "No nível máximo" with their two bar shades.</summary>
        private static void Legend(UiSkin skin, float x, float y, float w)
        {
            var half = w / 2f;
            Fill(skin, new Rect(x, y + 6f, 14f, 8f), UiSkin.Accent, 3f);
            GUI.Label(new Rect(x + 20f, y, half - 24f, UiSkin.SmallLine), FishCard.Fit(GameTexts.Shop.AtLevel1, skin.SmallMuted, half - 24f), skin.SmallMuted);
            Fill(skin, new Rect(x + half, y + 6f, 14f, 8f), new Color(UiSkin.Accent.r, UiSkin.Accent.g, UiSkin.Accent.b, 0.4f), 3f);
            GUI.Label(new Rect(x + half + 20f, y, half - 20f, UiSkin.SmallLine), FishCard.Fit(GameTexts.Shop.AtMax, skin.SmallMuted, half - 20f), skin.SmallMuted);
        }

        /// <summary>One bonus: label, "+x% → +y%" (always whole) and a bar with the level-1 value over the max-level one.</summary>
        private static void BonusBar(UiSkin skin, Rect r, string label, double at1, double atMax, double scale)
        {
            var value = "+" + Format.Percent(at1, 0) + " → +" + Format.Percent(atMax, 0);
            var vw = Mathf.Min(r.width, skin.SmallBold.CalcSize(new GUIContent(value)).x + 4f);
            var lw = Mathf.Max(0f, r.width - vw - 8f);
            GUI.Label(new Rect(r.x, r.y, lw, UiSkin.SmallLine), FishCard.Fit(label, skin.SmallMuted, lw), skin.SmallMuted);
            GUI.Label(new Rect(r.xMax - vw, r.y, vw, UiSkin.SmallLine), value, skin.SmallBold);

            var bar = new Rect(r.x, r.y + UiSkin.SmallLine + 3f, r.width, 6f);
            Fill(skin, bar, new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.9f), 3f);
            if (scale > 0.0)
            {
                var maxWidth = bar.width * Mathf.Clamp01((float)(atMax / scale));
                var oneWidth = bar.width * Mathf.Clamp01((float)(at1 / scale));
                if (maxWidth >= 6f)
                {
                    Fill(skin, new Rect(bar.x, bar.y, maxWidth, bar.height), new Color(UiSkin.Accent.r, UiSkin.Accent.g, UiSkin.Accent.b, 0.4f), 3f);
                }

                if (oneWidth >= 6f)
                {
                    Fill(skin, new Rect(bar.x, bar.y, oneWidth, bar.height), UiSkin.Accent, 3f);
                }
            }
        }

        /// <summary>The big "+x% de chance" of a boat or bait and a bar scaled by the biggest bonus of the tab.</summary>
        private void BigBonus(UiSkin skin, float x, ref float y, float w, double bonus, double max)
        {
            GUI.Label(new Rect(x, y, w, 28f), FishCard.Fit(GameTexts.Gear.Bonus(Format.Percent(bonus, 0)), _bonusBig, w), _bonusBig);
            y += 30f;
            var bar = new Rect(x, y, w, 6f);
            Fill(skin, bar, new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.9f), 3f);
            var fill = max > 0.0 ? bar.width * Mathf.Clamp01((float)(bonus / max)) : 0f;
            if (fill >= 6f)
            {
                Fill(skin, new Rect(bar.x, bar.y, fill, bar.height), UiSkin.Accent, 3f);
            }

            y += 14f;
        }

        /// <summary>"Disponível no Nível X", with a check when it is unlocked and a padlock when not.</summary>
        private static void RequireLine(UiSkin skin, float x, ref float y, float w, int level, bool locked)
        {
            skin.DrawIcon(new Rect(x, y + 1f, 18f, 18f), locked ? Icons.Lock : Icons.Check, locked ? UiSkin.Gold : UiSkin.Accent);
            var style = locked ? skin.SmallGold : skin.Small;
            GUI.Label(new Rect(x + 24f, y, w - 24f, UiSkin.SmallLine), FishCard.Fit(GameTexts.Shop.Requires(level), style, w - 24f), style);
            y += 24f;
        }

        /// <summary>A wrapped label; returns the y under it.</summary>
        private static float WrappedLabel(Rect r, string text, GUIStyle style)
        {
            var h = Mathf.Max(UiSkin.SmallLine, style.CalcHeight(new GUIContent(text), r.width));
            GUI.Label(new Rect(r.x, r.y, r.width, h), text, style);
            return r.y + h;
        }

        /// <summary>The tab's note on the card, only when it fits whole (it is always behind the "i" too).</summary>
        private static void Note(UiSkin skin, Rect r, string text)
        {
            if (r.height <= 0f || skin.SmallMuted.CalcHeight(new GUIContent(text), r.width) > r.height)
            {
                return;
            }

            GUI.Label(r, text, skin.SmallMuted);
        }

        // ------------------------------------------------------------------ dock: tackle box and bobbers

        private void DrawDock(UiSkin skin, Rect dock, GearView gear)
        {
            var boxWidth = Mathf.Clamp(dock.width * 0.46f, 360f, 420f);
            DrawBox(skin, new Rect(dock.x, dock.y, boxWidth, dock.height), gear);
            DrawPond(skin, new Rect(dock.x + boxWidth + Gap, dock.y, dock.width - boxWidth - Gap, dock.height), gear);
        }

        /// <summary>"Seu equipamento" in an open tackle box: rod, boat and bait in three wells, and the total bonus.</summary>
        private void DrawBox(UiSkin skin, Rect rect, GearView gear)
        {
            var body = new Rect(rect.x, rect.y + LidHeight - 2f, rect.width, rect.height - LidHeight + 2f);
            Fill(skin, new Rect(rect.x + 3f, rect.y + 8f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.35f), 10f);
            var art = ArtAssets.Texture(BoxArt);
            if (art != null)
            {
                UiSkin.NineSlice(rect, art, BoxArtBorder, BoxArtBorder, BoxArtBorder, BoxArtBorder, BoxArtScale);
            }
            else
            {
                var lid = new Rect(rect.x + rect.width * 0.04f, rect.y, rect.width * 0.92f, LidHeight);
                GUI.DrawTexture(lid, skin.White, ScaleMode.StretchToFill, true, 0, LidBottom, Vector4.zero, new Vector4(8f, 8f, 0f, 0f));
                GUI.DrawTexture(new Rect(lid.x, lid.y, lid.width, lid.height * 0.5f), skin.White, ScaleMode.StretchToFill, true, 0, LidTop, Vector4.zero, new Vector4(8f, 8f, 0f, 0f));
                GUI.DrawTexture(lid, skin.White, ScaleMode.StretchToFill, true, 0, BoxEdge, new Vector4(1.5f, 1.5f, 1.5f, 0f), new Vector4(8f, 8f, 0f, 0f));
                Fill(skin, body, BoxBottom, 10f);
                GUI.DrawTexture(new Rect(body.x, body.y, body.width, body.height * 0.5f), skin.White, ScaleMode.StretchToFill, true, 0, BoxTop, Vector4.zero, new Vector4(10f, 10f, 0f, 0f));
                GUI.DrawTexture(body, skin.White, ScaleMode.StretchToFill, true, 0, BoxEdge, 2f, 10f);
            }

            // The lid's handle is always drawn here (the art has none, so it does not stretch).
            Fill(skin, new Rect(rect.center.x - 22f, rect.y + 4f, 44f, 6f), TagHole, 3f);

            var x = body.x + 12f;
            var w = body.width - 24f;
            var y = body.y + 6f;
            var previous = GUI.contentColor;
            GUI.contentColor = BoxInk;
            GUI.Label(new Rect(x, y, w, 24f), GameTexts.Gear.Yours, skin.CenterBold);
            GUI.contentColor = previous;
            y += 26f;

            var cw = (w - 12f) / 3f;
            Compartment(skin, new Rect(x, y, cw, 100f), GameTexts.Gear.Rod, "Varas/" + gear.RodId, Icons.Rod, gear.RodName, gear.RodBonus, true);
            Compartment(skin, new Rect(x + cw + 6f, y, cw, 100f), GameTexts.Gear.Boat, "Barcos/" + gear.BoatId, Icons.Boat, gear.BoatName, gear.BoatBonus, true);
            Compartment(skin, new Rect(x + (cw + 6f) * 2f, y, cw, 100f), GameTexts.Gear.Bait, gear.BaitId != null ? "Iscas/" + gear.BaitId : null, Icons.Bait, gear.BaitName ?? GameTexts.Gear.NoBait, gear.BaitBonus, gear.BaitName != null);
            y += 104f;

            // The total on the right; the bait's attempts left on the left, after its icon.
            var total = "+" + Format.Percent(gear.TotalBonus, 0);
            var totalWidth = skin.Heading.CalcSize(new GUIContent(total)).x + 4f;
            GUI.Label(new Rect(body.xMax - 12f - totalWidth, y - 3f, totalWidth, 26f), total, skin.Heading);
            var labelWidth = skin.SmallBold.CalcSize(new GUIContent(GameTexts.Gear.Total)).x + 4f;
            var lx = body.xMax - 12f - totalWidth - 8f - labelWidth;
            GUI.contentColor = BoxInk;
            GUI.Label(new Rect(lx, y, labelWidth, UiSkin.SmallLine), GameTexts.Gear.Total, skin.SmallBold);
            GUI.contentColor = previous;
            if (gear.BaitName != null)
            {
                skin.DrawIcon(new Rect(x, y + 1f, 18f, 18f), Icons.Bait, UiSkin.Muted);
                var cl = Mathf.Max(0f, lx - 10f - (x + 24f));
                GUI.Label(new Rect(x + 24f, y, cl, UiSkin.SmallLine), FishCard.Fit(GameTexts.Gear.ChargesLeft(gear.BaitChargesLeft), skin.SmallMuted, cl), skin.SmallMuted);
            }
        }

        private void Compartment(UiSkin skin, Rect r, string label, string art, string icon, string name, double bonus, bool has)
        {
            Fill(skin, r, Well, 8f);
            Fill(skin, new Rect(r.x + 2f, r.y + 1f, r.width - 4f, 4f), new Color(0f, 0f, 0f, 0.35f), 2f);
            GUI.Label(new Rect(r.x + 4f, r.y + 2f, r.width - 8f, UiSkin.SmallLine), FishCard.Fit(label, skin.SmallMuted, r.width - 8f), skin.SmallMutedCenter);

            var picture = new Rect(r.x + 8f, r.y + 23f, r.width - 16f, 32f);
            var tex = art != null ? ArtAssets.Texture(art) : null;
            if (tex != null)
            {
                GUI.DrawTexture(picture, tex, ScaleMode.ScaleToFit, true);
            }
            else
            {
                skin.DrawIcon(new Rect(picture.center.x - 13f, picture.y + 3f, 26f, 26f), icon, has ? UiSkin.Accent : UiSkin.Muted);
            }

            GUI.Label(new Rect(r.x + 4f, r.y + 56f, r.width - 8f, UiSkin.SmallLine), FishCard.Fit(name, _boldCenter, r.width - 8f), _boldCenter);
            var previous = GUI.contentColor;
            GUI.contentColor = has ? TealLight : UiSkin.Muted;
            GUI.Label(new Rect(r.x + 4f, r.y + 77f, r.width - 8f, UiSkin.SmallLine), "+" + Format.Percent(bonus, 0), _boldCenter);
            GUI.contentColor = previous;
        }

        /// <summary>"Chance de puxar o peixe": one bobber per rarity on the water, the percentage above it. The ones that
        /// do not bite here lie grey on the water, with "não morde aqui". The explanation is behind the "i".</summary>
        private void DrawPond(UiSkin skin, Rect rect, GearView gear)
        {
            Fill(skin, rect, WaterB, 12f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 40f), skin.White, ScaleMode.StretchToFill, true, 0, WaterTop, Vector4.zero, new Vector4(12f, 12f, 0f, 0f));
            Fill(skin, new Rect(rect.x, rect.y + 40f, rect.width, (rect.height - 40f) * 0.5f), WaterA, 0f);
            Fill(skin, new Rect(rect.x + 1f, rect.y + 39f, rect.width - 2f, 2f), WaterLine, 0f);
            Fill(skin, new Rect(rect.x + 1f, rect.y + 41f, rect.width - 2f, 5f), new Color(WaterLine.r, WaterLine.g, WaterLine.b, 0.35f), 0f);
            GUI.DrawTexture(rect, skin.White, ScaleMode.StretchToFill, true, 0, Shelf, 1.5f, 12f);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, 24f), FishCard.Fit(GameTexts.Gear.ChanceTitle, skin.CenterBold, rect.width - 20f), skin.CenterBold);

            var count = gear.Chances.Count;
            if (count == 0)
            {
                return;
            }

            var cw = (rect.width - 16f) / count;
            for (var i = 0; i < count; i++)
            {
                var chance = gear.Chances[i];
                var col = new Rect(rect.x + 8f + i * cw, rect.y, cw, rect.height);
                var color = chance.BitesHere ? UiSkin.RarityColor(chance.RarityId) : UiSkin.Muted;

                var previous = GUI.contentColor;
                GUI.contentColor = color;
                GUI.Label(new Rect(col.x, rect.y + 46f, col.width, 26f), Format.Percent(chance.Chance, 0), _percent);
                GUI.contentColor = previous;

                Bobber(skin, new Vector2(col.center.x, rect.y + 96f), color, chance.BitesHere);

                GUI.contentColor = color;
                GUI.Label(new Rect(col.x + 2f, rect.y + 120f, col.width - 4f, UiSkin.SmallLine), FishCard.Fit(chance.RarityName, _boldCenter, col.width - 4f), _boldCenter);
                GUI.contentColor = previous;
                if (!chance.BitesHere)
                {
                    GUI.Label(new Rect(col.x + 2f, rect.y + 140f, col.width - 4f, UiSkin.SmallLine * 2f), GameTexts.Gear.NotHere, _mutedTop);
                }
            }
        }

        /// <summary>A bobber centred on <paramref name="centre"/>: upright in the rarity colour, or lying grey on the water.</summary>
        private static void Bobber(UiSkin skin, Vector2 centre, Color color, bool upright)
        {
            // A soft ripple at the waterline.
            Fill(skin, new Rect(centre.x - 18f, centre.y + 10f, 36f, 8f), new Color(0.75f, 0.92f, 1f, upright ? 0.14f : 0.08f), 4f);

            var r = new Rect(centre.x - 11f, centre.y - 18f, 22f, 34f);
            var matrix = GUI.matrix;
            if (!upright)
            {
                var p = new Vector3(centre.x, centre.y + 6f, 0f);
                GUI.matrix = matrix * Matrix4x4.TRS(p, Quaternion.Euler(0f, 0f, 70f), Vector3.one) * Matrix4x4.TRS(-p, Quaternion.identity, Vector3.one);
            }

            var art = ArtAssets.Texture(BobberArt);
            if (art != null)
            {
                // Top half tinted with the rarity colour, bottom half as drawn (texture v grows upwards).
                var previous = GUI.color;
                var top = new Rect(r.x, r.y, r.width, r.height / 2f);
                var bottom = new Rect(r.x, r.y + r.height / 2f, r.width, r.height / 2f);
                GUI.color = upright ? color : Lying;
                GUI.DrawTextureWithTexCoords(top, art, new Rect(0f, 0.5f, 1f, 0.5f), true);
                GUI.color = upright ? Color.white : Lying;
                GUI.DrawTextureWithTexCoords(bottom, art, new Rect(0f, 0f, 1f, 0.5f), true);
                GUI.color = previous;
            }
            else
            {
                var tint = upright ? Color.white : Lying;
                var cap = upright ? color : Lying;
                Fill(skin, new Rect(r.center.x - 1f, r.y, 2f, 10f), Antenna * tint, 0f);
                var body = new Rect(r.x, r.y + 8f, r.width, r.height - 8f);
                GUI.DrawTexture(new Rect(body.x, body.y, body.width, body.height / 2f), skin.White, ScaleMode.StretchToFill, true, 0, cap, Vector4.zero, new Vector4(11f, 11f, 0f, 0f));
                GUI.DrawTexture(new Rect(body.x, body.center.y, body.width, body.height / 2f), skin.White, ScaleMode.StretchToFill, true, 0, BobberWhite * tint, Vector4.zero, new Vector4(0f, 0f, 11f, 11f));
                Fill(skin, new Rect(body.x, body.center.y - 1f, body.width, 2f), BobberBand, 0f);
                Fill(skin, new Rect(body.x + 5f, body.y + 3f, 4f, 6f), new Color(1f, 1f, 1f, upright ? 0.35f : 0.15f), 2f);
            }

            GUI.matrix = matrix;
        }

        // ------------------------------------------------------------------ VIP (A-110)

        private void DrawVip(UiSkin skin, Rect content)
        {
            var vip = _root.Vip;
            if (vip == null)
            {
                return;
            }

            // The same card as before, on a dark well so it reads over the plank wall.
            Fill(skin, new Rect(content.x, content.y, content.width, Mathf.Min(content.height, 252f)), new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.7f), 12f);
            content = new Rect(content.x + 12f, content.y + 10f, content.width - 24f, content.height - 20f);

            GUI.Label(new Rect(content.x, content.y, content.width, 40), GameTexts.Vip.Note, skin.SmallMuted);
            var rect = new Rect(content.x, content.y + 42, content.width, 190);
            GUI.Box(rect, GUIContent.none, vip.Active ? skin.CardSelected : skin.Card);
            skin.IconBadge(new Rect(rect.x + 40, rect.y + 30, 56, 56), Icons.Dollar, vip.Active ? UiSkin.Gold : UiSkin.Muted);

            var x = rect.x + 140;
            var w = rect.width - 140 - 260;
            GUI.Label(new Rect(x, rect.y + 18, w, 28), GameTexts.Vip.Title, skin.Heading);
            GUI.Label(new Rect(x, rect.y + 50, w, 22), GameTexts.Vip.Benefit(Format.Percent(vip.OfflineFisherXpBonus, 0)), skin.BodyBold);
            GUI.Label(new Rect(x, rect.y + 76, w, 22), GameTexts.Vip.Duration(Format.Number((long)System.Math.Round(vip.DurationDays))), skin.Small);
            GUI.Label(new Rect(x, rect.y + 102, w, 22), GameTexts.Vip.Price(Format.Number(vip.PriceDollars)), skin.SmallGold);
            GUI.Label(new Rect(x, rect.y + 128, w, 22), GameTexts.Vip.YourDollars(Format.Number(vip.Dollars)), skin.SmallMuted);
            GUI.Label(new Rect(x, rect.y + 154, w, 22), vip.Active
                ? GameTexts.Vip.Until(Format.DateTimeFromUnixMs(vip.UntilMs), Format.Duration(vip.RemainingMs / 1000.0))
                : GameTexts.Vip.Inactive, vip.Active ? skin.SmallGold : skin.SmallMuted);

            var button = new Rect(rect.xMax - 240, rect.y + 20, 220, 42);
            if (vip.BuyBlocker != ServiceError.None)
            {
                Blocked(skin, new Rect(button.x - 20, button.y, button.width + 20, 42), vip.BuyBlocker);
            }
            else if (skin.IconButton(button, Icons.Buy, vip.Active ? GameTexts.Vip.Extend : GameTexts.Vip.Buy, skin.ButtonPrimary))
            {
                _root.BuyVip();
            }

            if (vip.Active)
            {
                var label = GameTexts.Vip.Active.ToUpperInvariant();
                skin.AccentPill(new Rect(button.x, button.y + 61, skin.PillWidth(label, true) + 6, 26), label, UiSkin.Accent, Icons.Check);
            }
        }

        // ------------------------------------------------------------------ helpers

        private static void Cost(UiSkin skin, Rect rect, long coins, long shells)
        {
            // The Conchas price sits right after the coins.
            var coinsText = Format.Short(coins);
            var coinsWidth = skin.CoinAmountWidth(coinsText, rect.height);
            skin.CoinAmount(new Rect(rect.x, rect.y, coinsWidth, rect.height), coinsText);
            if (shells > 0)
            {
                var sx = rect.x + coinsWidth + 10f;
                var shellsText = Format.Number(shells);
                skin.CurrencyIcon(new Rect(sx, rect.y, rect.height, rect.height), Icons.Shell);
                GUI.Label(new Rect(sx + 4 + rect.height, rect.y, skin.SmallGoldLine.CalcSize(new GUIContent(shellsText)).x + 4f, rect.height), shellsText, skin.SmallGoldLine);
            }
        }

        /// <summary>"JÁ É SUA" / "EM USO" as a pill centred in the button's place.</summary>
        private static void OwnedPill(UiSkin skin, Rect button, string text)
        {
            var label = text.ToUpperInvariant();
            var w = skin.PillWidth(label, true) + 6f;
            skin.AccentPill(new Rect(button.center.x - w / 2f, button.center.y - 13f, w, 26f), label, UiSkin.Accent, Icons.Check);
        }

        private static void Blocked(UiSkin skin, Rect rect, ServiceError blocker, string text = null)
        {
            skin.DrawIcon(new Rect(rect.x, rect.y + 11, 20, 20), Icons.Lock, UiSkin.Gold);
            GUI.Label(new Rect(rect.x + 28, rect.y + 2, rect.width - 28, 42), text ?? GameTexts.ServiceErrorMessage(blocker.ToString()), skin.SmallGold);
        }

        private static void Fill(UiSkin skin, Rect rect, Color color, float radius)
        {
            GUI.DrawTexture(rect, skin.White, ScaleMode.StretchToFill, true, 0, color, 0, radius);
        }

        private static Color Rgb(int rgb)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
        }

        private void EnsureStyles(UiSkin skin)
        {
            if (_plaque != null)
            {
                return;
            }

            _plaque = new GUIStyle(skin.BodyBold) { alignment = TextAnchor.MiddleLeft, wordWrap = false, clipping = TextClipping.Clip };
            _plaque.normal.textColor = Color.white;
            _percent = new GUIStyle(skin.Heading) { alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Clip };
            _percent.normal.textColor = Color.white;
            _boldCenter = new GUIStyle(skin.SmallBold) { alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Clip };
            _boldCenter.normal.textColor = Color.white;
            _mutedTop = new GUIStyle(skin.SmallMuted) { alignment = TextAnchor.UpperCenter, wordWrap = true };
            _bonusBig = new GUIStyle(skin.Heading) { wordWrap = false, clipping = TextClipping.Clip };
            _bonusBig.normal.textColor = TealLight;
        }
    }
}
