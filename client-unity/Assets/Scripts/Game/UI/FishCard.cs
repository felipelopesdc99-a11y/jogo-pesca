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
            // Excepcional and Perfeição: their own seal and gleam, in the size colour.
            var exceptional = VisualTheme.IsSpecialSize(m.SizeCategoryId);
            var sizeColor = UiSkin.SizeColor(m.SizeCategoryId);

            var clicked = GUI.Button(rect, GUIContent.none, m.Selected ? skin.CardSelected : hovered ? skin.CardHovered : skin.Card);
            if (!m.Selected)
            {
                var common = m.RarityId == null || m.RarityId == "common";
                skin.DrawOutline(rect, new Color(accent.r, accent.g, accent.b, common ? (hovered ? 0.55f : 0.30f) : hovered ? 1f : 0.85f));
            }

            var pad = 12f;
            var x = rect.x + pad;
            var w = rect.width - pad * 2;

            // Top row: the rarity seal on the left; a status badge or the selection check on the right.
            // When both do not fit, the seal drops its star, and then the badge moves onto the art.
            var y = rect.y + 10f;
            var badgeWidth = !m.Selected && !string.IsNullOrEmpty(m.Badge) ? skin.Badge.CalcSize(new GUIContent(m.Badge)).x + 4f : 0f;
            var rightWidth = m.Selected ? 22f : badgeWidth;
            var badgeOnArt = false;
            if (!string.IsNullOrEmpty(m.RarityName))
            {
                var label = m.RarityName.ToUpperInvariant();
                var withStar = skin.PillWidth(label, true);
                var plain = skin.PillWidth(label, false);
                var star = withStar + 6f + rightWidth <= w;
                badgeOnArt = !star && plain + 6f + rightWidth > w;
                skin.RarityPill(new Rect(x, y, star ? withStar : plain, 22), m.RarityId, label, star);
            }

            if (m.Selected)
            {
                skin.DrawIcon(new Rect(rect.xMax - 32, y + 1, 20, 20), Icons.Check, UiSkin.Accent);
            }
            else if (badgeWidth > 0f && !badgeOnArt)
            {
                skin.Tag(new Rect(rect.xMax - pad - badgeWidth, y + 2, badgeWidth, 18), m.Badge, m.BadgeColor);
            }

            // Laid out from the bottom up, so the texts never run into each other whatever the card height:
            // coins / footer row, then the bar, the size line and the name; the fish gets the space left.
            var hasBottomRow = !string.IsNullOrEmpty(m.Coins) || !string.IsNullOrEmpty(m.Footer);
            var cursor = rect.yMax - 8f;
            var bottomRowY = cursor - 20f;
            if (hasBottomRow)
            {
                cursor = bottomRowY - 4f;
            }

            var barY = cursor - 6f;
            if (m.Bar >= 0f)
            {
                cursor = barY - 6f;
            }

            var hasLine = !string.IsNullOrEmpty(m.Line) || (exceptional && !string.IsNullOrEmpty(m.SizeCategoryName));
            var lineY = cursor - 19f;
            if (hasLine)
            {
                cursor = lineY - 1f;
            }

            var nameY = cursor - 21f;
            cursor = nameY - 4f;

            // Fish art first, numbers second (GDD section 44).
            var artTop = y + 26f;
            var art = new Rect(x + 4, artTop, w - 8, Mathf.Max(20f, cursor - artTop));
            var bob = hovered ? Mathf.Sin(Time.unscaledTime * 3f) * 1.5f : 0f;
            if (exceptional && !m.Silhouette)
            {
                skin.DrawGlow(new Rect(art.x + art.width * 0.2f, art.y + art.height * 0.25f, art.width * 0.6f, art.height * 0.5f), sizeColor, 0.35f + 0.1f * Mathf.Sin(Time.unscaledTime * 2.4f));
            }

            var tex = m.Art != null ? m.Art : Scene.Art.FishTexture(m.SpeciesId);
            var tint = m.Silhouette ? new Color(0.02f, 0.05f, 0.09f, 0.85f) : Color.white;
            GUI.DrawTexture(new Rect(art.x, art.y + bob, art.width, art.height), tex, ScaleMode.ScaleToFit, true, 0, tint, 0, 0);

            if (badgeOnArt)
            {
                skin.Tag(new Rect(rect.xMax - pad - badgeWidth, artTop, badgeWidth, 18), m.Badge, m.BadgeColor);
            }

            GUI.Label(new Rect(x, nameY, w, 22), Fit(m.Name, skin.BodyBold, w), skin.BodyBold);

            if (exceptional && !string.IsNullOrEmpty(m.SizeCategoryName))
            {
                // The gold seal sits at the right end of the size line, never over the fish.
                var seal = m.SizeCategoryName.ToUpperInvariant();
                var sw = skin.Badge.CalcSize(new GUIContent(seal)).x + 10f;
                ExceptionalSeal(skin, new Rect(x + w - sw, lineY + 1, sw, 18), seal, sizeColor);
                var rest = WithoutSize(m.Line, m.SizeCategoryName);
                if (!string.IsNullOrEmpty(rest))
                {
                    GUI.Label(new Rect(x, lineY, w - sw - 6f, 20), Fit(rest, skin.SmallMuted, w - sw - 6f), skin.SmallMuted);
                }
            }
            else if (!string.IsNullOrEmpty(m.Line))
            {
                DrawLine(skin, new Rect(x, lineY, w, 20), m);
            }

            if (m.Bar >= 0f)
            {
                skin.Bar(new Rect(x, barY, w, 6), m.Bar, accent);
            }

            // Bottom row: the coins keep their full width on the right; the footer gets what is left.
            var coinsWidth = 0f;
            if (!string.IsNullOrEmpty(m.Coins))
            {
                coinsWidth = skin.CoinAmountWidth(m.Coins, 20f);
                skin.CoinAmount(new Rect(rect.xMax - pad - coinsWidth, bottomRowY - 1, coinsWidth, 20), m.Coins);
            }

            if (!string.IsNullOrEmpty(m.Footer))
            {
                var fw = w - coinsWidth - (coinsWidth > 0f ? 8f : 0f);
                GUI.contentColor = Color.Lerp(accent, Color.white, 0.35f);
                GUI.Label(new Rect(x, bottomRowY, fw, 20), Fit(m.Footer, skin.SmallBold, fw), skin.SmallBold);
                GUI.contentColor = Color.white;
            }

            return clicked;
        }

        /// <summary>The line under the name, with the size category in its own colour (addendum A-079).</summary>
        private static void DrawLine(UiSkin skin, Rect rect, FishCardModel m)
        {
            // A line too long for the card is shortened with "…" (and then drawn in one colour).
            if (m.Silhouette || skin.SmallMuted.CalcSize(new GUIContent(m.Line)).x > rect.width)
            {
                GUI.Label(rect, Fit(m.Line, skin.SmallMuted, rect.width), skin.SmallMuted);
                return;
            }

            skin.SizeLine(rect, m.Line, m.SizeCategoryName, m.SizeCategoryId, skin.SmallMuted);
        }

        /// <summary>The text as it fits in <paramref name="width"/>: shortened with "…" when it would overflow.</summary>
        public static string Fit(string text, GUIStyle style, float width)
        {
            if (string.IsNullOrEmpty(text) || style.CalcSize(new GUIContent(text)).x <= width)
            {
                return text;
            }

            for (var n = text.Length - 1; n > 0; n--)
            {
                var shorter = text.Substring(0, n).TrimEnd() + "…";
                if (style.CalcSize(new GUIContent(shorter)).x <= width)
                {
                    return shorter;
                }
            }

            return "…";
        }

        /// <summary>The line without the size name, which the Excepcional seal already shows ("98,4 cm · Excepcional · Nv. 3" → "98,4 cm · Nv. 3").</summary>
        private static string WithoutSize(string line, string size)
        {
            if (string.IsNullOrEmpty(line))
            {
                return line;
            }

            var sep = " · ";
            var parts = line.Split(new[] { sep }, System.StringSplitOptions.None);
            return string.Join(sep, System.Array.FindAll(parts, p => p != size));
        }

        /// <summary>The "EXCEPCIONAL" (gold) or "PERFEIÇÃO" (diamond) size seal, with a light sweeping across it.</summary>
        public static void ExceptionalSeal(UiSkin skin, Rect rect, string text, Color? color = null)
        {
            skin.Tag(rect, text, color ?? VisualTheme.Current.Exceptional);

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
