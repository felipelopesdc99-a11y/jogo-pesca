using FishingIdle.Game.Scene;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// Colours, textures and IMGUI styles for the game interface, built once on first use.
    /// </summary>
    /// <remarks>
    /// The MVP interface is drawn with Unity's immediate-mode GUI (docs/DECISOES.md, TD-017): no
    /// prefabs, no imported fonts, no input-module setup, so it works the moment Play is pressed.
    /// All look-and-feel lives here, which keeps a later move to UI Toolkit contained.
    /// </remarks>
    public sealed class UiSkin
    {
        public static readonly Color Text = new Color(0.95f, 0.97f, 0.99f);
        public static readonly Color Muted = new Color(0.70f, 0.77f, 0.84f);
        public static readonly Color Accent = new Color(0.20f, 0.66f, 0.70f);
        public static readonly Color AccentHover = new Color(0.26f, 0.75f, 0.79f);
        public static readonly Color Gold = new Color(1f, 0.82f, 0.36f);
        public static readonly Color Rare = new Color(0.76f, 0.56f, 1f);
        public static readonly Color Danger = new Color(0.86f, 0.36f, 0.32f);
        public static readonly Color PanelColor = new Color(0.06f, 0.10f, 0.16f, 0.86f);
        public static readonly Color CardColor = new Color(0.12f, 0.18f, 0.26f, 0.96f);
        public static readonly Color CardHover = new Color(0.16f, 0.23f, 0.33f, 0.98f);

        public GUIStyle Panel, Card, CardHovered, CardSelected, CardImportant;
        public GUIStyle Button, ButtonPrimary, ButtonDanger, Chip, ChipActive;
        public GUIStyle Title, Heading, Body, BodyBold, Small, SmallMuted, SmallRight, SmallMutedRight, SmallGold, SmallGoldRight, Number, NumberRight, Center, Badge;

        public Texture2D White, Overlay, Coin;

        private static UiSkin _instance;

        /// <summary>The shared skin. Only valid inside OnGUI (it reads GUI.skin the first time).</summary>
        public static UiSkin Get()
        {
            return _instance ?? (_instance = new UiSkin());
        }

        private UiSkin()
        {
            White = Art.SolidTexture(Color.white);
            Overlay = Art.SolidTexture(new Color(0.02f, 0.04f, 0.07f, 0.55f));
            Coin = Art.Circle.texture;

            Panel = Box(PanelColor, new Color(1f, 1f, 1f, 0.10f), 14);
            Card = Box(CardColor, new Color(1f, 1f, 1f, 0.06f), 12);
            CardHovered = Box(CardHover, new Color(1f, 1f, 1f, 0.16f), 12);
            CardSelected = Box(new Color(0.12f, 0.30f, 0.34f, 0.98f), Accent, 12, 2.5f);
            CardImportant = Box(CardColor, Gold, 12, 2f);

            Button = ButtonStyle(new Color(1f, 1f, 1f, 0.10f), new Color(1f, 1f, 1f, 0.18f), Text);
            ButtonPrimary = ButtonStyle(Accent, AccentHover, Color.white);
            ButtonDanger = ButtonStyle(Danger, new Color(0.95f, 0.45f, 0.40f), Color.white);
            Chip = ButtonStyle(new Color(1f, 1f, 1f, 0.07f), new Color(1f, 1f, 1f, 0.15f), Muted, 16, 13);
            ChipActive = ButtonStyle(Accent, AccentHover, Color.white, 16, 13);

            Title = Label(24, FontStyle.Bold, Text);
            Heading = Label(19, FontStyle.Bold, Text);
            Body = Label(16, FontStyle.Normal, Text);
            BodyBold = Label(16, FontStyle.Bold, Text);
            Small = Label(13, FontStyle.Normal, Text);
            SmallMuted = Label(13, FontStyle.Normal, Muted);
            SmallRight = new GUIStyle(Small) { alignment = TextAnchor.UpperRight };
            SmallMutedRight = new GUIStyle(SmallMuted) { alignment = TextAnchor.UpperRight };
            SmallGold = Label(13, FontStyle.Bold, Gold);
            SmallGoldRight = new GUIStyle(SmallGold) { alignment = TextAnchor.UpperRight };
            Number = Label(20, FontStyle.Bold, Gold);
            NumberRight = new GUIStyle(Number) { alignment = TextAnchor.UpperRight };
            Center = Label(16, FontStyle.Normal, Text);
            Center.alignment = TextAnchor.MiddleCenter;

            Badge = Label(11, FontStyle.Bold, new Color(0.08f, 0.08f, 0.1f));
            Badge.alignment = TextAnchor.MiddleCenter;
            Badge.normal.background = Art.RoundedRectTexture(Color.white, Color.clear, 6, 0f);
            Badge.border = new RectOffset(6, 6, 6, 6);
            Badge.padding = new RectOffset(6, 6, 2, 2);
        }

        private static GUIStyle Box(Color fill, Color border, int radius, float borderWidth = 1.5f)
        {
            var style = new GUIStyle(GUI.skin.box)
            {
                border = new RectOffset(radius, radius, radius, radius),
                padding = new RectOffset(16, 16, 14, 14),
                margin = new RectOffset(0, 0, 0, 0),
            };
            style.normal.background = Art.RoundedRectTexture(fill, border, radius, borderWidth);
            style.normal.textColor = Text;
            return style;
        }

        private static GUIStyle ButtonStyle(Color fill, Color hover, Color text, int radius = 10, int fontSize = 16)
        {
            var style = new GUIStyle(GUI.skin.button)
            {
                border = new RectOffset(radius, radius, radius, radius),
                padding = new RectOffset(14, 14, 8, 8),
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
            };
            style.normal.background = Art.RoundedRectTexture(fill, Color.clear, radius, 0f);
            style.hover.background = Art.RoundedRectTexture(hover, Color.clear, radius, 0f);
            style.active.background = Art.RoundedRectTexture(fill * 0.85f, Color.clear, radius, 0f);
            style.focused.background = style.normal.background;
            style.normal.textColor = text;
            style.hover.textColor = text;
            style.active.textColor = text;
            style.focused.textColor = text;
            return style;
        }

        private static GUIStyle Label(int size, FontStyle fontStyle, Color color)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                fontStyle = fontStyle,
                wordWrap = true,
                richText = false,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
            };
            style.normal.textColor = color;
            return style;
        }

        /// <summary>A horizontal progress bar.</summary>
        public void Bar(Rect rect, float fraction, bool gold = false)
        {
            GUI.DrawTexture(rect, White, ScaleMode.StretchToFill, true, 0, new Color(1f, 1f, 1f, 0.12f), 0, rect.height / 2f);
            var fill = new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fraction), rect.height);
            if (fill.width >= rect.height)
            {
                GUI.DrawTexture(fill, White, ScaleMode.StretchToFill, true, 0, gold ? Gold : Accent, 0, rect.height / 2f);
            }
        }

        /// <summary>A small coloured tag like "NOVA ESPÉCIE".</summary>
        public void Tag(Rect rect, string text, Color color)
        {
            var previous = GUI.backgroundColor;
            GUI.backgroundColor = color;
            GUI.Label(rect, text, Badge);
            GUI.backgroundColor = previous;
        }

        /// <summary>A coin symbol.</summary>
        public void CoinIcon(Rect rect)
        {
            var previous = GUI.color;
            GUI.color = Gold;
            GUI.DrawTexture(rect, Coin, ScaleMode.ScaleToFit, true);
            GUI.color = new Color(0.75f, 0.52f, 0.12f);
            var inner = new Rect(rect.x + rect.width * 0.3f, rect.y + rect.height * 0.3f, rect.width * 0.4f, rect.height * 0.4f);
            GUI.DrawTexture(inner, Coin, ScaleMode.ScaleToFit, true);
            GUI.color = previous;
        }
    }
}
