using System.Collections.Generic;
using UnityEngine;

namespace FishingIdle.Game.Scene
{
    public enum FishPattern
    {
        Plain,
        Bars,
        Spots,
        Stripe,
        Scales,
    }

    /// <summary>How the placeholder art paints one species.</summary>
    public sealed class FishLook
    {
        public Color Back;
        public Color Belly;
        public Color Fin;
        public Color Mark;
        public FishPattern Pattern;
        public float Chubbiness = 1f;
        public float FinSize = 1f;
    }

    /// <summary>
    /// Placeholder colours per species, loosely inspired by the real fish.
    /// </summary>
    /// <remarks>
    /// Presentation only — not balance, so it lives with the client, not in /config. When real
    /// fish art arrives, this table and Art.FishTexture are what it replaces. An unknown species
    /// (added to /config later) gets a neutral look instead of breaking.
    /// </remarks>
    public static class FishLooks
    {
        private static readonly Dictionary<string, FishLook> Looks = new Dictionary<string, FishLook>
        {
            // Mapa 1 — Lago Sereno
            ["lambari"] = Look("8FA3AE", "E8EEF0", "D9A441", "3B4750", FishPattern.Stripe, 0.8f),
            ["tilapia"] = Look("56676B", "C9D2CF", "8E6F7A", "3A4548", FishPattern.Bars, 1.15f),
            ["carpa"] = Look("9C7A3C", "E6D29A", "B0763A", "6E5226", FishPattern.Scales, 1.1f),
            ["piau"] = Look("A2957A", "EEE6D2", "C98B5A", "3E352A", FishPattern.Spots, 0.9f),
            ["traira"] = Look("4D4A33", "B7B08A", "5A5236", "2A281B", FishPattern.Spots, 0.85f),
            ["pacu"] = Look("5B5F63", "D9C3A2", "C06A3E", "3A3C40", FishPattern.Plain, 1.35f),
            ["cascudo"] = Look("4B4032", "8E7D60", "3E3528", "2A231A", FishPattern.Spots, 0.95f),
            ["curimbata"] = Look("7E8C8E", "E1E5DD", "A7A28A", "4E585A", FishPattern.Scales, 1.0f),
            ["matrinxa"] = Look("6D8190", "E3E8EA", "C7534A", "3F4C56", FishPattern.Plain, 0.95f),
            ["tambaqui"] = Look("3F4A4E", "D8B97A", "2E3538", "2A3134", FishPattern.Plain, 1.4f),

            // Mapa 2 — Rio Selvagem
            ["tucunare"] = Look("7E8C3A", "E8D782", "C2572E", "2D3218", FishPattern.Bars, 1.05f),
            ["piranha"] = Look("6F7B80", "D9612F", "B8452A", "4B5357", FishPattern.Spots, 1.3f),
            ["dourado"] = Look("C8962A", "F5D768", "E0A33A", "8A6218", FishPattern.Stripe, 1.0f),
            ["pintado"] = Look("6B6E70", "E9E7DF", "7A7C7E", "1F2224", FishPattern.Spots, 0.8f),
            ["cachara"] = Look("6A6752", "E4E0CE", "6E6A55", "2B2A20", FishPattern.Bars, 0.8f),
            ["jau"] = Look("4F4838", "C8BC95", "4A4334", "2E291F", FishPattern.Spots, 1.25f),
            ["peixe_cachorra"] = Look("8D9CA3", "EEF2F2", "A0AAB0", "4E5A60", FishPattern.Stripe, 0.7f),
            ["piracanjuba"] = Look("7A8D90", "E6ECE9", "CB7C4A", "46565A", FishPattern.Scales, 0.95f),
            ["pirarucu"] = Look("3F4B44", "B7483A", "8E3A30", "2A332E", FishPattern.Scales, 0.95f, 0.8f),
            ["aruana"] = Look("B8B39A", "F4F0DE", "8FA0A6", "6E6A56", FishPattern.Scales, 0.7f, 1.6f),
        };

        private static readonly FishLook Neutral = Look("708090", "E6EAEC", "90A0AA", "40505A", FishPattern.Plain, 1f);

        public static FishLook For(string speciesId)
        {
            return speciesId != null && Looks.TryGetValue(speciesId, out var look) ? look : Neutral;
        }

        private static FishLook Look(string back, string belly, string fin, string mark, FishPattern pattern, float chubbiness, float finSize = 1f)
        {
            return new FishLook
            {
                Back = Hex(back),
                Belly = Hex(belly),
                Fin = Hex(fin),
                Mark = Hex(mark),
                Pattern = pattern,
                Chubbiness = chubbiness,
                FinSize = finSize,
            };
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var color);
            return color;
        }
    }
}
