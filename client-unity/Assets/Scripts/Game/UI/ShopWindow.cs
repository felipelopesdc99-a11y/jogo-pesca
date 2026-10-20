using System.Collections.Generic;
using System.Linq;
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
    /// Layout since A-190 (minimal, the old "Balcão do Píer" decoration left): on the left the list of the tab (picture,
    /// name, class and the tag: price, fragments, "JÁ É SUA" or "EM USO"); in the middle the chosen item big with
    /// "‹ 4 de 6 ›"; on the right its sheet with the bonuses as bars and Comprar / Usar. At the bottom, "Seu equipamento"
    /// in three cards and the chance of pulling each rarity as solid pills. Nothing flies or turns.
    /// </remarks>
    public sealed class ShopWindow
    {
        public enum Tab
        {
            Rods,
            Boats,
            Baits,
            Vip,

            /// <summary>The five clothing pieces (M24-T33, A-164).</summary>
            Clothing,
        }

        private enum TagKind
        {
            Price,
            Free,
            Owned,
            InUse,

            /// <summary>A boat, rod or clothing piece not assembled yet: "12/40" fragments (M24-T16/T32/T33).</summary>
            Fragments,
        }

        /// <summary>One item on the hook wall, whatever the tab: only what the wall and the counter draw.</summary>
        private struct WallItem
        {
            public string Id, Name, Sub, Art, Icon, OwnedText;
            public int Tier;
            public long Coins, Shells;

            /// <summary>Boats, rods and clothing by fragments: how many the player has and how many assemble it.</summary>
            public long FragmentsHave, FragmentsNeed;
            public TagKind Tag;
            public bool Square, Turned;
        }

        // Layout (virtual canvas). The window is the common 1320×840 frame: the content is 1264×736 on any screen of
        // 4:3 or wider (the HUD scales a 1080-tall canvas), a little narrower on 5:4.
        // Since A-190 a plain list (rows of 64 to 80 on the grid of 8), the chosen item big in the middle and its sheet on
        // the right; "Seu equipamento" and the chances in one strip at the bottom. The plank wall, the beam, the hooks, the
        // pedestal, the flight, the tackle box and the bobbers left (ui_loja_caixa and ui_loja_boia are no longer read).
        private const float Gap = 16f, DockHeight = 168f;
        private const float RowGap = 8f, MinRow = 64f, MaxRow = 80f;

        /// <summary>The card's padding inside the ARC frame.</summary>
        private const float CardInsetX = 20f, CardInsetY = 18f;

        // Colours of the Mescla B page the owner approved (Art Bible palette: night blues and turquoise).
        private static Color TagDark => UiSkin.CardColor;
        private static Color TealLight => UiSkin.Accent;
        private static readonly Color TealInk = Rgb(0x06313A);

        private readonly GameRoot _root;
        private readonly List<WallItem> _items = new List<WallItem>();
        private readonly string[] _selected = new string[5];
        private ShopView _shop;
        private bool _dirty = true;
        private Tab _tab;
        private Vector2 _wallScroll;
        private int _scrollTo = -1;

        // When the tab or the chosen item changed (kept for the windows that ask; nothing flies since A-190).
        private bool _animPending;
        private float _animStart = -10f;
        private Rect _heroRect;

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
            var area = WindowFrame.Draw(skin, screenWidth, screenHeight, GameTexts.Shop.Title, GameTexts.Shop.Info(TabNote(), chanceNote), out var closed, 1320f, 840f, Icons.Shop, bookmarks: true);
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

            // The five tabs are bookmarks on the side of the window. Since A-190 the content sits on the plain panel: the
            // plank wall, the beam, the hooks, the pedestal and the tackle box left (a menu, not a scene).
            DrawTabs(skin);
            var body = area;
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

            DrawWall(skin, wall, selected);
            selected = SelectedIndex();
            DrawCounter(skin, counter, selected);
            DrawCard(skin, card, gear, selected);
            DrawDock(skin, dock, gear);
        }

        /// <summary>The sentence of the current tab for the "i".</summary>
        private string TabNote()
        {
            switch (_tab)
            {
                case Tab.Boats: return GameTexts.Gear.BoatsNote;
                case Tab.Baits: return GameTexts.Gear.BaitsNote;
                case Tab.Vip: return GameTexts.Vip.Note;
                case Tab.Clothing: return GameTexts.Gear.ClothingNote;
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
                            Art = "Barcos/" + boat.BoatId, Icon = Icons.Boat, Tier = boat.Tier,
                            FragmentsHave = boat.Fragments, FragmentsNeed = boat.FragmentsRequired, OwnedText = GameTexts.Shop.OwnedBoat,
                            Tag = boat.InUse ? TagKind.InUse : boat.Owned ? TagKind.Owned : TagKind.Fragments,
                        });
                    }

                    break;
                case Tab.Clothing:
                    var clothing = _root.Clothing;
                    foreach (var piece in clothing != null ? clothing.Pieces : new List<ClothingPieceView>())
                    {
                        _items.Add(new WallItem
                        {
                            Id = piece.PieceId, Name = piece.Name,
                            Sub = piece.Owned ? GameTexts.Shop.LevelOf(piece.Level, piece.MaxLevel) : GameTexts.Gear.ClothingAxisName(piece.Axis),
                            // Roupas/gloves … Roupas/shirt (art delivered 13/10/2026; the star icon if a file is missing).
                            Art = "Roupas/" + piece.PieceId, Icon = Icons.Star, Tier = 0, Square = true,
                            FragmentsHave = clothing.Fragments, FragmentsNeed = clothing.NextCost, OwnedText = GameTexts.Gear.Wearing,
                            Tag = piece.Owned ? TagKind.Owned : TagKind.Fragments,
                        });
                    }

                    break;
                case Tab.Baits:
                    foreach (var bait in gear.Baits)
                    {
                        _items.Add(new WallItem
                        {
                            Id = bait.BaitId, Name = bait.Name, Sub = GameTexts.Gear.Bonus(Format.Percent(bait.Bonus, 0)),
                            Art = "Iscas/" + bait.BaitId, Icon = Icons.Bait, Tier = bait.Tier,
                            Coins = bait.CostCoins, Shells = bait.CostShells, Square = true,
                            Tag = bait.InUse ? TagKind.InUse : TagKind.Price,
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
                            Art = "Varas/" + rod.RodId, Icon = Icons.Rod, Tier = rod.Tier,
                            // Rods by fragments (M24-T32, A-163): "12/25" until assembled; the Starter Rod is claimed for free.
                            FragmentsHave = rod.Fragments, FragmentsNeed = rod.FragmentsRequired, OwnedText = GameTexts.Shop.Owned, Turned = true,
                            Tag = inUse ? TagKind.InUse : rod.Owned ? TagKind.Owned : rod.IsFree ? TagKind.Free : TagKind.Fragments,
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

        private static readonly Tab[] TabOrder = { Tab.Rods, Tab.Boats, Tab.Clothing, Tab.Baits, Tab.Vip };
        private static readonly string[] TabLabels = { GameTexts.Gear.TabRods, GameTexts.Gear.TabBoats, GameTexts.Gear.TabClothing, GameTexts.Gear.TabBaits, GameTexts.Vip.Tab };
        private static readonly string[] TabIcons = { Icons.Rod, Icons.Boat, Icons.Star, Icons.Bait, Icons.Dollar };

        /// <summary>The five tabs as bookmarks on the side of the diary (A-170; they were plaques on the beam).</summary>
        private void DrawTabs(UiSkin skin)
        {
            var clicked = WindowFrame.Bookmarks(skin, TabLabels, TabIcons, System.Array.IndexOf(TabOrder, _tab));
            if (clicked >= 0 && TabOrder[clicked] != _tab)
            {
                _tab = TabOrder[clicked];
                _animStart = Time.unscaledTime;
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

                DrawShelf(skin, row, _items[i], i == selected, Mathf.Min(tagWidth, width * 0.36f), !scrolls);

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

        /// <summary>
        /// One row of the list (A-190): the item's picture on the left, name and "Classe 2" with the tier stripe, and its tag
        /// (price, fragments, "JÁ É SUA" or "EM USO") on the right; the chosen row on the raised panel with the gold line.
        /// Returns the picture's rect.
        /// </summary>
        private static Rect DrawShelf(UiSkin skin, Rect row, WallItem item, bool chosen, float tagColumn, bool turn)
        {
            var hover = GUI.enabled && row.Contains(Event.current.mousePosition);
            var theme = VisualTheme.Current;
            skin.LineBox(row, chosen ? theme.PanelElevated : hover ? Color.Lerp(theme.Card, theme.PanelElevated, 0.5f) : theme.Card,
                chosen ? UiSkin.FrameGold : UiSkin.ChipOffColor, 1);

            var midY = row.center.y;
            Fill(skin, UiSkin.Snap(new Rect(row.x + UiSkin.ArcPixel * 2f, row.y + 12f, UiSkin.ArcPixel * 2f, row.height - 24f)), UiSkin.TierColor(item.Tier), 0f);

            // The picture: a square box for clothing and baits, a wide one for rods and boats.
            Rect itemRect;
            if (item.Square)
            {
                var s = Mathf.Min(row.height - 16f, 56f);
                itemRect = new Rect(row.x + 16f, midY - s / 2f, s, s);
            }
            else
            {
                var w = Mathf.Min(row.height * 1.6f, 112f);
                itemRect = new Rect(row.x + 16f, midY - w / 4f, w, w / 2f);
            }

            var pictureRight = row.x + 16f + Mathf.Max(itemRect.width, 56f);
            DrawItemArt(skin, itemRect, item, 0f, Color.white);

            var tagCol = new Rect(row.xMax - tagColumn - 12f, row.y, tagColumn, row.height);
            DrawTag(skin, tagCol, item, row.y);

            var tx = pictureRight + 16f;
            var tw = Mathf.Max(40f, tagCol.x - 12f - tx);
            GUI.Label(new Rect(tx, midY - UiSkin.SmallLine, tw, UiSkin.SmallLine), FishCard.Fit(item.Name, skin.SmallBold, tw), skin.SmallBold);
            GUI.Label(new Rect(tx, midY, tw, UiSkin.SmallLine), FishCard.Fit(item.Sub, skin.SmallMuted, tw), skin.SmallMuted);
            return itemRect;
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
                case TagKind.Fragments:
                    return 24f + skin.SmallGoldLine.CalcSize(new GUIContent(FragmentsTag(item))).x + 4f;
                default:
                    return 18f + 4f + skin.PillText.CalcSize(new GUIContent(TagText(item))).x;
            }
        }

        /// <summary>Art path (ArtePixel) of the fragment icon of a wall item: clothing pieces share frag_roupa.</summary>
        private static string FragmentArt(WallItem item) =>
            item.Art != null && item.Art.StartsWith("Roupas/", System.StringComparison.Ordinal) ? "Fragmentos/frag_roupa" : "Fragmentos/frag_" + item.Id;

        private static string FragmentsTag(WallItem item) => GameTexts.Gear.FragmentsTag(Format.Number(item.FragmentsHave), Format.Number(item.FragmentsNeed));

        private static string TagText(WallItem item)
        {
            return (item.Tag == TagKind.InUse ? GameTexts.Gear.InUse : item.OwnedText ?? GameTexts.Shop.Owned).ToUpperInvariant();
        }

        /// <summary>The tag hanging from the shelf: price, "JÁ É SUA" / "JÁ É SEU" or "EM USO".</summary>
        private static void DrawTag(UiSkin skin, Rect col, WallItem item, float stringTop)
        {
            var content = TagContentWidth(skin, item);
            var w = Mathf.Min(col.width, content + 22f);
            var h = item.Tag == TagKind.Price && item.Shells > 0 ? 48f : 28f;
            var tag = new Rect(col.xMax - w, col.center.y - h / 2f, w, h);

            Color fill, edge, ink;
            switch (item.Tag)
            {
                case TagKind.InUse: fill = UiSkin.Accent; edge = UiSkin.Accent; ink = TealInk; break;
                case TagKind.Owned: fill = TagDark; edge = UiSkin.Accent; ink = TealLight; break;
                default: fill = TagDark; edge = UiSkin.Accent; ink = UiSkin.Gold; break;
            }

            // A plain pill since A-190 (the string, the hole and the shadow of the hanging tag left).
            skin.ArcFillLine(tag, fill, edge, 5f);

            var x = tag.x + 12f;
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
                case TagKind.Fragments:
                    // The fragment icon (frag_boat_NN, frag_rod_NN, frag_roupa); the item's icon if the file is missing.
                    var fragmentArt = ArtAssets.Texture(FragmentArt(item));
                    if (fragmentArt != null)
                    {
                        PixelDraw.Fit(new Rect(x, line.y + 1f, 18f, 18f), fragmentArt);
                    }
                    else
                    {
                        skin.DrawIcon(new Rect(x, line.y + 1f, 18f, 18f), item.Icon, UiSkin.Gold);
                    }

                    GUI.Label(new Rect(x + 24f, line.y, line.width - 24f, 20f), FragmentsTag(item), skin.SmallGoldLine);
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

            // The chosen item big in the middle of the column (A-190: no aura, pedestal or counter plank, and no flight).
            var pedestalTop = counter.center.y + Mathf.Min(counter.width * 0.2f, 100f);

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

            DrawItemArt(skin, _heroRect, item, 0f, Color.white);
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

            // Nothing turns (ART_BIBLE_PIXEL 4.9, A-190): the rods are already drawn on the diagonal.
            PixelDraw.Fit(rect, tex, color);
        }

        // ------------------------------------------------------------------ the card

        private void DrawCard(UiSkin skin, Rect card, GearView gear, int selected)
        {
            if (selected < 0)
            {
                return;
            }

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
                case Tab.Clothing:
                    foreach (var piece in _root.Clothing != null ? _root.Clothing.Pieces : new List<ClothingPieceView>())
                    {
                        if (piece.PieceId == id)
                        {
                            DrawClothingCard(skin, inner, piece, _root.Clothing);
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

        /// <summary>The card: the ARC frame (gold, a dark line, dark gold) around the panel night; returns the area inside it.</summary>
        private static Rect CardFrame(UiSkin skin, Rect card)
        {
            // A card with one line (A-190: at most two levels of box inside the window).
            skin.LineBox(card, UiSkin.CardColor, UiSkin.ChipOffColor, 1);
            return new Rect(card.x + CardInsetX, card.y + CardInsetY, card.width - CardInsetX * 2f, card.height - CardInsetY * 2f);
        }

        private void DrawRodCard(UiSkin skin, Rect inner, RodOfferView rod, bool inUse)
        {
            var x = inner.x;
            var w = inner.width;
            var y = inner.y;
            GUI.Label(new Rect(x, y, w, 30f), FishCard.Fit(rod.Name, skin.Heading, w), skin.Heading);
            y += 30f;
            // Your copy shows its level ("Nível 37/100", M24-T13); otherwise how far the rod goes.
            var hasCopy = rod.OwnedLevel > 0;
            var levelText = hasCopy && rod.MaxLevel > 1 ? GameTexts.Shop.LevelOf(rod.OwnedLevel, rod.MaxLevel) : GameTexts.Shop.MaxLevelOf(rod.MaxLevel);
            GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), FishCard.Fit(GameTexts.Profile.Tier(rod.Tier) + " · " + levelText, hasCopy ? skin.SmallGold : skin.SmallMuted, w), hasCopy ? skin.SmallGold : skin.SmallMuted);
            y += 24f;
            var catches = rod.CanCatchMythic ? GameTexts.Profile.CatchesUpToMythic : rod.CanCatchLegendary ? GameTexts.Profile.CatchesUpToLegendary : rod.CanCatchEpic ? GameTexts.Profile.CatchesRareAndEpic : rod.CanCatchRare ? GameTexts.Profile.CatchesRare : GameTexts.Profile.NoRare;
            y = WrappedLabel(new Rect(x, y, w, 0f), catches, skin.Small) + 4f;

            // Rods by fragments (M24-T32, A-163): "Fragmentos 12/25 · Caem em todos os mapas" (M24-T47), a bar and the
            // chance while the rod is not assembled; the extras once it is.
            if (rod.FragmentsRequired > 0)
            {
                FragmentLines(skin, x, ref y, w, rod.Owned, rod.Fragments, rod.FragmentsRequired, GameTexts.Gear.FragmentsEverywhere, rod.FragmentChance);
            }

            y += 8f;

            // Bottom: price (of the rod, or of your copy's next level), then the button row.
            var button = new Rect(x, inner.yMax - 44f, w, 44f);
            var price = new Rect(x, button.y - 32f, w, 24f);
            var canLevel = hasCopy && rod.MaxLevel > 1;
            if (canLevel && rod.NextUpgradeCost > 0)
            {
                Cost(skin, new Rect(price.x, price.y + 1f, price.width, 22f), rod.NextUpgradeCost, rod.NextUpgradeShells);
            }
            else if (!rod.Owned && rod.IsFree)
            {
                GUI.Label(price, GameTexts.Shop.Free, skin.BodyBold);
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
                skin.Inset(bars);
                var bx = bars.x + 10f;
                var bw = bars.width - 20f;
                var by = bars.y + 6f;
                // With a copy, the bright shade is its level now; otherwise Nv.1.
                Legend(skin, bx, by, bw, canLevel ? GameTexts.Shop.AtLevelNow(rod.OwnedLevel) : GameTexts.Shop.AtLevel1);
                by += 24f;
                var rowHeight = Mathf.Clamp((bars.yMax - 6f - by) / 4f, 30f, 40f);
                BonusBar(skin, new Rect(bx, by, bw, rowHeight), GameTexts.Shop.CatchBonus, canLevel ? rod.CatchBonusNow : rod.CatchBonus, rod.CatchBonusAtMax, maxCatch, canLevel);
                BonusBar(skin, new Rect(bx, by + rowHeight, bw, rowHeight), GameTexts.Profile.RarityBonus, canLevel ? rod.RarityBonusNow : rod.RarityBonus, rod.RarityBonusAtMax, maxRarity, canLevel);
                BonusBar(skin, new Rect(bx, by + rowHeight * 2f, bw, rowHeight), GameTexts.Profile.SizeBonus, canLevel ? rod.SizeBonusNow : rod.SizeBonus, rod.SizeBonusAtMax, maxSize, canLevel);
                BonusBar(skin, new Rect(bx, by + rowHeight * 3f, bw, rowHeight), GameTexts.Profile.ShellBonus, canLevel ? rod.ShellBonusNow : rod.ShellBonus, rod.ShellBonusAtMax, maxShell, canLevel);
            }

            if (canLevel)
            {
                // Your copy: "Melhorar → Nv. 38" (the same upgrade as the Inventory), or "Nível máximo".
                if (rod.UpgradeBlocker == ServiceError.RodAtMaxLevel)
                {
                    OwnedPill(skin, button, GameTexts.Shop.MaxLevel);
                }
                else if (rod.UpgradeBlocker != ServiceError.None)
                {
                    Blocked(skin, button, rod.UpgradeBlocker);
                }
                else if (skin.IconButton(button, Icons.Level, GameTexts.Shop.UpgradeTo(rod.OwnedLevel + 1), skin.ButtonPrimary))
                {
                    _root.UpgradeRod(rod.OwnedItemId);
                    _dirty = true;
                }
            }
            else if (inUse)
            {
                OwnedPill(skin, button, GameTexts.Gear.InUse);
            }
            else if (rod.Owned)
            {
                OwnedPill(skin, button, GameTexts.Shop.Owned);
            }
            else if (!rod.IsFree)
            {
                // Rods are not sold (M24-T32): "Montar" when the service allows it (M24-T58, A-180), otherwise why not.
                if (AssembleAction(skin, button, Icons.Rod, rod.AssembleBlocker, rod.AssembleBlocker == ServiceError.RodNeedsPrevious ? GameTexts.Gear.NeedsRod(rod.PreviousRodName) : null))
                {
                    _root.AssembleRod(rod.RodId);
                    _dirty = true;
                }
            }
            else if (rod.BuyBlocker != ServiceError.None)
            {
                Blocked(skin, button, rod.BuyBlocker);
            }
            else if (skin.IconButton(button, Icons.Buy, GameTexts.Shop.ClaimFree, skin.ButtonPrimary))
            {
                _root.BuyRod(rod.RodId);
                _dirty = true;
            }
        }

        /// <summary>
        /// "Fragmentos 12/25 · Caem no mapa …" with a bar and "x% de chance por peixe puxado" while the item is not
        /// assembled; "Fragmentos extras: N · Caem no mapa …" once it is (boats and rods, M24-T16/T32).
        /// </summary>
        private static void FragmentLines(UiSkin skin, float x, ref float y, float w, bool owned, long have, int required, string where, double chance)
        {
            var fragments = owned
                ? GameTexts.Gear.FragmentsExtra(Format.Number(have))
                : GameTexts.Gear.FragmentsLine(Format.Number(have), Format.Number(required));
            // Two short lines instead of one long sentence (A-190): "Fragmentos 12/25" with the bar, then where they drop
            // and the chance, small.
            var style = owned ? skin.SmallMuted : skin.SmallBold;
            GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), FishCard.Fit(owned ? fragments + " · " + where : fragments, style, w), style);
            y += 22f;
            if (owned)
            {
                return;
            }

            skin.Bar(new Rect(x, y + 2f, w, 10f), required > 0 ? have / (float)required : 0f, true);
            y += 18f;
            var text = where + " · " + GameTexts.Gear.FragmentsChanceShort(Format.Percent(chance, chance < 0.01 ? 2 : 1));
            GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), FishCard.Fit(text, skin.SmallMuted, w), skin.SmallMuted);
            y += 22f;
        }

        /// <summary>
        /// The button row of a boat or rod not owned yet (M24-T58, A-180): "Montar" when the service says it can be assembled
        /// (returns true when pressed); "Precisa do barco X" / "Precisa da vara X" when the one before it is missing; the gold
        /// "MONTE COM FRAGMENTOS" pill while fragments are missing.
        /// </summary>
        private static bool AssembleAction(UiSkin skin, Rect button, string icon, ServiceError blocker, string needsText)
        {
            if (blocker == ServiceError.None)
            {
                return skin.IconButton(button, Icons.Check, GameTexts.Gear.Assemble, skin.ButtonPrimary);
            }

            if (needsText != null)
            {
                var text = FishCard.Fit(needsText, skin.SmallBold, button.width - 40f);
                var width = Mathf.Min(button.width, skin.PillWidth(text, true) + 6f);
                skin.AccentPill(new Rect(button.center.x - width / 2f, button.center.y - 13f, width, 26f), text, UiSkin.Gold, Icons.Lock);
                return false;
            }

            AssemblePill(skin, button, icon);
            return false;
        }

        /// <summary>The gold "MONTE COM FRAGMENTOS" pill in the button's place (boats and rods are not sold).</summary>
        private static void AssemblePill(UiSkin skin, Rect button, string icon)
        {
            var hint = GameTexts.Gear.AssembleHint.ToUpperInvariant();
            var hw = skin.PillWidth(hint, true) + 6f;
            skin.AccentPill(new Rect(button.center.x - hw / 2f, button.center.y - 13f, hw, 26f), hint, UiSkin.Gold, icon);
        }

        /// <summary>
        /// A clothing piece (M24-T33, A-164): its level and use XP, the bonus now and at the top level, and, while it is
        /// not assembled, the Fragmentos de Roupa and "Montar (N fragmentos de roupa)".
        /// </summary>
        private void DrawClothingCard(UiSkin skin, Rect inner, ClothingPieceView piece, ClothingView clothing)
        {
            var x = inner.x;
            var w = inner.width;
            var y = inner.y;
            GUI.Label(new Rect(x, y, w, 30f), FishCard.Fit(piece.Name, skin.Heading, w), skin.Heading);
            y += 30f;
            var levelText = piece.Owned ? GameTexts.Shop.LevelOf(piece.Level, piece.MaxLevel) : GameTexts.Shop.MaxLevelOf(piece.MaxLevel);
            var levelStyle = piece.Owned ? skin.SmallGold : skin.SmallMuted;
            GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), FishCard.Fit(GameTexts.Gear.ClothingPiece + " · " + levelText, levelStyle, w), levelStyle);
            y += 24f;
            if (!string.IsNullOrEmpty(piece.Description))
            {
                y = WrappedLabel(new Rect(x, y, w, 0f), piece.Description, skin.SmallMuted) + 6f;
            }

            // The bonus now (Nv.1 before assembling) in big letters, its bar against the top level, then the top level.
            GUI.Label(new Rect(x, y, w, 28f), FishCard.Fit(GameTexts.Gear.ClothingAxis(piece.Axis, Format.PercentShort(piece.Bonus)), _bonusBig, w), _bonusBig);
            y += 30f;
            var bar = new Rect(x, y, w, 6f);
            Fill(skin, bar, new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.9f), 3f);
            var fill = piece.BonusAtMax > 0 ? bar.width * Mathf.Clamp01((float)(piece.Bonus / piece.BonusAtMax)) : 0f;
            if (fill >= 6f)
            {
                Fill(skin, new Rect(bar.x, bar.y, fill, bar.height), UiSkin.Accent, 3f);
            }

            y += 14f;
            var atMax = GameTexts.Gear.ClothingBonusAtMax(piece.MaxLevel, GameTexts.Gear.ClothingAxis(piece.Axis, Format.Percent(piece.BonusAtMax, 0)));
            GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), FishCard.Fit(atMax, skin.Small, w), skin.Small);
            y += 24f;

            if (piece.Owned)
            {
                // It goes up by itself with use: the XP bar of the level (one point per fish pulled out).
                if (piece.Level >= piece.MaxLevel)
                {
                    GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), FishCard.Fit(GameTexts.Gear.ClothingMaxLevel, skin.SmallGold, w), skin.SmallGold);
                    y += 22f;
                }
                else
                {
                    var use = GameTexts.Gear.ClothingUseXp(Format.Number(piece.Xp), Format.Number(piece.XpToNext));
                    GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), FishCard.Fit(use, skin.SmallBold, w), skin.SmallBold);
                    y += 22f;
                    skin.Bar(new Rect(x, y + 2f, w, 10f), piece.XpToNext > 0 ? piece.Xp / (float)piece.XpToNext : 0f, true);
                    y += 18f;
                }
            }
            else if (clothing != null)
            {
                FragmentLines(skin, x, ref y, w, false, clothing.Fragments, clothing.NextCost, GameTexts.Gear.FragmentsEverywhere, clothing.FragmentChance);
            }

            var button = new Rect(x, inner.yMax - 44f, w, 44f);
            Note(skin, new Rect(x, y + 6f, w, button.y - 8f - y - 6f), GameTexts.Gear.ClothingNote);
            if (piece.Owned)
            {
                OwnedPill(skin, button, GameTexts.Gear.Wearing);
            }
            else if (piece.AssembleBlocker != ServiceError.None)
            {
                Blocked(skin, button, piece.AssembleBlocker);
            }
            else if (skin.IconButton(button, Icons.Check, GameTexts.Gear.ClothingAssemble(Format.Number(clothing?.NextCost ?? 0)), skin.ButtonPrimary))
            {
                _root.AssembleClothing(piece.PieceId);
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
            // Owned boats with levels show "Nível 12/100" (M24-T13); the others how far they go.
            var levels = boat.HasLevels && boat.MaxLevel > 1;
            var owned = levels && boat.Owned;
            var tierLine = GameTexts.Profile.BoatTier(boat.Tier) + (owned ? " · " + GameTexts.Shop.LevelOf(boat.Level, boat.MaxLevel) : levels ? " · " + GameTexts.Shop.MaxLevelOf(boat.MaxLevel) : string.Empty);
            GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), FishCard.Fit(tierLine, owned ? skin.SmallGold : skin.SmallMuted, w), owned ? skin.SmallGold : skin.SmallMuted);
            y += 24f;
            if (!string.IsNullOrEmpty(boat.Description))
            {
                y = WrappedLabel(new Rect(x, y, w, 0f), boat.Description, skin.SmallMuted) + 6f;
            }

            var max = 0.0;
            foreach (var b in gear.Boats)
            {
                max = System.Math.Max(max, System.Math.Max(b.Bonus, b.BonusAtMax));
            }

            BigBonus(skin, x, ref y, w, boat.Bonus, max, levels ? 1 : 0);
            if (levels)
            {
                // The next level (small steps, with one decimal) or what the top level gives.
                var line = owned && boat.Level < boat.MaxLevel
                    ? GameTexts.Gear.NextLevelBonus(Format.Percent(boat.BonusNextLevel, 1))
                    : GameTexts.Gear.MaxLevelBonus(boat.MaxLevel, Format.Percent(boat.BonusAtMax, 1));
                GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), FishCard.Fit(line, skin.Small, w), skin.Small);
                y += 22f;
            }

            // Boats by fragments (M24-T16, A-157): how many there are ("Fragmentos 12/40 · Caem em todos os mapas", M24-T47),
            // with a bar while the boat is not assembled; the extras once it is.
            if (boat.FragmentsRequired > 0)
            {
                FragmentLines(skin, x, ref y, w, boat.Owned, boat.Fragments, boat.FragmentsRequired, GameTexts.Gear.FragmentsEverywhere, boat.FragmentChance);
            }

            // Owned with levels: the Use button / "Em uso" pill sits over the upgrade row.
            var button = new Rect(x, inner.yMax - 44f, w, 44f);
            var use = owned ? new Rect(x, button.y - 50f, w, 40f) : button;
            var price = new Rect(x, use.y - 32f, w, 24f);
            Note(skin, new Rect(x, y + 6f, w, (owned ? price.y : button.y) - 8f - y - 6f), GameTexts.Gear.BoatsNote);
            if (owned && boat.NextUpgradeCoins > 0)
            {
                Cost(skin, new Rect(price.x, price.y + 1f, price.width, 22f), boat.NextUpgradeCoins, boat.NextUpgradeShells);
            }

            if (owned)
            {
                if (boat.UpgradeBlocker == ServiceError.BoatAtMaxLevel)
                {
                    OwnedPill(skin, button, GameTexts.Shop.MaxLevel);
                }
                else if (boat.UpgradeBlocker != ServiceError.None)
                {
                    Blocked(skin, button, boat.UpgradeBlocker);
                }
                else if (skin.IconButton(button, Icons.Level, GameTexts.Shop.UpgradeTo(boat.Level + 1), skin.ButtonPrimary))
                {
                    _root.UpgradeBoat(boat.BoatId);
                }
            }

            if (boat.InUse)
            {
                OwnedPill(skin, use, GameTexts.Gear.InUse);
            }
            else if (boat.Owned)
            {
                if (skin.IconButton(use, Icons.Swap, GameTexts.Gear.Use, skin.Button))
                {
                    _root.UseBoat(boat.BoatId);
                }
            }
            else if (AssembleAction(skin, button, Icons.Boat, boat.AssembleBlocker, boat.AssembleBlocker == ServiceError.BoatNeedsPrevious ? GameTexts.Gear.NeedsBoat(boat.PreviousBoatName) : null))
            {
                // Boats are not sold: "Montar" when the service allows it (M24-T58, A-180), otherwise why not.
                _root.AssembleBoat(boat.BoatId);
                _dirty = true;
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

            // Bottom: price, Comprar, and Usar / Guardar under it.
            var use = new Rect(x, inner.yMax - 40f, w, 40f);
            var buy = new Rect(x, use.y - 50f, w, 44f);
            var price = new Rect(x, buy.y - 32f, w, 24f);
            Note(skin, new Rect(x, y + 6f, w, price.y - 8f - y - 6f), GameTexts.Gear.BaitsNote);
            Cost(skin, new Rect(price.x, price.y + 1f, price.width, 22f), bait.CostCoins, bait.CostShells);

            if (bait.BuyBlocker != ServiceError.None)
            {
                Blocked(skin, buy, bait.BuyBlocker);
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

        /// <summary>"No Nível 1" (or "No Nível 37 (agora)") and "No nível máximo" with their two bar shades.</summary>
        private static void Legend(UiSkin skin, float x, float y, float w, string first)
        {
            var half = w / 2f;
            Fill(skin, new Rect(x, y + 6f, 14f, 8f), UiSkin.Accent, 3f);
            GUI.Label(new Rect(x + 20f, y, half - 24f, UiSkin.SmallLine), FishCard.Fit(first, skin.SmallMuted, half - 24f), skin.SmallMuted);
            Fill(skin, new Rect(x + half, y + 6f, 14f, 8f), new Color(UiSkin.Accent.r, UiSkin.Accent.g, UiSkin.Accent.b, 0.4f), 3f);
            GUI.Label(new Rect(x + half + 20f, y, half - 20f, UiSkin.SmallLine), FishCard.Fit(GameTexts.Shop.AtMax, skin.SmallMuted, half - 20f), skin.SmallMuted);
        }

        /// <summary>
        /// One bonus: label, "+x% → +y%" and a bar with the level-1 (or current) value over the max-level one. Whole
        /// percentages, except the current value of a copy, which moves in small steps (one decimal when needed).
        /// </summary>
        private static void BonusBar(UiSkin skin, Rect r, string label, double at1, double atMax, double scale, bool now = false)
        {
            var value = "+" + (now ? Format.PercentShort(at1) : Format.Percent(at1, 0)) + " → +" + Format.Percent(atMax, 0);
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
        private void BigBonus(UiSkin skin, float x, ref float y, float w, double bonus, double max, int decimals = 0)
        {
            GUI.Label(new Rect(x, y, w, 28f), FishCard.Fit(GameTexts.Gear.Bonus(Format.Percent(bonus, decimals)), _bonusBig, w), _bonusBig);
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

        /// <summary>
        /// "Seu equipamento" (A-190): rod, boat and bait in three cards with their bonus, and the total, on the plain panel.
        /// </summary>
        private void DrawBox(UiSkin skin, Rect rect, GearView gear)
        {
            var x = rect.x;
            var w = rect.width;
            var y = rect.y;
            GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), GameTexts.Gear.Yours, skin.SmallBold);

            // The total on the right of the title (with the Luvas, M24-T33, the label says so).
            var total = "+" + Format.Percent(gear.TotalBonus, 0);
            var totalLabel = gear.ClothingBonus > 0 ? GameTexts.Gear.TotalWithGloves : GameTexts.Gear.Total;
            var totalWidth = skin.SmallGold.CalcSize(new GUIContent(total)).x + 4f;
            var labelWidth = skin.SmallMuted.CalcSize(new GUIContent(totalLabel)).x + 4f;
            GUI.Label(new Rect(rect.xMax - totalWidth, y, totalWidth, UiSkin.SmallLine), total, skin.SmallGold);
            GUI.Label(new Rect(rect.xMax - totalWidth - 8f - labelWidth, y, labelWidth, UiSkin.SmallLine), totalLabel, skin.SmallMuted);
            y += UiSkin.SmallLine + 8f;

            var cw = (w - 16f) / 3f;
            var ch = rect.yMax - y;
            Compartment(skin, new Rect(x, y, cw, ch), GameTexts.Gear.Rod, "Varas/" + gear.RodId, Icons.Rod, gear.RodName, gear.RodBonus, true);
            Compartment(skin, new Rect(x + cw + 8f, y, cw, ch), GameTexts.Gear.Boat, "Barcos/" + gear.BoatId, Icons.Boat, gear.BoatName, gear.BoatBonus, true);
            var bait = gear.BaitName != null ? gear.BaitName + " · " + Format.Number(gear.BaitChargesLeft) : GameTexts.Gear.NoBait;
            Compartment(skin, new Rect(x + (cw + 8f) * 2f, y, cw, ch), GameTexts.Gear.Bait, gear.BaitId != null ? "Iscas/" + gear.BaitId : null, Icons.Bait, bait, gear.BaitBonus, gear.BaitName != null);
        }

        private void Compartment(UiSkin skin, Rect r, string label, string art, string icon, string name, double bonus, bool has)
        {
            skin.LineBox(r, UiSkin.CardColor, UiSkin.ChipOffColor, 1);
            GUI.Label(new Rect(r.x + 8f, r.y + 6f, r.width - 16f, UiSkin.SmallLine), FishCard.Fit(label, skin.SmallMuted, r.width - 16f), skin.SmallMutedCenter);

            var picture = new Rect(r.x + 8f, r.y + 30f, r.width - 16f, Mathf.Max(24f, r.height - 30f - 2f * UiSkin.SmallLine - 8f));
            var tex = art != null ? ArtAssets.Texture(art) : null;
            if (tex != null)
            {
                PixelDraw.Fit(picture, tex);
            }
            else
            {
                skin.DrawIcon(new Rect(picture.center.x - 13f, picture.center.y - 13f, 26f, 26f), icon, has ? UiSkin.Accent : UiSkin.Muted);
            }

            var by = r.yMax - 6f - 2f * UiSkin.SmallLine;
            GUI.Label(new Rect(r.x + 4f, by, r.width - 8f, UiSkin.SmallLine), FishCard.Fit(name, _boldCenter, r.width - 8f), _boldCenter);
            var previous = GUI.contentColor;
            GUI.contentColor = has ? TealLight : UiSkin.Muted;
            GUI.Label(new Rect(r.x + 4f, by + UiSkin.SmallLine, r.width - 8f, UiSkin.SmallLine), "+" + Format.Percent(bonus, 0), _boldCenter);
            GUI.contentColor = previous;
        }

        /// <summary>
        /// "Chance de puxar o peixe" (A-190): one solid pill per rarity with the percentage inside ("Épico 10%"), in rows
        /// that wrap; the ones that do not bite here are soft outlined pills, with "não morde aqui" under them. Nothing
        /// is cut or turned. The explanation is behind the "i".
        /// </summary>
        private void DrawPond(UiSkin skin, Rect rect, GearView gear)
        {
            GUI.Label(new Rect(rect.x, rect.y, rect.width, UiSkin.SmallLine), FishCard.Fit(GameTexts.Gear.ChanceTitle, skin.SmallBold, rect.width), skin.SmallBold);
            var count = gear.Chances.Count;
            if (count == 0)
            {
                return;
            }

            const float pillHeight = 32f, gap = 8f;
            var x = rect.x;
            var y = rect.y + UiSkin.SmallLine + 8f;
            var anyAway = false;
            for (var i = 0; i < count; i++)
            {
                var chance = gear.Chances[i];
                var text = GameTexts.Fishing.ChanceShort(chance.RarityName, Format.Percent(chance.Chance, 0));
                var w = Mathf.Min(rect.width, skin.PillText.CalcSize(new GUIContent(text)).x + 24f);
                if (x + w > rect.xMax && x > rect.x)
                {
                    x = rect.x;
                    y += pillHeight + gap;
                }

                var pill = new Rect(x, y, w, pillHeight);
                if (chance.BitesHere)
                {
                    skin.Tag(pill, text, UiSkin.RarityColor(chance.RarityId));
                }
                else
                {
                    skin.PixelPill(pill, Color.clear, UiSkin.ChipOffColor);
                    var previous = GUI.contentColor;
                    GUI.contentColor = UiSkin.Muted;
                    GUI.Label(pill, text, skin.PillText);
                    GUI.contentColor = previous;
                    anyAway = true;
                }

                x += w + gap;
            }

            if (anyAway)
            {
                y += pillHeight + 8f;
                GUI.Label(new Rect(rect.x, y, rect.width, UiSkin.SmallLine), FishCard.Fit(GameTexts.Gear.NotHereLegend, _mutedTop, rect.width), _mutedTop);
            }
        }

        // ------------------------------------------------------------------ VIP (A-110)

        private void DrawVip(UiSkin skin, Rect content)
        {
            var vip = _root.Vip;
            if (vip == null)
            {
                return;
            }

            // The same card as before, on the plain panel (A-190).

            var note = vip.OfflineCycleSeconds > 0
                ? GameTexts.Vip.NoteWithCycle(Format.Duration(vip.OfflineCycleSeconds), Format.Duration(vip.NormalOfflineCycleSeconds))
                : GameTexts.Vip.Note;
            GUI.Label(new Rect(content.x, content.y, content.width, 40), note, skin.SmallMuted);
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

        /// <summary>A fill in the ARC skin: a pixel shape in place of the old rounded rect (UiSkin.ArcFill, A-188).</summary>
        private static void Fill(UiSkin skin, Rect rect, Color color, float radius)
        {
            skin.ArcFill(rect, color, radius);
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
            // The rarity names under the bobbers at the chip size: "Lendário" fits a fifth of the pond.
            _mutedTop = new GUIStyle(skin.SmallMuted) { alignment = TextAnchor.UpperCenter, wordWrap = true };
            _bonusBig = new GUIStyle(skin.Heading) { wordWrap = false, clipping = TextClipping.Clip };
            _bonusBig.normal.textColor = TealLight;
        }
    }
}
