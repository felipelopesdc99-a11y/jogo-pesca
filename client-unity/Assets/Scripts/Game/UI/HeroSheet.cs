using FishingIdle.Game.Visual;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The "hero sheet" stage shared by the Encyclopedia sheet (A-143) and the Aquarium's card mode (A-145): a water
    /// backdrop, a soft aura in the rarity colour (faint for Comum and Raro, Art Bible 2.4), a pedestal under the fish
    /// and round ‹ › arrows. Only drawing; nothing here decides anything about the fish.
    /// </summary>
    public static class HeroSheet
    {
        // Painted art (received 15/10/2026, A-181); without a file the drawn fallback below stays.
        private const string AuraArt = "UI/ui_enc_aura";         // 512×512 white radial glow, tinted by rarity
        private const string PedestalArt = "UI/ui_enc_pedestal"; // 328×61 stone and coral disc, cut to its edges
        private const float PedestalMaxHeight = 44f;              // the room under the fish (34 px row + 10 px under its feet)

        /// <summary>The water backdrop of the stage, like the Cardume formation.</summary>
        public static void Backdrop(UiSkin skin, Rect rect)
        {
            GUI.DrawTexture(rect, skin.White, ScaleMode.StretchToFill, true, 0, new Color(0.05f, 0.16f, 0.24f, 0.92f), 0, 14f);
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height * 0.55f, rect.width, rect.height * 0.45f), skin.White, ScaleMode.StretchToFill, true, 0, new Color(0.03f, 0.10f, 0.17f, 0.55f), 0, 14f);
            GUI.DrawTexture(rect, skin.White, ScaleMode.StretchToFill, true, 0, UiSkin.Border, 1.5f, 14f);
        }

        /// <summary>How strong the aura is per rarity: barely there for Comum and Raro, a little more for the high tiers.</summary>
        public static float AuraStrength(string rarityId)
        {
            switch (rarityId)
            {
                case "common": return 0.035f;
                case "rare": return 0.05f;
                case "epic": return 0.09f;
                case "legendary": return 0.12f;
                case "mythic": return 0.14f;
                default: return 0.04f;
            }
        }

        /// <summary>Four soft stacked discs around <paramref name="centre"/>, in the rarity colour.</summary>
        public static void Aura(UiSkin skin, Vector2 centre, float diameter, Color color, float strength)
        {
            var art = ArtAssets.Texture(AuraArt);
            if (art != null)
            {
                // One soft glow in the rarity colour; the same overall strength as the four discs it replaces.
                GUI.DrawTexture(new Rect(centre.x - diameter / 2f, centre.y - diameter / 2f, diameter, diameter), art, ScaleMode.StretchToFill, true, 0,
                    new Color(color.r, color.g, color.b, Mathf.Clamp01(strength * 3f)), 0, 0);
                return;
            }

            for (var i = 0; i < 4; i++)
            {
                var d = diameter * (1f - i * 0.18f);
                if (d <= 1f)
                {
                    continue;
                }

                GUI.DrawTexture(new Rect(centre.x - d / 2f, centre.y - d / 2f, d, d), skin.White, ScaleMode.StretchToFill, true, 0,
                    new Color(color.r, color.g, color.b, strength), 0, d / 2f);
            }
        }

        /// <summary>The pedestal seen from the front: its shadow, side and top face, the face rimmed in the rarity colour.</summary>
        public static void Pedestal(UiSkin skin, float centreX, float top, float width, Color rarity, float rimAlpha)
        {
            var art = ArtAssets.Texture(PedestalArt);
            if (art != null)
            {
                // Kept in proportion (never squashed): as wide as asked unless the row is too low, then narrower.
                // Its top face starts a little above the row so the fish stands on the disc; the aura carries the rarity.
                var h = Mathf.Min(width * art.height / art.width, PedestalMaxHeight);
                var w = h * art.width / art.height;
                var r = new Rect(centreX - w / 2f, top - 10f, w, h);
                GUI.DrawTexture(new Rect(r.x - 6f, r.yMax - h * 0.3f, w + 12f, h * 0.36f), skin.White, ScaleMode.StretchToFill, true, 0, new Color(0f, 0f, 0f, 0.22f), 0, h * 0.18f);
                GUI.DrawTexture(r, art, ScaleMode.StretchToFill, true);
                return;
            }

            GUI.DrawTexture(new Rect(centreX - width / 2f - 10f, top + 18f, width + 20f, 20f), skin.White, ScaleMode.StretchToFill, true, 0, new Color(0f, 0f, 0f, 0.28f), 0, 10f);
            GUI.DrawTexture(new Rect(centreX - width / 2f, top + 10f, width, 22f), skin.White, ScaleMode.StretchToFill, true, 0, new Color(0.07f, 0.13f, 0.21f, 1f), 0, 11f);
            var face = new Rect(centreX - width / 2f, top, width, 22f);
            GUI.DrawTexture(face, skin.White, ScaleMode.StretchToFill, true, 0, new Color(0.16f, 0.27f, 0.38f, 1f), 0, 11f);
            GUI.DrawTexture(face, skin.White, ScaleMode.StretchToFill, true, 0, new Color(rarity.r, rarity.g, rarity.b, rimAlpha), 1.5f, 11f);
        }

        /// <summary>A round arrow button; the chevron icon points right and is mirrored for "previous". Dimmed when it cannot be used.</summary>
        public static bool Arrow(UiSkin skin, Rect r, bool left, bool usable = true)
        {
            var wasEnabled = GUI.enabled;
            GUI.enabled = wasEnabled && usable;
            var live = GUI.enabled;
            var hovered = live && r.Contains(Event.current.mousePosition);
            var clicked = GUI.Button(r, GUIContent.none, GUIStyle.none);
            var alpha = live ? 1f : 0.35f;
            GUI.DrawTexture(r, skin.White, ScaleMode.StretchToFill, true, 0, new Color(0.04f, 0.09f, 0.15f, (hovered ? 0.95f : 0.75f) * alpha), 0, r.width / 2f);
            var ring = hovered ? UiSkin.Accent : UiSkin.Border;
            GUI.DrawTexture(r, skin.White, ScaleMode.StretchToFill, true, 0, new Color(ring.r, ring.g, ring.b, alpha), 1.5f, r.width / 2f);
            var tint = hovered ? UiSkin.Accent : new Color(1f, 1f, 1f, alpha);
            var icon = ArtAssets.Icon(Icons.Chevron);
            if (icon != null)
            {
                var prevColor = GUI.color;
                GUI.color = tint;
                var pad = r.width * 0.3f;
                GUI.DrawTextureWithTexCoords(new Rect(r.x + pad, r.y + pad, r.width - pad * 2f, r.height - pad * 2f), icon,
                    left ? new Rect(1f, 0f, -1f, 1f) : new Rect(0f, 0f, 1f, 1f), true);
                GUI.color = prevColor;
            }
            else
            {
                var prevContent = GUI.contentColor;
                GUI.contentColor = tint;
                GUI.Label(r, left ? GameTexts.Profile.EncPrevGlyph : GameTexts.Profile.EncNextGlyph, skin.TitleCenter);
                GUI.contentColor = prevContent;
            }

            GUI.enabled = wasEnabled;
            return clicked && live;
        }
    }
}
