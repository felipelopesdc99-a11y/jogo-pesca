using System.Collections.Generic;
using FishingIdle.Game.Bootstrap;
using FishingIdle.Game.Visual;
using FishingIdle.GameService.Missions;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The "Missões" window (M24-T54, addendum A-177), on a "Diário de bordo" page (A-170) with the "i": the 100 missions as cards
    /// in a scrolling list, split by the bookmarks Em andamento / Prontas / Concluídas, and "Resgatar todas". Each card: its
    /// number, the goal, a bar with "X / Y", the reward (Moedas, Conchas, Dólares), and Resgatar, the "Concluída" seal or the
    /// "Liga quando o jogo estiver online" note.
    /// </summary>
    /// <remarks>
    /// Shows what the mission service returns (progress, rewards, what is ready); decides nothing. The texts of each row are
    /// built when the list is refreshed (once a second while open), never per frame; only the cards in view are drawn. Icon
    /// ico_atalho_missoes (received 13/10/2026; the confirm icon only if the file is missing).
    /// </remarks>
    public sealed class MissionsWindow
    {
        // On the grid of 8 since A-190: one row of 88 (goal, bar and progress, the rewards in line), 8 between rows.
        private const float SummaryHeight = 48f, Gap = 16f, CardHeight = 88f, CardGap = 8f, ScrollBar = 18f;
        private const float RewardWidth = 232f, ActionWidth = 168f, RefreshSeconds = 1f;

        private enum Tab
        {
            Active,
            Ready,
            Done,
        }

        private static readonly string[] TabIcons = { Icons.Hourglass, Icons.Check, Icons.Star };

        /// <summary>One card's texts, built at refresh time.</summary>
        private sealed class Row
        {
            public MissionView View;
            public string Progress;
            public string Coins;
            public string Shells;
            public string Dollars;
            public string Note;
        }

        private readonly GameRoot _root;
        private readonly List<Row> _rows = new List<Row>();
        private readonly string[] _tabLabels = { GameTexts.Missions.TabActive, GameTexts.Missions.TabReady, GameTexts.Missions.TabDone };
        private readonly string[] _tabNotes = new string[3];
        private MissionsView _view;
        private Tab _tab;
        private float _nextRefresh;
        private Vector2 _scroll;
        private string _counter, _claimAll;

        public MissionsWindow(GameRoot root)
        {
            _root = root;
        }

        public bool IsOpen { get; private set; }

        /// <summary>The window and shortcut icon: ico_atalho_missoes when it exists, otherwise the check mark.</summary>
        public static string NavIcon => ArtAssets.Icon("atalho_missoes") != null ? "atalho_missoes" : Icons.Check;

        public void Open()
        {
            IsOpen = true;
            _scroll = Vector2.zero;
            Refresh();
            // Opens on what can be claimed, when there is anything.
            _tab = _view != null && _view.ReadyCount > 0 ? Tab.Ready : Tab.Active;
            Rebuild();
        }

        public void Close() => IsOpen = false;

        public void Draw(UiSkin skin, float screenWidth, float screenHeight)
        {
            if (!IsOpen)
            {
                return;
            }

            if (_view == null || Time.unscaledTime >= _nextRefresh)
            {
                Refresh();
                Rebuild();
            }

            var area = WindowFrame.Draw(skin, screenWidth, screenHeight, GameTexts.Missions.Title, GameTexts.Missions.Info, out var closed, 1320f, 840f, NavIcon, _counter, bookmarks: true);
            if (closed)
            {
                Close();
                return;
            }

            if (_view == null)
            {
                return;
            }

            var clicked = WindowFrame.Bookmarks(skin, _tabLabels, TabIcons, (int)_tab, _tabNotes);
            if (clicked >= 0 && clicked != (int)_tab)
            {
                _tab = (Tab)clicked;
                _scroll = Vector2.zero;
                Rebuild();
            }

            DrawSummary(skin, new Rect(area.x, area.y, area.width, SummaryHeight));
            DrawList(skin, new Rect(area.x, area.y + SummaryHeight + Gap, area.width, area.height - SummaryHeight - Gap));
        }

        private void Refresh()
        {
            _view = _root.IsRunning ? _root.Game.Missions.GetMissions() : null;
            _nextRefresh = Time.unscaledTime + RefreshSeconds;
            if (_view == null)
            {
                return;
            }

            _counter = GameTexts.Missions.Counter(_view.ClaimedCount, _view.Total);
            _claimAll = _view.ReadyCount > 0 ? GameTexts.Missions.ClaimAll(_view.ReadyCount) : GameTexts.Missions.NothingToClaim;
            _tabNotes[0] = (_view.Total - _view.ReadyCount - _view.ClaimedCount).ToString();
            _tabNotes[1] = _view.ReadyCount.ToString();
            _tabNotes[2] = _view.ClaimedCount.ToString();
        }

        /// <summary>The rows of the open tab, with every text they show.</summary>
        private void Rebuild()
        {
            _rows.Clear();
            if (_view == null)
            {
                return;
            }

            foreach (var m in _view.Missions)
            {
                var inTab = _tab == Tab.Ready ? m.Ready : _tab == Tab.Done ? m.Claimed : !m.Ready && !m.Claimed;
                if (!inTab)
                {
                    continue;
                }

                _rows.Add(new Row
                {
                    View = m,
                    Progress = GameTexts.Missions.Progress(m.Current, m.Target),
                    Coins = m.RewardCoins > 0 ? Format.Short(m.RewardCoins) : null,
                    Shells = m.RewardShells > 0 ? Format.Number(m.RewardShells) : null,
                    Dollars = m.RewardDollars > 0 ? GameTexts.Pass.DollarsLine(Format.Number(m.RewardDollars)) : null,
                    Note = m.OnlineOnly && !_view.Online && !m.Ready && !m.Claimed ? GameTexts.Missions.OnlineOnly
                        : m.Type == MissionTypes.ArenaRankTop && !m.Ready && !m.Claimed ? GameTexts.Missions.BestRank(m.BestRank)
                        : null,
                });
            }
        }

        // ------------------------------------------------------------------ summary

        /// <summary>
        /// "Resgatar todas (N)" on the right, with no box behind it (A-190: the tab's name was already on the tab).
        /// </summary>
        private void DrawSummary(UiSkin skin, Rect rect)
        {
            var all = new Rect(rect.xMax - 300f, rect.y, 300f, rect.height);
            var ready = _view.ReadyCount > 0;
            var was = GUI.enabled;
            GUI.enabled = was && ready;
            if (skin.IconButton(all, Icons.Check, FishCard.Fit(_claimAll, skin.ButtonPrimary, all.width - 56f), ready ? skin.ButtonPrimary : skin.Button))
            {
                _root.ClaimAllMissions();
                Refresh();
                Rebuild();
            }

            GUI.enabled = was;
        }

        // ------------------------------------------------------------------ the list

        private void DrawList(UiSkin skin, Rect area)
        {
            if (_rows.Count == 0)
            {
                var empty = _tab == Tab.Ready ? GameTexts.Missions.EmptyReady : _tab == Tab.Done ? GameTexts.Missions.EmptyDone : GameTexts.Missions.EmptyActive;
                GUI.Label(new Rect(area.x, area.y + 40f, area.width, 30f), empty, skin.SmallMutedCenter);
                return;
            }

            var contentHeight = _rows.Count * (CardHeight + CardGap) - CardGap;
            var scrolls = contentHeight > area.height;
            var inner = area.width - (scrolls ? ScrollBar : 0f);
            _scroll = GUI.BeginScrollView(area, _scroll, new Rect(0f, 0f, inner, Mathf.Max(contentHeight, area.height - 1f)));

            // Only the cards in view.
            var first = Mathf.Max(0, Mathf.FloorToInt(_scroll.y / (CardHeight + CardGap)));
            var last = Mathf.Min(_rows.Count - 1, Mathf.CeilToInt((_scroll.y + area.height) / (CardHeight + CardGap)));
            for (var i = first; i <= last; i++)
            {
                DrawCard(skin, new Rect(0f, i * (CardHeight + CardGap), inner, CardHeight), _rows[i]);
            }

            GUI.EndScrollView();
        }

        private void DrawCard(UiSkin skin, Rect rect, Row row)
        {
            var m = row.View;
            GUI.Box(rect, GUIContent.none, m.Ready ? skin.CardImportant : skin.Card);
            if (m.Ready)
            {
                skin.ArcLine(rect, UiSkin.Gold, 10f);
            }

            var previous = GUI.color;
            if (m.Claimed)
            {
                GUI.color = previous * new Color(1f, 1f, 1f, 0.7f);
            }

            // The goal, then its bar with the progress at the right of it (measured, never cut); the rewards in one
            // line in their own column (A-190: no "Nº 13" and no state written on each row, the tab says it).
            var action = new Rect(rect.xMax - 16f - ActionWidth, rect.center.y - 22f, ActionWidth, 44f);
            var reward = new Rect(action.x - 16f - RewardWidth, rect.y, RewardWidth, rect.height);
            var x = rect.x + 16f;
            var w = reward.x - 16f - x;
            GUI.Label(new Rect(x, rect.y + 8f, w, 28f), FishCard.Fit(m.Text, skin.BodyBold, w), skin.BodyBold);

            var barY = rect.y + 40f;
            var progressWidth = Mathf.Min(w * 0.5f, skin.SmallRight.CalcSize(new GUIContent(row.Progress)).x + 4f);
            skin.Bar(new Rect(x, barY + 6f, Mathf.Max(24f, w - progressWidth - 16f), 12f), (float)m.Fraction, m.Ready || m.Claimed);
            GUI.Label(new Rect(x + w - progressWidth, barY, progressWidth, UiSkin.SmallLine), FishCard.Fit(row.Progress, skin.SmallRight, progressWidth), skin.SmallRight);
            if (row.Note != null)
            {
                GUI.Label(new Rect(x, rect.yMax - 8f - UiSkin.SmallLine, w, UiSkin.SmallLine), FishCard.Fit(row.Note, skin.SmallMuted, w), skin.SmallMuted);
            }

            DrawReward(skin, reward, row);
            GUI.color = previous;

            if (m.Claimed)
            {
                skin.AccentPill(action, GameTexts.Missions.Done, UiSkin.Muted, Icons.Check);
            }
            else if (m.Ready)
            {
                if (skin.IconButton(action, Icons.Check, GameTexts.Missions.Claim, skin.ButtonPrimary))
                {
                    _root.ClaimMission(m.Id);
                    Refresh();
                    Rebuild();
                }
            }
        }

        /// <summary>The reward column: Moedas, Conchas and, in the hard missions, Dólares, in one line (icon + number).</summary>
        private static void DrawReward(UiSkin skin, Rect rect, Row row)
        {
            var x = rect.x;
            var y = rect.center.y - UiSkin.SmallLine / 2f;
            // The coloured pixel icons at 1× (24 px, M25-T15).
            if (row.Coins != null)
            {
                x = RewardPiece(skin, x, y, rect.xMax, Icons.Coin, row.Coins, skin.SmallGoldLine);
            }

            if (row.Shells != null)
            {
                x = RewardPiece(skin, x, y, rect.xMax, Icons.Shell, row.Shells, skin.SmallBold);
            }

            if (row.Dollars != null)
            {
                RewardPiece(skin, x, y, rect.xMax, Icons.Dollar, row.Dollars, skin.SmallBold);
            }
        }

        /// <summary>One icon + number of the reward line; returns where the next one starts (16 px after it).</summary>
        private static float RewardPiece(UiSkin skin, float x, float y, float right, string icon, string text, GUIStyle style)
        {
            if (x >= right - 24f)
            {
                return x;
            }

            skin.CurrencyIcon(new Rect(x, y - 1f, 24f, 24f), icon);
            var w = Mathf.Min(style.CalcSize(new GUIContent(text)).x + 4f, right - x - 30f);
            GUI.Label(new Rect(x + 30f, y, w, UiSkin.SmallLine), FishCard.Fit(text, style, w), style);
            return x + 30f + w + 16f;
        }
    }
}
