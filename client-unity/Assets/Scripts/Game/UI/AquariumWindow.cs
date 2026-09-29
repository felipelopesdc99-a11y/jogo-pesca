using System;
using System.Collections.Generic;
using System.Linq;
using FishingIdle.Game.Bootstrap;
using FishingIdle.Game.Scene;
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
        private const float CardHeight = 172f;
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
        private readonly HashSet<long> _foodBox = new HashSet<long>();
        private readonly HashSet<long> _foodFish = new HashSet<long>();
        private IReadOnlyList<CatchView> _box = new List<CatchView>();
        private ServiceResult<FeedPreview> _feedPreview;
        private Vector2 _feedScroll;

        // Confirmations
        private bool _confirmSell;
        private bool _confirmFeed;

        public AquariumWindow(GameRoot root)
        {
            _root = root;
            _root.AquariumChanged += () => _dirty = true;
            _root.BoxChanged += () => _dirty = true;
        }

        public bool IsOpen { get; private set; }

        private bool DialogOpen => _confirmSell || _confirmFeed;

        public void Open()
        {
            IsOpen = true;
            _dirty = true;
        }

        /// <summary>Closes the innermost thing first: a dialog, then feeding, then the window.</summary>
        public void Close()
        {
            if (DialogOpen)
            {
                _confirmSell = false;
                _confirmFeed = false;
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

            GUI.DrawTexture(new Rect(0, 0, screenWidth, screenHeight), skin.Overlay);
            var width = Mathf.Min(1320f, screenWidth - 80f);
            var height = Mathf.Min(840f, screenHeight - 120f);
            var panel = new Rect((screenWidth - width) / 2f, (screenHeight - height) / 2f + 20f, width, height);

            GUI.enabled = !DialogOpen;
            GUI.Box(panel, GUIContent.none, skin.Panel);

            var detailWidth = 420f;
            var left = new Rect(panel.x + 24, panel.y + 20, panel.width - detailWidth - 60, panel.height - 40);
            var right = new Rect(panel.xMax - detailWidth - 20, panel.y + 20, detailWidth, panel.height - 40);

            if (_feeding)
            {
                DrawFoodPicker(skin, left);
                DrawFeedPanel(skin, right);
            }
            else
            {
                DrawBrowser(skin, left, panel);
                DrawSheet(skin, right);
            }

            GUI.enabled = true;
            if (_confirmSell)
            {
                DrawSellDialog(skin, screenWidth, screenHeight);
            }
            else if (_confirmFeed)
            {
                DrawFeedDialog(skin, screenWidth, screenHeight);
            }
        }

        // ------------------------------------------------------------------ browsing

        private void DrawBrowser(UiSkin skin, Rect area, Rect panel)
        {
            GUI.Label(new Rect(area.x, area.y, 300, 32), GameTexts.Aquarium.Title, skin.Title);
            GUI.Label(new Rect(area.x + 2, area.y + 36, 300, 22), GameTexts.Aquarium.Count(_aquarium.Count, _aquarium.Capacity), skin.SmallMuted);

            // Sort chips
            var x = area.x;
            GUI.Label(new Rect(x, area.y + 76, 80, 24), GameTexts.Aquarium.SortLabel, skin.SmallMuted);
            x += 78;
            foreach (var sort in Sorts)
            {
                var label = new GUIContent(SortName(sort));
                var w = skin.Chip.CalcSize(label).x + 8;
                if (GUI.Button(new Rect(x, area.y + 70, w, 32), label, sort == _sort ? skin.ChipActive : skin.Chip))
                {
                    _sort = sort;
                    _dirty = true;
                }

                x += w + 8;
            }

            if (GUI.Button(new Rect(panel.xMax - 148, panel.y + 20, 120, 38), GameTexts.Box.Close, skin.Button))
            {
                Close();
            }

            var grid = new Rect(area.x - 4, area.y + 118, area.width + 8, area.height - 118);
            if (_aquarium.Count == 0)
            {
                GUI.Label(new Rect(grid.x, grid.y + grid.height / 2f - 30, grid.width, 60), GameTexts.Aquarium.Empty, skin.Center);
                return;
            }

            DrawGrid(grid, _aquarium.Fish, ref _scroll, (rect, fish) =>
            {
                var clicked = FishCard(skin, rect, fish.SpeciesId, fish.SpeciesName,
                    Format.SizeCm(fish.SizeCm) + " · " + fish.SizeCategoryName, (float)fish.SizePercentile,
                    GameTexts.Player.LevelShort + " " + fish.Level + (fish.CardumePosition > 0 ? "  ·  " + GameTexts.Cardume.Badge(fish.CardumePosition) : string.Empty),
                    fish.RarityId, fish.RarityName, fish.FishId == _selectedId, fish.IsImportant, fish.SizeCategoryId == "exceptional");
                if (clicked)
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

            var art = new Rect(x, y, w, 130);
            if (fish.IsImportant)
            {
                var previous = GUI.color;
                GUI.color = new Color(1f, 0.85f, 0.4f, 0.35f);
                GUI.DrawTexture(new Rect(art.x + 30, art.y - 6, art.width - 60, art.height + 12), Art.Glow.texture, ScaleMode.StretchToFill, true);
                GUI.color = previous;
            }

            GUI.DrawTexture(art, Art.FishTexture(fish.SpeciesId), ScaleMode.ScaleToFit, true);
            y += 140;

            GUI.Label(new Rect(x, y, w, 30), fish.SpeciesName, skin.Heading);
            y += 30;
            GUI.Label(new Rect(x, y, w, 22), fish.RarityName + " · " + fish.SizeCategoryName, skin.SmallMuted);
            y += 30;

            // Size: number and a bar inside the species range (GDD section 14).
            GUI.Label(new Rect(x, y, w, 22), GameTexts.Aquarium.Size + ": " + Format.SizeCm(fish.SizeCm) + "  (" + Format.SizeCm(fish.SpeciesMinCm) + " – " + Format.SizeCm(fish.SpeciesMaxCm) + ")", skin.Small);
            y += 22;
            skin.Bar(new Rect(x, y, w, 8), (float)fish.SizePercentile, fish.SizeCategoryId == "exceptional");
            y += 20;

            GUI.Label(new Rect(x, y, w, 22), GameTexts.Aquarium.LevelOf(fish.Level, fish.MaxLevel), skin.BodyBold);
            y += 24;
            if (fish.XpToNext > 0)
            {
                skin.Bar(new Rect(x, y, w, 8), fish.Xp / (float)fish.XpToNext);
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
            if (GUI.Button(new Rect(x, buttons, w / 2f - 6, 42), GameTexts.Aquarium.Feed, skin.ButtonPrimary))
            {
                StartFeeding();
            }

            GUI.enabled = enabled;
            if (GUI.Button(new Rect(x + w / 2f + 6, buttons, w / 2f - 6, 42), GameTexts.Aquarium.Sell, skin.Button))
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
                DrawGrid(grid, others, ref _feedScroll, (rect, fish) =>
                {
                    if (FishCard(skin, rect, fish.SpeciesId, fish.SpeciesName,
                        GameTexts.Player.LevelShort + " " + fish.Level + " · " + Format.SizeCm(fish.SizeCm), (float)fish.SizePercentile,
                        GameTexts.Aquarium.FeedXp(Format.Number(fish.FeedXp)), fish.RarityId, fish.RarityName,
                        _foodFish.Contains(fish.FishId), fish.IsValuableFood, fish.SizeCategoryId == "exceptional"))
                    {
                        Toggle(_foodFish, fish.FishId);
                    }
                });
            }
            else
            {
                DrawGrid(grid, _box, ref _feedScroll, (rect, c) =>
                {
                    if (FishCard(skin, rect, c.SpeciesId, c.SpeciesName,
                        Format.SizeCm(c.SizeCm) + " · " + c.SizeCategoryName, (float)c.SizePercentile,
                        GameTexts.Aquarium.FeedXp(Format.Number(c.FeedXp)), c.RarityId, c.RarityName,
                        _foodBox.Contains(c.CatchId), c.IsValuableFood, c.SizeCategoryId == "exceptional"))
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
            GUI.Box(rect, GUIContent.none, skin.CardImportant);
            return rect;
        }

        // ------------------------------------------------------------------ helpers

        private void Reload()
        {
            _dirty = false;
            _aquarium = _root.GetAquarium(_sort) ?? new AquariumView();
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

        /// <summary>One fish card. Returns true when clicked.</summary>
        private static bool FishCard(UiSkin skin, Rect rect, string speciesId, string name, string line, float sizeFraction,
            string corner, string rarityId, string rarityName, bool selected, bool important, bool exceptional)
        {
            var hovered = rect.Contains(Event.current.mousePosition);
            var style = selected ? skin.CardSelected : important ? skin.CardImportant : hovered ? skin.CardHovered : skin.Card;
            var clicked = GUI.Button(rect, GUIContent.none, style);

            GUI.DrawTexture(new Rect(rect.x + 14, rect.y + 12, rect.width - 28, 72), Art.FishTexture(speciesId), ScaleMode.ScaleToFit, true);
            GUI.Label(new Rect(rect.x + 12, rect.y + 90, rect.width - 24, 22), name, skin.BodyBold);
            GUI.Label(new Rect(rect.x + 12, rect.y + 112, rect.width - 24, 20), line, skin.Small);
            skin.Bar(new Rect(rect.x + 12, rect.y + 136, rect.width - 24, 7), sizeFraction, exceptional);
            GUI.Label(new Rect(rect.x + 12, rect.y + 148, rect.width - 24, 20), corner, skin.SmallGold);

            if (rarityId != null && rarityId != "common")
            {
                skin.Tag(new Rect(rect.xMax - 66, rect.y + 148, 56, 18), rarityName.ToUpperInvariant(), UiSkin.Rare);
            }

            return clicked;
        }
    }
}
