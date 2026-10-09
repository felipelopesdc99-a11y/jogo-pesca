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

        // The Encyclopedia album (A-143): the chosen card, the page, the sheet over the page in a short window,
        // and when the chosen species changed (the sheet fades it in).
        private string _encSelected;
        private int _encPage;
        private bool _encOpen;
        private bool _encOverlay;
        private float _encChangedAt = -10f;

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

            // In a short window the species sheet covers the album page: Esc goes back to the page first.
            if (_tab == Tab.Encyclopedia && _encOverlay && _encOpen)
            {
                _encOpen = false;
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
                    _encOpen = false;
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
                GUI.Label(new Rect(tile.x + 42, tile.y + 5, tw - 46, UiSkin.SmallLine), label, skin.SmallMuted);
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
            GUI.Label(new Rect(badge.x + 8, badge.y + 1, lw, UiSkin.SmallLine), level, skin.SmallBold);
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
                        GUI.Label(new Rect(r.x, r.y + 33, r.width - 4, UiSkin.SmallLine), GameTexts.Player.LevelShort + " " + slot.Fish.Level, skin.SmallMutedRight);
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
            GUI.Label(new Rect(colSpecies, y, w * 0.28f, UiSkin.SmallLine), GameTexts.Profile.ColumnSpecies, skin.SmallMuted);
            GUI.Label(new Rect(colCaught, y, x + w - colCaught, UiSkin.SmallLine), GameTexts.Profile.ColumnCaught, skin.SmallMutedRight);
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
                GUI.Label(new Rect(r.x + 10, r.yMax - 21, r.width - 16, UiSkin.SmallLine), FishCard.Fit(label, skin.SmallMuted, r.width - 16), skin.SmallMuted);
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
            GUI.Label(new Rect(x + 54, y + 21, tw, UiSkin.SmallLine), FishCard.Fit(detail, skin.SmallMuted, tw), skin.SmallMuted);
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
            GUI.Label(new Rect(badge.x + 6f, badge.y, w - 6f, UiSkin.SmallLine), text, skin.SmallBold);
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
                case TipKind.Line: return 21f;
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
            GUI.Label(new Rect(x, y, 260, UiSkin.SmallLine), GameTexts.Cardume.Strength, skin.SmallMuted);
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
                    GUI.Label(new Rect(rect.x + 12, sy + 21, rect.width - 24 - stw - 6, UiSkin.SmallLine), GameTexts.Player.LevelShort + " " + slot.Fish.Level, skin.SmallMuted);
                    GUI.Label(new Rect(rect.xMax - 12 - stw, sy + 21, stw, UiSkin.SmallLine), strength, skin.SmallMutedRight);
                    skin.Bar(new Rect(rect.x + 12, sy + 42, rect.width - 24, 5), Mathf.Clamp01(slot.Strength / maxStrength), accent);
                    sx += slotW + 12f;
                    continue;
                }

                var artH = rect.height - 40 - 78;
                GUI.DrawTexture(new Rect(rect.x + 10, rect.y + 40, rect.width - 20, artH), Art.FishTexture(slot.Fish.SpeciesId), ScaleMode.ScaleToFit, true);
                var ty = rect.yMax - 74;
                GUI.Label(new Rect(rect.x + 12, ty, rect.width - 24, 22), FishCard.Fit(slot.Fish.SpeciesName, skin.BodyBold, rect.width - 24), skin.BodyBold);
                GUI.Label(new Rect(rect.x + 12, ty + 21, rect.width - 24, UiSkin.SmallLine), GameTexts.Player.LevelShort + " " + slot.Fish.Level, skin.SmallMuted);
                skin.Bar(new Rect(rect.x + 12, ty + 46, rect.width - 24, 6), Mathf.Clamp01(slot.Strength / maxStrength), accent);
                GUI.Label(new Rect(rect.x + 12, ty + 53, rect.width - 24, UiSkin.SmallLine), GameTexts.Cardume.StrengthShort(Format.Number(slot.Strength)), skin.SmallMutedRight);
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

        /// <summary>
        /// The Encyclopedia as a card album (A-143, owner's decision on 08/10/2026): binder tabs per map on the left
        /// ("Todas" first, each with its x/y), a page of cards sized by the room, and the species sheet (the fish on a
        /// pedestal) beside the page, or over it in a short window. Only what the service reports; nothing to feed or sell.
        /// </summary>
        private void DrawEncyclopedia(UiSkin skin, Rect area)
        {
            var all = _profile.Encyclopedia;
            var maps = new List<(string Id, string Name)>();
            foreach (var e in all)
            {
                if (e.MapId != null && !maps.Any(m => m.Id == e.MapId))
                {
                    maps.Add((e.MapId, e.MapName));
                }
            }

            const float gap = 14f;
            var tabsW = Mathf.Clamp(area.width * 0.16f, 168f, 200f);
            DrawEncTabs(skin, new Rect(area.x, area.y, tabsW, area.height), all, maps);

            // The sheet sits beside the page when there is room for it; otherwise it covers the page, with Voltar.
            var rest = new Rect(area.x + tabsW, area.y, area.width - tabsW, area.height);
            var sheetW = Mathf.Clamp(rest.width * 0.38f, 340f, 400f);
            var docked = area.height >= 500f && rest.width - sheetW - gap >= 2f * EncCardMinW + 60f;
            _encOverlay = !docked;
            var page = docked ? new Rect(rest.x, rest.y, rest.width - sheetW - gap, rest.height) : rest;
            var sheet = docked ? new Rect(page.xMax + gap, rest.y, sheetW, rest.height) : rest;

            var entries = _encMap == null ? all : all.Where(e => e.MapId == _encMap).ToList();
            var title = _encMap == null ? GameTexts.Profile.EncAllTitle : maps.FirstOrDefault(m => m.Id == _encMap).Name ?? GameTexts.Profile.EncAllTitle;
            if (entries.Count == 0)
            {
                GUI.Box(page, GUIContent.none, skin.Card);
                GUI.Label(new Rect(page.x + 20, page.y + 20, page.width - 40, 24), GameTexts.Profile.EncEmpty, skin.SmallMuted);
                return;
            }

            var grid = EncGridLayout(page);
            var perPage = grid.Cols * grid.Rows;
            var pages = (entries.Count + perPage - 1) / perPage;
            _encPage = Mathf.Clamp(_encPage, 0, pages - 1);

            // The chosen card stays while it is in this tab; otherwise the first card of the page.
            var index = entries.FindIndex(e => e.SpeciesId == _encSelected);
            if (index < 0)
            {
                index = Mathf.Min(_encPage * perPage, entries.Count - 1);
                EncSelect(entries[index].SpeciesId);
            }

            // The keyboard arrows walk the species like ‹ › (not while a dialog is open); the page follows.
            var ev = Event.current;
            if (GUI.enabled && entries.Count > 1 && ev.type == EventType.KeyDown && (ev.keyCode == KeyCode.LeftArrow || ev.keyCode == KeyCode.RightArrow))
            {
                index = EncStep(entries, index, ev.keyCode == KeyCode.LeftArrow ? -1 : 1);
                _encPage = index / perPage;
                ev.Use();
            }

            if (docked || !_encOpen)
            {
                var clicked = DrawEncPage(skin, page, grid, entries, all, index, title, pages);
                if (clicked >= 0)
                {
                    index = clicked;
                    EncSelect(entries[index].SpeciesId);
                    _encOpen = !docked;
                }
            }

            if (docked || _encOpen)
            {
                var after = DrawEncSheet(skin, sheet, entries, index, !docked);
                if (after != index)
                {
                    _encPage = after / perPage;
                }
            }
        }

        private const float EncCardMinW = 142f, EncCardMinH = 196f, EncCardGap = 12f;

        /// <summary>How many cards fit on the page (by the room: about 8 in the big window, 6 in the short one) and their size.</summary>
        private static (Rect Area, int Cols, int Rows, float W, float H) EncGridLayout(Rect page)
        {
            var area = new Rect(page.x + 16f, page.y + 62f, page.width - 32f, page.height - 62f - 58f);
            var cols = Mathf.Max(1, Mathf.FloorToInt((area.width + EncCardGap) / (EncCardMinW + EncCardGap)));
            var rows = Mathf.Max(1, Mathf.FloorToInt((area.height + EncCardGap) / (EncCardMinH + EncCardGap)));
            var w = (area.width - (cols - 1) * EncCardGap) / cols;
            var h = (area.height - (rows - 1) * EncCardGap) / rows;

            // Card proportions: never a stretched strip, however the room is shaped.
            h = Mathf.Min(h, w * 1.5f);
            w = Mathf.Min(w, h * 0.85f);
            return (area, cols, rows, w, h);
        }

        /// <summary>The binder tabs: "Todas" and one per map, each with its found/total (gold when the map is complete).</summary>
        private void DrawEncTabs(UiSkin skin, Rect rect, List<EncyclopediaEntryView> all, List<(string Id, string Name)> maps)
        {
            var tabs = new List<(string Id, string Name, int Found, int Total)>
            {
                (null, GameTexts.Profile.EncAllTab, all.Count(e => e.Discovered), all.Count),
            };
            tabs.AddRange(maps.Select(m => (m.Id, m.Name, all.Count(e => e.MapId == m.Id && e.Discovered), all.Count(e => e.MapId == m.Id))));

            var theme = VisualTheme.Current;
            const float tabGap = 4f;
            var h = Mathf.Clamp((rect.height - (tabs.Count - 1) * tabGap) / tabs.Count, 26f, 44f);
            var y = rect.y;
            foreach (var (id, name, found, total) in tabs)
            {
                if (y + h > rect.yMax)
                {
                    break;
                }

                var active = _encMap == id;
                var r = new Rect(rect.x, y, rect.width - (active ? 0f : 8f), h);
                var hovered = GUI.enabled && r.Contains(Event.current.mousePosition);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none) && !active)
                {
                    _encMap = id;
                    _encPage = 0;
                    _encOpen = false;
                }

                // ASSET_PENDENTE: ui_enc_binder_tab.png (binder tab, 9-slice, tinted at runtime) replaces this drawn tab.
                var fill = active ? theme.PanelElevated : Color.Lerp(theme.Night, theme.Panel, hovered ? 0.85f : 0.55f);
                GUI.DrawTexture(r, skin.White, ScaleMode.StretchToFill, true, 0, fill, 0, 8f);
                GUI.DrawTexture(r, skin.White, ScaleMode.StretchToFill, true, 0, active ? UiSkin.Border : new Color(UiSkin.Border.r, UiSkin.Border.g, UiSkin.Border.b, 0.5f), 1f, 8f);
                if (active)
                {
                    GUI.DrawTexture(new Rect(r.x + 4f, r.y + 7f, 3f, r.height - 14f), skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.Accent, 0, 1.5f);
                }

                var progress = GameTexts.Profile.EncTabProgress(found, total);
                var complete = total > 0 && found == total;
                var progressStyle = complete ? skin.SmallGoldLine : skin.SmallMutedRightLine;
                var pw = progressStyle.CalcSize(new GUIContent(progress)).x + 4f;
                var ty = r.y + (r.height - 20f) / 2f;
                var nameStyle = active ? skin.SmallBold : skin.SmallMuted;
                var nw = r.width - 14f - pw - 16f;
                GUI.Label(new Rect(r.x + 14f, ty, nw, 20f), FishCard.Fit(name ?? string.Empty, nameStyle, nw), nameStyle);
                GUI.Label(new Rect(r.xMax - 10f - pw, ty, pw, 20f), progress, progressStyle);
                y += h + tabGap;
            }
        }

        /// <summary>The album page: title and progress, the cards, empty pockets and the page arrows. Returns the clicked card or -1.</summary>
        private int DrawEncPage(UiSkin skin, Rect page, (Rect Area, int Cols, int Rows, float W, float H) grid, List<EncyclopediaEntryView> entries,
            List<EncyclopediaEntryView> all, int selected, string title, int pages)
        {
            // ASSET_PENDENTE: ui_enc_album_page.png (album page, 9-slice) replaces the plain panel.
            GUI.Box(page, GUIContent.none, skin.Card);

            var found = entries.Count(e => e.Discovered);
            var x = page.x + 18f;
            var w = page.width - 36f;
            var progress = GameTexts.Profile.Discovery(found, entries.Count);
            var pw = Mathf.Min(w * 0.45f, skin.SmallMutedRightLine.CalcSize(new GUIContent(progress)).x + 4f);
            GUI.Label(new Rect(x, page.y + 14f, w - pw - 10f, 28f), FishCard.Fit(title, skin.Heading, w - pw - 10f), skin.Heading);
            GUI.Label(new Rect(x + w - pw, page.y + 20f, pw, 20f), FishCard.Fit(progress, skin.SmallMutedRightLine, pw), skin.SmallMutedRightLine);
            skin.Bar(new Rect(x, page.y + 46f, w, 6f), found / (float)entries.Count, UiSkin.Accent);

            // The mouse wheel over the cards turns the page.
            var ev = Event.current;
            if (GUI.enabled && pages > 1 && ev.type == EventType.ScrollWheel && grid.Area.Contains(ev.mousePosition))
            {
                _encPage = Mathf.Clamp(_encPage + (ev.delta.y > 0 ? 1 : -1), 0, pages - 1);
                ev.Use();
            }

            var per = grid.Cols * grid.Rows;
            var blockW = grid.Cols * grid.W + (grid.Cols - 1) * EncCardGap;
            var blockH = grid.Rows * grid.H + (grid.Rows - 1) * EncCardGap;
            var ox = grid.Area.x + (grid.Area.width - blockW) / 2f;
            var oy = grid.Area.y + (grid.Area.height - blockH) / 2f;
            var clicked = -1;
            for (var slot = 0; slot < per; slot++)
            {
                var r = new Rect(ox + (slot % grid.Cols) * (grid.W + EncCardGap), oy + (slot / grid.Cols) * (grid.H + EncCardGap), grid.W, grid.H);
                var i = _encPage * per + slot;
                if (i >= entries.Count)
                {
                    // An empty pocket on the last page.
                    skin.DashedFrame(r, new Color(UiSkin.Border.r, UiSkin.Border.g, UiSkin.Border.b, 0.35f), 8f, 6f, 1f);
                    continue;
                }

                if (DrawEncCard(skin, r, entries[i], all.IndexOf(entries[i]) + 1, i == selected))
                {
                    clicked = i;
                }
            }

            // ‹ Página n de m ›
            var fy = page.yMax - 50f;
            const float labelW = 150f;
            var cx = page.center.x;
            GUI.Label(new Rect(cx - labelW / 2f, fy + 10f, labelW, 20f), GameTexts.Profile.EncPage(_encPage + 1, pages), skin.SmallMutedCenter);
            if (HeroSheet.Arrow(skin, new Rect(cx - labelW / 2f - 44f, fy, 40f, 40f), true, _encPage > 0))
            {
                _encPage--;
            }

            if (HeroSheet.Arrow(skin, new Rect(cx + labelW / 2f + 4f, fy, 40f, 40f), false, _encPage < pages - 1))
            {
                _encPage++;
            }

            return clicked;
        }

        /// <summary>
        /// One album card: thin frame in the rarity colour, rarity seal and number on top, the fish, name, map, record and
        /// catches. Not found yet: the card back with the silhouette, "?", rarity and map (where to look). Returns true on click.
        /// </summary>
        private static bool DrawEncCard(UiSkin skin, Rect r, EncyclopediaEntryView e, int number, bool selected)
        {
            var theme = VisualTheme.Current;
            var hovered = GUI.enabled && r.Contains(Event.current.mousePosition);
            var clicked = GUI.Button(r, GUIContent.none, GUIStyle.none);
            var c = hovered && !selected ? new Rect(r.x, r.y - 2f, r.width, r.height) : r;
            var rarity = UiSkin.RarityColor(e.RarityId);
            var low = e.RarityId == "common" || e.RarityId == "rare";

            // ASSET_PENDENTE: ui_enc_card_frame.png (thin card frame, 9-slice, tinted per rarity) and ui_enc_card_back.png
            // (card back) replace the drawn frame and back.
            GUI.DrawTexture(new Rect(c.x + 1f, c.y + 4f, c.width, c.height), skin.White, ScaleMode.StretchToFill, true, 0, new Color(0f, 0f, 0f, hovered ? 0.34f : 0.22f), 0, 12f);
            var fill = e.Discovered
                ? (selected ? theme.PanelElevated : Color.Lerp(theme.Panel, theme.PanelElevated, hovered ? 0.6f : 0.25f))
                : Color.Lerp(theme.Night, theme.Panel, hovered ? 0.55f : 0.3f);
            GUI.DrawTexture(c, skin.White, ScaleMode.StretchToFill, true, 0, fill, 0, 12f);
            var frameA = e.Discovered ? (low ? 0.55f : 0.9f) : 0.3f;
            if (hovered)
            {
                frameA = Mathf.Min(1f, frameA + 0.2f);
            }

            GUI.DrawTexture(c, skin.White, ScaleMode.StretchToFill, true, 0, new Color(rarity.r, rarity.g, rarity.b, frameA), 1.5f, 12f);
            var inner = new Rect(c.x + 5f, c.y + 5f, c.width - 10f, c.height - 10f);
            if (e.Discovered)
            {
                GUI.DrawTexture(inner, skin.White, ScaleMode.StretchToFill, true, 0, new Color(1f, 1f, 1f, 0.06f), 1f, 9f);
            }
            else
            {
                skin.DashedFrame(inner, new Color(rarity.r, rarity.g, rarity.b, 0.22f), 6f, 5f, 1f);
            }

            if (selected)
            {
                GUI.DrawTexture(new Rect(c.x - 3f, c.y - 3f, c.width + 6f, c.height + 6f), skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.Accent, 2f, 14f);
            }

            // Top: rarity seal on the left, the album number on the right when it fits.
            const float pad = 10f;
            var x = c.x + pad;
            var w = c.width - pad * 2f;
            var seal = (e.RarityName ?? string.Empty).ToUpperInvariant();
            var sealW = seal.Length > 0 ? Mathf.Min(skin.PillWidth(seal, false), w) : 0f;
            if (sealW > 0)
            {
                skin.RarityPill(new Rect(x, c.y + pad, sealW, 20f), e.RarityId, seal, false);
            }

            var num = GameTexts.Profile.EncNumber(number);
            var numW = skin.SmallMutedRightLine.CalcSize(new GUIContent(num)).x + 2f;
            if (sealW + 6f + numW <= w)
            {
                GUI.Label(new Rect(x + w - numW, c.y + pad, numW, UiSkin.SmallLine), num, skin.SmallMutedRightLine);
            }

            // The fish (a dark silhouette with "?" until found, Art Bible section 8).
            // Name, map, record and catches: one 22 px line and three 20 px lines of the small text (A-149).
            const float bottomH = 88f;
            var artTop = c.y + pad + 26f;
            var art = new Rect(x, artTop, w, Mathf.Max(30f, c.yMax - pad - bottomH - artTop));
            GUI.DrawTexture(art, skin.White, ScaleMode.StretchToFill, true, 0, e.Discovered ? new Color(0.06f, 0.17f, 0.26f, 0.9f) : new Color(0.03f, 0.07f, 0.12f, 0.9f), 0, 8f);
            GUI.DrawTexture(new Rect(art.x + 6f, art.y + 6f, art.width - 12f, art.height - 12f), Art.FishTexture(e.SpeciesId), ScaleMode.ScaleToFit, true, 0,
                e.Discovered ? Color.white : new Color(0.02f, 0.05f, 0.09f, 0.9f), 0, 0);
            if (!e.Discovered)
            {
                var prevContent = GUI.contentColor;
                GUI.contentColor = new Color(UiSkin.Muted.r, UiSkin.Muted.g, UiSkin.Muted.b, 0.85f);
                GUI.Label(art, GameTexts.Profile.EncUnknownValue, skin.TitleCenter);
                GUI.contentColor = prevContent;
            }

            // Name, map, record and catches.
            var ty = art.yMax + 4f;
            var name = e.Discovered ? e.Name : GameTexts.Profile.Undiscovered;
            GUI.Label(new Rect(c.x + 6f, ty, c.width - 12f, 22f), FishCard.Fit(name ?? string.Empty, skin.CenterBold, c.width - 12f), skin.CenterBold);
            GUI.Label(new Rect(x, ty + 22f, w, UiSkin.SmallLine), FishCard.Fit(e.MapName ?? string.Empty, skin.SmallMutedCenter, w), skin.SmallMutedCenter);
            if (e.Discovered)
            {
                var record = GameTexts.Profile.EncRecordShort(Format.SizeCm(e.LargestCm));
                GUI.Label(new Rect(x, ty + 42f, w, UiSkin.SmallLine), FishCard.Fit(record, SmallBoldCenter(skin), w), SmallBoldCenter(skin));
                var caught = GameTexts.Profile.EncCaughtShort(Format.Short(e.TimesCaught));
                GUI.Label(new Rect(x, ty + 62f, w, UiSkin.SmallLine), FishCard.Fit(caught, skin.SmallMutedCenter, w), skin.SmallMutedCenter);
            }
            else
            {
                GUI.Label(new Rect(x, ty + 52f, w, UiSkin.SmallLine), FishCard.Fit(GameTexts.Profile.EncNotYet, skin.SmallMutedCenter, w), skin.SmallMutedCenter);
            }

            // A record in a special size (Excepcional, Perfeição): its seal on the art and a faint gold light crossing the card.
            if (e.Discovered && e.LargestIsSpecial && !string.IsNullOrEmpty(e.LargestSizeCategoryName))
            {
                var sizeSeal = e.LargestSizeCategoryName.ToUpperInvariant();
                var sw = Mathf.Min(skin.Badge.CalcSize(new GUIContent(sizeSeal)).x + 10f, art.width - 8f);
                FishCard.ExceptionalSeal(skin, new Rect(art.xMax - 4f - sw, art.yMax - 24f, sw, 20f), sizeSeal, UiSkin.SizeColor(e.LargestSizeCategoryId));
                EncGleam(skin, c, number);
            }

            return clicked;
        }

        /// <summary>A slow, faint gold light crossing the card now and then (Art Bible 2.4: a short glow, never a permanent one).</summary>
        private static void EncGleam(UiSkin skin, Rect card, int seed)
        {
            const float period = 4.5f;
            var phase = Mathf.Repeat(Time.unscaledTime + seed * 0.37f, period) / period;
            if (phase >= 0.35f)
            {
                return;
            }

            var k = phase / 0.35f;
            var bandW = card.width * 0.22f;
            var band = new Rect(card.x + (card.width + bandW) * k - bandW, card.y + 3f, bandW, card.height - 6f);
            band.xMin = Mathf.Max(band.xMin, card.x + 3f);
            band.xMax = Mathf.Min(band.xMax, card.xMax - 3f);
            if (band.width > 1f)
            {
                var gold = UiSkin.GoldLight;
                GUI.DrawTexture(band, skin.White, ScaleMode.StretchToFill, true, 0, new Color(gold.r, gold.g, gold.b, 0.10f * Mathf.Sin(k * Mathf.PI)), 0, 6f);
            }
        }

        private static GUIStyle _smallBoldCenter;

        private static GUIStyle SmallBoldCenter(UiSkin skin)
        {
            if (_smallBoldCenter == null || _smallBoldCenter.font != skin.SmallBold.font)
            {
                _smallBoldCenter = new GUIStyle(skin.SmallBold) { alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Clip };
            }

            return _smallBoldCenter;
        }

        private void EncSelect(string speciesId)
        {
            if (_encSelected != speciesId)
            {
                _encSelected = speciesId;
                _encChangedAt = Time.unscaledTime;
            }
        }

        /// <summary>Moves to the previous or next species of the tab, wrapping around.</summary>
        private int EncStep(List<EncyclopediaEntryView> entries, int index, int delta)
        {
            var next = ((index + delta) % entries.Count + entries.Count) % entries.Count;
            EncSelect(entries[next].SpeciesId);
            return next;
        }

        /// <summary>
        /// The species sheet: the fish on its pedestal with ‹ ›, then the name, rarity, map and the numbers. Stacked in a
        /// narrow column; side by side when it covers the page. Returns the index after the arrows.
        /// </summary>
        private int DrawEncSheet(UiSkin skin, Rect rect, List<EncyclopediaEntryView> entries, int index, bool overlay)
        {
            GUI.Box(rect, GUIContent.none, skin.Card);
            var inner = new Rect(rect.x + 16f, rect.y + 14f, rect.width - 32f, rect.height - 28f);
            Rect stage, info;
            if (inner.width >= 620f)
            {
                var sw = Mathf.Floor(inner.width * 0.46f);
                stage = new Rect(inner.x, inner.y, sw, inner.height);
                info = new Rect(stage.xMax + 20f, inner.y, inner.xMax - stage.xMax - 20f, inner.height);
            }
            else
            {
                var sh = Mathf.Clamp(inner.height * 0.36f, 160f, 250f);
                stage = new Rect(inner.x, inner.y, inner.width, sh);
                info = new Rect(inner.x, stage.yMax + 10f, inner.width, inner.yMax - stage.yMax - 10f);
            }

            index = DrawEncStage(skin, stage, entries, index, overlay);
            DrawEncInfo(skin, info, entries[index]);
            return index;
        }

        /// <summary>
        /// The chosen species big on a pedestal with an aura in its rarity colour (faint for Comum and Raro, Art Bible
        /// 2.4), ‹ › on the sides and its place in the tab on top. Returns the index after the arrows.
        /// </summary>
        private int DrawEncStage(UiSkin skin, Rect rect, List<EncyclopediaEntryView> entries, int index, bool overlay)
        {
            var e = entries[index];
            var rarity = UiSkin.RarityColor(e.RarityId);

            // Water backdrop, like the Cardume formation (shared with the Aquarium's card mode, A-145).
            HeroSheet.Backdrop(skin, rect);

            var position = GameTexts.Profile.EncPosition(index + 1, entries.Count);
            if (overlay)
            {
                if (GUI.Button(new Rect(rect.x + 12f, rect.y + 12f, 110f, 34f), GameTexts.Profile.EncBack, skin.Button))
                {
                    _encOpen = false;
                }

                GUI.Label(new Rect(rect.x + 132f, rect.y + 19f, rect.width - 144f, 20f), position, skin.SmallMutedRightLine);
            }
            else
            {
                GUI.Label(new Rect(rect.x + 12f, rect.y + 10f, rect.width - 24f, 20f), position, skin.SmallMutedCenter);
            }

            var zoneTop = rect.y + (overlay ? 54f : 38f);
            const float pedestalH = 34f;
            var zoneBottom = rect.yMax - 14f - pedestalH;
            var artW = Mathf.Max(60f, rect.width - 130f);
            var artH = Mathf.Clamp(Mathf.Min(zoneBottom - zoneTop - 10f, artW * 0.6f), 40f, 300f);
            var centre = new Vector2(rect.center.x, zoneBottom - artH / 2f - 6f);

            // The aura and the pedestal (HeroSheet, shared with the Aquarium's card mode).
            var strength = HeroSheet.AuraStrength(e.RarityId) * (e.Discovered ? 1f : 0.5f);
            var aura = Mathf.Max(0f, Mathf.Min(artW + 20f, 2f * (centre.y - rect.y - 8f)));
            HeroSheet.Aura(skin, centre, aura, rarity, strength);
            HeroSheet.Pedestal(skin, rect.center.x, zoneBottom + 4f, Mathf.Min(artW * 0.8f, 340f), rarity, e.Discovered ? 0.7f : 0.3f);

            // The fish fades in when it changes and floats a little.
            var t = Mathf.Clamp01((Time.unscaledTime - _encChangedAt) / 0.25f);
            var bob = Mathf.Sin(Time.unscaledTime * 1.6f) * 3f;
            var art = new Rect(centre.x - artW / 2f, centre.y - artH / 2f + bob + (1f - t) * 10f, artW, artH);
            var tint = e.Discovered ? new Color(1f, 1f, 1f, t) : new Color(0.02f, 0.05f, 0.09f, 0.85f * t);
            GUI.DrawTexture(art, Art.FishTexture(e.SpeciesId), ScaleMode.ScaleToFit, true, 0, tint, 0, 0);
            if (!e.Discovered)
            {
                var prevContent = GUI.contentColor;
                GUI.contentColor = new Color(UiSkin.Muted.r, UiSkin.Muted.g, UiSkin.Muted.b, t);
                GUI.Label(new Rect(art.x, art.center.y - 30f, art.width, 60f), GameTexts.Profile.EncUnknownValue, skin.TitleCenter);
                GUI.contentColor = prevContent;
            }

            // ‹ › walk the current tab.
            if (entries.Count > 1)
            {
                if (HeroSheet.Arrow(skin, new Rect(rect.x + 12f, centre.y - 24f, 48f, 48f), true))
                {
                    index = EncStep(entries, index, -1);
                }

                if (HeroSheet.Arrow(skin, new Rect(rect.xMax - 60f, centre.y - 24f, 48f, 48f), false))
                {
                    index = EncStep(entries, index, 1);
                }
            }

            return index;
        }

        /// <summary>
        /// Name, rarity seal and map, then the numbers the service reports: catches, the record with its size category,
        /// the species' size range, the bite share, the discovery date and the base attributes as bars.
        /// </summary>
        private void DrawEncInfo(UiSkin skin, Rect rect, EncyclopediaEntryView e)
        {
            var x = rect.x;
            var w = rect.width;
            var y = rect.y;
            var found = e.Discovered;
            var unknown = GameTexts.Profile.EncUnknownValue;

            GUI.Label(new Rect(x, y, w, 34f), FishCard.Fit(found ? e.Name ?? string.Empty : GameTexts.Profile.Undiscovered, skin.TitleCenter, w), skin.TitleCenter);
            y += 36f;

            var seal = (e.RarityName ?? string.Empty).ToUpperInvariant();
            var sealW = seal.Length > 0 ? Mathf.Min(skin.PillWidth(seal, true), w * 0.5f) : 0f;
            var mapText = e.MapName == null ? string.Empty : found ? e.MapName : GameTexts.Profile.EncLivesIn(e.MapName);
            var mapW = Mathf.Max(0f, Mathf.Min(skin.SmallMuted.CalcSize(new GUIContent(mapText)).x + 4f, w - sealW - 10f));
            var lineX = x + (w - (sealW + 10f + mapW)) / 2f;
            if (sealW > 0)
            {
                skin.RarityPill(new Rect(lineX, y, sealW, 22f), e.RarityId, seal, true);
            }

            GUI.Label(new Rect(lineX + sealW + 10f, y + 1f, mapW, 20f), FishCard.Fit(mapText, skin.SmallMuted, mapW), skin.SmallMuted);
            y += 30f;
            if (!found && e.MapName != null)
            {
                GUI.Label(new Rect(x, y, w, 20f), FishCard.Fit(GameTexts.Profile.EncHint(e.MapName), skin.SmallMutedCenter, w), skin.SmallMutedCenter);
                y += 24f;
            }

            // Five lines and four bars share the room left, with the bars' title.
            var rowH = Mathf.Clamp((rect.yMax - y - 30f) / 9.2f, 22f, 32f);
            y = EncRow(skin, x, y, w, rowH, GameTexts.Profile.EncCaught, found ? Format.Number(e.TimesCaught) : unknown, null);
            var largest = !found ? unknown
                : e.LargestSizeCategoryName == null ? Format.SizeCm(e.LargestCm)
                : GameTexts.Profile.EncLargestLine(Format.SizeCm(e.LargestCm), e.LargestSizeCategoryName);
            y = EncRow(skin, x, y, w, rowH, GameTexts.Profile.EncLargest, largest, found && e.LargestSizeCategoryId != null ? UiSkin.SizeColor(e.LargestSizeCategoryId) : (Color?)null);
            y = EncRow(skin, x, y, w, rowH, GameTexts.Profile.EncSizeRange, found ? GameTexts.Profile.EncRange(Format.SizeCm(e.SpeciesMinCm), Format.SizeCm(e.SpeciesMaxCm)) : unknown, null);
            y = EncRow(skin, x, y, w, rowH, GameTexts.Profile.EncBite, found ? Format.Percent(e.BiteShare, e.BiteShare < 0.01 ? 1 : 0) : unknown, null);
            var date = !found ? unknown
                : e.FirstCaughtAtMs > 0 ? Format.Date(DateTimeOffset.FromUnixTimeMilliseconds(e.FirstCaughtAtMs).LocalDateTime)
                : GameTexts.Profile.None;
            y = EncRow(skin, x, y, w, rowH, GameTexts.Profile.FirstCaught, date, null);

            // Base attributes as bars on the scale of the whole catalog (the service gives both).
            y += 4f;
            var noteW = Mathf.Min(w * 0.5f, skin.SmallMutedRightLine.CalcSize(new GUIContent(GameTexts.Profile.EncStatsNote)).x + 4f);
            GUI.Label(new Rect(x, y, w - noteW - 8f, 24f), FishCard.Fit(GameTexts.Profile.EncStats, skin.BodyBold, w - noteW - 8f), skin.BodyBold);
            GUI.Label(new Rect(x + w - noteW, y + 3f, noteW, 20f), FishCard.Fit(GameTexts.Profile.EncStatsNote, skin.SmallMutedRightLine, noteW), skin.SmallMutedRightLine);
            y += 26f;

            var stats = found ? e.BaseStats : null;
            var max = _profile.EncyclopediaStatsMax;
            var rows = new[]
            {
                (GameTexts.Aquarium.Hp, stats?.Hp ?? 0d, max?.Hp ?? 0d),
                (GameTexts.Aquarium.Attack, stats?.Attack ?? 0d, max?.Attack ?? 0d),
                (GameTexts.Aquarium.Defense, stats?.Defense ?? 0d, max?.Defense ?? 0d),
                (GameTexts.Aquarium.Speed, stats?.Speed ?? 0d, max?.Speed ?? 0d),
            };
            var labelW = Mathf.Min(110f, w * 0.34f);
            const float valueW = 52f;
            var barColor = UiSkin.RarityColor(e.RarityId);
            foreach (var (label, value, scale) in rows)
            {
                if (y + 22f > rect.yMax)
                {
                    break;
                }

                var ty = y + (rowH - 22f) / 2f;
                GUI.Label(new Rect(x, ty, labelW, 22f), FishCard.Fit(label, skin.SmallMuted, labelW), skin.SmallMuted);
                skin.Bar(new Rect(x + labelW + 6f, ty + 7f, w - labelW - valueW - 12f, 8f), stats != null && scale > 0 ? (float)(value / scale) : 0f, barColor);
                GUI.Label(new Rect(x + w - valueW, ty, valueW, 22f), stats != null ? Format.Decimal(value, 0) : unknown, RightBold(skin));
                y += rowH;
            }
        }

        private static float EncRow(UiSkin skin, float x, float y, float w, float h, string label, string value, Color? color)
        {
            var ty = y + (h - 22f) / 2f - 2f;
            var lw = Mathf.Min(skin.SmallMuted.CalcSize(new GUIContent(label)).x + 12f, w * 0.55f);
            GUI.Label(new Rect(x, ty, lw, 22f), FishCard.Fit(label, skin.SmallMuted, lw), skin.SmallMuted);
            var prevContent = GUI.contentColor;
            if (color.HasValue)
            {
                GUI.contentColor = color.Value;
            }

            GUI.Label(new Rect(x + lw, ty, w - lw, 22f), FishCard.Fit(value, skin.SmallBold, w - lw), RightBold(skin));
            GUI.contentColor = prevContent;
            skin.Divider(new Rect(x, y + h - 2f, w, 1f));
            return y + h;
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
