using System.Collections.Generic;
using System.Linq;
using FishingIdle.Game.Bootstrap;
using FishingIdle.Game.Visual;
using FishingIdle.GameService.Dev;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The owner's test tools (A-123): a floating panel, only in the Unity Editor or a development build,
    /// that asks the game service for currencies, fish, a level, Energy or more time. It never changes
    /// the save itself.
    /// </summary>
    public sealed class AdminWindow
    {
        private const int WindowId = 0x7E57;
        private readonly GameRoot _root;
        private Rect _rect = new Rect(24, 80, 460, 700);
        private Vector2 _scroll;
        private Vector2 _fishScroll;
        private string _search = string.Empty;
        private string _size;
        private int _level = 10;
        private IReadOnlyList<DevSpeciesView> _species;

        public AdminWindow(GameRoot root)
        {
            _root = root;
        }

        public bool IsOpen { get; private set; }

        public void Toggle()
        {
            IsOpen = !IsOpen && _root.DevToolsEnabled;
        }

        public void Draw(UiSkin skin, float screenWidth, float screenHeight)
        {
            if (!IsOpen || !_root.DevToolsEnabled)
            {
                return;
            }

            _rect.height = Mathf.Min(700f, screenHeight - 100f);
            _rect.x = Mathf.Clamp(_rect.x, 0, screenWidth - _rect.width);
            _rect.y = Mathf.Clamp(_rect.y, 0, screenHeight - 60f);
            _rect = GUI.Window(WindowId, _rect, id => Body(skin), GUIContent.none, skin.PanelSolid);
        }

        private void Body(UiSkin skin)
        {
            var w = _rect.width;
            GUI.Label(new Rect(20, 14, w - 140, 28), GameTexts.Dev.Title, skin.Heading);
            GUI.Label(new Rect(20, 40, w - 140, UiSkin.SmallLine), GameTexts.Dev.Subtitle, skin.SmallMuted);
            if (GUI.Button(new Rect(w - 110, 14, 90, 32), GameTexts.Dev.Close, skin.Button))
            {
                IsOpen = false;
            }

            var area = new Rect(16, 66, w - 32, _rect.height - 80);
            var inner = area.width - 18;
            _scroll = GUI.BeginScrollView(area, _scroll, new Rect(0, 0, inner, 1040));
            var y = 0f;

            // Currencies
            y = Section(skin, inner, y, GameTexts.Dev.Currencies);
            y = Row(skin, inner, y, Icons.Coin, GameTexts.Player.Coins, new[] { 10_000L, 1_000_000L, 100_000_000L }, DevCurrency.Coins);
            y = Row(skin, inner, y, Icons.Shell, GameTexts.Player.Shells, new[] { 100L, 1_000L, 10_000L }, DevCurrency.Shells);
            y = Row(skin, inner, y, Icons.Dollar, GameTexts.Player.Dollars, new[] { 10L, 100L, 1_000L }, DevCurrency.Dollars);
            y = Row(skin, inner, y, Icons.Honor, GameTexts.Arena.Honor, new[] { 100L, 500L, 5_000L }, DevCurrency.Honor);

            // Time
            y = Section(skin, inner, y + 6, GameTexts.Dev.Time);
            GUI.Label(new Rect(0, y, inner, 36), GameTexts.Dev.TimeNote, skin.SmallMuted);
            y += 40;
            y = TimeRow(skin, inner, y, GameTexts.Dev.Open, new[] { 1.0, 6.0 }, true);
            y = TimeRow(skin, inner, y, GameTexts.Dev.Closed, new[] { 1.0, 8.0, 24.0 }, false);

            // Level and Arena
            y = Section(skin, inner, y + 6, GameTexts.Dev.Level);
            var current = _root.Player != null ? _root.Player.FisherLevel : 1;
            var x = 0f;
            foreach (var step in new[] { -10, -1, 1, 10 })
            {
                if (GUI.Button(new Rect(x, y, 56, 32), (step > 0 ? "+" : "−") + Mathf.Abs(step), skin.Button))
                {
                    _root.DevSetLevel(current + step);
                }

                x += 62;
            }

            GUI.Label(new Rect(x + 6, y + 6, inner - x - 6, 22), GameTexts.Player.LevelShort + " " + current, skin.BodyBold);
            y += 40;
            // Level chips as wide as their label ("Nv. 100"), wrapping to a new line when the row is full.
            x = 0f;
            foreach (var level in new[] { 10, 30, 50, 70, 90, 100 })
            {
                var label = GameTexts.Player.LevelShort + " " + level;
                var lw = skin.Chip.CalcSize(new GUIContent(label)).x + 10f;
                if (x > 0f && x + lw > inner)
                {
                    x = 0f;
                    y += 38;
                }

                if (GUI.Button(new Rect(x, y, lw, 32), label, _level == level ? skin.ChipActive : skin.Chip))
                {
                    _level = level;
                    _root.DevSetLevel(level);
                }

                x += lw + 6f;
            }

            y += 44;
            y = Section(skin, inner, y, GameTexts.Dev.Arena);
            if (skin.IconButton(new Rect(0, y, 220, 34), Icons.Energy, GameTexts.Dev.FillEnergy, skin.Button))
            {
                _root.DevFillEnergy();
            }

            y += 46;

            // Fish
            y = Section(skin, inner, y, GameTexts.Dev.Fish);
            _search = NameSearch.Field(skin, new Rect(0, y, inner, 34), _search, "busca_teste");
            y += 42;

            // Size: random or a fixed category.
            x = 0f;
            var sizes = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>(null, GameTexts.Dev.RandomSize) };
            var config = _root.Game?.Session.Config;
            if (config != null)
            {
                sizes.AddRange(config.SizeCategories.Select(c => new KeyValuePair<string, string>(c.Id, c.DisplayName)));
            }

            foreach (var size in sizes)
            {
                var sw = skin.Chip.CalcSize(new GUIContent(size.Value)).x + 10f;
                if (x + sw > inner)
                {
                    x = 0f;
                    y += 36;
                }

                if (GUI.Button(new Rect(x, y, sw, 30), size.Value, _size == size.Key ? skin.ChipActive : skin.Chip))
                {
                    _size = size.Key;
                }

                x += sw + 6;
            }

            y += 40;
            _species = _species ?? _root.DevSpecies();
            var list = _species.Where(s => string.IsNullOrWhiteSpace(_search) || NameSearch.Matches(s.Name, _search)).ToList();
            var listRect = new Rect(0, y, inner, 1040 - y - 8);
            _fishScroll = GUI.BeginScrollView(listRect, _fishScroll, new Rect(0, 0, inner - 18, list.Count * 40f));
            for (var i = 0; i < list.Count; i++)
            {
                var s = list[i];
                var row = new Rect(0, i * 40f, inner - 18, 36);
                var color = UiSkin.RarityColor(s.RarityId);
                GUI.DrawTexture(new Rect(row.x + 4, row.y + 13, 10, 10), skin.White, ScaleMode.StretchToFill, true, 0, color, 0, 5);
                var textWidth = row.width - 20 - 3 * 58;
                GUI.Label(new Rect(row.x + 20, row.y, textWidth, 20), FishCard.Fit(s.Name, skin.SmallBold, textWidth), skin.SmallBold);
                GUI.Label(new Rect(row.x + 20, row.y + 18, textWidth, UiSkin.SmallLine), FishCard.Fit(s.RarityName + " · " + s.MapName, skin.SmallMuted, textWidth), skin.SmallMuted);
                var bx = row.xMax - 3 * 58;
                foreach (var count in new[] { 1, 5, 20 })
                {
                    if (GUI.Button(new Rect(bx, row.y + 3, 54, 30), "+" + count, skin.Chip))
                    {
                        _root.DevGiveFish(s.SpeciesId, s.Name, _size, count);
                    }

                    bx += 58;
                }
            }

            GUI.EndScrollView();
            GUI.EndScrollView();
            GUI.DragWindow(new Rect(0, 0, w, 60));
        }

        private static float Section(UiSkin skin, float width, float y, string title)
        {
            GUI.Label(new Rect(0, y, width, 24), title, skin.BodyBold);
            skin.Divider(new Rect(0, y + 26, width, 1));
            return y + 34;
        }

        private float Row(UiSkin skin, float width, float y, string icon, string label, long[] amounts, DevCurrency currency)
        {
            skin.DrawIcon(new Rect(0, y + 6, 22, 22), icon, Color.white);
            GUI.Label(new Rect(30, y + 7, 110, 22), label, skin.Small);
            // Short labels ("+100 mi"), and never narrower than the label itself.
            var x = 140f;
            var share = (width - x - (amounts.Length - 1) * 6f) / amounts.Length;
            foreach (var amount in amounts)
            {
                var text = "+" + Format.Short(amount);
                var bw = Mathf.Max(share, skin.Chip.CalcSize(new GUIContent(text)).x + 10f);
                if (GUI.Button(new Rect(x, y, bw, 32), text, skin.Chip))
                {
                    _root.DevGive(currency, amount, label);
                }

                x += bw + 6f;
            }

            return y + 40;
        }

        private float TimeRow(UiSkin skin, float width, float y, string label, double[] hours, bool online)
        {
            GUI.Label(new Rect(0, y + 7, 140, 22), label, skin.Small);
            var x = 140f;
            var bw = (width - x - 2 * 6f) / 3f;
            foreach (var h in hours)
            {
                if (GUI.Button(new Rect(x, y, bw, 32), GameTexts.Dev.Hours(h), skin.Chip))
                {
                    _root.DevAdvance(h, online);
                }

                x += bw + 6f;
            }

            return y + 40;
        }
    }
}
