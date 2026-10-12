using System.Collections.Generic;
using FishingIdle.Game.Scene;
using FishingIdle.Game.Visual;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The shared look of a full window, the "Diário de bordo" pattern the owner chose on 12/10/2026 (addendum A-180):
    /// the scene dimmed behind a dark, slightly warm veil; a wooden frame with a sheet of light paper inside; the
    /// title on a navy label with a gold border pinned to the top-left corner; a red seal "✕" on the top-right corner
    /// to close; the window's sub-tabs as coloured bookmarks sticking out of the right side.
    /// </summary>
    /// <remarks>
    /// The sheet turns the skin's paper mode on (<see cref="UiSkin.Paper"/>): everything a window draws on it uses
    /// dark ink (contrasts in A-180). What used to be the subtitle is split by what it is:
    /// <list type="bullet">
    /// <item><b>info</b> — a sentence that explains the menu, behind a round turquoise "i" on the label; clicking it
    /// opens the "Como funciona" note, drawn over the window body by <see cref="DrawInfoBalloon"/>.</item>
    /// <item><b>counter</b> — a short live number or time ("12 / 20 peixes", "Tempo: 0:42"), in sight on a sunken
    /// spot of the sheet next to the label.</item>
    /// </list>
    /// The free strip between the label (and its counter) and the seal is left in <see cref="Side"/> for the window's
    /// own header pieces (the Expedition's Cardume strength, the Ranking trophy).
    /// ASSET_PENDENTE: ui_diario_madeira, ui_diario_papel, ui_diario_etiqueta, ui_diario_lacre, ui_diario_marcador
    /// (docs/ASSETS_PENDENTES.md); each piece falls back to the drawing below while its file is missing.
    /// </remarks>
    public static class WindowFrame
    {
        // Layout, relative to the wooden frame. The content area keeps the old place (28 px in, 80 px down), so no
        // window had to move its content.
        private const float Margin = 28f, Wood = 14f;
        private const float ContentTop = 80f, ContentBottom = 24f;
        private const float LabelLeft = -12f, LabelTop = -10f, LabelHeight = 56f;
        private const float LabelPadLeft = 18f, LabelPadRight = 16f, LabelIcon = 32f, LabelIconGap = 10f;
        private const float SealSize = 54f, SealInset = 20f;
        private const float SideTop = 18f, SideHeight = 46f, SideGap = 12f;

        // The bookmarks: how far they stick out of the frame, and their size.
        private const float BookmarkOut = 72f, BookmarkUnder = 8f, BookmarkTop = 92f, BookmarkGap = 8f, BookmarkIcon = 28f, BookmarkTuck = 8f;

        // The "i" on the label and its note.
        private const float InfoSize = 28f, TipWidth = 460f, TipPad = 16f;

        // Art (Resources/Arte/UI). ASSET_PENDENTE: every file below; the drawing is the fallback.
        private const string WoodArt = "UI/ui_diario_madeira", PaperArt = "UI/ui_diario_papel", LabelArt = "UI/ui_diario_etiqueta",
            SealArt = "UI/ui_diario_lacre", BookmarkArt = "UI/ui_diario_marcador";

        /// <summary>Borders kept whole by the 9-slice files (texture pixels; see docs/ASSETS_PENDENTES.md).</summary>
        private const float WoodArtBorder = 48f, PaperArtBorder = 32f, LabelArtSide = 40f, LabelArtCap = 24f, BookmarkArtSide = 24f;

        // Colours of the drawn fallback (the example the owner chose).
        private static readonly Color WoodFill = Rgb(0x9B673D), WoodSeam = Rgb(0x8A5A33), WoodEdge = Rgb(0x4B301A);
        private static readonly Color LabelFill = Rgb(0x16304E), LabelTopFill = Rgb(0x1D3D60), LabelEdge = Rgb(0xF6B93B);
        private static readonly Color SealFill = Rgb(0xB83A2E), SealLight = Rgb(0xE0675A), SealLip = Rgb(0x6B1D16), SealEdge = Rgb(0xFFD2CB);
        private static readonly Color TitleInk = Rgb(0xF4F8FC), TitleShade = Rgb(0x061423), InfoInk = Rgb(0x06313A), PaperShade = Rgb(0x7A5A36);

        /// <summary>Bookmark colours, in tab order (white text on each at 4.7:1 or more).</summary>
        private static readonly Color[] BookmarkColors =
        {
            Rgb(0x1E7F7C), Rgb(0x2F5F9E), Rgb(0x94621A), Rgb(0x6A43A0), Rgb(0xA0473A), Rgb(0x2E7A4A),
        };

        private static readonly Dictionary<string, int> LastFrame = new Dictionary<string, int>();

        private static GUIStyle _title, _titleShadow, _infoGlyph, _counter, _tipTitle, _tipBody, _paper, _bookmark, _bookmarkNote;

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

        /// <summary>The content area of a frame: the sheet, less its margins and the label.</summary>
        public static Rect Content(Rect panel)
        {
            return new Rect(panel.x + Margin, panel.y + ContentTop, panel.width - Margin * 2f, panel.height - ContentTop - ContentBottom);
        }

        /// <summary>
        /// Dims the scene, draws the wooden frame and its sheet, and turns paper mode on; returns the frame rect.
        /// With <paramref name="bookmarks"/>, the frame is narrower by the bookmarks' width so the whole window keeps
        /// its old footprint.
        /// </summary>
        public static Rect Panel(UiSkin skin, float screenWidth, float screenHeight, float maxWidth, float maxHeight, bool bookmarks = false)
        {
            UiSkin.SetPaper(false);
            EnsureStyles(skin);

            // The veil: the night overlay plus a light warm tone (a real blur costs a render texture; not worth it).
            GUI.DrawTexture(new Rect(0, 0, screenWidth, screenHeight), skin.Overlay);
            Fill(skin, new Rect(0, 0, screenWidth, screenHeight), new Color(UiSkin.Ink.r, UiSkin.Ink.g, UiSkin.Ink.b, 0.12f), 0f);

            var width = Mathf.Min(maxWidth, screenWidth - 80f);
            var height = Mathf.Min(maxHeight, screenHeight - 120f);
            var panel = new Rect((screenWidth - width) / 2f, (screenHeight - height) / 2f + 20f, width, height);
            if (bookmarks)
            {
                panel = new Rect(panel.x, panel.y, panel.width - BookmarkOut, panel.height);
            }

            DrawWood(skin, panel);
            DrawSheet(skin, Sheet(panel));
            _panel = panel;
            UiSkin.SetPaper(true);
            return panel;
        }

        /// <summary>The paper sheet inside a frame.</summary>
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

            var seal = new Rect(panel.xMax - SealInset - SealSize / 2f, panel.y + SealInset - SealSize / 2f, SealSize, SealSize);
            var hasInfo = !string.IsNullOrEmpty(info);
            var hasCounter = !string.IsNullOrEmpty(counter);

            // The label is as wide as its title, never reaching the seal (a counter keeps at least 150 px).
            var labelX = panel.x + LabelLeft;
            var iconWidth = icon != null ? LabelIcon + LabelIconGap : 0f;
            var infoWidth = hasInfo ? InfoSize + 12f : 0f;
            var maxLabel = seal.x - 16f - reserveRight - labelX - 16f - (hasCounter ? 150f : 0f);
            var titleSpace = Mathf.Max(40f, maxLabel - LabelPadLeft - iconWidth - infoWidth - LabelPadRight);
            var titleText = FishCard.Fit(title ?? string.Empty, _title, titleSpace);
            var titleWidth = Mathf.Min(_title.CalcSize(new GUIContent(titleText)).x, titleSpace);
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
            GUI.Label(new Rect(titleRect.x, titleRect.y + 2f, titleRect.width, titleRect.height), titleText, _titleShadow);
            GUI.Label(titleRect, titleText, _title);

            if (hasInfo && InfoButton(skin, infoRect, tipOpen))
            {
                _tipKey = tipOpen ? null : key;
                if (_tipKey != null)
                {
                    // Opened in this event: DrawInfoBalloon runs later in the same event and must see it as drawn.
                    KeepTip(panel, label, info, frame);
                }
            }

            // The free strip right of the label; a counter takes its start, on a sunken spot of the sheet.
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
            var art = ArtAssets.Texture(BookmarkArt);
            for (var i = 0; i < labels.Count; i++)
            {
                var icon = icons != null && i < icons.Count ? icons[i] : null;
                var note = notes != null && i < notes.Count ? notes[i] : null;
                var textWidth = width - BookmarkUnder - 12f;
                var labelHeight = _bookmark.CalcHeight(new GUIContent(labels[i]), textWidth);
                var noteHeight = string.IsNullOrEmpty(note) ? 0f : 16f;
                var height = 12f + (icon != null ? BookmarkIcon + 2f : 0f) + labelHeight + noteHeight + 10f;
                var on = i == active;
                var tab = new Rect(_panel.xMax - BookmarkUnder - (on ? 0f : BookmarkTuck), y, width, height);
                var hover = GUI.enabled && tab.Contains(Event.current.mousePosition);
                var color = BookmarkColors[i % BookmarkColors.Length];
                if (!on)
                {
                    color = Color.Lerp(color, UiSkin.Ink, hover ? 0.05f : 0.22f);
                }

                // A shadow, the coloured tab (rounded on the outer side only) and a white edge on the open one.
                GUI.DrawTexture(new Rect(tab.x + 3f, tab.y + 4f, tab.width, tab.height), skin.White, ScaleMode.StretchToFill, true, 0, new Color(0f, 0f, 0f, 0.28f), Vector4.zero, new Vector4(0f, 12f, 12f, 0f));
                if (art != null)
                {
                    var previous = GUI.color;
                    GUI.color = previous * color;
                    UiSkin.NineSlice(tab, art, BookmarkArtSide, BookmarkArtSide, BookmarkArtSide, BookmarkArtSide, 1f);
                    GUI.color = previous;
                }
                else
                {
                    GUI.DrawTexture(tab, skin.White, ScaleMode.StretchToFill, true, 0, color, Vector4.zero, new Vector4(0f, 12f, 12f, 0f));
                    GUI.DrawTexture(new Rect(tab.x, tab.y, tab.width, 3f), skin.White, ScaleMode.StretchToFill, true, 0, new Color(1f, 1f, 1f, 0.14f), Vector4.zero, new Vector4(0f, 3f, 0f, 0f));
                }

                if (on)
                {
                    GUI.DrawTexture(tab, skin.White, ScaleMode.StretchToFill, true, 0, new Color(1f, 1f, 1f, 0.7f), new Vector4(0f, 2f, 2f, 2f), new Vector4(0f, 12f, 12f, 0f));
                }

                var cy = tab.y + 12f;
                var textX = tab.x + BookmarkUnder + 6f;
                if (icon != null)
                {
                    skin.DrawIconExact(new Rect(textX + textWidth / 2f - BookmarkIcon / 2f, cy, BookmarkIcon, BookmarkIcon), icon, Color.white);
                    cy += BookmarkIcon + 2f;
                }

                GUI.Label(new Rect(textX, cy, textWidth, labelHeight), labels[i], _bookmark);
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
        /// The window's own scene (aquarium tank, archipelago, nautical chart) set on the sheet like a photo: a white
        /// border with a soft shadow around <paramref name="scene"/>. Draw it before the scene.
        /// </summary>
        public static void Photo(UiSkin skin, Rect scene)
        {
            const float border = 6f;
            var outer = new Rect(scene.x - border, scene.y - border, scene.width + border * 2f, scene.height + border * 2f);
            Fill(skin, new Rect(outer.x + 3f, outer.y + 4f, outer.width, outer.height), new Color(PaperShade.r, PaperShade.g, PaperShade.b, 0.35f), 6f);
            Fill(skin, outer, Rgb(0xFFFAF0), 6f);
        }

        /// <summary>
        /// The footer of a sheet: a dashed line over the bottom <paramref name="height"/> px of <paramref name="area"/>,
        /// where the main action goes. Returns the footer rect (below the line).
        /// </summary>
        public static Rect Footer(UiSkin skin, Rect area, float height)
        {
            var y = area.yMax - height;
            for (var x = area.x; x < area.xMax; x += 10f)
            {
                Fill(skin, new Rect(x, y - 8f, Mathf.Min(6f, area.xMax - x), 1.5f), UiSkin.PaperLine, 0f);
            }

            return new Rect(area.x, y, area.width, height);
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
            Fill(skin, new Rect(r.x - 2f, r.y + 5f, r.width + 4f, r.height + 4f), new Color(0f, 0f, 0f, 0.3f), 10f);
            Fill(skin, r, UiSkin.PaperCard, 8f);
            GUI.DrawTexture(r, skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.PaperLine, 1.5f, 8f);
            GUI.DrawTexture(new Rect(r.x, r.y, 5f, r.height), skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.NightAccent, Vector4.zero, new Vector4(8f, 0f, 0f, 8f));
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

        /// <summary>
        /// A centred confirmation box over everything, on the night panel (paper mode off: dialogs keep the light
        /// text of the Art Bible). Returns the box rect.
        /// </summary>
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

        private static void DrawWood(UiSkin skin, Rect r)
        {
            var previous = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0f, 0f, 0f, 1f);
            skin.DrawShadow(r);
            GUI.backgroundColor = previous;

            var art = ArtAssets.Texture(WoodArt);
            if (art != null)
            {
                UiSkin.NineSlice(r, art, WoodArtBorder, WoodArtBorder, WoodArtBorder, WoodArtBorder, 0.5f);
                return;
            }

            // Vertical planks: the wood with a darker seam every 46 px, a dark edge and a light line along the top.
            const float radius = 16f;
            Fill(skin, r, WoodFill, radius);
            for (var x = r.x + 46f; x < r.xMax - 10f; x += 46f)
            {
                Fill(skin, new Rect(x, r.y + 3f, 2f, r.height - 6f), WoodSeam, 0f);
            }

            Fill(skin, new Rect(r.x + 10f, r.y + 3f, r.width - 20f, 1.5f), new Color(1f, 1f, 1f, 0.16f), 0f);
            GUI.DrawTexture(r, skin.White, ScaleMode.StretchToFill, true, 0, WoodEdge, 3f, radius);
        }

        private static void DrawSheet(UiSkin skin, Rect r)
        {
            var art = ArtAssets.Texture(PaperArt);
            if (art != null)
            {
                UiSkin.NineSlice(r, art, PaperArtBorder, PaperArtBorder, PaperArtBorder, PaperArtBorder, 0.5f);
                return;
            }

            GUI.Box(r, GUIContent.none, _paper);
            // A soft brown shade along the edges, as if the sheet sat a little under the frame.
            GUI.DrawTexture(r, skin.White, ScaleMode.StretchToFill, true, 0, new Color(PaperShade.r, PaperShade.g, PaperShade.b, 0.30f), 3f, 10f);
            GUI.DrawTexture(new Rect(r.x + 3f, r.y + 3f, r.width - 6f, r.height - 6f), skin.White, ScaleMode.StretchToFill, true, 0, new Color(PaperShade.r, PaperShade.g, PaperShade.b, 0.10f), 4f, 8f);
        }

        private static void DrawLabel(UiSkin skin, Rect r)
        {
            Fill(skin, new Rect(r.x + 2f, r.y + 6f, r.width, r.height), new Color(0f, 0f, 0f, 0.35f), 10f);
            var art = ArtAssets.Texture(LabelArt);
            if (art != null)
            {
                UiSkin.NineSlice(r, art, LabelArtSide, LabelArtSide, LabelArtCap, LabelArtCap, r.height / art.height);
                return;
            }

            const float radius = 9f;
            Fill(skin, r, LabelFill, radius);
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, r.height * 0.45f), skin.White, ScaleMode.StretchToFill, true, 0, LabelTopFill, Vector4.zero, new Vector4(radius, radius, 0f, 0f));
            GUI.DrawTexture(r, skin.White, ScaleMode.StretchToFill, true, 0, LabelEdge, 2.5f, radius);
        }

        /// <summary>
        /// The red wax seal with a "✕" on the top-right corner of <paramref name="panel"/> (windows that draw their own
        /// header, like the Profile, use it directly). Returns true when clicked.
        /// </summary>
        public static bool CloseSeal(UiSkin skin, Rect panel)
        {
            var rect = new Rect(panel.xMax - SealInset - SealSize / 2f, panel.y + SealInset - SealSize / 2f, SealSize, SealSize);
            var hover = GUI.enabled && rect.Contains(Event.current.mousePosition);
            var previous = GUI.color;
            if (!GUI.enabled)
            {
                GUI.color = previous * new Color(1f, 1f, 1f, 0.5f);
            }

            var art = ArtAssets.Texture(SealArt);
            if (art != null)
            {
                GUI.DrawTexture(rect, art, ScaleMode.ScaleToFit, true);
                if (hover)
                {
                    Fill(skin, rect, new Color(1f, 1f, 1f, 0.12f), rect.width / 2f);
                }
            }
            else
            {
                var r = rect.width / 2f;
                Fill(skin, new Rect(rect.x, rect.y + 4f, rect.width, rect.height), SealLip, r);
                Fill(skin, rect, hover ? Color.Lerp(SealFill, Color.white, 0.12f) : SealFill, r);
                Fill(skin, new Rect(rect.x + rect.width * 0.18f, rect.y + rect.height * 0.12f, rect.width * 0.5f, rect.height * 0.42f), new Color(SealLight.r, SealLight.g, SealLight.b, 0.55f), rect.width * 0.25f);
                GUI.DrawTexture(rect, skin.White, ScaleMode.StretchToFill, true, 0, SealEdge, 2f, r);
            }

            var glyph = rect.width * 0.4f;
            skin.DrawIconExact(new Rect(rect.center.x - glyph / 2f, rect.center.y - glyph / 2f, glyph, glyph), Icons.Close, Color.white);
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

            Fill(skin, new Rect(rect.x - 2f, rect.y - 2f, rect.width + 4f, rect.height + 4f), LabelFill, rect.width / 2f + 2f);
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

        // These styles set their own colours (white on the label, ink on the sheet) and are not tracked by the skin.
        private static void EnsureStyles(UiSkin skin)
        {
            if (_title != null)
            {
                return;
            }

            _title = new GUIStyle(skin.Title) { font = skin.TitleBoldFont, fontSize = 28, alignment = TextAnchor.MiddleLeft, wordWrap = false, clipping = TextClipping.Overflow };
            _title.normal.textColor = TitleInk;
            _titleShadow = new GUIStyle(_title);
            _titleShadow.normal.textColor = new Color(TitleShade.r, TitleShade.g, TitleShade.b, 0.9f);
            _infoGlyph = new GUIStyle(skin.Title) { font = skin.TitleBoldFont, fontSize = 18, alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Overflow };
            _infoGlyph.normal.textColor = InfoInk;
            _counter = new GUIStyle(skin.Heading) { fontSize = 18, alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Clip };
            _counter.normal.textColor = UiSkin.Ink;
            _tipTitle = new GUIStyle(skin.Heading) { fontSize = 16, wordWrap = false };
            _tipTitle.normal.textColor = UiSkin.InkAccent;
            _tipBody = new GUIStyle(skin.Body) { fontSize = UiSkin.SmallSize, wordWrap = true };
            _tipBody.normal.textColor = UiSkin.Ink;
            _bookmark = new GUIStyle(skin.SmallBold) { fontSize = UiSkin.ChipSize, alignment = TextAnchor.UpperCenter, wordWrap = true, clipping = TextClipping.Overflow };
            _bookmark.normal.textColor = Color.white;
            _bookmarkNote = new GUIStyle(_bookmark) { font = skin.BodyFont, fontSize = UiSkin.BadgeSize, wordWrap = false, clipping = TextClipping.Clip };
            _bookmarkNote.normal.textColor = new Color(1f, 1f, 1f, 0.92f);
            _paper = new GUIStyle(GUIStyle.none) { border = new RectOffset(10, 10, 10, 10) };
            _paper.normal.background = Art.PanelTexture(UiSkin.PaperFill, UiSkin.PaperFillBottom, UiSkin.PaperFill, 10, 0f, 128, 0f);
        }
    }
}
