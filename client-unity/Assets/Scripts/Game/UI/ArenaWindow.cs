using System.Collections.Generic;
using System.Linq;
using FishingIdle.Game.Bootstrap;
using FishingIdle.Game.Scene;
using FishingIdle.GameService.Arena;
using FishingIdle.GameService.Core;
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
        }

        public void Close()
        {
            if (_viewing != null)
            {
                _viewing = null;
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
                DrawReplay(skin, screenWidth, screenHeight);
                return;
            }

            if (Time.unscaledTime >= _nextRefresh)
            {
                _arena = _root.GetArena();
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

            // Summary: rank, Energy, Honor.
            Stat(skin, area.x, area.y, Icons.Arena, GameTexts.Arena.Rank, GameTexts.Arena.RankOfTotal(_arena.Rank, _arena.Participants));
            var energy = GameTexts.Arena.EnergyOf(_arena.Energy, _arena.EnergyMax);
            Stat(skin, area.x + 260, area.y, Icons.Energy, GameTexts.Arena.Energy, energy);
            if (_arena.NextEnergySeconds > 0)
            {
                GUI.Label(new Rect(area.x + 400, area.y + 30, 160, 20), GameTexts.Arena.NextEnergy(Format.Countdown(_arena.NextEnergySeconds)), skin.SmallMuted);
            }

            Stat(skin, area.x + 580, area.y, Icons.Honor, GameTexts.Arena.Honor, Format.Number(_arena.Honor));

            var tabs = new[] { (Tab.Opponents, GameTexts.Arena.TabOpponents), (Tab.Ranking, GameTexts.Arena.TabRanking), (Tab.History, GameTexts.Arena.TabHistory), (Tab.Shop, GameTexts.Arena.TabShop) };
            var x = area.x;
            foreach (var (tab, label) in tabs)
            {
                var w = skin.Chip.CalcSize(new GUIContent(label)).x + 12;
                if (GUI.Button(new Rect(x, area.y + 64, w, 32), label, _tab == tab ? skin.ChipActive : skin.Chip))
                {
                    _tab = tab;
                    _scroll = Vector2.zero;
                    _rankPage = 0;
                }

                x += w + 8;
            }

            var content = new Rect(area.x, area.y + 112, area.width, area.height - 112);
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

        private static void Stat(UiSkin skin, float x, float y, string icon, string label, string value)
        {
            skin.DrawIcon(new Rect(x, y + 10, 30, 30), icon, UiSkin.Gold);
            GUI.Label(new Rect(x + 40, y, 200, 20), label, skin.SmallMuted);
            GUI.Label(new Rect(x + 40, y + 20, 200, 30), value, skin.Number);
        }

        // ------------------------------------------------------------------ opponents

        private void DrawOpponents(UiSkin skin, Rect area)
        {
            GUI.Label(new Rect(area.x, area.y, area.width, 20), GameTexts.Arena.NoForceNote, skin.SmallMuted);
            if (_arena.AttackBlocker != ServiceError.None)
            {
                GUI.Label(new Rect(area.x, area.y + 22, area.width, 22), GameTexts.ServiceErrorMessage(_arena.AttackBlocker.ToString()), skin.SmallGold);
            }

            var top = area.y + 52;
            var bottom = area.yMax - 56;
            var count = _arena.Opponents.Count;
            var cardW = (area.width - (count - 1) * 16f) / Mathf.Max(1, count);
            for (var i = 0; i < count; i++)
            {
                var o = _arena.Opponents[i];
                var rect = new Rect(area.x + i * (cardW + 16f), top, cardW, bottom - top - 10);
                GUI.Box(rect, GUIContent.none, skin.Card);
                skin.IconBadge(new Rect(rect.x + 16, rect.y + 14, 50, 50), Icons.Profile, UiSkin.Accent);
                GUI.Label(new Rect(rect.x + 78, rect.y + 14, rect.width - 96 - 110, 26), FishCard.Fit(o.Name, skin.Heading, rect.width - 96 - 110), skin.Heading);
                if (GUI.Button(new Rect(rect.xMax - 120, rect.y + 16, 104, 30), GameTexts.Arena.ViewProfile, skin.Chip))
                {
                    _viewing = o;
                }
                GUI.Label(new Rect(rect.x + 78, rect.y + 42, rect.width - 96, 22), GameTexts.Arena.RankOf(o.Rank), skin.SmallGold);

                // The Cardume in formation order, with level and rarity — never its Strength.
                var fy = rect.y + 76;
                foreach (var f in o.Fish)
                {
                    GUI.DrawTexture(new Rect(rect.x + 18, fy, 70, 34), Art.FishTexture(f.SpeciesId), ScaleMode.ScaleToFit, true);
                    var pillWidth = string.IsNullOrEmpty(f.RarityName) ? 0f : skin.PillWidth(f.RarityName.ToUpperInvariant(), false) + 8f;
                    var nameWidth = rect.width - 96 - 18 - pillWidth;
                    GUI.Label(new Rect(rect.x + 96, fy + 2, nameWidth, 20), FishCard.Fit(f.Position + ". " + f.SpeciesName, skin.Small, nameWidth), skin.Small);
                    GUI.Label(new Rect(rect.x + 96, fy + 18, rect.width - 170, 18), GameTexts.Player.LevelShort + " " + f.Level, skin.SmallMuted);
                    if (!string.IsNullOrEmpty(f.RarityName))
                    {
                        var rarity = f.RarityName.ToUpperInvariant();
                        var pw = skin.PillWidth(rarity, false);
                        skin.RarityPill(new Rect(rect.xMax - 18 - pw, fy + 7, pw, 20), f.RarityId, rarity, false);
                    }

                    fy += 40;
                }

                var enabledBefore = GUI.enabled;
                GUI.enabled = enabledBefore && _arena.AttackBlocker == ServiceError.None;
                if (skin.IconButton(new Rect(rect.x + 18, rect.yMax - 58, rect.width - 36, 42), Icons.Attack, GameTexts.Arena.Attack, skin.ButtonPrimary))
                {
                    StartReplay(_root.Attack(i));
                }

                GUI.enabled = enabledBefore;
            }

            if (_arena.RerollsLeft > 0)
            {
                if (skin.IconButton(new Rect(area.x, bottom, 300, 42), Icons.Swap, GameTexts.Arena.RerollsLeft(_arena.RerollsLeft), skin.Button))
                {
                    _root.RerollOpponents();
                    _nextRefresh = 0f;
                }
            }
            else
            {
                GUI.Label(new Rect(area.x, bottom + 10, area.width, 22), GameTexts.Arena.RerollUsed, skin.SmallMuted);
            }
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

        private void StartReplay(BattleReport report)
        {
            if (report == null)
            {
                return;
            }

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
                var art = new Rect(x - 70, y - 34, 140, 68);
                var matrix = GUI.matrix;
                if (direction < 0f)
                {
                    // Player fish face right, toward the opponent.
                    GUI.DrawTexture(art, Art.FishTexture(fighter.SpeciesId), ScaleMode.ScaleToFit, true);
                }
                else
                {
                    // Mirrored around the fish's centre in the virtual canvas (not in screen pixels).
                    var pivot = new Vector3(art.center.x, art.center.y, 0f);
                    GUI.matrix = matrix * Matrix4x4.TRS(pivot, Quaternion.identity, new Vector3(-1f, 1f, 1f)) * Matrix4x4.TRS(-pivot, Quaternion.identity, Vector3.one);
                    GUI.DrawTexture(art, Art.FishTexture(fighter.SpeciesId), ScaleMode.ScaleToFit, true);
                    GUI.matrix = matrix;
                }

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
