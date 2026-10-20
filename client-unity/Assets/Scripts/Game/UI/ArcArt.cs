using FishingIdle.Game.Visual;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// Shared pixel pieces of the ARC screens (M25-T13, T14, T16; addendum A-188): rounded shapes turned into the skin's
    /// stepped shapes, backdrops at a whole scale cropped at the edges, mirrored pixel drawings, and the small things the
    /// Art Bible asks the code to draw (bubbles, the pixel aura). Only drawing; textures are made once.
    /// </summary>
    /// <remarks>
    /// The screens used to draw smooth rounded rects (GUI.DrawTexture with a corner radius). <see cref="Fill"/> and
    /// <see cref="Line"/> keep their place and colours and swap the shape: a pixel pill or disc when the radius made a
    /// round end, a stepped chamfer otherwise (Art Bible 7: "cantos com chanfro em degrau, nunca arredondados").
    /// </remarks>
    public static class ArcArt
    {
        // ------------------------------------------------------------------ shapes

        /// <summary>A filled pixel shape where a rounded rect of <paramref name="radius"/> was (UiSkin.ArcFill, TD-063).</summary>
        public static void Fill(UiSkin skin, Rect rect, Color color, float radius)
        {
            skin.ArcFill(rect, color, radius);
        }

        /// <summary>A one-art-pixel line where a rounded outline of <paramref name="radius"/> was (UiSkin.ArcLine, TD-063).</summary>
        public static void Line(UiSkin skin, Rect rect, Color color, float radius)
        {
            skin.ArcLine(rect, color, radius);
        }

        /// <summary>A pixel disc (or pill) with a one-pixel line; clear line = none.</summary>
        public static void Disc(UiSkin skin, Rect rect, Color fill, Color line)
        {
            skin.ArcFillLine(rect, fill, line, Mathf.Max(rect.width, rect.height));
        }

        /// <summary>
        /// The translucent "glass" over water (the Aquarium's sheet, drawer and messages): the panel night, a light-blue
        /// line and a one-pixel light edge at the top (the light comes from above).
        /// </summary>
        public static void Glass(UiSkin skin, Rect rect)
        {
            var night = VisualTheme.Current.Panel;
            skin.Shape(rect, 2, false, new Color(night.r, night.g, night.b, 0.86f), 2, new Color(0.62f, 0.86f, 0.96f, 0.55f), new Color(0f, 0f, 0f, 0.35f), Color.clear);
            if (Event.current.type == EventType.Repaint && rect.width > 24f)
            {
                var p = UiSkin.ArcPixel;
                GUI.DrawTexture(UiSkin.Snap(new Rect(rect.x + p * 4, rect.y + p * 2, rect.width - p * 8, p)), skin.White, ScaleMode.StretchToFill, true, 0, new Color(1f, 1f, 1f, 0.16f), 0, 0);
            }
        }

        /// <summary>
        /// A "/" split across <paramref name="rect"/>: <paramref name="left"/> above-left of the diagonal,
        /// <paramref name="right"/> below-right, in steps of two art pixels (no soft edge in pixel art).
        /// </summary>
        public static void Diagonal(Rect rect, Color left, Color right)
        {
            if (Event.current.type != EventType.Repaint || rect.height <= 0f)
            {
                return;
            }

            var skin = UiSkin.Get();
            const float step = UiSkin.ArcPixel * 2;
            for (var y = Mathf.Round(rect.y); y < rect.yMax; y += step)
            {
                var h = Mathf.Min(step, rect.yMax - y);
                var k = (y + h * 0.5f - rect.y) / rect.height;
                var split = Mathf.Round((rect.x + rect.width * (1f - k)) / UiSkin.ArcPixel) * UiSkin.ArcPixel;
                split = Mathf.Clamp(split, rect.x, rect.xMax);
                GUI.DrawTexture(new Rect(rect.x, y, split - rect.x, h), skin.White, ScaleMode.StretchToFill, true, 0, left, 0, 0);
                GUI.DrawTexture(new Rect(split, y, rect.xMax - split, h), skin.White, ScaleMode.StretchToFill, true, 0, right, 0, 0);
            }
        }

        // ------------------------------------------------------------------ pixel art at a whole scale

        /// <summary>
        /// A backdrop (Art Bible 3.3: "na maior escala inteira, cortando as bordas"): PixelDraw.Cover (TD-063), with
        /// <paramref name="anchorX"/>/<paramref name="anchorY"/> picking what stays in sight (0 = left/top edge, 1 = right/
        /// bottom edge). Returns where the whole picture lies.
        /// </summary>
        public static Rect Cover(Rect rect, Texture tex, Color color, float anchorX = 0.5f, float anchorY = 0.5f)
        {
            return PixelDraw.Cover(rect, tex, color, anchorX, anchorY);
        }

        /// <summary>
        /// Pixel art at the largest whole scale that fits <paramref name="rect"/> (as <see cref="PixelDraw.Fit(Rect, Texture, Color)"/>),
        /// mirrored left-right when <paramref name="flip"/> (mirroring is allowed, turning is not: Art Bible 4.9).
        /// Returns the rect drawn.
        /// </summary>
        public static Rect Fit(Rect rect, Texture tex, Color color, bool flip = false)
        {
            if (tex == null)
            {
                return rect;
            }

            var drawn = PixelDraw.FitRect(rect, ref tex);
            if (Event.current.type == EventType.Repaint)
            {
                var previous = GUI.color;
                GUI.color = previous * color;
                GUI.DrawTextureWithTexCoords(drawn, tex, flip ? new Rect(1f, 0f, -1f, 1f) : new Rect(0f, 0f, 1f, 1f), true);
                GUI.color = previous;
            }

            return drawn;
        }

        /// <summary>
        /// A drawing made by this code (always pixel art) at the largest whole scale of its art pixel that fits
        /// <paramref name="rect"/>, at least 1×, centred and on a screen pixel.
        /// </summary>
        private static void Whole(Rect rect, Texture2D tex, Color color)
        {
            var unit = PixelDraw.ScreenScale();
            var k = Mathf.Max(1, PixelDraw.WholeScale(rect, tex, unit));
            var w = tex.width * k / unit;
            var h = tex.height * k / unit;
            var x = Mathf.Round((rect.center.x - w / 2f) * unit) / unit;
            var y = Mathf.Round((rect.center.y - h / 2f) * unit) / unit;
            GUI.DrawTexture(new Rect(x, y, w, h), tex, ScaleMode.StretchToFill, true, 0, color, 0, 0);
        }

        // ------------------------------------------------------------------ drawn by the code

        private static Texture2D _bubbleSmall, _bubbleBig, _aura;

        /// <summary>
        /// A rising bubble (Art Bible: "bolhas pelo código"): a pixel ring with a light dot, 4 or 6 art pixels across
        /// (<paramref name="big"/>), drawn at the interface scale (one art pixel = <see cref="UiSkin.ArcPixel"/>) around
        /// <paramref name="centre"/>.
        /// </summary>
        public static void Bubble(Vector2 centre, bool big, Color color)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            var tex = big ? (_bubbleBig ?? (_bubbleBig = MakeBubble(6))) : (_bubbleSmall ?? (_bubbleSmall = MakeBubble(4)));
            var size = tex.width * UiSkin.ArcPixel;
            var x = Mathf.Round(centre.x - size / 2f);
            var y = Mathf.Round(centre.y - size / 2f);
            // Bubbles move in whole art pixels.
            x -= x % UiSkin.ArcPixel;
            y -= y % UiSkin.ArcPixel;
            GUI.DrawTexture(new Rect(x, y, size, size), tex, ScaleMode.StretchToFill, true, 0, color, 0, 0);
        }

        /// <summary>
        /// The aura behind a fish on its pedestal, in pixel art (Art Bible 4.10: no diffuse glow): a dithered disc, dense
        /// in the middle and thinning out in an ordered 4 × 4 pattern, tinted by the rarity the service reports and drawn
        /// at a whole scale. <paramref name="strength"/> is the old soft aura's (≈ 0,03 Comum to 0,14 Mítico).
        /// </summary>
        public static void Aura(Vector2 centre, float diameter, Color color, float strength)
        {
            if (Event.current.type != EventType.Repaint || diameter < 8f || strength <= 0f)
            {
                return;
            }

            var tex = _aura ?? (_aura = MakeAura(40));
            Whole(new Rect(centre.x - diameter / 2f, centre.y - diameter / 2f, diameter, diameter), tex, new Color(color.r, color.g, color.b, Mathf.Clamp01(strength * 5f)));
        }

        private static readonly int[] Bayer4 = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };

        private static Texture2D MakeAura(int n)
        {
            var tex = NewTexture(n, n);
            var r = n / 2f;
            for (var y = 0; y < n; y++)
            {
                for (var x = 0; x < n; x++)
                {
                    var dx = x + 0.5f - r;
                    var dy = y + 0.5f - r;
                    var d = Mathf.Sqrt(dx * dx + dy * dy) / r;
                    var density = d >= 1f ? 0f : Mathf.Pow(1f - d, 1.3f) * 0.95f;
                    var on = density > (Bayer4[(y % 4) * 4 + x % 4] + 0.5f) / 16f;
                    tex.SetPixel(x, y, on ? Color.white : Color.clear);
                }
            }

            tex.Apply();
            return tex;
        }

        private static Texture2D MakeBubble(int n)
        {
            var tex = NewTexture(n, n);
            for (var y = 0; y < n; y++)
            {
                for (var x = 0; x < n; x++)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }

            // Row 0 is the bottom. A ring without its four corners, a light dot near the top-left.
            var last = n - 1;
            for (var i = 1; i < last; i++)
            {
                tex.SetPixel(i, 0, new Color(1f, 1f, 1f, 0.75f));
                tex.SetPixel(i, last, new Color(1f, 1f, 1f, 0.75f));
                tex.SetPixel(0, i, new Color(1f, 1f, 1f, 0.75f));
                tex.SetPixel(last, i, new Color(1f, 1f, 1f, 0.75f));
            }

            for (var y = 1; y < last; y++)
            {
                for (var x = 1; x < last; x++)
                {
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0.12f));
                }
            }

            tex.SetPixel(1, last - 1, Color.white);
            tex.Apply();
            return tex;
        }

        private static Texture2D NewTexture(int w, int h)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave,
            };
        }
    }
}
