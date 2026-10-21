using System.Collections.Generic;
using System.Linq;
using FishingIdle.Game.Bootstrap;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Maps;
using FishingIdle.Game.Scene;
using FishingIdle.Game.Visual;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The Map menu (GDD section 18) as an archipelago (addendum A-141): a sea from turquoise to night,
    /// one round island-medallion per map in a zigzag along a dotted boat route, and a panel with the
    /// chosen map's requirements and the Viajar button.
    /// </summary>
    /// <remarks>
    /// Presentation only: every state shown (current, unlocked, blocker, trip progress) comes from the
    /// map service's <see cref="MapsView"/>; the window never decides whether a trip is allowed.
    /// </remarks>
    public sealed class MapWindow
    {
        private readonly GameRoot _root;
        private MapsView _maps;
        private List<MapView> _ordered = new List<MapView>();
        private float _nextRefresh;
        private int _refreshFrame = -1;
        private string _selectedId;
        private string _tip;
        private float _tipWidth;

        private readonly Dictionary<string, string> _thumbPaths = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _islandPaths = new Dictionary<string, string>();
        private GUIStyle _nameStyle, _badgeStyle, _noteStyle, _tipStyle, _rowValue, _rowValueGold;
        private static Texture2D _sea;

        // Painted art (A-141, received 15/10/2026, A-181). Without a file the window keeps its drawn fallback.
        private const string SeaArt = "UI/ui_map_sea_bg";
        private const string BoatArt = "UI/ui_map_boat";
        private const string IslandArtPrefix = "UI/ui_map_island_";

        private const float LabelsHeight = 56f; // gap 6 + name 24 + gap 2 + state 24 (one line of the small text each, M25-T14; was 50)
        private const float RouteDotGap = 14f;

        private static readonly Color LockedFrame = new Color(0.45f, 0.52f, 0.60f);
        private static readonly Color LockedTint = new Color(0.40f, 0.45f, 0.55f, 1f);
        private static readonly Color Ink = new Color(0.04f, 0.16f, 0.20f);

        public MapWindow(GameRoot root)
        {
            _root = root;
        }

        public bool IsOpen { get; private set; }

        public void Open()
        {
            IsOpen = true;
            _nextRefresh = 0f;
            _selectedId = null;
        }

        public void Close() => IsOpen = false;

        /// <summary>The Map has no confirmation dialog of its own.</summary>
        public bool HasDialog => false;

        public void Draw(UiSkin skin, float screenWidth, float screenHeight)
        {
            if (!IsOpen)
            {
                return;
            }

            // Every frame while the boat is on its way (so it glides), twice a second otherwise.
            var traveling = _maps != null && _maps.Travel != null && _maps.Travel.Active;
            if (Time.unscaledTime >= _nextRefresh || (traveling && Time.frameCount != _refreshFrame))
            {
                _maps = _root.GetMaps();
                _nextRefresh = Time.unscaledTime + 0.5f;
                _refreshFrame = Time.frameCount;
                // The journey order of the config: by the Fisher level that unlocks each map.
                _ordered = _maps != null ? _maps.Maps.OrderBy(m => m.UnlockFisherLevel).ToList() : new List<MapView>();
            }

            if (_maps == null)
            {
                return;
            }

            // A-148: the travel time stays in sight next to the sign; the explanation goes behind the "i".
            var area = WindowFrame.Draw(skin, screenWidth, screenHeight, GameTexts.Map.Title, GameTexts.Map.TravelNote, out var closed, 1320f, 840f, Icons.Map,
                GameTexts.Map.TravelTime(Format.Duration(_maps.TravelSeconds)));
            if (closed)
            {
                Close();
                return;
            }

            // The archipelago is the window's own scene, framed like a photo over its dark sea.
            WindowFrame.Photo(skin, area);
            EnsureStyles(skin);
            _tip = null;

            var selected = Selected();
            DrawSea(skin, area);

            var panelHeight = Mathf.Clamp(area.height * 0.24f, 136f, 160f);
            var panel = new Rect(area.x + 12f, area.yMax - panelHeight - 12f, area.width - 24f, panelHeight);
            var sea = new Rect(area.x, area.y, area.width, panel.y - 4f - area.y);

            if (_ordered.Count > 0)
            {
                DrawArchipelago(skin, sea, selected);
            }

            if (selected != null)
            {
                DrawPanel(skin, panel, selected);
            }

            DrawTip(skin, area);
        }

        // ------------------------------------------------------------------ selection

        /// <summary>The chosen map; on opening, the trip's destination or the map the player is on.</summary>
        private MapView Selected()
        {
            var found = _selectedId != null ? _ordered.Find(m => m.MapId == _selectedId) : null;
            if (found != null)
            {
                return found;
            }

            var travel = _maps.Travel;
            found = travel != null && travel.Active ? _ordered.Find(m => m.MapId == travel.ToMapId) : null;
            found = found ?? _ordered.Find(m => m.IsCurrent) ?? (_ordered.Count > 0 ? _ordered[0] : null);
            _selectedId = found?.MapId;
            return found;
        }

        // ------------------------------------------------------------------ sea and route

        private static void DrawSea(UiSkin skin, Rect area)
        {
            // The sea seen from above (L7-03) at the smallest whole scale that fills the chart, cropped at the edges
            // (M25-T14); the ARC frame around it is WindowFrame.Photo. The generated gradient stays only without the file.
            var painted = ArtAssets.Texture(SeaArt);
            if (painted != null)
            {
                ArcArt.Cover(area, painted, Color.white);
                return;
            }

            if (_sea == null)
            {
                _sea = HorizontalGradient(new[]
                {
                    new Color(0.498f, 0.847f, 0.894f),
                    new Color(0.184f, 0.576f, 0.769f),
                    new Color(0.090f, 0.302f, 0.525f),
                    new Color(0.047f, 0.141f, 0.282f),
                    new Color(0.020f, 0.047f, 0.114f),
                }, new[] { 0f, 0.35f, 0.65f, 0.85f, 1f });
            }

            GUI.DrawTexture(area, _sea, ScaleMode.StretchToFill, true, 0, Color.white, 0, 0);

            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            // A few still stars over the night end of the sea (decoration only).
            for (var i = 0; i < 46; i++)
            {
                var x = area.x + area.width * (0.70f + 0.29f * Hash(i * 2 + 1));
                var y = area.y + 8f + (area.height - 16f) * Hash(i * 2 + 2);
                var size = 1.5f + 2f * Hash(i * 7 + 3);
                var alpha = 0.25f + 0.5f * Hash(i * 5 + 4) * Mathf.InverseLerp(area.x + area.width * 0.70f, area.xMax, x);
                ArcArt.Fill(skin, new Rect(x, y, size, size), new Color(1f, 1f, 1f, alpha), size / 2f);
            }
        }

        private static float Hash(int n)
        {
            var v = Mathf.Sin(n * 12.9898f) * 43758.5453f;
            return v - Mathf.Floor(v);
        }

        private static Texture2D HorizontalGradient(Color[] colors, float[] stops)
        {
            const int width = 256;
            var tex = new Texture2D(width, 1, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };
            for (var x = 0; x < width; x++)
            {
                var t = x / (float)(width - 1);
                var k = 1;
                while (k < stops.Length - 1 && t > stops[k])
                {
                    k++;
                }

                var u = Mathf.InverseLerp(stops[k - 1], stops[k], t);
                tex.SetPixel(x, 0, Color.Lerp(colors[k - 1], colors[k], u));
            }

            tex.Apply();
            return tex;
        }

        /// <summary>Lays out the medallions in a zigzag inside <paramref name="sea"/> and draws route, medallions and boat.</summary>
        private void DrawArchipelago(UiSkin skin, Rect sea, MapView selected)
        {
            var count = _ordered.Count;
            const float pad = 10f;

            // Size from the room available; columns get a margin so the first and last medallions stay inside.
            var step = sea.width / count;
            var diameter = Mathf.Clamp(Mathf.Min((sea.height - pad * 2f - LabelsHeight * 2f - 14f) / 2f, step * 1.2f), 56f, 150f);
            var margin = Mathf.Max(10f, diameter / 2f + 16f - step / 2f);
            step = (sea.width - margin * 2f) / count;
            diameter = Mathf.Min(diameter, step * 1.2f);

            var rowDistance = Mathf.Max(diameter * 0.5f, Mathf.Min(sea.height - pad * 2f - LabelsHeight - diameter, diameter + LabelsHeight + 40f));
            var band = rowDistance + diameter + LabelsHeight;
            var top = sea.y + Mathf.Max(pad, (sea.height - band) / 2f) + diameter / 2f;

            var points = new Vector2[count];
            for (var i = 0; i < count; i++)
            {
                points[i] = new Vector2(sea.x + margin + step * (i + 0.5f), top + (i % 2 == 0 ? 0f : rowDistance));
            }

            var current = _ordered.FindIndex(m => m.IsCurrent);
            var bend = step * 0.45f;
            DrawRoute(skin, points, current, bend);

            for (var i = 0; i < count; i++)
            {
                DrawMedallion(skin, _ordered[i], i, points[i], diameter, step, _ordered[i] == selected);
            }

            // The boat: moving along the route during a trip, otherwise beside the map the player is on.
            var boat = Mathf.Clamp(diameter * 0.42f, 34f, 56f);
            var bob = Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / 3.5f) * 2f;
            var travel = _maps.Travel;
            var destination = travel != null && travel.Active ? _ordered.FindIndex(m => m.MapId == travel.ToMapId) : -1;
            if (current >= 0 && destination >= 0)
            {
                var t = Mathf.Lerp(current, destination, (float)travel.Progress);
                var at = PathPoint(points, t, bend);
                // The painted boat points its bow along the route (it faces right in the file).
                var ahead = PathPoint(points, Mathf.Min(t + 0.02f, count - 1f), bend);
                var angle = (ahead - at).sqrMagnitude > 0.01f ? Mathf.Atan2(ahead.y - at.y, ahead.x - at.x) * Mathf.Rad2Deg : 0f;
                if (destination < current)
                {
                    // Sailing back along the route: the bow faces the way the boat goes.
                    ahead = PathPoint(points, Mathf.Max(t - 0.02f, 0f), bend);
                    angle = (ahead - at).sqrMagnitude > 0.01f ? Mathf.Atan2(ahead.y - at.y, ahead.x - at.x) * Mathf.Rad2Deg : 180f;
                }

                DrawBoat(skin, new Rect(at.x - boat / 2f, at.y - boat / 2f + bob, boat, boat), angle);
            }
            else if (current >= 0)
            {
                var c = points[current];
                // The painted hull is drawn a little larger than the slot (see DrawBoat): keep the extra clear of the island.
                var spill = ArtAssets.Texture(BoatArt) != null ? boat * 0.13f : 0f;
                var x = c.x + diameter / 2f + 6f + spill;
                if (x + boat + spill > sea.xMax - 6f)
                {
                    x = c.x - diameter / 2f - 6f - boat - spill;
                }

                DrawBoat(skin, new Rect(x, c.y - boat * 0.2f + bob, boat, boat), 0f);
            }
        }

        private static void DrawRoute(UiSkin skin, Vector2[] points, int current, float bend)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            for (var i = 1; i < points.Length; i++)
            {
                // Travelled stretch (up to the map the player is on) bright; the rest faded.
                var color = new Color(1f, 1f, 1f, i <= current ? 0.95f : 0.3f);
                var dots = Mathf.Max(2, Mathf.CeilToInt(Vector2.Distance(points[i - 1], points[i]) * 1.2f / RouteDotGap));
                for (var k = 0; k <= dots; k++)
                {
                    var p = Bezier(points[i - 1], points[i], k / (float)dots, bend);
                    // A 2 × 2 art-pixel dot on the art-pixel grid.
                    var dx = Mathf.Round(p.x / UiSkin.ArcPixel) * UiSkin.ArcPixel;
                    var dy = Mathf.Round(p.y / UiSkin.ArcPixel) * UiSkin.ArcPixel;
                    ArcArt.Fill(skin, new Rect(dx - UiSkin.ArcPixel, dy - UiSkin.ArcPixel, UiSkin.ArcPixel * 2, UiSkin.ArcPixel * 2), color, 0f);
                }
            }
        }

        private static Vector2 Bezier(Vector2 a, Vector2 b, float t, float bend)
        {
            var c1 = new Vector2(a.x + bend, a.y);
            var c2 = new Vector2(b.x - bend, b.y);
            var u = 1f - t;
            return u * u * u * a + 3f * u * u * t * c1 + 3f * u * t * t * c2 + t * t * t * b;
        }

        /// <summary>A point on the route at a fractional map index (1,5 = halfway between maps 2 and 3).</summary>
        private static Vector2 PathPoint(Vector2[] points, float t, float bend)
        {
            if (points.Length == 1)
            {
                return points[0];
            }

            var i = Mathf.Clamp(Mathf.FloorToInt(t), 0, points.Length - 2);
            return Bezier(points[i], points[i + 1], Mathf.Clamp01(t - i), bend);
        }

        private static void DrawBoat(UiSkin skin, Rect rect, float angle)
        {
            // The painted boat seen from above (bow to the right), turned to its heading; the tinted icon without it.
            var art = ArtAssets.Texture(BoatArt);
            if (art != null)
            {
                if (Event.current.type != EventType.Repaint)
                {
                    return;
                }

                var size = rect.width * 1.25f; // the hull is about 95 % wide and 55 % tall in the 128 px file
                var drawn = new Rect(rect.center.x - size / 2f, rect.center.y - size / 2f, size, size);
                if (ArtAssets.IsPixel(art))
                {
                    // Pixel art never turns (ART_BIBLE_PIXEL 4.9): heading left is a mirror image instead; whole scale,
                    // a hard shadow one art pixel down and right, the bob in whole art pixels.
                    var flip = Mathf.Cos(angle * Mathf.Deg2Rad) < -0.2f;
                    drawn.y = Mathf.Round(drawn.y / UiSkin.ArcPixel) * UiSkin.ArcPixel;
                    ArcArt.Fit(new Rect(drawn.x + UiSkin.ArcPixel, drawn.y + UiSkin.ArcPixel, size, size), art, new Color(0f, 0f, 0f, 0.3f), flip);
                    ArcArt.Fit(drawn, art, Color.white, flip);
                    return;
                }

                var matrix = GUI.matrix;
                if (Mathf.Abs(angle) > 0.5f)
                {
                    GUIUtility.RotateAroundPivot(angle, drawn.center);
                }

                PixelDraw.Fit(new Rect(drawn.x + 2f, drawn.y + 3f, size, size), art, new Color(0f, 0f, 0f, 0.3f));
                PixelDraw.Fit(drawn, art);
                GUI.matrix = matrix;
                return;
            }

            skin.DrawIcon(new Rect(rect.x + 2f, rect.y + 3f, rect.width, rect.height), Icons.Boat, new Color(0f, 0f, 0f, 0.35f));
            skin.DrawIcon(rect, Icons.Boat, Color.white);
        }

        // ------------------------------------------------------------------ medallions

        private void DrawMedallion(UiSkin skin, MapView map, int index, Vector2 center, float d, float step, bool selected)
        {
            var circle = new Rect(center.x - d / 2f, center.y - d / 2f, d, d);
            var hovered = circle.Contains(Event.current.mousePosition);
            var locked = !map.LevelUnlocked;
            var frame = map.IsCurrent ? UiSkin.Gold : locked ? LockedFrame : UiSkin.Accent;

            // The pixel island (L3-08) brings its own rocky rim: no drawn frame around it; the current map has the slow
            // gold pixel ring (3,5 s, calm), the chosen one the turquoise ring (Art Bible 4.10: rings, no soft halo).
            var pixelIsland = PixelIsland(map.MapId);
            if (!pixelIsland)
            {
                ArcArt.Fill(skin, new Rect(center.x - d * 0.42f, center.y + d * 0.42f, d * 0.84f, d * 0.14f), new Color(0f, 0f, 0f, 0.22f), 0f);
            }

            if (map.IsCurrent)
            {
                var pulse = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / 3.5f);
                Ring(skin, circle, UiSkin.ArcPixel * 4, new Color(UiSkin.GoldLight.r, UiSkin.GoldLight.g, UiSkin.GoldLight.b, 0.85f * pulse));
            }

            if (selected)
            {
                Ring(skin, circle, UiSkin.ArcPixel * 2, UiSkin.Accent);
            }
            else if (hovered)
            {
                Ring(skin, circle, UiSkin.ArcPixel * 2, new Color(1f, 1f, 1f, 0.45f));
            }

            DrawPicture(skin, map, circle, locked);
            if (!pixelIsland)
            {
                Ring(skin, circle, 0f, frame);
            }

            // Number medal (turquoise, gold or grey).
            var r = Mathf.Clamp(d * 0.14f, 12f, 18f);
            var medal = new Rect(center.x - d * 0.36f - r, center.y - d * 0.36f - r, r * 2f, r * 2f);
            ArcArt.Disc(skin, medal, frame, UiSkin.DarkInk);
            var previous = GUI.contentColor;
            GUI.contentColor = locked ? Color.white : Ink;
            GUI.Label(medal, (index + 1).ToString(), _badgeStyle);
            GUI.contentColor = previous;

            // Name (shortened with a tooltip when it does not fit) and the state line.
            var labelWidth = Mathf.Min(step * 2f - 16f, 220f);
            var name = FishCard.Fit(map.Name, _nameStyle, labelWidth - 14f);
            var nameWidth = _nameStyle.CalcSize(new GUIContent(name)).x + 14f;
            var nameRect = new Rect(center.x - nameWidth / 2f, circle.yMax + 6f, nameWidth, UiSkin.SmallLine);
            skin.PixelPill(nameRect, new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.78f), Color.clear);
            GUI.Label(nameRect, name, _nameStyle);
            if (name != map.Name && (hovered || nameRect.Contains(Event.current.mousePosition)))
            {
                ShowTip(map.Name, 320f);
            }

            var state = locked ? GameTexts.Map.LevelShort(map.UnlockFisherLevel) : map.IsCurrent ? GameTexts.Map.YouAreHere : null;
            if (state != null)
            {
                var stateRect = new Rect(center.x - labelWidth / 2f, nameRect.yMax + 2f, labelWidth, UiSkin.SmallLine);
                var stateWidth = skin.SmallGoldCenter.CalcSize(new GUIContent(state)).x + 14f;
                skin.PixelPill(new Rect(center.x - stateWidth / 2f, stateRect.y, stateWidth, stateRect.height), new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.68f), Color.clear);
                GUI.Label(stateRect, state, skin.SmallGoldCenter);
            }

            if (GUI.Button(circle, GUIContent.none, GUIStyle.none))
            {
                _selectedId = map.MapId;
            }
        }

        /// <summary>The map's painted thumbnail cut into a circle (dimmed when locked, with a padlock).</summary>
        private void DrawPicture(UiSkin skin, MapView map, Rect circle, bool locked)
        {
            // The painted round island of the map (A-181) replaces the cropped thumbnail; the frame, number and padlock
            // stay drawn by the game on top of it.
            var radius = circle.width / 2f;
            var islandPath = IslandPath(map.MapId);
            var island = islandPath.Length > 0 ? ArtAssets.Texture(islandPath) : null;
            var thumb = island == null ? ArtAssets.Texture(ThumbPath(map.MapId)) : null;
            if (island != null && ArtAssets.IsPixel(island))
            {
                // The pixel island at the largest whole scale in the circle, greyed when locked, with the padlock.
                PixelDraw.Fit(circle, island, locked ? LockedTint : Color.white);
                if (locked)
                {
                    var lockSize = circle.width * 0.3f;
                    skin.DrawIcon(new Rect(circle.center.x - lockSize / 2f, circle.center.y - lockSize / 2f, lockSize, lockSize), Icons.Lock, Color.white);
                }

                return;
            }

            if (island != null)
            {
                // Water under the island's transparent shore, so the round frame always closes on a full disc.
                ArcArt.Fill(skin, circle, new Color(0.10f, 0.42f, 0.62f, 1f), radius);
                PixelDraw.Fit(circle, island, locked ? LockedTint : Color.white);
            }
            else if (thumb != null)
            {
                GUI.DrawTexture(circle, thumb, ScaleMode.ScaleAndCrop, true, 0, locked ? LockedTint : Color.white, 0, radius);
            }
            else
            {
                ArcArt.Fill(skin, circle, Color.Lerp(UiSkin.Accent, UiSkin.Night, locked ? 0.7f : 0.35f), radius);
            }

            if (locked)
            {
                ArcArt.Fill(skin, circle, new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.3f), radius);
                var s = circle.width * 0.3f;
                skin.DrawIcon(new Rect(circle.center.x - s / 2f, circle.center.y - s / 2f, s, s), Icons.Lock, Color.white);
            }
        }

        /// <summary>Whether the map's island is pixel art (ArtePixel, L3-08).</summary>
        private bool PixelIsland(string mapId)
        {
            var path = IslandPath(mapId);
            return path.Length > 0 && ArtAssets.IsPixel(ArtAssets.Texture(path));
        }

        /// <summary>"map_03" → "UI/ui_map_island_03" (cached, so no string is built per frame).</summary>
        private string IslandPath(string mapId)
        {
            if (!_islandPaths.TryGetValue(mapId, out var path))
            {
                var cut = mapId.LastIndexOf('_');
                path = cut >= 0 && int.TryParse(mapId.Substring(cut + 1), out var number) ? IslandArtPrefix + number.ToString("00") : string.Empty;
                _islandPaths[mapId] = path;
            }

            return path;
        }

        private string ThumbPath(string mapId)
        {
            if (!_thumbPaths.TryGetValue(mapId, out var path))
            {
                path = SceneTheme.For(mapId).ArtPath("thumb");
                _thumbPaths[mapId] = path;
            }

            return path;
        }

        /// <summary>A circular ring <paramref name="grow"/> px outside <paramref name="circle"/>.</summary>
        /// <summary>A one-art-pixel pixel circle <paramref name="grow"/> outside <paramref name="circle"/>.</summary>
        private static void Ring(UiSkin skin, Rect circle, float grow, Color color)
        {
            var rect = new Rect(circle.x - grow, circle.y - grow, circle.width + grow * 2f, circle.height + grow * 2f);
            ArcArt.Line(skin, rect, color, rect.width / 2f);
        }

        // ------------------------------------------------------------------ chosen map panel

        private void DrawPanel(UiSkin skin, Rect panel, MapView map)
        {
            GUI.Box(panel, GUIContent.none, skin.PanelSolid);
            var inner = new Rect(panel.x + 18f, panel.y + 14f, panel.width - 36f, panel.height - 28f);
            const float gap = 24f;
            var leftWidth = inner.width * 0.40f;
            var midWidth = inner.width * 0.32f;
            var rightWidth = inner.width - leftWidth - midWidth - gap * 2f;

            // Left: the medallion, chapter, name and the map's feeling.
            var thumbSize = Mathf.Min(inner.height, 104f);
            var thumb = new Rect(inner.x, inner.center.y - thumbSize / 2f, thumbSize, thumbSize);
            var locked = !map.LevelUnlocked;
            DrawPicture(skin, map, thumb, locked);
            if (!PixelIsland(map.MapId))
            {
                Ring(skin, thumb, 0f, map.IsCurrent ? UiSkin.Gold : locked ? LockedFrame : UiSkin.Accent);
            }

            var tx = thumb.xMax + 16f;
            var tw = inner.x + leftWidth - tx;
            var ty = inner.center.y - 34f;
            var chapter = ArrivalTitle.ChapterOf(_root, map.MapId);
            if (chapter > 0)
            {
                GUI.Label(new Rect(tx, ty - 1f, tw, UiSkin.SmallLine), GameTexts.Map.Chapter(chapter), skin.SmallGoldLine);
            }

            var name = FishCard.Fit(map.Name, skin.Heading, tw);
            GUI.Label(new Rect(tx, ty + 18f, tw, 28f), name, skin.Heading);
            var feeling = GameTexts.Map.Feeling(map.MapId);
            if (!string.IsNullOrEmpty(feeling))
            {
                GUI.Label(new Rect(tx, ty + 48f, tw, UiSkin.SmallLine), FishCard.Fit(feeling, skin.SmallMuted, tw), skin.SmallMuted);
            }

            // Hovering the left block shows the full name (when cut) and the map's description.
            var left = new Rect(inner.x, inner.y, leftWidth, inner.height);
            if (left.Contains(Event.current.mousePosition))
            {
                var summary = map.Summary ?? string.Empty;
                var tip = name != map.Name ? (summary.Length > 0 ? map.Name + "\n" + summary : map.Name) : summary;
                if (tip.Length > 0)
                {
                    ShowTip(tip, 380f);
                }
            }

            // Middle: the requirements.
            var mx = inner.x + leftWidth + gap;
            var my = inner.center.y - 39f;
            Row(skin, mx, ref my, midWidth, GameTexts.Map.NeedsLevel, GameTexts.Map.LevelRequirement(map.UnlockFisherLevel), map.LevelUnlocked);
            skin.Divider(new Rect(mx, my - 3f, midWidth, 1f));
            // The rod is information, not a lock (M24-T58, A-180): which rarities of this map the rod in use cannot catch.
            var misses = map.RaritiesRodMisses.Count == 0 ? GameTexts.Map.RodCatchesAll : GameTexts.Map.RodMisses(string.Join(", ", map.RaritiesRodMisses));
            Row(skin, mx, ref my, midWidth, GameTexts.Map.RodHere, misses, map.RaritiesRodMisses.Count == 0);
            skin.Divider(new Rect(mx, my - 3f, midWidth, 1f));
            Row(skin, mx, ref my, midWidth, GameTexts.Map.Species, GameTexts.Map.Discovered(map.SpeciesDiscovered, map.SpeciesTotal), true);

            // Right: Viajar, the trip in progress, "Você está aqui" or why the trip is blocked.
            DrawAction(skin, new Rect(inner.xMax - rightWidth, inner.y, rightWidth, inner.height), map);
        }

        private void DrawAction(UiSkin skin, Rect rect, MapView map)
        {
            var travel = _maps.Travel;
            if (travel != null && travel.Active && travel.ToMapId == map.MapId)
            {
                var y = rect.center.y - 30f;
                GUI.Label(new Rect(rect.x, y, rect.width, UiSkin.SmallLine), FishCard.Fit(GameTexts.Map.Traveling(map.Name), skin.SmallBold, rect.width), skin.SmallBold);
                skin.Bar(new Rect(rect.x, y + 24f, rect.width, 10f), (float)travel.Progress);
                GUI.Label(new Rect(rect.x, y + 40f, rect.width, UiSkin.SmallLine), GameTexts.Map.ArrivesIn(Format.Countdown(travel.SecondsLeft)), skin.SmallMuted);
                return;
            }

            if (map.IsCurrent)
            {
                var label = GameTexts.Map.YouAreHere;
                var width = Mathf.Min(rect.width, skin.PillWidth(label, true) + 6f);
                skin.AccentPill(new Rect(rect.center.x - width / 2f, rect.center.y - 14f, width, 28f), label, UiSkin.Accent, Icons.Pin);
                return;
            }

            if (map.TravelBlocker != ServiceError.None)
            {
                var message = GameTexts.ServiceErrorMessage(map.TravelBlocker.ToString());
                var noteHeight = Mathf.Min(_noteStyle.CalcHeight(new GUIContent(message), rect.width), rect.height - 50f);
                var y = rect.center.y - (42f + 6f + noteHeight) / 2f;
                var enabled = GUI.enabled;
                GUI.enabled = false;
                skin.IconButton(new Rect(rect.x, y, rect.width, 42f), Icons.Lock, GameTexts.Map.Locked, skin.Button);
                GUI.enabled = enabled;
                GUI.Label(new Rect(rect.x, y + 48f, rect.width, noteHeight), message, _noteStyle);
                return;
            }

            if (skin.IconButton(new Rect(rect.x, rect.center.y - 22f, rect.width, 44f), Icons.Arrow, GameTexts.Map.TravelWithTime(Format.Duration(_maps.TravelSeconds)), skin.ButtonPrimary))
            {
                _root.TravelTo(map.MapId);
                Close();
            }
        }

        private void Row(UiSkin skin, float x, ref float y, float w, string label, string value, bool met)
        {
            GUI.Label(new Rect(x, y, w * 0.5f, UiSkin.SmallLine), FishCard.Fit(label, skin.SmallMuted, w * 0.5f - 4f), skin.SmallMuted);
            var style = met ? _rowValue : _rowValueGold;
            GUI.Label(new Rect(x + w * 0.5f, y, w * 0.5f, UiSkin.SmallLine), FishCard.Fit(value ?? string.Empty, style, w * 0.5f), style);
            y += 26f;
        }

        // ------------------------------------------------------------------ tooltip and styles

        private void ShowTip(string text, float maxWidth)
        {
            _tip = text;
            _tipWidth = maxWidth;
        }

        private void DrawTip(UiSkin skin, Rect bounds)
        {
            if (_tip == null)
            {
                return;
            }

            var content = new GUIContent(_tip);
            var width = Mathf.Min(_tipWidth, _tipStyle.CalcSize(content).x) + 20f;
            var height = _tipStyle.CalcHeight(content, width - 20f) + 14f;
            var mouse = Event.current.mousePosition;
            var x = Mathf.Clamp(mouse.x + 14f, bounds.x, bounds.xMax - width);
            var y = mouse.y + 20f + height > bounds.yMax ? mouse.y - height - 8f : mouse.y + 20f;
            var rect = new Rect(x, y, width, height);
            skin.TipBox(rect);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 7f, rect.width - 20f, rect.height - 14f), _tip, _tipStyle);
        }

        private void EnsureStyles(UiSkin skin)
        {
            if (_nameStyle != null)
            {
                return;
            }

            _nameStyle = new GUIStyle(skin.SmallBold) { alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Clip };
            _badgeStyle = new GUIStyle(skin.SmallBold) { fontSize = UiSkin.ChipSize, alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Overflow };
            _noteStyle = new GUIStyle(skin.SmallMuted) { alignment = TextAnchor.UpperCenter, wordWrap = true };
            _tipStyle = new GUIStyle(skin.Small) { wordWrap = true };
            _rowValue = new GUIStyle(skin.SmallRight) { wordWrap = false, clipping = TextClipping.Clip };
            _rowValueGold = new GUIStyle(skin.SmallGoldRight) { wordWrap = false, clipping = TextClipping.Clip };
        }
    }
}
