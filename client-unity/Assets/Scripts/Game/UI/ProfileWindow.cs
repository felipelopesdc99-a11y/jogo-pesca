using System;
using System.Collections.Generic;
using System.Linq;
using FishingIdle.Game.Bootstrap;
using FishingIdle.Game.Scene;
using FishingIdle.GameService.Aquarium;
using FishingIdle.GameService.Arena;
using FishingIdle.GameService.Expeditions;
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
        private ExpeditionsView _expeditions;
        private float _nextRefresh;
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
            _tab = Tab.Summary;
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

            GUI.enabled = _pendingRod == null;

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
                (Tab.Equipment, GameTexts.Profile.TabEquipment), (Tab.Inventory, GameTexts.Profile.TabInventory),
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

        // ------------------------------------------------------------------ header and Summary (A-121)

        private void DrawHeader(UiSkin skin, Rect panel)
        {
            var player = _root.Player;
            var portrait = new Rect(panel.x + 28, panel.y + 20, 84, 84);
            GUI.DrawTexture(portrait, skin.White, ScaleMode.StretchToFill, true, 0, new Color(0.06f, 0.11f, 0.2f, 1f), 0, 18);
            var face = ArtAssets.Texture("Cena/retrato");
            if (face != null)
            {
                GUI.DrawTexture(new Rect(portrait.x + 3, portrait.y + 3, 78, 78), face, ScaleMode.ScaleAndCrop, true, 0, Color.white, 0, 15);
            }
            else
            {
                skin.DrawIcon(new Rect(portrait.x + 22, portrait.y + 22, 40, 40), Icons.Profile, UiSkin.Accent);
            }

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
            GUI.Label(new Rect(tx, panel.y + 18, tw2, 36), FishCard.Fit(_profile.PlayerName, skin.Title, tw2), skin.Title);

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
                        GUI.Label(new Rect(r.x, r.y + 34, r.width - 4, 18), GameTexts.Player.LevelShort + slot.Fish.Level, skin.SmallMutedRight);
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
            var tiles = new[]
            {
                (Format.Number(records.TotalCatches), GameTexts.Profile.StatCatches, UiSkin.Text),
                (Format.Number(records.ExceptionalCatches), GameTexts.Profile.StatExceptional, UiSkin.Gold),
                (Format.Number(records.PerfectCatches), GameTexts.Profile.StatPerfect, UiSkin.SizeColor("perfect")),
                (records.BiggestSpeciesName == null ? GameTexts.Profile.None : Format.SizeCm(records.BiggestCm), records.BiggestSpeciesName == null ? GameTexts.Profile.Biggest : GameTexts.Profile.StatBiggest(records.BiggestSpeciesName), UiSkin.Text),
                (Format.Number(records.CoinsFromSales), GameTexts.Profile.StatSales, UiSkin.Text),
            };
            var tileW = (w - 8f) / 2f;
            var tileH = Mathf.Clamp((right.yMax - 14 - y - 16f) / 3f, 54f, 70f);
            for (var i = 0; i < tiles.Length; i++)
            {
                var (value, label, color) = tiles[i];
                var r = new Rect(x + (i % 2) * (tileW + 8f), y + (i / 2) * (tileH + 8f), tileW, tileH);
                GUI.DrawTexture(r, skin.White, ScaleMode.StretchToFill, true, 0, new Color(0.06f, 0.11f, 0.2f, 0.9f), 0, 10);
                GUI.contentColor = color;
                GUI.Label(new Rect(r.x + 10, r.y + 6, r.width - 16, 28), FishCard.Fit(value, skin.Heading, r.width - 16), skin.Heading);
                GUI.contentColor = Color.white;
                GUI.Label(new Rect(r.x + 10, r.y + 34, r.width - 16, 18), FishCard.Fit(label, skin.SmallMuted, r.width - 16), skin.SmallMuted);
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
            GUI.Label(new Rect(x, y + 4, w, 20), rod.CanCatchMythic ? GameTexts.Profile.CatchesUpToMythic : rod.CanCatchLegendary ? GameTexts.Profile.CatchesUpToLegendary : rod.CanCatchEpic ? GameTexts.Profile.CatchesRareAndEpic : rod.CanCatchRare ? GameTexts.Profile.CatchesRare : GameTexts.Profile.NoRare, skin.Small);

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
                    if (GUI.Button(new Rect(actions.x, actions.y, upgradeWidth - 6, 38), (rod.NextUpgradeShells > 0
                            ? GameTexts.Shop.UpgradeForWithShells(rod.Level + 1, Format.Number(rod.NextUpgradeCost), Format.Number(rod.NextUpgradeShells))
                            : GameTexts.Shop.UpgradeFor(rod.Level + 1, Format.Number(rod.NextUpgradeCost))), skin.Button))
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

                    if (!string.IsNullOrEmpty(slot.Fish.RarityName))
                    {
                        var rarity = slot.Fish.RarityName.ToUpperInvariant();
                        skin.RarityPill(new Rect(rect.x + 10, rect.y + 6, skin.PillWidth(rarity, false), 20), slot.Fish.RarityId, rarity, false);
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
