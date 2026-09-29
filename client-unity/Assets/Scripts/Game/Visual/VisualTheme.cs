using System;
using UnityEngine;

namespace FishingIdle.Game.Visual
{
    /// <summary>
    /// The central visual theme (Art Bible, section 36): palette, rarity accents, glow intensity and
    /// the durations of toasts and celebrations, read from Resources/Visual/tema_visual.json.
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

        public Color Night = Hex("#0A1422");
        public Color Panel = Hex("#11223A");
        public Color PanelElevated = Hex("#183050");
        public Color Border = Hex("#274A70");
        public Color Text = Hex("#EEF4FB");
        public Color TextMuted = Hex("#9DB2C9");
        public Color Action = Hex("#25C4C1");
        public Color ActionHover = Hex("#3AD6D2");
        public Color Reward = Hex("#F6B93B");
        public Color RewardLight = Hex("#FFE08A");
        public Color Danger = Hex("#EF6B5B");
        public Color Success = Hex("#2CCB7F");
        public Color Exceptional = Hex("#F6B93B");

        public float PanelOpacity = 0.94f;
        public float OverlayOpacity = 0.5f;
        public float GlowIntensity = 0.7f;
        public float ToastSeconds = 4f;
        public float ToastImportantSeconds = 6f;
        public float ToastWarningSeconds = 8f;
        public float CelebrationSeconds = 3.2f;
        public float WindowFadeSeconds = 0.18f;
        public float CoinCountSeconds = 0.8f;

        private RarityColor[] _rarities =
        {
            new RarityColor { id = "common", color = "#8193A8" },
            new RarityColor { id = "uncommon", color = "#2CCB7F" },
            new RarityColor { id = "rare", color = "#4D8DFF" },
            new RarityColor { id = "epic", color = "#A855F7" },
            new RarityColor { id = "legendary", color = "#F6B93B" },
            new RarityColor { id = "mythic", color = "#EF6B5B" },
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

            return Hex("#8193A8");
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

            Exceptional = Parse(f.exceptional_color, Exceptional);
            if (f.rarities != null && f.rarities.Length > 0)
            {
                _rarities = f.rarities;
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
            public RarityColor[] rarities;
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
        }

        [Serializable]
        private sealed class Palette
        {
            public string night, panel, panel_elevated, border, text, text_muted, action, action_hover, reward, reward_light, danger, success;
        }

        [Serializable]
        public sealed class RarityColor
        {
            public string id;
            public string color;
        }
    }
}
