using FishingIdle.Game.Bootstrap;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Maps;
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

        public MapWindow(GameRoot root)
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
                _maps = _root.GetMaps();
                _nextRefresh = Time.unscaledTime + 0.5f;
            }

            if (_maps == null)
            {
                return;
            }

            var area = WindowFrame.Draw(skin, screenWidth, screenHeight, GameTexts.Map.Title,
                GameTexts.Map.TravelTime(Format.Duration(_maps.TravelSeconds)) + " · " + GameTexts.Map.TravelNote, out var closed, 1240f, 720f);
            if (closed)
            {
                Close();
                return;
            }

            var cardWidth = (area.width - 20f) / 2f;
            for (var i = 0; i < _maps.Maps.Count; i++)
            {
                var rect = new Rect(area.x + i % 2 * (cardWidth + 20f), area.y + 10 + i / 2 * 300f, cardWidth, 280f);
                DrawMap(skin, rect, _maps.Maps[i]);
            }
        }

        private void DrawMap(UiSkin skin, Rect rect, MapView map)
        {
            GUI.Box(rect, GUIContent.none, map.IsCurrent ? skin.CardSelected : skin.Card);
            var x = rect.x + 22;
            var w = rect.width - 44;
            var y = rect.y + 18;

            GUI.Label(new Rect(x, y, w, 30), map.Name, skin.Heading);
            y += 34;
            GUI.Label(new Rect(x, y, w, 64), map.Summary ?? string.Empty, skin.Small);
            y += 70;

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
                skin.Tag(new Rect(x, button.y + 10, 150, 24), GameTexts.Map.YouAreHere.ToUpperInvariant(), UiSkin.Accent);
            }
            else if (map.TravelBlocker != ServiceError.None)
            {
                GUI.Label(new Rect(x, button.y, w, 42), GameTexts.ServiceErrorMessage(map.TravelBlocker.ToString()), skin.SmallGold);
            }
            else if (GUI.Button(button, GameTexts.Map.Travel, skin.ButtonPrimary))
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
