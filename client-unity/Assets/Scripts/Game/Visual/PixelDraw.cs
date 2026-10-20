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
            GUI.DrawTexture(drawn, tex, ScaleMode.StretchToFill, true, 0, color, 0, 0);
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
