using System.Collections.Generic;
using FishingIdle.Game.Scene;
using FishingIdle.Game.Visual;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The shared look of a full window in the ARC skin (addendum A-184, which updates the "Diário de bordo" of A-170):
    /// the scene dimmed behind the night veil; a night-blue panel with the thin gold frame; the title in Press Start 2P
    /// on a raised tag with a gold line at the top-left; a pixel "✕" pill at the top-right to close; the window's
    /// sub-tabs as ARC tabs sticking out of the right side.
    /// </summary>
    /// <remarks>
    /// The layout of A-170 is kept to the pixel (content area, header strip, tabs), so no window had to move its content;
    /// only the pieces are drawn differently. Paper mode no longer turns on. What used to be the subtitle is split by
    /// what it is:
    /// <list type="bullet">
    /// <item><b>info</b> — a sentence that explains the menu, behind a round turquoise "i" on the label; clicking it
    /// opens the "Como funciona" note, drawn over the window body by <see cref="DrawInfoBalloon"/>.</item>
    /// <item><b>counter</b> — a short live number or time ("12 / 20 peixes", "Tempo: 0:42"), in sight on a sunken
    /// spot of the sheet next to the label.</item>
    /// </list>
    /// The free strip between the label (and its counter) and the seal is left in <see cref="Side"/> for the window's
    /// own header pieces (the Expedition's Cardume strength, the Ranking trophy).
    /// Every piece is drawn by the code (<see cref="UiSkin.Shape"/>); the diary's painted files (ui_diario_*) are no longer
    /// read and leave in M25-T17.
    /// </remarks>
    public static class WindowFrame
    {
        // Layout, relative to the panel. The content area keeps the old place (28 px in, 80 px down), so no window had to
        // move its content.
        private const float Margin = 28f, Wood = 14f;

        // Each side keeps clear of the HUD's round shortcuts (20 margin + 96 column + room for their labels), which stay
        // clickable beside an open window.
        private const float ShortcutClearance = 140f;
        private const float ContentTop = 80f, ContentBottom = 24f;
        // The title tag sits inside the frame now (the diary's label stuck out of the corner).
        private const float LabelLeft = 12f, LabelTop = 14f, LabelHeight = 52f;
        private const float LabelPadLeft = 16f, LabelPadRight = 16f, LabelIcon = 32f, LabelIconGap = 10f;

        // The closing "✕": a 44 px pill 18 px inside the top-right corner (its left edge stays right of HeaderRight).
        private const float SealSize = 54f, SealInset = 24f, CloseSize = 44f, CloseInset = 18f;
        private const float SideTop = 18f, SideHeight = 46f, SideGap = 12f;

        // The bookmarks: how far they stick out of the frame, and their size.
        private const float BookmarkOut = 96f, BookmarkUnder = 8f, BookmarkTop = 92f, BookmarkGap = 8f, BookmarkIcon = 28f, BookmarkTuck = 8f;

        // The "i" on the label and its note.
        private const float InfoSize = 28f, TipWidth = 460f, TipPad = 16f;

        /// <summary>Dark text on the turquoise "i" (#06313A, 6,3:1).</summary>
        private static readonly Color InfoInk = Rgb(0x06313A);

        private static readonly Dictionary<string, int> LastFrame = new Dictionary<string, int>();

        private static GUIStyle _title, _infoGlyph, _counter, _tipTitle, _tipBody, _bookmark, _bookmarkNote, _bookmarkOn;

        // The open "Como funciona" note: which header owns it, its text and where it goes.
        private static string _tipKey, _tipText;
        private static Rect _tipRect;
        private static int _tipFrame = -1;

        // The last frame drawn by Panel, for the bookmarks.
        private static Rect _panel;

        /// <summary>
        /// The free strip of the last header drawn: right of the label (and its counter), left of the seal, 46 px tall.
        /// Windows put their own header pieces here with <see cref="TakeSide"/>.
        /// </summary>
        public static Rect Side { get; private set; }

        /// <summary>
        /// Draws the frame and returns the content area. <paramref name="closed"/> is true when the seal was clicked.
        /// <paramref name="info"/> goes behind the "i"; <paramref name="counter"/> stays in sight next to the label.
        /// <paramref name="bookmarks"/> leaves room on the right for <see cref="Bookmarks"/>.
        /// </summary>
        public static Rect Draw(UiSkin skin, float screenWidth, float screenHeight, string title, string info, out bool closed, float maxWidth = 1320f, float maxHeight = 840f, string icon = null, string counter = null, bool bookmarks = false)
        {
            var panel = Panel(skin, screenWidth, screenHeight, maxWidth, maxHeight, bookmarks);
            closed = Header(skin, panel, title, info, icon, counter);
            return Content(panel);
        }

        /// <summary>
        /// Where the closing "✕" of a frame starts on the left (the old seal's place, kept), less the usual 16 px gap:
        /// header pieces a window draws itself (search, wallet) end here.
        /// </summary>
        public static float HeaderRight(Rect panel) => panel.xMax - SealInset - SealSize / 2f - 16f;

        /// <summary>The content area of a frame: the sheet, less its margins and the label.</summary>
        public static Rect Content(Rect panel)
        {
            return new Rect(panel.x + Margin, panel.y + ContentTop, panel.width - Margin * 2f, panel.height - ContentTop - ContentBottom);
        }

        /// <summary>
        /// Dims the scene behind the night veil and draws the ARC panel; returns the frame rect.
        /// With <paramref name="bookmarks"/>, the frame is narrower by the bookmarks' width so the whole window keeps
        /// its old footprint.
        /// </summary>
        public static Rect Panel(UiSkin skin, float screenWidth, float screenHeight, float maxWidth, float maxHeight, bool bookmarks = false)
        {
            UiSkin.SetPaper(false);
            EnsureStyles(skin);

            // The veil (#0A0C1C at 45%, Art Bible 7).
            GUI.DrawTexture(new Rect(0, 0, screenWidth, screenHeight), skin.Overlay);

            var width = Mathf.Min(maxWidth, screenWidth - ShortcutClearance * 2f);
            var height = Mathf.Min(maxHeight, screenHeight - 120f);
            var panel = new Rect((screenWidth - width) / 2f, (screenHeight - height) / 2f + 20f, width, height);
            if (bookmarks)
            {
                panel = new Rect(panel.x, panel.y, panel.width - BookmarkOut, panel.height);
            }

            DrawFrame(skin, panel);
            _panel = panel;
            return panel;
        }

        /// <summary>The inside of a frame (where the diary's paper sheet was).</summary>
        public static Rect Sheet(Rect panel)
        {
            return new Rect(panel.x + Wood, panel.y + Wood, panel.width - Wood * 2f, panel.height - Wood * 2f);
        }

        /// <summary>
        /// The label (icon and title), the "i" with <paramref name="info"/>, the <paramref name="counter"/> on a sunken
        /// spot and the seal that closes. <paramref name="reserveRight"/> keeps room left of the seal for the window's
        /// own controls (search, view toggle). Returns true when the seal was clicked.
        /// </summary>
        public static bool Header(UiSkin skin, Rect panel, string title, string info, string icon, string counter = null, float reserveRight = 0f)
        {
            EnsureStyles(skin);
            var key = (icon ?? string.Empty) + "|" + title;
            var frame = Time.frameCount;
            if ((!LastFrame.TryGetValue(key, out var last) || last < frame - 1) && _tipKey == key)
            {
                // Not drawn last frame: the window has just opened; start with the note closed.
                _tipKey = null;
            }

            LastFrame[key] = frame;

            var seal = CloseRect(panel);
            var hasInfo = !string.IsNullOrEmpty(info);
            var hasCounter = !string.IsNullOrEmpty(counter);

            // The label is as wide as its title, never reaching the seal (a counter keeps at least 150 px).
            var labelX = panel.x + LabelLeft;
            var iconWidth = icon != null ? LabelIcon + LabelIconGap : 0f;
            var infoWidth = hasInfo ? InfoSize + 12f : 0f;
            var maxLabel = seal.x - 16f - reserveRight - labelX - 16f - (hasCounter ? 150f : 0f);
            var titleSpace = Mathf.Max(40f, maxLabel - LabelPadLeft - iconWidth - infoWidth - LabelPadRight);
            var titleStyle = skin.TitleSafe(_title, title);
            var titleText = FishCard.Fit(title ?? string.Empty, titleStyle, titleSpace);
            var titleWidth = Mathf.Min(titleStyle.CalcSize(new GUIContent(titleText)).x, titleSpace);
            var label = new Rect(labelX, panel.y + LabelTop, LabelPadLeft + iconWidth + titleWidth + infoWidth + LabelPadRight, LabelHeight);
            var infoRect = new Rect(label.xMax - LabelPadRight - InfoSize, label.center.y - InfoSize / 2f, InfoSize, InfoSize);

            // The note: only while the window takes input (a confirmation dialog closes it).
            var tipOpen = hasInfo && _tipKey == key && GUI.enabled;
            if (!tipOpen && _tipKey == key)
            {
                _tipKey = null;
            }

            if (tipOpen)
            {
                KeepTip(panel, label, info, frame);

                // The header runs before the body, so it sees the click first: a click on the note never reaches
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
                        if (panel.Contains(e.mousePosition) && !seal.Contains(e.mousePosition))
                        {
                            e.Use();
                        }
                    }
                }
            }

            DrawLabel(skin, label);
            var x = label.x + LabelPadLeft;
            if (icon != null)
            {
                skin.DrawIconExact(new Rect(x, label.center.y - LabelIcon / 2f, LabelIcon, LabelIcon), icon, Color.white);
                x += iconWidth;
            }

            var titleRect = new Rect(x, label.y, titleWidth + 4f, label.height);
            GUI.Label(titleRect, titleText, titleStyle);

            if (hasInfo && InfoButton(skin, infoRect, tipOpen))
            {
                _tipKey = tipOpen ? null : key;
                if (_tipKey != null)
                {
                    // Opened in this event: DrawInfoBalloon runs later in the same event and must see it as drawn.
                    KeepTip(panel, label, info, frame);
                }
            }

            // The free strip right of the label; a counter takes its start, on a sunken field.
            var sideX = label.xMax + 16f;
            Side = new Rect(sideX, panel.y + SideTop, Mathf.Max(0f, seal.x - 16f - reserveRight - sideX), SideHeight);
            if (hasCounter && Side.width > 60f)
            {
                var well = TakeSide(Mathf.Min(_counter.CalcSize(new GUIContent(counter)).x + 36f, Side.width));
                skin.Inset(well);
                GUI.Label(well, FishCard.Fit(counter, _counter, well.width - 20f), _counter);
            }

            if (CloseSeal(skin, panel))
            {
                if (_tipKey == key)
                {
                    _tipKey = null;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// The window's sub-tabs as coloured bookmarks out of the right side of the last frame drawn (it must have been
        /// opened with bookmarks). <paramref name="notes"/> (optional) adds a short line under each label ("Você #1").
        /// Returns the index clicked, or -1.
        /// </summary>
        public static int Bookmarks(UiSkin skin, IList<string> labels, IList<string> icons, int active, IList<string> notes = null)
        {
            EnsureStyles(skin);
            if (labels == null || labels.Count == 0)
            {
                return -1;
            }

            var clicked = -1;
            var width = BookmarkOut + BookmarkUnder;
            var y = _panel.y + BookmarkTop;
            for (var i = 0; i < labels.Count; i++)
            {
                var icon = icons != null && i < icons.Count ? icons[i] : null;
                var note = notes != null && i < notes.Count ? notes[i] : null;
                var textWidth = width - BookmarkUnder - 8f;
                var labelHeight = _bookmark.CalcHeight(new GUIContent(labels[i]), textWidth);
                var noteHeight = string.IsNullOrEmpty(note) ? 0f : UiSkin.SmallLine;
                var height = 12f + (icon != null ? BookmarkIcon + 2f : 0f) + labelHeight + noteHeight + 10f;
                var on = i == active;
                var tab = new Rect(_panel.xMax - BookmarkUnder - (on ? 0f : BookmarkTuck), y, width, height);
                var hover = GUI.enabled && tab.Contains(Event.current.mousePosition);

                // ARC tabs (A-184): the open one is the raised panel with the gold frame and title-coloured text; the others
                // are the field with the dark-blue line and soft text (gold line on hover).
                skin.HardShadow(tab);
                if (on)
                {
                    skin.FramedBox(tab, VisualTheme.Current.PanelElevated, UiSkin.FrameGold);
                }
                else
                {
                    skin.LineBox(tab, VisualTheme.Current.Field, hover ? UiSkin.FrameGold : UiSkin.ChipOffColor, 2);
                }

                var cy = tab.y + 12f;
                var textX = tab.x + BookmarkUnder + 4f;
                if (icon != null)
                {
                    skin.DrawIconExact(UiSkin.Snap(new Rect(textX + textWidth / 2f - BookmarkIcon / 2f, cy, BookmarkIcon, BookmarkIcon)), icon, on || hover ? Color.white : new Color(1f, 1f, 1f, 0.75f));
                    cy += BookmarkIcon + 2f;
                }

                GUI.Label(new Rect(textX, cy, textWidth, labelHeight), labels[i], on ? _bookmarkOn : _bookmark);
                if (noteHeight > 0f)
                {
                    GUI.Label(new Rect(textX, cy + labelHeight, textWidth, noteHeight), note, _bookmarkNote);
                }

                if (GUI.Button(tab, GUIContent.none, GUIStyle.none) && !on)
                {
                    clicked = i;
                }

                y += height + BookmarkGap;
            }

            return clicked;
        }

        /// <summary>
        /// The window's own scene (aquarium tank, archipelago, nautical chart) set in the panel like a framed picture: the
        /// three-line ARC frame (gold, a dark line, dark gold) around <paramref name="scene"/>. Draw it before the scene.
        /// </summary>
        public static void Photo(UiSkin skin, Rect scene)
        {
            const float border = 6f;
            var outer = new Rect(scene.x - border, scene.y - border, scene.width + border * 2f, scene.height + border * 2f);
            skin.FramedBox(outer, VisualTheme.Current.Field, UiSkin.FrameGold);
        }

        /// <summary>
        /// A window's own picture (Resources/Arte/<paramref name="artPath"/>) set on the sheet like a photo (A-170, A-179):
        /// the white border with its shadow, then the art covering <paramref name="rect"/> (ScaleAndCrop: cropped to fit,
        /// never stretched). Returns the texture, or null when the file is missing (nothing is drawn; the caller keeps
        /// its old look).
        /// </summary>
        public static Texture2D PhotoArt(UiSkin skin, Rect rect, string artPath)
        {
            var art = ArtAssets.Texture(artPath);
            if (art == null || rect.width <= 0f || rect.height <= 0f)
            {
                return null;
            }

            Photo(skin, rect);
            GUI.DrawTexture(rect, art, ScaleMode.ScaleAndCrop, true, 0, Color.white, 0, 0f);
            return art;
        }

        /// <summary>
        /// A navy veil over part of a photo, so light text reads on it (A-179): <paramref name="left"/> alpha at the left
        /// edge easing to <paramref name="right"/> at the right edge, in 12 bands (no texture, no allocation).
        /// </summary>
        public static void Veil(UiSkin skin, Rect rect, float left, float right)
        {
            const int bands = 12;
            var night = UiSkin.SceneNight;
            var bandWidth = rect.width / bands;
            for (var i = 0; i < bands; i++)
            {
                var alpha = Mathf.Lerp(left, right, (i + 0.5f) / bands);
                Fill(skin, new Rect(rect.x + i * bandWidth, rect.y, bandWidth + (i < bands - 1 ? 1f : 0f), rect.height), new Color(night.r, night.g, night.b, alpha), 0f);
            }
        }

        /// <summary>
        /// Where a region of a picture lands when it is drawn ScaleAndCrop into <paramref name="drawn"/>:
        /// <paramref name="region"/> is in the picture's pixels (top-left origin). Used to put cards on a part of the art
        /// (the cork of the Eventos board).
        /// </summary>
        public static Rect CropMap(Rect drawn, Texture art, Rect region)
        {
            var scale = Mathf.Max(drawn.width / art.width, drawn.height / art.height);
            var ox = drawn.x + (drawn.width - art.width * scale) / 2f;
            var oy = drawn.y + (drawn.height - art.height * scale) / 2f;
            var r = Rect.MinMaxRect(ox + region.xMin * scale, oy + region.yMin * scale, ox + region.xMax * scale, oy + region.yMax * scale);
            return Rect.MinMaxRect(Mathf.Max(r.xMin, drawn.xMin), Mathf.Max(r.yMin, drawn.yMin), Mathf.Min(r.xMax, drawn.xMax), Mathf.Min(r.yMax, drawn.yMax));
        }

        /// <summary>
        /// Text with a dark outline (four 2 px offsets in navy, then the text), for a word drawn on a busy picture such as
        /// the "VS" over the duel emblem (A-179).
        /// </summary>
        public static void OutlinedLabel(Rect rect, string text, GUIStyle style)
        {
            var previous = GUI.contentColor;
            var night = UiSkin.SceneNight;
            GUI.contentColor = new Color(night.r, night.g, night.b, previous.a * 0.95f);
            GUI.Label(new Rect(rect.x - 2f, rect.y, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x + 2f, rect.y, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x, rect.y - 2f, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x, rect.y + 3f, rect.width, rect.height), text, style);
            GUI.contentColor = previous;
            GUI.Label(rect, text, style);
        }

        /// <summary>
        /// The footer of a window: a dotted dark-gold line (art pixels two apart) over the bottom <paramref name="height"/>
        /// px of <paramref name="area"/>, where the main action goes. Returns the footer rect (below the line).
        /// </summary>
        public static Rect Footer(UiSkin skin, Rect area, float height)
        {
            var y = Mathf.Round(area.yMax - height - 8f);
            if (Event.current.type == EventType.Repaint)
            {
                const float step = UiSkin.ArcPixel * 3;
                for (var x = Mathf.Round(area.x); x < area.xMax; x += step)
                {
                    Fill(skin, new Rect(x, y, Mathf.Min(UiSkin.ArcPixel * 2, area.xMax - x), UiSkin.ArcPixel), UiSkin.FrameDark, 0f);
                }
            }

            return new Rect(area.x, area.yMax - height, area.width, height);
        }

        /// <summary>Records the open note's text and place for <see cref="DrawInfoBalloon"/> this frame.</summary>
        private static void KeepTip(Rect panel, Rect label, string info, int frame)
        {
            var width = Mathf.Min(TipWidth, panel.width - Margin * 2f);
            var textHeight = _tipBody.CalcHeight(new GUIContent(info), width - TipPad * 2f);
            _tipRect = new Rect(label.x + 18f, label.yMax + 12f, width, TipPad + 22f + 4f + textHeight + TipPad);
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
        /// Ends the windows: turns paper mode off and draws the open "Como funciona" note over everything the windows
        /// drew this frame. The HUD calls it right after the windows; <paramref name="blocked"/> (a window dialog is
        /// open) closes the note.
        /// </summary>
        public static void DrawInfoBalloon(UiSkin skin, bool blocked)
        {
            UiSkin.SetPaper(false);
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
            // The ARC tooltip: the panel night with a dark-gold line and a turquoise stripe (the "i" is turquoise).
            skin.HardShadow(r);
            skin.TipBox(r);
            Fill(skin, UiSkin.Snap(new Rect(r.x + UiSkin.ArcPixel * 2, r.y + UiSkin.ArcPixel * 3, UiSkin.ArcPixel * 2, r.height - UiSkin.ArcPixel * 6)), UiSkin.NightAccent, 0f);
            GUI.Label(new Rect(r.x + TipPad, r.y + TipPad - 2f, r.width - TipPad * 2f, 22f), GameTexts.WindowHeader.InfoTitle, _tipTitle);
            GUI.Label(new Rect(r.x + TipPad, r.y + TipPad + 24f, r.width - TipPad * 2f, r.height - TipPad * 2f - 24f), _tipText, _tipBody);
        }

        /// <summary>Closes the "Como funciona" note (Esc). Returns true when it was open.</summary>
        public static bool CloseInfoBalloon()
        {
            if (_tipKey == null)
            {
                return false;
            }

            _tipKey = null;
            return true;
        }

        /// <summary>A centred confirmation box over everything, on the ARC panel. Returns the box rect.</summary>
        public static Rect Dialog(UiSkin skin, float screenWidth, float screenHeight, float height, float width = 640f)
        {
            UiSkin.SetPaper(false);
            var rect = new Rect(screenWidth / 2f - width / 2f, screenHeight / 2f - height / 2f, width, height);
            GUI.DrawTexture(new Rect(0, 0, screenWidth, screenHeight), skin.Overlay);
            skin.DrawShadow(rect);
            GUI.Box(rect, GUIContent.none, skin.PanelSolid);
            return rect;
        }

        // ------------------------------------------------------------------ the frame

        /// <summary>The ARC panel of a window: its hard shadow and the three-line gold frame around the night.</summary>
        private static void DrawFrame(UiSkin skin, Rect r)
        {
            skin.HardShadow(r);
            GUI.Box(r, GUIContent.none, skin.Panel);
        }

        /// <summary>The title tag: the raised panel with a gold line and the small chamfer, a one-pixel dark gap inside.</summary>
        private static void DrawLabel(UiSkin skin, Rect r)
        {
            skin.Shape(r, 2, false, VisualTheme.Current.PanelElevated, 2, UiSkin.FrameGold, UiSkin.FrameInner, Color.clear);
        }

        /// <summary>Where the closing "✕" of a frame goes.</summary>
        private static Rect CloseRect(Rect panel)
        {
            return new Rect(panel.xMax - CloseInset - CloseSize, panel.y + CloseInset, CloseSize, CloseSize);
        }

        /// <summary>
        /// The pixel "✕" on the top-right corner of <paramref name="panel"/>: a field pill with the gold line and a
        /// reward-gold cross drawn in art pixels (windows that draw their own header, like the Profile, use it directly).
        /// Returns true when clicked.
        /// </summary>
        public static bool CloseSeal(UiSkin skin, Rect panel)
        {
            var rect = CloseRect(panel);
            var hover = GUI.enabled && rect.Contains(Event.current.mousePosition);
            var previous = GUI.color;
            if (!GUI.enabled)
            {
                GUI.color = previous * new Color(1f, 1f, 1f, 0.5f);
            }

            var theme = VisualTheme.Current;
            skin.Pill(rect, hover ? theme.PanelElevated : theme.Field, hover ? theme.Title : UiSkin.FrameGold);
            if (Event.current.type == EventType.Repaint)
            {
                // A 7 × 7 art-pixel cross, two pixels thick, in the title colour.
                const int p = UiSkin.ArcPixel;
                var x0 = Mathf.Round(rect.center.x - 3.5f * p);
                var y0 = Mathf.Round(rect.center.y - 3.5f * p);
                var ink = hover ? Color.white : theme.Title;
                for (var i = 0; i < 7; i++)
                {
                    Fill(skin, new Rect(x0 + i * p, y0 + i * p, p, p), ink, 0f);
                    Fill(skin, new Rect(x0 + (6 - i) * p, y0 + i * p, p, p), ink, 0f);
                    if (i < 6)
                    {
                        Fill(skin, new Rect(x0 + (i + 1) * p, y0 + i * p, p, p), ink, 0f);
                        Fill(skin, new Rect(x0 + (5 - i) * p, y0 + i * p, p, p), ink, 0f);
                    }
                }
            }

            GUI.color = previous;
            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        /// <summary>The round turquoise "i". Returns true when clicked.</summary>
        private static bool InfoButton(UiSkin skin, Rect rect, bool open)
        {
            var hover = GUI.enabled && rect.Contains(Event.current.mousePosition);
            var face = open || hover ? UiSkin.NightAccentHover : UiSkin.NightAccent;
            if (!GUI.enabled)
            {
                face.a *= 0.5f;
            }

            skin.Pill(rect, face, UiSkin.FrameInner);
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

        // These styles set their own colours (title colour on the tag, text on the panel) and are not tracked by the skin.
        private static void EnsureStyles(UiSkin skin)
        {
            if (_title != null)
            {
                return;
            }

            _title = new GUIStyle(skin.Title) { font = skin.TitleBoldFont, fontSize = UiSkin.TitleSize, alignment = TextAnchor.MiddleLeft, wordWrap = false, clipping = TextClipping.Overflow };
            _title.normal.textColor = UiSkin.TitleColor;
            _infoGlyph = new GUIStyle(skin.Title) { font = skin.TitleBoldFont, fontSize = UiSkin.NumberSize, alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Overflow };
            _infoGlyph.normal.textColor = InfoInk;
            _counter = new GUIStyle(skin.Heading) { fontSize = UiSkin.BodySize, alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Clip };
            _counter.normal.textColor = UiSkin.Ink;
            _tipTitle = new GUIStyle(skin.Title) { fontSize = UiSkin.NumberSize, wordWrap = false };
            _tipTitle.normal.textColor = UiSkin.TitleColor;
            _tipBody = new GUIStyle(skin.Body) { fontSize = UiSkin.SmallSize, wordWrap = true };
            _tipBody.normal.textColor = UiSkin.Ink;
            // The tab label at the small size, the note under it at the chip size (Art Bible 7.1 minimums).
            _bookmark = new GUIStyle(skin.SmallBold) { fontSize = UiSkin.SmallSize, alignment = TextAnchor.UpperCenter, wordWrap = true, clipping = TextClipping.Overflow };
            _bookmark.normal.textColor = UiSkin.Muted;
            _bookmarkOn = new GUIStyle(_bookmark);
            _bookmarkOn.normal.textColor = UiSkin.TitleColor;
            _bookmarkNote = new GUIStyle(_bookmark) { font = skin.BodyFont, fontSize = UiSkin.ChipSize, wordWrap = false, clipping = TextClipping.Clip };
            _bookmarkNote.normal.textColor = UiSkin.Muted;
        }
    }
}
