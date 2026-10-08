using System;
using System.Collections.Generic;
using System.Linq;
using FishingIdle.Game.Bootstrap;
using FishingIdle.Game.Scene;
using FishingIdle.GameService.Aquarium;
using FishingIdle.GameService.Arena;
using FishingIdle.GameService.Expeditions;
using FishingIdle.GameService.Profile;
using FishingIdle.GameService.Shop;
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
            Summary,
            Equipment,
            Inventory,
            Cardume,
            Encyclopedia,
            Records,
        }

        private readonly GameRoot _root;
        private Tab _tab = Tab.Summary;
        private ProfileView _profile;
        private CardumeView _cardume;
        private AquariumView _aquarium;
        private ArenaView _arena;
        private string _encMap;

        // The name and avatar editor (M18-T03).
        private bool _editing;
        private string _nameDraft = string.Empty;
        private string _avatarDraft;
        private ExpeditionsView _expeditions;
        private float _nextRefresh;
        private int _selectedPosition = 1;
        private Vector2 _scroll;
        private Vector2 _listScroll;
        private bool _dirty = true;

        // Rod sell/destroy confirmation.
        private RodItemView _pendingRod;
        private bool _pendingDestroy;

        // The backpack inventory: the rod whose actions are shown, and the tooltip of the slot under the mouse.
        private long _selectedRodId = -1;
        private string _tipTitle;
        private Color _tipColor;
        private readonly List<(TipKind Kind, string Text, string Value)> _tipLines = new List<(TipKind Kind, string Text, string Value)>();

        public ProfileWindow(GameRoot root)
        {
            _root = root;
            _root.AquariumChanged += () => _dirty = true;
            _root.BoxChanged += () => _dirty = true;
        }

        public bool IsOpen { get; private set; }

        /// <summary>True while an internal dialog (rod sell/destroy, name and avatar editor) is open.</summary>
        public bool HasDialog => _pendingRod != null || _editing;

        public void Open()
        {
            IsOpen = true;
            _dirty = true;
            _tab = Tab.Summary;
        }

        /// <summary>Closes the rod confirmation first, then the window.</summary>
        public void Close()
        {
            if (_editing)
            {
                _editing = false;
                return;
            }

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
            else if (Time.unscaledTime >= _nextRefresh)
            {
                // The Summary shows live numbers (Energy, the Expedition countdown): refresh them once a second.
                _arena = _root.GetArena();
                _expeditions = _root.GetExpeditions();
                _nextRefresh = Time.unscaledTime + 1f;
            }

            if (_profile == null)
            {
                return;
            }

            GUI.enabled = _pendingRod == null && !_editing;

            var panel = WindowFrame.Panel(skin, screenWidth, screenHeight, 1320f, 840f);

            // Header (A-121): portrait, level with its XP bar, map and VIP on the left; the wallet on the right.
            DrawHeader(skin, panel);
            if (skin.IconButton(new Rect(panel.xMax - 156, panel.y + 22, 128, 42), Icons.Close, GameTexts.Box.Close, skin.Button))
            {
                Close();
            }

            // Tabs
            var tabs = new[]
            {
                (Tab.Summary, GameTexts.Profile.TabSummary),
                (Tab.Inventory, GameTexts.Profile.TabInventory),
                (Tab.Cardume, GameTexts.Profile.TabCardume), (Tab.Encyclopedia, GameTexts.Profile.TabEncyclopedia),
                (Tab.Records, GameTexts.Profile.TabRecords),
            };
            var x = panel.x + 28;
            foreach (var (tab, label) in tabs)
            {
                var w = skin.Chip.CalcSize(new GUIContent(label)).x + 12;
                if (GUI.Button(new Rect(x, panel.y + 116, w, 34), label, _tab == tab ? skin.ChipActive : skin.Chip))
                {
                    _tab = tab;
                    _scroll = Vector2.zero;
                }

                x += w + 8;
            }

            var content = new Rect(panel.x + 28, panel.y + 166, panel.width - 56, panel.height - 190);
            switch (_tab)
            {
                case Tab.Summary: DrawSummary(skin, content); break;
                case Tab.Equipment:
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
            else if (_editing)
            {
                DrawIdentityDialog(skin, screenWidth, screenHeight);
            }
        }

        private void DrawIdentityDialog(UiSkin skin, float screenWidth, float screenHeight)
        {
            var avatars = _profile.Avatars;
            var rect = WindowFrame.Dialog(skin, screenWidth, screenHeight, 470f, 700f);
            var x = rect.x + 28;
            var w = rect.width - 56;
            GUI.Label(new Rect(x, rect.y + 22, w, 30), GameTexts.Profile.EditTitle, skin.Heading);

            GUI.Label(new Rect(x, rect.y + 62, w, 20), GameTexts.Profile.NameLabel, skin.SmallMuted);
            var identity = _root.Game?.Session.Config.Progression.PlayerIdentity;
            var nameMin = identity?.NameMin ?? 3;
            var nameMax = identity?.NameMax ?? 16;
            _nameDraft = GUI.TextField(new Rect(x, rect.y + 84, w, 38), _nameDraft ?? string.Empty, nameMax, NameField(skin));
            GUI.Label(new Rect(x, rect.y + 126, w, 20), GameTexts.Profile.NameRuleFor(nameMin, nameMax), skin.SmallMuted);

            GUI.Label(new Rect(x, rect.y + 156, w, 20), GameTexts.Profile.AvatarLabel, skin.SmallMuted);
            var size = Mathf.Min(96f, (w - 5 * 12f) / 6f);
            for (var i = 0; i < avatars.Count; i++)
            {
                var a = avatars[i];
                var r = new Rect(x + i * (size + 12f), rect.y + 180, size, size);
                AvatarArt.Draw(skin, r, a.Id, 14);
                var picked = _avatarDraft == a.Id || (_avatarDraft == null && i == 0);
                GUI.DrawTexture(r, skin.White, ScaleMode.StretchToFill, true, 0, picked ? UiSkin.Accent : UiSkin.Border, picked ? 3f : 1f, 14);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none))
                {
                    _avatarDraft = a.Id;
                }

                GUI.Label(new Rect(r.x - 6, r.yMax + 4, size + 12, 34), a.Name, skin.SmallMutedCenter);
            }

            if (GUI.Button(new Rect(x, rect.yMax - 64, 150, 42), GameTexts.Dialogs.Cancel, skin.Button))
            {
                _editing = false;
            }

            if (skin.IconButton(new Rect(rect.xMax - 218, rect.yMax - 64, 190, 42), Icons.Check, GameTexts.Profile.Save, skin.ButtonPrimary))
            {
                if (_root.SaveIdentity(_nameDraft, _avatarDraft))
                {
                    _editing = false;
                    _dirty = true;
                }
            }
        }

        private static GUIStyle _nameField;
        private static GUIStyle _nameFieldBase;

        /// <summary>The search field look without the room for a magnifier: this field has no icon.</summary>
        private static GUIStyle NameField(UiSkin skin)
        {
            if (_nameField == null || _nameFieldBase != skin.SearchField)
            {
                _nameFieldBase = skin.SearchField;
                _nameField = new GUIStyle(skin.SearchField);
                _nameField.padding = new RectOffset(12, skin.SearchField.padding.right, skin.SearchField.padding.top, skin.SearchField.padding.bottom);
            }

            return _nameField;
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

        // ------------------------------------------------------------------ header and Summary (A-121)

        private void DrawHeader(UiSkin skin, Rect panel)
        {
            var player = _root.Player;
            var portrait = new Rect(panel.x + 28, panel.y + 20, 84, 84);
            GUI.DrawTexture(portrait, skin.White, ScaleMode.StretchToFill, true, 0, new Color(0.06f, 0.11f, 0.2f, 1f), 0, 18);
            AvatarArt.Draw(skin, new Rect(portrait.x + 3, portrait.y + 3, 78, 78), _profile.AvatarId, 15);

            GUI.DrawTexture(portrait, skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.Accent, 2f, 18);

            // The wallet, right to left from Fechar; whatever is left goes to the name and the level.
            var wallet = new[]
            {
                (Icons.Coin, GameTexts.Player.Coins, player != null ? player.Coins : 0L),
                (Icons.Shell, GameTexts.Player.Shells, player != null ? player.Shells : 0L),
                (Icons.Dollar, GameTexts.Player.Dollars, player != null ? player.Dollars : 0L),
                (Icons.Honor, GameTexts.Arena.Honor, _arena != null ? _arena.Honor : 0L),
            };
            var wx = panel.xMax - 156 - 14;
            for (var i = wallet.Length - 1; i >= 0; i--)
            {
                var (icon, label, amount) = wallet[i];
                var value = Format.Short(amount);
                var tw = Mathf.Max(skin.BodyBold.CalcSize(new GUIContent(value)).x, skin.SmallMuted.CalcSize(new GUIContent(label)).x) + 52f;
                var tile = new Rect(wx - tw, panel.y + 22, tw, 52);
                GUI.DrawTexture(tile, skin.White, ScaleMode.StretchToFill, true, 0, new Color(0.06f, 0.11f, 0.2f, 0.9f), 0, 12);
                GUI.DrawTexture(tile, skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.Border, 1f, 12);
                skin.DrawIcon(new Rect(tile.x + 10, tile.y + 13, 26, 26), icon, Color.white);
                GUI.Label(new Rect(tile.x + 42, tile.y + 6, tw - 46, 18), label, skin.SmallMuted);
                GUI.Label(new Rect(tile.x + 42, tile.y + 22, tw - 46, 24), value, skin.BodyBold);
                Hud.ExactOnHover(skin, tile, amount);
                wx = tile.x - 8;
            }

            var tx = portrait.xMax + 16;
            var tw2 = Mathf.Max(120f, wx - 12 - tx);
            var nameText = FishCard.Fit(_profile.PlayerName, skin.Title, tw2 - 96);
            GUI.Label(new Rect(tx, panel.y + 18, tw2 - 90, 36), nameText, skin.Title);

            // Name and avatar (M18-T03): a small "Editar" next to the name, or a click on the portrait.
            var nw = Mathf.Min(tw2 - 96, skin.Title.CalcSize(new GUIContent(nameText)).x);
            if (GUI.Button(new Rect(tx + nw + 10, panel.y + 24, 80, 26), GameTexts.Profile.Edit, skin.Chip)
                || GUI.Button(portrait, GUIContent.none, GUIStyle.none))
            {
                _editing = true;
                _nameDraft = _profile.PlayerName;

                // With no avatar chosen yet, the editor shows the first one as picked: draft it, so what is shown is what is saved.
                _avatarDraft = _profile.AvatarId ?? (_profile.Avatars != null && _profile.Avatars.Count > 0 ? _profile.Avatars[0].Id : null);
            }

            // Level badge and XP bar.
            var level = GameTexts.Player.LevelShort + " " + _profile.FisherLevel;
            var lw = skin.SmallBold.CalcSize(new GUIContent(level)).x + 16;
            var badge = new Rect(tx, panel.y + 58, lw, 22);
            GUI.DrawTexture(badge, skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.Accent, 0, 8);
            var prev = GUI.contentColor;
            GUI.contentColor = new Color(0.03f, 0.12f, 0.16f);
            GUI.Label(new Rect(badge.x + 8, badge.y + 2, lw, 18), level, skin.SmallBold);
            GUI.contentColor = prev;
            if (player != null && player.FisherXpToNext > 0)
            {
                skin.Bar(new Rect(badge.xMax + 10, badge.y + 7, Mathf.Clamp(tw2 - lw - 10, 60f, 300f), 8), player.FisherXp / (float)player.FisherXpToNext);
            }

            // XP numbers, map and VIP on the third line.
            var line = player != null && player.FisherXpToNext > 0
                ? GameTexts.Player.Xp(Format.Number(player.FisherXp), Format.Number(player.FisherXpToNext)) + " · " + _profile.MapName
                : _profile.MapName;
            var vip = _root.Vip;
            var vipText = vip != null && vip.Active ? GameTexts.Profile.VipUntil(Format.Date(System.DateTimeOffset.FromUnixTimeMilliseconds(vip.UntilMs).LocalDateTime)) : null;
            var vipWidth = vipText != null ? skin.PillWidth(vipText, true) : 0f;
            var lineWidth = tw2 - (vipWidth > 0 ? vipWidth + 10 : 0);
            GUI.Label(new Rect(tx, panel.y + 84, lineWidth, 20), FishCard.Fit(line, skin.SmallMuted, lineWidth), skin.SmallMuted);
            if (vipText != null)
            {
                var used = Mathf.Min(lineWidth, skin.SmallMuted.CalcSize(new GUIContent(line)).x);
                skin.AccentPill(new Rect(tx + used + 10, panel.y + 83, vipWidth, 20), vipText, UiSkin.Gold, Icons.Star);
            }
        }

        /// <summary>The player's own hub: Arena, Cardume, gear, space and collection in one screen.</summary>
        private void DrawSummary(UiSkin skin, Rect area)
        {
            var gap = 14f;
            var colW = (area.width - gap * 2) / 3f;
            var left = new Rect(area.x, area.y, colW, area.height);
            var mid = new Rect(area.x + colW + gap, area.y, colW, area.height);
            var right = new Rect(area.x + 2 * (colW + gap), area.y, colW, area.height);
            foreach (var r in new[] { left, mid, right })
            {
                GUI.Box(r, GUIContent.none, skin.Card);
            }

            // Left: Arena and Cardume.
            var x = left.x + 18;
            var w = left.width - 36;
            var y = left.y + 14;
            y = SectionTitle(skin, x, y, w, Icons.Arena, GameTexts.Arena.Title, null);
            if (_arena != null)
            {
                var rank = GameTexts.Arena.RankOf(_arena.Rank);
                var rw = skin.Display.CalcSize(new GUIContent(rank)).x;
                GUI.contentColor = UiSkin.Gold;
                GUI.Label(new Rect(x, y - 4, rw + 4, 44), rank, skin.Display);
                GUI.contentColor = Color.white;
                GUI.Label(new Rect(x + rw + 12, y + 14, w - rw - 12, 20), FishCard.Fit(GameTexts.Profile.OfPlayers(_arena.Participants), skin.SmallMuted, w - rw - 12), skin.SmallMuted);
                y += 46;
                y = InfoRow(skin, x, y, w, GameTexts.Arena.Honor, Format.Number(_arena.Honor), null);
                y = InfoRow(skin, x, y, w, GameTexts.Arena.Energy, GameTexts.Arena.EnergyOf(_arena.Energy, _arena.EnergyMax), null);
                var last = _arena.History.Count > 0 ? _arena.History[0] : null;
                y = InfoRow(skin, x, y, w, GameTexts.Profile.LastBattle, last == null ? GameTexts.Profile.None : GameTexts.Profile.BattleLine(last.PlayerWon, last.RankBefore - last.RankAfter),
                    last == null ? (Color?)null : last.PlayerWon ? UiSkin.Success : UiSkin.Danger);
            }

            y += 10;
            y = SectionTitle(skin, x, y, w, Icons.Fish, GameTexts.Profile.TabCardume, _cardume != null ? GameTexts.Profile.StrengthShort(Format.Number(_cardume.Strength)) : null);
            if (_cardume != null)
            {
                CardumeSlotView hovered = null;
                var hoveredRect = default(Rect);
                var n = Mathf.Max(1, _cardume.Slots.Count);
                var sw = (w - (n - 1) * 6f) / n;
                for (var i = 0; i < _cardume.Slots.Count; i++)
                {
                    var slot = _cardume.Slots[i];
                    var r = new Rect(x + i * (sw + 6f), y, sw, 54);
                    GUI.DrawTexture(r, skin.White, ScaleMode.StretchToFill, true, 0, new Color(0.06f, 0.11f, 0.2f, 0.9f), 0, 10);
                    var c = slot.Fish != null ? UiSkin.RarityColor(slot.Fish.RarityId) : UiSkin.Border;
                    GUI.DrawTexture(r, skin.White, ScaleMode.StretchToFill, true, 0, new Color(c.r, c.g, c.b, slot.Fish != null ? 0.8f : 0.5f), 1.5f, 10);
                    if (slot.Fish != null)
                    {
                        GUI.DrawTexture(new Rect(r.x + 4, r.y + 4, r.width - 8, 32), Art.FishTexture(slot.Fish.SpeciesId), ScaleMode.ScaleToFit, true);
                        GUI.Label(new Rect(r.x, r.y + 34, r.width - 4, 18), GameTexts.Player.LevelShort + " " + slot.Fish.Level, skin.SmallMutedRight);
                        if (r.Contains(Event.current.mousePosition))
                        {
                            hovered = slot;
                            hoveredRect = r;
                        }
                    }
                }

                // Species, rarity and level of the fish under the mouse: the rarity in words, not only the border.
                if (hovered != null)
                {
                    var tip = hovered.Fish.SpeciesName + " · " + hovered.Fish.RarityName + " · " + GameTexts.Player.LevelShort + " " + hovered.Fish.Level;
                    var tw = skin.SmallBold.CalcSize(new GUIContent(tip)).x + 20f;
                    var tr = new Rect(Mathf.Min(hoveredRect.x, x + w - tw), hoveredRect.yMax + 4, tw, 26);
                    GUI.DrawTexture(tr, skin.White, ScaleMode.StretchToFill, true, 0, new Color(0.04f, 0.08f, 0.14f, 0.95f), 0, 8);
                    GUI.Label(new Rect(tr.x + 10, tr.y + 4, tw - 12, 20), tip, skin.SmallBold);
                }

                y += 62;
                var note = _cardume.CompleteBonusActive
                    ? GameTexts.Profile.CardumeFull(_cardume.Filled, _cardume.Size, Format.Percent(_cardume.CompleteBonusPercent / 100.0, 0))
                    : GameTexts.Profile.CardumeCount(_cardume.Filled, _cardume.Size);
                if (hovered == null)
                {
                    GUI.Label(new Rect(x, y, w, 20), FishCard.Fit(note, skin.SmallMuted, w), skin.SmallMuted);
                }
            }

            // Middle: gear and space.
            x = mid.x + 18;
            w = mid.width - 36;
            y = mid.y + 14;
            var gear = _root.Gear;
            y = SectionTitle(skin, x, y, w, Icons.Rod, GameTexts.Gear.Yours, gear != null ? GameTexts.Profile.ChanceBonus(Format.Percent(gear.TotalBonus, 0)) : null);
            if (gear != null)
            {
                var rod = _profile.EquippedRod;
                y = GearLine(skin, x, y, w, Icons.Rod, gear.RodName, rod != null && rod.HasLevels ? GameTexts.Profile.RodLevel(rod.Level, rod.MaxLevel) : GameTexts.Gear.Rod, gear.RodBonus);
                y = GearLine(skin, x, y, w, Icons.Boat, gear.BoatName, GameTexts.Gear.Boat, gear.BoatBonus);
                y = GearLine(skin, x, y, w, Icons.Bait, gear.BaitName ?? GameTexts.Gear.NoBait,
                    gear.BaitName != null ? GameTexts.Profile.BaitLeft(gear.BaitChargesLeft) : GameTexts.Gear.Bait, gear.BaitBonus);
            }

            y += 8;
            y = SectionTitle(skin, x, y, w, Icons.Box, GameTexts.Profile.Space, null);
            var player = _root.Player;
            if (player != null)
            {
                y = MeterRow(skin, x, y, w, GameTexts.Box.Title, player.FishingBoxCount, player.FishingBoxCapacity);
                y = MeterRow(skin, x, y, w, GameTexts.Aquarium.Title, player.AquariumCount, player.AquariumCapacity);
            }

            var expedition = _expeditions?.Active;
            y = InfoRow(skin, x, y, w, GameTexts.Profile.Expedition,
                expedition == null ? GameTexts.Profile.NoExpedition : expedition.Name + " · " + Format.Countdown(expedition.SecondsLeft),
                expedition == null ? (Color?)null : UiSkin.Gold);

            // Right: collection and highlights.
            x = right.x + 18;
            w = right.width - 36;
            y = right.y + 14;
            var records = _profile.Records;
            y = SectionTitle(skin, x, y, w, Icons.Book, GameTexts.Profile.Collection, null);
            y = MeterRow(skin, x, y, w, GameTexts.Profile.Discovered, records.SpeciesDiscovered, records.SpeciesTotal, UiSkin.Rare);

            // Every rarity with its numbers (owner's request): species found / total and fish caught.
            var colSpecies = x + w * 0.42f;
            var colCaught = x + w * 0.70f;
            GUI.Label(new Rect(colSpecies, y, w * 0.28f, 18), GameTexts.Profile.ColumnSpecies, skin.SmallMuted);
            GUI.Label(new Rect(colCaught, y, x + w - colCaught, 18), GameTexts.Profile.ColumnCaught, skin.SmallMutedRight);
            y += 22;
            foreach (var t in records.ByRarity)
            {
                var color = UiSkin.RarityColor(t.RarityId);
                GUI.DrawTexture(new Rect(x, y + 6, 10, 10), skin.White, ScaleMode.StretchToFill, true, 0, color, 0, 5);
                GUI.contentColor = Color.Lerp(color, Color.white, 0.25f);
                GUI.Label(new Rect(x + 16, y, colSpecies - x - 20, 22), FishCard.Fit(t.RarityName, skin.SmallBold, colSpecies - x - 20), skin.SmallBold);
                GUI.contentColor = Color.white;
                GUI.Label(new Rect(colSpecies, y, colCaught - colSpecies, 22), t.SpeciesFound + " / " + t.SpeciesTotal, skin.Small);
                GUI.Label(new Rect(colCaught, y, x + w - colCaught, 22), Format.Number(t.Caught), skin.SmallRight);
                y += 24;
            }

            y += 10;
            // Big amounts in short form ("12,4 mi"), the exact number under the mouse (A-124).
            var tiles = new[]
            {
                (Format.Short(records.TotalCatches), GameTexts.Profile.StatCatches, UiSkin.Text, (long)records.TotalCatches),
                (Format.Short(records.ExceptionalCatches), GameTexts.Profile.StatExceptional, UiSkin.Gold, (long)records.ExceptionalCatches),
                (Format.Short(records.PerfectCatches), GameTexts.Profile.StatPerfect, UiSkin.SizeColor("perfect"), (long)records.PerfectCatches),
                (records.BiggestSpeciesName == null ? GameTexts.Profile.None : Format.SizeCm(records.BiggestCm), records.BiggestSpeciesName == null ? GameTexts.Profile.Biggest : GameTexts.Profile.StatBiggest(records.BiggestSpeciesName), UiSkin.Text, 0L),
                (Format.Short(records.CoinsFromSales), GameTexts.Profile.StatSales, UiSkin.Text, (long)records.CoinsFromSales),
            };
            var tileW = (w - 8f) / 2f;

            // Small panels: the tiles shrink (label pinned to the bottom) so the grid ends inside the card.
            var tileH = Mathf.Clamp((right.yMax - 14 - y - 16f) / 3f, 46f, 70f);
            var hoverTile = default(Rect);
            var hoverExact = 0L;
            for (var i = 0; i < tiles.Length; i++)
            {
                var (value, label, color, exact) = tiles[i];
                var r = new Rect(x + (i % 2) * (tileW + 8f), y + (i / 2) * (tileH + 8f), tileW, tileH);
                GUI.DrawTexture(r, skin.White, ScaleMode.StretchToFill, true, 0, new Color(0.06f, 0.11f, 0.2f, 0.9f), 0, 10);
                GUI.contentColor = color;
                GUI.Label(new Rect(r.x + 10, r.y + (tileH < 54f ? 2 : 6), r.width - 16, 28), FishCard.Fit(value, skin.Heading, r.width - 16), skin.Heading);
                GUI.contentColor = Color.white;
                GUI.Label(new Rect(r.x + 10, r.yMax - 20, r.width - 16, 18), FishCard.Fit(label, skin.SmallMuted, r.width - 16), skin.SmallMuted);
                if (exact > 0 && r.Contains(Event.current.mousePosition))
                {
                    hoverTile = r;
                    hoverExact = exact;
                }
            }

            // After every tile, so the exact number is drawn on top of the tile below.
            if (hoverExact > 0)
            {
                Hud.ExactOnHover(skin, hoverTile, hoverExact);
            }
        }

        private static GUIStyle _rightBold;

        private static GUIStyle RightBold(UiSkin skin)
        {
            if (_rightBold == null || _rightBold.font != skin.SmallBold.font)
            {
                _rightBold = new GUIStyle(skin.SmallBold) { alignment = TextAnchor.UpperRight };
            }

            return _rightBold;
        }

        private static float SectionTitle(UiSkin skin, float x, float y, float w, string icon, string title, string right)
        {
            skin.DrawIcon(new Rect(x, y + 3, 20, 20), icon, UiSkin.Accent);
            GUI.Label(new Rect(x + 28, y, w - 28, 26), title, skin.Heading);
            if (right != null)
            {
                GUI.Label(new Rect(x + w * 0.4f, y + 4, w * 0.6f, 20), right, skin.SmallMutedRight);
            }

            return y + 34;
        }

        private static float InfoRow(UiSkin skin, float x, float y, float w, string label, string value, Color? color)
        {
            var lw = skin.SmallMuted.CalcSize(new GUIContent(label)).x + 12f;
            GUI.Label(new Rect(x, y, lw, 22), label, skin.SmallMuted);
            if (color.HasValue) GUI.contentColor = color.Value;
            GUI.Label(new Rect(x + lw, y, w - lw, 22), FishCard.Fit(value, skin.SmallBold, w - lw), RightBold(skin));
            GUI.contentColor = Color.white;
            skin.Divider(new Rect(x, y + 27, w, 1));
            return y + 34;
        }

        private static float GearLine(UiSkin skin, float x, float y, float w, string icon, string name, string detail, double bonus)
        {
            var tile = new Rect(x, y, 42, 42);
            GUI.DrawTexture(tile, skin.White, ScaleMode.StretchToFill, true, 0, new Color(0.06f, 0.11f, 0.2f, 0.9f), 0, 10);
            skin.DrawIcon(new Rect(tile.x + 10, tile.y + 10, 22, 22), icon, Color.white);
            var bonusText = "+" + Format.Percent(bonus, 0);
            var bw = skin.SmallBold.CalcSize(new GUIContent(bonusText)).x + 6f;
            var tw = w - 54 - bw - 6;
            GUI.Label(new Rect(x + 54, y + 2, tw, 22), FishCard.Fit(name, skin.BodyBold, tw), skin.BodyBold);
            GUI.Label(new Rect(x + 54, y + 22, tw, 18), FishCard.Fit(detail, skin.SmallMuted, tw), skin.SmallMuted);
            GUI.contentColor = UiSkin.Accent;
            GUI.Label(new Rect(x + w - bw, y + 12, bw, 20), bonusText, skin.SmallBold);
            GUI.contentColor = Color.white;
            return y + 50;
        }

        private static float MeterRow(UiSkin skin, float x, float y, float w, string label, int count, int capacity, Color? color = null)
        {
            var value = capacity > 0 ? Format.Number(count) + " / " + Format.Number(capacity) : Format.Number(count);
            var vw = skin.SmallBold.CalcSize(new GUIContent(value)).x + 4f;
            GUI.Label(new Rect(x, y, w - vw - 8, 20), FishCard.Fit(label, skin.SmallMuted, w - vw - 8), skin.SmallMuted);
            GUI.Label(new Rect(x + w - vw, y, vw, 20), value, skin.SmallBold);
            if (capacity > 0)
            {
                var fill = Mathf.Clamp01(count / (float)capacity);
                if (color.HasValue)
                {
                    skin.Bar(new Rect(x, y + 24, w, 8), fill, color.Value);
                }
                else
                {
                    skin.Bar(new Rect(x, y + 24, w, 8), fill, fill >= 0.9f);
                }
            }

            return y + 42;
        }

        // ------------------------------------------------------------------ Inventory (the backpack)

        private enum TipKind
        {
            Line,
            Row,
            Text,
            Warning,
            Divider,
            Hint,
        }

        private const float SlotGap = 10f;
        private const float ActionPanelHeight = 110f;
        private const float TipWidth = 330f;

        /// <summary>
        /// The Inventory as a game backpack: the equipped rod, boat and bait as big slots on the left with
        /// the bonuses they give, every rod as an item slot on the right, and the actions of the chosen rod.
        /// </summary>
        private void DrawInventory(UiSkin skin, Rect area)
        {
            var mouse = Event.current.mousePosition;
            _tipTitle = null;
            _tipLines.Clear();

            var rods = _profile.Inventory;
            var selected = rods.FirstOrDefault(r => r.ItemId == _selectedRodId) ?? _profile.EquippedRod ?? (rods.Count > 0 ? rods[0] : null);

            var leftWidth = Mathf.Clamp(area.width * 0.3f, 300f, 400f);
            DrawEquipped(skin, new Rect(area.x, area.y, leftWidth, area.height), mouse);

            var right = new Rect(area.x + leftWidth + 20f, area.y, area.width - leftWidth - 20f, area.height);
            DrawBackpack(skin, new Rect(right.x, right.y, right.width, right.height - ActionPanelHeight - 12f), rods, selected, mouse);
            if (selected != null)
            {
                DrawRodActions(skin, new Rect(right.x, right.yMax - ActionPanelHeight, right.width, ActionPanelHeight), selected);
            }

            // Last, so no slot, button or panel is drawn over it.
            if (_tipTitle != null)
            {
                DrawTip(skin, mouse, new Rect(area.x - 16f, area.y - 16f, area.width + 32f, area.height + 32f));
            }
        }

        /// <summary>The left column: rod, boat and bait slots, then the bonuses they add up to.</summary>
        private void DrawEquipped(UiSkin skin, Rect rect, Vector2 mouse)
        {
            GUI.Box(rect, GUIContent.none, skin.Card);
            var x = rect.x + 14f;
            var w = rect.width - 28f;
            var y = rect.y + 12f;
            GUI.Label(new Rect(x, y, w, 20), GameTexts.Profile.EquippedHeader, skin.SmallBold);
            y += 28f;

            // Three big slots, as large as the column allows once the bonus panel has its room.
            var size = Mathf.Clamp((rect.yMax - y - 2 * SlotGap - 12f - 130f) / 3f, 56f, 120f);
            var textX = x + size + 14f;
            var textW = rect.xMax - 14f - textX;
            var gear = _root.Gear;
            var rod = _profile.EquippedRod;

            var slot = new Rect(x, y, size, size);
            if (rod != null)
            {
                var hovered = Hover(slot, mouse);
                ItemSlot(skin, slot, RodArt(rod.RodId), Icons.Rod, UiSkin.TierColor(rod.Tier), false, hovered);
                if (rod.HasLevels)
                {
                    SlotBadge(skin, slot, GameTexts.Player.LevelShort + " " + rod.Level);
                }

                SlotText(skin, textX, slot, textW, rod.Name, GameTexts.Profile.RodLine(rod.Tier));
                if (hovered)
                {
                    RodTip(rod, gear);
                }

                if (GUI.Button(slot, GUIContent.none, GUIStyle.none))
                {
                    _selectedRodId = rod.ItemId;
                }
            }
            else
            {
                EmptySlot(skin, slot, Icons.Rod);
            }

            y += size + SlotGap;
            slot = new Rect(x, y, size, size);
            if (gear != null)
            {
                var boat = gear.Boats.FirstOrDefault(b => b.BoatId == gear.BoatId);
                var hovered = Hover(slot, mouse);
                ItemSlot(skin, slot, ArtAssets.Texture("Barcos/" + gear.BoatId), Icons.Boat, UiSkin.TierColor(boat?.Tier ?? 0), false, hovered);
                SlotText(skin, textX, slot, textW, gear.BoatName, GameTexts.Profile.BoatLine(Format.Percent(gear.BoatBonus, 0)));
                if (hovered)
                {
                    BoatTip(gear, boat);
                }
            }
            else
            {
                EmptySlot(skin, slot, Icons.Boat);
            }

            y += size + SlotGap;
            slot = new Rect(x, y, size, size);
            if (gear != null && gear.BaitName != null)
            {
                var bait = gear.Baits.FirstOrDefault(b => b.BaitId == gear.BaitId);
                var hovered = Hover(slot, mouse);
                ItemSlot(skin, slot, ArtAssets.Texture("Iscas/" + gear.BaitId), Icons.Bait, UiSkin.TierColor(bait?.Tier ?? 0), false, hovered);
                SlotBadge(skin, slot, GameTexts.Profile.ChargesBadge(gear.BaitChargesLeft));
                SlotText(skin, textX, slot, textW, gear.BaitName, GameTexts.Profile.BaitLine(gear.BaitChargesLeft));
                if (hovered)
                {
                    BaitTip(gear, bait);
                }
            }
            else
            {
                EmptySlot(skin, slot, Icons.Bait);
                SlotText(skin, textX, slot, textW, GameTexts.Profile.NoBaitSlot, GameTexts.Profile.NoBaitLine);
            }

            y += size + 12f;

            // The bonuses as the service gives them: the Catch Success total of rod + boat + bait, and the rod's own.
            skin.Divider(new Rect(x, y, w, 1));
            y += 10f;
            var bottom = rect.yMax - 10f;
            if (bottom - y < 26f)
            {
                return;
            }

            GUI.Label(new Rect(x, y, w, 24), GameTexts.Profile.BonusTotals, skin.BodyBold);
            y += 28f;
            if (gear != null)
            {
                y = BonusRow(skin, x, y, w, bottom, GameTexts.Shop.CatchBonus, "+" + Format.Percent(gear.TotalBonus, 0), true);
                if (bottom - y >= 20f + 3 * 24f)
                {
                    var split = GameTexts.Profile.SuccessSplit(Format.Percent(gear.RodBonus, 0), Format.Percent(gear.BoatBonus, 0), Format.Percent(gear.BaitBonus, 0));
                    GUI.Label(new Rect(x, y - 4f, w, 20), FishCard.Fit(split, skin.SmallMuted, w), skin.SmallMuted);
                    y += 20f;
                }
            }

            if (rod != null)
            {
                y = BonusRow(skin, x, y, w, bottom, GameTexts.Profile.RarityBonus, "+" + Format.Percent(rod.RarityBonus, 0), false);
                y = BonusRow(skin, x, y, w, bottom, GameTexts.Profile.SizeBonus, "+" + Format.Percent(rod.SizeBonus, 0), false);
                BonusRow(skin, x, y, w, bottom, GameTexts.Profile.ShellBonus, rod.GeneratesShells ? "+" + Format.Percent(rod.ShellBonus, 0) : GameTexts.Profile.NoShells, false);
            }
        }

        /// <summary>One "label ... value" line of the bonus panel; left out when it would pass the bottom.</summary>
        private static float BonusRow(UiSkin skin, float x, float y, float w, float bottom, string label, string value, bool gold)
        {
            if (y + 22f > bottom)
            {
                return y;
            }

            var style = gold ? skin.SmallGoldRight : RightBold(skin);
            var vw = Mathf.Min(w * 0.5f, style.CalcSize(new GUIContent(value)).x + 4f);
            GUI.Label(new Rect(x, y, w - vw - 8f, 22), FishCard.Fit(label, skin.SmallMuted, w - vw - 8f), skin.SmallMuted);
            GUI.Label(new Rect(x + w - vw, y, vw, 22), value, style);
            return y + 24f;
        }

        /// <summary>The right area: every rod as an item slot, then empty slots to fill the visible grid.</summary>
        private void DrawBackpack(UiSkin skin, Rect rect, List<RodItemView> rods, RodItemView selected, Vector2 mouse)
        {
            var title = GameTexts.Profile.RodsCount(rods.Count);
            var titleW = skin.Heading.CalcSize(new GUIContent(title)).x + 8f;
            GUI.Label(new Rect(rect.x, rect.y, titleW, 28), title, skin.Heading);
            var noteW = rect.width - titleW - 16f;
            if (noteW > 80f)
            {
                GUI.Label(new Rect(rect.xMax - noteW, rect.y + 6, noteW, 20), FishCard.Fit(GameTexts.Profile.InventoryNote, skin.SmallMuted, noteW), skin.SmallMutedRight);
            }

            // Slots of about 100 px: as many columns as fit, then the size stretches to fill the row.
            const float pad = 4f;
            var view = new Rect(rect.x - pad, rect.y + 36f, rect.width + pad, rect.height - 36f);
            var inner = view.width - 20f - pad * 2;
            var columns = Mathf.Max(3, Mathf.FloorToInt((inner + SlotGap) / (100f + SlotGap)));
            var size = (inner - (columns - 1) * SlotGap) / columns;
            var visibleRows = Mathf.Max(1, Mathf.FloorToInt((view.height - pad * 2 + SlotGap) / (size + SlotGap)));
            var rows = Mathf.Max(visibleRows, Mathf.CeilToInt(rods.Count / (float)columns));
            var mouseInView = view.Contains(mouse);

            _scroll = GUI.BeginScrollView(view, _scroll, new Rect(0, 0, view.width - 20f, rows * (size + SlotGap) - SlotGap + pad * 2));
            for (var i = 0; i < rows * columns; i++)
            {
                var r = new Rect(pad + (i % columns) * (size + SlotGap), pad + (i / columns) * (size + SlotGap), size, size);
                if (i >= rods.Count)
                {
                    EmptySlot(skin, r, null);
                    continue;
                }

                var rod = rods[i];
                var hovered = mouseInView && Hover(r, Event.current.mousePosition);
                ItemSlot(skin, r, RodArt(rod.RodId), Icons.Rod, UiSkin.TierColor(rod.Tier), selected != null && rod.ItemId == selected.ItemId, hovered);
                if (rod.HasLevels)
                {
                    SlotBadge(skin, r, GameTexts.Player.LevelShort + " " + rod.Level);
                }

                if (rod.IsEquipped)
                {
                    CheckBadge(skin, r);
                }

                if (hovered)
                {
                    RodTip(rod, _root.Gear);
                }

                if (GUI.Button(r, GUIContent.none, GUIStyle.none))
                {
                    _selectedRodId = rod.ItemId;
                }
            }

            GUI.EndScrollView();
        }

        /// <summary>The chosen rod's actions: equip, upgrade, sell or destroy (with the same confirmations as before).</summary>
        private void DrawRodActions(UiSkin skin, Rect rect, RodItemView rod)
        {
            GUI.Box(rect, GUIContent.none, skin.CardSelected);
            var tier = UiSkin.TierColor(rod.Tier);
            var thumb = new Rect(rect.x + 12f, rect.y + 12f, rect.height - 24f, rect.height - 24f);
            ItemSlot(skin, thumb, RodArt(rod.RodId), Icons.Rod, tier, false, false);
            if (rod.IsEquipped)
            {
                CheckBadge(skin, thumb);
            }

            var bw = Mathf.Clamp(rect.width * 0.55f, 300f, 520f);
            var bx = rect.xMax - 14f - bw;
            var tx = thumb.xMax + 14f;
            var tw = bx - 14f - tx;

            var prev = GUI.contentColor;
            GUI.contentColor = Color.Lerp(tier, Color.white, 0.35f);
            GUI.Label(new Rect(tx, rect.y + 12f, tw, 28), FishCard.Fit(rod.Name, skin.Heading, tw), skin.Heading);
            GUI.contentColor = prev;
            var level = rod.HasLevels ? " · " + GameTexts.Aquarium.LevelOf(rod.Level, rod.MaxLevel) : string.Empty;
            GUI.Label(new Rect(tx, rect.y + 42f, tw, 20), FishCard.Fit(GameTexts.Profile.Tier(rod.Tier) + level, skin.SmallMuted, tw), skin.SmallMuted);
            GUI.Label(new Rect(tx, rect.y + 64f, tw, 20), FishCard.Fit(CatchesText(rod), skin.Small, tw), skin.Small);

            // The upgrade on its own full row (its label is the longest), then equip state, sell and destroy.
            var row1 = rect.y + 12f;
            var row2 = row1 + 46f;
            if (rod.HasLevels)
            {
                if (rod.NextUpgradeCost > 0)
                {
                    var upgrade = rod.NextUpgradeShells > 0
                        ? GameTexts.Shop.UpgradeForWithShells(rod.Level + 1, Format.Number(rod.NextUpgradeCost), Format.Number(rod.NextUpgradeShells))
                        : GameTexts.Shop.UpgradeFor(rod.Level + 1, Format.Number(rod.NextUpgradeCost));
                    if (GUI.Button(new Rect(bx, row1, bw, 38), FishCard.Fit(upgrade, skin.Button, bw - 28f), skin.Button))
                    {
                        _root.UpgradeRod(rod.ItemId);
                    }
                }
                else
                {
                    GUI.Label(new Rect(bx, row1 + 10f, bw, 22), GameTexts.Shop.MaxLevel, skin.SmallGold);
                }
            }

            // Equip and destroy are short words; the sale shows its value, so it gets the widest place.
            var side = (bw - 16f) * 0.3f;
            var sell = bw - 16f - side * 2f;
            if (rod.IsEquipped)
            {
                var equipped = GameTexts.Profile.Equipped.ToUpperInvariant();
                skin.AccentPill(new Rect(bx, row2 + 7f, Mathf.Min(bw, skin.PillWidth(equipped, true) + 6f), 24), equipped, UiSkin.Accent, Icons.Check);
            }
            else if (!rod.AllowedOnCurrentMap)
            {
                GUI.Label(new Rect(bx, row2, side, 38), GameTexts.Profile.NotAllowedHere, skin.SmallGold);
            }
            else if (GUI.Button(new Rect(bx, row2, side, 38), FishCard.Fit(GameTexts.Profile.Equip, skin.ButtonPrimary, side - 24f), skin.ButtonPrimary))
            {
                _root.EquipRod(rod.ItemId);
            }

            if (rod.CanDispose)
            {
                if (GUI.Button(new Rect(bx + side + 8f, row2, sell, 38), FishCard.Fit(GameTexts.Shop.SellFor(Format.Number(rod.ResaleValue)), skin.Button, sell - 24f), skin.Button))
                {
                    _pendingRod = rod;
                    _pendingDestroy = false;
                }

                if (GUI.Button(new Rect(bx + side + sell + 16f, row2, side, 38), FishCard.Fit(GameTexts.Shop.DestroyRod, skin.Button, side - 24f), skin.Button))
                {
                    _pendingRod = rod;
                    _pendingDestroy = true;
                }
            }
        }

        private static Texture2D RodArt(string rodId) => ArtAssets.Texture("Varas/" + rodId);

        private static bool Hover(Rect rect, Vector2 mouse) => GUI.enabled && rect.Contains(mouse);

        private static string CatchesText(RodItemView rod)
        {
            return rod.CanCatchMythic ? GameTexts.Profile.CatchesUpToMythic
                : rod.CanCatchLegendary ? GameTexts.Profile.CatchesUpToLegendary
                : rod.CanCatchEpic ? GameTexts.Profile.CatchesRareAndEpic
                : rod.CanCatchRare ? GameTexts.Profile.CatchesRare
                : GameTexts.Profile.NoRare;
        }

        /// <summary>An item slot: dark tile tinted and framed in the tier colour, the art fitted inside (or the icon).</summary>
        private static void ItemSlot(UiSkin skin, Rect r, Texture2D art, string icon, Color tier, bool selected, bool hovered)
        {
            if (selected)
            {
                skin.DrawGlow(r, UiSkin.Accent, 0.35f);
            }

            GUI.DrawTexture(r, skin.White, ScaleMode.StretchToFill, true, 0, new Color(0.05f, 0.09f, 0.16f, 0.95f), 0, 12);
            GUI.DrawTexture(r, skin.White, ScaleMode.StretchToFill, true, 0, new Color(tier.r, tier.g, tier.b, hovered ? 0.22f : 0.13f), 0, 12);
            GUI.DrawTexture(new Rect(r.x + 3f, r.y + 3f, r.width - 6f, r.height * 0.4f), skin.White, ScaleMode.StretchToFill, true, 0, new Color(1f, 1f, 1f, 0.04f), 0, 10);

            var pad = Mathf.Max(6f, r.width * 0.1f);
            if (art != null)
            {
                GUI.DrawTexture(new Rect(r.x + pad, r.y + pad, r.width - pad * 2f, r.height - pad * 2f), art, ScaleMode.ScaleToFit, true);
            }
            else
            {
                skin.DrawIcon(new Rect(r.center.x - r.width * 0.25f, r.center.y - r.height * 0.25f, r.width * 0.5f, r.height * 0.5f), icon, Color.Lerp(tier, Color.white, 0.2f));
            }

            GUI.DrawTexture(r, skin.White, ScaleMode.StretchToFill, true, 0, new Color(tier.r, tier.g, tier.b, hovered ? 1f : 0.8f), hovered ? 2.5f : 2f, 12);
            if (selected)
            {
                GUI.DrawTexture(new Rect(r.x - 4f, r.y - 4f, r.width + 8f, r.height + 8f), skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.Accent, 2.5f, 15);
            }
        }

        /// <summary>An empty place: a dashed frame (and a faint icon of what goes there, when given).</summary>
        private static void EmptySlot(UiSkin skin, Rect r, string icon)
        {
            GUI.DrawTexture(r, skin.White, ScaleMode.StretchToFill, true, 0, new Color(0.05f, 0.09f, 0.16f, 0.45f), 0, 12);
            skin.DashedFrame(new Rect(r.x + 3f, r.y + 3f, r.width - 6f, r.height - 6f), new Color(UiSkin.Border.r, UiSkin.Border.g, UiSkin.Border.b, 0.9f));
            if (icon != null)
            {
                skin.DrawIcon(new Rect(r.center.x - r.width * 0.2f, r.center.y - r.height * 0.2f, r.width * 0.4f, r.height * 0.4f), icon, new Color(1f, 1f, 1f, 0.14f));
            }
        }

        /// <summary>A small dark tag in the slot's bottom-left corner ("Nv. 3", "×84").</summary>
        private static void SlotBadge(UiSkin skin, Rect slot, string text)
        {
            var w = Mathf.Min(slot.width - 12f, skin.SmallBold.CalcSize(new GUIContent(text)).x + 12f);
            var badge = new Rect(slot.x + 6f, slot.yMax - 26f, w, 20);
            GUI.DrawTexture(badge, skin.White, ScaleMode.StretchToFill, true, 0, new Color(0.02f, 0.05f, 0.1f, 0.88f), 0, 8);
            GUI.Label(new Rect(badge.x + 6f, badge.y + 2f, w - 6f, 18), text, skin.SmallBold);
        }

        /// <summary>The teal check in the top-right corner of the equipped rod.</summary>
        private static void CheckBadge(UiSkin skin, Rect slot)
        {
            var c = new Rect(slot.xMax - 27f, slot.y + 5f, 22, 22);
            GUI.DrawTexture(c, skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.Accent, 0, 11);
            skin.DrawIcon(new Rect(c.x + 4f, c.y + 4f, 14, 14), Icons.Check, new Color(0.03f, 0.12f, 0.16f));
        }

        /// <summary>Name and a short muted line beside an equipped slot, centred on it.</summary>
        private static void SlotText(UiSkin skin, float x, Rect slot, float w, string name, string line)
        {
            var y = slot.center.y - 22f;
            GUI.Label(new Rect(x, y, w, 24), FishCard.Fit(name, skin.BodyBold, w), skin.BodyBold);
            GUI.Label(new Rect(x, y + 24f, w, 20), FishCard.Fit(line, skin.SmallMuted, w), skin.SmallMuted);
        }

        // ------------------------------------------------------------------ Inventory tooltips

        private void BeginTip(string title, Color color)
        {
            _tipTitle = title ?? string.Empty;
            _tipColor = color;
            _tipLines.Clear();
        }

        private void TipAdd(TipKind kind, string text = null, string value = null) => _tipLines.Add((kind, text, value));

        private void RodTip(RodItemView rod, GearView gear)
        {
            BeginTip(rod.Name, UiSkin.TierColor(rod.Tier));
            TipAdd(TipKind.Line, GameTexts.Profile.Tier(rod.Tier) + (rod.HasLevels ? " · " + GameTexts.Aquarium.LevelOf(rod.Level, rod.MaxLevel) : string.Empty));
            TipAdd(TipKind.Divider);
            if (rod.IsEquipped && gear != null && gear.RodId == rod.RodId)
            {
                TipAdd(TipKind.Row, GameTexts.Shop.CatchBonus, "+" + Format.Percent(gear.RodBonus, 0));
            }

            TipAdd(TipKind.Row, GameTexts.Profile.RarityBonus, "+" + Format.Percent(rod.RarityBonus, 0));
            TipAdd(TipKind.Row, GameTexts.Profile.SizeBonus, "+" + Format.Percent(rod.SizeBonus, 0));
            TipAdd(TipKind.Row, GameTexts.Profile.ShellBonus, rod.GeneratesShells ? "+" + Format.Percent(rod.ShellBonus, 0) : GameTexts.Profile.NoShells);
            TipAdd(TipKind.Text, CatchesText(rod));
            if (!rod.AllowedOnCurrentMap)
            {
                TipAdd(TipKind.Warning, GameTexts.Profile.NotAllowedHere);
            }

            TipAdd(TipKind.Hint, GameTexts.Profile.ClickForActions);
        }

        private void BoatTip(GearView gear, BoatOfferView boat)
        {
            var tier = boat?.Tier ?? 0;
            BeginTip(gear.BoatName, UiSkin.TierColor(tier));
            TipAdd(TipKind.Line, GameTexts.Profile.BoatTier(tier));
            TipAdd(TipKind.Divider);
            TipAdd(TipKind.Row, GameTexts.Shop.CatchBonus, "+" + Format.Percent(gear.BoatBonus, 0));
            if (!string.IsNullOrEmpty(boat?.Description))
            {
                TipAdd(TipKind.Text, boat.Description);
            }

            TipAdd(TipKind.Hint, GameTexts.Profile.GearInShop);
        }

        private void BaitTip(GearView gear, BaitOfferView bait)
        {
            BeginTip(gear.BaitName, UiSkin.TierColor(bait?.Tier ?? 0));
            TipAdd(TipKind.Line, bait != null ? GameTexts.Profile.BaitTier(bait.Tier) : GameTexts.Gear.Bait);
            TipAdd(TipKind.Divider);
            TipAdd(TipKind.Row, GameTexts.Shop.CatchBonus, "+" + Format.Percent(gear.BaitBonus, 0));
            TipAdd(TipKind.Text, GameTexts.Gear.ChargesLeft(gear.BaitChargesLeft));
            if (!string.IsNullOrEmpty(bait?.Description))
            {
                TipAdd(TipKind.Text, bait.Description);
            }

            TipAdd(TipKind.Hint, GameTexts.Profile.GearInShop);
        }

        private static GUIStyle TipStyle(UiSkin skin, TipKind kind)
        {
            switch (kind)
            {
                case TipKind.Text: return skin.Small;
                case TipKind.Warning: return skin.SmallGold;
                case TipKind.Hint: return skin.SmallBold;
                default: return skin.SmallMuted;
            }
        }

        private static float TipLineHeight(UiSkin skin, TipKind kind, string text, float width)
        {
            switch (kind)
            {
                case TipKind.Divider: return 9f;
                case TipKind.Row: return 22f;
                case TipKind.Line: return 20f;
                case TipKind.Hint: return 26f;
                default: return Mathf.Max(20f, TipStyle(skin, kind).CalcHeight(new GUIContent(text), width) + 2f);
            }
        }

        /// <summary>The tooltip of the slot under the mouse, beside the cursor and kept inside <paramref name="bounds"/>.</summary>
        private void DrawTip(UiSkin skin, Vector2 mouse, Rect bounds)
        {
            var innerW = TipWidth - 28f;
            var height = 14f + 26f + 10f;
            foreach (var (kind, text, _) in _tipLines)
            {
                height += TipLineHeight(skin, kind, text, innerW);
            }

            var x = mouse.x + 18f;
            if (x + TipWidth > bounds.xMax)
            {
                x = mouse.x - 18f - TipWidth;
            }

            var y = mouse.y + 18f;
            if (y + height > bounds.yMax)
            {
                y = bounds.yMax - height;
            }

            var rect = new Rect(Mathf.Max(bounds.x, x), Mathf.Max(bounds.y, y), TipWidth, height);
            GUI.DrawTexture(rect, skin.White, ScaleMode.StretchToFill, true, 0, new Color(0.03f, 0.07f, 0.13f, 0.97f), 0, 10);
            GUI.DrawTexture(rect, skin.White, ScaleMode.StretchToFill, true, 0, new Color(_tipColor.r, _tipColor.g, _tipColor.b, 0.85f), 1.5f, 10);

            var tx = rect.x + 14f;
            var ty = rect.y + 14f;
            var prev = GUI.contentColor;
            GUI.contentColor = Color.Lerp(_tipColor, Color.white, 0.2f);
            GUI.Label(new Rect(tx, ty, innerW, 24), FishCard.Fit(_tipTitle, skin.BodyBold, innerW), skin.BodyBold);
            GUI.contentColor = prev;
            ty += 26f;

            foreach (var (kind, text, value) in _tipLines)
            {
                var h = TipLineHeight(skin, kind, text, innerW);
                switch (kind)
                {
                    case TipKind.Divider:
                        skin.Divider(new Rect(tx, ty + 4f, innerW, 1));
                        break;
                    case TipKind.Row:
                        var vw = Mathf.Min(innerW * 0.5f, RightBold(skin).CalcSize(new GUIContent(value)).x + 4f);
                        GUI.Label(new Rect(tx, ty + 1f, innerW - vw - 8f, 20), FishCard.Fit(text, skin.SmallMuted, innerW - vw - 8f), skin.SmallMuted);
                        GUI.Label(new Rect(tx + innerW - vw, ty + 1f, vw, 20), value, RightBold(skin));
                        break;
                    case TipKind.Line:
                        GUI.Label(new Rect(tx, ty, innerW, 20), FishCard.Fit(text, skin.SmallMuted, innerW), skin.SmallMuted);
                        break;
                    case TipKind.Hint:
                        GUI.contentColor = UiSkin.Accent;
                        GUI.Label(new Rect(tx, ty + 6f, innerW, 20), FishCard.Fit(text, skin.SmallBold, innerW), skin.SmallBold);
                        GUI.contentColor = prev;
                        break;
                    default:
                        GUI.Label(new Rect(tx, ty, innerW, h), text, TipStyle(skin, kind));
                        break;
                }

                ty += h;
            }
        }

        // ------------------------------------------------------------------ Cardume

        private void DrawCardume(UiSkin skin, Rect area)
        {
            var listWidth = 380f;
            var formation = new Rect(area.x, area.y, area.width - listWidth - 24, area.height);
            var list = new Rect(area.xMax - listWidth, area.y, listWidth, area.height);

            // A battle line, not a form (owner's request, A-129): one panel like water, a header with the
            // Strength and the 6 places, then the front row and the back row, slightly shifted for depth.
            GUI.DrawTexture(formation, skin.White, ScaleMode.StretchToFill, true, 0, new Color(0.05f, 0.16f, 0.24f, 0.92f), 0, 16);
            GUI.DrawTexture(new Rect(formation.x, formation.y + formation.height * 0.45f, formation.width, formation.height * 0.55f), skin.White, ScaleMode.StretchToFill, true, 0, new Color(0.03f, 0.10f, 0.17f, 0.55f), 0, 16);
            GUI.DrawTexture(formation, skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.Border, 1.5f, 16);

            var x = formation.x + 20;
            var w = formation.width - 40;
            var y = formation.y + 14;

            // Header: Strength, the six places as dots, and the full-Cardume bonus.
            GUI.Label(new Rect(x, y, 260, 18), GameTexts.Cardume.Strength, skin.SmallMuted);
            var strengthText = Format.Number(_cardume.Strength);
            var strengthWidth = skin.Display.CalcSize(new GUIContent(strengthText)).x;
            GUI.contentColor = UiSkin.Gold;
            GUI.Label(new Rect(x, y + 16, Mathf.Max(260f, strengthWidth + 8f), 40), strengthText, skin.Display);
            GUI.contentColor = Color.white;

            // The dots start after the number as drawn, so a big Strength never touches them.
            var dotsX = x + Mathf.Max(230f, strengthWidth + 18f);
            for (var i = 0; i < _cardume.Size; i++)
            {
                var filled = i < _cardume.Filled;
                GUI.DrawTexture(new Rect(dotsX + i * 18, y + 26, 12, 12), skin.White, ScaleMode.StretchToFill, true, 0, filled ? UiSkin.Accent : new Color(1f, 1f, 1f, 0.15f), 0, 6);
            }

            GUI.Label(new Rect(dotsX + _cardume.Size * 18 + 6, y + 22, 120, 20), GameTexts.Cardume.Filled(_cardume.Filled, _cardume.Size), skin.SmallBold);
            var percent = Format.Percent(_cardume.CompleteBonusPercent / 100.0, 0);
            var bonus = _cardume.CompleteBonusActive ? GameTexts.Cardume.BonusShort(percent) : GameTexts.Cardume.BonusMissingShort(_cardume.CompleteBonusSlots - _cardume.Filled, percent);
            var bw = skin.PillWidth(bonus, true) + 8;
            skin.AccentPill(new Rect(formation.xMax - 20 - bw, y + 20, bw, 24), bonus, _cardume.CompleteBonusActive ? UiSkin.Gold : UiSkin.Muted, Icons.Star);
            y += 68;

            // The two rows. Slots are as wide as fits; the back row is shifted a little to the right.
            var tagWidth = 34f;
            var slotW = Mathf.Min(210f, (w - tagWidth - 24 - 2 * 12f) / 3f);
            // The slot height comes from the room left once the footer is reserved, so the footer always
            // fits; when space is short the footer becomes one line with a lower button.
            var compact = (formation.yMax - y - 28f - 54f) / 2f < 150f;
            var footerH = compact ? 30f : 40f;
            var slotH = Mathf.Clamp((formation.yMax - y - 28f - footerH - 12f) / 2f, 120f, 196f);
            var maxStrength = Mathf.Max(1f, _cardume.Slots.Where(sl => sl.Fish != null).Select(sl => (float)sl.Strength).DefaultIfEmpty(1f).Max());
            DrawRow(skin, _cardume.Slots.Where(sl => sl.IsFront).ToList(), GameTexts.Cardume.FrontTag, x, y, tagWidth, slotW, slotH, maxStrength);
            y += slotH + 14;
            DrawRow(skin, _cardume.Slots.Where(sl => !sl.IsFront).ToList(), GameTexts.Cardume.BackTag, x + 24, y, tagWidth, slotW, slotH, maxStrength);
            y += slotH + 14;

            // Footer: the attack order and how Strength is used; "Tirar da posição" on the right.
            var selected = _cardume.Slots.FirstOrDefault(sl => sl.Position == _selectedPosition);
            var removeWidth = selected?.Fish != null ? 220f : 0f;
            var textWidth = w - removeWidth - 12;
            if (compact)
            {
                var oneLine = GameTexts.Cardume.OrderShort + " · " + GameTexts.Cardume.StrengthPrivate;
                GUI.Label(new Rect(x, y + 5, textWidth, 20), FishCard.Fit(oneLine, skin.SmallMuted, textWidth), skin.SmallMuted);
            }
            else
            {
                GUI.Label(new Rect(x, y, textWidth, 20), FishCard.Fit(GameTexts.Cardume.OrderShort, skin.SmallMuted, textWidth), skin.SmallMuted);
                GUI.Label(new Rect(x, y + 20, textWidth, 20), FishCard.Fit(GameTexts.Cardume.StrengthPrivate, skin.SmallMuted, textWidth), skin.SmallMuted);
            }

            if (selected?.Fish != null && skin.IconButton(new Rect(formation.xMax - 20 - removeWidth, y, removeWidth, compact ? 30f : 36f), Icons.Close, GameTexts.Cardume.Remove, skin.Button))
            {
                _root.ClearCardumeSlot(_selectedPosition);
            }

            DrawFishList(skin, list);
        }

        private void DrawRow(UiSkin skin, List<CardumeSlotView> slots, string tag, float x, float y, float tagWidth, float slotW, float slotH, float maxStrength)
        {
            // A vertical tag for the row ("FRENTE" / "TRÁS").
            var tagRect = new Rect(x, y, tagWidth - 8, slotH);
            GUI.DrawTexture(tagRect, skin.White, ScaleMode.StretchToFill, true, 0, new Color(UiSkin.Accent.r, UiSkin.Accent.g, UiSkin.Accent.b, 0.14f), 0, 10);
            var letters = tag.ToCharArray();
            var ly = tagRect.center.y - letters.Length * 9f;
            foreach (var ch in letters)
            {
                GUI.Label(new Rect(tagRect.x, ly, tagRect.width, 18), ch.ToString(), skin.SmallMutedCenter);
                ly += 18;
            }

            var sx = x + tagWidth;
            foreach (var slot in slots)
            {
                var rect = new Rect(sx, y, slotW, slotH);
                var selected = slot.Position == _selectedPosition;
                var hovered = GUI.enabled && rect.Contains(Event.current.mousePosition);
                var accent = slot.Fish != null ? UiSkin.RarityColor(slot.Fish.RarityId) : UiSkin.Accent;
                if (selected)
                {
                    skin.DrawGlow(rect, UiSkin.Accent, 0.45f);
                }

                if (GUI.Button(rect, GUIContent.none, selected ? skin.CardSelected : hovered ? skin.CardHovered : skin.Card))
                {
                    _selectedPosition = slot.Position;
                }

                // Position number in a round badge, top-left; the rarity seal top-right.
                var badge = new Rect(rect.x + 10, rect.y + 10, 26, 26);
                GUI.DrawTexture(badge, skin.White, ScaleMode.StretchToFill, true, 0, selected ? UiSkin.Accent : new Color(0.06f, 0.12f, 0.2f, 0.95f), 0, 13);
                GUI.DrawTexture(badge, skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.Accent, 1.5f, 13);
                var prev = GUI.contentColor;
                GUI.contentColor = selected ? new Color(0.03f, 0.12f, 0.16f) : Color.white;
                GUI.Label(new Rect(badge.x, badge.y + 4, badge.width, 18), slot.Position.ToString(), CenteredBold(skin));
                GUI.contentColor = prev;

                if (slot.Fish == null)
                {
                    // An inviting empty place: a soft dashed frame, a "+" and the word.
                    GUI.DrawTexture(new Rect(rect.x + 6, rect.y + 6, rect.width - 12, rect.height - 12), skin.White, ScaleMode.StretchToFill, true, 0, new Color(UiSkin.Accent.r, UiSkin.Accent.g, UiSkin.Accent.b, hovered || selected ? 0.5f : 0.22f), 1.5f, 12);
                    var plus = new Rect(rect.center.x - 20, rect.center.y - 26, 40, 40);
                    GUI.DrawTexture(plus, skin.White, ScaleMode.StretchToFill, true, 0, new Color(UiSkin.Accent.r, UiSkin.Accent.g, UiSkin.Accent.b, 0.18f), 0, 20);
                    skin.DrawIcon(new Rect(plus.x + 10, plus.y + 10, 20, 20), Icons.Add, UiSkin.Accent);
                    GUI.Label(new Rect(rect.x, plus.yMax + 6, rect.width, 22), GameTexts.Cardume.Empty, skin.SmallMutedCenter);
                    sx += slotW + 12f;
                    continue;
                }

                if (!selected)
                {
                    skin.DrawOutline(rect, new Color(accent.r, accent.g, accent.b, slot.Fish.RarityId == "common" ? 0.45f : 0.85f));
                }

                if (!string.IsNullOrEmpty(slot.Fish.RarityName))
                {
                    var rarity = slot.Fish.RarityName.ToUpperInvariant();
                    var pw = skin.PillWidth(rarity, false);
                    skin.RarityPill(new Rect(rect.xMax - 10 - pw, rect.y + 13, pw, 20), slot.Fish.RarityId, rarity, false);
                }

                // The fish, big, then name, level and its share of the Strength as a bar.
                if (rect.height < 160f)
                {
                    // Short slots (small panels): level and Strength share one line, the bar goes under it.
                    var shortArt = rect.height - 40 - 54;
                    if (shortArt > 8f)
                    {
                        GUI.DrawTexture(new Rect(rect.x + 10, rect.y + 40, rect.width - 20, shortArt), Art.FishTexture(slot.Fish.SpeciesId), ScaleMode.ScaleToFit, true);
                    }

                    var sy = rect.yMax - 52;
                    GUI.Label(new Rect(rect.x + 12, sy, rect.width - 24, 22), FishCard.Fit(slot.Fish.SpeciesName, skin.BodyBold, rect.width - 24), skin.BodyBold);
                    var strength = GameTexts.Cardume.StrengthShort(Format.Number(slot.Strength));
                    var stw = Mathf.Min(rect.width - 24, skin.SmallMuted.CalcSize(new GUIContent(strength)).x + 4f);
                    GUI.Label(new Rect(rect.x + 12, sy + 22, rect.width - 24 - stw - 6, 18), GameTexts.Player.LevelShort + " " + slot.Fish.Level, skin.SmallMuted);
                    GUI.Label(new Rect(rect.xMax - 12 - stw, sy + 22, stw, 18), strength, skin.SmallMutedRight);
                    skin.Bar(new Rect(rect.x + 12, sy + 42, rect.width - 24, 5), Mathf.Clamp01(slot.Strength / maxStrength), accent);
                    sx += slotW + 12f;
                    continue;
                }

                var artH = rect.height - 40 - 78;
                GUI.DrawTexture(new Rect(rect.x + 10, rect.y + 40, rect.width - 20, artH), Art.FishTexture(slot.Fish.SpeciesId), ScaleMode.ScaleToFit, true);
                var ty = rect.yMax - 74;
                GUI.Label(new Rect(rect.x + 12, ty, rect.width - 24, 22), FishCard.Fit(slot.Fish.SpeciesName, skin.BodyBold, rect.width - 24), skin.BodyBold);
                GUI.Label(new Rect(rect.x + 12, ty + 22, rect.width - 24, 18), GameTexts.Player.LevelShort + " " + slot.Fish.Level, skin.SmallMuted);
                skin.Bar(new Rect(rect.x + 12, ty + 46, rect.width - 24, 6), Mathf.Clamp01(slot.Strength / maxStrength), accent);
                GUI.Label(new Rect(rect.x + 12, ty + 52, rect.width - 24, 18), GameTexts.Cardume.StrengthShort(Format.Number(slot.Strength)), skin.SmallMutedRight);
                sx += slotW + 12f;
            }
        }

        private static GUIStyle _centeredBold;

        private static GUIStyle CenteredBold(UiSkin skin)
        {
            if (_centeredBold == null || _centeredBold.font != skin.SmallBold.font)
            {
                _centeredBold = new GUIStyle(skin.SmallBold) { alignment = TextAnchor.UpperCenter };
            }

            return _centeredBold;
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
                GUI.Label(new Rect(row.x + 96, row.y + 8, row.width - 150, 20), FishCard.Fit(f.SpeciesName + " · " + GameTexts.Player.LevelShort + " " + f.Level, skin.BodyBold, row.width - 150), skin.BodyBold);
                skin.SizeLine(new Rect(row.x + 96, row.y + 30, row.width - 150, 20), Format.SizeCm(f.SizeCm) + " · " + f.SizeCategoryName, f.SizeCategoryName, f.SizeCategoryId, skin.Small);
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
            var all = _profile.Encyclopedia;

            // By map (M22-T11): a chip per map with its progress; "Todos" shows every species.
            var maps = new List<(string Id, string Name)>();
            foreach (var e in all)
            {
                if (e.MapId != null && !maps.Any(m => m.Id == e.MapId))
                {
                    maps.Add((e.MapId, e.MapName));
                }
            }

            var cx = area.x;
            var cy = area.y;
            var chips = new List<(string Id, string Label)> { (null, GameTexts.Profile.AllMaps(all.Count(e => e.Discovered), all.Count)) };
            chips.AddRange(maps.Select(m => (m.Id, GameTexts.Profile.MapProgress(m.Name, all.Count(e => e.MapId == m.Id && e.Discovered), all.Count(e => e.MapId == m.Id)))));
            foreach (var (id, label) in chips)
            {
                var cw = skin.Chip.CalcSize(new GUIContent(label)).x + 12f;
                if (cx + cw > area.xMax)
                {
                    cx = area.x;
                    cy += 38;
                }

                if (GUI.Button(new Rect(cx, cy, cw, 32), label, _encMap == id ? skin.ChipActive : skin.Chip))
                {
                    _encMap = id;
                    _scroll = Vector2.zero;
                }

                cx += cw + 6;
            }

            var entries = _encMap == null ? all : all.Where(e => e.MapId == _encMap).ToList();
            var top = cy + 44 - area.y;

            const float cardW = 236f, cardH = 176f, gap = 12f;
            var view = new Rect(area.x - 4, area.y + top, area.width + 8, area.height - top);
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
                    // Still a secret, but you know where to look and how rare it is (M22-T11).
                    GUI.Label(new Rect(rect.x + 14, rect.y + 90, rect.width - 28, 22), GameTexts.Profile.Undiscovered, skin.BodyBold);
                    var hidden = (e.RarityName ?? string.Empty).ToUpperInvariant();
                    var hw = skin.PillWidth(hidden, false);
                    if (hidden.Length > 0)
                    {
                        skin.RarityPill(new Rect(rect.xMax - 14 - hw, rect.y + 116, hw, 20), e.RarityId, hidden, false);
                    }

                    GUI.Label(new Rect(rect.x + 14, rect.y + 116, rect.width - 36 - hw, 20), FishCard.Fit(e.MapName ?? string.Empty, skin.SmallMuted, rect.width - 36 - hw), skin.SmallMuted);
                    continue;
                }

                // The rarity seal goes on the map line, so a long name never runs under it.
                GUI.Label(new Rect(rect.x + 14, rect.y + 88, rect.width - 28, 22), FishCard.Fit(e.Name, skin.BodyBold, rect.width - 28), skin.BodyBold);
                var rarity = (e.RarityName ?? string.Empty).ToUpperInvariant();
                var pw = skin.PillWidth(rarity, false);
                skin.RarityPill(new Rect(rect.xMax - 14 - pw, rect.y + 110, pw, 20), e.RarityId, rarity, false);
                GUI.Label(new Rect(rect.x + 14, rect.y + 110, rect.width - 36 - pw, 20), FishCard.Fit(e.MapName, skin.SmallMuted, rect.width - 36 - pw), skin.SmallMuted);
                GUI.Label(new Rect(rect.x + 14, rect.y + 130, rect.width - 28, 20), GameTexts.Profile.Largest + ": " + Format.SizeCm(e.LargestCm), skin.Small);
                GUI.Label(new Rect(rect.x + 14, rect.y + 150, rect.width - 28, 20),
                    FishCard.Fit(GameTexts.Profile.TimesCaught + ": " + Format.Number(e.TimesCaught) + " · " + GameTexts.Profile.BiteShare(Format.Percent(e.BiteShare, e.BiteShare < 0.01 ? 1 : 0)), skin.Small, rect.width - 28), skin.Small);
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
            Row(skin, x, ref y, w, GameTexts.Profile.Perfect, Format.Number(r.PerfectCatches));
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
            _arena = _root.GetArena();
            _expeditions = _root.GetExpeditions();
        }
    }
}
