using FishingIdle.Game.Visual;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// Draws the player's avatar (M18-T03): the chosen picture from Resources/Arte/Avatares, the painted
    /// fisherman's head when none was chosen, and a clean coloured badge while an avatar's art is pending
    /// (ASSET_PENDENTE, docs/ASSETS_PENDENTES.md).
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
            var tex = string.IsNullOrEmpty(avatarId) ? ArtAssets.Texture("Cena/retrato") : ArtAssets.Texture("Avatares/" + avatarId);
            if (tex != null)
            {
                GUI.DrawTexture(rect, tex, ScaleMode.ScaleAndCrop, true, 0, Color.white, 0, radius);
                return;
            }

            var color = Badges[Index(avatarId) % Badges.Length];
            GUI.DrawTexture(rect, skin.White, ScaleMode.StretchToFill, true, 0, new Color(color.r, color.g, color.b, 0.25f), 0, radius);
            var s = rect.width * 0.55f;
            skin.DrawIcon(new Rect(rect.center.x - s / 2f, rect.center.y - s / 2f, s, s), Icons.Profile, color);
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
