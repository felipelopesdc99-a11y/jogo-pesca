using System.Collections.Generic;
using FishingIdle.Game.Visual;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// Draws the player's avatar (M18-T03): the chosen picture from Avatares, the fisherman's portrait (Cena/retrato) when
    /// none was chosen, and a clean coloured badge if an avatar's file is missing. The six avatars are busts on a
    /// transparent background: they sit on a calm sea-blue backdrop so every frame shows the same ground.
    /// </summary>
    /// <remarks>
    /// In pixel art (L2-02, M25-T16, A-188) the 64 × 64 busts are drawn at the largest whole scale that fits the frame (1×
    /// in the top bar, 2× in the Profile and the Arena), the 32 px "_p" version when even 1× does not fit (the Ranking
    /// lines). A pixel drawing is never cut by a smooth circle: a round frame (a radius of about half the frame) uses the
    /// "_r" version, already cut in a circle of pixels by tools/Arte/retratos_pixel.py, on a pixel disc; any other frame
    /// gets the square bust on a stepped box. The old painted avatars keep the cropped drawing of before.
    /// </remarks>
    public static class AvatarArt
    {
        private static readonly Color[] Badges =
        {
            new Color(0.15f, 0.77f, 0.76f), new Color(0.30f, 0.55f, 1f), new Color(0.96f, 0.73f, 0.23f),
            new Color(0.66f, 0.33f, 0.97f), new Color(0.17f, 0.80f, 0.50f), new Color(0.93f, 0.28f, 0.60f),
        };

        private const string Portrait = "Cena/retrato";

        public static void Draw(UiSkin skin, Rect rect, string avatarId, float radius = 10f)
        {
            var path = string.IsNullOrEmpty(avatarId) ? Portrait : PathOf(avatarId);
            var tex = ArtAssets.Texture(path);
            if (tex != null && ArtAssets.IsPixel(tex))
            {
                DrawPixel(skin, rect, path, tex, radius);
                return;
            }

            if (tex != null)
            {
                if (!string.IsNullOrEmpty(avatarId))
                {
                    GUI.DrawTexture(rect, skin.White, ScaleMode.StretchToFill, true, 0, Backdrop, 0, radius);
                }

                GUI.DrawTexture(rect, tex, ScaleMode.ScaleAndCrop, true, 0, Color.white, 0, radius);
                return;
            }

            var color = Badges[Index(avatarId) % Badges.Length];
            ArcArt.Fill(skin, rect, new Color(color.r, color.g, color.b, 0.25f), radius);
            var s = rect.width * 0.55f;
            skin.DrawIcon(new Rect(rect.center.x - s / 2f, rect.center.y - s / 2f, s, s), Icons.Profile, color);
        }

        /// <summary>The pixel bust: backdrop shape, then the bust at a whole scale, sitting on the bottom of the frame.</summary>
        private static void DrawPixel(UiSkin skin, Rect rect, string path, Texture2D square, float radius)
        {
            var round = radius > 0.5f && radius >= Mathf.Min(rect.width, rect.height) * 0.5f - 3f;
            Texture tex = square;
            if (round)
            {
                tex = ArtAssets.Texture(RoundOf(path)) ?? square;
                ArcArt.Disc(skin, rect, Backdrop, Color.clear);
            }
            else
            {
                ArcArt.Fill(skin, rect, Backdrop, radius);
            }

            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            // Whole scale (the "_p" half when 1× does not fit); a square bust stands on the bottom edge of the frame, a
            // round one stays centred on its disc.
            var drawn = PixelDraw.FitRect(rect, ref tex);
            if (!round)
            {
                var unit = PixelDraw.ScreenScale();
                drawn.y = Mathf.Round((rect.yMax - drawn.height) * unit) / unit;
            }

            GUI.DrawTexture(drawn, tex, ScaleMode.StretchToFill, true);
        }

        private static readonly Color Backdrop = new Color(0.24f, 0.55f, 0.67f, 1f);
        private static readonly Dictionary<string, string> Paths = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> RoundPaths = new Dictionary<string, string>();

        /// <summary>"avatar_03" → "Avatares/avatar_03", cached so no string is built per frame.</summary>
        private static string PathOf(string avatarId)
        {
            if (!Paths.TryGetValue(avatarId, out var path))
            {
                path = "Avatares/" + avatarId;
                Paths[avatarId] = path;
            }

            return path;
        }

        /// <summary>"Avatares/avatar_03" → "Avatares/avatar_03_r" (cached).</summary>
        private static string RoundOf(string path)
        {
            if (!RoundPaths.TryGetValue(path, out var round))
            {
                round = path + "_r";
                RoundPaths[path] = round;
            }

            return round;
        }

        private static int Index(string avatarId)
        {
            if (string.IsNullOrEmpty(avatarId))
            {
                return 0;
            }

            var digits = avatarId.Substring(avatarId.LastIndexOf('_') + 1);
            return int.TryParse(digits, out var n) ? Mathf.Max(0, n - 1) : avatarId.Length;
        }
    }
}
