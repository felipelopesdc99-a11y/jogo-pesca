using FishingIdle.Game.Bootstrap;
using FishingIdle.Game.Visual;
using FishingIdle.GameService.Ranking;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The Ranking menu (addendum A-100): Nível, Moedas, Conchas and Peixes pescados, real players only.
    /// It shows what the ranking service returns; in the local MVP that is just the player on this PC.
    /// </summary>
    public sealed class RankingWindow
    {
        private readonly GameRoot _root;
        private RankingCategory _category = RankingCategory.Level;
        private RankingView _view;
        private float _nextRefresh;
        private Vector2 _scroll;

        private const float RowHeight = 58f;

        public RankingWindow(GameRoot root)
        {
            _root = root;
        }

        public bool IsOpen { get; private set; }

        public void Open()
        {
            IsOpen = true;
            _nextRefresh = 0f;
        }

        public void Close() => IsOpen = false;

        public void Draw(UiSkin skin, float screenWidth, float screenHeight)
        {
            if (!IsOpen)
            {
                return;
            }

            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + 1f;
                _view = _root.GetRanking(_category);
            }

            var area = WindowFrame.Draw(skin, screenWidth, screenHeight, GameTexts.Ranking.Title, GameTexts.Ranking.Subtitle, out var closed, 980f, 760f, Icons.Ranking);
            if (closed)
            {
                Close();
                return;
            }

            // Tabs: what the ranking is sorted by.
            var x = area.x;
            foreach (var (category, label) in new[]
                     {
                         (RankingCategory.Level, GameTexts.Ranking.TabLevel), (RankingCategory.Coins, GameTexts.Ranking.TabCoins),
                         (RankingCategory.Shells, GameTexts.Ranking.TabShells), (RankingCategory.FishCaught, GameTexts.Ranking.TabFish),
                     })
            {
                var w = skin.Chip.CalcSize(new GUIContent(label)).x + 12f;
                if (GUI.Button(new Rect(x, area.y, w, 34), label, _category == category ? skin.ChipActive : skin.Chip) && _category != category)
                {
                    _category = category;
                    _scroll = Vector2.zero;
                    _nextRefresh = 0f;
                }

                x += w + 8f;
            }

            if (_view == null)
            {
                return;
            }

            // Header row.
            var table = new Rect(area.x, area.y + 52, area.width, area.height - 52);
            GUI.Label(new Rect(table.x + 16, table.y, 120, 20), GameTexts.Ranking.ColumnPosition, skin.SmallMuted);
            GUI.Label(new Rect(table.x + 140, table.y, 300, 20), GameTexts.Ranking.ColumnPlayer, skin.SmallMuted);
            GUI.Label(new Rect(table.xMax - 316, table.y, 300, 20), TabName(_category), skin.SmallMutedRight);

            var noteHeight = _view.LocalOnly ? 64f : 0f;
            var list = new Rect(table.x, table.y + 26, table.width, table.height - 26 - noteHeight);
            var content = new Rect(0, 0, list.width - 20, _view.Entries.Count * (RowHeight + 8f));
            _scroll = GUI.BeginScrollView(list, _scroll, content);
            var y = 0f;
            foreach (var entry in _view.Entries)
            {
                var row = new Rect(0, y, content.width, RowHeight);
                GUI.Box(row, GUIContent.none, entry.IsYou ? skin.CardSelected : skin.Card);
                GUI.Label(new Rect(row.x + 16, row.y + 14, 110, 30), "#" + entry.Position, skin.Heading);
                var name = entry.IsYou ? entry.PlayerName + "  ·  " + GameTexts.Ranking.You : entry.PlayerName;
                GUI.Label(new Rect(row.x + 140, row.y + 8, row.width - 480, 26), name, skin.BodyBold);
                GUI.Label(new Rect(row.x + 140, row.y + 32, 300, 20), GameTexts.Player.Level + " " + entry.FisherLevel, skin.SmallMuted);
                DrawValue(skin, new Rect(row.xMax - 316, row.y + 14, 300, 30), entry.Value);
                y += RowHeight + 8f;
            }

            GUI.EndScrollView();

            if (_view.LocalOnly)
            {
                var note = new Rect(table.x, table.yMax - noteHeight + 8, table.width, noteHeight - 8);
                skin.DrawIcon(new Rect(note.x + 2, note.y + 2, 18, 18), Icons.Info, UiSkin.Muted);
                GUI.Label(new Rect(note.x + 28, note.y, note.width - 28, note.height), GameTexts.Ranking.LocalNote, skin.SmallMuted);
            }
        }

        private void DrawValue(UiSkin skin, Rect rect, long value)
        {
            var text = _category == RankingCategory.Level ? GameTexts.Player.Level + " " + value : Format.Number(value);
            var icon = _category == RankingCategory.Coins ? Icons.Coin : _category == RankingCategory.Shells ? Icons.Shell : _category == RankingCategory.FishCaught ? Icons.Fish : null;
            GUI.Label(rect, text, skin.NumberRight);
            if (icon != null)
            {
                var w = skin.NumberRight.CalcSize(new GUIContent(text)).x;
                skin.DrawIcon(new Rect(rect.xMax - w - 30, rect.y + 3, 22, 22), icon, icon == Icons.Fish ? UiSkin.Accent : Color.white);
            }
        }

        private static string TabName(RankingCategory category)
        {
            switch (category)
            {
                case RankingCategory.Coins: return GameTexts.Ranking.TabCoins;
                case RankingCategory.Shells: return GameTexts.Ranking.TabShells;
                case RankingCategory.FishCaught: return GameTexts.Ranking.TabFish;
                default: return GameTexts.Ranking.TabLevel;
            }
        }
    }
}
