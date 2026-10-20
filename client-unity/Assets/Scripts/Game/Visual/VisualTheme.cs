using System;
using UnityEngine;

namespace FishingIdle.Game.Visual
{
    /// <summary>
    /// The central visual theme (Art Bible, section 36): palette, rarity accents, glow intensity and
    /// the size-category accents, the durations of toasts and celebrations, read from Resources/Visual/tema_visual.json.
    /// </summary>
    /// <remarks>
    /// Presentation only, so it lives with the client and not in /config. Every colour the
    /// interface uses comes from here; a missing or broken file falls back to the defaults below,
    /// which match the Art Bible, and logs one warning.
    /// </remarks>
    public sealed class VisualTheme
    {
        private const string ResourcePath = "Visual/tema_visual";

        private static VisualTheme _current;

        // The ARC interface palette (docs/ART_BIBLE_PIXEL.md, section 6.1; addendum A-184).
        public Color Night = Hex("#090B18");
        public Color Panel = Hex("#0D0F20");
        public Color PanelElevated = Hex("#161A35");
        public Color Border = Hex("#7D6127");
        public Color Text = Hex("#F4ECD6");
        public Color TextMuted = Hex("#A7A28F");
        public Color Action = Hex("#25C4C1");
        public Color ActionHover = Hex("#3AD6D2");
        public Color Reward = Hex("#FFD667");
        public Color RewardLight = Hex("#FFE3A0");
        public Color Danger = Hex("#EF6B5B");
        public Color Success = Hex("#2CCB7F");
        public Color Exceptional = Hex("#F6B93B");

        // ARC pieces drawn by the code (structure, never reward): the thin gold frame and its dark twin, the line between
        // them, the title colour of Press Start 2P, fields, cards, idle chips, bars and the alert counters.
        public Color Frame = Hex("#D6AD57");
        public Color FrameDark = Hex("#7D6127");
        public Color FrameInner = Hex("#0A0C1A");
        public Color Title = Hex("#FFE3A0");
        public Color Field = Hex("#090B18");
        public Color Card = Hex("#141832");
        public Color CardLine = Hex("#0B0D1E");
        public Color ChipOff = Hex("#2A2E50");
        public Color ChipOn = Hex("#1D2244");
        public Color BarTrack = Hex("#22264A");
        public Color Xp = Hex("#7FD6C2");
        public Color XpStripe = Hex("#62C0AD");
        public Color Attribute = Hex("#7FC4EA");
        public Color Alert = Hex("#E0473A");
        public Color Veil = Hex("#0A0C1C");

        public float PanelOpacity = 0.93f;
        public float OverlayOpacity = 0.45f;
        public float GlowIntensity = 0.7f;
        public float ToastSeconds = 4f;
        public float ToastImportantSeconds = 6f;
        public float ToastWarningSeconds = 8f;
        public float CelebrationSeconds = 3.2f;
        public float WindowFadeSeconds = 0.18f;
        public float CoinCountSeconds = 0.8f;

        /// <summary>How much small ambient life the scene shows (dust, dragonflies, fish shadows…): 0 = none, 1 = as designed, up to 2.</summary>
        public float AmbientLife = 1f;

        private RarityColor[] _rarities =
        {
            new RarityColor { id = "common", color = "#F2F5F8" },
            new RarityColor { id = "uncommon", color = "#2CCB7F" },
            new RarityColor { id = "rare", color = "#4D8DFF" },
            new RarityColor { id = "epic", color = "#A855F7" },
            new RarityColor { id = "legendary", color = "#E5484D" },
            new RarityColor { id = "mythic", color = "#EC4899" },
        };

        private RarityColor[] _sizes =
        {
            new RarityColor { id = "small", color = "#8FD9A8" },
            new RarityColor { id = "adult", color = "#B9A6F2" },
            new RarityColor { id = "large", color = "#FF9F6B" },
            new RarityColor { id = "perfect", color = "#8FF3FF" },
        };

        /// <summary>The theme in use. Loaded on first access.</summary>
        public static VisualTheme Current => _current ?? (_current = Load());

        /// <summary>Accent colour of a rarity id; unknown ids get the Comum neutral.</summary>
        public Color Rarity(string rarityId)
        {
            foreach (var r in _rarities)
            {
                if (r.id == rarityId)
                {
                    return Hex(r.color);
                }
            }

            return Hex("#F2F5F8");
        }

        /// <summary>Whether a size has its own seal and celebration (Excepcional and, above it, Perfeição).</summary>
        public static bool IsSpecialSize(string sizeCategoryId) => sizeCategoryId == "exceptional" || sizeCategoryId == "perfect";

        /// <summary>Accent colour of a size category id; Excepcional always uses its own gold.</summary>
        public Color Size(string sizeCategoryId)
        {
            if (sizeCategoryId == "exceptional")
            {
                return Exceptional;
            }

            foreach (var s in _sizes)
            {
                if (s.id == sizeCategoryId)
                {
                    return Hex(s.color);
                }
            }

            return TextMuted;
        }

