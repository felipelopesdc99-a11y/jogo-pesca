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

        // Footer buttons, wide enough for their Nunito SemiBold 24 labels with the icon (A-191).
        private const float SelectWidth = 240f, KeepWidth = 272f, SellWidth = 288f;

        // One "Filtros" button (M25-T24, A-191) opens order, size and quality in three columns.
        private const float FiltersWidth = 168f, FiltersHeight = 40f, FilterColumn = 232f, FilterItem = 36f, FilterHeading = 32f;

        // The deck with the ice chest (A-179): a short strip on top, so the grid keeps two full rows of cards.
        private const string BannerArt = "UI/caixa_cena";
        private const float BannerHeight = 110f, BannerLowHeight = 72f, BannerGap = 18f;

        /// <summary>The Box orders (A-119): the five the owner chose in the mock-up "Exemplo 2".</summary>
        private enum SortOrder
        {
            Newest,
            Valuable,
            Largest,
            Rarest,
            Species,
            Quality,
        }

        private bool _filtersOpen;
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
        // The quality filter (M24-T52): the least stars a fish needs to show; 0 = every fish.
        private int _starsFilter;
        private bool _review;
        private string _search = string.Empty;
        private SortOrder _sort;
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

        /// <summary>True while the "Revisar peixes / Confirmar" check of a sale is open.</summary>
        public bool HasDialog => _pendingConfirmation != null;

        public void Open()
        {
            IsOpen = true;
            _dirty = true;
            _rarityFilter = null;
            _sizeFilter = null;
            _starsFilter = 0;
            _review = false;
            _search = string.Empty;
            _sort = SortOrder.Newest;
            _filtersOpen = false;
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
            // A-148: the fish count is a counter, next to the sign; the search (300 + 16) keeps its place.
            if (WindowFrame.Header(skin, panel, GameTexts.Box.Title, GeneUi.WithInfo(GameTexts.Box.Info, _root), Icons.Box, capacity > 0 ? GameTexts.Box.CountOf(boxCount, capacity) : GameTexts.Box.Count(_catches.Count), 316f))
            {
                Close();
            }

            // Search by name, next to the seal (the same place in the Aquarium; the header keeps 316 for it).
            var search = NameSearch.Field(skin, new Rect(WindowFrame.HeaderRight(panel) - 300, panel.y + 24, 300, 38), _search, "busca_caixa");
            if (search != _search)
            {
                _search = search;
                FiltersChanged();
            }

            // A game bag, not a spreadsheet (owner's request, A-118): rarity tabs with counts, and one "Filtros" button on
            // the right for the order, the size and the quality (M25-T24, A-191).
            var left = panel.x + 28;
            var right = panel.xMax - 28;
            var tabsY = panel.y + 82;

            // Tabs and the button share a row on a wide window; on a narrow one the button goes to a short row below,
            // next to how many fish the filter shows.
            var sameRow = TabsWidth(skin) + 16f + FiltersWidth <= right - left;

            // The scene strip (a header strip, A-190 rule 10); lower when the button needs its own row.
            var bannerHeight = sameRow ? BannerHeight : BannerLowHeight;
            // The pixel deck at a whole scale, cut at the edges, centred low on the ice chest full of fish (M25-T15).
            if (WindowFrame.PhotoArt(skin, new Rect(left, tabsY, right - left, bannerHeight), BannerArt, 0.5f, 0.7f) != null)
            {
                tabsY += bannerHeight + BannerGap;
            }

            DrawRarityTabs(skin, new Rect(left, tabsY, sameRow ? right - left - FiltersWidth - 16f : right - left, TabHeight));
            var buttonY = sameRow ? tabsY + (TabHeight - FiltersHeight) / 2f : tabsY + TabHeight + 8f;
            if (!sameRow || ActiveFilters() > 0)
            {
                // How many fish the filters leave, left of the button (on its own row, or under the tabs' row end).
                var showing = GameTexts.Box.Showing(_visible.Count);
                var sw = skin.SmallMuted.CalcSize(new GUIContent(showing)).x + 4f;
                var sx = sameRow ? right - FiltersWidth - 16f - sw : left + 4f;
                if (!sameRow || sx > left + TabsWidth(skin))
                {
                    GUI.Label(new Rect(sx, buttonY + 8f, sw, UiSkin.SmallLine), showing, skin.SmallMuted);
                }
            }

            var filtersButton = new Rect(right - FiltersWidth, buttonY, FiltersWidth, FiltersHeight);
            if (FiltersButton(skin, filtersButton))
            {
                _filtersOpen = !_filtersOpen;
            }

            // The open panel sits over the grid: its clicks are handled before the grid so a card under it is never picked
            // by mistake, and it is drawn after the grid so it shows on top.
            var filtersRect = FiltersRect(filtersButton);
            if (_filtersOpen)
            {
                DrawFilters(skin, filtersRect, true);
            }

            // Grid, aligned with the tabs above it.
            var gridTop = (sameRow ? tabsY + TabHeight : buttonY + FiltersHeight) + 16f;
            var gridRect = new Rect(left, gridTop, right - left + 8f, panel.yMax - 112f - gridTop);
            DrawGrid(skin, gridRect);

            if (_filtersOpen)
            {
                DrawFilters(skin, filtersRect, false);
            }

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
                // No size bar (M25-T24): the line under the name already says the size and its category.
                Coins = Format.Short(c.SalePriceCoins),
                Selected = _selected.Contains(c.CatchId),
                Genes = c.Genes,
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

            // One button that alternates (M25-T24): "Selecionar todos" while a fish in sight is not selected, then
            // "Limpar seleção".
            var allSelected = _visible.Count > 0 && _visible.All(c => _selected.Contains(c.CatchId));
            if (allSelected ? GUI.Button(new Rect(x, y, SelectWidth, 40), GameTexts.Box.ClearSelection, skin.Button)
                            : skin.IconButton(new Rect(x, y, SelectWidth, 40), Icons.Check, GameTexts.Box.SelectAll, skin.Button))
            {
                if (allSelected)
                {
                    _selected.Clear();
                    if (_review)
                    {
                        _review = false;
                        ApplyFilter();
                    }
                }
                else
                {
                    foreach (var c in _visible)
                    {
                        _selected.Add(c.CatchId);
                    }
                }

                _preview = null;
            }

            // Recomputed only when the selection or the box changes, not on every GUI event.
            var preview = _preview ?? (_preview = _root.PreviewSale(_selected.ToList()));
            // The status gets the room actually free between the selection button and "Guardar no Aquário": how many are
            // selected, then the sale value as icon + number (A-190 rule 7; the word "Moedas" no longer cut).
            var keepX = panel.xMax - 28 - SellWidth - 16 - KeepWidth;
            var infoX = x + SelectWidth + 16;
            var infoWidth = Mathf.Max(0f, keepX - 16 - infoX);
            if (preview.Count == 0)
            {
                GUI.Label(new Rect(infoX, y + 6, infoWidth, 28), FishCard.Fit(GameTexts.Box.NothingSelected, skin.Body, infoWidth), skin.Body);
            }
            else
            {
                var coins = Format.Short(preview.TotalCoins);
                var coinsWidth = skin.CoinAmountWidth(coins, 24f, skin.Number);
                var countText = GameTexts.Box.SelectedShort(preview.Count);
                var countWidth = Mathf.Min(Mathf.Max(0f, infoWidth - coinsWidth - 16f), skin.Body.CalcSize(new GUIContent(countText)).x + 4f);
                GUI.Label(new Rect(infoX, y + 6, countWidth, 28), FishCard.Fit(countText, skin.Body, countWidth), skin.Body);
                skin.CoinAmount(new Rect(infoX + countWidth + 16f, y + 8, coinsWidth, 24), coins, skin.Number);
            }

            GUI.enabled = GUI.enabled && preview.Count > 0;
            if (skin.IconButton(new Rect(keepX, y, KeepWidth, 40), Icons.Aquarium, GameTexts.Aquarium.KeepSelected, skin.Button))
            {
                if (_root.KeepCatches(_selected.ToList()))
                {
                    _selected.Clear();
                    _preview = null;
                }

                Reload();
            }

            if (skin.IconButton(new Rect(panel.xMax - 28 - SellWidth, y, SellWidth, 40), Icons.Sell, GameTexts.Box.SellSelected, skin.ButtonPrimary))
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
            var player = _root.Player;
            var slotsText = player != null ? GameTexts.Aquarium.Slots(player.AquariumCount, player.AquariumCapacity) : null;
            var slotsWidth = slotsText != null ? Mathf.Min(332f, skin.SmallMutedRight.CalcSize(new GUIContent(slotsText)).x + 4f) : 0f;
            if (capacity > 0)
            {
                var fill = Mathf.Clamp01(boxCount / (float)capacity);
                var full = boxCount >= capacity;
                var warn = full || fill >= 0.9f;
                skin.DrawIcon(new Rect(x, lineY, 18, 18), warn ? Icons.Warning : Icons.Box, warn ? UiSkin.Gold : UiSkin.Muted);
                skin.Bar(new Rect(x + 26, lineY + 6, 180, 8), fill, warn);
                var noteStyle = warn ? skin.SmallGold : skin.SmallMuted;
                var noteWidth = Mathf.Max(0f, panel.xMax - 28 - slotsWidth - 16 - (x + 216));
                var note = full ? GameTexts.Box.Full : warn ? GameTexts.Box.AlmostFull : GameTexts.Box.LimitNote(capacity);
                GUI.Label(new Rect(x + 216, lineY - 3, noteWidth, UiSkin.SmallLine), FishCard.Fit(note, noteStyle, noteWidth), noteStyle);
            }

            if (slotsText != null)
            {
                GUI.Label(new Rect(panel.xMax - 28 - slotsWidth, lineY - 3, slotsWidth, UiSkin.SmallLine), slotsText, skin.SmallMutedRight);
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
            if (GUI.Button(new Rect(rect.x + 28, buttonsY, 192, 42), GameTexts.Dialogs.Review, skin.Button))
            {
                // Show exactly the valuable fish in this sale, so the player can deselect any.
                _pendingConfirmation = null;
                _review = true;
                ApplyFilter();
                _scroll = Vector2.zero;
            }

            if (GUI.Button(new Rect(rect.x + 236, buttonsY, 140, 42), GameTexts.Dialogs.Cancel, skin.Button))
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
            return TabLabel(skin).CalcSize(new GUIContent(label)).x + skin.SmallMuted.CalcSize(new GUIContent(Format.Number(count))).x + 48f;
        }

        private GUIStyle _tabLabel;

        /// <summary>The rarity tab's name: SemiBold at the small size, like every tab (A-191).</summary>
        private GUIStyle TabLabel(UiSkin skin) => _tabLabel ?? (_tabLabel = new GUIStyle(skin.ChipText) { alignment = TextAnchor.MiddleLeft });

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
                    skin.ArcFill(tab, new Color(color.r, color.g, color.b, active ? 0.16f : 0.07f), 10f);
                }

                if (active)
                {
                    skin.ArcFill(new Rect(tab.x + 10, tab.yMax - UiSkin.ArcPixel * 2f, tab.width - 20, UiSkin.ArcPixel), color, 0f);
                }

                // A coloured gem dot, the name, and the count in a small pill.
                var dim = count == 0 && !all;
                // A square pixel gem (4 × 4 art pixels), like the filter chips.
                var dot = UiSkin.ArcPixel * 4f;
                skin.ArcFill(new Rect(tab.x + 12, Mathf.Round(tab.center.y - dot / 2f), dot, dot), dim ? UiSkin.Muted : color, 0f);
                var labelColor = active ? Color.Lerp(color, Color.white, 0.6f) : dim ? UiSkin.Muted : UiSkin.Text;
                var lw = TabLabel(skin).CalcSize(new GUIContent(label)).x;
                UiSkin.ColoredLabel(new Rect(tab.x + 28, tab.y, lw + 4, tab.height), label, TabLabel(skin), labelColor);
                GUI.Label(new Rect(tab.x + 36 + lw, tab.y + (tab.height - UiSkin.SmallLine) / 2f, w - lw - 36, UiSkin.SmallLine), number, skin.SmallMuted);

                if (GUI.Button(tab, GUIContent.none, GUIStyle.none))
                {
                    _rarityFilter = all ? null : f.Key;
                    _filtersOpen = false;
                    FiltersChanged();
                }

                x += w + 4f;
            }
        }

        /// <summary>How many of the size and quality filters are on (the order is not a filter).</summary>
        private int ActiveFilters() => (_sizeFilter != null ? 1 : 0) + (_starsFilter > 0 ? 1 : 0);

        /// <summary>The "Filtros" button: the icon and the word, "Filtros · 2" with a gold line while filters are on.</summary>
        private bool FiltersButton(UiSkin skin, Rect rect)
        {
            var active = ActiveFilters();
            var clicked = GUI.Button(rect, GUIContent.none, _filtersOpen ? skin.ChipActive : skin.Chip);
            if (active > 0 && !_filtersOpen)
            {
                skin.DrawOutline(rect, UiSkin.Gold);
            }

            var text = active > 0 ? GameTexts.Box.FiltersOn(active) : GameTexts.Box.Filters;
            var style = TabLabel(skin);
            var tw = style.CalcSize(new GUIContent(text)).x;
            var x = rect.center.x - (20f + 8f + tw) / 2f;
            skin.DrawIcon(new Rect(x, rect.center.y - 10f, 20f, 20f), Icons.Sort, _filtersOpen ? Color.white : UiSkin.Accent);
            UiSkin.ColoredLabel(new Rect(x + 28f, rect.y, tw + 4f, rect.height), text, style, _filtersOpen ? UiSkin.TitleColor : UiSkin.Text);
            return clicked;
        }

        /// <summary>The filters panel under the button, right-aligned with it: order, size and quality in three columns.</summary>
        private Rect FiltersRect(Rect button)
        {
            var rows = Mathf.Max(SortCount, Mathf.Max(_sizeFilters.Count, TopStars()));
            var width = FilterColumn * 3f + 16f * 2f + 32f;
            var height = 16f + FilterHeading + rows * FilterItem + 16f;
            return new Rect(button.xMax - width, button.yMax + 8f, width, height);
        }

        private List<MenuEntry> SortEntries()
        {
            var items = new List<MenuEntry>();
            foreach (SortOrder order in System.Enum.GetValues(typeof(SortOrder)))
            {
                var picked = order;
                items.Add(new MenuEntry(SortName(order), SortIcon(order), UiSkin.Accent, _sort == order, () =>
                {
                    _sort = picked;
                    ApplyFilter();
                    _scroll = Vector2.zero;
                }));
            }

            return items;
        }

        private List<MenuEntry> SizeEntries()
        {
            var items = new List<MenuEntry>();
            foreach (var f in _sizeFilters)
            {
                var all = f.Key.Length == 0;
                var key = f.Key;
                items.Add(new MenuEntry(f.Value, null, all ? UiSkin.Accent : UiSkin.SizeColor(key), all ? _sizeFilter == null : _sizeFilter == key, () =>
                {
                    _sizeFilter = all ? null : key;
                    FiltersChanged();
                }));
            }

            return items;
        }

        /// <summary>The quality filter (M24-T52): every fish, or the top stars, then "N estrelas ou mais" down to 2.</summary>
        private List<MenuEntry> QualityEntries()
        {
            var items = new List<MenuEntry>();
            var top = TopStars();
            items.Add(new MenuEntry(GameTexts.Genes.FilterAll, null, UiSkin.Accent, _starsFilter == 0, () =>
            {
                _starsFilter = 0;
                FiltersChanged();
            }));
            for (var stars = top; stars >= 2; stars--)
            {
                var picked = stars;
                items.Add(new MenuEntry(StarsName(stars), null, UiSkin.Gold, _starsFilter == stars, () =>
                {
                    _starsFilter = picked;
                    FiltersChanged();
                }));
            }

            return items;
        }

        private static readonly int SortCount = System.Enum.GetValues(typeof(SortOrder)).Length;

        /// <summary>The top star count of the config (genes.quality_stars), 5.</summary>
        private int TopStars()
        {
            var bands = _root.Game?.Session.Config?.Progression?.Genes?.QualityStars;
            return bands != null && bands.Count > 0 ? bands.Count : 5;
        }

        private string StarsName(int stars)
        {
            return stars <= 0 ? GameTexts.Genes.FilterAll : GameTexts.Genes.FilterAtLeast(stars, TopStars());
        }

        private static string SortName(SortOrder order)
        {
            switch (order)
            {
                case SortOrder.Quality: return GameTexts.Box.SortQuality;
                case SortOrder.Valuable: return GameTexts.Box.SortValuable;
                case SortOrder.Largest: return GameTexts.Box.SortLargest;
                case SortOrder.Rarest: return GameTexts.Box.SortRarest;
                case SortOrder.Species: return GameTexts.Box.SortSpecies;
                default: return GameTexts.Box.SortNewest;
            }
        }

        private static string SortIcon(SortOrder order)
        {
            switch (order)
            {
                case SortOrder.Valuable: return Icons.Coin;
                case SortOrder.Largest: return Icons.Level;
                case SortOrder.Rarest: return Icons.Star;
                case SortOrder.Species: return Icons.Fish;
                case SortOrder.Quality: return Icons.Star;
                default: return Icons.Clock;
            }
        }

        private sealed class MenuEntry
        {
            public MenuEntry(string label, string icon, Color color, bool active, System.Action pick)
            {
                Label = label;
                Icon = icon;
                Color = color;
                Active = active;
                Pick = pick;
            }

            public string Label { get; }
            public string Icon { get; }
            public Color Color { get; }
            public bool Active { get; }
            public System.Action Pick { get; }
        }

        /// <summary>
        /// The filters panel. With <paramref name="input"/> it only handles the click (a pick, or a click outside that
        /// closes it); otherwise it only draws. One panel, three columns with a heading each, no box inside (A-190).
        /// </summary>
        private void DrawFilters(UiSkin skin, Rect rect, bool input)
        {
            var e = Event.current;
            if (input && e.type == EventType.MouseDown && !rect.Contains(e.mousePosition))
            {
                _filtersOpen = false;
                e.Use();
                return;
            }

            if (!input)
            {
                skin.DrawShadow(rect);
                GUI.Box(rect, GUIContent.none, skin.PanelSolid);
            }

            var columns = new[] { SortEntries(), SizeEntries(), QualityEntries() };
            var headings = new[] { GameTexts.Box.SortHeading, GameTexts.Box.SizeHeading, GameTexts.Box.QualityHeading };
            for (var c = 0; c < columns.Length; c++)
            {
                var column = new Rect(rect.x + 16f + c * (FilterColumn + 16f), rect.y + 16f, FilterColumn, rect.height - 32f);
                if (!input)
                {
                    GUI.Label(new Rect(column.x + 8f, column.y, column.width - 8f, UiSkin.SmallLine), headings[c], skin.SmallMuted);
                }

                var items = columns[c];
                for (var i = 0; i < items.Count; i++)
                {
                    var r = new Rect(column.x, column.y + FilterHeading + i * FilterItem, column.width, FilterItem - 4f);
                    if (input)
                    {
                        if (e.type == EventType.MouseDown && r.Contains(e.mousePosition))
                        {
                            items[i].Pick();
                            e.Use();
                            return;
                        }

                        continue;
                    }

                    DrawEntry(skin, r, items[i], r.Contains(e.mousePosition));
                }
            }

            if (input && e.type == EventType.MouseDown)
            {
                // A click on the panel between the items does nothing (and never reaches the grid).
                e.Use();
            }
        }

        private static void DrawEntry(UiSkin skin, Rect r, MenuEntry item, bool hovered)
        {
            if (item.Active || hovered)
            {
                skin.ArcFill(r, new Color(item.Color.r, item.Color.g, item.Color.b, item.Active ? 0.18f : 0.08f), 8f);
            }

            float tx;
            if (item.Icon != null)
            {
                skin.DrawIcon(new Rect(r.x + 8, r.center.y - 10, 20, 20), item.Icon, item.Active ? item.Color : UiSkin.Text);
                tx = r.x + 36;
            }
            else
            {
                var dot = UiSkin.ArcPixel * 3f;
                skin.ArcFill(new Rect(r.x + 12, Mathf.Round(r.center.y - dot / 2f), dot, dot), item.Color, 0f);
                tx = r.x + 28;
            }

            var style = item.Active ? skin.SmallBold : skin.Small;
            var tw = r.xMax - 28 - tx;
            GUI.Label(new Rect(tx, r.center.y - UiSkin.SmallLine / 2f, tw, UiSkin.SmallLine), FishCard.Fit(item.Label, style, tw), style);
            if (item.Active)
            {
                skin.DrawIcon(new Rect(r.xMax - 24, r.center.y - 8, 16, 16), Icons.Check, item.Color);
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

                if (_starsFilter > 0)
                {
                    query = query.Where(c => c.Genes != null && c.Genes.Stars >= _starsFilter);
                }

                if (!string.IsNullOrWhiteSpace(_search))
                {
                    query = query.Where(c => NameSearch.Matches(c.SpeciesName, _search));
                }
            }

            // The orders of A-119; ties always fall back to the newest first.
            var config = _root.Game?.Session.Config;
            switch (_sort)
            {
                case SortOrder.Valuable:
                    query = query.OrderByDescending(c => c.SalePriceCoins).ThenByDescending(c => c.CatchId);
                    break;
                case SortOrder.Largest:
                    // Large for its species, not in centimetres: a big Lambari beats a small Pirarucu.
                    query = query.OrderByDescending(c => c.SizePercentile).ThenByDescending(c => c.SizeCm).ThenByDescending(c => c.CatchId);
                    break;
                case SortOrder.Rarest:
                    query = query.OrderByDescending(c => config != null ? config.RarityRank(c.RarityId) : 0).ThenByDescending(c => c.SizePercentile).ThenByDescending(c => c.CatchId);
                    break;
                case SortOrder.Species:
                    query = query.OrderBy(c => c.SpeciesName, System.StringComparer.Create(System.Globalization.CultureInfo.GetCultureInfo("pt-BR"), true)).ThenByDescending(c => c.SalePriceCoins);
                    break;
                case SortOrder.Quality:
                    // The hunt (M24-T52): the best genes first, then the most perfect genes, then the newest.
                    query = query.OrderByDescending(c => c.Genes?.Total ?? 0).ThenByDescending(c => c.Genes?.PerfectCount ?? 0).ThenByDescending(c => c.CatchId);
                    break;
                default:
                    query = query.OrderByDescending(c => c.CatchId);
                    break;
            }

            // The scroll position is kept: new catches arriving must not throw the player back to the top.
            _visible = query.ToList();
        }
    }
}
