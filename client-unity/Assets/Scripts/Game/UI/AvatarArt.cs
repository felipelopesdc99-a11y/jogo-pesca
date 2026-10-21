using FishingIdle.Game.Visual;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// Draws the player's avatar (M18-T03): the chosen picture from Resources/Arte/Avatares, the painted
    /// fisherman's head when none was chosen, and a clean coloured badge if an avatar's file is missing. The six
    /// avatars (received 15/10/2026, A-181) are busts on a transparent background: they sit on a calm sea-blue disc
    /// so every frame (round or rounded) shows the same backdrop.
    /// </summary>
    public static class AvatarArt
    {
        private static readonly Color[] Badges =
        {
            new Color(0.15f, 0.77f, 0.76f), new Color(0.30f, 0.55f, 1f), new Color(0.96f, 0.73f, 0.23f),
            new Color(0.66f, 0.33f, 0.97f), new Color(0.17f, 0.80f, 0.50f), new Color(0.93f, 0.28f, 0.60f),
        };

        public static void Draw(UiSkin skin, Rect rect, string avatarId, float radius = 10f)
        {
            var tex = string.IsNullOrEmpty(avatarId) ? ArtAssets.Texture("Cena/retrato") : ArtAssets.Texture(PathOf(avatarId));
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
            GUI.DrawTexture(rect, skin.White, ScaleMode.StretchToFill, true, 0, new Color(color.r, color.g, color.b, 0.25f), 0, radius);
            var s = rect.width * 0.55f;
            skin.DrawIcon(new Rect(rect.center.x - s / 2f, rect.center.y - s / 2f, s, s), Icons.Profile, color);
        }

        private static readonly Color Backdrop = new Color(0.24f, 0.55f, 0.67f, 1f);
        private static readonly System.Collections.Generic.Dictionary<string, string> Paths = new System.Collections.Generic.Dictionary<string, string>();

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
