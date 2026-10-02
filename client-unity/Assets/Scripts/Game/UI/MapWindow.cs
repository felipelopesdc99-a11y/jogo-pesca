using FishingIdle.Game.Bootstrap;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Maps;
using FishingIdle.Game.Scene;
using FishingIdle.Game.Visual;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>The Map menu (GDD section 18): the maps, their requirements, and the Viajar button.</summary>
    public sealed class MapWindow
    {
        private readonly GameRoot _root;
        private MapsView _maps;
        private float _nextRefresh;
        private Vector2 _scroll;
        private bool _scrollToCurrent;

        private const float CardHeight = 540f;
        private const float RowGap = 20f;

        public MapWindow(GameRoot root)
        {
            _root = root;
        }

        public bool IsOpen { get; private set; }

        public void Open()
        {
            IsOpen = true;
            _nextRefresh = 0f;
            _scrollToCurrent = true;
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
                _maps = _root.GetMaps();
                _nextRefresh = Time.unscaledTime + 0.5f;
            }

            if (_maps == null)
            {
                return;
            }

            var area = WindowFrame.Draw(skin, screenWidth, screenHeight, GameTexts.Map.Title,
                GameTexts.Map.TravelTime(Format.Duration(_maps.TravelSeconds)) + " · " + GameTexts.Map.TravelNote, out var closed, 1240f, 780f, Icons.Map);
            if (closed)
            {
                Close();
                return;
            }

            // Two cards per row; with more than two maps the rows scroll inside the window.
            var rows = (_maps.Maps.Count + 1) / 2;
            var contentHeight = 6f + rows * (CardHeight + RowGap);
            var scrolls = contentHeight > area.height;
            var cardWidth = (area.width - 20f - (scrolls ? 18f : 0f)) / 2f;
            if (_scrollToCurrent)
            {
                _scrollToCurrent = false;
                var current = _maps.Maps.FindIndex(m => m.IsCurrent);
                _scroll = new Vector2(0f, current > 1 ? current / 2 * (CardHeight + RowGap) : 0f);
            }

            var view = new Rect(0f, 0f, area.width - (scrolls ? 18f : 0f), contentHeight);
            _scroll = GUI.BeginScrollView(area, _scroll, view, false, scrolls);
            for (var i = 0; i < _maps.Maps.Count; i++)
            {
                var rect = new Rect(i % 2 * (cardWidth + 20f), 6f + i / 2 * (CardHeight + RowGap), cardWidth, CardHeight);
                DrawMap(skin, rect, _maps.Maps[i]);
            }

            GUI.EndScrollView();
        }

        private void DrawMap(UiSkin skin, Rect rect, MapView map)
        {
            GUI.Box(rect, GUIContent.none, map.IsCurrent ? skin.CardSelected : skin.Card);
            var x = rect.x + 22;
            var w = rect.width - 44;
            var y = rect.y + 16;

            // The map sells the place first (Art Bible, section 18): its picture on top.
            var thumb = ArtAssets.Texture(SceneTheme.For(map.MapId).ArtPath("thumb"));
            if (thumb != null)
            {
                var picture = new Rect(rect.x + 12, rect.y + 12, rect.width - 24, (rect.width - 24) / 2.5f);
                GUI.DrawTexture(picture, thumb, ScaleMode.ScaleAndCrop, true, 0, map.LevelUnlocked ? Color.white : new Color(0.55f, 0.6f, 0.7f, 1f), 0, 10);
                if (!map.LevelUnlocked)
                {
                    skin.DrawIcon(new Rect(picture.center.x - 22, picture.center.y - 22, 44, 44), Icons.Lock, Color.white);
                }

                y = picture.yMax + 14;
            }

            // Each map is a chapter of the journey (addendum A-085).
            var chapter = ArrivalTitle.ChapterOf(_root, map.MapId);
            if (chapter > 0)
            {
                GUI.Label(new Rect(x, y, w, 18), GameTexts.Map.Chapter(chapter), skin.SmallGold);
                y += 18;
            }

            GUI.Label(new Rect(x, y, w, 30), map.Name, skin.Heading);
            y += 32;
            var feeling = GameTexts.Map.Feeling(map.MapId);
            if (!string.IsNullOrEmpty(feeling))
            {
                GUI.Label(new Rect(x, y, w, 20), feeling, skin.SmallMuted);
                y += 22;
            }

            GUI.Label(new Rect(x, y, w, 50), map.Summary ?? string.Empty, skin.Small);
            y += 56;

            Row(skin, x, ref y, w, GameTexts.Map.NeedsLevel, GameTexts.Map.LevelRequirement(map.UnlockFisherLevel), map.LevelUnlocked);
            Row(skin, x, ref y, w, GameTexts.Map.NeedsRod, map.MinimumRodTier == 0 ? GameTexts.Map.AnyRod : map.MinimumRodName, map.RodAllowed);
            Row(skin, x, ref y, w, GameTexts.Map.Species, GameTexts.Map.Discovered(map.SpeciesDiscovered, map.SpeciesTotal), true);

            var button = new Rect(x, rect.yMax - 58, w, 42);
            var travel = _maps.Travel;
            if (travel.Active && travel.ToMapId == map.MapId)
            {
                skin.Bar(new Rect(x, button.y + 4, w, 10), (float)travel.Progress);
                GUI.Label(new Rect(x, button.y + 18, w, 22), GameTexts.Map.ArrivesIn(Format.Countdown(travel.SecondsLeft)), skin.SmallMuted);
            }
            else if (map.IsCurrent)
            {
                var label = GameTexts.Map.YouAreHere.ToUpperInvariant();
                skin.AccentPill(new Rect(x, button.y + 9, skin.PillWidth(label, true) + 6, 26), label, UiSkin.Accent, Icons.Pin);
            }
            else if (map.TravelBlocker != ServiceError.None)
            {
                skin.DrawIcon(new Rect(x, button.y + 11, 20, 20), Icons.Lock, UiSkin.Gold);
                GUI.Label(new Rect(x + 28, button.y + 2, w - 28, 42), GameTexts.ServiceErrorMessage(map.TravelBlocker.ToString()), skin.SmallGold);
            }
            else if (skin.IconButton(button, Icons.Arrow, GameTexts.Map.Travel, skin.ButtonPrimary))
            {
                _root.TravelTo(map.MapId);
                Close();
            }
        }

        private static void Row(UiSkin skin, float x, ref float y, float w, string label, string value, bool met)
        {
            GUI.Label(new Rect(x, y, w * 0.5f, 22), label, skin.SmallMuted);
            GUI.Label(new Rect(x + w * 0.5f, y, w * 0.5f, 22), value ?? string.Empty, met ? skin.SmallRight : skin.SmallGoldRight);
            y += 26;
        }
    }
}
