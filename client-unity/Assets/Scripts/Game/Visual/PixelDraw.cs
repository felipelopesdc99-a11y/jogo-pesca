using UnityEngine;

namespace FishingIdle.Game.Visual
{
    /// <summary>
    /// Draws art in the IMGUI interface (TD-059, M25-T04): pixel art at the largest whole scale that fits the rect,
    /// in real screen pixels, never smoothed; the old art keeps the ScaleToFit of before.
    /// </summary>
    /// <remarks>
    /// The HUD draws in a virtual 1080-pixel-high frame scaled by GUI.matrix, so the whole scale is counted in screen
    /// pixels (rect size × the matrix scale). When not even 1× fits, the small "_p" version is used (the fish); when
    /// that does not fit either, the drawing is reduced like before (ScaleToFit) — readable, if not crisp. Nothing
    /// is allocated: it only computes a rect.
    /// </remarks>
    public static class PixelDraw
    {
        /// <summary>Like GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit, true).</summary>
        public static void Fit(Rect rect, Texture tex)
        {
            Fit(rect, tex, Color.white);
        }

        /// <summary>Like GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit, true, 0, color, 0, 0).</summary>
        public static void Fit(Rect rect, Texture tex, Color color)
        {
            if (tex == null)
            {
                return;
            }

            if (!ArtAssets.IsPixel(tex))
            {
                GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit, true, 0, color, 0, 0);
                return;
            }

            var drawn = FitRect(rect, ref tex);
            if (color.a > 0f && IsDarkItem(tex))
            {
                Well(drawn, color.a);
            }

            GUI.DrawTexture(drawn, tex, ScaleMode.StretchToFill, true, 0, color, 0, 0);
        }

        // Items whose art is mostly navy or dark leather and disappears on the slate cards of A-190 (measured in the
        // research of 20/10/2026): drawn on a light well, a square of the raised panel colour a little larger than the
        // drawing. The files did not change.
        private static readonly System.Collections.Generic.HashSet<string> DarkItems = new System.Collections.Generic.HashSet<string>
        {
            "Varas/rod_03", "Varas/rod_04", "Varas/rod_05",
            "Fragmentos/frag_rod_03", "Fragmentos/frag_rod_04", "Fragmentos/frag_rod_05",
            "Roupas/pants", "Roupas/gloves", "Roupas/boots",
        };

        // Backgrounds 55–71% dark that turn into holes on the lighter panel (A-190): a veil of the panel colour over them.
        private static readonly System.Collections.Generic.HashSet<string> DarkBackdrops = new System.Collections.Generic.HashSet<string>
        {
            "UI/ui_map_sea_bg", "UI/ui_exp_carta", "UI/cla_fundo", "Boss/boss_fundo_abismo",
        };

        /// <summary>How much of the panel colour is laid over a dark backdrop.</summary>
        private const float BackdropLift = 0.3f;

        private static bool IsDarkItem(Texture tex)
        {
            var path = ArtAssets.PathOfTexture(tex);
            if (path == null)
            {
                return false;
            }

            // The "_p" small versions share the look.
            return DarkItems.Contains(path.EndsWith("_p", System.StringComparison.Ordinal) ? path.Substring(0, path.Length - 2) : path);
        }

