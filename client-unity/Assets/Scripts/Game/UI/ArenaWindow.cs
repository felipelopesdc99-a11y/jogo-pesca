using System.Collections.Generic;
using System.Linq;
using FishingIdle.Game.Bootstrap;
using FishingIdle.Game.Scene;
using FishingIdle.GameService.Arena;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Profile;
using FishingIdle.Game.Visual;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The Arena (GDD sections 27–30): opponents, ranking, history, Arena Shop, and the battle replay.
    /// The battle is already resolved by the game service; this only plays the events back.
    /// </summary>
    public sealed class ArenaWindow
    {
        private enum Tab
        {
            Opponents,
            Ranking,
            History,
            Shop,
        }

        private readonly GameRoot _root;
        private Tab _tab;
        private ArenaView _arena;
        private float _nextRefresh;
        private Vector2 _scroll;
        private int _rankPage;

        // The opponent whose profile is open (M18-T02), or null.
        private OpponentView _viewing;

        // The player's side of the duel: name and avatar (read once per opening) and the Cardume.
        private ProfileView _profile;
        private CardumeView _cardume;

        // The opponent picked on the pedestals; back to the first one whenever the set changes.
        private int _selected;
        private string _opponentsKey;
        private readonly SlotFish[] _mine = new SlotFish[6];
        private readonly SlotFish[] _theirs = new SlotFish[6];

        /// <summary>One place of a formation as the duel shows it: never the fish's Strength.</summary>
        private struct SlotFish
        {
            public string SpeciesId;
            public string Name;
            public int Level;
            public string RarityId;
            public string RarityName;
        }

        // VS intro before the replay (presentation only).
        private const float IntroSeconds = 1.2f;
        private float _introStart;
        private float _introUntil;
        private int _introOpponentRank;

        // Replay state.
        private BattleReport _battle;
        private float _time;
        private float _speed = 1f;
        private int _applied;
        private double[][] _hp;
        private double[][] _maxHp;
        private readonly List<(float at, int side, int pos, double dmg)> _hits = new List<(float, int, int, double)>();
        private readonly Dictionary<(int side, int pos), float> _lunges = new Dictionary<(int, int), float>();

        public ArenaWindow(GameRoot root)
        {
            _root = root;
        }

        public bool IsOpen { get; private set; }

        /// <summary>True while the opponent's profile dialog is open over the window.</summary>
        public bool HasDialog => IsOpen && _viewing != null;

        public void Open()
        {
            IsOpen = true;
            _nextRefresh = 0f;
            _profile = null;
        }

        /// <summary>True while the VS screen plays before the replay.</summary>
        private bool IntroPlaying => _battle != null && Time.unscaledTime < _introUntil;

        private string PlayerName => _profile != null && !string.IsNullOrEmpty(_profile.PlayerName) ? _profile.PlayerName : GameTexts.Arena.You;

        public void Close()
        {
            if (_viewing != null)
            {
                _viewing = null;
                return;
            }

            if (IntroPlaying)
            {
                // Escape on the VS screen skips to the replay.
                _introUntil = 0f;
                return;
            }

            if (_battle != null)
            {
                // Leaving the replay jumps to the result; the battle is already decided.
                _battle = null;
                _nextRefresh = 0f;
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

            if (_battle != null)
            {
                if (IntroPlaying)
                {
                    DrawIntro(skin, screenWidth, screenHeight);
                }
                else
                {
                    DrawReplay(skin, screenWidth, screenHeight);
                }

                return;
            }

            if (Time.unscaledTime >= _nextRefresh)
            {
                _arena = _root.GetArena();
                _cardume = _root.GetCardume();
                if (_profile == null)
                {
                    _profile = _root.GetProfile();
                }

                _nextRefresh = Time.unscaledTime + 0.5f;
            }

            if (_arena == null)
            {
                return;
            }

            // While an opponent's profile is open, the window under it is shown but inert.
            var wasEnabled = GUI.enabled;
            GUI.enabled = wasEnabled && _viewing == null;
            var area = WindowFrame.Draw(skin, screenWidth, screenHeight, GameTexts.Arena.Title, GameTexts.Arena.Note, out var closed, icon: Icons.Arena);
            if (closed)
            {
                Close();
                return;
            }

            // Summary: the player, rank, Energy, Honor.
            DrawHeader(skin, new Rect(area.x, area.y, area.width, 64));

            var tabs = new[] { (Tab.Opponents, GameTexts.Arena.TabOpponents), (Tab.Ranking, GameTexts.Arena.TabRanking), (Tab.History, GameTexts.Arena.TabHistory), (Tab.Shop, GameTexts.Arena.TabShop) };
            var x = area.x;
            foreach (var (tab, label) in tabs)
            {
                var w = skin.Chip.CalcSize(new GUIContent(label)).x + 12;
                if (GUI.Button(new Rect(x, area.y + 76, w, 32), label, _tab == tab ? skin.ChipActive : skin.Chip))
                {
                    _tab = tab;
                    _scroll = Vector2.zero;
                    _rankPage = 0;
                }

                x += w + 8;
            }

            var content = new Rect(area.x, area.y + 120, area.width, area.height - 120);
            switch (_tab)
            {
                case Tab.Ranking: DrawRanking(skin, content); break;
                case Tab.History: DrawHistory(skin, content); break;
                case Tab.Shop: DrawShop(skin, content); break;
                default: DrawOpponents(skin, content); break;
            }

            GUI.enabled = wasEnabled;
            if (_viewing != null)
            {
                DrawOpponentProfile(skin, screenWidth, screenHeight);
            }
        }

        // ------------------------------------------------------------------ header: player, rank, Energy, Honor

        private void DrawHeader(UiSkin skin, Rect r)
        {
            var playerW = r.width * 0.28f;
            var rankW = r.width * 0.2f;
            var energyW = r.width * 0.3f;

            // The player: avatar and name.
            var avatar = new Rect(r.x, r.y + 4, 56, 56);
            AvatarFrame(skin, avatar, _profile?.AvatarId, UiSkin.Accent);
            var nameX = avatar.xMax + 12;
            var nameW = Mathf.Max(20f, playerW - (nameX - r.x) - 12);
            GUI.Label(new Rect(nameX, r.y + 8, nameW, 18), GameTexts.Arena.You, skin.SmallMuted);
            GUI.Label(new Rect(nameX, r.y + 26, nameW, 28), FishCard.Fit(PlayerName, skin.Heading, nameW), skin.Heading);

            // The rank as a gold medal, "de N" beside it.
            var rx = r.x + playerW;
            var medal = new Rect(rx, r.y + 4, 56, 56);
            DrawMedal(skin, medal, GameTexts.Arena.RankOf(_arena.Rank));
            var lx = medal.xMax + 10;
            var lw = Mathf.Max(20f, rankW - (lx - rx) - 12);
            GUI.Label(new Rect(lx, r.y + 12, lw, 18), GameTexts.Arena.Rank, skin.SmallMuted);
            GUI.Label(new Rect(lx, r.y + 30, lw, 22), FishCard.Fit(GameTexts.Arena.OfTotal(_arena.Participants), skin.BodyBold, lw), skin.BodyBold);

            // Energy: label and amount, one segment per point, and the time to the next point.
            var ex = rx + rankW;
            var ew = energyW - 24;
            skin.DrawIcon(new Rect(ex, r.y + 6, 22, 22), Icons.Energy, UiSkin.Gold);
            var labelW = skin.SmallMuted.CalcSize(new GUIContent(GameTexts.Arena.Energy)).x + 8;
            GUI.Label(new Rect(ex + 28, r.y + 8, labelW, 20), GameTexts.Arena.Energy, skin.SmallMuted);
            var amountX = ex + 28 + labelW;
            var amountW = Mathf.Max(20f, ex + ew - amountX);
            GUI.Label(new Rect(amountX, r.y + 6, amountW, 22), FishCard.Fit(GameTexts.Arena.EnergyOf(_arena.Energy, _arena.EnergyMax), skin.BodyBold, amountW), skin.BodyBold);
            EnergyBar(skin, new Rect(ex, r.y + 34, ew, 10), _arena.Energy, _arena.EnergyMax);
            if (_arena.NextEnergySeconds > 0)
            {
                GUI.Label(new Rect(ex, r.y + 46, ew, 18), FishCard.Fit(GameTexts.Arena.NextEnergy(Format.Countdown(_arena.NextEnergySeconds)), skin.SmallMuted, ew), skin.SmallMuted);
            }

            // Honor, big and in gold, against the right edge.
            var honor = Format.Number(_arena.Honor);
            var numberW = skin.NumberBig.CalcSize(new GUIContent(honor)).x + 4;
            var honorLabelW = skin.SmallMuted.CalcSize(new GUIContent(GameTexts.Arena.Honor)).x + 4;
            var hx = Mathf.Max(ex + energyW, r.xMax - 44 - Mathf.Max(numberW, honorLabelW));
            var hw = Mathf.Max(20f, r.xMax - hx - 44);
            skin.DrawIcon(new Rect(hx, r.y + 14, 34, 34), Icons.Honor, UiSkin.Gold);
            GUI.Label(new Rect(hx + 44, r.y + 4, hw, 18), GameTexts.Arena.Honor, skin.SmallMuted);
            GUI.Label(new Rect(hx + 44, r.y + 20, hw, 40), FishCard.Fit(honor, skin.NumberBig, hw), skin.NumberBig);
        }

        /// <summary>An avatar in a dark rounded frame with a coloured ring.</summary>
        private static void AvatarFrame(UiSkin skin, Rect rect, string avatarId, Color ring)
        {
            var radius = rect.width * 0.22f;
            GUI.DrawTexture(rect, skin.White, ScaleMode.StretchToFill, true, 0, new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.92f), 0, radius);
            AvatarArt.Draw(skin, new Rect(rect.x + 3, rect.y + 3, rect.width - 6, rect.height - 6), avatarId, Mathf.Max(0f, radius - 3f));
            GUI.DrawTexture(rect, skin.White, ScaleMode.StretchToFill, true, 0, ring, 2f, radius);
        }

        /// <summary>A round gold medal with the position written on it ("#198").</summary>
        private static void DrawMedal(UiSkin skin, Rect rect, string text)
        {
            var radius = rect.width / 2f;
            GUI.DrawTexture(rect, skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.Gold, 0, radius);
            var inner = new Rect(rect.x + 4, rect.y + 4, rect.width - 8, rect.height - 8);
            GUI.DrawTexture(inner, skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.GoldLight, 1.5f, radius - 4f);
            GUI.Label(rect, FishCard.Fit(text, skin.MedalText, rect.width - 8), skin.MedalText);
        }

        /// <summary>One segment per Energy point; a plain bar when there are too many points to tell apart.</summary>
        private static void EnergyBar(UiSkin skin, Rect rect, int energy, int max)
        {
            if (max <= 0)
            {
                return;
            }

            const float gap = 2f;
            var segW = (rect.width - gap * (max - 1)) / max;
            if (segW < 3f)
            {
                skin.Bar(rect, energy / (float)max, false);
                return;
            }

            for (var i = 0; i < max; i++)
            {
                var seg = new Rect(rect.x + i * (segW + gap), rect.y, segW, rect.height);
                var color = i < energy ? UiSkin.Accent : new Color(1f, 1f, 1f, 0.12f);
                GUI.DrawTexture(seg, skin.White, ScaleMode.StretchToFill, true, 0, color, 0, Mathf.Min(3f, segW / 2f));
            }
        }

        // ------------------------------------------------------------------ opponents: the duel

        private void DrawOpponents(UiSkin skin, Rect area)
        {
            var opponents = _arena.Opponents;
            var count = opponents.Count;

            // A new set of opponents (attack, reroll) starts again on the first one.
            var key = string.Join("|", opponents.Select(o => o.ParticipantId));
            if (key != _opponentsKey)
            {
                _opponentsKey = key;
                _selected = 0;
            }

            _selected = Mathf.Clamp(_selected, 0, Mathf.Max(0, count - 1));
            var selected = count > 0 ? opponents[_selected] : null;

            const float buttonH = 46f;
            const float messageH = 22f;
            var pedestalH = Mathf.Clamp(area.height * 0.17f, 60f, 96f);
            var bottomY = area.yMax - buttonH;
            var messageY = bottomY - messageH - 4f;

            // Top: the three opponents as pedestals, rising from left to right.
            if (count > 0)
            {
                DrawPedestals(skin, new Rect(area.x, area.y, area.width, pedestalH), opponents);
            }

            // Middle: the two formations facing each other.
            FillMine();
            FillTheirs(selected);
            var bandTop = area.y + pedestalH + 10f;
            var band = messageY - 6f - bandTop;
            var headerH = band < 260f ? 34f : 42f;
            const float rowGap = 8f;
            var vsW = Mathf.Clamp(area.width * 0.08f, 70f, 120f);
            var panelW = (area.width - vsW) / 2f;
            var slotH = Mathf.Clamp((band - headerH - 8f - 10f - rowGap) / 2f, 24f, 150f);
            var widthBased = (panelW - 2 * FormationPad - TagW - 6f - BackShift - 2 * SlotGap) / 3f;
            var slotW = Mathf.Max(24f, Mathf.Min(widthBased, slotH * 1.9f));
            var panelH = headerH + 8f + slotH * 2f + rowGap + 10f;
            var panelY = bandTop + Mathf.Max(0f, (band - panelH) / 2f);
            var left = new Rect(area.x, panelY, panelW, panelH);
            var right = new Rect(area.xMax - panelW, panelY, panelW, panelH);

            DrawFormation(skin, left, _mine, false, headerH, slotW, slotH, rowGap);
            FormationHeader(skin, left, headerH, _profile?.AvatarId, PlayerName, _arena.Rank, null);
            DrawFormation(skin, right, _theirs, true, headerH, slotW, slotH, rowGap);
            if (selected != null)
            {
                FormationHeader(skin, right, headerH, null, selected.Name, selected.Rank, selected);
            }

            DrawVsColumn(skin, new Rect(left.xMax, panelY, right.x - left.xMax, panelH));

            // Bottom: why attacking is refused (if it is), the attack, the reroll and the no-Strength note.
            if (_arena.AttackBlocker != ServiceError.None)
            {
                GUI.Label(new Rect(area.x, messageY, area.width, messageH), GameTexts.ServiceErrorMessage(_arena.AttackBlocker.ToString()), skin.SmallGoldCenter);
            }

            var attackW = Mathf.Min(380f, area.width * 0.34f);
            var attack = new Rect(area.center.x - attackW / 2f, bottomY, attackW, buttonH);
            var canAttack = _arena.AttackBlocker == ServiceError.None && selected != null;
            if (canAttack && GUI.enabled)
            {
                skin.DrawGlow(attack, UiSkin.Accent, 0.3f);
            }

            var enabledBefore = GUI.enabled;
            GUI.enabled = enabledBefore && canAttack;
            if (skin.IconButton(attack, Icons.Attack, GameTexts.Arena.AttackCost(_arena.EnergyCost), skin.ButtonPrimary))
            {
                StartReplay(_root.Attack(_selected), selected.Rank);
            }

            GUI.enabled = enabledBefore;

            var sideW = Mathf.Max(60f, (area.width - attackW) / 2f - 16f);
            if (_arena.RerollsLeft > 0)
            {
                if (skin.IconButton(new Rect(area.x, bottomY, Mathf.Min(300f, sideW), buttonH), Icons.Swap, GameTexts.Arena.RerollsLeft(_arena.RerollsLeft), skin.Button))
                {
                    _root.RerollOpponents();
                    _nextRefresh = 0f;
                }
            }
            else
            {
                GUI.Label(new Rect(area.x, bottomY + 4, sideW, buttonH - 4), GameTexts.Arena.RerollUsed, skin.SmallMuted);
            }

            GUI.Label(new Rect(area.xMax - sideW, bottomY + 4, sideW, buttonH - 4), GameTexts.Arena.NoForceNote, skin.SmallMutedRight);
        }

        /// <summary>The opponents as selector chips, each a little taller than the previous one (the service orders them).</summary>
        private void DrawPedestals(UiSkin skin, Rect row, List<OpponentView> opponents)
        {
            const float gap = 14f;
            var count = opponents.Count;
            var chipW = Mathf.Min(320f, (row.width - gap * (count - 1)) / count);
            var x0 = row.x + (row.width - (chipW * count + gap * (count - 1))) / 2f;
            var step = row.height * 0.12f;
            for (var i = 0; i < count; i++)
            {
                var o = opponents[i];
                var h = row.height - (count - 1 - i) * step;
                var rect = new Rect(x0 + i * (chipW + gap), row.yMax - h, chipW, h);
                var isSelected = i == _selected;
                var hovered = GUI.enabled && rect.Contains(Event.current.mousePosition);
                if (isSelected)
                {
                    skin.DrawGlow(rect, UiSkin.Accent, 0.35f);
                }

                if (GUI.Button(rect, GUIContent.none, isSelected ? skin.CardSelected : hovered ? skin.CardHovered : skin.Card))
                {
                    _selected = i;
                }

                // The pedestal's top edge: accent on the chosen one, neutral on the others.
                var edge = isSelected ? UiSkin.Accent : new Color(UiSkin.Border.r, UiSkin.Border.g, UiSkin.Border.b, 0.8f);
                GUI.DrawTexture(new Rect(rect.x + 12, rect.yMax - 7, rect.width - 24, 3), skin.White, ScaleMode.StretchToFill, true, 0, edge, 0, 1.5f);

                var a = Mathf.Clamp(rect.height - 20f, 20f, 56f);
                var avatar = new Rect(rect.x + 12, rect.y + (rect.height - a) / 2f - 2f, a, a);
                AvatarFrame(skin, avatar, null, isSelected ? UiSkin.Accent : UiSkin.Border);
                var tx = avatar.xMax + 12;
                var tw = Mathf.Max(20f, rect.xMax - 12 - tx);
                var ty = rect.y + rect.height / 2f - 23f;
                GUI.Label(new Rect(tx, ty, tw, 22), FishCard.Fit(o.Name, skin.BodyBold, tw), skin.BodyBold);
                GUI.Label(new Rect(tx, ty + 22, tw, 20), GameTexts.Arena.RankOf(o.Rank), skin.SmallGold);
            }
        }

        private void FillMine()
        {
            for (var i = 0; i < 6; i++)
            {
                _mine[i] = default;
            }

            if (_cardume == null)
            {
                return;
            }

            foreach (var slot in _cardume.Slots)
            {
                if (slot.Fish != null && slot.Position >= 1 && slot.Position <= 6)
                {
                    _mine[slot.Position - 1] = new SlotFish
                    {
                        SpeciesId = slot.Fish.SpeciesId,
                        Name = slot.Fish.SpeciesName,
                        Level = slot.Fish.Level,
                        RarityId = slot.Fish.RarityId,
                        RarityName = slot.Fish.RarityName,
                    };
                }
            }
        }

        private void FillTheirs(OpponentView opponent)
        {
            for (var i = 0; i < 6; i++)
            {
                _theirs[i] = default;
            }

            if (opponent == null)
            {
                return;
            }

            foreach (var f in opponent.Fish)
            {
                if (f.Position >= 1 && f.Position <= 6)
                {
                    _theirs[f.Position - 1] = new SlotFish
                    {
                        SpeciesId = f.SpeciesId,
                        Name = f.SpeciesName,
                        Level = f.Level,
                        RarityId = f.RarityId,
                        RarityName = f.RarityName,
                    };
                }
            }
        }

        private const float FormationPad = 12f;
        private const float TagW = 22f;
        private const float BackShift = 18f;
        private const float SlotGap = 8f;

        /// <summary>
        /// A formation as a battle line (the A-129 look of the Profile's Cardume): a panel like water, the
        /// front row on top and the back row under it, slightly shifted. Mirrored for the opponent: the row
        /// tags on the right, position 1 next to them, and the fish facing left.
        /// </summary>
        private static void DrawFormation(UiSkin skin, Rect panel, SlotFish[] fish, bool mirrored, float headerH, float slotW, float slotH, float rowGap)
        {
            GUI.DrawTexture(panel, skin.White, ScaleMode.StretchToFill, true, 0, new Color(0.05f, 0.16f, 0.24f, 0.92f), 0, 16);
            GUI.DrawTexture(new Rect(panel.x, panel.y + panel.height * 0.45f, panel.width, panel.height * 0.55f), skin.White, ScaleMode.StretchToFill, true, 0, new Color(0.03f, 0.10f, 0.17f, 0.55f), 0, 16);
            GUI.DrawTexture(panel, skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.Border, 1.5f, 16);
            skin.Divider(new Rect(panel.x + FormationPad, panel.y + headerH, panel.width - FormationPad * 2, 1));

            var rowW = TagW + 6f + 3f * slotW + 2f * SlotGap + BackShift;
            var x0 = panel.x + (panel.width - rowW) / 2f;
            var y = panel.y + headerH + 8f;
            for (var row = 0; row < 2; row++)
            {
                var offset = row == 1 ? BackShift : 0f;
                var start = mirrored ? x0 + (BackShift - offset) : x0 + offset;
                var tagX = mirrored ? start + 3f * slotW + 2f * SlotGap + 6f : start;
                var slotsX = mirrored ? start : start + TagW + 6f;
                RowTag(skin, new Rect(tagX, y, TagW, slotH), row == 0 ? GameTexts.Cardume.FrontTag : GameTexts.Cardume.BackTag);
                for (var k = 0; k < 3; k++)
                {
                    // Position 1 (and 4) sits next to the row tag on both sides.
                    var index = row * 3 + (mirrored ? 2 - k : k);
                    var rect = new Rect(slotsX + k * (slotW + SlotGap), y, slotW, slotH);
                    DrawSlot(skin, rect, index + 1, fish[index], mirrored);
                }

                y += slotH + rowGap;
            }
        }

        /// <summary>A vertical "FRENTE" / "TRÁS" tag; letters get smaller when the row is short.</summary>
        private static void RowTag(UiSkin skin, Rect rect, string tag)
        {
            GUI.DrawTexture(rect, skin.White, ScaleMode.StretchToFill, true, 0, new Color(UiSkin.Accent.r, UiSkin.Accent.g, UiSkin.Accent.b, 0.14f), 0, 8);
            var letters = tag.ToCharArray();
            var spacing = Mathf.Min(17f, (rect.height - 6f) / Mathf.Max(1, letters.Length));
            var style = spacing < 14f ? skin.TinyMutedCenter : skin.SmallMutedCenter;
            var ly = rect.center.y - letters.Length * spacing / 2f;
            foreach (var ch in letters)
            {
                GUI.Label(new Rect(rect.x, ly, rect.width, spacing), ch.ToString(), style);
                ly += spacing;
            }
        }

        /// <summary>One place: number, rarity seal, fish art and level. Never the Strength (GDD section 28).</summary>
        private static void DrawSlot(UiSkin skin, Rect rect, int position, SlotFish fish, bool flipped)
        {
            var badgeSize = Mathf.Clamp(rect.height * 0.28f, 16f, 22f);
            var badge = new Rect(rect.x + 6, rect.y + 6, badgeSize, badgeSize);
            if (fish.SpeciesId == null)
            {
                // An empty place: a soft frame and the word.
                GUI.DrawTexture(rect, skin.White, ScaleMode.StretchToFill, true, 0, new Color(1f, 1f, 1f, 0.03f), 0, 10);
                GUI.DrawTexture(new Rect(rect.x + 3, rect.y + 3, rect.width - 6, rect.height - 6), skin.White, ScaleMode.StretchToFill, true, 0, new Color(UiSkin.Accent.r, UiSkin.Accent.g, UiSkin.Accent.b, 0.22f), 1.5f, 10);
                PositionBadge(skin, badge, position, false);
                GUI.Label(new Rect(rect.x, rect.center.y - 10, rect.width, 20), GameTexts.Cardume.Empty, skin.SmallMutedCenter);
                return;
            }

            var accent = UiSkin.RarityColor(fish.RarityId);
            GUI.Box(rect, GUIContent.none, skin.Card);
            skin.DrawOutline(rect, new Color(accent.r, accent.g, accent.b, fish.RarityId == "common" ? 0.45f : 0.85f));
            PositionBadge(skin, badge, position, true);

            if (!string.IsNullOrEmpty(fish.RarityName))
            {
                var rarity = fish.RarityName.ToUpperInvariant();
                var pw = Mathf.Min(skin.PillWidth(rarity, false), rect.width - badgeSize - 18);
                var pill = new Rect(rect.xMax - 6 - pw, rect.y + 6, pw, badgeSize);
                skin.RarityPill(pill, fish.RarityId, rarity, false);
            }

            var level = GameTexts.Player.LevelShort + " " + fish.Level;
            var artTop = badge.yMax + 4;
            if (rect.height < 100f)
            {
                // Short places: the fish fills the place and the level sits in a small tag in the corner.
                var artH = rect.yMax - 4 - artTop;
                if (artH > 8f)
                {
                    DrawFish(new Rect(rect.x + 8, artTop, rect.width - 16, artH), fish.SpeciesId, flipped);
                }

                var lw = Mathf.Min(rect.width - 12, skin.SmallBold.CalcSize(new GUIContent(level)).x + 10);
                var tag = new Rect(rect.xMax - 6 - lw, rect.yMax - 22, lw, 18);
                GUI.DrawTexture(tag, skin.White, ScaleMode.StretchToFill, true, 0, new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.8f), 0, 9);
                GUI.Label(tag, FishCard.Fit(level, skin.ChipText, lw - 6), skin.ChipText);
                return;
            }

            var line = fish.Name + " · " + level;
            GUI.Label(new Rect(rect.x + 8, rect.yMax - 26, rect.width - 16, 20), FishCard.Fit(line, skin.ChipText, rect.width - 16), skin.ChipText);
            var artBottom = rect.yMax - 28;
            if (artBottom - artTop > 8f)
            {
                DrawFish(new Rect(rect.x + 8, artTop, rect.width - 16, artBottom - artTop), fish.SpeciesId, flipped);
            }
        }

        private static void PositionBadge(UiSkin skin, Rect badge, int position, bool filled)
        {
            var radius = badge.width / 2f;
            GUI.DrawTexture(badge, skin.White, ScaleMode.StretchToFill, true, 0, new Color(0.06f, 0.12f, 0.2f, filled ? 0.95f : 0.6f), 0, radius);
            GUI.DrawTexture(badge, skin.White, ScaleMode.StretchToFill, true, 0, new Color(UiSkin.Accent.r, UiSkin.Accent.g, UiSkin.Accent.b, filled ? 1f : 0.5f), 1.5f, radius);
            GUI.Label(badge, position.ToString(), skin.PillText);
        }

        /// <summary>A formation's header: avatar, name and rank; "Ver perfil" on the opponent's side.</summary>
        private void FormationHeader(UiSkin skin, Rect panel, float headerH, string avatarId, string name, int rank, OpponentView opponent)
        {
            var size = headerH - 12f;
            var avatar = new Rect(panel.x + FormationPad, panel.y + 6, size, size);
            AvatarFrame(skin, avatar, avatarId, opponent == null ? UiSkin.Accent : UiSkin.Danger);

            var right = panel.xMax - FormationPad;
            if (opponent != null)
            {
                var bw = Mathf.Min(120f, panel.width * 0.3f);
                if (GUI.Button(new Rect(right - bw, panel.y + 6, bw, size), FishCard.Fit(GameTexts.Arena.ViewProfile, skin.Chip, bw - 16), skin.Chip))
                {
                    _viewing = opponent;
                }

                right -= bw + 10;
            }

            var rankText = GameTexts.Arena.RankOf(rank);
            var rw = skin.SmallGold.CalcSize(new GUIContent(rankText)).x + 4;
            var nx = avatar.xMax + 10;
            var nw = Mathf.Max(20f, right - nx - rw - 10);
            var ty = panel.y + (headerH - 22f) / 2f;
            GUI.Label(new Rect(nx, ty, nw, 22), FishCard.Fit(name, skin.BodyBold, nw), skin.BodyBold);
            GUI.Label(new Rect(right - rw, ty + 2, rw, 20), rankText, skin.SmallGoldRight);
        }

        /// <summary>The gold "VS" between the formations.</summary>
        private static void DrawVsColumn(UiSkin skin, Rect column)
        {
            var cx = column.center.x;
            var line = new Color(UiSkin.Gold.r, UiSkin.Gold.g, UiSkin.Gold.b, 0.25f);
            GUI.DrawTexture(new Rect(cx - 1, column.y + 12, 2, column.height - 24), skin.White, ScaleMode.StretchToFill, true, 0, line, 0, 1);

            var d = Mathf.Min(column.width - 8f, 84f);
            var disc = new Rect(cx - d / 2f, column.center.y - d / 2f, d, d);
            skin.DrawGlow(disc, UiSkin.Gold, 0.35f);
            GUI.DrawTexture(disc, skin.White, ScaleMode.StretchToFill, true, 0, new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.96f), 0, d / 2f);
            GUI.DrawTexture(disc, skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.Gold, 2f, d / 2f);
            GUI.Label(disc, GameTexts.Arena.VsMark, skin.VsMark);
        }

        /// <summary>
        /// A fish's art; flipped = mirrored around its centre in the virtual canvas, composed with the HUD's
        /// GUI.matrix (not in screen pixels).
        /// </summary>
        private static void DrawFish(Rect art, string speciesId, bool flipped)
        {
            if (!flipped)
            {
                GUI.DrawTexture(art, Art.FishTexture(speciesId), ScaleMode.ScaleToFit, true);
                return;
            }

            var matrix = GUI.matrix;
            var pivot = new Vector3(art.center.x, art.center.y, 0f);
            GUI.matrix = matrix * Matrix4x4.TRS(pivot, Quaternion.identity, new Vector3(-1f, 1f, 1f)) * Matrix4x4.TRS(-pivot, Quaternion.identity, Vector3.one);
            GUI.DrawTexture(art, Art.FishTexture(speciesId), ScaleMode.ScaleToFit, true);
            GUI.matrix = matrix;
        }

        // ------------------------------------------------------------------ ranking (A-112): podium + pages up to the top 100

        private static readonly Color Silver = new Color(0.78f, 0.82f, 0.88f);
        private static readonly Color Bronze = new Color(0.80f, 0.52f, 0.30f);

        private static Color MedalColor(int rank) => rank == 1 ? UiSkin.Gold : rank == 2 ? Silver : Bronze;

        private void DrawRanking(UiSkin skin, Rect area)
        {
            var rows = _arena.Ranking;
            if (rows.Count == 0)
            {
                GUI.Label(new Rect(area.x, area.y, area.width, 22), GameTexts.Arena.RankingEmpty, skin.SmallMuted);
                return;
            }

            var size = Mathf.Max(1, _arena.RankingPageSize);
            var pages = (rows.Count + size - 1) / size;
            _rankPage = Mathf.Clamp(_rankPage, 0, pages - 1);

            // Pager at the bottom.
            var pager = new Rect(area.x, area.yMax - 44, area.width, 40);
            var listBottom = pager.y - 8;

            var y = area.y;
            if (_rankPage == 0)
            {
                var podiumHeight = Mathf.Min(230f, (listBottom - area.y) * 0.5f);
                DrawPodium(skin, new Rect(area.x, y, area.width, podiumHeight), rows);
                y += podiumHeight + 10;
            }

            // The page's rows (on the first page, the podium already shows 1 to 3).
            var first = _rankPage * size;
            var last = Mathf.Min(rows.Count, first + size);
            if (_rankPage == 0)
            {
                first = Mathf.Min(3, rows.Count);
            }

            var count = last - first;
            var rowHeight = count > 0 ? Mathf.Min(38f, (listBottom - y) / count) : 38f;
            for (var i = first; i < last; i++)
            {
                var r = rows[i];
                var rect = new Rect(area.x, y, area.width, rowHeight - 4);
                GUI.Box(rect, GUIContent.none, r.IsPlayer ? skin.CardSelected : skin.Card);
                var ty = rect.y + (rect.height - 20) / 2f;
                GUI.Label(new Rect(rect.x + 16, ty, 90, 20), GameTexts.Arena.RankOf(r.Rank), skin.SmallGold);
                GUI.Label(new Rect(rect.x + 110, ty, rect.width - 130, 20), r.IsPlayer ? r.Name + " (" + GameTexts.Arena.You + ")" : r.Name, r.IsPlayer ? skin.BodyBold : skin.Small);
                y += rowHeight;
            }

            // Previous / page / next, and a jump to the player's page.
            if (_rankPage > 0 && GUI.Button(new Rect(pager.x, pager.y, 150, 40), "‹  " + GameTexts.Arena.Previous, skin.Button))
            {
                _rankPage--;
            }

            GUI.Label(new Rect(pager.x + 160, pager.y + 10, 160, 22), GameTexts.Arena.PageOf(_rankPage + 1, pages), skin.BodyBold);
            if (_rankPage < pages - 1 && GUI.Button(new Rect(pager.x + 330, pager.y, 150, 40), GameTexts.Arena.Next + "  ›", skin.Button))
            {
                _rankPage++;
            }

            var mine = rows.FindIndex(r => r.IsPlayer);
            if (mine >= 0)
            {
                if (mine / size != _rankPage && skin.IconButton(new Rect(pager.xMax - 220, pager.y, 220, 40), Icons.Arena, GameTexts.Arena.MyPosition, skin.Button))
                {
                    _rankPage = mine / size;
                }
            }
            else
            {
                GUI.Label(new Rect(pager.xMax - 360, pager.y + 10, 360, 22), GameTexts.Arena.OutsideTop(_arena.Rank, rows.Count), skin.SmallGoldRight);
            }
        }

        /// <summary>Second, first and third side by side, the first one taller (A-112).</summary>
        private static void DrawPodium(UiSkin skin, Rect area, List<RankingEntryView> rows)
        {
            // ASSET_PENDENTE: podium and medal art (docs/ASSETS_PENDENTES.md); clean blocks meanwhile.
            var gap = 16f;
            var w = Mathf.Min(260f, (area.width - gap * 2) / 3f);
            var x0 = area.x + (area.width - (w * 3 + gap * 2)) / 2f;
            var order = new[] { 2, 1, 3 };
            for (var k = 0; k < 3; k++)
            {
                var rank = order[k];
                if (rank > rows.Count)
                {
                    continue;
                }

                var entry = rows[rank - 1];
                var color = MedalColor(rank);
                var blockHeight = area.height * (rank == 1 ? 0.42f : rank == 2 ? 0.32f : 0.25f);
                var x = x0 + k * (w + gap);
                var block = new Rect(x, area.yMax - blockHeight, w, blockHeight);

                var pedestal = ArtAssets.Texture("Arena/podio_" + rank);
                if (pedestal != null)
                {
                    GUI.DrawTexture(block, pedestal, ScaleMode.StretchToFill, true);
                }
                else
                {
                    GUI.Box(block, GUIContent.none, entry.IsPlayer ? skin.CardSelected : skin.Card);
                    var previous = GUI.color;
                    GUI.color = previous * color;
                    GUI.DrawTexture(new Rect(block.x, block.y, block.width, 4), Texture2D.whiteTexture);
                    GUI.color = previous;
                }

                GUI.contentColor = color;
                GUI.Label(new Rect(block.x, block.y + 8, block.width, 26), GameTexts.Arena.Place(rank), skin.CenterBold);
                GUI.contentColor = Color.white;

                // Above the pedestal: medal, best fish, name.
                var top = area.y;
                var medal = ArtAssets.Icon(rank == 1 ? "medalha_ouro" : rank == 2 ? "medalha_prata" : "medalha_bronze");
                var nameY = block.y - 46;
                if (entry.LeadSpeciesId != null)
                {
                    var fishHeight = Mathf.Max(24f, nameY - top - 34);
                    GUI.DrawTexture(new Rect(x + 16, top + 30, w - 32, fishHeight), Art.FishTexture(entry.LeadSpeciesId), ScaleMode.ScaleToFit, true);
                }

                if (medal != null)
                {
                    GUI.DrawTexture(new Rect(x + w / 2f - 14, top, 28, 28), medal, ScaleMode.ScaleToFit, true);
                }
                else
                {
                    skin.DrawIcon(new Rect(x + w / 2f - 12, top + 2, 24, 24), Icons.Honor, color);
                }

                GUI.Label(new Rect(x, nameY, w, 24), entry.IsPlayer ? entry.Name + " (" + GameTexts.Arena.You + ")" : entry.Name, skin.CenterBold);
                if (entry.LeadSpeciesName != null)
                {
                    GUI.Label(new Rect(x, nameY + 22, w, 20), GameTexts.Arena.PodiumLead(entry.LeadSpeciesName, entry.LeadLevel), skin.SmallMutedCenter);
                }
            }
        }

        // ------------------------------------------------------------------ opponent profile (M18-T02)

        /// <summary>
        /// The opponent's public profile: name, position and the Cardume in formation, each fish as a card.
        /// Never its Strength nor a predicted result (GDD section 28).
        /// </summary>
        private void DrawOpponentProfile(UiSkin skin, float screenWidth, float screenHeight)
        {
            var o = _viewing;
            var rect = WindowFrame.Dialog(skin, screenWidth, screenHeight, 560f, 820f);
            skin.IconBadge(new Rect(rect.x + 28, rect.y + 24, 58, 58), Icons.Profile, UiSkin.Accent);
            GUI.Label(new Rect(rect.x + 100, rect.y + 24, rect.width - 260, 32), FishCard.Fit(o.Name, skin.Title, rect.width - 260), skin.Title);
            GUI.Label(new Rect(rect.x + 101, rect.y + 60, rect.width - 260, 22), GameTexts.Arena.Rank + ": " + GameTexts.Arena.RankOf(o.Rank), skin.SmallGold);
            if (skin.IconButton(new Rect(rect.xMax - 156, rect.y + 24, 128, 42), Icons.Close, GameTexts.Box.Close, skin.Button))
            {
                _viewing = null;
                return;
            }

            GUI.Label(new Rect(rect.x + 28, rect.y + 100, rect.width - 56, 24), GameTexts.Arena.Formation, skin.Heading);
            var cardW = (rect.width - 56 - 2 * 12f) / 3f;
            var cardH = 190f;
            for (var i = 0; i < 6; i++)
            {
                var slot = new Rect(rect.x + 28 + (i % 3) * (cardW + 12f), rect.y + 134 + (i / 3) * (cardH + 12f), cardW, cardH);
                var f = o.Fish.Find(x => x.Position == i + 1);
                if (f == null)
                {
                    GUI.Box(slot, GUIContent.none, skin.Card);
                    GUI.Label(new Rect(slot.x, slot.center.y - 10, slot.width, 20), GameTexts.Cardume.Empty, skin.SmallMutedCenter);
                    continue;
                }

                FishCard.Draw(skin, slot, new FishCardModel
                {
                    SpeciesId = f.SpeciesId,
                    Name = f.SpeciesName,
                    Line = GameTexts.Player.LevelShort + " " + f.Level,
                    RarityId = f.RarityId,
                    RarityName = f.RarityName,
                    Footer = GameTexts.Arena.PositionOf(f.Position),
                });
            }
        }

        // ------------------------------------------------------------------ Arena Shop (OD-009)

        private void DrawShop(UiSkin skin, Rect area)
        {
            if (_arena.ShopItems.Count == 0)
            {
                GUI.Label(new Rect(area.x, area.y, area.width, 60), GameTexts.Arena.ShopEmpty, skin.Body);
                return;
            }

            GUI.Label(new Rect(area.x, area.y, area.width, 22), GameTexts.Arena.ShopNote, skin.SmallMuted);
            var y = area.y + 32;
            foreach (var item in _arena.ShopItems)
            {
                var dollars = item.RewardCurrency == "dollars";
                var rect = new Rect(area.x, y, area.width, 96);
                GUI.Box(rect, GUIContent.none, skin.Card);
                skin.IconBadge(new Rect(rect.x + 24, rect.y + 20, 56, 56), dollars ? Icons.Dollar : Icons.Shell, UiSkin.Gold);

                var x = rect.x + 100;
                GUI.Label(new Rect(x, rect.y + 14, 320, 28), item.Name, skin.Heading);
                GUI.Label(new Rect(x, rect.y + 44, 320, 22), GameTexts.Arena.ShopReward(Format.Number(item.RewardAmount), dollars), skin.BodyBold);
                if (item.WeeklyLimit > 0)
                {
                    GUI.Label(new Rect(x, rect.y + 68, 320, 20), GameTexts.Arena.ShopWeekly(item.BoughtThisWeek, item.WeeklyLimit), skin.SmallMuted);
                }

                var px = rect.xMax - 250 - 200;
                skin.DrawIcon(new Rect(px, rect.y + 37, 22, 22), Icons.Honor, UiSkin.Gold);
                GUI.Label(new Rect(px + 28, rect.y + 36, 170, 24), GameTexts.Arena.ShopPrice(Format.Number(item.PriceHonor)), skin.SmallGold);

                var button = new Rect(rect.xMax - 236, rect.y + 27, 216, 42);
                if (item.BuyBlocker != ServiceError.None)
                {
                    skin.DrawIcon(new Rect(button.x, button.y + 11, 20, 20), Icons.Lock, UiSkin.Gold);
                    GUI.Label(new Rect(button.x + 28, button.y + 2, button.width - 28, 42), GameTexts.ServiceErrorMessage(item.BuyBlocker.ToString()), skin.SmallGold);
                }
                else if (skin.IconButton(button, Icons.Buy, GameTexts.Shop.Buy, skin.ButtonPrimary))
                {
                    _root.BuyArenaItem(item.Id);
                    _nextRefresh = 0f;
                }

                y += 106;
            }
        }

        private void DrawHistory(UiSkin skin, Rect area)
        {
            if (_arena.History.Count == 0)
            {
                GUI.Label(new Rect(area.x, area.y, area.width, 22), GameTexts.Arena.EmptyHistory, skin.SmallMuted);
                return;
            }

            var rows = _arena.History;
            _scroll = GUI.BeginScrollView(area, _scroll, new Rect(0, 0, area.width - 20, rows.Count * 40f));
            for (var i = 0; i < rows.Count; i++)
            {
                var h = rows[i];
                var y = i * 40f;
                GUI.Label(new Rect(0, y + 8, 150, 20), Format.DateTimeFromUnixMs(h.AtMs), skin.SmallMuted);
                GUI.Label(new Rect(160, y + 8, 110, 20), h.IsDefense ? GameTexts.Arena.Defended : GameTexts.Arena.Attacked, skin.Small);
                GUI.Label(new Rect(270, y + 8, 300, 20), h.OpponentName, skin.Small);
                GUI.Label(new Rect(580, y + 8, 100, 20), h.PlayerWon ? GameTexts.Arena.Win : GameTexts.Arena.Loss, h.PlayerWon ? skin.SmallGold : skin.Small);
                GUI.Label(new Rect(690, y + 8, 260, 20), GameTexts.Arena.RankChange(h.RankBefore, h.RankAfter), skin.Small);
                GUI.Label(new Rect(960, y + 8, 180, 20), h.HonorChange == 0 ? string.Empty : GameTexts.Arena.HonorChange(h.HonorChange), skin.Small);
            }

            GUI.EndScrollView();
        }

        // ------------------------------------------------------------------ replay (GDD section 27)

        private void StartReplay(BattleReport report, int opponentRank)
        {
            if (report == null)
            {
                return;
            }

            // The VS screen plays first; the replay clock only runs once it is over.
            _introOpponentRank = opponentRank;
            _introStart = Time.unscaledTime;
            _introUntil = _introStart + IntroSeconds;
            _battle = report;
            _time = 0f;
            _speed = 1f;
            _applied = 0;
            _hits.Clear();
            _lunges.Clear();
            _hp = new[] { MaxHp(report.PlayerTeam), MaxHp(report.OpponentTeam) };
            _maxHp = new[] { MaxHp(report.PlayerTeam), MaxHp(report.OpponentTeam) };
        }

        private static double[] MaxHp(List<Fighter> team)
        {
            var hp = new double[6];
            for (var i = 0; i < 6; i++)
            {
                hp[i] = i < team.Count && team[i] != null ? team[i].Stats.Hp : 0;
            }

            return hp;
        }

        /// <summary>
        /// The VS screen (about 1,2 s) before the replay: the window split on a diagonal, the player's side in
        /// blue and the opponent's in warm red, both avatars, names, positions and teams, and a "VS" that
        /// scales in. A click, Space or Escape skips it. Presentation only: the battle is already decided.
        /// </summary>
        private void DrawIntro(UiSkin skin, float screenWidth, float screenHeight)
        {
            var e = Event.current;
            if (e.type == EventType.MouseDown || (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Space || e.keyCode == KeyCode.Escape)))
            {
                _introUntil = 0f;
                e.Use();
                return;
            }

            var t = Mathf.Clamp01((Time.unscaledTime - _introStart) / IntroSeconds);
            var panel = WindowFrame.Panel(skin, screenWidth, screenHeight, 1320f, 840f);

            // The diagonal split: blue on the left, red on the right, a slanted border in the middle.
            var blue = Color.Lerp(UiSkin.Night, UiSkin.Accent, 0.38f);
            var red = Color.Lerp(UiSkin.Night, UiSkin.Danger, 0.5f);
            blue.a = 1f;
            red.a = 1f;
            var cx = panel.center.x;
            var slant = panel.height * 0.18f;
            GUI.DrawTexture(panel, skin.White, ScaleMode.StretchToFill, true, 0, blue, 0, 16);
            var redRect = new Rect(cx + slant, panel.y, panel.xMax - cx - slant, panel.height);
            GUI.DrawTexture(redRect, skin.White, ScaleMode.StretchToFill, true, 0, red, Vector4.zero, new Vector4(0f, 16f, 16f, 0f));
            GUI.DrawTexture(new Rect(cx - slant, panel.y, slant * 2f, panel.height), skin.Diagonal, ScaleMode.StretchToFill, true, 0, red, 0, 0);
            GUI.DrawTexture(panel, skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.Border, 1.5f, 16);

            // Both sides slide in from the edges (ease-out).
            var ease = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / 0.35f), 3f);
            var slide = (1f - ease) * 120f;
            var sideW = panel.width * 0.4f;
            var avatarSize = Mathf.Min(200f, panel.height * 0.3f);
            var top = panel.y + panel.height * 0.16f;
            IntroSide(skin, panel.x + panel.width * 0.25f - slide, top, avatarSize, sideW, _profile?.AvatarId, PlayerName, _battle.RankBefore, _battle.PlayerTeam, false, UiSkin.Accent);
            IntroSide(skin, panel.x + panel.width * 0.75f + slide, top, avatarSize, sideW, null, _battle.OpponentName, _introOpponentRank, _battle.OpponentTeam, true, UiSkin.Danger);

            // The "VS" scales down into place and fades in.
            var vsT = Mathf.Clamp01((t - 0.1f) / 0.4f);
            if (vsT > 0f)
            {
                var vsEase = 1f - Mathf.Pow(1f - vsT, 3f);
                var scale = Mathf.Lerp(2.4f, 1f, vsEase);
                var vs = new Rect(cx - 160f, top + avatarSize / 2f - 70f, 320f, 140f);
                skin.DrawGlow(new Rect(vs.center.x - 70f, vs.center.y - 50f, 140f, 100f), UiSkin.Gold, 0.5f * vsEase);
                var before = GUI.matrix;
                var color = GUI.color;
                var content = GUI.contentColor;
                // Composed in the virtual canvas (the HUD's GUI.matrix already scales it), not in screen pixels.
                var pivot = new Vector3(vs.center.x, vs.center.y, 0f);
                GUI.matrix = before * Matrix4x4.TRS(pivot, Quaternion.identity, new Vector3(scale, scale, 1f)) * Matrix4x4.TRS(-pivot, Quaternion.identity, Vector3.one);
                GUI.color = new Color(color.r, color.g, color.b, color.a * vsEase);
                GUI.contentColor = new Color(0f, 0f, 0f, 0.55f);
                GUI.Label(new Rect(vs.x + 3f, vs.y + 4f, vs.width, vs.height), GameTexts.Arena.VsMark, skin.VsDisplay);
                GUI.contentColor = content;
                GUI.Label(vs, GameTexts.Arena.VsMark, skin.VsDisplay);
                GUI.color = color;
                GUI.matrix = before;
            }

            GUI.Label(new Rect(panel.x, panel.yMax - 40f, panel.width, 22f), GameTexts.Arena.IntroSkip, skin.SmallMutedCenter);
        }

        /// <summary>One side of the VS screen: big avatar, name, position and the team in a row.</summary>
        private static void IntroSide(UiSkin skin, float cx, float top, float size, float width, string avatarId, string name, int rank, List<Fighter> team, bool flipped, Color ring)
        {
            var avatar = new Rect(cx - size / 2f, top, size, size);
            skin.DrawGlow(avatar, ring, 0.4f);
            AvatarFrame(skin, avatar, avatarId, ring);
            GUI.Label(new Rect(cx - width / 2f, avatar.yMax + 14f, width, 36f), FishCard.Fit(name, skin.TitleCenter, width), skin.TitleCenter);
            GUI.Label(new Rect(cx - width / 2f, avatar.yMax + 52f, width, 22f), GameTexts.Arena.RankOf(rank), skin.SmallGoldCenter);

            const int count = 6;
            const float gap = 6f;
            var fw = Mathf.Min(76f, width / count - gap);
            var fh = fw * 0.55f;
            var rowW = count * fw + (count - 1) * gap;
            var fy = avatar.yMax + 84f;
            for (var i = 0; i < count; i++)
            {
                var slot = new Rect(cx - rowW / 2f + i * (fw + gap), fy, fw, fh);
                GUI.DrawTexture(slot, skin.White, ScaleMode.StretchToFill, true, 0, new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.45f), 0, 8);
                var fighter = team != null && i < team.Count ? team[i] : null;
                if (fighter != null)
                {
                    DrawFish(new Rect(slot.x + 3f, slot.y + 2f, slot.width - 6f, slot.height - 4f), fighter.SpeciesId, flipped);
                }
            }
        }

        private void DrawReplay(UiSkin skin, float screenWidth, float screenHeight)
        {
            var events = _battle.Outcome.Events;
            var duration = (float)_battle.Outcome.DurationSeconds;
            if (Event.current.type == EventType.Repaint && _time <= duration)
            {
                _time += Time.unscaledDeltaTime * _speed;
            }

            while (_applied < events.Count && events[_applied].Time <= _time)
            {
                var e = events[_applied++];
                var targetSide = 1 - e.AttackerSide;
                _hp[targetSide][e.TargetPosition - 1] = e.TargetHpAfter;
                _hits.Add((Time.unscaledTime, targetSide, e.TargetPosition, e.Damage));
                _lunges[(e.AttackerSide, e.AttackerPosition)] = Time.unscaledTime;
            }

            _hits.RemoveAll(h => Time.unscaledTime - h.at > 0.9f);

            var area = WindowFrame.Draw(skin, screenWidth, screenHeight, GameTexts.Arena.Versus(_battle.OpponentName), GameTexts.Arena.Clock(Format.Countdown(Mathf.Min(_time, duration))), out var closed, icon: Icons.Attack);
            if (closed)
            {
                Close();
                return;
            }

            var cx = area.x + area.width / 2f;
            var cy = area.y + area.height / 2f - 40f;
            DrawSide(skin, _battle.PlayerTeam, 0, cx, cy, -1f);
            DrawSide(skin, _battle.OpponentTeam, 1, cx, cy, 1f);

            var finished = _time > duration;
            var by = area.yMax - 56;
            if (!finished)
            {
                if (GUI.Button(new Rect(cx - 250, by, 150, 42), GameTexts.Arena.Speed1, _speed == 1f ? skin.ChipActive : skin.Chip)) _speed = 1f;
                if (GUI.Button(new Rect(cx - 80, by, 150, 42), GameTexts.Arena.Speed2, _speed == 2f ? skin.ChipActive : skin.Chip)) _speed = 2f;
                if (GUI.Button(new Rect(cx + 90, by, 160, 42), GameTexts.Arena.Skip, skin.Button)) _time = duration + 0.01f;
                return;
            }

            // Result panel: a wide strip under the formations, so it never covers the bottom row's
            // HP bars and names (the back row's name ends at cy + 150 + 18 + 70, see DrawSide).
            var formationBottom = cy + 150f + 18f + 70f;
            var panelTop = Mathf.Max(formationBottom + 8f, area.yMax - 190f);
            var panel = new Rect(cx - 380, panelTop, 760, Mathf.Clamp(area.yMax - panelTop, 120f, 190f));
            var won = _battle.PlayerWon;
            if (won)
            {
                // Arena victory: a moderate celebration (Art Bible, section 16.12).
                var rays = panel.height * 2.6f;
                var before = GUI.matrix;
                // Composed in the virtual canvas (the HUD's GUI.matrix already scales it), not in screen pixels.
                var pivot = new Vector3(panel.center.x, panel.center.y, 0f);
                GUI.matrix = before * Matrix4x4.TRS(pivot, Quaternion.Euler(0f, 0f, Time.unscaledTime * 12f), Vector3.one) * Matrix4x4.TRS(-pivot, Quaternion.identity, Vector3.one);
                GUI.DrawTexture(new Rect(panel.center.x - rays / 2f, panel.center.y - rays / 2f, rays, rays), skin.Rays, ScaleMode.StretchToFill, true, 0, new Color(UiSkin.Gold.r, UiSkin.Gold.g, UiSkin.Gold.b, 0.35f), 0, 0);
                GUI.matrix = before;
                skin.DrawGlow(panel, UiSkin.Gold, 0.45f);
            }

            skin.DrawShadow(panel);
            GUI.Box(panel, GUIContent.none, skin.PanelSolid);
            skin.DrawOutline(panel, won ? UiSkin.Gold : UiSkin.Border);
            skin.DrawIcon(new Rect(panel.x + 24, panel.y + 18, 44, 44), won ? Icons.Arena : Icons.Swap, won ? UiSkin.Gold : UiSkin.Muted);
            var textX = panel.x + 84;
            var textW = panel.width - 84 - 268;
            GUI.contentColor = won ? UiSkin.GoldLight : Color.white;
            GUI.Label(new Rect(textX, panel.y + 16, textW, 34), won ? GameTexts.Arena.Victory : GameTexts.Arena.Defeat, skin.Title);
            GUI.contentColor = Color.white;
            GUI.Label(new Rect(textX, panel.y + 56, textW, 22), GameTexts.Arena.RankChange(_battle.RankBefore, _battle.RankAfter), skin.Body);
            GUI.Label(new Rect(textX, panel.y + 80, textW, 22), GameTexts.Arena.HonorChange(_battle.HonorChange), skin.Body);
            if (skin.IconButton(new Rect(panel.xMax - 244, panel.center.y - 21, 220, 42), Icons.Arena, GameTexts.Arena.BackToArena, skin.ButtonPrimary))
            {
                _battle = null;
                _nextRefresh = 0f;
            }
        }

        /// <summary>A mirrored 3 front + 3 back formation (GDD section 26). direction −1 = left side.</summary>
        private void DrawSide(UiSkin skin, List<Fighter> team, int side, float cx, float cy, float direction)
        {
            for (var i = 0; i < 6; i++)
            {
                var fighter = i < team.Count ? team[i] : null;
                if (fighter == null)
                {
                    continue;
                }

                var front = i < 3;
                var row = i % 3;
                var x = cx + direction * (front ? 170f : 360f);
                var y = cy - 150f + row * 150f + (front ? 0f : 18f);

                // Tiny forward tic when attacking (GDD section 27).
                if (_lunges.TryGetValue((side, i + 1), out var at) && Time.unscaledTime - at < 0.18f)
                {
                    x -= direction * 22f * Mathf.Sin((Time.unscaledTime - at) / 0.18f * Mathf.PI);
                }

                var hp = _hp[side][i];
                var alive = hp > 0;
                var previous = GUI.color;
                GUI.color = previous * (alive ? Color.white : new Color(1f, 1f, 1f, 0.15f));
                // Player fish face right, toward the opponent; the opponent's are mirrored.
                DrawFish(new Rect(x - 70, y - 34, 140, 68), fighter.SpeciesId, direction > 0f);
                GUI.color = previous;
                var max = _maxHp[side][i];
                skin.Bar(new Rect(x - 60, y + 38, 120, 8), max > 0 ? (float)(hp / max) : 0f, false);
                var fighterLabel = fighter.SpeciesName + " · " + GameTexts.Player.LevelShort + " " + fighter.Level;
                GUI.Label(new Rect(x - 70, y + 50, 140, 20), FishCard.Fit(fighterLabel, skin.SmallMutedCenter, 140f), skin.SmallMutedCenter);

                // Damage numbers float up and fade.
                foreach (var hit in _hits.Where(h => h.side == side && h.pos == i + 1))
                {
                    var t = Time.unscaledTime - hit.at;
                    var c = GUI.color;
                    GUI.color = c * new Color(1f, 1f, 1f, 1f - t / 0.9f);
                    GUI.Label(new Rect(x - 40, y - 60 - t * 40f, 80, 24), "-" + Format.Number((long)System.Math.Round(hit.dmg)), skin.SmallGold);
                    GUI.color = c;
                }
            }
        }
    }
}
