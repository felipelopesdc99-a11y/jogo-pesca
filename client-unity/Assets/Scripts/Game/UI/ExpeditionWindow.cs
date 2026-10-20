using FishingIdle.Game.Bootstrap;
using FishingIdle.Game.Scene;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Expeditions;
using FishingIdle.Game.Visual;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The Expedition menu (GDD section 32). When the Cardume is back, the report of what it brought
    /// opens on top of this window the next time the player opens it, and goes away once read
    /// (addendum A-082).
    /// </summary>
    /// <remarks>
    /// With the owner's art (addendum A-147) the menu is a nautical chart: the pier in a corner, the destinations as
    /// medallions farther from it the longer they take, a dotted route to each, and a mission card on the right with
    /// the chosen one. Away, the Cardume marker walks the route (the progress the service reports) and the card
    /// shows a compass with the time left. The report is drawn over the harbour at dusk. Without the chart the
    /// menu falls back to the four cards side by side.
    /// </remarks>
    public sealed class ExpeditionWindow
    {
        private const string ChartArt = "UI/ui_exp_carta", HarbourArt = "UI/ui_exp_porto", RoseArt = "UI/ui_exp_rosa",
            MedallionArt = "UI/ui_exp_medalhao", PierArt = "UI/ui_exp_pier", MarkerArt = "UI/ui_exp_marcador",
            SealArt = "UI/ui_exp_selo", CompassArt = "UI/ui_exp_bussola";

        // Measured on the pixel art (L3-06, M25-T14): the see-through hole of the 50 px medallion is 28 px across and its
        // brass ring reaches 23 px from the centre, so a picture square of 30 art px fills the hole and hides its corners
        // under the ring; the compass dial (centre and size as shares of the 48 × 50 picture).
        private const float MedallionPicture = 30f / 50f;
        private const float DialX = 0.5f, DialY = 0.66f, DialW = 0.46f, DialH = 0.44f;

        private static readonly Color Brass = new Color(0.79f, 0.65f, 0.42f);
        private static readonly Color DimTint = new Color(0.45f, 0.50f, 0.58f, 1f);
        private static readonly Color Ink = new Color(0.04f, 0.16f, 0.20f);

        private readonly GameRoot _root;
        private ExpeditionsView _view;
        private float _nextRefresh;
        private bool _confirmCancel;
        private string _selectedId;
        private Vector2[] _points = new Vector2[0];
        private GUIStyle _nameStyle, _sealStyle, _dialStyle;

        public ExpeditionWindow(GameRoot root)
        {
            _root = root;
        }

        public bool IsOpen { get; private set; }

        /// <summary>True while the cancel confirmation or the report of a returned Expedition is open.</summary>
        public bool HasDialog => _confirmCancel || (IsOpen && _root.ExpeditionResult != null);

        public void Open()
        {
            IsOpen = true;
            _nextRefresh = 0f;
        }

        public void Close()
        {
            if (_confirmCancel)
            {
                _confirmCancel = false;
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

            if (Time.unscaledTime >= _nextRefresh)
            {
                _view = _root.GetExpeditions();
                _nextRefresh = Time.unscaledTime + 0.5f;
            }

            if (_view == null)
            {
                return;
            }

            EnsureStyles(skin);

            // With a report waiting, the menu underneath is shown but inert until it is read.
            var report = _root.ExpeditionResult;
            var wasEnabled = GUI.enabled;
            GUI.enabled = wasEnabled && report == null && !_confirmCancel;
            var area = WindowFrame.Draw(skin, screenWidth, screenHeight, GameTexts.Expedition.Title, GameTexts.Expedition.Note, out var closed, 1320f, 760f, Icons.Expedition);
            if (closed)
            {
                GUI.enabled = wasEnabled;
                Close();
                return;
            }

            DrawStrength(skin);

            var chart = ArtAssets.Texture(ChartArt);
            if (chart != null && _view.Expeditions.Count > 0)
            {
                DrawChartMenu(skin, area, chart);
            }
            else
            {
                DrawCardsMenu(skin, area);
            }

            GUI.enabled = wasEnabled;
            if (report != null)
            {
                DrawReport(skin, screenWidth, screenHeight, report);
            }
            else if (_confirmCancel)
            {
                if (_view.Active == null)
                {
                    _confirmCancel = false;
                    return;
                }

                var dialog = WindowFrame.Dialog(skin, screenWidth, screenHeight, 220f);
                GUI.Label(new Rect(dialog.x + 28, dialog.y + 24, dialog.width - 56, 30), GameTexts.Expedition.CancelTitle, skin.Heading);
                GUI.Label(new Rect(dialog.x + 28, dialog.y + 64, dialog.width - 56, 70), GameTexts.Expedition.CancelBody, skin.Body);
                if (GUI.Button(new Rect(dialog.x + 28, dialog.yMax - 64, 190, 42), GameTexts.Expedition.KeepGoing, skin.Button))
                {
                    _confirmCancel = false;
                }

                if (GUI.Button(new Rect(dialog.xMax - 238, dialog.yMax - 64, 210, 42), GameTexts.Expedition.Cancel, skin.ButtonDanger))
                {
                    _confirmCancel = false;
                    _root.CancelExpedition();
                    _nextRefresh = 0f;
                }
            }
        }

        /// <summary>
        /// "Força do seu Cardume" and x / 6 in a dark inset next to the header sign (A-148), where the old header
        /// had its subtitle.
        /// </summary>
        private void DrawStrength(UiSkin skin)
        {
            var value = Format.Number(_view.CardumeStrength) + "  ·  " + GameTexts.Cardume.Filled(_view.CardumeFilled, _view.CardumeSize);
            var textWidth = Mathf.Max(skin.SmallMuted.CalcSize(new GUIContent(GameTexts.Expedition.YourStrength)).x, skin.Number.CalcSize(new GUIContent(value)).x);
            var well = WindowFrame.TakeSide(10f + 32f + 10f + textWidth + 16f);
            if (well.width < 80f)
            {
                return;
            }

            skin.Inset(well);
            skin.IconBadge(new Rect(well.x + 8f, well.center.y - 16f, 32f, 32f), Icons.Fish, UiSkin.Gold);
            var tx = well.x + 52f;
            var tw = well.xMax - tx - 8f;
            GUI.Label(new Rect(tx, well.y + 2f, tw, UiSkin.SmallLine), FishCard.Fit(GameTexts.Expedition.YourStrength, skin.SmallMuted, tw), skin.SmallMuted);
            GUI.Label(new Rect(tx, well.y + 17f, tw, 28f), FishCard.Fit(value, skin.Number, tw), skin.Number);
        }

        private void EnsureStyles(UiSkin skin)
        {
            if (_nameStyle != null)
            {
                return;
            }

            _nameStyle = new GUIStyle(skin.SmallBold) { alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Clip };
            _sealStyle = new GUIStyle(skin.Number) { font = skin.TitleBoldFont, fontSize = UiSkin.NumberSize, alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Overflow };
            _sealStyle.normal.textColor = Ink;
            _dialStyle = new GUIStyle(skin.Number) { font = skin.TitleBoldFont, fontSize = UiSkin.NumberSize, alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Overflow };
            _dialStyle.normal.textColor = UiSkin.Text;
        }

        // ------------------------------------------------------------------ nautical chart (A-147)

        private void DrawChartMenu(UiSkin skin, Rect area, Texture2D chart)
        {
            var offers = _view.Expeditions;
            var active = _view.Active;
            if (active == null && _view.CardumeFilled == 0)
            {
                // Beside the Cardume strength, in the header strip (A-148).
                var note = WindowFrame.Side;
                if (note.width > 60f)
                {
                    skin.DrawIcon(new Rect(note.x, note.center.y - 10f, 20, 20), Icons.Info, UiSkin.Gold);
                    GUI.Label(new Rect(note.x + 28, note.center.y - 11f, note.width - 28, 22), FishCard.Fit(GameTexts.Expedition.NoCardume, skin.SmallGoldLine, note.width - 28), skin.SmallGoldLine);
                }
            }

            // The nautical chart and its card are the window's own scene, framed like a photo.
            WindowFrame.Photo(skin, area);
            GUI.Box(area, GUIContent.none, skin.PanelSolid);

            // The Cardume strength moved up into the header (A-148): the chart starts at the top of the content.
            var top = area.y;
            var panelWidth = Mathf.Clamp(area.width * 0.32f, 320f, 400f);
            var panel = new Rect(area.xMax - panelWidth, top, panelWidth, area.yMax - top);
            var sea = new Rect(area.x, top, area.width - panelWidth - 20f, area.yMax - top);

            // The chosen destination: the one away, else the one clicked, else the first that can be sent.
            ExpeditionOfferView selected = null;
            var activeIndex = -1;
            for (var i = 0; i < offers.Count; i++)
            {
                if (active != null && offers[i].ExpeditionId == active.ExpeditionId)
                {
                    activeIndex = i;
                }
            }

            if (activeIndex >= 0)
            {
                selected = offers[activeIndex];
            }

            for (var i = 0; selected == null && i < offers.Count; i++)
            {
                if (offers[i].ExpeditionId == _selectedId)
                {
                    selected = offers[i];
                }
            }

            for (var i = 0; selected == null && i < offers.Count; i++)
            {
                if (offers[i].StartBlocker == ServiceError.None)
                {
                    selected = offers[i];
                }
            }

            selected ??= offers[0];
            _selectedId = selected.ExpeditionId;

            DrawChart(skin, sea, chart, selected, active, activeIndex);

            if (active != null)
            {
                DrawVoyageCard(skin, panel, active);
            }
            else
            {
                DrawMissionCard(skin, panel, selected);
            }
        }

        private void DrawChart(UiSkin skin, Rect sea, Texture2D chart, ExpeditionOfferView selected, ActiveExpeditionView active, int activeIndex)
        {
            // The nautical chart (L7-12) at the smallest whole scale that fills the sea, cropped at the edges, with a dark-gold line.
            ArcArt.Cover(sea, chart, Color.white);
            ArcArt.Line(skin, sea, UiSkin.FrameDark, 4f);

            var offers = _view.Expeditions;
            var n = offers.Count;
            if (_points.Length != n)
            {
                _points = new Vector2[n];
            }

            // The pier (the origin) in the bottom-left corner, on the shore of the chart.
            var pierSize = Mathf.Clamp(sea.height * 0.2f, 80f, 116f);
            var pier = new Vector2(sea.x + sea.width * 0.11f, sea.yMax - sea.height * 0.12f);
            var origin = new Vector2(pier.x, pier.y - pierSize * 0.42f);

            // Destinations climb away from the pier in duration order: one column each, farther right and higher the
            // longer the Expedition. The names sit above the medallions, so the routes (which come from below) never
            // cross a name.
            var xNear = Mathf.Max(sea.x + sea.width * 0.30f, pier.x + pierSize / 2f + 100f);
            var xFar = sea.xMax - 95f;
            var spacing = n > 1 ? (xFar - xNear) / (n - 1) : 0f;
            var d = Mathf.Clamp(Mathf.Min(sea.height * 0.2f, n > 1 ? spacing * 0.8f : 120f), 72f, 120f);
            var yNear = sea.y + sea.height * 0.60f;
            var yFar = sea.y + 8f + 50f + 8f + d / 2f;
            for (var i = 0; i < n; i++)
            {
                var rank = 0;
                for (var j = 0; j < n; j++)
                {
                    if (offers[j].DurationMinutes < offers[i].DurationMinutes || (offers[j].DurationMinutes == offers[i].DurationMinutes && j < i))
                    {
                        rank++;
                    }
                }

                var f = n > 1 ? rank / (n - 1f) : 1f;
                _points[i] = new Vector2(n > 1 ? Mathf.Lerp(xNear, xFar, f) : (xNear + xFar) / 2f, Mathf.Lerp(yNear, yFar, f));
            }

            // Compass rose in the top-left corner, as decoration.
            var rose = ArtAssets.Texture(RoseArt);
            if (rose != null)
            {
                var rs = Mathf.Clamp(sea.height * 0.17f, 64f, 100f);
                PixelDraw.Fit(new Rect(sea.x + 16f, sea.y + 16f, rs, rs), rose, new Color(1f, 1f, 1f, 0.85f));
            }

            // Routes: the chosen one turquoise; away, the travelled stretch turquoise and the rest light.
            if (Event.current.type == EventType.Repaint)
            {
                for (var i = 0; i < n; i++)
                {
                    var end = new Vector2(_points[i].x, _points[i].y + d / 2f + 4f);
                    if (activeIndex >= 0)
                    {
                        if (i == activeIndex)
                        {
                            DrawRoute(skin, origin, end, (float)active.Progress, 6f, UiSkin.Accent, new Color(1f, 1f, 1f, 0.6f));
                        }
                        else
                        {
                            DrawRoute(skin, origin, end, 0f, 4f, UiSkin.Accent, new Color(1f, 1f, 1f, 0.14f));
                        }
                    }
                    else if (offers[i] == selected)
                    {
                        DrawRoute(skin, origin, end, 1f, 6f, UiSkin.Accent, UiSkin.Accent);
                    }
                    else
                    {
                        DrawRoute(skin, origin, end, 0f, 4f, UiSkin.Accent, new Color(1f, 1f, 1f, 0.3f));
                    }
                }
            }

            var pierArt = ArtAssets.Texture(PierArt);
            var pierRect = new Rect(pier.x - pierSize / 2f, pier.y - pierSize / 2f, pierSize, pierSize);
            if (pierArt != null)
            {
                PixelDraw.Fit(pierRect, pierArt);
            }
            else
            {
                ArcArt.Fill(skin, new Rect(pier.x - pierSize * 0.18f, origin.y, pierSize * 0.36f, pierRect.yMax - origin.y), new Color(0.54f, 0.38f, 0.25f), 6f);
            }

            for (var i = 0; i < n; i++)
            {
                var chosen = offers[i] == selected;
                DrawDestination(skin, offers[i], _points[i], d, chosen, activeIndex >= 0 && !chosen, active == null);
            }

            // The Cardume marker walking its route (presentation of the progress the service reports).
            if (activeIndex >= 0)
            {
                var end = new Vector2(_points[activeIndex].x, _points[activeIndex].y + d / 2f + 4f);
                var at = RoutePoint(origin, end, (float)active.Progress);
                var ms = Mathf.Clamp(d * 0.5f, 44f, 60f);
                var bob = Mathf.Round(Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / 2.5f)) * UiSkin.ArcPixel;
                var marker = new Rect(at.x - ms / 2f, at.y - ms + 4f + bob, ms, ms);
                var markerArt = ArtAssets.Texture(MarkerArt);
                if (markerArt != null)
                {
                    PixelDraw.Fit(marker, markerArt);
                }
                else
                {
                    ArcArt.Fill(skin, marker, UiSkin.Accent, ms / 2f);
                    skin.DrawIcon(new Rect(marker.x + ms * 0.2f, marker.y + ms * 0.2f, ms * 0.6f, ms * 0.6f), Icons.Fish, Ink);
                }
            }
        }

        /// <summary>The dotted route; dots before <paramref name="done"/> (0–1 of the way) take the first colour.</summary>
        private static void DrawRoute(UiSkin skin, Vector2 from, Vector2 to, float done, float size, Color doneColor, Color restColor)
        {
            var length = Vector2.Distance(from, to) * 1.25f;
            var dots = Mathf.Max(3, Mathf.CeilToInt(length / 13f));
            for (var k = 0; k <= dots; k++)
            {
                var t = k / (float)dots;
                var p = RoutePoint(from, to, t);
                var reached = done > 0f && t <= done;
                // Square dots of 2 or 3 art pixels on the art-pixel grid.
                var s = (reached ? size : size * 0.8f) >= 5f ? UiSkin.ArcPixel * 3f : UiSkin.ArcPixel * 2f;
                var dx = Mathf.Round(p.x / UiSkin.ArcPixel) * UiSkin.ArcPixel;
                var dy = Mathf.Round(p.y / UiSkin.ArcPixel) * UiSkin.ArcPixel;
                ArcArt.Fill(skin, new Rect(dx - s / 2f, dy - s / 2f, s, s), reached ? doneColor : restColor, 0f);
            }
        }

        /// <summary>Out of the pier along the bottom of the chart, then up into the destination's column.</summary>
        private static Vector2 RoutePoint(Vector2 a, Vector2 b, float t)
        {
            var c1 = new Vector2(Mathf.Lerp(a.x, b.x, 0.75f), a.y);
            var c2 = new Vector2(b.x, Mathf.Lerp(a.y, b.y, 0.35f));
            var u = 1f - t;
            return u * u * u * a + 3f * u * u * t * c1 + 3f * u * t * t * c2 + t * t * t * b;
        }

        /// <summary>A destination: its landscape cut into a circle inside the brass medallion, with name and duration above.</summary>
        private void DrawDestination(UiSkin skin, ExpeditionOfferView e, Vector2 c, float d, bool chosen, bool dimmed, bool clickable)
        {
            var circle = new Rect(c.x - d / 2f, c.y - d / 2f, d, d);
            var hovered = clickable && GUI.enabled && circle.Contains(Event.current.mousePosition);

            if (chosen)
            {
                Ring(skin, circle, UiSkin.ArcPixel * 3, UiSkin.Accent);
            }
            else if (hovered)
            {
                Ring(skin, circle, UiSkin.ArcPixel * 3, new Color(1f, 1f, 1f, 0.45f));
            }

            Texture frame = ArtAssets.Texture(MedallionArt);
            var picture = ArtAssets.Texture("Expedicoes/" + e.ExpeditionId);
            if (frame != null && ArtAssets.IsPixel(frame))
            {
                // The brass medallion at the largest whole scale; the landscape at its own whole scale in a square under
                // the medallion's hole, its corners hidden by the ring (pixel art is never cut by a smooth circle).
                var medal = PixelDraw.FitRect(circle, ref frame);
                var side = medal.width * MedallionPicture;
                var square = new Rect(medal.center.x - side / 2f, medal.center.y - side / 2f, side, side);
                ArcArt.Fill(skin, square, UiSkin.Night, 0f);
                if (picture != null)
                {
                    ArcArt.Cover(square, picture, dimmed ? DimTint : Color.white);
                }

                GUI.DrawTexture(medal, frame, ScaleMode.StretchToFill, true);
            }
            else
            {
                var inner = new Rect(c.x - d * 0.37f, c.y - d * 0.37f, d * 0.74f, d * 0.74f);
                ArcArt.Fill(skin, inner, Color.Lerp(UiSkin.Accent, UiSkin.Night, dimmed ? 0.75f : 0.4f), inner.width / 2f);
                if (picture != null)
                {
                    ArcArt.Cover(inner, picture, dimmed ? DimTint : Color.white);
                }

                Ring(skin, circle, 0f, Brass);
            }

            if (dimmed)
            {
                var s = d * 0.26f;
                skin.DrawIcon(new Rect(c.x - s / 2f, c.y - s / 2f, s, s), Icons.Lock, Color.white);
            }

            // Name and duration above the medallion, each on a dark pill.
            const float maxWidth = 176f;
            var name = FishCard.Fit(e.Name, _nameStyle, maxWidth - 16f);
            var nameWidth = _nameStyle.CalcSize(new GUIContent(name)).x + 16f;
            var nameRect = new Rect(c.x - nameWidth / 2f, circle.y - 52f, nameWidth, UiSkin.SmallLine);
            skin.PixelPill(nameRect, new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.78f), chosen ? UiSkin.Accent : Color.clear);

            GUI.Label(nameRect, name, _nameStyle);

            var duration = Format.Duration(e.DurationMinutes * 60);
            var durationWidth = Mathf.Min(maxWidth, skin.SmallMutedCenter.CalcSize(new GUIContent(duration)).x + 34f);
            var durationRect = new Rect(c.x - durationWidth / 2f, nameRect.yMax + 2f, durationWidth, UiSkin.SmallLine);
            skin.PixelPill(durationRect, new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.68f), Color.clear);
            skin.DrawIcon(new Rect(durationRect.x + 6f, durationRect.y + 2f, 20f, 20f), Icons.Clock, UiSkin.Muted);
            GUI.Label(new Rect(durationRect.x + 26f, durationRect.y, durationRect.width - 30f, durationRect.height), duration, skin.SmallMutedCenter);

            if (clickable && GUI.Button(circle, GUIContent.none, GUIStyle.none))
            {
                _selectedId = e.ExpeditionId;
            }
        }

        /// <summary>A one-art-pixel pixel circle <paramref name="grow"/> px outside <paramref name="circle"/>.</summary>
        private static void Ring(UiSkin skin, Rect circle, float grow, Color color)
        {
            var rect = new Rect(circle.x - grow, circle.y - grow, circle.width + grow * 2f, circle.height + grow * 2f);
            ArcArt.Line(skin, rect, color, rect.width / 2f);
        }

        /// <summary>The mission card in the ARC skin (the solid panel with the three-line frame); returns the area inside it.</summary>
        private static Rect CardFrame(UiSkin skin, Rect panel)
        {
            skin.HardShadow(panel);
            GUI.Box(panel, GUIContent.none, skin.PanelSolid);
            return new Rect(panel.x + 20f, panel.y + 18f, panel.width - 40f, panel.height - 36f);
        }

        /// <summary>An Expedition's landscape (96 × 64 pixel art) filling <paramref name="rect"/> at a whole scale, cropped, with a dark-gold line.</summary>
        private static void Landscape(UiSkin skin, Rect rect, Texture picture)
        {
            ArcArt.Cover(rect, picture, Color.white, 0.5f, 0.6f);
            ArcArt.Line(skin, rect, UiSkin.FrameDark, 4f);
        }

        /// <summary>The chosen destination: landscape, name, duration, the numbers in blocks and "Enviar o Cardume" right under them.</summary>
        private void DrawMissionCard(UiSkin skin, Rect panel, ExpeditionOfferView e)
        {
            var inner = CardFrame(skin, panel);
            var x = inner.x;
            var w = inner.width;
            var y = inner.y;

            const float firstRow = 64f, secondRow = 112f, button = 46f;
            const float fixedHeight = 28f + 22f + 10f + firstRow + 8f + secondRow + 12f + button;
            var picture = ArtAssets.Texture("Expedicoes/" + e.ExpeditionId);
            var pictureHeight = Mathf.Min(inner.height - fixedHeight - 12f, w / 1.45f);
            if (picture != null && pictureHeight >= 70f)
            {
                Landscape(skin, new Rect(x, y, w, pictureHeight), picture);
                y += pictureHeight + 12f;
            }

            GUI.Label(new Rect(x, y, w, 28f), FishCard.Fit(e.Name, skin.Heading, w), skin.Heading);
            y += 28f;
            skin.DrawIcon(new Rect(x, y + 1f, 18f, 18f), Icons.Clock, UiSkin.Muted);
            GUI.Label(new Rect(x + 24f, y, w - 24f, UiSkin.SmallLine), FishCard.Fit(GameTexts.Expedition.Duration(Format.Duration(e.DurationMinutes * 60)), skin.SmallMuted, w - 24f), skin.SmallMuted);
            y += 32f;

            // Row 1: recommended strength and efficiency (with its bar). The left block is a little wider: its label,
            // "Força recomendada", is the longer one (A-149).
            var leftWidth = Mathf.Round((w - 8f) * 0.53f);
            var left = new Rect(x, y, leftWidth, firstRow);
            Block(skin, left, false);
            BlockLabel(skin, left, GameTexts.Expedition.Recommended);
            GUI.Label(new Rect(left.x + 10f, left.y + 24f, left.width - 20f, 26f), FishCard.Fit(Format.Number(e.RecommendedStrength), skin.Heading, left.width - 20f), skin.Heading);

            var right = new Rect(left.xMax + 8f, y, w - leftWidth - 8f, firstRow);
            Block(skin, right, false);
            BlockLabel(skin, right, GameTexts.Expedition.Efficiency);
            GUI.Label(new Rect(right.x + 10f, right.y + 22f, right.width - 20f, 26f), Format.Percent(e.Efficiency, 0), skin.Heading);
            skin.Bar(new Rect(right.x + 10f, right.yMax - 12f, right.width - 20f, 6f), Mathf.Clamp01((float)e.Efficiency / 1.5f), e.Efficiency >= 1.0);
            y += firstRow + 8f;

            // Row 2: the coins, larger and in gold, and the fish chance written on the seal.
            const float sealColumn = 112f;
            var coins = new Rect(x, y, w - sealColumn - 8f, secondRow);
            Block(skin, coins, true);
            BlockLabel(skin, coins, GameTexts.Expedition.Reward);
            var amount = Format.Number(e.ExpectedCoins);
            skin.CoinIcon(new Rect(coins.x + 10f, coins.y + 46f, 30f, 30f));
            GUI.Label(new Rect(coins.x + 46f, coins.y + 40f, coins.width - 54f, 42f), FishCard.Fit(amount, skin.NumberBig, coins.width - 54f), skin.NumberBig);

            var sealArea = new Rect(coins.xMax + 8f, y, sealColumn, secondRow);
            var sealSize = secondRow - 22f;
            var seal = new Rect(sealArea.center.x - sealSize / 2f, sealArea.y, sealSize, sealSize);
            var sealArt = ArtAssets.Texture(SealArt);
            if (sealArt != null)
            {
                // The rosette with its fish (L3-06) at a whole scale; the chance on a night pill over its lower edge, so the
                // fish drawn on it stays in sight.
                PixelDraw.Fit(seal, sealArt);
                var chance = Format.Percent(e.FishFindChance, 1);
                var pw = Mathf.Min(sealArea.width, _sealStyle.CalcSize(new GUIContent(chance)).x + 20f);
                var pill = new Rect(seal.center.x - pw / 2f, seal.yMax - 30f, pw, 28f);
                skin.PixelPill(pill, new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.9f), UiSkin.FrameDark);
                GUI.Label(pill, chance, _sealStyle);
            }
            else
            {
                var disc = new Rect(seal.x + 6f, seal.y + 6f, seal.width - 12f, seal.height - 12f);
                ArcArt.Disc(skin, disc, UiSkin.Accent, Color.clear);
                GUI.Label(new Rect(disc.x, disc.center.y - 14f, disc.width, 28f), Format.Percent(e.FishFindChance, 1), _sealStyle);
            }
            GUI.Label(new Rect(sealArea.x - 4f, seal.yMax + 2f, sealArea.width + 8f, UiSkin.SmallLine), FishCard.Fit(GameTexts.Expedition.FishChanceShort, skin.SmallMutedCenter, sealArea.width + 8f), skin.SmallMutedCenter);
            y += secondRow + 12f;

            var send = new Rect(x, y, w, button);
            if (e.StartBlocker == ServiceError.None)
            {
                if (skin.IconButton(send, Icons.Arrow, GameTexts.Expedition.Send, skin.ButtonPrimary))
                {
                    _root.StartExpedition(e.ExpeditionId);
                    _nextRefresh = 0f;
                }
            }
            else if (e.StartBlocker != ServiceError.ExpeditionActive)
            {
                skin.DrawIcon(new Rect(x, send.y + 12f, 20f, 20f), Icons.Lock, UiSkin.Gold);
                GUI.Label(new Rect(x + 28f, send.y, w - 28f, button), GameTexts.ServiceErrorMessage(e.StartBlocker.ToString()), skin.SmallGold);
            }
        }

        private static void Block(UiSkin skin, Rect rect, bool gold)
        {
            var edge = gold ? UiSkin.Gold : UiSkin.Border;
            ArcArt.Fill(skin, rect, gold ? new Color(edge.r, edge.g, edge.b, 0.08f) : new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.6f), 10f);
            ArcArt.Line(skin, rect, new Color(edge.r, edge.g, edge.b, gold ? 0.6f : 0.7f), 10f);
        }

        private static void BlockLabel(UiSkin skin, Rect block, string label)
        {
            GUI.Label(new Rect(block.x + 10f, block.y + 5f, block.width - 20f, UiSkin.SmallLine), FishCard.Fit(label, skin.SmallMuted, block.width - 20f), skin.SmallMuted);
        }

        /// <summary>Away: where it went, the compass with the time left and a progress ring, the lock note and Cancelar.</summary>
        private void DrawVoyageCard(UiSkin skin, Rect panel, ActiveExpeditionView active)
        {
            var inner = CardFrame(skin, panel);
            var x = inner.x;
            var w = inner.width;
            var y = inner.y;

            // The lock note takes four lines of the small text in the card's width (A-149).
            const float button = 44f, note = 86f, labels = 50f;
            var bottom = inner.yMax - button - 10f - note - 8f;
            var picture = ArtAssets.Texture("Expedicoes/" + active.ExpeditionId);
            var pictureHeight = Mathf.Min(bottom - y - labels - 8f - 240f - 12f, 110f);
            if (picture != null && pictureHeight >= 60f)
            {
                Landscape(skin, new Rect(x, y, w, pictureHeight), picture);
                y += pictureHeight + 12f;
            }

            GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), FishCard.Fit(GameTexts.Expedition.Away, skin.SmallGoldLine, w), skin.SmallGoldLine);
            GUI.Label(new Rect(x, y + 18f, w, 28f), FishCard.Fit(active.Name, skin.Heading, w), skin.Heading);
            y += labels + 8f;

            // The compass fills what is left between the name and the note.
            var space = new Rect(x, y, w, Mathf.Max(0f, bottom - y));
            var compassArt = ArtAssets.Texture(CompassArt);
            Vector2 dialCenter;
            float dialRadius;
            if (compassArt != null)
            {
                // The pocket compass (L3-06) at the largest whole scale; the dial gets a night disc under the ring and the
                // countdown, so the numbers read over the drawn needle.
                var h = Mathf.Min(space.height, 300f);
                var box = new Rect(space.center.x - space.width / 2f, space.center.y - h / 2f, space.width, h);
                var compass = ArcArt.Fit(box, compassArt, Color.white);
                dialCenter = new Vector2(compass.x + compass.width * DialX, compass.y + compass.height * DialY);
                dialRadius = Mathf.Min(compass.width * DialW, compass.height * DialH) / 2f;
                var face = new Rect(dialCenter.x - dialRadius, dialCenter.y - dialRadius, dialRadius * 2f, dialRadius * 2f);
                ArcArt.Disc(skin, face, new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.82f), Color.clear);
            }
            else
            {
                dialRadius = Mathf.Min(space.width, space.height) * 0.4f;
                dialCenter = space.center;
                var disc = new Rect(dialCenter.x - dialRadius, dialCenter.y - dialRadius, dialRadius * 2f, dialRadius * 2f);
                ArcArt.Disc(skin, disc, UiSkin.Night, Color.clear);
                Ring(skin, disc, 6f, Brass);
            }

            if (dialRadius > 12f)
            {
                DrawProgressRing(skin, dialCenter, dialRadius * 0.86f, (float)active.Progress);
                GUI.Label(new Rect(dialCenter.x - dialRadius, dialCenter.y - 24f, dialRadius * 2f, 18f), GameTexts.Expedition.ReturnsInLabel, skin.TinyMutedCenter);
                GUI.Label(new Rect(dialCenter.x - dialRadius, dialCenter.y - 8f, dialRadius * 2f, 26f), Format.Countdown(active.SecondsLeft), _dialStyle);
            }

            GUI.Label(new Rect(x, inner.yMax - button - 10f - note, w, note), GameTexts.Expedition.LockNote, skin.SmallMuted);
            if (skin.IconButton(new Rect(x, inner.yMax - button, w, button), Icons.Close, GameTexts.Expedition.Cancel, skin.ButtonDanger))
            {
                _confirmCancel = true;
            }
        }

        /// <summary>A dotted ring on the dial: the share already sailed turquoise, clockwise from the top.</summary>
        private static void DrawProgressRing(UiSkin skin, Vector2 center, float radius, float progress)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            var dots = Mathf.Clamp(Mathf.RoundToInt(radius * 1.5f), 36, 90);
            var size = radius >= 50f ? UiSkin.ArcPixel * 2f : UiSkin.ArcPixel;
            for (var k = 0; k < dots; k++)
            {
                var t = k / (float)dots;
                var angle = (t * 360f - 90f) * Mathf.Deg2Rad;
                var p = new Vector2(center.x + Mathf.Cos(angle) * radius, center.y + Mathf.Sin(angle) * radius);
                var color = t < progress ? UiSkin.Accent : new Color(1f, 1f, 1f, 0.18f);
                var px = Mathf.Round(p.x / UiSkin.ArcPixel) * UiSkin.ArcPixel;
                var py = Mathf.Round(p.y / UiSkin.ArcPixel) * UiSkin.ArcPixel;
                ArcArt.Fill(skin, new Rect(px - size / 2f, py - size / 2f, size, size), color, 0f);
            }
        }

        // ------------------------------------------------------------------ cards (without the chart art)

        private void DrawCardsMenu(UiSkin skin, Rect area)
        {
            var top = area.y;
            var active = _view.Active;
            if (active != null)
            {
                var box = new Rect(area.x, top, area.width, 110);
                GUI.Box(box, GUIContent.none, skin.CardSelected);
                GUI.Label(new Rect(box.x + 20, box.y + 14, box.width - 260, 26), FishCard.Fit(GameTexts.Expedition.Away + ": " + active.Name, skin.Heading, box.width - 260), skin.Heading);
                if (GUI.Button(new Rect(box.xMax - 220, box.y + 10, 200, 34), GameTexts.Expedition.Cancel, skin.ButtonDanger))
                {
                    _confirmCancel = true;
                }
                skin.Bar(new Rect(box.x + 20, box.y + 48, box.width - 40, 12), (float)active.Progress);
                GUI.Label(new Rect(box.x + 20, box.y + 66, 300, 22), GameTexts.Expedition.ReturnsIn(Format.Countdown(active.SecondsLeft)), skin.Small);
                GUI.Label(new Rect(box.x + 320, box.y + 66, box.width - 340, 40), GameTexts.Expedition.LockNote, skin.SmallMuted);
                top += 126;
            }
            else if (_view.CardumeFilled == 0)
            {
                skin.DrawIcon(new Rect(area.x, top + 1, 20, 20), Icons.Info, UiSkin.Gold);
                GUI.Label(new Rect(area.x + 28, top, area.width - 28, 22), GameTexts.Expedition.NoCardume, skin.SmallGold);
                top += 32;
            }

            var count = _view.Expeditions.Count;
            var cardW = (area.width - (count - 1) * 16f) / Mathf.Max(1, count);
            for (var i = 0; i < count; i++)
            {
                DrawOffer(skin, new Rect(area.x + i * (cardW + 16f), top, cardW, area.yMax - top), _view.Expeditions[i]);
            }
        }

        private void DrawOffer(UiSkin skin, Rect rect, ExpeditionOfferView e)
        {
            GUI.Box(rect, GUIContent.none, skin.Card);
            var x = rect.x + 18;
            var w = rect.width - 36;
            var y = rect.y + 16;

            // Each expedition has its own landscape (Art Bible, section 23).
            // The picture gives way to the text: it is only as tall as the room left once the rows and the
            // "Enviar" button are counted (~254 px), and is left out when that would be under 60 px.
            var picture = ArtAssets.Texture("Expedicoes/" + e.ExpeditionId);
            var pictureHeight = Mathf.Min(180f, (rect.width - 20) / 1.54f, rect.height - 254f);
            if (picture != null && pictureHeight >= 60f)
            {
                var pr = new Rect(rect.x + 10, rect.y + 10, rect.width - 20, pictureHeight);
                Landscape(skin, pr, picture);
                y = pr.yMax + 12;
            }

            GUI.Label(new Rect(x, y, w, 28), FishCard.Fit(e.Name, skin.Heading, w), skin.Heading);
            y += 30;
            skin.DrawIcon(new Rect(x, y + 1, 18, 18), Icons.Clock, UiSkin.Muted);
            GUI.Label(new Rect(x + 24, y, w - 24, UiSkin.SmallLine), GameTexts.Expedition.Duration(Format.Duration(e.DurationMinutes * 60)), skin.SmallMuted);
            y += 30;

            Row(skin, x, ref y, w, GameTexts.Expedition.Recommended, Format.Number(e.RecommendedStrength));
            Row(skin, x, ref y, w, GameTexts.Expedition.Efficiency, Format.Percent(e.Efficiency, 0));
            Row(skin, x, ref y, w, GameTexts.Expedition.Reward, Format.Number(e.ExpectedCoins));
            Row(skin, x, ref y, w, GameTexts.Expedition.FishChance, Format.Percent(e.FishFindChance, 1));
            skin.Bar(new Rect(x, y + 6, w, 8), (float)Mathf.Clamp01((float)e.Efficiency / 1.5f), e.Efficiency >= 1.0);

            var button = new Rect(x, rect.yMax - 58, w, 42);
            if (e.StartBlocker == ServiceError.None)
            {
                if (skin.IconButton(button, Icons.Arrow, GameTexts.Expedition.Send, skin.ButtonPrimary))
                {
                    _root.StartExpedition(e.ExpeditionId);
                    _nextRefresh = 0f;
                }
            }
            else if (e.StartBlocker != ServiceError.ExpeditionActive)
            {
                skin.DrawIcon(new Rect(x, button.y + 11, 20, 20), Icons.Lock, UiSkin.Gold);
                GUI.Label(new Rect(x + 28, button.y + 2, w - 28, 42), GameTexts.ServiceErrorMessage(e.StartBlocker.ToString()), skin.SmallGold);
            }
        }

        private static void Row(UiSkin skin, float x, ref float y, float w, string label, string value)
        {
            // The label takes what the value leaves, on one line ("Chance de achar um peixe" must not wrap).
            var vw = Mathf.Min(w, skin.SmallRight.CalcSize(new GUIContent(value)).x + 4f);
            var lw = Mathf.Max(0f, w - vw - 8f);
            GUI.Label(new Rect(x, y, lw, UiSkin.SmallLine), FishCard.Fit(label, skin.SmallMuted, lw), skin.SmallMuted);
            GUI.Label(new Rect(x + w - vw, y, vw, UiSkin.SmallLine), value, skin.SmallRight);
            y += 24;
        }

        // ------------------------------------------------------------------ report

        /// <summary>
        /// The report of the Expedition that came back: where it went and when it returned, the coins
        /// and the fish it found, shown prominently (GDD section 32). "Ótimo!" marks it as read. With the
        /// owner's art it is drawn over the harbour at dusk (A-147).
        /// </summary>
        private void DrawReport(UiSkin skin, float screenWidth, float screenHeight, ExpeditionResultView report)
        {
            var fish = report.FoundFish;
            var height = fish != null ? 590f : 400f;
            var width = Mathf.Min(660f, screenWidth - 40f);

            // The landscape of the Expedition across the top; left out when the screen is too short for it.
            var picture = !string.IsNullOrEmpty(report.ExpeditionId) ? ArtAssets.Texture("Expedicoes/" + report.ExpeditionId) : null;
            if (picture == null)
            {
                height -= 108f;
            }
            else if (height > screenHeight - 40f)
            {
                picture = null;
                height -= 108f;
            }

            Rect rect;
            var harbour = ArtAssets.Texture(HarbourArt);
            if (harbour != null)
            {
                // The harbour fills the window; the report sits on it on a dark, slightly see-through panel.
                var scene = WindowFrame.Panel(skin, screenWidth, screenHeight, 1320f, 760f);
                // The harbour at dusk (L7-11) at a whole scale inside the frame, cropped at the edges; the report on the ARC
                // panel, a little see-through.
                var inset = WindowFrame.Sheet(scene);
                ArcArt.Cover(inset, harbour, Color.white, 0.5f, 0.55f);
                ArcArt.Fill(skin, inset, new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.3f), 0f);
                rect = new Rect(screenWidth / 2f - width / 2f, screenHeight / 2f - height / 2f, width, height);
                skin.HardShadow(rect);
                skin.Shape(rect, 2, false, new Color(UiSkin.Night.r, UiSkin.Night.g, UiSkin.Night.b, 0.9f), 3, UiSkin.FrameGold, UiSkin.FrameInner, UiSkin.FrameDark);
            }
            else
            {
                // Centred on the whole screen over a full-screen overlay, like the other dialogs.
                rect = WindowFrame.Dialog(skin, screenWidth, screenHeight, height, width);
            }

            var y = rect.y + 14;
            if (picture != null)
            {
                Landscape(skin, new Rect(rect.x + 14, y, rect.width - 28, 96), picture);
                y += 108;
            }

            var x = rect.x + 30;
            var w = rect.width - 60;
            GUI.Label(new Rect(x, y, w, UiSkin.SmallLine), GameTexts.Expedition.ReportLabel, skin.SmallGold);
            skin.TitleLabel(new Rect(x, y + 18, w, 34), GameTexts.Expedition.ResultTitle, skin.Title);
            GUI.Label(new Rect(x, y + 54, w, UiSkin.SmallLine), GameTexts.Expedition.ReturnedAt(report.Name, Format.DateTimeFromUnixMs(report.CompletedAtMs)), skin.SmallMuted);
            y += 86;

            GUI.Label(new Rect(x, y, w, 22), GameTexts.Expedition.Brought, skin.BodyBold);
            y += 28;
            skin.CoinIcon(new Rect(x, y + 2, 26, 26));
            GUI.Label(new Rect(x + 34, y, w * 0.6f, 30), GameTexts.Expedition.Coins(Format.Number(report.Coins)), skin.Number);
            GUI.Label(new Rect(x + w * 0.5f, y + 6, w * 0.5f, 20), GameTexts.Expedition.Efficiency + " " + Format.Percent(report.Efficiency, 0), skin.SmallMutedRight);
            y += 40;

            if (fish != null)
            {
                GUI.Label(new Rect(x, y, w, 22), GameTexts.Expedition.FoundFish, skin.Heading);
                y += 30;
                var model = new FishCardModel
                {
                    SpeciesId = fish.SpeciesId,
                    Name = fish.SpeciesName,
                    Line = VisualTheme.IsSpecialSize(fish.SizeCategoryId) ? Format.SizeCm(fish.SizeCm) : Format.SizeCm(fish.SizeCm) + " · " + fish.SizeCategoryName,
                    RarityId = fish.RarityId,
                    RarityName = fish.RarityName,
                    SizeCategoryId = fish.SizeCategoryId,
                    SizeCategoryName = fish.SizeCategoryName,
                    Bar = (float)fish.SizePercentile,
                    Coins = Format.Short(fish.SalePriceCoins),
                    Genes = fish.Genes,
                };
                FishCard.StatusBadge(model, fish.IsNewSpecies, fish.IsPersonalRecord);
                var card = new Rect(rect.center.x - 106, y, 212, 196);
                skin.DrawGlow(card, UiSkin.RarityColor(fish.RarityId), 0.3f);
                FishCard.Draw(skin, card, model);
            }
            else
            {
                GUI.Label(new Rect(x, y, w, 22), report.FoundAFish ? GameTexts.Expedition.FoundFish : GameTexts.Expedition.NoFish, skin.SmallMuted);
            }

            GUI.Label(new Rect(x, rect.yMax - 64, w - 220, 46), GameTexts.Expedition.ReportNote, skin.SmallMuted);
            if (skin.IconButton(new Rect(rect.xMax - 230, rect.yMax - 62, 200, 42), Icons.Check, GameTexts.Expedition.Collect, skin.ButtonReward))
            {
                _root.AcknowledgeExpedition();
            }
        }
    }
}
