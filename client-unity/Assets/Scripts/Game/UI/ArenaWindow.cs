using System.Collections.Generic;
using System.Linq;
using FishingIdle.Game.Bootstrap;
using FishingIdle.Game.Scene;
using FishingIdle.GameService.Arena;
using FishingIdle.GameService.Core;
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

        public void Open()
        {
            IsOpen = true;
            _nextRefresh = 0f;
        }

        public void Close()
        {
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

            var area = WindowFrame.Draw(skin, screenWidth, screenHeight, GameTexts.Arena.Title, GameTexts.Arena.Note, out var closed);
            if (closed)
            {
                Close();
                return;
            }

            // Summary: rank, Energy, Honor.
            Stat(skin, area.x, area.y, GameTexts.Arena.Rank, GameTexts.Arena.RankOfTotal(_arena.Rank, _arena.Participants));
            var energy = GameTexts.Arena.EnergyOf(_arena.Energy, _arena.EnergyMax);
            Stat(skin, area.x + 260, area.y, GameTexts.Arena.Energy, energy);
            if (_arena.NextEnergySeconds > 0)
            {
                GUI.Label(new Rect(area.x + 400, area.y + 30, 160, 20), GameTexts.Arena.NextEnergy(Format.Countdown(_arena.NextEnergySeconds)), skin.SmallMuted);
            }

            Stat(skin, area.x + 580, area.y, GameTexts.Arena.Honor, Format.Number(_arena.Honor));

            var tabs = new[] { (Tab.Opponents, GameTexts.Arena.TabOpponents), (Tab.Ranking, GameTexts.Arena.TabRanking), (Tab.History, GameTexts.Arena.TabHistory), (Tab.Shop, GameTexts.Arena.TabShop) };
            var x = area.x;
            foreach (var (tab, label) in tabs)
            {
                var w = skin.Chip.CalcSize(new GUIContent(label)).x + 12;
                if (GUI.Button(new Rect(x, area.y + 64, w, 32), label, _tab == tab ? skin.ChipActive : skin.Chip))
                {
                    _tab = tab;
                    _scroll = Vector2.zero;
                }

                x += w + 8;
            }

            var content = new Rect(area.x, area.y + 112, area.width, area.height - 112);
            switch (_tab)
            {
                case Tab.Ranking: DrawRanking(skin, content); break;
                case Tab.History: DrawHistory(skin, content); break;
                case Tab.Shop: GUI.Label(new Rect(content.x, content.y, content.width, 60), GameTexts.Arena.ShopEmpty, skin.Body); break;
                default: DrawOpponents(skin, content); break;
            }
        }

        private static void Stat(UiSkin skin, float x, float y, string label, string value)
        {
            GUI.Label(new Rect(x, y, 240, 20), label, skin.SmallMuted);
            GUI.Label(new Rect(x, y + 20, 240, 30), value, skin.Number);
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
                GUI.Label(new Rect(rect.x + 18, rect.y + 14, rect.width - 36, 26), o.Name, skin.Heading);
                GUI.Label(new Rect(rect.x + 18, rect.y + 42, rect.width - 36, 22), GameTexts.Arena.RankOf(o.Rank), skin.SmallGold);

                // The Cardume in formation order, with level and rarity — never its Strength.
                var fy = rect.y + 76;
                foreach (var f in o.Fish)
                {
                    GUI.DrawTexture(new Rect(rect.x + 18, fy, 70, 34), Art.FishTexture(f.SpeciesId), ScaleMode.ScaleToFit, true);
                    GUI.Label(new Rect(rect.x + 96, fy + 2, rect.width - 170, 20), f.Position + ". " + f.SpeciesName, skin.Small);
                    GUI.Label(new Rect(rect.x + 96, fy + 18, rect.width - 170, 18), GameTexts.Player.LevelShort + " " + f.Level, skin.SmallMuted);
                    if (f.RarityId != null && f.RarityId != "common")
                    {
                        skin.Tag(new Rect(rect.xMax - 76, fy + 8, 58, 18), f.RarityName.ToUpperInvariant(), UiSkin.Rare);
                    }

                    fy += 40;
                }

                GUI.enabled = _arena.AttackBlocker == ServiceError.None;
                if (GUI.Button(new Rect(rect.x + 18, rect.yMax - 58, rect.width - 36, 42), GameTexts.Arena.Attack, skin.ButtonPrimary))
                {
                    StartReplay(_root.Attack(i));
                }

                GUI.enabled = true;
            }

            if (_arena.RerollsLeft > 0)
            {
                if (GUI.Button(new Rect(area.x, bottom, 280, 42), GameTexts.Arena.RerollsLeft(_arena.RerollsLeft), skin.Button))
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

        private void DrawRanking(UiSkin skin, Rect area)
        {
            var rows = _arena.Ranking;
            _scroll = GUI.BeginScrollView(area, _scroll, new Rect(0, 0, area.width - 20, rows.Count * 34f));
            for (var i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                var rect = new Rect(0, i * 34f, area.width - 20, 30);
                if (r.IsPlayer)
                {
                    GUI.Box(rect, GUIContent.none, skin.CardSelected);
                }

                GUI.Label(new Rect(rect.x + 16, rect.y + 6, 90, 20), GameTexts.Arena.RankOf(r.Rank), skin.SmallGold);
                GUI.Label(new Rect(rect.x + 110, rect.y + 6, rect.width - 130, 20), r.IsPlayer ? r.Name + " (" + GameTexts.Arena.You + ")" : r.Name, r.IsPlayer ? skin.BodyBold : skin.Small);
            }

            GUI.EndScrollView();
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

            var area = WindowFrame.Draw(skin, screenWidth, screenHeight, GameTexts.Arena.Versus(_battle.OpponentName), GameTexts.Arena.Clock(Format.Countdown(Mathf.Min(_time, duration))), out var closed);
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

            // Result panel.
            var panel = new Rect(cx - 260, by - 150, 520, 190);
            GUI.Box(panel, GUIContent.none, skin.CardImportant);
            GUI.Label(new Rect(panel.x + 24, panel.y + 18, panel.width - 48, 34), _battle.PlayerWon ? GameTexts.Arena.Victory : GameTexts.Arena.Defeat, skin.Title);
            GUI.Label(new Rect(panel.x + 24, panel.y + 60, panel.width - 48, 22), GameTexts.Arena.RankChange(_battle.RankBefore, _battle.RankAfter), skin.Body);
            GUI.Label(new Rect(panel.x + 24, panel.y + 86, panel.width - 48, 22), GameTexts.Arena.HonorChange(_battle.HonorChange), skin.Body);
            if (GUI.Button(new Rect(panel.xMax - 244, panel.yMax - 58, 220, 42), GameTexts.Arena.BackToArena, skin.ButtonPrimary))
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
                GUI.color = alive ? Color.white : new Color(1f, 1f, 1f, 0.15f);
                var art = new Rect(x - 70, y - 34, 140, 68);
                var matrix = GUI.matrix;
                if (direction < 0f)
                {
                    // Player fish face right, toward the opponent.
                    GUI.DrawTexture(art, Art.FishTexture(fighter.SpeciesId), ScaleMode.ScaleToFit, true);
                }
                else
                {
                    GUIUtility.ScaleAroundPivot(new Vector2(-1f, 1f), art.center);
                    GUI.DrawTexture(art, Art.FishTexture(fighter.SpeciesId), ScaleMode.ScaleToFit, true);
                    GUI.matrix = matrix;
                }

                GUI.color = previous;
                var max = _maxHp[side][i];
                skin.Bar(new Rect(x - 60, y + 38, 120, 8), max > 0 ? (float)(hp / max) : 0f, false);
                GUI.Label(new Rect(x - 70, y + 50, 140, 20), fighter.SpeciesName + " · " + GameTexts.Player.LevelShort + " " + fighter.Level, skin.Small);

                // Damage numbers float up and fade.
                foreach (var hit in _hits.Where(h => h.side == side && h.pos == i + 1))
                {
                    var t = Time.unscaledTime - hit.at;
                    var c = GUI.color;
                    GUI.color = new Color(1f, 1f, 1f, 1f - t / 0.9f);
                    GUI.Label(new Rect(x - 40, y - 60 - t * 40f, 80, 24), "-" + Format.Number((long)System.Math.Round(hit.dmg)), skin.SmallGold);
                    GUI.color = c;
                }
            }
        }
    }
}
