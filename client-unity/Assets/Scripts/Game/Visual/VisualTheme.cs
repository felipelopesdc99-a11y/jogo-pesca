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

        // Palette A "Ardósia" (owner, 20/10/2026; addendum A-190): 14 interface colours in tema_visual.json, the other
        // pieces derived from them in Derive() (docs/ART_BIBLE_PIXEL.md 6.1 stays the locked reference).
        public Color Panel = Hex("#283050");
        public Color PanelElevated = Hex("#36406A");
        public Color Card = Hex("#1E2541");
        public Color Ink = Hex("#0A0C1C");
        public Color Text = Hex("#F4ECD6");
        public Color TextMuted = Hex("#C9C0A4");
        public Color Frame = Hex("#D6AD57");
        public Color FrameDark = Hex("#8A6C30");
        public Color Action = Hex("#5ED6D2");
        public Color ActionDark = Hex("#4BC0BC");
        public Color Reward = Hex("#FFD667");
        public Color Danger = Hex("#FF7A6B");
        public Color Success = Hex("#5BDB9A");
        public Color Exceptional = Hex("#F6B93B");

        // Derived pieces (not in the file since A-190): kept as fields so every window keeps its call sites.
        /// <summary>Sunken fills (wells, pills over the scene): the card colour.</summary>
        public Color Night;
        public Color Border;
        public Color ActionHover;
        public Color RewardLight;
        public Color FrameInner;
        /// <summary>Titles are cream, like the text (A-190): the yellow is only for rewards.</summary>
        public Color Title;
        public Color Field;
        public Color CardLine;
        /// <summary>The one-pixel line of idle chips and plain cards.</summary>
        public Color ChipOff;
        public Color ChipOn;
        public Color BarTrack;
        public Color Xp;
        public Color XpStripe;
        public Color Attribute;
        public Color Alert;
        public Color Veil;

        public VisualTheme()
        {
            Derive();
        }

        /// <summary>Recomputes the derived pieces from the 14 palette colours.</summary>
        private void Derive()
        {
            Night = Card;
            Field = Card;
            Border = FrameDark;
            ActionHover = Color.Lerp(Action, Color.white, 0.2f);
            RewardLight = Color.Lerp(Reward, Color.white, 0.4f);
            FrameInner = Ink;
            CardLine = Ink;
            Veil = Ink;
            Title = Text;
            ChipOff = Color.Lerp(PanelElevated, Text, 0.14f);
            ChipOn = PanelElevated;
            BarTrack = Color.Lerp(Card, Ink, 0.45f);
            Xp = Action;
            XpStripe = ActionDark;
            Attribute = Action;
            Alert = Danger;
        }

        public float PanelOpacity = 1f;
        public float OverlayOpacity = 0.35f;
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
            new RarityColor { id = "uncommon", color = "#5BDB9A" },
            new RarityColor { id = "rare", color = "#7AAEFF" },
            new RarityColor { id = "epic", color = "#C084FC" },
            new RarityColor { id = "legendary", color = "#FF6B6E" },
            new RarityColor { id = "mythic", color = "#F472B6" },
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
                Panel = Parse(p.panel, Panel);
                PanelElevated = Parse(p.panel_elevated, PanelElevated);
                Card = Parse(p.card, Card);
                Ink = Parse(p.ink, Ink);
                Text = Parse(p.text, Text);
                TextMuted = Parse(p.text_muted, TextMuted);
                Frame = Parse(p.frame, Frame);
                FrameDark = Parse(p.frame_dark, FrameDark);
                Action = Parse(p.action, Action);
                ActionDark = Parse(p.action_dark, ActionDark);
                Reward = Parse(p.reward, Reward);
                Danger = Parse(p.danger, Danger);
                Success = Parse(p.success, Success);
            }

            Derive();
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
            public string panel, panel_elevated, card, ink, text, text_muted, frame, frame_dark, action, action_dark, reward, danger, success;
        }

        [Serializable]
        public sealed class RarityColor
        {
            public string id;
            public string color;
        }
    }
}