        private static void Well(Rect drawn, float alpha)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            var unit = ScreenScale();
            var pad = Mathf.Max(2f, Mathf.Round(2f * unit) / unit);
            var theme = VisualTheme.Current;
            var c = Color.Lerp(theme.PanelElevated, theme.Text, 0.22f);
            GUI.DrawTexture(new Rect(drawn.x - pad, drawn.y - pad, drawn.width + pad * 2f, drawn.height + pad * 2f), Texture2D.whiteTexture,
                ScaleMode.StretchToFill, true, 0, new Color(c.r, c.g, c.b, 0.85f * alpha), 0, 0);
        }

        /// <summary>
        /// An icon: the coloured pixel icons are never tinted (only the alpha of <paramref name="color"/> applies, so a
        /// disabled icon still fades); the one-colour symbols and the old white icons take the colour as before.
        /// </summary>
        public static void Icon(Rect rect, Texture tex, string name, Color color)
        {
            if (tex != null && ArtAssets.IsPixel(tex) && !Icons.IsSymbol(name))
            {
                color = new Color(1f, 1f, 1f, color.a);
            }

            Fit(rect, tex, color);
        }

        /// <summary>
        /// The rect a pixel texture takes inside <paramref name="rect"/>: centred, at the largest whole scale in screen
        /// pixels, its corner on a screen pixel. Swaps <paramref name="tex"/> for its "_p" version when 1× does not fit.
        /// Non-pixel textures get the ScaleToFit rect.
        /// </summary>
        public static Rect FitRect(Rect rect, ref Texture tex)
        {
            if (tex == null || rect.width <= 0f || rect.height <= 0f)
            {
                return rect;
            }

            var unit = ScreenScale();
            if (ArtAssets.IsPixel(tex))
            {
                var k = WholeScale(rect, tex, unit);
                if (k < 1)
                {
                    var small = ArtAssets.Small(tex);
                    if (small != null && WholeScale(rect, small, unit) >= 1)
                    {
                        tex = small;
                        k = WholeScale(rect, small, unit);
                    }
                }

                if (k >= 1)
                {
                    var w = tex.width * k / unit;
                    var h = tex.height * k / unit;
                    var x = Snap(rect.x + (rect.width - w) * 0.5f, unit);
                    var y = Snap(rect.y + (rect.height - h) * 0.5f, unit);
                    return new Rect(x, y, w, h);
                }
            }

            // Reduced like ScaleToFit (old art, or pixel art in a rect smaller than its 1×).
            var aspect = tex.width / (float)tex.height;
            var fw = rect.width;
            var fh = fw / aspect;
            if (fh > rect.height)
            {
                fh = rect.height;
                fw = fh * aspect;
            }

            return new Rect(rect.x + (rect.width - fw) * 0.5f, rect.y + (rect.height - fh) * 0.5f, fw, fh);
        }

        /// <summary>
        /// A background or a banner covering <paramref name="rect"/> (Art Bible 3.3, "fundos e faixas das telas"): pixel
        /// art at the smallest whole scale that fills the rect, centred on <paramref name="focusX"/>/<paramref name="focusY"/>
        /// (0–1 of the picture) and cut at the rect's edges; the old art keeps the ScaleAndCrop of before. Returns the rect
        /// the whole picture takes (larger than <paramref name="rect"/>), to place things on a part of it.
        /// </summary>
        public static Rect Cover(Rect rect, Texture tex, Color color, float focusX = 0.5f, float focusY = 0.5f)
        {
            if (tex == null || rect.width <= 0f || rect.height <= 0f)
            {
                return rect;
            }

            var drawn = CoverRect(rect, tex, focusX, focusY);
            var shown = Rect.MinMaxRect(Mathf.Max(drawn.xMin, rect.xMin), Mathf.Max(drawn.yMin, rect.yMin), Mathf.Min(drawn.xMax, rect.xMax), Mathf.Min(drawn.yMax, rect.yMax));
            if (shown.width <= 0f || shown.height <= 0f)
            {
                return drawn;
            }

            // Texture v grows upwards: the top of the shown part is the larger v.
            var uv = new Rect(
                (shown.xMin - drawn.xMin) / drawn.width,
                1f - (shown.yMax - drawn.yMin) / drawn.height,
                shown.width / drawn.width,
                shown.height / drawn.height);
            var previous = GUI.color;
            GUI.color = previous * color;
            GUI.DrawTextureWithTexCoords(shown, tex, uv, true);
            GUI.color = previous;
            var path = ArtAssets.PathOfTexture(tex);
            if (path != null && DarkBackdrops.Contains(path) && Event.current.type == EventType.Repaint)
            {
                var panel = VisualTheme.Current.Panel;
                GUI.DrawTexture(shown, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(panel.r, panel.g, panel.b, BackdropLift * color.a), 0, 0);
            }

            return drawn;
        }

        /// <summary>
        /// The rect a whole picture takes when <see cref="Cover"/> draws it into <paramref name="rect"/>: for pixel art the
        /// smallest whole scale in screen pixels that covers the rect, its corner on a screen pixel; otherwise the
        /// ScaleAndCrop scale.
        /// </summary>
        public static Rect CoverRect(Rect rect, Texture tex, float focusX = 0.5f, float focusY = 0.5f)
        {
            if (tex == null || rect.width <= 0f || rect.height <= 0f)
            {
                return rect;
            }

            var unit = ScreenScale();
            float w, h;
            if (ArtAssets.IsPixel(tex))
            {
                var k = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(rect.width * unit / tex.width, rect.height * unit / tex.height) - 0.001f));
                w = tex.width * k / unit;
                h = tex.height * k / unit;
            }
            else
            {
                var s = Mathf.Max(rect.width / tex.width, rect.height / tex.height);
                w = tex.width * s;
                h = tex.height * s;
            }

            // The focus point of the picture goes to the centre of the rect, without uncovering an edge.
            var x = Mathf.Clamp(rect.center.x - w * Mathf.Clamp01(focusX), rect.xMax - w, rect.x);
            var y = Mathf.Clamp(rect.center.y - h * Mathf.Clamp01(focusY), rect.yMax - h, rect.y);
            return new Rect(Snap(x, unit), Snap(y, unit), w, h);
        }

        /// <summary>The largest whole scale of <paramref name="tex"/> that fits <paramref name="rect"/> on screen (0 if none).</summary>
        public static int WholeScale(Rect rect, Texture tex, float unit)
        {
            var kx = Mathf.FloorToInt(rect.width * unit / tex.width + 0.001f);
            var ky = Mathf.FloorToInt(rect.height * unit / tex.height + 0.001f);
            return Mathf.Max(0, Mathf.Min(kx, ky));
        }

        /// <summary>Screen pixels per unit of the current GUI space (the HUD's virtual frame scale).</summary>
        public static float ScreenScale()
        {
            var m = GUI.matrix;
            var s = new Vector2(m.m00, m.m10).magnitude;
            return s > 0.0001f ? s : 1f;
        }

        private static float Snap(float v, float unit) => Mathf.Round(v * unit) / unit;
    }
}
