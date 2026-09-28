using System.Collections.Generic;
using System.Linq;
using FishingIdle.Game.Bootstrap;
using FishingIdle.Game.Scene;
using FishingIdle.GameService.Fishing;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The Fishing Box (GDD section 11): visual cards, one primary filter at a time, multi-select,
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

        private const string FilterAll = "all";
        private const string FilterReview = "review";

        private readonly GameRoot _root;
        private readonly HashSet<long> _selected = new HashSet<long>();
        private IReadOnlyList<CatchView> _catches = new List<CatchView>();
        private List<CatchView> _visible = new List<CatchView>();
        private List<KeyValuePair<string, string>> _filters = new List<KeyValuePair<string, string>>();
        private string _filter = FilterAll;
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
            _filter = FilterAll;
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

            GUI.DrawTexture(new Rect(0, 0, screenWidth, screenHeight), skin.Overlay);

            var width = Mathf.Min(1240f, screenWidth - 80f);
            var height = Mathf.Min(820f, screenHeight - 120f);
            var panel = new Rect((screenWidth - width) / 2f, (screenHeight - height) / 2f + 20f, width, height);

            GUI.enabled = _pendingConfirmation == null;
            GUI.Box(panel, GUIContent.none, skin.Panel);

            // Header
            GUI.Label(new Rect(panel.x + 28, panel.y + 22, 400, 32), GameTexts.Box.Title, skin.Title);
            GUI.Label(new Rect(panel.x + 30, panel.y + 58, 400, 22), GameTexts.Box.Count(_catches.Count), skin.SmallMuted);
            if (GUI.Button(new Rect(panel.xMax - 148, panel.y + 22, 120, 38), GameTexts.Box.Close, skin.Button))
            {
                Close();
            }

            // Filters (one at a time)
            var fx = panel.x + 28;
            foreach (var filter in _filters)
            {
                var label = new GUIContent(filter.Value);
                var w = skin.Chip.CalcSize(label).x + 8;
                if (GUI.Button(new Rect(fx, panel.y + 92, w, 32), label, _filter == filter.Key ? skin.ChipActive : skin.Chip))
                {
                    _filter = filter.Key;
                    ApplyFilter();
                }

                fx += w + 8;
            }

            // Grid
            var gridRect = new Rect(panel.x + 20, panel.y + 140, panel.width - 40, panel.height - 140 - 96);
            DrawGrid(skin, gridRect);

            // Footer
            DrawFooter(skin, panel);
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
                    _catches.Count == 0 ? GameTexts.Box.Empty : GameTexts.Box.EmptyFilter, skin.Center);
                return;
            }

            var columns = Mathf.Max(1, Mathf.FloorToInt((area.width - 20 + Gap) / (CardWidth + Gap)));
            var rows = Mathf.CeilToInt(_visible.Count / (float)columns);
            var content = new Rect(0, 0, area.width - 20, rows * (CardHeight + Gap));
            var used = columns * (CardWidth + Gap) - Gap;
            var offsetX = Mathf.Max(0f, (content.width - used) / 2f);

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
            var selected = _selected.Contains(c.CatchId);
            var hovered = rect.Contains(Event.current.mousePosition);
            var style = selected ? skin.CardSelected : c.IsImportant ? skin.CardImportant : hovered ? skin.CardHovered : skin.Card;

            if (GUI.Button(rect, GUIContent.none, style))
            {
                if (!_selected.Remove(c.CatchId))
                {
                    _selected.Add(c.CatchId);
                }

                _preview = null;
            }

            // Fish art first, numbers second (GDD section 44).
            var art = new Rect(rect.x + 16, rect.y + 12, rect.width - 32, 78);
            if (c.IsImportant)
            {
                var previous = GUI.color;
                GUI.color = new Color(1f, 0.85f, 0.4f, 0.35f + Mathf.Sin(Time.unscaledTime * 3f) * 0.1f);
                GUI.DrawTexture(new Rect(art.x - 10, art.y - 6, art.width + 20, art.height + 12), Art.Glow.texture, ScaleMode.StretchToFill, true);
                GUI.color = previous;
            }

            GUI.DrawTexture(art, Art.FishTexture(c.SpeciesId), ScaleMode.ScaleToFit, true);

            GUI.Label(new Rect(rect.x + 14, rect.y + 94, rect.width - 28, 22), c.SpeciesName, skin.BodyBold);
            GUI.Label(new Rect(rect.x + 14, rect.y + 116, rect.width - 28, 20), Format.SizeCm(c.SizeCm) + " · " + c.SizeCategoryName, skin.Small);
            skin.Bar(new Rect(rect.x + 14, rect.y + 140, rect.width - 28, 7), (float)c.SizePercentile, c.SizeCategoryId == "exceptional");

            skin.CoinIcon(new Rect(rect.x + 14, rect.y + 158, 18, 18));
            GUI.Label(new Rect(rect.x + 38, rect.y + 157, 100, 22), Format.Number(c.SalePriceCoins), skin.SmallGold);

            if (c.RarityId != null && c.RarityId != "common")
            {
                skin.Tag(new Rect(rect.xMax - 70, rect.y + 157, 58, 20), c.RarityName.ToUpperInvariant(), UiSkin.Rare);
            }

            var tagY = rect.y + 10;
            if (c.IsNewSpecies)
            {
                skin.Tag(new Rect(rect.x + 10, tagY, 104, 20), GameTexts.Box.NewSpeciesBadge, new Color(0.55f, 0.95f, 1f));
            }
            else if (c.IsPersonalRecord)
            {
                skin.Tag(new Rect(rect.x + 10, tagY, 76, 20), GameTexts.Box.RecordBadge, UiSkin.Gold);
            }

        }

        private void DrawFooter(UiSkin skin, Rect panel)
        {
            var y = panel.yMax - 84;
            var x = panel.x + 28;

            if (GUI.Button(new Rect(x, y, 180, 40), GameTexts.Box.SelectAll, skin.Button))
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
                if (_filter == FilterReview)
                {
                    _filter = FilterAll;
                    ApplyFilter();
                }
            }

            // Recomputed only when the selection or the box changes, not on every GUI event.
            var preview = _preview ?? (_preview = _root.PreviewSale(_selected.ToList()));
            var info = preview.Count == 0 ? GameTexts.Box.NothingSelected : GameTexts.Box.Selected(preview.Count, Format.Number(preview.TotalCoins));
            GUI.Label(new Rect(x + 380, y + 10, panel.width - 950, 24), info, skin.Body);

            GUI.enabled = GUI.enabled && preview.Count > 0;
            if (GUI.Button(new Rect(panel.xMax - 528, y, 250, 40), GameTexts.Aquarium.KeepSelected, skin.Button))
            {
                if (_root.KeepCatches(_selected.ToList()))
                {
                    _selected.Clear();
                    _preview = null;
                }

                Reload();
            }

            if (GUI.Button(new Rect(panel.xMax - 268, y, 240, 40), GameTexts.Box.SellSelected, skin.ButtonPrimary))
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
            var player = _root.Player;
            if (player != null)
            {
                GUI.Label(new Rect(x, y + 50, panel.width - 56, 22), GameTexts.Aquarium.Slots(player.AquariumCount, player.AquariumCapacity), skin.SmallMuted);
            }
        }

        private void DrawConfirmation(UiSkin skin, float screenWidth, float screenHeight)
        {
            var protectedCatches = _pendingConfirmation.ProtectedCatches;
            var height = 250f + Mathf.Min(5, protectedCatches.Count) * 26f;
            var rect = new Rect(screenWidth / 2f - 330, screenHeight / 2f - height / 2f, 660, height);
            GUI.DrawTexture(new Rect(0, 0, screenWidth, screenHeight), UiSkin.Get().Overlay);
            GUI.Box(rect, GUIContent.none, skin.CardImportant);

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
                _filter = FilterReview;
                ApplyFilter();
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
                if (_filter == FilterReview)
                {
                    _filter = FilterAll;
                }
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
            BuildFilters();
            ApplyFilter();
        }

        private void BuildFilters()
        {
            _filters = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>(FilterAll, GameTexts.Box.FilterAll) };
            var config = _root.Game?.Session.Config;
            if (config == null)
            {
                return;
            }

            foreach (var tier in config.Progression.Rarity.Tiers)
            {
                _filters.Add(new KeyValuePair<string, string>("rarity:" + tier.Id, tier.DisplayName));
            }

            foreach (var category in config.SizeCategories)
            {
                _filters.Add(new KeyValuePair<string, string>("size:" + category.Id, category.DisplayName));
            }
        }

        private void ApplyFilter()
        {
            IEnumerable<CatchView> query = _catches;
            if (_filter == FilterReview)
            {
                query = query.Where(c => c.IsProtected && _selected.Contains(c.CatchId));
            }
            else if (_filter.StartsWith("rarity:"))
            {
                var id = _filter.Substring("rarity:".Length);
                query = query.Where(c => c.RarityId == id);
            }
            else if (_filter.StartsWith("size:"))
            {
                var id = _filter.Substring("size:".Length);
                query = query.Where(c => c.SizeCategoryId == id);
            }

            _visible = query.ToList();
            _scroll = Vector2.zero;
        }
    }
}
