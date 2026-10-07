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
    /// The Aquarium (GDD sections 12, 13, 22): card grid of kept fish, the fish sheet with its
    /// attributes, feeding and selling. Everything shown comes from the game service's views;
    /// feeding and selling are requests the service validates.
    /// </summary>
    public sealed class AquariumWindow
    {
        private const float CardWidth = 196f;
        private const float CardHeight = 196f;
        private const float Gap = 12f;

        private static readonly AquariumSort[] Sorts = { AquariumSort.Size, AquariumSort.Level, AquariumSort.Species, AquariumSort.Newest };

        private readonly GameRoot _root;
        private AquariumView _aquarium;
        private AquariumSort _sort = AquariumSort.Size;
        private long _selectedId;
        private FishView _selected;
        private Vector2 _scroll;
        private bool _dirty = true;

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

        // Confirmations
        private bool _confirmSell;
        private bool _confirmSellMany;
        private bool _confirmFeed;

        public AquariumWindow(GameRoot root)
        {
            _root = root;
            _root.AquariumChanged += () => _dirty = true;
            _root.BoxChanged += () => _dirty = true;
        }

        public bool IsOpen { get; private set; }

        private bool DialogOpen => _confirmSell || _confirmSellMany || _confirmFeed;

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
            if (WindowFrame.Header(skin, panel, _feeding ? GameTexts.Aquarium.Title : GameTexts.Aquarium.Title,
                    GameTexts.Aquarium.Count(_aquarium.Count, _aquarium.Capacity), Icons.Aquarium))
            {
                Close();
            }

            // Search by name, next to Fechar (the same place in the Fishing Box).
            var search = NameSearch.Field(skin, new Rect(panel.xMax - 156 - 16 - 300, panel.y + 24, 300, 38), _search, "busca_aquario");
            if (search != _search)
            {
                _search = search;
                _scroll = Vector2.zero;
                _feedScroll = Vector2.zero;
            }

            var detailWidth = 420f;
            var left = new Rect(panel.x + 28, panel.y + 96, panel.width - detailWidth - 72, panel.height - 116);
            var right = new Rect(panel.xMax - detailWidth - 24, panel.y + 96, detailWidth, panel.height - 116);

            if (_feeding)
            {
                DrawFoodPicker(skin, left);
                DrawFeedPanel(skin, right);
            }
            else
            {
                DrawBrowser(skin, left, panel);
                if (_multi)
                {
                    DrawSelectionPanel(skin, right);
                }
                else
                {
                    DrawSheet(skin, right);
                }
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

        private void DrawBrowser(UiSkin skin, Rect area, Rect panel)
        {
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

            // "Selecionar vários" on the right of the sort row: turns clicks into a selection to sell.
            var multiLabel = new GUIContent(GameTexts.Aquarium.MultiSelect);
            var multiWidth = skin.Chip.CalcSize(multiLabel).x + 34;
            if (_aquarium.Count > 0 && skin.IconButton(new Rect(area.xMax - multiWidth, area.y, multiWidth, 32), Icons.Check, GameTexts.Aquarium.MultiSelect, _multi ? skin.ChipActive : skin.Chip))
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

            var grid = new Rect(area.x - 4, area.y + 46, area.width + 8, area.height - 46);
            if (_aquarium.Count == 0)
            {
                GUI.Label(new Rect(grid.x, grid.y + grid.height / 2f - 30, grid.width, 60), GameTexts.Aquarium.Empty, skin.Center);
                return;
            }

            var shown = _aquarium.Fish.Where(f => NameSearch.Matches(f.SpeciesName, _search)).ToList();
            _shown = shown;
            if (shown.Count == 0)
            {
                GUI.Label(new Rect(grid.x, grid.y + grid.height / 2f - 30, grid.width, 60), GameTexts.Search.NoMatch, skin.Center);
                return;
            }

            DrawGrid(grid, shown, ref _scroll, (rect, fish) =>
            {
                var clicked = FishCard(skin, rect, fish.SpeciesId, fish.SpeciesName,
                    Format.SizeCm(fish.SizeCm) + " · " + fish.SizeCategoryName, (float)fish.SizePercentile,
                    GameTexts.Player.LevelShort + " " + fish.Level + (fish.CardumePosition > 0 ? "  ·  " + GameTexts.Cardume.Badge(fish.CardumePosition) : string.Empty),
                    fish.RarityId, fish.RarityName, _multi ? _sellSet.Contains(fish.FishId) : fish.FishId == _selectedId,
                    fish.IsImportant, fish.SizeCategoryId, fish.SizeCategoryName);
                if (!clicked)
                {
                    return;
                }

                // Ctrl + click starts a selection (with the fish already open, if any).
                if (!_multi && Event.current != null && (Event.current.control || Event.current.command))
                {
                    StartMulti();
                }

                if (_multi)
                {
                    ToggleSell(fish.FishId);
                }
                else
                {
                    _selectedId = fish.FishId;
                    _selected = fish;
                }
            });
        }

        private void DrawSheet(UiSkin skin, Rect area)
        {
            GUI.Box(area, GUIContent.none, skin.Card);
            var fish = _selected;
            if (fish == null)
            {
                GUI.Label(new Rect(area.x + 20, area.y + area.height / 2f - 20, area.width - 40, 40), GameTexts.Aquarium.PickFish, skin.Center);
                return;
            }

            var x = area.x + 22;
            var w = area.width - 44;
            var y = area.y + 18;

            var accent = UiSkin.RarityColor(fish.RarityId);
            skin.DrawOutline(area, new Color(accent.r, accent.g, accent.b, 0.8f));
            var art = new Rect(x, y + 6, w, 124);
            var exceptional = VisualTheme.IsSpecialSize(fish.SizeCategoryId);
            if (fish.IsImportant)
            {
                skin.DrawGlow(new Rect(art.x + art.width * 0.25f, art.y + art.height * 0.3f, art.width * 0.5f, art.height * 0.4f),
                    exceptional ? UiSkin.SizeColor(fish.SizeCategoryId) : accent, 0.4f);
            }

            GUI.DrawTexture(art, Art.FishTexture(fish.SpeciesId), ScaleMode.ScaleToFit, true);
            y += 140;

            GUI.Label(new Rect(x, y, w, 30), fish.SpeciesName, skin.Heading);
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
            StatRow(skin, x, ref y, w, GameTexts.Aquarium.Hp, fish.Stats.Hp);
            StatRow(skin, x, ref y, w, GameTexts.Aquarium.Attack, fish.Stats.Attack);
            StatRow(skin, x, ref y, w, GameTexts.Aquarium.Defense, fish.Stats.Defense);
            StatRow(skin, x, ref y, w, GameTexts.Aquarium.Speed, fish.Stats.Speed);
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
            GUI.Label(new Rect(x, y, 140, 20), label, skin.SmallMuted);
            GUI.Label(new Rect(x + 140, y, w - 140, 20), Format.Number((long)Math.Round(value)), skin.SmallRight);
            y += 22;
        }

        private static void InfoRow(UiSkin skin, float x, ref float y, float w, string label, string value)
        {
            GUI.Label(new Rect(x, y, 170, 20), label, skin.SmallMuted);
            GUI.Label(new Rect(x + 170, y, w - 170, 20), value, skin.SmallRight);
            y += 22;
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
            GUI.Box(area, GUIContent.none, skin.Card);
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
                if (GUI.Button(new Rect(row.xMax - 26, row.y + 7, 22, 22), "×", skin.Chip))
                {
                    ToggleSell(f.FishId);
                }
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
            if (_multi)
            {
                var present = new HashSet<long>(_aquarium.Fish.Select(f => f.FishId));
                _sellSet.RemoveWhere(id => !present.Contains(id));
                _salePreview = null;
            }

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
