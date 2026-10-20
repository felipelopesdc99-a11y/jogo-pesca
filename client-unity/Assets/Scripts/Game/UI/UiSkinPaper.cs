using System.Collections.Generic;
using FishingIdle.Game.Scene;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// What is left of the "no papel" mode of the "Diário de bordo" (addendum A-170). Since the ARC skin (A-184) a window
    /// is a night panel like the rest of the interface, so paper mode never turns on: <see cref="SetPaper"/> keeps
    /// <see cref="Paper"/> false and every colour and style answers with its night (ARC) value.
    /// </summary>
    /// <remarks>
    /// The API stays so the windows written for the diary keep compiling and drawing: the "paper" colours below now hold
    /// their ARC twins (the sheet is the panel night, the note card is the card, the ink is the light text), and
    /// <see cref="InkOf"/> lightens a colour until it reads at 4.5:1 on a card. <see cref="NightScope"/> and
    /// <see cref="Track"/> are kept and change nothing visible.
    /// </remarks>
    public sealed partial class UiSkin
    {
        // ------------------------------------------------------------------ the old paper palette, now its ARC twins (A-184)

        /// <summary>Where a window's content sits: the panel night (#0D0F20). Was the paper sheet of A-170.</summary>
        public static Color PaperFill => Theme.Panel;

        /// <summary>Same as <see cref="PaperFill"/> (the sheet had two tones; the panel has one).</summary>
        public static Color PaperFillBottom => Theme.Panel;

        /// <summary>A card on the panel (#141832). Was the note card.</summary>
        public static Color PaperCard => Theme.Card;

        /// <summary>A sunken spot (counter, bar track): the field (#090B18).</summary>
        public static Color PaperInset => Theme.Field;

        /// <summary>A raised fill behind text on the panel: the raised panel (#161A35).</summary>
        public static Color PaperShade => Theme.PanelElevated;

        /// <summary>Lines and borders drawn on the panel: the dark gold (#7D6127, decoration only, never text).</summary>
        public static Color PaperLine => Theme.FrameDark;

        /// <summary>Text on the panel (#F4ECD6, 16:1). Was the dark ink.</summary>
        public static Color Ink => Theme.Text;

        /// <summary>Secondary text on the panel (#A7A28F, 7,4:1).</summary>
        public static Color InkSoft => Theme.TextMuted;

        /// <summary>Action text on the panel: the turquoise (8,8:1).</summary>
        public static Color InkAccent => Theme.Action;

        /// <summary>Reward text on the panel: the reward gold (13,6:1), still only for rewards.</summary>
        public static Color InkReward => Theme.Reward;

        /// <summary>Red text on the panel (6,3:1).</summary>
        public static Color InkDanger => Theme.Danger;

        /// <summary>Green text on the panel (9,0:1).</summary>
        public static Color InkSuccess => Theme.Success;

        /// <summary>The contrast every coloured text reaches against the cards (a margin over 4.5:1).</summary>
        private const float InkContrast = 4.6f;

        /// <summary>The night of the theme whatever the mode: a dark disc that frames art or a white icon on the sheet.</summary>
        public static Color SceneNight => Theme.Night;

        /// <summary>The night turquoise whatever the mode (the "i" on the window label, the accent of the note).</summary>
        public static Color NightAccent => Theme.Action;

        /// <summary>The night gold whatever the mode (the "RECORDE" seal reads the same on the sheet and over the scene).</summary>
        public static Color NightReward => Theme.Reward;

        /// <summary>The night red whatever the mode (an icon on a dark disc inside a window).</summary>
        public static Color NightDanger => Theme.Danger;

        /// <summary>The night twin of a colour that may have been taken as its paper ink (seals keep their night look).</summary>
        public static Color NightOf(Color color) => ToNight(color);

        private static readonly Dictionary<GUIStyle, GUIStyle> ColoredStyles = new Dictionary<GUIStyle, GUIStyle>();

        /// <summary>
        /// A label in <paramref name="color"/>. On the sheet the colour becomes its ink (<see cref="InkOf"/>) and is set as
        /// the text colour: tinting with GUI.contentColor multiplies the dark ink and the colour vanishes. With
        /// <paramref name="ink"/> false the colour is used as given in both modes (text on a dark seal).
        /// </summary>
        public static void ColoredLabel(Rect rect, string text, GUIStyle style, Color color, bool ink = true)
        {
            style = _instance != null ? _instance.TitleSafe(style, text) : style;
            if (!ColoredStyles.TryGetValue(style, out var colored))
            {
                colored = new GUIStyle(style);
                ColoredStyles[style] = colored;
            }

            var c = ink ? InkOf(color) : color;
            colored.normal.textColor = c;
            colored.hover.textColor = c;
            GUI.Label(rect, text, colored);
        }

        /// <summary>The night turquoise hover whatever the mode.</summary>
        public static Color NightAccentHover => Theme.ActionHover;

        /// <summary>Always false since the ARC skin (A-184): windows are night panels. Kept for the code written for A-170.</summary>
        public static bool Paper { get; private set; }

        private static readonly Dictionary<Color, Color> InkCache = new Dictionary<Color, Color>();

        /// <summary>
        /// A dark fill a window draws behind text (a tile, a badge, a tooltip), as it should look where it is drawn: on
        /// the sheet, a light paper tone (the darkest fills become <see cref="PaperShade"/>, the lighter "selected" ones
        /// almost <see cref="PaperCard"/>), keeping the alpha; at night, unchanged.
        /// </summary>
        public static Color OnSheet(Color nightFill)
        {
            if (!Paper)
            {
                return nightFill;
            }

            var v = Mathf.Clamp01((Mathf.Max(nightFill.r, Mathf.Max(nightFill.g, nightFill.b)) - 0.12f) / 0.22f);
            var c = Color.Lerp(PaperShade, PaperCard, v);
            return new Color(c.r, c.g, c.b, nightFill.a);
        }

        /// <summary>
        /// <paramref name="color"/> lightened (towards white, same hue) until it reads at 4.5:1 on a card, the lightest of
        /// the ARC fills text sits on: rarity, size and accent colours used as text (Épico and Lendário need it). Colours
        /// that already pass come back unchanged.
        /// </summary>
        public static Color InkOf(Color color)
        {
            var key = new Color(color.r, color.g, color.b, 1f);
            if (!InkCache.TryGetValue(key, out var ink))
            {
                ink = key;
                for (var i = 0; i < 40 && Contrast(ink, Theme.Card) < InkContrast; i++)
                {
                    ink = Color.Lerp(ink, Color.white, 0.06f);
                }

                InkCache[key] = ink;
            }

            return new Color(ink.r, ink.g, ink.b, color.a);
        }

        /// <summary>The same as <see cref="InkOf"/>: a colour made readable as text on the ARC panels and cards.</summary>
        public static Color Readable(Color color) => InkOf(color);

        /// <summary>WCAG contrast ratio between two colours (alpha ignored).</summary>
        public static float Contrast(Color a, Color b)
        {
            var la = Luminance(a);
            var lb = Luminance(b);
            return (Mathf.Max(la, lb) + 0.05f) / (Mathf.Min(la, lb) + 0.05f);
        }

        private static float Luminance(Color c)
        {
            return 0.2126f * Channel(c.r) + 0.7152f * Channel(c.g) + 0.0722f * Channel(c.b);
        }

        private static float Channel(float v)
        {
            return v <= 0.03928f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
        }

        // ------------------------------------------------------------------ the switch

        /// <summary>
        /// Was the switch of paper mode (A-170). Since the ARC skin (A-184) windows are night panels: the call is kept for
        /// the code that still makes it, and paper mode stays off whatever <paramref name="on"/> says.
        /// </summary>
        public static void SetPaper(bool on)
        {
            if (!Paper || on)
            {
                return;
            }

            Paper = false;
            if (_instance != null)
            {
                _instance.ApplyMode();
            }
        }

        /// <summary>
        /// Night colours for a block drawn over a dark scene inside a window; restores the previous mode when
        /// disposed: <c>using (UiSkin.NightScope()) { … }</c>.
        /// </summary>
        public static Scope NightScope()
        {
            var previous = Paper;
            SetPaper(false);
            return new Scope(previous);
        }

        /// <summary>Restores the mode a <see cref="NightScope"/> found.</summary>
        public readonly struct Scope : System.IDisposable
        {
            private readonly bool _previous;

            public Scope(bool previous)
            {
                _previous = previous;
            }

            public void Dispose()
            {
                SetPaper(_previous);
            }
        }

        // ------------------------------------------------------------------ tracked styles

        private sealed class Tracked
        {
            public GUIStyle Style;
            public readonly Color[] NightText = new Color[8], PaperText = new Color[8];
            public readonly Texture2D[] NightBack = new Texture2D[8], PaperBack = new Texture2D[8];
        }

        private readonly List<Tracked> _tracked = new List<Tracked>();
        private readonly HashSet<GUIStyle> _trackedSet = new HashSet<GUIStyle>();
        private readonly Dictionary<Texture2D, Texture2D> _paperBack = new Dictionary<Texture2D, Texture2D>();
        private readonly Dictionary<Texture2D, Texture2D> _nightBack = new Dictionary<Texture2D, Texture2D>();

        /// <summary>
        /// Registers <paramref name="style"/> so it follows paper mode (text colours from the palette swap to ink,
        /// skin backgrounds swap to paper). Call it after setting the style's colours; returns the style.
        /// Colours outside the palette (white on a button, dark text on a medal) stay as they are.
        /// </summary>
        public GUIStyle Track(GUIStyle style)
        {
            if (style == null || _trackedSet.Contains(style))
            {
                return style;
            }

            var t = new Tracked { Style = style };
            var states = States(style);
            for (var i = 0; i < states.Length; i++)
            {
                // The style may have been copied while paper mode was on: bring it back to night first.
                var night = ToNight(states[i].textColor);
                t.NightText[i] = night;
                t.PaperText[i] = ToPaper(night);
                var back = states[i].background;
                var nightBack = back != null && _nightBack.TryGetValue(back, out var nb) ? nb : back;
                t.NightBack[i] = nightBack;
                t.PaperBack[i] = nightBack != null && _paperBack.TryGetValue(nightBack, out var pb) ? pb : nightBack;
            }

            _tracked.Add(t);
            _trackedSet.Add(style);
            Apply(t, Paper);
            return style;
        }

        private void ApplyMode()
        {
            for (var i = 0; i < _tracked.Count; i++)
            {
                Apply(_tracked[i], Paper);
            }
        }

        private static void Apply(Tracked t, bool paper)
        {
            var states = States(t.Style);
            for (var i = 0; i < states.Length; i++)
            {
                states[i].textColor = paper ? t.PaperText[i] : t.NightText[i];
                states[i].background = paper ? t.PaperBack[i] : t.NightBack[i];
            }
        }

        private static GUIStyleState[] States(GUIStyle s)
        {
            return new[] { s.normal, s.hover, s.active, s.focused, s.onNormal, s.onHover, s.onActive, s.onFocused };
        }

        /// <summary>The paper colour of a night palette colour (others unchanged).</summary>
        private static Color ToPaper(Color c)
        {
            var t = Theme;
            Color ink;
            if (Same(c, t.Text))
            {
                ink = Ink;
            }
            else if (Same(c, t.TextMuted))
            {
                ink = InkSoft;
            }
            else if (Same(c, t.Reward) || Same(c, t.RewardLight))
            {
                ink = InkReward;
            }
            else if (Same(c, t.Action) || Same(c, t.ActionHover))
            {
                ink = InkAccent;
            }
            else if (Same(c, t.Danger))
            {
                ink = InkDanger;
            }
            else if (Same(c, t.Success))
            {
                ink = InkSuccess;
            }
            else
            {
                return c;
            }

            return new Color(ink.r, ink.g, ink.b, c.a);
        }

        /// <summary>The night colour of a paper palette colour (others unchanged).</summary>
        private static Color ToNight(Color c)
        {
            var t = Theme;
            Color night;
            if (Same(c, Ink))
            {
                night = t.Text;
            }
            else if (Same(c, InkSoft))
            {
                night = t.TextMuted;
            }
            else if (Same(c, InkReward))
            {
                night = t.Reward;
            }
            else if (Same(c, InkAccent))
            {
                night = t.Action;
            }
            else if (Same(c, InkDanger))
            {
                night = t.Danger;
            }
            else if (Same(c, InkSuccess))
            {
                night = t.Success;
            }
            else
            {
                return c;
            }

            return new Color(night.r, night.g, night.b, c.a);
        }

        private static bool Same(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.004f && Mathf.Abs(a.g - b.g) < 0.004f && Mathf.Abs(a.b - b.b) < 0.004f;
        }

        // ------------------------------------------------------------------ paper backgrounds

        /// <summary>Registers every skin style with <see cref="Track"/>. Called once by the constructor (no paper twins since A-184).</summary>
        private void BuildPaper()
        {
            foreach (var style in new[]
            {
                Panel, PanelSolid, Card, CardHovered, CardSelected, CardImportant, IconTile, Button, ButtonPrimary, ButtonDanger,
                ButtonReward, Chip, ChipActive, Nav, NavActive, SearchField, Title, Heading, Body, BodyBold, ChipText, Small,
                SmallBold, SmallMuted, SmallRight, SmallMutedRight, SmallGold, SmallGoldRight, Number, NumberRight, Center,
                CenterBold, SmallMutedCenter, Display, DisplaySub,
            })
            {
                Track(style);
            }
        }
    }
}
