using System.Collections.Generic;
using FishingIdle.Game.Scene;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The "no papel" variants of the skin: inside a window ("Diário de bordo", addendum A-169) the content sits on
    /// light paper, so text turns to dark ink and panels, cards, chips and secondary buttons turn to paper tones.
    /// </summary>
    /// <remarks>
    /// <see cref="WindowFrame"/> turns paper mode on when it draws a window's sheet; the HUD turns it off after the
    /// windows (<see cref="WindowFrame.DrawInfoBalloon"/>), and every confirmation dialog stays on the night panel.
    /// The switch is global: the colour properties (<see cref="Text"/>, <see cref="Muted"/>, <see cref="Gold"/>…)
    /// answer with the ink palette, and every style registered with <see cref="Track"/> has its text colours and
    /// backgrounds swapped in place, so window styles copied from the skin follow too. Art drawn over a dark scene
    /// inside a window (the aquarium tank, the shop wall, the battle) opens a <see cref="NightScope"/>.
    /// Every ink colour keeps at least 4.5:1 against the paper (contrasts in docs/GDD_ADENDO.md, A-169).
    /// </remarks>
    public sealed partial class UiSkin
    {
        // ------------------------------------------------------------------ the paper palette (A-169)

        /// <summary>The sheet: #F2E4C4 at the top, #EFDFBB at the bottom.</summary>
        public static readonly Color PaperFill = Hex(0xF2E4C4), PaperFillBottom = Hex(0xEFDFBB);

        /// <summary>A note card on the sheet (fichas, cards): #FFFAF0.</summary>
        public static readonly Color PaperCard = Hex(0xFFFAF0);

        /// <summary>A sunken spot on the sheet (counter, bar track): #E8D7B2.</summary>
        public static readonly Color PaperInset = Hex(0xE8D7B2);

        /// <summary>
        /// What <see cref="Night"/> answers on the sheet: the dark fills windows put behind text (tiles, discs, glass)
        /// become this light paper shade, so their ink text stays readable (#E3CFA6; ink on it 8,9:1).
        /// </summary>
        public static readonly Color PaperShade = Hex(0xE3CFA6);

        /// <summary>Lines and borders drawn on the sheet: #C9AB78 (decoration only, never text).</summary>
        public static readonly Color PaperLine = Hex(0xC9AB78);

        /// <summary>Dark ink (10,9:1 on the sheet).</summary>
        public static readonly Color Ink = Hex(0x3A2A18);

        /// <summary>Soft ink for secondary text (6,4:1 on the sheet).</summary>
        public static readonly Color InkSoft = Hex(0x6A4A2A);

        /// <summary>Darkened turquoise for action text on paper (5,8:1).</summary>
        public static readonly Color InkAccent = Hex(0x12605D);

        /// <summary>Darkened gold, still only for rewards (5,7:1).</summary>
        public static readonly Color InkReward = Hex(0x7A4E0A);

        /// <summary>Darkened red (5,8:1) and green (6,0:1).</summary>
        public static readonly Color InkDanger = Hex(0x9C3022), InkSuccess = Hex(0x17603A);

        /// <summary>The contrast every coloured ink reaches against the sheet (a margin over 4.5:1 for the darker spots).</summary>
        private const float InkContrast = 5f;

        /// <summary>The night of the theme whatever the mode: a dark disc that frames art or a white icon on the sheet.</summary>
        public static Color SceneNight => Theme.Night;

        /// <summary>The night turquoise whatever the mode (the "i" on the window label, the accent of the note).</summary>
        public static Color NightAccent => Theme.Action;

        /// <summary>The night turquoise hover whatever the mode.</summary>
        public static Color NightAccentHover => Theme.ActionHover;

        /// <summary>True while a window sheet is being drawn: colours and styles answer with their paper variants.</summary>
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
        /// <paramref name="color"/> darkened (same hue) until it reads at 5:1 on the sheet: rarity, size and accent
        /// colours used as text on paper. Colours that already pass come back unchanged.
        /// </summary>
        public static Color InkOf(Color color)
        {
            var key = new Color(color.r, color.g, color.b, 1f);
            if (!InkCache.TryGetValue(key, out var ink))
            {
                ink = key;
                for (var i = 0; i < 40 && Contrast(ink, PaperFill) < InkContrast; i++)
                {
                    ink = new Color(ink.r * 0.94f, ink.g * 0.94f, ink.b * 0.94f, 1f);
                }

                InkCache[key] = ink;
            }

            return new Color(ink.r, ink.g, ink.b, color.a);
        }

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

        private static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
        }

        // ------------------------------------------------------------------ the switch

        /// <summary>Turns paper mode on or off (cheap when nothing changes).</summary>
        public static void SetPaper(bool on)
        {
            if (Paper == on)
            {
                return;
            }

            Paper = on;
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

        /// <summary>Builds the paper twin of each night background and registers every skin style. Called once by the constructor.</summary>
        private void BuildPaper()
        {
            var cardBorder = Hex(0xD8C393);
            var sheetPanel = Hex(0xF7EDD6);
            PaperBack(Panel, Art.PanelTexture(sheetPanel, sheetPanel, PaperLine, 16, 1.5f, 96, 0f));
            PaperBack(PanelSolid, Art.PanelTexture(sheetPanel, sheetPanel, PaperLine, 16, 1.5f, 96, 0f));
            PaperBack(Card, Art.PanelTexture(PaperCard, PaperCard, cardBorder, 12, 1.5f, 64, 0f));
            PaperBack(CardHovered, Art.PanelTexture(Color.white, PaperCard, Hex(0xB9976A), 12, 1.5f, 64, 0f));
            PaperBack(CardSelected, Art.PanelTexture(Hex(0xE6F3EC), Hex(0xDCEEE6), Theme.Action, 12, 2.5f, 64, 0f));
            PaperBack(CardImportant, Art.PanelTexture(Hex(0xFFF4D6), Hex(0xFBEBC4), Theme.Reward, 12, 2f, 64, 0f));
            PaperBack(IconTile, Art.PanelTexture(PaperCard, PaperCard, cardBorder, 12, 1.5f, 48, 0f));

            var button = Art.PanelTexture(Hex(0xEEDFBD), Hex(0xE3CFA3), Hex(0xB9976A), 10, 1.5f, 48, 0.05f);
            var buttonHover = Art.PanelTexture(Hex(0xF6EAD0), Hex(0xEADAB4), Hex(0x9E7C50), 10, 1.5f, 48, 0.06f);
            var buttonDown = Art.PanelTexture(Hex(0xE0CB9C), Hex(0xE0CB9C), Hex(0xB9976A), 10, 1.5f, 48, 0f);
            PaperStates(Button, button, buttonHover, buttonDown);

            var chip = Art.PanelTexture(Hex(0xF8F0DC), Hex(0xF3E8CE), PaperLine, 10, 1.2f, 40, 0f);
            var chipHover = Art.PanelTexture(Color.white, Hex(0xF8F0DC), Hex(0x9E7C50), 10, 1.2f, 40, 0f);
            var chipDown = Art.PanelTexture(PaperInset, PaperInset, PaperLine, 10, 1.2f, 40, 0f);
            PaperStates(Chip, chip, chipHover, chipDown);
            PaperStates(Nav, chip, chipHover, chipDown);

            var search = Art.PanelTexture(PaperCard, PaperCard, Alpha(InkAccent, 0.8f), 10, 2f, 40, 0f);
            var searchFocus = Art.PanelTexture(PaperCard, PaperCard, InkAccent, 10, 2.5f, 40, 0f);
            PaperStates(SearchField, search, search, searchFocus);
            PaperBack(SearchField.focused.background, searchFocus);

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

        private void PaperBack(GUIStyle style, Texture2D paper)
        {
            PaperBack(style.normal.background, paper);
        }

        private void PaperBack(Texture2D night, Texture2D paper)
        {
            if (night == null || _paperBack.ContainsKey(night))
            {
                return;
            }

            _paperBack[night] = paper;
            _nightBack[paper] = night;
        }

        private void PaperStates(GUIStyle style, Texture2D normal, Texture2D hover, Texture2D active)
        {
            PaperBack(style.normal.background, normal);
            PaperBack(style.hover.background, hover);
            PaperBack(style.active.background, active);
        }
    }
}
