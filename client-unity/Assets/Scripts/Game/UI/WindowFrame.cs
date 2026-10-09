using System.Collections.Generic;
using FishingIdle.Game.Visual;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The shared look of a full window (Art Bible, section 13): the scene dimmed but still visible,
    /// a floating panel with a soft shadow, the "Placa do Píer" header (addendum A-148) and "✕ Fechar".
    /// </summary>
    /// <remarks>
    /// The header is a sign of dark-blue planks with a turquoise border and four bolts, hanging by two ropes
    /// from under the main bar over the top edge of the window, with the menu icon and the title. It swings once
    /// when the window opens (presentation only, real UI time, damped, no loop). What used to be the subtitle
    /// is split by what it is:
    /// <list type="bullet">
    /// <item><b>info</b> — a sentence that explains the menu. It hides behind a round turquoise "i" on the corner
    /// of the sign; clicking it opens the "Como funciona" balloon, drawn over the window body by
    /// <see cref="DrawInfoBalloon"/> at the end of the frame.</item>
    /// <item><b>counter</b> — a short live number or time ("12 / 20 peixes", "Tempo: 0:42"). It stays in sight, in a
    /// dark inset next to the sign.</item>
    /// </list>
    /// The free strip between the sign (and its counter) and Fechar is left in <see cref="Side"/> for the window's
    /// own header pieces (the Expedition's Cardume strength, the Ranking trophy).
    /// </remarks>
    public static class WindowFrame
    {
        // Layout, relative to the panel. The sign starts 6 px above the panel edge and ends 56 px into it; the
        // content area starts 24 px under it.
        private const float Margin = 28f;
        private const float PlankTop = -6f, PlankHeight = 62f;
        private const float PlankPadLeft = 22f, PlankPadRight = 30f, PlankIcon = 34f, PlankIconGap = 12f;
        private const float ContentTop = 80f, ContentBottom = 24f;
        private const float CloseRight = 156f, CloseTop = 22f, CloseWidth = 128f, CloseHeight = 42f;
        private const float SideTop = 18f, SideHeight = 46f, SideGap = 12f;

        // The ropes go up to the bottom of the main bar (Hud.BarHeight), so the sign hangs from it.
        private const float RopeAnchorY = 80f, RopeInset = 30f, RopeWidth = 6f, RingSize = 18f;

        // The "i" button on the top-right corner of the sign and its balloon.
        private const float InfoSize = 28f, TipWidth = 460f, TipPad = 16f;

        // The swing when the window opens: from -3° back to rest, about two and a half swings in 0.9 s.
        private const float SwingSeconds = 0.9f, SwingPeriod = 0.5f, SwingDegrees = 3f;

        // Art (Resources/Arte/UI). Each piece falls back to the drawing below when its file is missing.
        // ASSET_PENDENTE: ui_hdr_placa (9-slice), ui_hdr_corda (repeats along the rope), ui_hdr_argola.
        private const string PlankArt = "UI/ui_hdr_placa", RopeArt = "UI/ui_hdr_corda", RingArt = "UI/ui_hdr_argola";

        /// <summary>Border of ui_hdr_placa kept whole (texture pixels; 512×128 file): sides 48, top and bottom 32.</summary>
        private const float PlankArtSide = 48f, PlankArtCap = 32f;

        // Sign colours (the "Placa do Píer" example the owner chose).
        private static readonly Color BoardTop = Rgb(0x1D4266), BoardMiddle = Rgb(0x1A3C5E), BoardBottom = Rgb(0x173757);
        private static readonly Color Seam = Rgb(0x0D2440), PlankEdge = Rgb(0x2A8B98), PlankInnerEdge = Rgb(0x0A1A2D);
        private static readonly Color Bolt = Rgb(0xC9D6E3), BoltShade = Rgb(0x6C7D90), Steel = Rgb(0xC9D6E3);
        private static readonly Color Rope = Rgb(0xC9A77A), RopeTwist = Rgb(0x8E6F47);
        private static readonly Color TitleInk = Rgb(0xF4F8FC), TitleShade = Rgb(0x061423), InfoInk = Rgb(0x06313A);
        private static readonly Color TipFill = Rgb(0x0B1B2E);

        private static readonly Dictionary<string, int> LastFrame = new Dictionary<string, int>();
        private static readonly Dictionary<string, float> OpenedAt = new Dictionary<string, float>();

        private static GUIStyle _title, _titleShadow, _infoGlyph, _counter, _tipTitle, _tipBody;

        // The open "Como funciona" balloon: which header owns it, its text and where it goes.
        private static string _tipKey, _tipText;
        private static Rect _tipRect;
        private static int _tipFrame = -1;

        /// <summary>
        /// The free strip of the last header drawn: right of the sign (and its counter), left of Fechar, 46 px tall.
        /// Windows put their own header pieces here with <see cref="TakeSide"/>.
        /// </summary>
        public static Rect Side { get; private set; }

        /// <summary>
        /// Draws the frame and returns the content area. <paramref name="closed"/> is true when Fechar was clicked.
        /// <paramref name="info"/> goes behind the "i"; <paramref name="counter"/> stays in sight next to the sign.
        /// </summary>
        public static Rect Draw(UiSkin skin, float screenWidth, float screenHeight, string title, string info, out bool closed, float maxWidth = 1320f, float maxHeight = 840f, string icon = null, string counter = null)
        {
            var panel = Panel(skin, screenWidth, screenHeight, maxWidth, maxHeight);
            closed = Header(skin, panel, title, info, icon, counter);
            return Content(panel);
        }

        /// <summary>The content area of a panel with the shared header.</summary>
        public static Rect Content(Rect panel)
        {
            return new Rect(panel.x + Margin, panel.y + ContentTop, panel.width - Margin * 2f, panel.height - ContentTop - ContentBottom);
        }

        /// <summary>Dims the scene and draws the floating panel; returns its rect.</summary>
        public static Rect Panel(UiSkin skin, float screenWidth, float screenHeight, float maxWidth, float maxHeight)
        {
            GUI.DrawTexture(new Rect(0, 0, screenWidth, screenHeight), skin.Overlay);
            var width = Mathf.Min(maxWidth, screenWidth - 80f);
            var height = Mathf.Min(maxHeight, screenHeight - 120f);
            var panel = new Rect((screenWidth - width) / 2f, (screenHeight - height) / 2f + 20f, width, height);
            skin.FloatingPanel(panel);
            return panel;
        }

        /// <summary>
        /// The hanging sign (icon and title), the "i" with <paramref name="info"/>, the <paramref name="counter"/> in a
        /// dark inset and the close button. <paramref name="reserveRight"/> keeps room left of Fechar for the window's
        /// own controls (search, view toggle). Returns true when Fechar was clicked.
        /// </summary>
        public static bool Header(UiSkin skin, Rect panel, string title, string info, string icon, string counter = null, float reserveRight = 0f)
        {
            EnsureStyles(skin);
            var key = (icon ?? string.Empty) + "|" + title;
            var frame = Time.frameCount;
            if (!LastFrame.TryGetValue(key, out var last) || last < frame - 1)
            {
                // Not drawn last frame: the window has just opened. Swing, and start with the balloon closed.
                OpenedAt[key] = Time.unscaledTime;
                if (_tipKey == key)
                {
                    _tipKey = null;
                }
            }

            LastFrame[key] = frame;

            var close = new Rect(panel.xMax - CloseRight, panel.y + CloseTop, CloseWidth, CloseHeight);
            var hasInfo = !string.IsNullOrEmpty(info);
            var hasCounter = !string.IsNullOrEmpty(counter);

            // The sign is as wide as its title, never reaching Fechar (a counter keeps at least 150 px).
            var plankX = panel.x + Margin;
            var iconWidth = icon != null ? PlankIcon + PlankIconGap : 0f;
            var maxPlank = close.x - 16f - reserveRight - plankX - (hasInfo ? 30f : 20f) - (hasCounter ? 150f : 0f);
            var titleSpace = Mathf.Max(40f, maxPlank - PlankPadLeft - iconWidth - PlankPadRight);
            var titleText = FishCard.Fit(title ?? string.Empty, _title, titleSpace);
            var titleWidth = Mathf.Min(_title.CalcSize(new GUIContent(titleText)).x, titleSpace);
            var plank = new Rect(plankX, panel.y + PlankTop, PlankPadLeft + iconWidth + titleWidth + PlankPadRight, PlankHeight);
            var infoRect = new Rect(plank.xMax - InfoSize / 2f - 2f, plank.y - InfoSize / 2f + 2f, InfoSize, InfoSize);

            // The balloon: only while the window takes input (a confirmation dialog closes it).
            var tipOpen = hasInfo && _tipKey == key && GUI.enabled;
            if (!tipOpen && _tipKey == key)
            {
                _tipKey = null;
            }

            if (tipOpen)
            {
                KeepTip(panel, plank, info, frame);

                // The header runs before the body, so it sees the click first: a click on the balloon never reaches
                // what is under it; a click anywhere else closes it (inside the window it does nothing more).
                var e = Event.current;
                if (e.type == EventType.MouseDown && !infoRect.Contains(e.mousePosition))
                {
                    if (_tipRect.Contains(e.mousePosition))
                    {
                        e.Use();
                    }
                    else
                    {
                        _tipKey = null;
                        tipOpen = false;
                        if (panel.Contains(e.mousePosition) && !close.Contains(e.mousePosition))
                        {
                            e.Use();
                        }
                    }
                }
            }

            // The sign, its ropes and the "i", swinging once around the top of the ropes.
            var ropeTop = Mathf.Min(RopeAnchorY, plank.y - 16f);
            var matrix = GUI.matrix;
            var angle = SwingAngle(Time.unscaledTime - OpenedAt[key]);
            if (angle != 0f)
            {
                var pivot = new Vector3(plank.center.x, ropeTop, 0f);
                GUI.matrix = matrix * Matrix4x4.TRS(pivot, Quaternion.Euler(0f, 0f, angle), Vector3.one) * Matrix4x4.TRS(-pivot, Quaternion.identity, Vector3.one);
            }

            DrawRope(skin, plank.x + RopeInset, ropeTop, plank.y + 4f);
            DrawRope(skin, plank.xMax - RopeInset, ropeTop, plank.y + 4f);
            DrawPlank(skin, plank);
            DrawRing(skin, plank.x + RopeInset, plank.y);
            DrawRing(skin, plank.xMax - RopeInset, plank.y);

            var x = plank.x + PlankPadLeft;
            if (icon != null)
            {
                skin.DrawIcon(new Rect(x, plank.center.y - PlankIcon / 2f + 1f, PlankIcon, PlankIcon), icon, UiSkin.AccentHover);
                x += iconWidth;
            }

            var titleRect = new Rect(x, plank.y, titleWidth + 4f, plank.height);
            GUI.Label(new Rect(titleRect.x, titleRect.y + 2.5f, titleRect.width, titleRect.height), titleText, _titleShadow);
            GUI.Label(titleRect, titleText, _title);

            if (hasInfo && InfoButton(skin, infoRect, tipOpen))
            {
                _tipKey = tipOpen ? null : key;
                if (_tipKey != null)
                {
                    // Opened in this event: DrawInfoBalloon runs later in the same event and must see it as drawn.
                    KeepTip(panel, plank, info, frame);
                }
            }

            GUI.matrix = matrix;

            // The free strip right of the sign; a counter takes its start, in a dark inset.
            var sideX = plank.xMax + (hasInfo ? 30f : 20f);
            Side = new Rect(sideX, panel.y + SideTop, Mathf.Max(0f, close.x - 16f - reserveRight - sideX), SideHeight);
            if (hasCounter && Side.width > 60f)
            {
                var well = TakeSide(Mathf.Min(_counter.CalcSize(new GUIContent(counter)).x + 36f, Side.width));
                skin.Inset(well);
                GUI.Label(well, FishCard.Fit(counter, _counter, well.width - 20f), _counter);
            }

            if (skin.IconButton(close, Icons.Close, GameTexts.Box.Close, skin.Button))
            {
                if (_tipKey == key)
                {
                    _tipKey = null;
                }

                return true;
            }

            return false;
        }

        /// <summary>Records the open balloon's text and place for <see cref="DrawInfoBalloon"/> this frame.</summary>
        private static void KeepTip(Rect panel, Rect plank, string info, int frame)
        {
            var width = Mathf.Min(TipWidth, panel.width - Margin * 2f);
            var textHeight = _tipBody.CalcHeight(new GUIContent(info), width - TipPad * 2f);
            _tipRect = new Rect(plank.x + 6f, plank.yMax + 12f, width, TipPad + 22f + 4f + textHeight + TipPad);
            _tipText = info;
            _tipFrame = frame;
        }

        /// <summary>Takes <paramref name="width"/> px from the start of <see cref="Side"/> (and the gap after it); returns that piece.</summary>
        public static Rect TakeSide(float width)
        {
            var side = Side;
            width = Mathf.Clamp(width, 0f, side.width);
            Side = new Rect(side.x + width + SideGap, side.y, Mathf.Max(0f, side.width - width - SideGap), side.height);
            return new Rect(side.x, side.y, width, side.height);
        }

        /// <summary>
        /// Draws the open "Como funciona" balloon over everything the windows drew this frame. The HUD calls it right
        /// after the windows; <paramref name="blocked"/> (a window dialog is open) closes it.
        /// </summary>
        public static void DrawInfoBalloon(UiSkin skin, bool blocked)
        {
            if (_tipKey == null)
            {
                return;
            }

            // Its header was not drawn this frame: the window closed.
            if (_tipFrame != Time.frameCount || blocked)
            {
                _tipKey = null;
                return;
            }

            EnsureStyles(skin);
            var r = _tipRect;
            GUI.DrawTexture(new Rect(r.x - 4f, r.y + 4f, r.width + 8f, r.height + 10f), skin.White, ScaleMode.StretchToFill, true, 0, new Color(0f, 0f, 0f, 0.35f), 0, 14f);
            GUI.DrawTexture(r, skin.White, ScaleMode.StretchToFill, true, 0, new Color(TipFill.r, TipFill.g, TipFill.b, 0.98f), 0, 10f);
            GUI.DrawTexture(r, skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.Accent, 1.5f, 10f);
            GUI.Label(new Rect(r.x + TipPad, r.y + TipPad - 2f, r.width - TipPad * 2f, 22f), GameTexts.WindowHeader.InfoTitle, _tipTitle);
            GUI.Label(new Rect(r.x + TipPad, r.y + TipPad + 24f, r.width - TipPad * 2f, r.height - TipPad * 2f - 24f), _tipText, _tipBody);
        }

        /// <summary>Closes the "Como funciona" balloon (Esc). Returns true when it was open.</summary>
        public static bool CloseInfoBalloon()
        {
            if (_tipKey == null)
            {
                return false;
            }

            _tipKey = null;
            return true;
        }

        /// <summary>A centred confirmation box over everything. Returns the box rect.</summary>
        public static Rect Dialog(UiSkin skin, float screenWidth, float screenHeight, float height, float width = 640f)
        {
            var rect = new Rect(screenWidth / 2f - width / 2f, screenHeight / 2f - height / 2f, width, height);
            GUI.DrawTexture(new Rect(0, 0, screenWidth, screenHeight), skin.Overlay);
            skin.DrawShadow(rect);
            GUI.Box(rect, GUIContent.none, skin.PanelSolid);
            return rect;
        }

        // ------------------------------------------------------------------ the sign

        /// <summary>The swing angle <paramref name="t"/> seconds after opening: damped, then 0 for good.</summary>
        private static float SwingAngle(float t)
        {
            if (t < 0f || t >= SwingSeconds)
            {
                return 0f;
            }

            var fade = 1f - t / SwingSeconds;
            return -SwingDegrees * fade * fade * Mathf.Cos(2f * Mathf.PI * t / SwingPeriod);
        }

        private static void DrawPlank(UiSkin skin, Rect r)
        {
            Fill(skin, new Rect(r.x + 2f, r.y + 8f, r.width, r.height), new Color(0f, 0f, 0f, 0.38f), 10f);

            var art = ArtAssets.Texture(PlankArt);
            if (art != null)
            {
                UiSkin.NineSlice(r, art, PlankArtSide, PlankArtSide, PlankArtCap, PlankArtCap, r.height / art.height);
                return;
            }

            // Three horizontal boards with dark seams, a turquoise border and a bolt in each corner.
            const float radius = 9f;
            Fill(skin, r, BoardBottom, radius);
            var topBand = new Rect(r.x, r.y, r.width, r.height * 0.31f);
            GUI.DrawTexture(topBand, skin.White, ScaleMode.StretchToFill, true, 0, BoardTop, Vector4.zero, new Vector4(radius, radius, 0f, 0f));
            Fill(skin, new Rect(r.x, r.y + r.height * 0.34f, r.width, r.height * 0.30f), BoardMiddle, 0f);
            Fill(skin, new Rect(r.x + 3f, r.y + r.height * 0.31f, r.width - 6f, 2f), Seam, 0f);
            Fill(skin, new Rect(r.x + 3f, r.y + r.height * 0.64f, r.width - 6f, 2f), Seam, 0f);
            Fill(skin, new Rect(r.x + 8f, r.y + 4f, r.width - 16f, 1f), new Color(1f, 1f, 1f, 0.08f), 0f);
            GUI.DrawTexture(r, skin.White, ScaleMode.StretchToFill, true, 0, PlankEdge, 3f, radius);
            GUI.DrawTexture(new Rect(r.x + 3f, r.y + 3f, r.width - 6f, r.height - 6f), skin.White, ScaleMode.StretchToFill, true, 0, PlankInnerEdge, 1.5f, radius - 2f);

            const float inset = 9f;
            DrawBolt(skin, r.x + inset, r.y + inset);
            DrawBolt(skin, r.xMax - inset, r.y + inset);
            DrawBolt(skin, r.x + inset, r.yMax - inset);
            DrawBolt(skin, r.xMax - inset, r.yMax - inset);
        }

        private static void DrawBolt(UiSkin skin, float cx, float cy)
        {
            Fill(skin, new Rect(cx - 4.5f, cy - 4.5f, 9f, 9f), BoltShade, 4.5f);
            Fill(skin, new Rect(cx - 4f, cy - 4f, 7f, 7f), Bolt, 3.5f);
            Fill(skin, new Rect(cx - 3f, cy - 3f, 3f, 3f), new Color(1f, 1f, 1f, 0.9f), 1.5f);
        }

        /// <summary>A rope from <paramref name="top"/> down to <paramref name="bottom"/>, centred on <paramref name="cx"/>.</summary>
        private static void DrawRope(UiSkin skin, float cx, float top, float bottom)
        {
            var length = bottom - top;
            if (length <= 0f)
            {
                return;
            }

            var x = cx - RopeWidth / 2f;
            var art = ArtAssets.Texture(RopeArt);
            if (art != null)
            {
                // The file repeats along the rope, one tile at a time (the art is loaded clamped).
                var tile = RopeWidth * art.height / art.width;
                for (var y = bottom; y > top; y -= tile)
                {
                    var h = Mathf.Min(tile, y - top);
                    GUI.DrawTextureWithTexCoords(new Rect(x, y - h, RopeWidth, h), art, new Rect(0f, 0f, 1f, h / tile), true);
                }

                return;
            }

            // A twisted rope: the light strand with a darker twist every 6 px.
            Fill(skin, new Rect(x, top, RopeWidth, length), Rope, 0f);
            for (var y = bottom - 4f; y > top; y -= 6f)
            {
                Fill(skin, new Rect(x, Mathf.Max(top, y - 2f), RopeWidth, Mathf.Min(2f, y - top)), RopeTwist, 0f);
            }
        }

        /// <summary>The steel ring where a rope meets the sign.</summary>
        private static void DrawRing(UiSkin skin, float cx, float cy)
        {
            var rect = new Rect(cx - RingSize / 2f, cy - RingSize / 2f - 2f, RingSize, RingSize);
            var art = ArtAssets.Texture(RingArt);
            if (art != null)
            {
                GUI.DrawTexture(rect, art, ScaleMode.ScaleToFit, true);
                return;
            }

            GUI.DrawTexture(rect, skin.White, ScaleMode.StretchToFill, true, 0, PlankInnerEdge, 5f, RingSize / 2f);
            var inner = new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f);
            GUI.DrawTexture(inner, skin.White, ScaleMode.StretchToFill, true, 0, Steel, 3f, inner.width / 2f);
        }

        /// <summary>The round turquoise "i". Returns true when clicked.</summary>
        private static bool InfoButton(UiSkin skin, Rect rect, bool open)
        {
            var hover = GUI.enabled && rect.Contains(Event.current.mousePosition);
            var face = open || hover ? UiSkin.AccentHover : UiSkin.Accent;
            if (!GUI.enabled)
            {
                face.a *= 0.5f;
            }

            if (open)
            {
                skin.DrawGlow(rect, UiSkin.Accent, 0.35f);
            }

            Fill(skin, new Rect(rect.x - 3f, rect.y - 3f, rect.width + 6f, rect.height + 6f), PlankInnerEdge, rect.width / 2f + 3f);
            Fill(skin, rect, face, rect.width / 2f);
            GUI.Label(new Rect(rect.x, rect.y - 1f, rect.width, rect.height), GameTexts.WindowHeader.InfoButton, _infoGlyph);
            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        // ------------------------------------------------------------------ helpers

        private static void Fill(UiSkin skin, Rect rect, Color color, float radius)
        {
            GUI.DrawTexture(rect, skin.White, ScaleMode.StretchToFill, true, 0, color, 0, radius);
        }

        private static Color Rgb(int rgb)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
        }

        private static void EnsureStyles(UiSkin skin)
        {
            if (_title != null)
            {
                return;
            }

            _title = new GUIStyle(skin.Title) { font = skin.TitleBoldFont, fontSize = 30, alignment = TextAnchor.MiddleLeft, wordWrap = false, clipping = TextClipping.Overflow };
            _title.normal.textColor = TitleInk;
            _titleShadow = new GUIStyle(_title);
            _titleShadow.normal.textColor = new Color(TitleShade.r, TitleShade.g, TitleShade.b, 0.9f);
            _infoGlyph = new GUIStyle(skin.Title) { font = skin.TitleBoldFont, fontSize = 18, alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Overflow };
            _infoGlyph.normal.textColor = InfoInk;
            _counter = new GUIStyle(skin.Heading) { fontSize = 18, alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Clip };
            _counter.normal.textColor = UiSkin.Text;
            _tipTitle = new GUIStyle(skin.Heading) { fontSize = 16, wordWrap = false };
            _tipTitle.normal.textColor = UiSkin.AccentHover;
            _tipBody = new GUIStyle(skin.Body) { fontSize = UiSkin.SmallSize, wordWrap = true };
        }
    }
}