        private static VisualTheme Load()
        {
            var theme = new VisualTheme();
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                Debug.LogWarning("[FishingIdle] Visual theme not found at Resources/" + ResourcePath + ".json; using defaults.");
                return theme;
            }

            try
            {
                var file = JsonUtility.FromJson<ThemeFile>(asset.text);
                theme.Apply(file);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[FishingIdle] Visual theme could not be read; using defaults. " + e.Message);
                return new VisualTheme();
            }

            return theme;
        }

        private void Apply(ThemeFile f)
        {
            if (f == null)
            {
                return;
            }

            var p = f.palette;
            if (p != null)
            {
                Night = Parse(p.night, Night);
                Panel = Parse(p.panel, Panel);
                PanelElevated = Parse(p.panel_elevated, PanelElevated);
                Border = Parse(p.border, Border);
                Text = Parse(p.text, Text);
                TextMuted = Parse(p.text_muted, TextMuted);
                Action = Parse(p.action, Action);
                ActionHover = Parse(p.action_hover, ActionHover);
                Reward = Parse(p.reward, Reward);
                RewardLight = Parse(p.reward_light, RewardLight);
                Danger = Parse(p.danger, Danger);
                Success = Parse(p.success, Success);
            }

            var a = f.arc;
            if (a != null)
            {
                Frame = Parse(a.frame, Frame);
                FrameDark = Parse(a.frame_dark, FrameDark);
                FrameInner = Parse(a.frame_inner, FrameInner);
                Title = Parse(a.title, Title);
                Field = Parse(a.field, Field);
                Card = Parse(a.card, Card);
                CardLine = Parse(a.card_line, CardLine);
                ChipOff = Parse(a.chip_off, ChipOff);
                ChipOn = Parse(a.chip_on, ChipOn);
                BarTrack = Parse(a.bar_track, BarTrack);
                Xp = Parse(a.xp, Xp);
                XpStripe = Parse(a.xp_stripe, XpStripe);
                Attribute = Parse(a.attribute, Attribute);
                Alert = Parse(a.alert, Alert);
                Veil = Parse(a.veil, Veil);
            }

            Exceptional = Parse(f.exceptional_color, Exceptional);
            if (f.rarities != null && f.rarities.Length > 0)
            {
                _rarities = f.rarities;
            }

            if (f.size_categories != null && f.size_categories.Length > 0)
            {
                _sizes = f.size_categories;
            }

            PanelOpacity = Positive(f.panel_opacity, PanelOpacity, 1f);
            OverlayOpacity = Positive(f.overlay_opacity, OverlayOpacity, 1f);
            GlowIntensity = Positive(f.glow_intensity, GlowIntensity, 1f);
            ToastSeconds = Positive(f.toast_seconds, ToastSeconds, 60f);
            ToastImportantSeconds = Positive(f.toast_important_seconds, ToastImportantSeconds, 60f);
            ToastWarningSeconds = Positive(f.toast_warning_seconds, ToastWarningSeconds, 60f);
            CelebrationSeconds = Positive(f.celebration_seconds, CelebrationSeconds, 10f);
            WindowFadeSeconds = Positive(f.window_fade_seconds, WindowFadeSeconds, 2f);
            CoinCountSeconds = Positive(f.coin_count_seconds, CoinCountSeconds, 5f);
            if (f.ambient_life >= 0f)
            {
                AmbientLife = Mathf.Clamp(f.ambient_life, 0f, 2f);
            }
        }

        private static float Positive(float value, float fallback, float max)
        {
            return value > 0f ? Mathf.Min(value, max) : fallback;
        }

        private static Color Parse(string hex, Color fallback)
        {
            return !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out var c) ? c : fallback;
        }

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        // ------------------------------------------------------------------ file shape (JsonUtility)

        [Serializable]
        private sealed class ThemeFile
        {
            public Palette palette;
            public Arc arc;
            public RarityColor[] rarities;
            public RarityColor[] size_categories;
            public string exceptional_color;
            public float panel_opacity;
            public float overlay_opacity;
            public float glow_intensity;
            public float toast_seconds;
            public float toast_important_seconds;
            public float toast_warning_seconds;
            public float celebration_seconds;
            public float window_fade_seconds;
            public float coin_count_seconds;

            // -1 = not in the file (0 is a valid value: no ambient life).
            public float ambient_life = -1f;
        }

        [Serializable]
        private sealed class Palette
        {
            public string night, panel, panel_elevated, border, text, text_muted, action, action_hover, reward, reward_light, danger, success;
        }

        [Serializable]
        private sealed class Arc
        {
            public string frame, frame_dark, frame_inner, title, field, card, card_line, chip_off, chip_on, bar_track, xp, xp_stripe, attribute, alert, veil;
        }

        [Serializable]
        public sealed class RarityColor
        {
            public string id;
            public string color;
        }
    }
}
