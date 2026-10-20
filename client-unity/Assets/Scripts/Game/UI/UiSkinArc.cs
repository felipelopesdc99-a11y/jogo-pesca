using System.Collections.Generic;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The ARC skin of the interface (docs/ART_BIBLE_PIXEL.md, section 7; addendum A-184): night-blue panels with a thin
    /// gold frame, pixel pills, pixel bars and the two pixel fonts. Everything here is drawn by the code from small
    /// textures made once (Point filter), never from painted files.
    /// </summary>
    /// <remarks>
    /// One pixel of interface art is <see cref="ArcPixel"/> pixels of the 1080-tall canvas (Art Bible 3.2). Shapes are
    /// 9-slice masks: a stepped corner (chamfer or pixel circle) that keeps its size and a middle that stretches, so a
    /// pill or a frame of any size costs a handful of draw calls and no allocation per frame.
    /// </remarks>
    public sealed partial class UiSkin
    {
        /// <summary>Canvas pixels per pixel of interface art (Art Bible 3.2: escala 2×).</summary>
        public const int ArcPixel = 2;

        /// <summary>Rings a mask set can draw (outer gold, dark gap, dark gold).</summary>
        private const int MaxRings = 3;

        // ------------------------------------------------------------------ palette (tema_visual.json, "arc")

        /// <summary>The thin gold of the frame: structure, never reward.</summary>
        public static Color FrameGold => Theme.Frame;

        /// <summary>The second, darker line of the frame and the border of fields.</summary>
        public static Color FrameDark => Theme.FrameDark;

        /// <summary>The dark line between the two gold lines.</summary>
        public static Color FrameInner => Theme.FrameInner;

        /// <summary>The colour of titles in Press Start 2P.</summary>
        public static Color TitleColor => Theme.Title;

        /// <summary>Where a window's content sits: the panel night (#0D0F20).</summary>
        public static Color PanelColor => Theme.Panel;

        /// <summary>Fields, secondary pills, bar tracks behind numbers.</summary>
        public static Color FieldColor => Theme.Field;

        /// <summary>Fish and item cards.</summary>
        public static Color CardColor => Theme.Card;

        /// <summary>Idle chips and the border of plain cards.</summary>
        public static Color ChipOffColor => Theme.ChipOff;

        /// <summary>Attribute and gene bars (#7FC4EA).</summary>
        public static Color AttributeColor => Theme.Attribute;

        /// <summary>The red of the alert counters (with a reward-gold border).</summary>
        public static Color AlertColor => Theme.Alert;

        /// <summary>Dark text on a bright face (the alert counter, a rarity seal): #0B0D1E.</summary>
        public static Color DarkInk => Theme.CardLine;

        // ------------------------------------------------------------------ readable coloured text

        /// <summary>The contrast every coloured text reaches against the cards (a margin over 4.5:1).</summary>
        private const float ReadableContrast = 4.6f;

        private static readonly Dictionary<GUIStyle, GUIStyle> ColoredStyles = new Dictionary<GUIStyle, GUIStyle>();
        private static readonly Dictionary<Color, Color> ReadableCache = new Dictionary<Color, Color>();

        /// <summary>
        /// A label in <paramref name="color"/>, set as the text colour (tinting with GUI.contentColor would multiply the
        /// style's own colour). With <paramref name="readable"/> the colour is first made readable on the cards
        /// (<see cref="Readable"/>); with false it is used as given (text on a dark seal).
        /// </summary>
        public static void ColoredLabel(Rect rect, string text, GUIStyle style, Color color, bool readable = true)
        {
            style = _instance != null ? _instance.TitleSafe(style, text) : style;
            if (!ColoredStyles.TryGetValue(style, out var colored))
            {
                colored = new GUIStyle(style);
                ColoredStyles[style] = colored;
            }

            var c = readable ? Readable(color) : color;
            colored.normal.textColor = c;
            colored.hover.textColor = c;
            GUI.Label(rect, text, colored);
        }

        /// <summary>
        /// <paramref name="color"/> lightened (towards white, same hue) until it reads at 4.5:1 on a card, the lightest of
        /// the ARC fills text sits on: rarity, size and accent colours used as text (Épico and Lendário need it). Colours
        /// that already pass come back unchanged.
        /// </summary>
        public static Color Readable(Color color)
        {
            var key = new Color(color.r, color.g, color.b, 1f);
            if (!ReadableCache.TryGetValue(key, out var readable))
            {
                readable = key;
                for (var i = 0; i < 40 && Contrast(readable, Theme.Card) < ReadableContrast; i++)
                {
                    readable = Color.Lerp(readable, Color.white, 0.06f);
                }

                ReadableCache[key] = readable;
            }

            return new Color(readable.r, readable.g, readable.b, color.a);
        }

        /// <summary>WCAG contrast ratio between two colours (alpha ignored).</summary>
        public static float Contrast(Color a, Color b)
        {
            var la = Luminance(a);
            var lb = Luminance(b);
            return (Mathf.Max(la, lb) + 0.05f) / (Mathf.Min(la, lb) + 0.05f);
        }

        private static float Luminance(Color c)
        {
            return 0.2126f * Channel(c.r) + 0.7152f * Channel(c.g) + 0.0722f * Channel(c.b);
        }

        private static float Channel(float v)
        {
            return v <= 0.03928f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
        }

        // ------------------------------------------------------------------ fonts

        /// <summary>Press Start 2P, the title font (OFL). Null when the file is missing.</summary>
        public Font PixelTitleFont { get; private set; }

        /// <summary>VT323, the text font (OFL). Null when the file is missing.</summary>
        public Font PixelTextFont { get; private set; }

        private readonly Dictionary<GUIStyle, GUIStyle> _accentSafe = new Dictionary<GUIStyle, GUIStyle>();

        /// <summary>
        /// Press Start 2P draws the accented capitals (Á É Í Ó Ú Â Ê Ô Ã Õ À) squeezed into the letter cell, so they read as
        /// lower case ("ÉPICO" looks like "éPICO"). A title whose text has one of them is drawn in VT323 at the same visual
        /// size instead (the twin style is made once and kept). Returns <paramref name="style"/> for every other text.
        /// </summary>
        public GUIStyle TitleSafe(GUIStyle style, string text)
        {
            if (style == null || PixelTitleFont == null || PixelTextFont == null || style.font != PixelTitleFont || !HasAccentedCapital(text))
            {
                return style;
            }

            if (!_accentSafe.TryGetValue(style, out var twin))
            {
                // Capitals of Press Start 2P fill the em; VT323 capitals are 0,56 em: the same height needs 1,75× the size.
                twin = new GUIStyle(style) { font = PixelTextFont, fontSize = Mathf.RoundToInt(style.fontSize * AccentTwinScale) };
                twin.padding = new RectOffset(style.padding.left, style.padding.right, style.padding.top - TextLift(twin.fontSize), style.padding.bottom);
                _accentSafe[style] = twin;
            }

            // The style may have been recoloured since (ColoredLabel and the window styles set their own colours).
            twin.normal.textColor = style.normal.textColor;
            twin.hover.textColor = style.hover.textColor;
            return twin;
        }

        /// <summary>A label in a title style, switched to VT323 when the text has an accented capital (<see cref="TitleSafe"/>).</summary>
        public void TitleLabel(Rect rect, string text, GUIStyle style)
        {
            GUI.Label(rect, text, TitleSafe(style, text));
        }

        /// <summary>Whether <paramref name="text"/> has an accented capital (no allocation).</summary>
        public static bool HasAccentedCapital(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            for (var i = 0; i < text.Length; i++)
            {
                switch (text[i])
                {
                    case 'Á': case 'É': case 'Í': case 'Ó': case 'Ú': case 'Â': case 'Ê': case 'Ô': case 'Ã': case 'Õ': case 'À': case 'Ü':
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// How far a VT323 line is lifted inside its rect. VT323 keeps a quarter of its line above the capitals (room for
        /// accents) and its descenders reach the bottom of the line; lifting it by ~0,12 em centres the letters where a
        /// line of the old font sat, so rects sized for one line keep the "g", "p" and "ç" whole.
        /// </summary>
        private static int TextLift(int fontSize) => Mathf.RoundToInt(fontSize * TextLiftEm);

        // ------------------------------------------------------------------ textures made once

        /// <summary>
        /// A 9-slice shape of <paramref name="rings"/> lines around a fill, in pixel steps of <see cref="ArcPixel"/>:
        /// <paramref name="round"/> gives a pixel circle of <paramref name="radiusUi"/> art pixels on each corner, otherwise a
        /// stepped chamfer. The fill can be split in two bands (top and bottom halves). Returns the texture and its border.
        /// </summary>
        public static Texture2D ArcTexture(int radiusUi, bool round, Color[] rings, Color fill, Color fillBottom, out int border)
        {
            var reach = Mathf.Max(radiusUi, rings.Length);
            var n = (reach + 1) * 2 + 2;
            border = (reach + 1) * ArcPixel;
            var size = n * ArcPixel;
            var pixels = new Color32[size * size];
            for (var v = 0; v < n; v++)
            {
                for (var u = 0; u < n; u++)
                {
                    var depth = Depth(u, v, n, radiusUi, round);
                    Color c;
                    if (depth < 0)
                    {
                        c = Color.clear;
                    }
                    else if (depth < rings.Length)
                    {
                        c = rings[depth];
                    }
                    else
                    {
                        // Row 0 of the texture is the bottom.
                        c = v < n / 2 ? fillBottom : fill;
                    }

                    var c32 = (Color32)c;
                    for (var y = 0; y < ArcPixel; y++)
                    {
                        for (var x = 0; x < ArcPixel; x++)
                        {
                            pixels[(v * ArcPixel + y) * size + u * ArcPixel + x] = c32;
                        }
                    }
                }
            }

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave,
            };
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Distance of art pixel (u, v) from the outside of an n × n shape, in art pixels (-1 = outside): 0 is the outer
        /// line. Corners are a pixel circle of radius r (round) or a 45° chamfer of r steps.
        /// </summary>
        private static int Depth(int u, int v, int n, int r, bool round)
        {
            var dx = Mathf.Min(u, n - 1 - u);
            var dy = Mathf.Min(v, n - 1 - v);
            var straight = Mathf.Min(dx, dy);
            if (r <= 0 || dx >= r || dy >= r)
            {
                return straight;
            }

            if (!round)
            {
                return Mathf.Min(straight, dx + dy - r);
            }

            var ox = r - (dx + 0.5f);
            var oy = r - (dy + 0.5f);
            return Mathf.Min(straight, Mathf.FloorToInt(r - Mathf.Sqrt(ox * ox + oy * oy)));
        }

        private static GUIStyle ArcBox(int radiusUi, bool round, Color[] rings, Color fill)
        {
            var tex = ArcTexture(radiusUi, round, rings, fill, fill, out var border);
            var style = new GUIStyle(GUI.skin.box)
            {
                border = new RectOffset(border, border, border, border),
                padding = new RectOffset(16, 16, 14, 14),
                margin = new RectOffset(0, 0, 0, 0),
            };
            style.normal.background = tex;
            style.normal.textColor = Theme.Text;
            return style;
        }

        private static Texture2D ArcFace(int radiusUi, bool round, Color fill, Color fillBottom, params Color[] rings)
        {
            return ArcTexture(radiusUi, round, rings, fill, fillBottom, out _);
        }

        /// <summary>Border of the textures made by <see cref="ArcFace"/> for a radius and ring count.</summary>
        private static int ArcFaceBorder(int radiusUi, int rings) => (Mathf.Max(radiusUi, rings) + 1) * ArcPixel;

        // ------------------------------------------------------------------ tinted masks (pills, discs, frames of any colour)

        private readonly Dictionary<int, Texture2D[]> _masks = new Dictionary<int, Texture2D[]>();

        /// <summary>
        /// White masks of one shape: [0..2] each line alone (outer first), then [3 + k] everything from line k inwards
        /// ([3] the whole shape, [6] the fill under three lines). Tinted and stacked they draw a shape of any colours.
        /// </summary>
        private Texture2D[] Masks(int radiusUi, bool round)
        {
            var key = radiusUi * 2 + (round ? 1 : 0);
            if (_masks.TryGetValue(key, out var set))
            {
                return set;
            }

            set = new Texture2D[MaxRings * 2 + 1];
            for (var d = 0; d < MaxRings; d++)
            {
                var rings = new Color[MaxRings];
                rings[d] = Color.white;
                set[d] = ArcTexture(radiusUi, round, rings, Color.clear, Color.clear, out _);
            }

            for (var k = 0; k <= MaxRings; k++)
            {
                var rings = new Color[MaxRings];
                for (var i = 0; i < MaxRings; i++)
                {
                    rings[i] = i < k ? Color.clear : Color.white;
                }

                set[MaxRings + k] = ArcTexture(radiusUi, round, rings, Color.white, Color.white, out _);
            }

            _masks[key] = set;
            return set;
        }

        /// <summary>The corner radius (art pixels) that makes a pill of a rect's height: half of it, in whole art pixels.</summary>
        public static int PillRadius(float height) => Mathf.Clamp(Mathf.FloorToInt(height / (2f * ArcPixel)) - 1, 1, 18);

        /// <summary>
        /// Draws a shape: <paramref name="rings"/> lines (outer first: <paramref name="a"/>, <paramref name="b"/>,
        /// <paramref name="c"/>) around a <paramref name="fill"/>. Each line and the fill have their own mask, so a
        /// translucent (or clear) fill never shows the lines through it.
        /// </summary>
        public void Shape(Rect rect, int radiusUi, bool round, Color fill, int rings, Color a, Color b, Color c)
        {
            if (Event.current.type != EventType.Repaint || rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            rect = Snap(rect);
            var masks = Masks(radiusUi, round);
            var border = ArcFaceBorder(radiusUi, MaxRings);
            var previous = GUI.color;
            rings = Mathf.Clamp(rings, 0, MaxRings);
            for (var i = 0; i < rings; i++)
            {
                var color = i == 0 ? a : i == 1 ? b : c;
                if (color.a <= 0f)
                {
                    continue;
                }

                GUI.color = previous * color;
                NineSlice(rect, masks[i], border, border, border, border, 1f);
            }

            if (fill.a > 0f)
            {
                GUI.color = previous * fill;
                NineSlice(rect, masks[MaxRings + rings], border, border, border, border, 1f);
            }

            GUI.color = previous;
        }

        /// <summary>A pill (round ends in pixel steps): a fill with a one-pixel line of <paramref name="line"/> (clear = none).</summary>
        public void PixelPill(Rect rect, Color fill, Color line)
        {
            Shape(rect, PillRadius(rect.height), true, fill, line.a > 0f ? 1 : 0, line, Color.clear, Color.clear);
        }

        /// <summary>A pixel disc with the three-line ARC frame (gold, dark gap, dark gold) around <paramref name="fill"/>.</summary>
        public void FramedDisc(Rect rect, Color fill, Color outer)
        {
            Shape(rect, PillRadius(rect.height), true, fill, 3, outer, FrameInner, FrameDark);
        }

        /// <summary>A box with a stepped chamfer and one line (fields, tooltips, wells).</summary>
        public void LineBox(Rect rect, Color fill, Color line, int chamfer = 1)
        {
            Shape(rect, chamfer, false, fill, line.a > 0f ? 1 : 0, line, Color.clear, Color.clear);
        }

        /// <summary>A box with the full ARC frame: gold, a dark line, dark gold, then <paramref name="fill"/>.</summary>
        public void FramedBox(Rect rect, Color fill, Color outer)
        {
            Shape(rect, 2, false, fill, 3, outer, FrameInner, FrameDark);
        }

        /// <summary>The small tooltip box: the panel night with a dark-gold line.</summary>
        public void TipBox(Rect rect)
        {
            LineBox(rect, new Color(Theme.Panel.r, Theme.Panel.g, Theme.Panel.b, 0.97f), FrameDark, 1);
        }

        /// <summary>A rect moved to whole canvas pixels, so the art pixels of a shape never land between screen pixels at 1080p.</summary>
        public static Rect Snap(Rect r)
        {
            return new Rect(Mathf.Round(r.x), Mathf.Round(r.y), Mathf.Round(r.width), Mathf.Round(r.height));
        }

        // ------------------------------------------------------------------ bars

        private Texture2D _blocks;

        /// <summary>A 12 × 1 white strip with a darker 2-pixel seam at its end: repeated over a bar fill, it draws the pixel blocks.</summary>
        private Texture2D Blocks
        {
            get
            {
                if (_blocks != null)
                {
                    return _blocks;
                }

                const int w = 6 * ArcPixel;
                _blocks = new Texture2D(w, 1, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Repeat,
                    filterMode = FilterMode.Point,
                    hideFlags = HideFlags.HideAndDontSave,
                };
                for (var x = 0; x < w; x++)
                {
                    _blocks.SetPixel(x, 0, x >= w - ArcPixel ? new Color(0.80f, 0.80f, 0.80f, 1f) : Color.white);
                }

                _blocks.Apply();
                return _blocks;
            }
        }

        /// <summary>
        /// The ARC bar: the track, a dark-gold line when the bar is tall enough for it, and the fill in pixel blocks (a seam
        /// every six art pixels), its length rounded to whole art pixels.
        /// </summary>
        private void ArcBar(Rect rect, float fraction, Color color, Color seam)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            rect = Snap(rect);
            var inner = rect;
            if (rect.height >= 10f)
            {
                GUI.DrawTexture(rect, White, ScaleMode.StretchToFill, true, 0, FrameDark, 0, 0);
                inner = new Rect(rect.x + ArcPixel, rect.y + ArcPixel, rect.width - ArcPixel * 2, rect.height - ArcPixel * 2);
            }

            GUI.DrawTexture(inner, White, ScaleMode.StretchToFill, true, 0, Theme.BarTrack, 0, 0);
            var f = Mathf.Clamp01(fraction);
            if (f <= 0f)
            {
                return;
            }

            var width = f >= 1f ? inner.width : Mathf.Max(ArcPixel, Mathf.Floor(inner.width * f / ArcPixel) * ArcPixel);
            var fill = new Rect(inner.x, inner.y, width, inner.height);
            var previous = GUI.color;
            GUI.color = previous * color;
            GUI.DrawTextureWithTexCoords(fill, Blocks, new Rect(0f, 0f, fill.width / Blocks.width, 1f), true);
            if (seam.a > 0f && inner.height >= 6f)
            {
                // A one-pixel light edge along the top: the light comes from above (Art Bible 4.3).
                GUI.color = previous * seam;
                GUI.DrawTexture(new Rect(fill.x, fill.y, fill.width, ArcPixel), White, ScaleMode.StretchToFill, true);
            }

            GUI.color = previous;
        }

        /// <summary>The fisher's XP bar: the Art Bible's turquoise-green stripes (#7FD6C2 / #62C0AD).</summary>
        public void XpBar(Rect rect, float fraction)
        {
            ArcBar(rect, fraction, Theme.Xp, new Color(1f, 1f, 1f, 0.25f));
        }

        /// <summary>An attribute or gene bar (#7FC4EA).</summary>
        public void AttributeBar(Rect rect, float fraction)
        {
            ArcBar(rect, fraction, Theme.Attribute, new Color(1f, 1f, 1f, 0.25f));
        }

        // ------------------------------------------------------------------ pieces of a window

        /// <summary>The veil behind windows (#0A0C1C at 45%).</summary>
        public void Veil(Rect screen)
        {
            var v = Theme.Veil;
            GUI.DrawTexture(screen, White, ScaleMode.StretchToFill, true, 0, new Color(v.r, v.g, v.b, Theme.OverlayOpacity), 0, 0);
        }

        /// <summary>A hard pixel shadow one art pixel down and right of a floating piece (no soft blur in pixel art).</summary>
        public void HardShadow(Rect rect)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            var shadow = new Rect(rect.x + ArcPixel * 2, rect.y + ArcPixel * 2, rect.width, rect.height);
            Shape(shadow, 2, false, new Color(0f, 0f, 0f, 0.35f), 0, Color.clear, Color.clear, Color.clear);
        }

        /// <summary>
        /// The pixel "brilho" (Art Bible 4.10): a one-art-pixel ring just outside <paramref name="rect"/>, in
        /// <paramref name="color"/> at <paramref name="strength"/> (0–1). Callers that pulse it pass a changing strength.
        /// </summary>
        public void PixelGlow(Rect rect, Color color, float strength)
        {
            var a = Mathf.Clamp01(strength);
            if (a <= 0.01f)
            {
                return;
            }

            var ring = new Rect(rect.x - ArcPixel * 2, rect.y - ArcPixel * 2, rect.width + ArcPixel * 4, rect.height + ArcPixel * 4);
            var radius = rect.height >= 40f && rect.width >= rect.height * 0.9f && rect.width <= rect.height * 1.1f ? PillRadius(ring.height) : 2;
            Shape(ring, radius, radius > 2, Color.clear, 1, new Color(color.r, color.g, color.b, color.a * a), Color.clear, Color.clear);
        }
    }
}
