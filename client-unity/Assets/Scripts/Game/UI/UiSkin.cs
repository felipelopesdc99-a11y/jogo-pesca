using FishingIdle.Game.Scene;
using FishingIdle.Game.Visual;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// Colours, fonts, textures and IMGUI styles of the game interface ("Lago Dourado — Clean
    /// Premium", docs/ART_BIBLE_V0_1.md), built once on first use from the visual theme.
    /// </summary>
    /// <remarks>
    /// The MVP interface is drawn with Unity's immediate-mode GUI (docs/DECISOES.md, TD-017). All
    /// look-and-feel lives here and in <see cref="VisualTheme"/>: turquoise for actions, gold only
    /// for rewards, rarity colour only as an accent. Windows use the shared pieces below (panels,
    /// buttons with icons, pills, bars, outlines) so every screen speaks the same language.
    /// </remarks>
    public sealed class UiSkin
    {
        private static VisualTheme Theme => VisualTheme.Current;

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
        public static Color Rare => Theme.Rarity("rare");

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
            TitleFont = Resources.Load<Font>("Fontes/Fredoka-SemiBold");
            TitleBoldFont = Resources.Load<Font>("Fontes/Fredoka-Bold") ?? TitleFont;
            BodyFont = Resources.Load<Font>("Fontes/Nunito-Regular");
            BodyBoldFont = Resources.Load<Font>("Fontes/Nunito-Bold") ?? BodyFont;

            White = Art.SolidTexture(Color.white);
            Overlay = Art.SolidTexture(new Color(t.Night.r, t.Night.g, t.Night.b, t.OverlayOpacity));
            Coin = ArtAssets.Icon(Icons.Coin) ?? Art.Circle.texture;
            Rays = Art.RaysTexture(14);

            var panelTop = Alpha(Color.Lerp(t.Panel, t.PanelElevated, 0.45f), t.PanelOpacity);
            var panelBottom = Alpha(t.Panel, t.PanelOpacity);
            Panel = Box(Art.PanelTexture(panelTop, panelBottom, Alpha(t.Border, 0.9f), 16, 1.5f, 96), 16);
            PanelSolid = Box(Art.PanelTexture(Alpha(t.PanelElevated, 1f), Alpha(t.Panel, 1f), t.Border, 16, 1.5f, 96), 16);
            Card = Box(Art.PanelTexture(Alpha(Color.Lerp(t.PanelElevated, Color.white, 0.03f), 0.97f), Alpha(t.PanelElevated, 0.97f), Alpha(t.Border, 0.85f), 12, 1.5f, 64, 0.04f), 12);
            CardHovered = Box(Art.PanelTexture(Alpha(Color.Lerp(t.PanelElevated, Color.white, 0.08f), 0.98f), Alpha(Color.Lerp(t.PanelElevated, Color.white, 0.04f), 0.98f), Alpha(Color.Lerp(t.Border, Color.white, 0.25f), 1f), 12, 1.5f, 64, 0.05f), 12);
            CardSelected = Box(Art.PanelTexture(Alpha(Color.Lerp(t.PanelElevated, t.Action, 0.22f), 0.98f), Alpha(Color.Lerp(t.Panel, t.Action, 0.14f), 0.98f), t.Action, 12, 2.5f, 64, 0.05f), 12);
            CardImportant = Box(Art.PanelTexture(Alpha(Color.Lerp(t.PanelElevated, t.Reward, 0.10f), 0.98f), Alpha(t.Panel, 0.98f), t.Reward, 12, 2f, 64, 0.05f), 12);

            Shadow = Box(Art.SoftTexture(16, 18, 0.55f), 34);
            Glow = Box(Art.SoftTexture(14, 22, 0.9f), 36);
            Outline = Box(Art.OutlineTexture(12, 2f), 12);
            Pill = Box(Art.OutlineTexture(10, 1.5f, 0.20f), 10);
            Pill.padding = new RectOffset(8, 8, 2, 2);
            IconTile = Box(Art.PanelTexture(Alpha(Color.Lerp(t.PanelElevated, Color.white, 0.06f), 1f), Alpha(t.PanelElevated, 1f), Alpha(t.Border, 1f), 12, 1.5f, 48), 12);

            Button = ButtonStyle(
                Art.PanelTexture(Alpha(Color.Lerp(t.PanelElevated, Color.white, 0.06f), 0.98f), Alpha(t.PanelElevated, 0.98f), t.Border, 10, 1.5f, 48, 0.05f),
                Art.PanelTexture(Alpha(Color.Lerp(t.PanelElevated, Color.white, 0.14f), 1f), Alpha(Color.Lerp(t.PanelElevated, Color.white, 0.06f), 1f), Color.Lerp(t.Border, Color.white, 0.3f), 10, 1.5f, 48, 0.06f),
                Art.PanelTexture(Alpha(t.Panel, 1f), Alpha(t.Panel, 1f), t.Border, 10, 1.5f, 48, 0f),
                t.Text);
            ButtonPrimary = ButtonStyle(
                Art.PanelTexture(Alpha(t.ActionHover, 1f), Alpha(t.Action * 0.92f, 1f), Alpha(Color.Lerp(t.Action, Color.white, 0.25f), 1f), 10, 1f, 48, 0.1f),
                Art.PanelTexture(Alpha(Color.Lerp(t.ActionHover, Color.white, 0.18f), 1f), Alpha(t.ActionHover, 1f), Alpha(Color.Lerp(t.Action, Color.white, 0.45f), 1f), 10, 1f, 48, 0.12f),
                Art.PanelTexture(Alpha(t.Action * 0.85f, 1f), Alpha(t.Action * 0.75f, 1f), Alpha(t.Action, 1f), 10, 1f, 48, 0f),
                Color.white);
            ButtonDanger = ButtonStyle(
                Art.PanelTexture(Alpha(Color.Lerp(t.Danger, Color.white, 0.08f), 1f), Alpha(t.Danger * 0.9f, 1f), Alpha(Color.Lerp(t.Danger, Color.white, 0.2f), 1f), 10, 1f, 48, 0.08f),
                Art.PanelTexture(Alpha(Color.Lerp(t.Danger, Color.white, 0.2f), 1f), Alpha(t.Danger, 1f), Alpha(Color.Lerp(t.Danger, Color.white, 0.4f), 1f), 10, 1f, 48, 0.1f),
                Art.PanelTexture(Alpha(t.Danger * 0.8f, 1f), Alpha(t.Danger * 0.7f, 1f), Alpha(t.Danger, 1f), 10, 1f, 48, 0f),
                Color.white);
            var ink = new Color(0.23f, 0.15f, 0.02f);
            ButtonReward = ButtonStyle(
                Art.PanelTexture(Alpha(t.RewardLight, 1f), Alpha(t.Reward, 1f), Alpha(Color.Lerp(t.Reward, Color.white, 0.4f), 1f), 10, 1f, 48, 0.12f),
                Art.PanelTexture(Alpha(Color.Lerp(t.RewardLight, Color.white, 0.25f), 1f), Alpha(Color.Lerp(t.Reward, Color.white, 0.12f), 1f), Alpha(Color.white, 1f), 10, 1f, 48, 0.15f),
                Art.PanelTexture(Alpha(t.Reward * 0.9f, 1f), Alpha(t.Reward * 0.8f, 1f), Alpha(t.Reward, 1f), 10, 1f, 48, 0f),
                ink);

            Chip = ButtonStyle(
                Art.PanelTexture(Alpha(t.PanelElevated, 0.85f), Alpha(t.Panel, 0.85f), Alpha(t.Border, 0.9f), 10, 1.2f, 40, 0.04f),
                Art.PanelTexture(Alpha(Color.Lerp(t.PanelElevated, Color.white, 0.1f), 0.95f), Alpha(t.PanelElevated, 0.95f), Color.Lerp(t.Border, Color.white, 0.3f), 10, 1.2f, 40, 0.05f),
                Art.PanelTexture(Alpha(t.Panel, 1f), Alpha(t.Panel, 1f), t.Border, 10, 1.2f, 40, 0f),
                t.TextMuted, 13);
            ChipActive = ButtonStyle(ButtonPrimary.normal.background, ButtonPrimary.hover.background, ButtonPrimary.active.background, Color.white, 13);
            Nav = new GUIStyle(Chip) { fontSize = 14 };
            Nav.normal.textColor = Nav.hover.textColor = Nav.active.textColor = Nav.focused.textColor = t.Text;
            NavActive = new GUIStyle(ChipActive) { fontSize = 14 };

            // The name search box (A-084): opaque, a touch lighter than the panel and with an accent
            // border, so it reads as a place to type and not as one more chip.
            var searchFill = Alpha(Color.Lerp(t.PanelElevated, Color.white, 0.14f), 1f);
            SearchField = new GUIStyle(GUI.skin.textField)
            {
                font = BodyFont,
                fontSize = 15,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(38, 32, 4, 4),
                border = new RectOffset(10, 10, 10, 10),
                wordWrap = false,
                clipping = TextClipping.Clip,
            };
            SearchField.normal.background = Art.PanelTexture(searchFill, searchFill, Alpha(t.Action, 0.7f), 10, 2f, 40, 0f);
            SearchField.hover.background = Art.PanelTexture(searchFill, searchFill, Alpha(t.Action, 0.95f), 10, 2f, 40, 0f);
            SearchField.focused.background = SearchField.active.background = Art.PanelTexture(searchFill, searchFill, Alpha(t.ActionHover, 1f), 10, 2.5f, 40, 0f);
            SearchField.normal.textColor = SearchField.hover.textColor = SearchField.focused.textColor = SearchField.active.textColor = t.Text;

            Title = Label(TitleFont, 27, Text);
            Heading = Label(TitleFont, 20, Text);
            Body = Label(BodyFont, 16, Text);
            BodyBold = Label(BodyBoldFont, 16, Text);
            Small = Label(BodyFont, 13, Text);
            ChipText = Label(BodyBoldFont, 13, Text);
            ChipText.alignment = TextAnchor.MiddleCenter;
            ChipText.wordWrap = false;
            SmallMuted = Label(BodyFont, 13, Muted);
            SmallBold = Label(BodyBoldFont, 13, Text);
            SmallRight = new GUIStyle(Small) { alignment = TextAnchor.UpperRight };
            SmallMutedRight = new GUIStyle(SmallMuted) { alignment = TextAnchor.UpperRight };
            SmallGold = Label(BodyBoldFont, 13, Gold);
            SmallGoldRight = new GUIStyle(SmallGold) { alignment = TextAnchor.UpperRight };
            Number = Label(TitleFont, 21, Gold);
            NumberRight = new GUIStyle(Number) { alignment = TextAnchor.UpperRight };
            Center = Label(BodyFont, 16, Text);
            Center.alignment = TextAnchor.MiddleCenter;
            CenterBold = Label(BodyBoldFont, 17, Text);
            CenterBold.alignment = TextAnchor.MiddleCenter;
            SmallMutedCenter = new GUIStyle(SmallMuted) { alignment = TextAnchor.MiddleCenter };
            Display = Label(TitleBoldFont, 44, Text);
            Display.wordWrap = false;
            DisplaySub = Label(BodyBoldFont, 18, Text);
            PillText = Label(BodyBoldFont, 12, Text);
            PillText.alignment = TextAnchor.MiddleCenter;
            PillText.wordWrap = false;

            Badge = Label(BodyBoldFont, 11, new Color(0.06f, 0.08f, 0.12f));
            Badge.alignment = TextAnchor.MiddleCenter;
            Badge.wordWrap = false;
            Badge.normal.background = Art.RoundedRectTexture(Color.white, Color.clear, 7, 0f);
            Badge.border = new RectOffset(7, 7, 7, 7);
            Badge.padding = new RectOffset(6, 6, 2, 2);
        }

        private static Color Alpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        private static GUIStyle Box(Texture2D texture, int border)
        {
            var style = new GUIStyle(GUI.skin.box)
            {
                border = new RectOffset(border, border, border, border),
                padding = new RectOffset(16, 16, 14, 14),
                margin = new RectOffset(0, 0, 0, 0),
            };
            style.normal.background = texture;
            style.normal.textColor = Text;
            return style;
        }

        private GUIStyle ButtonStyle(Texture2D normal, Texture2D hover, Texture2D active, Color text, int fontSize = 15)
        {
            const int radius = 10;
            var style = new GUIStyle(GUI.skin.button)
            {
                border = new RectOffset(radius, radius, radius, radius),
                padding = new RectOffset(14, 14, 7, 7),
                fontSize = fontSize,
                fontStyle = BodyBoldFont != null ? FontStyle.Normal : FontStyle.Bold,
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

        private static GUIStyle Label(Font font, int size, Color color)
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

            style.normal.textColor = color;
            return style;
        }

        // ------------------------------------------------------------------ shared pieces

        /// <summary>A horizontal progress bar (turquoise, or gold for rewards).</summary>
        public void Bar(Rect rect, float fraction, bool gold = false)
        {
            Bar(rect, fraction, gold ? Gold : Accent);
        }

        /// <summary>A horizontal progress bar in any accent colour (e.g. the rarity colour on a fish card).</summary>
        public void Bar(Rect rect, float fraction, Color color)
        {
            var radius = rect.height / 2f;
            GUI.DrawTexture(rect, White, ScaleMode.StretchToFill, true, 0, new Color(Night.r, Night.g, Night.b, 0.75f), 0, radius);
            var fill = new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fraction), rect.height);
            if (fill.width >= rect.height)
            {
                GUI.DrawTexture(fill, White, ScaleMode.StretchToFill, true, 0, color, 0, radius);
                var shine = new Rect(fill.x + 2, fill.y + 1, fill.width - 4, Mathf.Max(1f, fill.height * 0.35f));
                GUI.DrawTexture(shine, White, ScaleMode.StretchToFill, true, 0, new Color(1f, 1f, 1f, 0.18f), 0, radius);
            }
        }

        /// <summary>A small solid tag like "NOVA ESPÉCIE" (dark text on a colour).</summary>
        public void Tag(Rect rect, string text, Color color)
        {
            var previous = GUI.backgroundColor;
            GUI.backgroundColor = color;
            GUI.Label(rect, text, Badge);
            GUI.backgroundColor = previous;
        }

        /// <summary>An outlined pill in an accent colour, e.g. the rarity seal "RARO", with an optional icon.</summary>
        public void AccentPill(Rect rect, string text, Color color, string icon = null)
        {
            // Drawn with Unity's rounded rectangles instead of the 9-slice Pill box: the pills are shorter
            // than the box's borders, and the squeezed 9-slice drew a line across the text.
            var radius = rect.height / 2f;
            GUI.DrawTexture(rect, White, ScaleMode.StretchToFill, true, 0, new Color(color.r, color.g, color.b, 0.20f), 0, radius);
            GUI.DrawTexture(rect, White, ScaleMode.StretchToFill, true, 0, new Color(color.r, color.g, color.b, 0.85f), 1.5f, radius);

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
                DrawGlow(rect, color, rarityId == "mythic" ? 0.55f : 0.4f);
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
                GUI.DrawTexture(new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2), White, ScaleMode.StretchToFill, true, 0, new Color(color.r, color.g, color.b, 0.2f), 0, 9);
                DrawOutline(rect, new Color(color.r, color.g, color.b, 0.95f));
            }
            else if (hovered)
            {
                DrawOutline(rect, new Color(color.r, color.g, color.b, 0.45f));
            }

            var dot = 8f;
            var x = rect.x + 12f;
            GUI.DrawTexture(new Rect(x, rect.y + (rect.height - dot) / 2f, dot, dot), White, ScaleMode.StretchToFill, true, 0, color, 0, dot / 2f);

            var previous = GUI.contentColor;
            GUI.contentColor = active ? Color.Lerp(color, Color.white, 0.55f) : hovered ? Text : Muted;
            GUI.Label(new Rect(x + dot + 2f, rect.y, rect.width - dot - 16f, rect.height), text, ChipText);
            GUI.contentColor = previous;
            return clicked;
        }

        /// <summary>Width a <see cref="ColorChip"/> needs for its text.</summary>
        public float ColorChipWidth(string text)
        {
            return ChipText.CalcSize(new GUIContent(text)).x + 40f;
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
                bold = new GUIStyle(style) { font = BodyBoldFont, wordWrap = false };
                _sizeBold[style] = bold;
            }

            return bold;
        }

        /// <summary>Width a pill needs for its text (and icon).</summary>
        public float PillWidth(string text, bool icon)
        {
            return PillText.CalcSize(new GUIContent(text)).x + (icon ? 30f : 18f);
        }

        /// <summary>A coloured outline around a rect (rarity accent, selection).</summary>
        public void DrawOutline(Rect rect, Color color)
        {
            var previous = GUI.backgroundColor;
            GUI.backgroundColor = color;
            GUI.Box(rect, GUIContent.none, Outline);
            GUI.backgroundColor = previous;
        }

        /// <summary>A soft outer glow around a rect; <paramref name="strength"/> 0–1.</summary>
        public void DrawGlow(Rect rect, Color color, float strength)
        {
            var grow = 22f;
            var previous = GUI.backgroundColor;
            GUI.backgroundColor = new Color(color.r, color.g, color.b, Mathf.Clamp01(strength * VisualTheme.Current.GlowIntensity));
            GUI.Box(new Rect(rect.x - grow, rect.y - grow, rect.width + grow * 2, rect.height + grow * 2), GUIContent.none, Glow);
            GUI.backgroundColor = previous;
        }

        /// <summary>The soft shadow under a floating panel.</summary>
        public void DrawShadow(Rect rect)
        {
            GUI.Box(new Rect(rect.x - 18, rect.y - 10, rect.width + 36, rect.height + 34), GUIContent.none, Shadow);
        }

        /// <summary>A panel with its shadow.</summary>
        public void FloatingPanel(Rect rect)
        {
            var previous = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0f, 0f, 0f, 1f);
            DrawShadow(rect);
            GUI.backgroundColor = previous;
            GUI.Box(rect, GUIContent.none, Panel);
        }

        /// <summary>Draws an icon tinted with a colour. Missing icon files draw nothing.</summary>
        public void DrawIcon(Rect rect, string icon, Color color)
        {
            var tex = icon != null ? ArtAssets.Icon(icon) : null;
            if (tex == null)
            {
                return;
            }

            GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit, true, 0, color, 0, 0);
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
            DrawIcon(new Rect(x, rect.center.y - iconSize / 2f, iconSize, iconSize), icon, color);
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
                GUI.DrawTexture(rect, Coin, ScaleMode.ScaleToFit, true);
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

        /// <summary>A thin divider line.</summary>
        public void Divider(Rect rect)
        {
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1f), White, ScaleMode.StretchToFill, true, 0, new Color(Border.r, Border.g, Border.b, 0.7f), 0, 0);
        }
    }
}
