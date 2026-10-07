using System.Collections.Generic;
using System.Linq;
using FishingIdle.Game.Bootstrap;
using FishingIdle.Game.Visual;
using FishingIdle.GameService.Fishing;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The Fishing Box (GDD section 11): visual cards, a rarity filter and a size filter that combine
    /// (addendum A-079), multi-select,
    /// bulk sale, and a single "Revisar peixes / Confirmar" check when valuable fish are included.
    /// </summary>
    /// <remarks>
    /// Prices, protection flags and names all come from the game service's views. This window only
    /// displays them and sends the chosen ids back; the service validates and pays.
    /// </remarks>
    public sealed class FishingBoxWindow
    {
        private const float CardWidth = 212f;
        private const float CardHeight = 196f;
        private const float Gap = 12f;

        private const float TabHeight = 44f;
        private const float MenuWidth = 196f;

        private enum Menu
        {
            None,
            Size,
            Sort,
        }

        private Menu _menu;
        private readonly Dictionary<string, int> _rarityCounts = new Dictionary<string, int>();

        private readonly GameRoot _root;
        private readonly HashSet<long> _selected = new HashSet<long>();
        private IReadOnlyList<CatchView> _catches = new List<CatchView>();
        private List<CatchView> _visible = new List<CatchView>();
        private List<KeyValuePair<string, string>> _rarityFilters = new List<KeyValuePair<string, string>>();
        private List<KeyValuePair<string, string>> _sizeFilters = new List<KeyValuePair<string, string>>();

        // null = every rarity / every size. Review mode shows only the valuable fish of a pending sale.
        private string _rarityFilter;
        private string _sizeFilter;
        private bool _review;
        private string _search = string.Empty;
        private bool _byPrice;
        private Vector2 _scroll;
        private bool _dirty = true;
        private SalePreview _pendingConfirmation;
        private SalePreview _preview;

        public FishingBoxWindow(GameRoot root)
        {
            _root = root;
            _root.BoxChanged += () => _dirty = true;
        }

        public bool IsOpen { get; private set; }

        public void Open()
        {
            IsOpen = true;
            _dirty = true;
            _rarityFilter = null;
            _sizeFilter = null;
            _review = false;
            _search = string.Empty;
            _byPrice = false;
            _menu = Menu.None;
        }

        public void Close()
        {
            if (_pendingConfirmation != null)
            {
                _pendingConfirmation = null;
                return;
            }

            IsOpen = false;
            _selected.Clear();
            _preview = null;
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

            GUI.enabled = _pendingConfirmation == null;
            var panel = WindowFrame.Panel(skin, screenWidth, screenHeight, 1240f, 820f);
            var owner = _root.Player;
            var capacity = owner != null ? owner.FishingBoxCapacity : 0;
            var boxCount = owner != null ? owner.FishingBoxCount : _catches.Count;
            if (WindowFrame.Header(skin, panel, GameTexts.Box.Title, capacity > 0 ? GameTexts.Box.CountOf(boxCount, capacity) : GameTexts.Box.Count(_catches.Count), Icons.Box))
            {
                Close();
            }

            // Search by name, next to Fechar (the same place in the Aquarium).
            var search = NameSearch.Field(skin, new Rect(panel.xMax - 156 - 16 - 300, panel.y + 24, 300, 38), _search, "busca_caixa");
            if (search != _search)
            {
                _search = search;
                FiltersChanged();
            }

            // A game bag, not a spreadsheet (owner's request, A-118): rarity tabs with counts, and the size
            // filter and the order folded into two small menus on the right.
            var left = panel.x + 28;
            var right = panel.xMax - 28;
            var tabsY = panel.y + 98;
            var menusWidth = 2 * MenuWidth + 10f;

            // Tabs and menus share a row on a wide window; on a narrow one the menus go to a short row below,
            // next to how many fish the filter shows.
            var sameRow = TabsWidth(skin) + 16f + menusWidth <= right - left;
            DrawRarityTabs(skin, new Rect(left, tabsY, sameRow ? right - left - menusWidth - 16f : right - left, TabHeight));
            var menusY = sameRow ? tabsY + 4f : tabsY + TabHeight + 8f;
            if (!sameRow)
            {
                GUI.Label(new Rect(left + 4f, menusY + 9f, right - left - menusWidth - 20f, 20f), GameTexts.Box.Showing(_visible.Count), skin.SmallMuted);
            }

            var sizeMenu = new Rect(right - menusWidth, menusY, MenuWidth, 36f);
            var sortMenu = new Rect(right - MenuWidth, menusY, MenuWidth, 36f);
            if (MenuButton(skin, sizeMenu, GameTexts.Box.SizeMenu(SizeName(_sizeFilter)), _menu == Menu.Size, _sizeFilter != null ? UiSkin.SizeColor(_sizeFilter) : (Color?)null))
            {
                _menu = _menu == Menu.Size ? Menu.None : Menu.Size;
            }

            if (MenuButton(skin, sortMenu, GameTexts.Box.SortMenu(_byPrice ? GameTexts.Box.SortPrice : GameTexts.Box.SortNewest), _menu == Menu.Sort, null))
            {
                _menu = _menu == Menu.Sort ? Menu.None : Menu.Sort;
            }

            // The open menu sits over the grid: its clicks are handled before the grid so a card under it
            // is never picked by mistake, and it is drawn after the grid so it shows on top.
            var sizeRect = new Rect(sizeMenu.x, sizeMenu.yMax + 4f, MenuWidth, _sizeFilters.Count * 34f + 12f);
            var sortRect = new Rect(sortMenu.x, sortMenu.yMax + 4f, MenuWidth, 2 * 34f + 12f);
            if (_menu == Menu.Size) DrawSizeMenu(skin, sizeRect, true);
            else if (_menu == Menu.Sort) DrawSortMenu(skin, sortRect, true);

            // Grid, aligned with the tabs above it.
            var gridTop = (sameRow ? tabsY + TabHeight : menusY + 36f) + 14f;
            var gridRect = new Rect(left, gridTop, right - left + 8f, panel.yMax - 112f - gridTop);
            DrawGrid(skin, gridRect);

            if (_menu == Menu.Size) DrawSizeMenu(skin, sizeRect, false);
            else if (_menu == Menu.Sort) DrawSortMenu(skin, sortRect, false);

            // Footer
            DrawFooter(skin, panel, boxCount, capacity);
            GUI.enabled = true;

            if (_pendingConfirmation != null)
            {
                DrawConfirmation(skin, screenWidth, screenHeight);
            }
        }

        private void DrawGrid(UiSkin skin, Rect area)
        {
            if (_visible.Count == 0)
            {
                GUI.Label(new Rect(area.x, area.y + area.height / 2f - 20, area.width, 40),
                    _catches.Count == 0 ? GameTexts.Box.Empty : !string.IsNullOrWhiteSpace(_search) ? GameTexts.Search.NoMatch : GameTexts.Box.EmptyFilter, skin.Center);
                return;
            }

            var columns = Mathf.Max(1, Mathf.FloorToInt((area.width - 20 + Gap) / (CardWidth + Gap)));
            var rows = Mathf.CeilToInt(_visible.Count / (float)columns);
            var content = new Rect(0, 0, area.width - 20, rows * (CardHeight + Gap));
            var used = columns * (CardWidth + Gap) - Gap;
            var offsetX = 0f;

            _scroll = GUI.BeginScrollView(area, _scroll, content);

            // Only draw rows that are on screen, so a box with hundreds of fish stays smooth.
            var firstRow = Mathf.Max(0, Mathf.FloorToInt(_scroll.y / (CardHeight + Gap)));
            var lastRow = Mathf.Min(rows - 1, Mathf.CeilToInt((_scroll.y + area.height) / (CardHeight + Gap)));
            for (var row = firstRow; row <= lastRow; row++)
            {
                for (var col = 0; col < columns; col++)
                {
                    var index = row * columns + col;
                    if (index >= _visible.Count)
                    {
                        break;
                    }

                    var rect = new Rect(offsetX + col * (CardWidth + Gap), row * (CardHeight + Gap), CardWidth, CardHeight);
                    DrawCard(skin, rect, _visible[index]);
                }
            }

            GUI.EndScrollView();
        }

        private void DrawCard(UiSkin skin, Rect rect, CatchView c)
        {
            var model = new FishCardModel
            {
                SpeciesId = c.SpeciesId,
                Name = c.SpeciesName,
                Line = VisualTheme.IsSpecialSize(c.SizeCategoryId) ? Format.SizeCm(c.SizeCm) : Format.SizeCm(c.SizeCm) + " · " + c.SizeCategoryName,
                RarityId = c.RarityId,
                RarityName = c.RarityName,
                SizeCategoryId = c.SizeCategoryId,
                SizeCategoryName = c.SizeCategoryName,
                Bar = (float)c.SizePercentile,
                Coins = Format.Number(c.SalePriceCoins),
                Selected = _selected.Contains(c.CatchId),
            };
            FishCard.StatusBadge(model, c.IsNewSpecies, c.IsPersonalRecord);

            if (FishCard.Draw(skin, rect, model))
            {
                if (!_selected.Remove(c.CatchId))
                {
                    _selected.Add(c.CatchId);
                }

                _preview = null;
            }
        }

        private void DrawFooter(UiSkin skin, Rect panel, int boxCount, int capacity)
        {
            var y = panel.yMax - 84;
            var x = panel.x + 28;

            if (skin.IconButton(new Rect(x, y, 180, 40), Icons.Check, GameTexts.Box.SelectAll, skin.Button))
            {
                foreach (var c in _visible)
                {
                    _selected.Add(c.CatchId);
                }

                _preview = null;
            }

            if (GUI.Button(new Rect(x + 190, y, 170, 40), GameTexts.Box.ClearSelection, skin.Button))
            {
                _selected.Clear();
                _preview = null;
                if (_review)
                {
                    _review = false;
                    ApplyFilter();
                }
            }

            // Recomputed only when the selection or the box changes, not on every GUI event.
            var preview = _preview ?? (_preview = _root.PreviewSale(_selected.ToList()));
            var info = preview.Count == 0 ? GameTexts.Box.NothingSelected : GameTexts.Box.Selected(preview.Count, Format.Number(preview.TotalCoins));
            GUI.Label(new Rect(x + 380, y + 10, panel.width - 950, 24), info, skin.Body);

            GUI.enabled = GUI.enabled && preview.Count > 0;
            if (skin.IconButton(new Rect(panel.xMax - 528, y, 250, 40), Icons.Aquarium, GameTexts.Aquarium.KeepSelected, skin.Button))
            {
                if (_root.KeepCatches(_selected.ToList()))
                {
                    _selected.Clear();
                    _preview = null;
                }

                Reload();
            }

            if (skin.IconButton(new Rect(panel.xMax - 268, y, 240, 40), Icons.Sell, GameTexts.Box.SellSelected, skin.ButtonPrimary))
            {
                if (preview.NeedsConfirmation)
                {
                    _pendingConfirmation = preview;
                }
                else
                {
                    Sell();
                }
            }

            GUI.enabled = _pendingConfirmation == null;

            // Second line: how full the box is (OD-025) on the left, the Aquarium slots on the right.
            var lineY = y + 52;
            if (capacity > 0)
            {
                var fill = Mathf.Clamp01(boxCount / (float)capacity);
                var full = boxCount >= capacity;
                var warn = full || fill >= 0.9f;
                skin.DrawIcon(new Rect(x, lineY, 18, 18), warn ? Icons.Warning : Icons.Box, warn ? UiSkin.Gold : UiSkin.Muted);
                skin.Bar(new Rect(x + 26, lineY + 6, 180, 8), fill, warn);
                GUI.Label(new Rect(x + 216, lineY - 1, panel.width - 600, 22),
                    full ? GameTexts.Box.Full : warn ? GameTexts.Box.AlmostFull : GameTexts.Box.LimitNote(capacity),
                    warn ? skin.SmallGold : skin.SmallMuted);
            }

            var player = _root.Player;
            if (player != null)
            {
                GUI.Label(new Rect(panel.xMax - 360, lineY - 1, 332, 22), GameTexts.Aquarium.Slots(player.AquariumCount, player.AquariumCapacity), skin.SmallMutedRight);
            }
        }

        private void DrawConfirmation(UiSkin skin, float screenWidth, float screenHeight)
        {
            var protectedCatches = _pendingConfirmation.ProtectedCatches;
            var height = 250f + Mathf.Min(5, protectedCatches.Count) * 26f;
            var rect = WindowFrame.Dialog(skin, screenWidth, screenHeight, height, 660f);
            skin.DrawIcon(new Rect(rect.xMax - 64, rect.y + 24, 32, 32), Icons.Warning, UiSkin.Gold);

            GUI.Label(new Rect(rect.x + 28, rect.y + 24, rect.width - 56, 30), GameTexts.Dialogs.ValuableTitle, skin.Heading);
            GUI.Label(new Rect(rect.x + 28, rect.y + 62, rect.width - 56, 48), GameTexts.Dialogs.ValuableBody(protectedCatches.Count), skin.Body);

            var y = rect.y + 118;
            foreach (var c in protectedCatches.Take(5))
            {
                GUI.Label(new Rect(rect.x + 40, y, rect.width - 80, 24), "•  " + c.SpeciesName + " · " + Format.SizeCm(c.SizeCm) + " (" + c.SizeCategoryName + ")", skin.Small);
                y += 26;
            }

            var buttonsY = rect.yMax - 64;
            if (GUI.Button(new Rect(rect.x + 28, buttonsY, 190, 42), GameTexts.Dialogs.Review, skin.Button))
            {
                // Show exactly the valuable fish in this sale, so the player can deselect any.
                _pendingConfirmation = null;
                _review = true;
                ApplyFilter();
                _scroll = Vector2.zero;
            }

            if (GUI.Button(new Rect(rect.x + 228, buttonsY, 150, 42), GameTexts.Dialogs.Cancel, skin.Button))
            {
                _pendingConfirmation = null;
            }

            if (GUI.Button(new Rect(rect.xMax - 218, buttonsY, 190, 42), GameTexts.Dialogs.ConfirmButton, skin.ButtonPrimary))
            {
                _pendingConfirmation = null;
                Sell();
            }
        }

        private void Sell()
        {
            if (_root.Sell(_selected.ToList()))
            {
                _selected.Clear();
                _review = false;
            }

            Reload();
        }

        private void Reload()
        {
            _dirty = false;
            _catches = _root.GetFishingBox();
            var present = new HashSet<long>(_catches.Select(c => c.CatchId));
            _selected.RemoveWhere(id => !present.Contains(id));
            _preview = null;
            _rarityCounts.Clear();
            foreach (var fish in _catches)
            {
                var key = fish.RarityId ?? string.Empty;
                _rarityCounts[key] = (_rarityCounts.TryGetValue(key, out var seen) ? seen : 0) + 1;
            }

            BuildFilters();
            ApplyFilter();
        }

        private int CountOf(string rarityId) => _rarityCounts.TryGetValue(rarityId, out var n) ? n : 0;

        private void BuildFilters()
        {
            _rarityFilters = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>(string.Empty, GameTexts.Box.AllRarities) };
            _sizeFilters = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>(string.Empty, GameTexts.Box.AllSizes) };
            var config = _root.Game?.Session.Config;
            if (config == null)
            {
                return;
            }

            foreach (var tier in config.Progression.Rarity.Tiers)
            {
                _rarityFilters.Add(new KeyValuePair<string, string>(tier.Id, tier.DisplayName));
            }

            foreach (var category in config.SizeCategories)
            {
                _sizeFilters.Add(new KeyValuePair<string, string>(category.Id, category.DisplayName));
            }
        }

        private float TabWidth(UiSkin skin, string label, int count)
        {
            return skin.BodyBold.CalcSize(new GUIContent(label)).x + skin.SmallMuted.CalcSize(new GUIContent(Format.Number(count))).x + 54f;
        }

        private float TabsWidth(UiSkin skin)
        {
            var total = 0f;
            foreach (var f in _rarityFilters)
            {
                total += TabWidth(skin, f.Value, f.Key.Length == 0 ? _catches.Count : CountOf(f.Key)) + 4f;
            }

            return total;
        }

        /// <summary>Rarity tabs with how many fish of each are in the box; empty rarities are dimmed.</summary>
        private void DrawRarityTabs(UiSkin skin, Rect area)
        {
            var x = area.x;
            foreach (var f in _rarityFilters)
            {
                var all = f.Key.Length == 0;
                var count = all ? _catches.Count : CountOf(f.Key);
                var active = !_review && (all ? _rarityFilter == null : _rarityFilter == f.Key);
                var color = all ? UiSkin.Accent : UiSkin.RarityColor(f.Key);
                var label = f.Value;
                var number = Format.Number(count);
                var w = TabWidth(skin, label, count);
                if (x + w > area.xMax)
                {
                    break;
                }

                var tab = new Rect(x, area.y, w, area.height);
                var hovered = GUI.enabled && tab.Contains(Event.current.mousePosition);
                if (active || hovered)
                {
                    GUI.DrawTexture(tab, skin.White, ScaleMode.StretchToFill, true, 0, new Color(color.r, color.g, color.b, active ? 0.16f : 0.07f), 0, 10);
                }

                if (active)
                {
                    GUI.DrawTexture(new Rect(tab.x + 10, tab.yMax - 3, tab.width - 20, 3), skin.White, ScaleMode.StretchToFill, true, 0, color, 0, 1.5f);
                }

                // A coloured gem dot, the name, and the count in a small pill.
                var dim = count == 0 && !all;
                var dot = 10f;
                GUI.DrawTexture(new Rect(tab.x + 12, tab.center.y - dot / 2f, dot, dot), skin.White, ScaleMode.StretchToFill, true, 0, dim ? UiSkin.Muted : color, 0, dot / 2f);
                var previous = GUI.contentColor;
                GUI.contentColor = active ? Color.Lerp(color, Color.white, 0.6f) : dim ? UiSkin.Muted : UiSkin.Text;
                var lw = skin.BodyBold.CalcSize(new GUIContent(label)).x;
                GUI.Label(new Rect(tab.x + 28, tab.y + 11, lw + 4, 22), label, skin.BodyBold);
                GUI.contentColor = previous;
                GUI.Label(new Rect(tab.x + 34 + lw, tab.y + 13, w - lw - 34, 20), number, skin.SmallMuted);

                if (GUI.Button(tab, GUIContent.none, GUIStyle.none))
                {
                    _rarityFilter = all ? null : f.Key;
                    _menu = Menu.None;
                    FiltersChanged();
                }

                x += w + 4f;
            }
        }

        /// <summary>A small "Label: value ▾" button that opens a menu; tinted when a filter is on.</summary>
        private static bool MenuButton(UiSkin skin, Rect rect, string text, bool open, Color? accent)
        {
            var clicked = GUI.Button(rect, GUIContent.none, open ? skin.ChipActive : skin.Chip);
            if (accent.HasValue && !open)
            {
                skin.DrawOutline(rect, new Color(accent.Value.r, accent.Value.g, accent.Value.b, 0.9f));
            }

            GUI.Label(new Rect(rect.x + 12, rect.y + 8, rect.width - 40, 20), text, open ? skin.SmallBold : skin.Small);
            GUI.Label(new Rect(rect.xMax - 26, rect.y + 7, 18, 20), "▾", skin.SmallBold);
            return clicked;
        }

        private void DrawSizeMenu(UiSkin skin, Rect rect, bool input)
        {
            var items = new List<(string Label, Color Color, bool Active, System.Action Pick)>();
            foreach (var f in _sizeFilters)
            {
                var all = f.Key.Length == 0;
                var key = f.Key;
                items.Add((f.Value, all ? UiSkin.Accent : UiSkin.SizeColor(key), all ? _sizeFilter == null : _sizeFilter == key, () =>
                {
                    _sizeFilter = all ? null : key;
                    FiltersChanged();
                }));
            }

            MenuList(skin, rect, items, input);
        }

        private void DrawSortMenu(UiSkin skin, Rect rect, bool input)
        {
            var items = new List<(string Label, Color Color, bool Active, System.Action Pick)>();
            foreach (var option in new[] { false, true })
            {
                var byPrice = option;
                items.Add((option ? GameTexts.Box.SortPrice : GameTexts.Box.SortNewest, UiSkin.Accent, _byPrice == option, () =>
                {
                    _byPrice = byPrice;
                    ApplyFilter();
                    _scroll = Vector2.zero;
                }));
            }

            MenuList(skin, rect, items, input);
        }

        /// <summary>
        /// A dropdown list. With <paramref name="input"/> it only handles the click (a pick, or a click
        /// outside that closes it); otherwise it only draws.
        /// </summary>
        private void MenuList(UiSkin skin, Rect rect, List<(string Label, Color Color, bool Active, System.Action Pick)> items, bool input)
        {
            var e = Event.current;
            if (input)
            {
                if (e.type != EventType.MouseDown)
                {
                    return;
                }

                if (!rect.Contains(e.mousePosition))
                {
                    _menu = Menu.None;
                    e.Use();
                    return;
                }

                for (var i = 0; i < items.Count; i++)
                {
                    if (new Rect(rect.x + 6, rect.y + 6 + i * 34f, rect.width - 12, 32).Contains(e.mousePosition))
                    {
                        _menu = Menu.None;
                        items[i].Pick();
                        break;
                    }
                }

                e.Use();
                return;
            }

            skin.DrawShadow(rect);
            GUI.Box(rect, GUIContent.none, skin.PanelSolid);
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var r = new Rect(rect.x + 6, rect.y + 6 + i * 34f, rect.width - 12, 32);
                var hovered = r.Contains(e.mousePosition);
                if (item.Active || hovered)
                {
                    GUI.DrawTexture(r, skin.White, ScaleMode.StretchToFill, true, 0, new Color(item.Color.r, item.Color.g, item.Color.b, item.Active ? 0.18f : 0.08f), 0, 8);
                }

                var dot = 8f;
                GUI.DrawTexture(new Rect(r.x + 10, r.center.y - dot / 2f, dot, dot), skin.White, ScaleMode.StretchToFill, true, 0, item.Color, 0, dot / 2f);
                GUI.Label(new Rect(r.x + 26, r.y + 6, r.width - 54, 20), item.Label, item.Active ? skin.SmallBold : skin.Small);
                if (item.Active)
                {
                    skin.DrawIcon(new Rect(r.xMax - 26, r.y + 7, 18, 18), Icons.Check, item.Color);
                }
            }
        }

        private string SizeName(string id)
        {
            if (id == null)
            {
                return GameTexts.Box.AllSizes;
            }

            foreach (var f in _sizeFilters)
            {
                if (f.Key == id)
                {
                    return f.Value;
                }
            }

            return id;
        }

        private void FiltersChanged()
        {
            _review = false;
            ApplyFilter();
            _scroll = Vector2.zero;
        }

        private void ApplyFilter()
        {
            IEnumerable<CatchView> query = _catches;
            if (_review)
            {
                query = query.Where(c => c.IsProtected && _selected.Contains(c.CatchId));
            }
            else
            {
                // The two filters combine: e.g. Raro + Grande shows only large rare fish.
                if (_rarityFilter != null)
                {
                    query = query.Where(c => c.RarityId == _rarityFilter);
                }

                if (_sizeFilter != null)
                {
                    query = query.Where(c => c.SizeCategoryId == _sizeFilter);
                }

                if (!string.IsNullOrWhiteSpace(_search))
                {
                    query = query.Where(c => NameSearch.Matches(c.SpeciesName, _search));
                }
            }

            if (_byPrice)
            {
                // Most valuable first: the same price the card shows; newest first among equal prices.
                query = query.OrderByDescending(c => c.SalePriceCoins).ThenByDescending(c => c.CatchId);
            }

            // The scroll position is kept: new catches arriving must not throw the player back to the top.
            _visible = query.ToList();
        }
    }
}
