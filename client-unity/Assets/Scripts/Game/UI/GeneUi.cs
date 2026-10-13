using FishingIdle.Game.Bootstrap;
using FishingIdle.Game.Visual;
using FishingIdle.GameService.Fishing;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// How the genes are drawn (M24-T52, A-176): the quality stars and one row per attribute with its gene ("Ataque 27/31"),
    /// a perfect gene (31) in gold. Every number comes from the service's <see cref="GeneView"/>; this only draws it.
    /// </summary>
    public static class GeneUi
    {
        private const float StarGap = 1f;

        /// <summary>The width of a row of <paramref name="count"/> stars of <paramref name="size"/> px.</summary>
        public static float StarsWidth(int count, float size) => count <= 0 ? 0f : count * size + (count - 1) * StarGap;

        /// <summary>
        /// The quality stars from the left of <paramref name="rect"/>: earned ones in gold, the rest faint. A perfect
        /// specimen gets a soft gold glow behind them. Returns the width used.
        /// </summary>
        public static float Stars(UiSkin skin, Rect rect, GeneView genes, float size = 14f)
        {
            if (genes == null || genes.Stars <= 0)
            {
                return 0f;
            }

            var total = Mathf.Max(genes.MaxStars, genes.Stars);
            var width = StarsWidth(total, size);
            var y = rect.y + (rect.height - size) / 2f;
            if (genes.IsPerfectSpecimen)
            {
                skin.DrawGlow(new Rect(rect.x - 4f, y - 4f, width + 8f, size + 8f), UiSkin.Gold, 0.45f);
            }

            var faint = UiSkin.Muted;
            faint.a = 0.45f;
            for (var i = 0; i < total; i++)
            {
                skin.DrawIcon(new Rect(rect.x + i * (size + StarGap), y, size, size), Icons.Star, i < genes.Stars ? UiSkin.Gold : faint);
            }

            return width;
        }

        /// <summary>
        /// One attribute row of a fish sheet: the name, a small bar with the gene, "27/31" (gold when perfect) and the
        /// attribute's value on the right. Draws on one line of <paramref name="rowHeight"/>.
        /// </summary>
        public static void AttributeRow(UiSkin skin, float x, float y, float w, float rowHeight, string label, int gene, int max, string value)
        {
            var ty = y + (rowHeight - 20f) / 2f;
            var perfect = gene >= max;
            var labelWidth = Mathf.Min(96f, w * 0.30f);
            const float geneWidth = 46f;
            var valueWidth = Mathf.Min(w * 0.25f, skin.SmallRight.CalcSize(new GUIContent(value ?? string.Empty)).x + 6f);
            var barX = x + labelWidth + 6f;
            var barWidth = Mathf.Max(12f, w - labelWidth - geneWidth - valueWidth - 18f);

            GUI.Label(new Rect(x, ty, labelWidth, 20f), FishCard.Fit(label, skin.SmallMuted, labelWidth), skin.SmallMuted);
            skin.Bar(new Rect(barX, ty + 7f, barWidth, 6f), max > 0 ? Mathf.Clamp01(gene / (float)max) : 0f, perfect ? UiSkin.Gold : UiSkin.Accent);
            var geneText = GameTexts.Genes.Value(gene, max);
            GUI.Label(new Rect(barX + barWidth + 4f, ty, geneWidth, 20f), geneText, perfect ? skin.SmallGoldRight : skin.SmallMutedRight);
            if (!string.IsNullOrEmpty(value))
            {
                GUI.Label(new Rect(x + w - valueWidth, ty, valueWidth, 20f), value, skin.SmallRight);
            }
        }

        /// <summary>The four attribute rows in the order Vida, Ataque, Defesa, Velocidade. Returns the y below them.</summary>
        public static float AttributeRows(UiSkin skin, float x, float y, float w, float rowHeight, GeneView genes, string hp, string attack, string defense, string speed)
        {
            var max = genes?.Max ?? 31;
            AttributeRow(skin, x, y, w, rowHeight, GameTexts.Aquarium.Hp, genes?.Hp ?? 0, max, hp);
            y += rowHeight;
            AttributeRow(skin, x, y, w, rowHeight, GameTexts.Aquarium.Attack, genes?.Attack ?? 0, max, attack);
            y += rowHeight;
            AttributeRow(skin, x, y, w, rowHeight, GameTexts.Aquarium.Defense, genes?.Defense ?? 0, max, defense);
            y += rowHeight;
            AttributeRow(skin, x, y, w, rowHeight, GameTexts.Aquarium.Speed, genes?.Speed ?? 0, max, speed);
            return y + rowHeight;
        }

        /// <summary>
        /// The heading of the attributes with the stars and the quality on its right ("Atributos ★★★★☆ Qualidade 72%").
        /// A perfect specimen shows its gold seal instead of the percentage.
        /// </summary>
        public static void Heading(UiSkin skin, Rect rect, string title, GeneView genes)
        {
            if (genes == null)
            {
                GUI.Label(rect, FishCard.Fit(title, skin.BodyBold, rect.width), skin.BodyBold);
                return;
            }

            var quality = genes.IsPerfectSpecimen ? GameTexts.Genes.PerfectSpecimen : GameTexts.Genes.QualityLine(Format.Percent(genes.Quality, 0));
            var qualityStyle = genes.IsPerfectSpecimen ? skin.SmallGoldRight : skin.SmallMutedRight;
            var qualityWidth = Mathf.Min(rect.width * 0.4f, qualityStyle.CalcSize(new GUIContent(quality)).x + 4f);
            var starsWidth = StarsWidth(genes.MaxStars, 14f);
            var titleWidth = Mathf.Max(0f, rect.width - qualityWidth - starsWidth - 16f);
            GUI.Label(new Rect(rect.x, rect.y, titleWidth, rect.height), FishCard.Fit(title, skin.BodyBold, titleWidth), skin.BodyBold);
            Stars(skin, new Rect(rect.xMax - qualityWidth - 8f - starsWidth, rect.y + 1f, starsWidth, 20f), genes);
            GUI.Label(new Rect(rect.xMax - qualityWidth, rect.y + 2f, qualityWidth, 20f), FishCard.Fit(quality, qualityStyle, qualityWidth), qualityStyle);
        }

        /// <summary>The genes paragraph of the "Como funciona" notes, with the numbers of the config.</summary>
        public static string Info(GameRoot root)
        {
            var genes = root?.Game?.Session.Config?.Progression?.Genes;
            if (genes == null)
            {
                return null;
            }

            var down = "−" + Format.PercentShort(1.0 - genes.StatMultiplierMin);
            var up = "+" + Format.PercentShort(genes.StatMultiplierMax - 1.0);
            return GameTexts.Genes.Info(genes.MaxValue, down, up, genes.QualityStars?.Count ?? 5);
        }

        /// <summary>A window's note with the genes paragraph after it.</summary>
        public static string WithInfo(string note, GameRoot root)
        {
            var genes = Info(root);
            return string.IsNullOrEmpty(genes) ? note : string.IsNullOrEmpty(note) ? genes : note + "\n\n" + genes;
        }

        /// <summary>The attributes with a perfect gene, "Ataque, Velocidade", or null.</summary>
        public static string PerfectAttributes(GeneView genes)
        {
            if (genes == null || genes.PerfectCount == 0)
            {
                return null;
            }

            var names = new System.Collections.Generic.List<string>(4);
            if (genes.HpPerfect) names.Add(GameTexts.Aquarium.Hp);
            if (genes.AttackPerfect) names.Add(GameTexts.Aquarium.Attack);
            if (genes.DefensePerfect) names.Add(GameTexts.Aquarium.Defense);
            if (genes.SpeedPerfect) names.Add(GameTexts.Aquarium.Speed);
            return string.Join(", ", names);
        }
    }
}
