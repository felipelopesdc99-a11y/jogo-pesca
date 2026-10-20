using FishingIdle.Game.Visual;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The "hero sheet" stage shared by the Encyclopedia sheet (A-143) and the Aquarium's card mode (A-145): a water
    /// backdrop, an aura in the rarity colour (faint for Comum and Raro), a pedestal under the fish and round ‹ › arrows,
    /// all in pixel art since M25-T13 (A-188). Only drawing; nothing here decides anything about the fish.
    /// </summary>
    public static class HeroSheet
    {
        // Pixel art (ArtePixel/UI/ui_enc_pedestal, L3-07, 50 × 29); without the file the drawn pedestal below stays.
        private const string PedestalArt = "UI/ui_enc_pedestal";
        private const float PedestalMaxHeight = 58f;              // 2× the 29 px art: the 34 px row, 14 px under the fish's feet, the 10 px margin

        /// <summary>
        /// The water backdrop of the stage, in pixel art (M25-T13): two flat bands of deep water, a dotted line of light
        /// where they meet, and a dark-gold line around (the ARC frame of WindowFrame.Photo sits outside it).
        /// </summary>
        public static void Backdrop(UiSkin skin, Rect rect)
        {
            ArcArt.Fill(skin, rect, new Color(0.05f, 0.16f, 0.24f, 0.92f), 0f);
            var split = Mathf.Round(rect.y + rect.height * 0.55f);
            ArcArt.Fill(skin, new Rect(rect.x, split, rect.width, rect.yMax - split), new Color(0.03f, 0.10f, 0.17f, 0.55f), 0f);
            if (Event.current.type == EventType.Repaint)
            {
                // The seam as a row of pixels two apart (Art Bible 4.1: one dithered line between bands).
                const float p = UiSkin.ArcPixel;
                for (var x = Mathf.Round(rect.x) + p; x < rect.xMax - p; x += p * 2f)
                {
                    GUI.DrawTexture(new Rect(x, split - p, p, p), skin.White, ScaleMode.StretchToFill, true, 0, new Color(0.05f, 0.16f, 0.24f, 0.6f), 0, 0);
                }
            }

            ArcArt.Line(skin, rect, UiSkin.FrameDark, 4f);
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

        /// <summary>The aura around <paramref name="centre"/> in the rarity colour, in pixel art (a dithered disc, ArcArt.Aura).</summary>
        public static void Aura(UiSkin skin, Vector2 centre, float diameter, Color color, float strength)
        {
            ArcArt.Aura(centre, diameter, color, strength);
        }

        /// <summary>
        /// The pedestal seen from the front: the pixel art at the largest whole scale under the fish, with a flat pixel
        /// shadow; drawn by the code without the file (side and top face, the face rimmed in the rarity colour).
        /// </summary>
        public static void Pedestal(UiSkin skin, float centreX, float top, float width, Color rarity, float rimAlpha)
        {
            var art = ArtAssets.Texture(PedestalArt);
            if (art != null)
            {
                // As wide as asked unless the row is too low; whole scale (PixelDraw). Its top face starts a little above
                // the row so the fish stands on the disc; the aura carries the rarity.
                var room = new Rect(centreX - width / 2f, top - 14f, width, PedestalMaxHeight);
                Texture tex = art;
                var r = PixelDraw.FitRect(room, ref tex);
                r = new Rect(r.x, room.y, r.width, r.height);
                ArcArt.Fill(skin, new Rect(r.x + UiSkin.ArcPixel * 2, r.yMax - UiSkin.ArcPixel * 3, r.width - UiSkin.ArcPixel * 4, UiSkin.ArcPixel * 4), new Color(0f, 0f, 0f, 0.25f), 0f);
                GUI.DrawTexture(r, tex, ScaleMode.StretchToFill, true);
                return;
            }

            ArcArt.Fill(skin, new Rect(centreX - width / 2f - 10f, top + 18f, width + 20f, 20f), new Color(0f, 0f, 0f, 0.28f), 10f);
            ArcArt.Fill(skin, new Rect(centreX - width / 2f, top + 10f, width, 22f), new Color(0.07f, 0.13f, 0.21f, 1f), 11f);
            var face = new Rect(centreX - width / 2f, top, width, 22f);
            ArcArt.Fill(skin, face, new Color(0.16f, 0.27f, 0.38f, 1f), 11f);
            ArcArt.Line(skin, face, new Color(rarity.r, rarity.g, rarity.b, rimAlpha), 11f);
        }

        /// <summary>
        /// A round arrow button in the ARC skin: a pixel disc with a one-pixel line (gold on hover) and the chevron icon
        /// at a whole scale, mirrored for "previous". Dimmed when it cannot be used.
        /// </summary>
        public static bool Arrow(UiSkin skin, Rect r, bool left, bool usable = true)
        {
            var wasEnabled = GUI.enabled;
            GUI.enabled = wasEnabled && usable;
            var live = GUI.enabled;
            var hovered = live && r.Contains(Event.current.mousePosition);
            var clicked = GUI.Button(r, GUIContent.none, GUIStyle.none);
            var alpha = live ? 1f : 0.35f;
            var field = VisualTheme.Current.Field;
            var ring = hovered ? UiSkin.TitleColor : UiSkin.FrameGold;
            ArcArt.Disc(skin, r, new Color(field.r, field.g, field.b, (hovered ? 0.98f : 0.85f) * alpha), new Color(ring.r, ring.g, ring.b, alpha));
            var tint = hovered ? UiSkin.TitleColor : new Color(UiSkin.FrameGold.r, UiSkin.FrameGold.g, UiSkin.FrameGold.b, alpha);
            var icon = ArtAssets.Icon(Icons.Chevron);
            if (icon != null)
            {
                var pad = r.width * 0.25f;
                ArcArt.Fit(new Rect(r.x + pad, r.y + pad, r.width - pad * 2f, r.height - pad * 2f), icon, tint, left);
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
