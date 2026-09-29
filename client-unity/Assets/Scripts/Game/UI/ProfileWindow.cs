using System;
using System.Collections.Generic;
using System.Linq;
using FishingIdle.Game.Bootstrap;
using FishingIdle.Game.Scene;
using FishingIdle.GameService.Aquarium;
using FishingIdle.GameService.Profile;
using FishingIdle.Game.Visual;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The player's own Profile (GDD sections 20, 23, 37, 38): Equipment, Inventory, Cardume,
    /// Encyclopedia and Highlights. The private Cardume Strength is shown only here.
    /// </summary>
    public sealed class ProfileWindow
    {
        private enum Tab
        {
            Equipment,
            Inventory,
            Cardume,
            Encyclopedia,
            Records,
        }

        private readonly GameRoot _root;
        private Tab _tab = Tab.Cardume;
        private ProfileView _profile;
        private CardumeView _cardume;
        private AquariumView _aquarium;
        private int _selectedPosition = 1;
        private Vector2 _scroll;
        private Vector2 _listScroll;
        private bool _dirty = true;

        // Rod sell/destroy confirmation.
        private RodItemView _pendingRod;
        private bool _pendingDestroy;

        public ProfileWindow(GameRoot root)
        {
            _root = root;
            _root.AquariumChanged += () => _dirty = true;
            _root.BoxChanged += () => _dirty = true;
        }

        public bool IsOpen { get; private set; }

        public void Open()
        {
            IsOpen = true;
            _dirty = true;
        }

        /// <summary>Closes the rod confirmation first, then the window.</summary>
        public void Close()
        {
            if (_pendingRod != null)
            {
                _pendingRod = null;
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

            if (_profile == null)
            {
                return;
            }

            GUI.enabled = _pendingRod == null;

            var panel = WindowFrame.Panel(skin, screenWidth, screenHeight, 1320f, 840f);

            // Header: portrait and identity on the left, private Strength on the right.
            var portrait = new Rect(panel.x + 28, panel.y + 18, 60, 60);
            GUI.Box(portrait, GUIContent.none, skin.IconTile);
            var face = ArtAssets.Texture("Cena/pescador");
            if (face != null)
            {
                GUI.DrawTextureWithTexCoords(new Rect(portrait.x + 4, portrait.y + 4, 52, 52), face, new Rect(0.08f, 0.56f, 0.8f, 0.46f));
            }
            else
            {
                skin.DrawIcon(new Rect(portrait.x + 14, portrait.y + 14, 32, 32), Icons.Profile, UiSkin.Accent);
            }

            GUI.Label(new Rect(portrait.xMax + 16, panel.y + 18, 500, 36), _profile.PlayerName, skin.Title);
            GUI.Label(new Rect(portrait.xMax + 17, panel.y + 56, 500, 22), GameTexts.Player.Level + " " + _profile.FisherLevel + " · " + _profile.MapName, skin.SmallMuted);
            GUI.Label(new Rect(panel.xMax - 560, panel.y + 22, 240, 22), GameTexts.Cardume.Strength, skin.SmallMutedRight);
            GUI.Label(new Rect(panel.xMax - 560, panel.y + 44, 240, 30), Format.Number(_profile.CardumeStrength), skin.NumberRight);
            if (skin.IconButton(new Rect(panel.xMax - 156, panel.y + 22, 128, 42), Icons.Close, GameTexts.Box.Close, skin.Button))
            {
                Close();
            }

            // Tabs
            var tabs = new[]
            {
                (Tab.Equipment, GameTexts.Profile.TabEquipment), (Tab.Inventory, GameTexts.Profile.TabInventory),
                (Tab.Cardume, GameTexts.Profile.TabCardume), (Tab.Encyclopedia, GameTexts.Profile.TabEncyclopedia),
                (Tab.Records, GameTexts.Profile.TabRecords),
            };
            var x = panel.x + 28;
            foreach (var (tab, label) in tabs)
            {
                var w = skin.Chip.CalcSize(new GUIContent(label)).x + 12;
                if (GUI.Button(new Rect(x, panel.y + 92, w, 34), label, _tab == tab ? skin.ChipActive : skin.Chip))
                {
                    _tab = tab;
                    _scroll = Vector2.zero;
                }

                x += w + 8;
            }

            var content = new Rect(panel.x + 28, panel.y + 146, panel.width - 56, panel.height - 170);
            switch (_tab)
            {
                case Tab.Equipment: DrawEquipment(skin, content); break;
                case Tab.Inventory: DrawInventory(skin, content); break;
                case Tab.Encyclopedia: DrawEncyclopedia(skin, content); break;
                case Tab.Records: DrawRecords(skin, content); break;
                default: DrawCardume(skin, content); break;
            }

            GUI.enabled = true;
            if (_pendingRod != null)
            {
                DrawRodDialog(skin, screenWidth, screenHeight);
            }
        }

        private void DrawRodDialog(UiSkin skin, float screenWidth, float screenHeight)
        {
            var rod = _pendingRod;
            var rect = WindowFrame.Dialog(skin, screenWidth, screenHeight, 220f);
            GUI.Label(new Rect(rect.x + 28, rect.y + 24, rect.width - 56, 30), _pendingDestroy ? GameTexts.Shop.DestroyTitle(rod.Name) : GameTexts.Shop.SellTitle(rod.Name), skin.Heading);
            GUI.Label(new Rect(rect.x + 28, rect.y + 64, rect.width - 56, 70), _pendingDestroy ? GameTexts.Shop.DestroyBody : GameTexts.Shop.SellBody(Format.Number(rod.ResaleValue)), skin.Body);

            if (GUI.Button(new Rect(rect.x + 28, rect.yMax - 64, 150, 42), GameTexts.Dialogs.Cancel, skin.Button))
            {
                _pendingRod = null;
            }

            if (GUI.Button(new Rect(rect.xMax - 218, rect.yMax - 64, 190, 42), _pendingDestroy ? GameTexts.Shop.DestroyRod : GameTexts.Shop.SellRod, _pendingDestroy ? skin.ButtonDanger : skin.ButtonPrimary))
            {
                if (_pendingDestroy)
                {
                    _root.DestroyRod(rod.ItemId);
                }
                else
                {
                    _root.SellRod(rod.ItemId);
                }

                _pendingRod = null;
            }
        }

        // ------------------------------------------------------------------ Equipment and Inventory

        private void DrawEquipment(UiSkin skin, Rect area)
        {
            var rod = _profile.EquippedRod;
            GUI.Label(new Rect(area.x, area.y, 400, 26), GameTexts.Profile.RodSlot, skin.Heading);
            if (rod != null)
            {
                RodCard(skin, new Rect(area.x, area.y + 40, 460, 250), rod, false);
            }
        }

        private void DrawInventory(UiSkin skin, Rect area)
        {
            GUI.Label(new Rect(area.x, area.y, area.width, 22), GameTexts.Profile.InventoryNote, skin.SmallMuted);
            var x = area.x;
            var y = area.y + 36;
            foreach (var rod in _profile.Inventory)
            {
                if (x + 400 > area.xMax)
                {
                    x = area.x;
                    y += 316;
                }

                RodCard(skin, new Rect(x, y, 400, 300), rod, true);
                x += 416;
            }
        }

        private void RodCard(UiSkin skin, Rect rect, RodItemView rod, bool withAction)
        {
            GUI.Box(rect, GUIContent.none, rod.IsEquipped ? skin.CardSelected : skin.Card);
            var x = rect.x + 20;
            var w = rect.width - 40;
            var y = rect.y + 16;

            GUI.Label(new Rect(x, y, w, 26), rod.Name, skin.Heading);
            y += 28;
            var level = rod.HasLevels ? " · " + GameTexts.Aquarium.LevelOf(rod.Level, rod.MaxLevel) : string.Empty;
            GUI.Label(new Rect(x, y, w, 20), GameTexts.Profile.Tier(rod.Tier) + level, skin.SmallMuted);
            y += 32;

            Row(skin, x, ref y, w, GameTexts.Profile.RarityBonus, "+" + Format.Percent(rod.RarityBonus, 0));
            Row(skin, x, ref y, w, GameTexts.Profile.SizeBonus, "+" + Format.Percent(rod.SizeBonus, 0));
            Row(skin, x, ref y, w, GameTexts.Profile.ShellBonus, rod.GeneratesShells ? "+" + Format.Percent(rod.ShellBonus, 0) : GameTexts.Profile.NoShells);
            GUI.Label(new Rect(x, y + 4, w, 20), rod.CanCatchRare ? GameTexts.Profile.CatchesRare : GameTexts.Profile.NoRare, skin.Small);

            if (!withAction)
            {
                return;
            }

            // Row 1: equip state. Row 2: upgrade / sell / destroy.
            var button = new Rect(x, rect.yMax - 100, w, 38);
            if (rod.IsEquipped)
            {
                var equipped = GameTexts.Profile.Equipped.ToUpperInvariant();
                skin.AccentPill(new Rect(x, button.y + 7, skin.PillWidth(equipped, true) + 6, 24), equipped, UiSkin.Accent, Icons.Check);
            }
            else if (!rod.AllowedOnCurrentMap)
            {
                GUI.Label(button, GameTexts.Profile.NotAllowedHere, skin.SmallGold);
            }
            else if (GUI.Button(button, GameTexts.Profile.Equip, skin.ButtonPrimary))
            {
                _root.EquipRod(rod.ItemId);
            }

            var actions = new Rect(x, rect.yMax - 54, w, 38);
            if (rod.HasLevels)
            {
                var upgradeWidth = rod.CanDispose ? w * 0.5f : w;
                if (rod.NextUpgradeCost > 0)
                {
                    if (GUI.Button(new Rect(actions.x, actions.y, upgradeWidth - 6, 38), GameTexts.Shop.UpgradeFor(rod.Level + 1, Format.Number(rod.NextUpgradeCost)), skin.Button))
                    {
                        _root.UpgradeRod(rod.ItemId);
                    }
                }
                else
                {
                    GUI.Label(new Rect(actions.x, actions.y + 8, upgradeWidth, 22), GameTexts.Shop.MaxLevel, skin.SmallGold);
                }

                actions.x += upgradeWidth;
                actions.width -= upgradeWidth;
            }

            if (rod.CanDispose)
            {
                var half = actions.width / 2f;
                if (GUI.Button(new Rect(actions.x, actions.y, half - 6, 38), GameTexts.Shop.SellFor(Format.Number(rod.ResaleValue)), skin.Button))
                {
                    _pendingRod = rod;
                    _pendingDestroy = false;
                }

                if (GUI.Button(new Rect(actions.x + half, actions.y, half, 38), GameTexts.Shop.DestroyRod, skin.Button))
                {
                    _pendingRod = rod;
                    _pendingDestroy = true;
                }
            }
        }

        // ------------------------------------------------------------------ Cardume

        private void DrawCardume(UiSkin skin, Rect area)
        {
            var listWidth = 380f;
            var formation = new Rect(area.x, area.y, area.width - listWidth - 24, area.height);
            var list = new Rect(area.xMax - listWidth, area.y, listWidth, area.height);

            // Two rows of three: front (1–3) and back (4–6), with a small depth offset.
            var slotW = Mathf.Min(220f, (formation.width - 40) / 3f);
            const float slotH = 176f;
            DrawRow(skin, _cardume.Slots.Where(s => s.IsFront).ToList(), GameTexts.Cardume.Front, formation.x, formation.y, slotW, slotH);
            DrawRow(skin, _cardume.Slots.Where(s => !s.IsFront).ToList(), GameTexts.Cardume.Back, formation.x + 24, formation.y + slotH + 44, slotW, slotH);

            var y = formation.y + 2 * (slotH + 44) + 8;
            GUI.Label(new Rect(formation.x, y, formation.width, 22), GameTexts.Cardume.Filled(_cardume.Filled, _cardume.Size), skin.BodyBold);
            y += 26;
            var percent = Format.Percent(_cardume.CompleteBonusPercent / 100.0, 0);
            GUI.Label(new Rect(formation.x, y, formation.width, 22), _cardume.CompleteBonusActive
                ? GameTexts.Cardume.BonusActive(percent)
                : GameTexts.Cardume.BonusMissing(_cardume.CompleteBonusSlots - _cardume.Filled, percent), _cardume.CompleteBonusActive ? skin.SmallGold : skin.Small);
            y += 26;
            GUI.Label(new Rect(formation.x, y, formation.width, 40), GameTexts.Cardume.OrderNote, skin.SmallMuted);
            y += 40;
            GUI.Label(new Rect(formation.x, y, formation.width, 40), GameTexts.Cardume.StrengthPrivate, skin.SmallMuted);

            var selected = _cardume.Slots.FirstOrDefault(s => s.Position == _selectedPosition);
            if (selected?.Fish != null && skin.IconButton(new Rect(formation.xMax - 220, formation.y + 2 * (slotH + 44) + 4, 220, 36), Icons.Close, GameTexts.Cardume.Remove, skin.Button))
            {
                _root.ClearCardumeSlot(_selectedPosition);
            }

            DrawFishList(skin, list);
        }

        private void DrawRow(UiSkin skin, List<CardumeSlotView> slots, string label, float x, float y, float slotW, float slotH)
        {
            GUI.Label(new Rect(x, y, 200, 20), label, skin.SmallMuted);
            y += 24;
            foreach (var slot in slots)
            {
                var rect = new Rect(x, y, slotW - 12, slotH - 24);
                var selected = slot.Position == _selectedPosition;
                if (GUI.Button(rect, GUIContent.none, selected ? skin.CardSelected : slot.Fish != null ? skin.Card : skin.CardHovered))
                {
                    _selectedPosition = slot.Position;
                }

                GUI.Label(new Rect(rect.x + 12, rect.y + 8, 120, 20), GameTexts.Cardume.Position(slot.Position), skin.SmallGold);
                if (slot.Fish == null)
                {
                    // An inviting empty slot: a "+" and the word, never a blank box.
                    var plus = new Rect(rect.center.x - 18, rect.y + rect.height / 2f - 30, 36, 36);
                    GUI.DrawTexture(plus, skin.White, ScaleMode.StretchToFill, true, 0, new Color(UiSkin.Accent.r, UiSkin.Accent.g, UiSkin.Accent.b, 0.18f), 0, 18);
                    skin.DrawIcon(new Rect(plus.x + 9, plus.y + 9, 18, 18), Icons.Add, UiSkin.Accent);
                    GUI.Label(new Rect(rect.x, plus.yMax + 6, rect.width, 22), GameTexts.Cardume.Empty, skin.Center);
                }
                else
                {
                    if (!selected)
                    {
                        var accent = UiSkin.RarityColor(slot.Fish.RarityId);
                        skin.DrawOutline(rect, new Color(accent.r, accent.g, accent.b, 0.7f));
                    }

                    GUI.DrawTexture(new Rect(rect.x + 12, rect.y + 30, rect.width - 24, 58), Art.FishTexture(slot.Fish.SpeciesId), ScaleMode.ScaleToFit, true);
                    GUI.Label(new Rect(rect.x + 12, rect.y + 92, rect.width - 24, 20), slot.Fish.SpeciesName + " · " + GameTexts.Player.LevelShort + " " + slot.Fish.Level, skin.BodyBold);
                    GUI.Label(new Rect(rect.x + 12, rect.y + 114, rect.width - 24, 20), GameTexts.Cardume.Strength + ": " + Format.Number(slot.Strength), skin.Small);
                }

                x += slotW;
            }
        }

        private void DrawFishList(UiSkin skin, Rect area)
        {
            GUI.Box(area, GUIContent.none, skin.Card);
            GUI.Label(new Rect(area.x + 16, area.y + 12, area.width - 32, 22), GameTexts.Cardume.AquariumList, skin.BodyBold);
            GUI.Label(new Rect(area.x + 16, area.y + 36, area.width - 32, 40), GameTexts.Cardume.PickSlot, skin.SmallMuted);

            var fish = _aquarium?.Fish ?? new List<FishView>();
            if (fish.Count == 0)
            {
                GUI.Label(new Rect(area.x + 16, area.y + 100, area.width - 32, 60), GameTexts.Cardume.NoFish, skin.Small);
                return;
            }

            const float rowH = 64f;
            var view = new Rect(area.x + 8, area.y + 84, area.width - 16, area.height - 92);
            _listScroll = GUI.BeginScrollView(view, _listScroll, new Rect(0, 0, view.width - 20, fish.Count * rowH));
            for (var i = 0; i < fish.Count; i++)
            {
                var f = fish[i];
                var row = new Rect(0, i * rowH, view.width - 20, rowH - 6);
                if (GUI.Button(row, GUIContent.none, f.CardumePosition == _selectedPosition ? skin.CardSelected : skin.CardHovered))
                {
                    _root.SetCardumeSlot(_selectedPosition, f.FishId);
                }

                GUI.DrawTexture(new Rect(row.x + 8, row.y + 6, 80, 46), Art.FishTexture(f.SpeciesId), ScaleMode.ScaleToFit, true);
                GUI.Label(new Rect(row.x + 96, row.y + 8, row.width - 150, 20), f.SpeciesName + " · " + GameTexts.Player.LevelShort + " " + f.Level, skin.BodyBold);
                GUI.Label(new Rect(row.x + 96, row.y + 30, row.width - 150, 20), Format.SizeCm(f.SizeCm) + " · " + f.SizeCategoryName, skin.Small);
                if (f.CardumePosition > 0)
                {
                    skin.AccentPill(new Rect(row.xMax - 50, row.y + 17, 40, 22), GameTexts.Cardume.Badge(f.CardumePosition), UiSkin.Accent);
                }
            }

            GUI.EndScrollView();
        }

        // ------------------------------------------------------------------ Encyclopedia and Records

        private void DrawEncyclopedia(UiSkin skin, Rect area)
        {
            var entries = _profile.Encyclopedia;
            GUI.Label(new Rect(area.x, area.y, area.width, 22), GameTexts.Profile.Discovery(entries.Count(e => e.Discovered), entries.Count), skin.BodyBold);

            const float cardW = 236f, cardH = 176f, gap = 12f;
            var view = new Rect(area.x - 4, area.y + 34, area.width + 8, area.height - 34);
            var columns = Mathf.Max(1, Mathf.FloorToInt((view.width - 20 + gap) / (cardW + gap)));
            var rows = Mathf.CeilToInt(entries.Count / (float)columns);
            _scroll = GUI.BeginScrollView(view, _scroll, new Rect(0, 0, view.width - 20, rows * (cardH + gap)));
            for (var i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                var rect = new Rect((i % columns) * (cardW + gap), (i / columns) * (cardH + gap), cardW, cardH);
                GUI.Box(rect, GUIContent.none, skin.Card);
                if (e.Discovered)
                {
                    var accent = UiSkin.RarityColor(e.RarityId);
                    skin.DrawOutline(rect, new Color(accent.r, accent.g, accent.b, e.RarityId == "common" ? 0.35f : 0.85f));
                }

                // Undiscovered species are a dark silhouette with no name (GDD section 38).
                GUI.DrawTexture(new Rect(rect.x + 16, rect.y + 12, rect.width - 32, 70), Art.FishTexture(e.SpeciesId), ScaleMode.ScaleToFit, true, 0,
                    e.Discovered ? Color.white : new Color(0.02f, 0.05f, 0.09f, 0.85f), 0, 0);

                if (!e.Discovered)
                {
                    GUI.Label(new Rect(rect.x + 14, rect.y + 90, rect.width - 28, 22), GameTexts.Profile.Undiscovered, skin.BodyBold);
                    continue;
                }

                GUI.Label(new Rect(rect.x + 14, rect.y + 88, rect.width - 28, 22), e.Name, skin.BodyBold);
                var rarity = (e.RarityName ?? string.Empty).ToUpperInvariant();
                var pw = skin.PillWidth(rarity, false);
                skin.AccentPill(new Rect(rect.xMax - 14 - pw, rect.y + 90, pw, 20), rarity, UiSkin.RarityColor(e.RarityId));
                GUI.Label(new Rect(rect.x + 14, rect.y + 110, rect.width - 28, 20), e.MapName, skin.SmallMuted);
                GUI.Label(new Rect(rect.x + 14, rect.y + 130, rect.width - 28, 20), GameTexts.Profile.Largest + ": " + Format.SizeCm(e.LargestCm), skin.Small);
                GUI.Label(new Rect(rect.x + 14, rect.y + 150, rect.width - 28, 20), GameTexts.Profile.TimesCaught + ": " + Format.Number(e.TimesCaught), skin.Small);
            }

            GUI.EndScrollView();
        }

        private void DrawRecords(UiSkin skin, Rect area)
        {
            var r = _profile.Records;
            var x = area.x;
            var w = Mathf.Min(620f, area.width);
            var y = area.y;
            Row(skin, x, ref y, w, GameTexts.Profile.TotalCatches, Format.Number(r.TotalCatches));
            Row(skin, x, ref y, w, GameTexts.Profile.Discovered, GameTexts.Profile.Discovery(r.SpeciesDiscovered, r.SpeciesTotal));
            Row(skin, x, ref y, w, GameTexts.Profile.Biggest, r.BiggestSpeciesName == null ? GameTexts.Profile.None : r.BiggestSpeciesName + " · " + Format.SizeCm(r.BiggestCm));
            Row(skin, x, ref y, w, GameTexts.Profile.HighestLevel, r.HighestFishLevel == 0 ? GameTexts.Profile.None : GameTexts.Player.LevelShort + " " + r.HighestFishLevel);
            Row(skin, x, ref y, w, GameTexts.Profile.Exceptional, Format.Number(r.ExceptionalCatches));
            Row(skin, x, ref y, w, GameTexts.Profile.Rare, Format.Number(r.RareCatches));
            Row(skin, x, ref y, w, GameTexts.Profile.Sold, Format.Number(r.FishSold));
            Row(skin, x, ref y, w, GameTexts.Profile.CoinsFromSales, Format.Number(r.CoinsFromSales));
        }

        private static void Row(UiSkin skin, float x, ref float y, float w, string label, string value)
        {
            GUI.Label(new Rect(x, y, w * 0.6f, 24), label, skin.SmallMuted);
            GUI.Label(new Rect(x + w * 0.4f, y, w * 0.6f, 24), value, skin.SmallRight);
            y += 30;
        }

        private void Reload()
        {
            _dirty = false;
            _profile = _root.GetProfile();
            _cardume = _root.GetCardume();
            _aquarium = _root.GetAquarium(AquariumSort.Size);
        }
    }
}
