using FishingIdle.Game.Scene;
using FishingIdle.Game.Visual;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// Colours, fonts, textures and IMGUI styles of the game interface, in the ARC skin (docs/ART_BIBLE_PIXEL.md,
    /// section 7; addendum A-184), built once on first use from the visual theme.
    /// </summary>
    /// <remarks>
    /// The MVP interface is drawn with Unity's immediate-mode GUI (docs/DECISOES.md, TD-017). All
    /// look-and-feel lives here, in UiSkinArc.cs and in <see cref="VisualTheme"/>: night-blue panels with a thin gold
    /// frame (structure), turquoise for actions, the bright gold only for rewards, rarity colour only as an accent,
    /// Press Start 2P for titles and VT323 for text. Windows use the shared pieces below (panels, buttons with icons,
    /// pills, bars, outlines) so every screen speaks the same language.
    /// </remarks>
    public sealed partial class UiSkin
    {
        private static VisualTheme Theme => VisualTheme.Current;

        // The ARC palette (A-184). The paper mode of the old diary (A-170) and its ink colours left in M25-T17.
        public static Color Text => Theme.Text;
        public static Color Muted => Theme.TextMuted;
        public static Color Accent => Theme.Action;
        public static Color AccentHover => Theme.ActionHover;
        public static Color Gold => Theme.Reward;
        public static Color GoldLight => Theme.RewardLight;
        public static Color Danger => Theme.Danger;
        public static Color Success => Theme.Success;
        public static Color Border => Theme.Border;
        public static Color Night => Theme.Night;

        /// <summary>Accent of the Raro tier (kept for older call sites; prefer <see cref="RarityColor"/>).</summary>
        public static Color Rare => RarityColor("rare");

        /// <summary>The accent colour of a rarity id.</summary>
        public static Color RarityColor(string rarityId) => Theme.Rarity(rarityId);

        /// <summary>The accent colour of a size category id (Excepcional = gold).</summary>
        public static Color SizeColor(string sizeCategoryId) => Theme.Size(sizeCategoryId);

        public GUIStyle Panel, PanelSolid, Card, CardHovered, CardSelected, CardImportant, Shadow, Glow, Outline, Pill, IconTile;
        public GUIStyle Button, ButtonPrimary, ButtonDanger, ButtonReward, Chip, ChipActive, Nav, NavActive, SearchField;
        public GUIStyle Title, Heading, Body, BodyBold, ChipText, Small, SmallBold, SmallMuted, SmallRight, SmallMutedRight, SmallGold, SmallGoldRight, Number, NumberRight, Center, CenterBold, SmallMutedCenter, Badge, Display, DisplaySub, PillText;

        public Texture2D White, Overlay, Coin, Rays;

        public Font TitleFont { get; private set; }
        public Font TitleBoldFont { get; private set; }
        public Font BodyFont { get; private set; }
        public Font BodyBoldFont { get; private set; }

        private static UiSkin _instance;

        /// <summary>The shared skin. Only valid inside OnGUI (it reads GUI.skin the first time).</summary>
        public static UiSkin Get()
        {
            return _instance ?? (_instance = new UiSkin());
        }

        private UiSkin()
        {
            var t = Theme;

            // ARC fonts (Art Bible 7.1, TD-061): Press Start 2P for titles and big numbers, VT323 for everything else. While
            // a file is missing, Unity's default font is used (null font in a style). The old Fredoka and Nunito left in
            // M25-T17.
            PixelTitleFont = Resources.Load<Font>(TitleFontFile);
            PixelTextFont = Resources.Load<Font>(TextFontFile);
            TitleFont = PixelTitleFont;
            TitleBoldFont = PixelTitleFont;
            BodyFont = PixelTextFont;
            // VT323 has one weight: "bold" text keeps the same letters (emphasis comes from colour).
            BodyBoldFont = PixelTextFont;

            White = Art.SolidTexture(Color.white);
            Overlay = Art.SolidTexture(new Color(t.Veil.r, t.Veil.g, t.Veil.b, t.OverlayOpacity));
            // The pixel coin icon (ico_moeda); a coin drawn by the code while it is missing.
            Coin = ArtAssets.Icon(Icons.Coin) ?? Art.Circle.texture;
            Rays = Art.RaysTexture(14);

            // Panels: the three-line frame (gold, dark line, dark gold) with a 2-pixel chamfer (Art Bible 7).
            var frame = new[] { t.Frame, t.FrameInner, t.FrameDark };
            Panel = ArcBox(2, false, frame, Alpha(t.Panel, t.PanelOpacity));
            PanelSolid = ArcBox(2, false, frame, Alpha(t.Panel, 1f));

            // Cards (A-190, at most two levels of box): the card fill, one shade darker than the panel ("sunken"), with a
            // single one-pixel line and no inner line; the selected one has the reward gold line (Art Bible 7).
            Card = ArcBox(1, false, new[] { t.ChipOff }, Alpha(t.Card, 1f));
            CardHovered = ArcBox(1, false, new[] { Color.Lerp(t.ChipOff, t.Frame, 0.55f) }, Alpha(Color.Lerp(t.Card, t.PanelElevated, 0.5f), 1f));
            CardSelected = ArcBox(1, false, new[] { t.Reward }, Alpha(Color.Lerp(t.Card, t.PanelElevated, 0.6f), 1f));
            CardImportant = ArcBox(1, false, new[] { t.Frame }, Alpha(Color.Lerp(t.Card, t.Reward, 0.06f), 1f));

            // No soft blur in pixel art: the shadow is a hard offset (HardShadow) and the glow a pixel ring (PixelGlow).
            // The two styles stay for windows that tint them, as plain translucent shapes.
            Shadow = ArcBox(2, false, new Color[0], new Color(0f, 0f, 0f, 0.35f));
            Glow = ArcBox(2, false, new Color[0], new Color(1f, 1f, 1f, 0.18f));
            Outline = ArcBox(1, false, new[] { Color.white }, Color.clear);
            Pill = ArcBox(4, true, new[] { Color.white }, new Color(1f, 1f, 1f, 0.20f));
            Pill.padding = new RectOffset(8, 8, 2, 2);
            IconTile = ArcBox(1, false, new[] { t.FrameDark }, Alpha(t.Field, 1f));

            // Buttons are pills (Art Bible 7). Secondary: field with the structural gold line and title text. Primary
            // (action): dark with the turquoise line (turquesa = ação). Reward: the gold face in two bands, dark text, for
            // collecting and claiming only (dourado = recompensa). Danger: dark with the red line.
            var actionText = Color.Lerp(t.Action, Color.white, 0.45f);
            var dangerText = Color.Lerp(t.Danger, Color.white, 0.5f);
            Button = ButtonStyle(
                ArcFace(ButtonRadius, true, Alpha(t.Field, 0.98f), Alpha(t.Field, 0.98f), t.Frame),
                ArcFace(ButtonRadius, true, Alpha(t.PanelElevated, 1f), Alpha(t.PanelElevated, 1f), t.Title),
                ArcFace(ButtonRadius, true, Alpha(t.Panel, 1f), Alpha(t.Panel, 1f), t.FrameDark),
                t.Title);
            ButtonPrimary = ButtonStyle(
                ArcFace(ButtonRadius, true, Alpha(Color.Lerp(t.Field, t.Action, 0.14f), 1f), Alpha(Color.Lerp(t.Field, t.Action, 0.10f), 1f), t.Action),
                ArcFace(ButtonRadius, true, Alpha(Color.Lerp(t.Field, t.Action, 0.26f), 1f), Alpha(Color.Lerp(t.Field, t.Action, 0.20f), 1f), t.ActionHover),
                ArcFace(ButtonRadius, true, Alpha(Color.Lerp(t.Field, t.Action, 0.08f), 1f), Alpha(Color.Lerp(t.Field, t.Action, 0.08f), 1f), t.Action),
                actionText);
            ButtonPrimary.hover.textColor = Color.white;
            ButtonDanger = ButtonStyle(
                ArcFace(ButtonRadius, true, Alpha(Color.Lerp(t.Field, t.Danger, 0.16f), 1f), Alpha(Color.Lerp(t.Field, t.Danger, 0.12f), 1f), t.Danger),
                ArcFace(ButtonRadius, true, Alpha(Color.Lerp(t.Field, t.Danger, 0.28f), 1f), Alpha(Color.Lerp(t.Field, t.Danger, 0.22f), 1f), Color.Lerp(t.Danger, Color.white, 0.3f)),
                ArcFace(ButtonRadius, true, Alpha(Color.Lerp(t.Field, t.Danger, 0.10f), 1f), Alpha(Color.Lerp(t.Field, t.Danger, 0.10f), 1f), t.Danger),
                dangerText);
            ButtonDanger.hover.textColor = Color.white;
            var ink = new Color32(0x2A, 0x1A, 0x08, 0xFF);
            Color goldTop = new Color32(0xFF, 0xD9, 0x77, 0xFF), goldBottom = new Color32(0xD6, 0xA4, 0x40, 0xFF), goldLine = new Color32(0xFF, 0xF0, 0xB8, 0xFF);
            ButtonReward = ButtonStyle(
                ArcFace(ButtonRadius, true, goldTop, goldBottom, goldLine),
                ArcFace(ButtonRadius, true, Color.Lerp(goldTop, Color.white, 0.2f), Color.Lerp(goldBottom, Color.white, 0.15f), Color.white),
                ArcFace(ButtonRadius, true, goldBottom, goldBottom, goldTop),
                ink);

            // Chips (filters, sub-tabs): idle on the field with the dark-blue line and soft text; chosen with the gold line
            // and the title colour (the prototype's "Todas").
            Chip = ButtonStyle(
                ArcFace(ButtonRadius, true, Alpha(t.Field, 0.95f), Alpha(t.Field, 0.95f), t.ChipOff),
                ArcFace(ButtonRadius, true, Alpha(t.PanelElevated, 0.98f), Alpha(t.PanelElevated, 0.98f), Color.Lerp(t.ChipOff, t.Frame, 0.55f)),
                ArcFace(ButtonRadius, true, Alpha(t.Panel, 1f), Alpha(t.Panel, 1f), t.ChipOff),
                t.TextMuted, ChipSize);
            Chip.hover.textColor = t.Text;
            ChipActive = ButtonStyle(
                ArcFace(ButtonRadius, true, Alpha(t.ChipOn, 1f), Alpha(t.ChipOn, 1f), t.Frame),
                ArcFace(ButtonRadius, true, Alpha(Color.Lerp(t.ChipOn, Color.white, 0.06f), 1f), Alpha(Color.Lerp(t.ChipOn, Color.white, 0.06f), 1f), t.Title),
                ArcFace(ButtonRadius, true, Alpha(t.ChipOn, 1f), Alpha(t.ChipOn, 1f), t.Frame),
                t.Title, ChipSize);
            Nav = new GUIStyle(Chip) { fontSize = SmallSize };
            Nav.normal.textColor = Nav.hover.textColor = Nav.active.textColor = Nav.focused.textColor = t.Text;
            NavActive = new GUIStyle(ChipActive) { fontSize = SmallSize };

            // The name search box (A-084): the field with a dark-gold line, gold on hover, turquoise while typing.
            SearchField = new GUIStyle(GUI.skin.textField)
            {
                font = BodyFont,
                fontSize = SmallSize,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(38, 32, 2, 6),
                border = new RectOffset(ButtonBorder, ButtonBorder, ButtonBorder, ButtonBorder),
                wordWrap = false,
                clipping = TextClipping.Clip,
            };
            SearchField.normal.background = ArcFace(ButtonRadius, true, Alpha(t.Field, 1f), Alpha(t.Field, 1f), t.FrameDark);
            SearchField.hover.background = ArcFace(ButtonRadius, true, Alpha(t.Field, 1f), Alpha(t.Field, 1f), t.Frame);
            SearchField.focused.background = SearchField.active.background = ArcFace(ButtonRadius, true, Alpha(t.Field, 1f), Alpha(t.Field, 1f), t.Action);
            SearchField.normal.textColor = SearchField.hover.textColor = SearchField.focused.textColor = SearchField.active.textColor = t.Text;

            // Text sizes on the 1080-tall canvas (Art Bible 7.1): Press Start 2P at 16, 24 and 32; VT323 body 28,
            // small 24, chips and tags 22 (the absolute minimum).
            Title = Label(TitleFont, TitleSize, t.Title);
            Heading = Label(BodyFont, HeadingSize, Text);
            Body = Label(BodyFont, BodySize, Text);
            BodyBold = Label(BodyBoldFont, BodySize, Text);
            Small = Label(BodyFont, SmallSize, Text);
            ChipText = Label(BodyBoldFont, ChipSize, Text);
            ChipText.alignment = TextAnchor.MiddleCenter;
            ChipText.wordWrap = false;
            SmallMuted = Label(BodyFont, SmallSize, Muted);
            SmallBold = Label(BodyBoldFont, SmallSize, Text);
            SmallRight = new GUIStyle(Small) { alignment = TextAnchor.UpperRight };
            SmallMutedRight = new GUIStyle(SmallMuted) { alignment = TextAnchor.UpperRight };
            SmallGold = Label(BodyBoldFont, SmallSize, Gold);
            SmallGoldRight = new GUIStyle(SmallGold) { alignment = TextAnchor.UpperRight };
            Number = Label(TitleFont, NumberSize, Gold);
            NumberRight = new GUIStyle(Number) { alignment = TextAnchor.UpperRight };
            Center = Label(BodyFont, BodySize, Text);
            Center.alignment = TextAnchor.MiddleCenter;
            CenterBold = Label(BodyBoldFont, BodySize, Text);
            CenterBold.alignment = TextAnchor.MiddleCenter;
            SmallMutedCenter = new GUIStyle(SmallMuted) { alignment = TextAnchor.MiddleCenter };
            Display = Label(TitleBoldFont, DisplaySize, Text);
            Display.wordWrap = false;
            DisplaySub = Label(BodyBoldFont, BodySize, Text);
            PillText = Label(BodyBoldFont, ChipSize, Text);
            PillText.alignment = TextAnchor.MiddleCenter;
            PillText.wordWrap = false;

            Badge = Label(BodyBoldFont, BadgeSize, DarkInk);
            Badge.alignment = TextAnchor.MiddleCenter;
            Badge.wordWrap = false;
            Badge.normal.background = ArcFace(3, true, Color.white, Color.white);
            Badge.border = new RectOffset(ArcFaceBorder(3, 0), ArcFaceBorder(3, 0), ArcFaceBorder(3, 0), ArcFaceBorder(3, 0));
            Badge.padding = new RectOffset(6, 6, Badge.padding.top, 1);
        }

        // ------------------------------------------------------------------ fonts and sizes: the one place to change them
        // (A-190). To swap the typefaces, put the new files in Resources/Fontes, change the two names below, the sizes under
        // them and the two metrics of the text font (TextLiftEm, AccentTwinScale); every window takes its fonts and sizes
        // from here. Text sizes are on the 1080-tall virtual canvas (Art Bible 7.1, A-184): on a 600 px tall window 22 px
        // becomes about 12 px, so nothing that carries information goes below these.

        /// <summary>The title font (Resources path, no extension): Press Start 2P today.</summary>
        public const string TitleFontFile = "Fontes/PressStart2P-Regular";

        /// <summary>The text font (Resources path, no extension): VT323 today (one weight; "bold" uses the same file).</summary>
        public const string TextFontFile = "Fontes/VT323-Regular";

        /// <summary>How far a line of the text font is lifted in its rect, in em (VT323: 0,12; see <see cref="TextLift"/>).</summary>
        private const float TextLiftEm = 0.12f;

        /// <summary>The text-font size that matches the capital height of the title font at 1 (VT323 vs Press Start 2P: 1,75).</summary>
        private const float AccentTwinScale = 1.75f;

        /// <summary>Small labels and secondary text in VT323 ("pequeno"). One line of it needs a <see cref="SmallLine"/> rect.</summary>
        public const int SmallSize = 24;

        /// <summary>Chips, pills and the smallest centred tags in VT323 (the Art Bible's absolute minimum).</summary>
        public const int ChipSize = 22;

        /// <summary>Solid uppercase tags ("NOVA ESPÉCIE", "EXCEPCIONAL"): dark text on a bright colour, in VT323.</summary>
        public const int BadgeSize = 22;

        /// <summary>Body text, names and long texts (Missões, Mercado, descrições) in VT323 ("corpo").</summary>
        public const int BodySize = 28;

        /// <summary>Section headings inside a window, in VT323.</summary>
        public const int HeadingSize = 32;

        /// <summary>Window titles and big names, in Press Start 2P.</summary>
        public const int TitleSize = 24;

        /// <summary>Gold numbers (Moedas, prices), in Press Start 2P (its minimum).</summary>
        public const int NumberSize = 16;

        /// <summary>Big numbers (Honra, damage), in Press Start 2P.</summary>
        public const int NumberBigSize = 24;

        /// <summary>Celebration banners and big headline numbers, in Press Start 2P.</summary>
        public const int DisplaySize = 32;

        /// <summary>The map name of the arrival title, in the title font.</summary>
        public const int ArrivalSize = 40;

        /// <summary>The Arena's big Honor number, in the title font.</summary>
        public const int HonorSize = 30;

        /// <summary>The giant "VS" of the battle intro, in the title font.</summary>
        public const int VsSize = 96;

        /// <summary>The smallest size a squeezed label may shrink to (the Arena's row letters).</summary>
        public const int ShrinkMinSize = 16;

        /// <summary>Height of the virtual canvas the interface is laid out on.</summary>
        public const float CanvasHeight = 1080f;

        /// <summary>The tallest canvas a snapped scale may give (beyond it the interface would shrink too much).</summary>
        private const float CanvasHeightMax = 1280f;

        /// <summary>
        /// The scale of the interface on a screen <paramref name="screenHeight"/> pixels tall (A-190). The canvas is drawn
        /// through GUI.matrix, and a pixel font that was rasterised at its size and then scaled by a fraction is resampled
        /// and blurs. So the scale snaps to a half step (0,5 · 1 · 1,5 · 2 …), the largest that keeps the canvas at least
        /// 1080 tall, as long as the canvas stays under 1280 (600, 1080 to 1280 and 2160 px tall windows get a clean scale);
        /// otherwise it falls back to the exact fraction, as before, so the layout never shrinks more than that.
        /// </summary>
        public static float CanvasScale(float screenHeight)
        {
            var exact = Mathf.Max(1f, screenHeight) / CanvasHeight;
            var snapped = Mathf.Floor(exact * 2f) / 2f;
            if (snapped >= 0.5f && screenHeight / snapped <= CanvasHeightMax)
            {
                return snapped;
            }

            return exact;
        }

        /// <summary>Height of a rect holding one line of <see cref="SmallSize"/> text without clipping its descenders.</summary>
        public const float SmallLine = 24f;

        /// <summary>Corner radius of the button and chip pills, in art pixels, and the border of their 9-slice.</summary>
        private const int ButtonRadius = 5;

        private static int ButtonBorder => ArcFaceBorder(ButtonRadius, 1);

        private static Color Alpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        private GUIStyle ButtonStyle(Texture2D normal, Texture2D hover, Texture2D active, Color text, int fontSize = SmallSize)
        {
            var border = ButtonBorder;
            var style = new GUIStyle(GUI.skin.button)
            {
                border = new RectOffset(border, border, border, border),
                // VT323 sits low in its line: two more pixels under it than over it centre the letters on the pill.
                padding = new RectOffset(14, 14, 4, 8),
                fontSize = fontSize,
                fontStyle = FontStyle.Normal,
                font = BodyBoldFont,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                clipping = TextClipping.Clip,
            };
            style.normal.background = normal;
            style.hover.background = hover;
            style.active.background = active;
            style.focused.background = normal;
            style.onNormal.background = normal;
            style.normal.textColor = text;
            style.hover.textColor = text;
            style.active.textColor = text;
            style.focused.textColor = text;
            return style;
        }

        private GUIStyle Label(Font font, int size, Color color)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = size,
                fontStyle = font != null ? FontStyle.Normal : FontStyle.Bold,
                wordWrap = true,
                richText = false,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
            };
            if (font == null && size < 17)
            {
                style.fontStyle = FontStyle.Normal;
            }

            if (font != null && font == PixelTextFont)
            {
                style.padding = new RectOffset(0, 0, -TextLift(size), 0);
            }

            style.normal.textColor = color;
            return style;
        }

        // ------------------------------------------------------------------ shared pieces

        /// <summary>A horizontal progress bar (turquoise, or gold for rewards).</summary>
        public void Bar(Rect rect, float fraction, bool gold = false)
        {
            Bar(rect, fraction, gold ? Gold : Accent);
        }

        /// <summary>
        /// A horizontal progress bar in any accent colour (e.g. the rarity colour on a fish card): the ARC bar, square and
        /// filled in pixel blocks (Art Bible 7).
        /// </summary>
        public void Bar(Rect rect, float fraction, Color color)
        {
            ArcBar(rect, fraction, color, new Color(1f, 1f, 1f, 0.22f));
        }

        /// <summary>
        /// A small solid tag like "NOVA ESPÉCIE": dark text on a pixel pill of the colour.
        /// </summary>
        public void Tag(Rect rect, string text, Color color)
        {
            PixelPill(rect, color, Color.clear);
            var previous = GUI.contentColor;
            GUI.contentColor = Color.white;
            GUI.Label(rect, text, BadgeText);
            GUI.contentColor = previous;
        }

        private GUIStyle _badgeText;

        /// <summary>The text of <see cref="Badge"/> with no background (the pill is drawn by <see cref="PixelPill"/>).</summary>
        private GUIStyle BadgeText
        {
            get
            {
                if (_badgeText == null)
                {
                    _badgeText = new GUIStyle(Badge);
                    _badgeText.normal.background = null;
                }

                return _badgeText;
            }
        }

        /// <summary>An outlined pill in an accent colour, e.g. the rarity seal "RARO", with an optional icon.</summary>
        public void AccentPill(Rect rect, string text, Color color, string icon = null)
        {
            // A pixel pill (Art Bible 7): the colour as a dark tint with a one-pixel line in it.
            var tint = Color.Lerp(Theme.Field, color, 0.22f);
            PixelPill(rect, new Color(tint.r, tint.g, tint.b, 0.95f), new Color(color.r, color.g, color.b, 0.95f));

            var textRect = rect;
            if (icon != null)
            {
                var s = rect.height - 8f;
                DrawIcon(new Rect(rect.x + 7, rect.y + 4, s, s), icon, Color.Lerp(color, Color.white, 0.2f));
                textRect = new Rect(rect.x + s + 6, rect.y, rect.width - s - 8, rect.height);
            }

            var prevContent = GUI.contentColor;
            GUI.contentColor = Color.Lerp(color, Color.white, 0.45f);
            GUI.Label(textRect, text, PillText);
            GUI.contentColor = prevContent;
        }

        /// <summary>
        /// The rarity seal (e.g. "LENDÁRIO"): a pill in the rarity colour with a star; Lendário and Mítico
        /// also get a soft glow, so the rare ones stand out at a glance.
        /// </summary>
        public void RarityPill(Rect rect, string rarityId, string text, bool icon = true)
        {
            var color = RarityColor(rarityId);
            if (rarityId == "legendary" || rarityId == "mythic")
            {
                // A one-pixel ring that breathes slowly: the pixel "brilho" of the rare ones (Art Bible 4.10).
                PixelGlow(rect, color, (rarityId == "mythic" ? 0.55f : 0.4f) * (0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 2.5f)));
            }

            AccentPill(rect, text, color, icon ? Icons.Star : null);
        }

        /// <summary>
        /// A filter chip in a category colour (rarity or size): a coloured dot when idle, a tinted fill
        /// and outline when chosen. Returns true when clicked.
        /// </summary>
        public bool ColorChip(Rect rect, string text, Color color, bool active)
        {
            var clicked = GUI.Button(rect, GUIContent.none, Chip);
            var hovered = GUI.enabled && rect.Contains(Event.current.mousePosition);
            if (active)
            {
                PixelPill(rect, new Color(Theme.ChipOn.r, Theme.ChipOn.g, Theme.ChipOn.b, 1f), new Color(color.r, color.g, color.b, 0.95f));
            }
            else if (hovered)
            {
                PixelPill(rect, Color.clear, new Color(color.r, color.g, color.b, 0.55f));
            }

            // A square pixel dot (3 × 3 art pixels) in the category colour.
            var dot = 3f * ArcPixel;
            var x = rect.x + 12f;
            GUI.DrawTexture(Snap(new Rect(x, rect.y + (rect.height - dot) / 2f, dot, dot)), White, ScaleMode.StretchToFill, true, 0, color, 0, 0);

            var previous = GUI.contentColor;
            GUI.contentColor = active ? Readable(Color.Lerp(color, Color.white, 0.55f)) : hovered ? Text : Muted;
            GUI.Label(new Rect(x + dot + 2f, rect.y, rect.width - dot - 16f, rect.height), text, ChipText);
            GUI.contentColor = previous;
            return clicked;
        }

        /// <summary>
        /// A "48,6 cm · Grande · Nv. 3" line where the size category is written in its own colour
        /// (addendum A-079), wherever it sits in the line; the rest keeps <paramref name="style"/>.
        /// </summary>
        public void SizeLine(Rect rect, string line, string sizeName, string sizeId, GUIStyle style)
        {
            const string sep = " · ";
            if (string.IsNullOrEmpty(line) || string.IsNullOrEmpty(sizeName) || string.IsNullOrEmpty(sizeId))
            {
                GUI.Label(rect, line, style);
                return;
            }

            var parts = line.Split(new[] { sep }, System.StringSplitOptions.None);
            if (System.Array.IndexOf(parts, sizeName) < 0)
            {
                GUI.Label(rect, line, style);
                return;
            }

            // Draw piece by piece; each label's text starts where the previous one ended.
            var x = rect.x + style.padding.left;
            var bold = SizeBold(style);
            var previous = GUI.contentColor;
            for (var i = 0; i < parts.Length; i++)
            {
                var text = i < parts.Length - 1 ? parts[i] + sep : parts[i];
                var isSize = parts[i] == sizeName;
                var piece = isSize ? parts[i] : text;
                var s = isSize ? bold : style;
                var width = s.CalcSize(new GUIContent(piece)).x - s.padding.horizontal;
                if (isSize)
                {
                    GUI.contentColor = Color.Lerp(SizeColor(sizeId), Color.white, 0.15f);
                }

                GUI.Label(new Rect(x - s.padding.left, rect.y, Mathf.Max(0f, rect.xMax - x + s.padding.left), rect.height), piece, s);
                GUI.contentColor = previous;
                x += width;
                if (isSize && i < parts.Length - 1)
                {
                    var tail = style.CalcSize(new GUIContent(sep)).x - style.padding.horizontal;
                    GUI.Label(new Rect(x - style.padding.left, rect.y, Mathf.Max(0f, rect.xMax - x + style.padding.left), rect.height), sep, style);
                    x += tail;
                }

                if (x >= rect.xMax)
                {
                    break;
                }
            }
        }

        private readonly System.Collections.Generic.Dictionary<GUIStyle, GUIStyle> _sizeBold = new System.Collections.Generic.Dictionary<GUIStyle, GUIStyle>();

        /// <summary>The bold twin of a label style, made once and kept (IMGUI draws every frame).</summary>
        private GUIStyle SizeBold(GUIStyle style)
        {
            if (!_sizeBold.TryGetValue(style, out var bold))
            {
                // Light text tinted by the size colour (GUI.contentColor multiplies it), in both modes.
                bold = new GUIStyle(style) { font = BodyBoldFont, wordWrap = false };
                bold.normal.textColor = Theme.Text;
                _sizeBold[style] = bold;
            }

            return bold;
        }

        /// <summary>Width a pill needs for its text (and icon).</summary>
        public float PillWidth(string text, bool icon)
        {
            return PillText.CalcSize(new GUIContent(text)).x + (icon ? 30f : 18f);
        }

        /// <summary>A coloured one-pixel outline around a rect (rarity accent, selection), with the small chamfer of the cards.</summary>
        public void DrawOutline(Rect rect, Color color)
        {
            Shape(rect, 1, false, Color.clear, 1, color, Color.clear, Color.clear);
        }

        /// <summary>
        /// The glow around a rect, in pixel art (Art Bible 4.10: no diffuse glow): a one-pixel ring just outside it;
        /// <paramref name="strength"/> 0–1 sets how strong it shows.
        /// </summary>
        public void DrawGlow(Rect rect, Color color, float strength)
        {
            PixelGlow(rect, color, Mathf.Clamp01(strength * 1.6f));
        }

        /// <summary>The hard pixel shadow under a floating panel.</summary>
        public void DrawShadow(Rect rect)
        {
            HardShadow(rect);
        }

        /// <summary>A panel with its shadow.</summary>
        public void FloatingPanel(Rect rect)
        {
            HardShadow(rect);
            GUI.Box(rect, GUIContent.none, Panel);
        }

        /// <summary>Draws an icon tinted with a colour. Missing icon files draw nothing (same as <see cref="DrawIconExact"/>).</summary>
        public void DrawIcon(Rect rect, string icon, Color color)
        {
            DrawIconExact(rect, icon, color);
        }

        /// <summary>Draws an icon tinted with exactly <paramref name="color"/> (white on a coloured face).</summary>
        public void DrawIconExact(Rect rect, string icon, Color color)
        {
            var tex = icon != null ? ArtAssets.Icon(icon) : null;
            if (tex == null)
            {
                return;
            }

            // Pixel icons at the largest whole scale, coloured ones never tinted (PixelDraw, M25-T11 hook).
            PixelDraw.Icon(rect, tex, icon, color);
        }

        /// <summary>An icon inside a small rounded tile, like the window headers.</summary>
        public void IconBadge(Rect rect, string icon, Color color)
        {
            GUI.Box(rect, GUIContent.none, IconTile);
            var pad = rect.width * 0.22f;
            DrawIcon(new Rect(rect.x + pad, rect.y + pad, rect.width - pad * 2, rect.height - pad * 2), icon, color);
        }

        /// <summary>A button with an icon before its label. Returns true when clicked.</summary>
        public bool IconButton(Rect rect, string icon, string label, GUIStyle style)
        {
            var clicked = GUI.Button(rect, GUIContent.none, style);
            var hover = GUI.enabled && rect.Contains(Event.current.mousePosition);
            var color = hover ? style.hover.textColor : style.normal.textColor;
            if (!GUI.enabled)
            {
                color.a *= 0.5f;
            }

            var content = new GUIContent(label ?? string.Empty);
            var labelStyle = LabelOf(style);
            var textWidth = string.IsNullOrEmpty(label) ? 0f : labelStyle.CalcSize(content).x;
            var iconSize = Mathf.Min(rect.height - 16f, 22f);
            var gap = textWidth > 0f ? 8f : 0f;
            var total = Mathf.Min(rect.width - 16f, iconSize + gap + textWidth);
            var x = rect.center.x - total / 2f;
            DrawIconExact(new Rect(x, rect.center.y - iconSize / 2f, iconSize, iconSize), icon, color);
            if (textWidth > 0f)
            {
                var previous = GUI.contentColor;
                GUI.contentColor = color;
                GUI.Label(new Rect(x + iconSize + gap, rect.y, rect.xMax - (x + iconSize + gap) - 6f, rect.height), content, labelStyle);
                GUI.contentColor = previous;
            }

            return clicked;
        }

        private readonly System.Collections.Generic.Dictionary<GUIStyle, GUIStyle> _labelOf = new System.Collections.Generic.Dictionary<GUIStyle, GUIStyle>();

        private GUIStyle LabelOf(GUIStyle button)
        {
            if (!_labelOf.TryGetValue(button, out var label))
            {
                label = new GUIStyle(GUI.skin.label)
                {
                    font = button.font,
                    fontSize = button.fontSize,
                    fontStyle = button.fontStyle,
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = false,
                    clipping = TextClipping.Clip,
                    padding = new RectOffset(0, 0, 0, 0),
                };
                label.normal.textColor = Color.white;
                _labelOf[button] = label;
            }

            return label;
        }

        /// <summary>A coin symbol.</summary>
        public void CoinIcon(Rect rect)
        {
            if (Coin != null && Coin != Art.Circle.texture)
            {
                PixelDraw.Fit(rect, Coin);
                return;
            }

            var previous = GUI.color;
            GUI.color = Gold;
            GUI.DrawTexture(rect, Art.Circle.texture, ScaleMode.ScaleToFit, true);
            GUI.color = new Color(0.75f, 0.52f, 0.12f);
            var inner = new Rect(rect.x + rect.width * 0.3f, rect.y + rect.height * 0.3f, rect.width * 0.4f, rect.height * 0.4f);
            GUI.DrawTexture(inner, Art.Circle.texture, ScaleMode.ScaleToFit, true);
            GUI.color = previous;
        }

        /// <summary>A coin followed by an amount in gold.</summary>
        public void CoinAmount(Rect rect, string amount, GUIStyle style = null)
        {
            style = style ?? SmallGold;
            var s = Mathf.Min(rect.height, 22f);
            CoinIcon(new Rect(rect.x, rect.y + (rect.height - s) / 2f, s, s));
            // Never narrower than the number itself: a tight label wraps its last digit out of sight.
            var width = Mathf.Max(rect.width - s - 6f, style.CalcSize(new GUIContent(amount)).x + 4f);
            GUI.Label(new Rect(rect.x + s + 6f, rect.y, width, rect.height), amount, style);
        }

        /// <summary>The width <see cref="CoinAmount"/> needs for a number at a given height.</summary>
        public float CoinAmountWidth(string amount, float height, GUIStyle style = null)
        {
            return Mathf.Min(height, 22f) + 6f + (style ?? SmallGold).CalcSize(new GUIContent(amount)).x + 4f;
        }

        /// <summary>
        /// A label with a soft 1 px dark shadow under it (A-149), only for text drawn straight over art with no
        /// panel behind it: the arrival title over the scene and the fighters' lines in the Arena battle.
        /// Text on panels, cards and pills never gets it.
        /// </summary>
        public static void ShadowLabel(Rect rect, string text, GUIStyle style)
        {
            style = _instance != null ? _instance.TitleSafe(style, text) : style;
            var previous = GUI.contentColor;
            GUI.contentColor = new Color(0f, 0f, 0f, previous.a * 0.5f);
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, style);
            GUI.contentColor = previous;
            GUI.Label(rect, text, style);
        }

        /// <summary>A thin divider line.</summary>
        public void Divider(Rect rect)
        {
            GUI.DrawTexture(Snap(new Rect(rect.x, rect.y, rect.width, ArcPixel)), White, ScaleMode.StretchToFill, true, 0, new Color(Border.r, Border.g, Border.b, 0.7f), 0, 0);
        }

        private static readonly Color[] TierColors =
        {
            new Color(0.62f, 0.67f, 0.74f), // 0 · grey (starter gear)
            new Color(0.38f, 0.80f, 0.47f), // 1 · green
            new Color(0.34f, 0.62f, 0.96f), // 2 · blue
            new Color(0.70f, 0.47f, 0.96f), // 3 · purple
            new Color(0.98f, 0.76f, 0.28f), // 4 · gold
            new Color(0.94f, 0.38f, 0.35f), // 5+ · red
        };

        /// <summary>
        /// The frame colour of a gear tier (rods, boats, baits) in the Profile's backpack: grey, green, blue,
        /// purple, gold, red. Interface only; tiers above the last colour keep the last one.
        /// </summary>
        public static Color TierColor(int tier) => TierColors[Mathf.Clamp(tier, 0, TierColors.Length - 1)];

        /// <summary>A dashed rectangular frame: the look of an empty inventory slot.</summary>
        public void DashedFrame(Rect rect, Color color, float dash = 8f, float gap = 6f, float thickness = 1.5f)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            for (var x = rect.x; x < rect.xMax; x += dash + gap)
            {
                var w = Mathf.Min(dash, rect.xMax - x);
                GUI.DrawTexture(new Rect(x, rect.y, w, thickness), White, ScaleMode.StretchToFill, true, 0, color, 0, 0);
                GUI.DrawTexture(new Rect(x, rect.yMax - thickness, w, thickness), White, ScaleMode.StretchToFill, true, 0, color, 0, 0);
            }

            for (var y = rect.y; y < rect.yMax - thickness; y += dash + gap)
            {
                var h = Mathf.Min(dash, rect.yMax - thickness - y);
                GUI.DrawTexture(new Rect(rect.x, y, thickness, h), White, ScaleMode.StretchToFill, true, 0, color, 0, 0);
                GUI.DrawTexture(new Rect(rect.xMax - thickness, y, thickness, h), White, ScaleMode.StretchToFill, true, 0, color, 0, 0);
            }
        }

        // Single-line variants (never wrap; text that does not fit is clipped). Made on first use.
        private GUIStyle _smallMutedRightLine, _smallGoldLine;

        /// <summary><see cref="SmallMutedRight"/> on one line, never wrapping.</summary>
        public GUIStyle SmallMutedRightLine => _smallMutedRightLine ?? (_smallMutedRightLine = new GUIStyle(SmallMutedRight) { wordWrap = false, clipping = TextClipping.Clip });

        /// <summary><see cref="SmallGold"/> on one line, never wrapping.</summary>
        public GUIStyle SmallGoldLine => _smallGoldLine ?? (_smallGoldLine = new GUIStyle(SmallGold) { wordWrap = false, clipping = TextClipping.Clip });

        // Arena duel and VS intro styles. Made on first use.
        private GUIStyle _smallGoldCenter, _tinyMutedCenter, _numberBig, _titleCenter, _vsMark, _vsDisplay, _medalText;

        /// <summary><see cref="SmallGold"/> centred on one line.</summary>
        public GUIStyle SmallGoldCenter => _smallGoldCenter ?? (_smallGoldCenter = new GUIStyle(SmallGold) { alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Clip });

        /// <summary>A small bold muted line, centred (short captions such as "Volta em" on the compass). 14 px since A-149 (was 10).</summary>
        public GUIStyle TinyMutedCenter => _tinyMutedCenter ?? (_tinyMutedCenter = new GUIStyle(SmallMutedCenter) { fontSize = ChipSize, font = BodyBoldFont, wordWrap = false });

        /// <summary>A large gold number on one line (the Arena's Honor).</summary>
        public GUIStyle NumberBig => _numberBig ?? (_numberBig = new GUIStyle(Number) { fontSize = HonorSize, wordWrap = false });

        /// <summary><see cref="Title"/> centred on one line.</summary>
        public GUIStyle TitleCenter => _titleCenter ?? (_titleCenter = new GUIStyle(Title) { alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Clip });

        /// <summary>The gold "VS" between the two formations (Press Start 2P at 32: fits the 76 px disc, M25-T14).</summary>
        public GUIStyle VsMark => _vsMark ?? (_vsMark = GoldDisplay(DisplaySize));

        /// <summary>The big gold "VS" of the battle intro.</summary>
        public GUIStyle VsDisplay => _vsDisplay ?? (_vsDisplay = GoldDisplay(VsSize));

        /// <summary>Dark bold text centred on a gold medal.</summary>
        public GUIStyle MedalText
        {
            get
            {
                if (_medalText == null)
                {
                    _medalText = new GUIStyle(CenterBold) { wordWrap = false, clipping = TextClipping.Overflow };
                    _medalText.normal.textColor = new Color(0.23f, 0.15f, 0.02f);
                }

                return _medalText;
            }
        }

        private GUIStyle GoldDisplay(int size)
        {
            var style = new GUIStyle(Display) { fontSize = size, alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Overflow };
            style.normal.textColor = Gold;
            return style;
        }

        private Texture2D _diagonal;

        /// <summary>
        /// A square mask for a "/" split: opaque below the diagonal, transparent above it, with a soft edge.
        /// Stretched over a strip and tinted, it draws the slanted border of the Arena's VS screen.
        /// </summary>
        public Texture2D Diagonal
        {
            get
            {
                if (_diagonal != null)
                {
                    return _diagonal;
                }

                const int size = 128;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    hideFlags = HideFlags.HideAndDontSave,
                };
                var pixels = new Color32[size * size];
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        // Row 0 is drawn at the bottom: the opaque part is the lower-right triangle.
                        var a = Mathf.Clamp01((x - y) / 1.5f + 0.5f);
                        pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                    }
                }

                tex.SetPixels32(pixels);
                tex.Apply();
                _diagonal = tex;
                return _diagonal;
            }
        }

        // ------------------------------------------------------------------ main bar and player card (owner request 07/10/2026)

        /// <summary>Icon size of a main-menu button (the old menu used 22).</summary>
        public const float NavGameIconSize = 28f;

        /// <summary>Width of a main-menu button showing only its icon (narrow screens).</summary>
        public const float NavGameIconWidth = 60f;

        private const float NavGameGap = 8f;

        private GUIStyle _navGame, _navGameActive;

        private static readonly float[] SliceX = new float[4], SliceY = new float[4], SliceU = new float[4], SliceV = new float[4];

        /// <summary>
        /// Draws <paramref name="tex"/> as a 9-slice: the borders (texture pixels) keep their shape, scaled by
        /// <paramref name="scale"/>; the middle stretches. Respects GUI.color.
        /// </summary>
        public static void NineSlice(Rect rect, Texture2D tex, float left, float right, float top, float bottom, float scale)
        {
            if (Event.current.type != EventType.Repaint || rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            float w = tex.width, h = tex.height;
            SliceX[0] = rect.x;
            SliceX[1] = rect.x + Mathf.Min(left * scale, rect.width / 2f);
            SliceX[2] = rect.xMax - Mathf.Min(right * scale, rect.width / 2f);
            SliceX[3] = rect.xMax;
            SliceY[0] = rect.y;
            SliceY[1] = rect.y + Mathf.Min(top * scale, rect.height / 2f);
            SliceY[2] = rect.yMax - Mathf.Min(bottom * scale, rect.height / 2f);
            SliceY[3] = rect.yMax;
            SliceU[0] = 0f;
            SliceU[1] = left / w;
            SliceU[2] = 1f - right / w;
            SliceU[3] = 1f;
            // Texture v grows upwards: the top row of the rect is v = 1.
            SliceV[0] = 1f;
            SliceV[1] = 1f - top / h;
            SliceV[2] = bottom / h;
            SliceV[3] = 0f;
            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    var piece = new Rect(SliceX[i], SliceY[j], SliceX[i + 1] - SliceX[i], SliceY[j + 1] - SliceY[j]);
                    if (piece.width <= 0f || piece.height <= 0f)
                    {
                        continue;
                    }

                    var uv = new Rect(SliceU[i], SliceV[j + 1], SliceU[i + 1] - SliceU[i], SliceV[j] - SliceV[j + 1]);
                    GUI.DrawTextureWithTexCoords(piece, tex, uv, true);
                }
            }
        }

        /// <summary>The coloured pixel icon of a currency (Conchas, Dólares, Moedas), or the tinted icon when it is not pixel art.</summary>
        public void CurrencyIcon(Rect rect, string icon)
        {
            if (icon == Icons.Coin)
            {
                CoinIcon(rect);
                return;
            }

            var pixel = ArtAssets.Icon(icon);
            if (pixel != null && ArtAssets.IsPixel(pixel))
            {
                PixelDraw.Icon(rect, pixel, icon, Color.white);
                return;
            }

            DrawIcon(rect, icon, Color.white);
        }

        /// <summary>The round-ended button of the top strip (bell, options), idle: the field with the structural gold line.</summary>
        public GUIStyle NavGame => _navGame ?? (_navGame = ButtonStyle(
            ArcFace(ButtonRadius, true, Alpha(Theme.Field, 0.98f), Alpha(Theme.Field, 0.98f), Theme.Frame),
            ArcFace(ButtonRadius, true, Alpha(Theme.PanelElevated, 1f), Alpha(Theme.PanelElevated, 1f), Theme.Title),
            ArcFace(ButtonRadius, true, Alpha(Theme.Panel, 1f), Alpha(Theme.Panel, 1f), Theme.FrameDark),
            Theme.Text, SmallSize));

        /// <summary>The same button while its panel is open: the raised panel colour with the gold line, title-coloured.</summary>
        public GUIStyle NavGameActive => _navGameActive ?? (_navGameActive = ButtonStyle(
            ArcFace(ButtonRadius, true, Alpha(Theme.PanelElevated, 1f), Alpha(Theme.PanelElevated, 1f), Theme.Frame),
            ArcFace(ButtonRadius, true, Alpha(Theme.PanelElevated, 1f), Alpha(Theme.PanelElevated, 1f), Theme.Title),
            ArcFace(ButtonRadius, true, Alpha(Theme.PanelElevated, 1f), Alpha(Theme.PanelElevated, 1f), Theme.Frame),
            Theme.Title, SmallSize));

        /// <summary>
        /// A button of the top strip in the ARC skin (A-184): a dark pill with the thin gold line, the raised panel colour
        /// and title-coloured icon while its panel is open, and the icon left of the label (or alone when
        /// <paramref name="label"/> is null). Returns true when clicked.
        /// </summary>
        public bool NavGameButton(Rect rect, string icon, string label, bool active)
        {
            var enabled = GUI.enabled;
            var hover = enabled && rect.Contains(Event.current.mousePosition);
            var style = active ? NavGameActive : NavGame;
            var clicked = GUI.Button(rect, GUIContent.none, style);
            var color = active ? Theme.Title : hover ? Color.white : Theme.Text;
            if (!enabled)
            {
                color.a *= 0.5f;
            }

            var content = new GUIContent(label ?? string.Empty);
            var labelStyle = LabelOf(style);
            var textWidth = string.IsNullOrEmpty(label) ? 0f : labelStyle.CalcSize(content).x;
            var iconSize = Mathf.Min(rect.height - 20f, NavGameIconSize);
            var gap = textWidth > 0f ? NavGameGap : 0f;
            var total = Mathf.Min(rect.width - 12f, iconSize + gap + textWidth);
            var x = rect.center.x - total / 2f;
            var centreY = rect.center.y;
            DrawIcon(Snap(new Rect(x, centreY - iconSize / 2f, iconSize, iconSize)), icon, color);
            if (textWidth > 0f)
            {
                var previous = GUI.contentColor;
                GUI.contentColor = color;
                GUI.Label(new Rect(x + iconSize + gap, centreY - rect.height / 2f, rect.xMax - (x + iconSize + gap) - 4f, rect.height), content, labelStyle);
                GUI.contentColor = previous;
            }

            return clicked;
        }

        /// <summary>
        /// The main bar background in the ARC skin: the panel night across the top, closed at the bottom by the frame
        /// lines (gold, a dark line, dark gold), each one art pixel tall.
        /// </summary>
        public void TopBarBackground(Rect bar)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            bar = Snap(bar);
            GUI.DrawTexture(bar, White, ScaleMode.StretchToFill, true, 0, Alpha(Theme.Panel, Theme.PanelOpacity), 0, 0);
            var y = bar.yMax - ArcPixel * 3;
            GUI.DrawTexture(new Rect(bar.x, y, bar.width, ArcPixel), White, ScaleMode.StretchToFill, true, 0, Theme.FrameDark, 0, 0);
            GUI.DrawTexture(new Rect(bar.x, y + ArcPixel, bar.width, ArcPixel), White, ScaleMode.StretchToFill, true, 0, Theme.FrameInner, 0, 0);
            GUI.DrawTexture(new Rect(bar.x, y + ArcPixel * 2, bar.width, ArcPixel), White, ScaleMode.StretchToFill, true, 0, Theme.Frame, 0, 0);
        }

        /// <summary>A sunken area inside a panel (the wallet of the player card, a counter): the field with a dark-gold line.</summary>
        public void Inset(Rect rect)
        {
            LineBox(rect, Alpha(Theme.Field, 0.95f), Theme.FrameDark, 1);
        }
    }
}
