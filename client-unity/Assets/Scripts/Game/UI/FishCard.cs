using FishingIdle.Game.Scene;
using FishingIdle.Game.Visual;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>What one fish card shows. Every value comes from a game-service view; the card only draws it.</summary>
    public sealed class FishCardModel
    {
        public string SpeciesId;
        public string Name;
        /// <summary>The line under the name, e.g. "48,6 cm · Grande".</summary>
        public string Line;
        public string RarityId;
        public string RarityName;
        public string SizeCategoryId;
        public string SizeCategoryName;
        /// <summary>0–1 fill of the bar in the rarity colour (level XP or size), or negative for none.</summary>
        public float Bar = -1f;
        /// <summary>Bottom-left text, e.g. "Nv. 3 · Cardume 2".</summary>
        public string Footer;
        /// <summary>Bottom-right amount of coins, already formatted, or null.</summary>
        public string Coins;
        /// <summary>A small tag in the top-right corner, e.g. "NOVA ESPÉCIE".</summary>
        public string Badge;
        public Color BadgeColor = Color.white;
        public bool Selected;
        /// <summary>Draws the fish as a dark silhouette (undiscovered in the Encyclopedia).</summary>
        public bool Silhouette;
        /// <summary>Optional art instead of the species (e.g. a rod in the Market).</summary>
        public Texture2D Art;
    }

    /// <summary>
    /// The official fish card (Art Bible, section 6): the same layout for every rarity, the rarity
    /// colour only as an accent (border, seal, bar), the size category in its own colour, and a
    /// gold gleam reserved for the Excepcional size. Used by the Fishing Box, Aquarium, Market,
    /// Profile and Encyclopedia.
    /// </summary>
    public static class FishCard
    {
        /// <summary>Draws the card; returns true when it was clicked.</summary>
        public static bool Draw(UiSkin skin, Rect rect, FishCardModel m)
        {
            var hovered = GUI.enabled && rect.Contains(Event.current.mousePosition);
            var accent = UiSkin.RarityColor(m.RarityId);
            var exceptional = m.SizeCategoryId == "exceptional";

            var clicked = GUI.Button(rect, GUIContent.none, m.Selected ? skin.CardSelected : hovered ? skin.CardHovered : skin.Card);
            if (!m.Selected)
            {
                var common = m.RarityId == null || m.RarityId == "common";
                skin.DrawOutline(rect, new Color(accent.r, accent.g, accent.b, common ? (hovered ? 0.55f : 0.30f) : hovered ? 1f : 0.85f));
            }

            var pad = 12f;
            var x = rect.x + pad;
            var w = rect.width - pad * 2;
            var compact = rect.height < 170f;

            // Seal of the rarity, top-left; a status badge or the selection check, top-right.
            var y = rect.y + 10f;
            if (!string.IsNullOrEmpty(m.RarityName))
            {
                var label = m.RarityName.ToUpperInvariant();
                skin.AccentPill(new Rect(x, y, skin.PillWidth(label, true), 20), label, accent, Icons.Star);
            }

            if (m.Selected)
            {
                skin.DrawIcon(new Rect(rect.xMax - 32, y, 20, 20), Icons.Check, UiSkin.Accent);
            }
            else if (!string.IsNullOrEmpty(m.Badge))
            {
                var bw = skin.Badge.CalcSize(new GUIContent(m.Badge)).x + 4f;
                skin.Tag(new Rect(rect.xMax - pad - bw, y + 1, bw, 18), m.Badge, m.BadgeColor);
            }

            // Fish art first, numbers second (GDD section 44).
            var artHeight = compact ? rect.height * 0.34f : rect.height * 0.38f;
            var art = new Rect(x + 4, y + 24, w - 8, artHeight);
            var bob = hovered ? Mathf.Sin(Time.unscaledTime * 3f) * 1.5f : 0f;
            if (exceptional && !m.Silhouette)
            {
                skin.DrawGlow(new Rect(art.x + art.width * 0.2f, art.y + art.height * 0.25f, art.width * 0.6f, art.height * 0.5f), VisualTheme.Current.Exceptional, 0.35f + 0.1f * Mathf.Sin(Time.unscaledTime * 2.4f));
            }

            var tex = m.Art != null ? m.Art : Scene.Art.FishTexture(m.SpeciesId);
            var tint = m.Silhouette ? new Color(0.02f, 0.05f, 0.09f, 0.85f) : Color.white;
            GUI.DrawTexture(new Rect(art.x, art.y + bob, art.width, art.height), tex, ScaleMode.ScaleToFit, true, 0, tint, 0, 0);

            if (exceptional && !string.IsNullOrEmpty(m.SizeCategoryName))
            {
                ExceptionalSeal(skin, new Rect(art.xMax - 104, art.yMax - 16, 104, 20), m.SizeCategoryName.ToUpperInvariant());
            }

            y = art.yMax + 6f;
            GUI.Label(new Rect(x, y, w, 22), m.Name, skin.BodyBold);
            y += 21f;
            if (!string.IsNullOrEmpty(m.Line))
            {
                DrawLine(skin, new Rect(x, y, w, 20), m);
                y += 20f;
            }

            var bottom = rect.yMax - 26f;
            if (m.Bar >= 0f)
            {
                skin.Bar(new Rect(x, bottom - 12f, w, 6), m.Bar, accent);
            }

            if (!string.IsNullOrEmpty(m.Footer))
            {
                GUI.contentColor = Color.Lerp(accent, Color.white, 0.35f);
                GUI.Label(new Rect(x, bottom, w * 0.62f, 20), m.Footer, skin.SmallBold);
                GUI.contentColor = Color.white;
            }

            if (!string.IsNullOrEmpty(m.Coins))
            {
                var cw = skin.SmallGold.CalcSize(new GUIContent(m.Coins)).x + 24f;
                skin.CoinAmount(new Rect(rect.xMax - pad - cw, bottom - 1, cw, 20), m.Coins);
            }

            return clicked;
        }

        /// <summary>
        /// The line under the name. When it ends with the size category ("48,6 cm · Grande"), the
        /// category is written in its size colour (addendum A-079); the rest stays muted.
        /// </summary>
        private static void DrawLine(UiSkin skin, Rect rect, FishCardModel m)
        {
            var size = m.SizeCategoryName;
            if (string.IsNullOrEmpty(size) || string.IsNullOrEmpty(m.SizeCategoryId) || m.Silhouette
                || m.Line.Length <= size.Length || !m.Line.EndsWith(size, System.StringComparison.Ordinal))
            {
                GUI.Label(rect, m.Line, skin.SmallMuted);
                return;
            }

            var head = m.Line.Substring(0, m.Line.Length - size.Length);
            var hw = skin.SmallMuted.CalcSize(new GUIContent(head)).x - skin.SmallMuted.padding.right;
            GUI.Label(rect, head, skin.SmallMuted);

            var previous = GUI.contentColor;
            GUI.contentColor = Color.Lerp(UiSkin.SizeColor(m.SizeCategoryId), Color.white, 0.15f);
            GUI.Label(new Rect(rect.x + hw - skin.SmallBold.padding.left, rect.y, rect.width - hw, rect.height), size, skin.SmallBold);
            GUI.contentColor = previous;
        }

        /// <summary>The gold "EXCEPCIONAL" size seal, with a light sweeping across it.</summary>
        public static void ExceptionalSeal(UiSkin skin, Rect rect, string text)
        {
            var gold = VisualTheme.Current.Exceptional;
            skin.Tag(rect, text, gold);

            // Gleam: a soft white band that crosses the seal every few seconds.
            var period = 2.8f;
            var phase = Mathf.Repeat(Time.unscaledTime, period) / period;
            if (phase < 0.45f)
            {
                var k = phase / 0.45f;
                var band = new Rect(rect.x + (rect.width + 30f) * k - 30f, rect.y + 1, 22f, rect.height - 2);
                band.xMin = Mathf.Max(band.xMin, rect.x + 3);
                band.xMax = Mathf.Min(band.xMax, rect.xMax - 3);
                if (band.width > 1f)
                {
                    GUI.DrawTexture(band, skin.White, ScaleMode.StretchToFill, true, 0, new Color(1f, 1f, 1f, 0.45f * Mathf.Sin(k * Mathf.PI)), 0, 4);
                }
            }
        }

        /// <summary>Sets the top-right badge for a new species or a personal record.</summary>
        public static void StatusBadge(FishCardModel m, bool newSpecies, bool record)
        {
            if (newSpecies)
            {
                m.Badge = GameTexts.Box.NewSpeciesBadge;
                m.BadgeColor = UiSkin.Accent;
            }
            else if (record)
            {
                m.Badge = GameTexts.Box.RecordBadge;
                m.BadgeColor = UiSkin.Gold;
            }
        }
    }
}
