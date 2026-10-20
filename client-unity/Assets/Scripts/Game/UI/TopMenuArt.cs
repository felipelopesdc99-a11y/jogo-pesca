using FishingIdle.Game.Visual;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// Drawing pieces of the top menu in the ARC skin (A-184, which updates the look of A-169): the night strip closed by
    /// the thin gold frame lines, the four tabs (the open one a raised pill with the gold line, the others soft text and
    /// icon), the coin plaque as a pill, the round shortcuts on the right as discs with the gold frame and their label in
    /// a pill under them, and the red counters with a gold line. Presentation only.
    /// </summary>
    /// <remarks>
    /// Everything is drawn by the code (<see cref="UiSkin.Shape"/>); the wooden trim, the coloured tab faces and the
    /// painted shortcut ring (UI/ui_atalho_moldura) of A-169 are no longer used. The tab and shortcut icons are the game's
    /// icons (ico_aba_*, ico_atalho_* when they exist), drawn untinted.
    /// </remarks>
    internal static class TopMenuArt
    {
        public enum Round { Blue, Pass, Gold }

        private static GUIStyle _tabLabel, _tabLabelOn, _roundLabel, _tinyGold, _badgeCount;

        private static VisualTheme Theme => VisualTheme.Current;

        private static GUIStyle TabLabel(UiSkin skin) => _tabLabel ?? (_tabLabel = new GUIStyle(skin.SmallBold) { alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Clip, normal = { textColor = UiSkin.Muted } });

        private static GUIStyle TabLabelOn(UiSkin skin) => _tabLabelOn ?? (_tabLabelOn = new GUIStyle(TabLabel(skin)) { normal = { textColor = UiSkin.TitleColor } });

        public static GUIStyle RoundLabel(UiSkin skin) => _roundLabel ?? (_roundLabel = new GUIStyle(skin.SmallBold) { fontSize = UiSkin.ChipSize, alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Overflow, normal = { textColor = UiSkin.Text } });

        private static GUIStyle TinyGold(UiSkin skin) => _tinyGold ?? (_tinyGold = new GUIStyle(skin.SmallGoldCenter) { fontSize = UiSkin.ChipSize, clipping = TextClipping.Overflow });

        private static GUIStyle BadgeCount(UiSkin skin) => _badgeCount ?? (_badgeCount = new GUIStyle(skin.PillText) { normal = { textColor = UiSkin.DarkInk } });

        /// <summary>Width a tab needs to show its label on one line.</summary>
        public static float TabLabelWidth(UiSkin skin, string label) => TabLabel(skin).CalcSize(new GUIContent(label)).x;

        /// <summary>The strip across the top: the panel night closed at the bottom by the three frame lines.</summary>
        public static void Strip(UiSkin skin, Rect bar)
        {
            skin.TopBarBackground(bar);
        }

        /// <summary>
        /// A tab: icon over the label (or the icon alone). The open one is a raised pill with the gold line and the label
        /// in the title colour (Art Bible 7: "Abas"); the others are only the icon and the soft label, brighter on hover.
        /// Returns true when clicked.
        /// </summary>
        public static bool Tab(UiSkin skin, Rect rect, int group, string icon, string label, bool active)
        {
            var hover = GUI.enabled && rect.Contains(Event.current.mousePosition);
            var previous = GUI.color;
            if (!GUI.enabled)
            {
                GUI.color = previous * new Color(1f, 1f, 1f, 0.6f);
            }

            if (active)
            {
                skin.Shape(rect, 6, true, Theme.PanelElevated, 1, UiSkin.FrameGold, Color.clear, Color.clear);
            }
            else if (hover)
            {
                skin.Shape(rect, 6, true, new Color(Theme.PanelElevated.r, Theme.PanelElevated.g, Theme.PanelElevated.b, 0.6f), 1, UiSkin.ChipOffColor, Color.clear, Color.clear);
            }

            var iconColor = active || hover ? Color.white : new Color(1f, 1f, 1f, 0.78f);
            if (string.IsNullOrEmpty(label))
            {
                skin.DrawIconExact(UiSkin.Snap(new Rect(rect.center.x - 19f, rect.center.y - 19f, 38f, 38f)), icon, iconColor);
            }
            else
            {
                skin.DrawIconExact(UiSkin.Snap(new Rect(rect.center.x - 16f, rect.y + 6f, 32f, 32f)), icon, iconColor);
                var labelRect = new Rect(rect.x + 4f, rect.y + 36f, rect.width - 8f, rect.height - 38f);
                var style = active ? TabLabelOn(skin) : TabLabel(skin);
                if (hover && !active)
                {
                    UiSkin.ColoredLabel(labelRect, label, style, UiSkin.Text, false);
                }
                else
                {
                    GUI.Label(labelRect, label, style);
                }
            }

            GUI.color = previous;
            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        /// <summary>A small sub-tab (the windows of a group): an ARC chip. Returns true when clicked.</summary>
        public static bool SubTab(UiSkin skin, Rect rect, string label, bool active)
        {
            return GUI.Button(rect, label, active ? skin.ChipActive : skin.Chip);
        }

        /// <summary>The coin plaque under the strip: a night pill with the gold frame (the ropes of A-169 are gone).</summary>
        public static void Plaque(UiSkin skin, Rect rect, float ropeTop)
        {
            skin.HardShadow(rect);
            skin.Shape(rect, UiSkin.PillRadius(rect.height), true, new Color(Theme.Panel.r, Theme.Panel.g, Theme.Panel.b, 0.95f), 3, UiSkin.FrameGold, UiSkin.FrameInner, UiSkin.FrameDark);
        }

        /// <summary>
        /// A round shortcut: a night disc with the gold frame and the coloured icon; its label in a pill under it (Art Bible
        /// 7: "Atalhos redondos"). The Boss Mundial's one (<see cref="Round.Gold"/>) has the reward-gold line and a
        /// blinking one-pixel ring while the event is on. Returns true when clicked.
        /// </summary>
        public static bool RoundButton(UiSkin skin, Rect circle, string icon, string label, Round kind, bool active)
        {
            var hover = GUI.enabled && circle.Contains(Event.current.mousePosition);
            var c = UiSkin.Snap(circle);
            if (kind == Round.Gold)
            {
                // Pixel "brilho" (Art Bible 4.10): a reward ring that blinks twice a second, no soft glow and no scaling.
                if (Mathf.Repeat(Time.unscaledTime, 0.5f) < 0.3f)
                {
                    skin.PixelGlow(c, UiSkin.Gold, 1f);
                }
            }
            else if (active)
            {
                skin.PixelGlow(c, UiSkin.Accent, 0.9f);
            }

            skin.HardShadow(c);
            var fill = active || hover ? Theme.PanelElevated : Theme.Panel;
            var outer = kind == Round.Gold ? UiSkin.Gold : hover || active ? UiSkin.TitleColor : UiSkin.FrameGold;
            skin.FramedDisc(c, fill, outer);

            var iconSize = Mathf.Round(c.width * 0.5f);
            skin.DrawIconExact(UiSkin.Snap(new Rect(c.center.x - iconSize / 2f, c.center.y - iconSize / 2f, iconSize, iconSize)), icon, Color.white);
            if (!string.IsNullOrEmpty(label))
            {
                var style = RoundLabel(skin);
                var w = style.CalcSize(new GUIContent(label)).x + 16f;
                var pill = new Rect(circle.center.x - w / 2f, circle.yMax + 4f, w, UiSkin.SmallLine);
                skin.Pill(pill, new Color(Theme.Panel.r, Theme.Panel.g, Theme.Panel.b, 0.88f), Color.clear);
                GUI.Label(pill, label, style);
            }

            return GUI.Button(circle, GUIContent.none, GUIStyle.none);
        }

        /// <summary>The time left of an event in a field pill with the gold line over the top of a round shortcut.</summary>
        public static void TimePill(UiSkin skin, Rect circle, string text)
        {
            var w = TinyGold(skin).CalcSize(new GUIContent(text)).x + 16f;
            var pill = new Rect(circle.center.x - w / 2f, circle.y - 14f, w, 24f);
            skin.Pill(pill, new Color(Theme.Field.r, Theme.Field.g, Theme.Field.b, 0.95f), UiSkin.Gold);
            GUI.Label(pill, text, TinyGold(skin));
        }

        /// <summary>A small field pill with a short text over the bottom of a round shortcut (the Aquário's "12/40").</summary>
        public static void BottomPill(UiSkin skin, Rect circle, string text)
        {
            var style = RoundLabel(skin);
            var w = style.CalcSize(new GUIContent(text)).x + 12f;
            // Ends 2 px above the label under the button.
            var pill = new Rect(circle.center.x - w / 2f, circle.yMax - 20f, w, 22f);
            skin.Pill(pill, new Color(Theme.Field.r, Theme.Field.g, Theme.Field.b, 0.95f), UiSkin.FrameDark);
            GUI.Label(pill, text, style);
        }

        /// <summary>
        /// A counter on the top-right corner of a button (Art Bible 6.1 "Alerta"): a dot with no count, or a pill with the
        /// count, in <paramref name="color"/> with a reward-gold line; the count in dark text (4,6:1 on the alert red).
        /// </summary>
        public static void Badge(UiSkin skin, Rect target, int count, Color color)
        {
            // The game's red becomes the Art Bible's alert red; other colours (the gold dot of an event) stay.
            var fill = color == UiSkin.Danger ? UiSkin.AlertColor : color;
            if (count <= 0)
            {
                var dot = UiSkin.Snap(new Rect(target.xMax - 12f, target.y - 2f, 16f, 16f));
                skin.Shape(dot, 3, true, fill, 1, UiSkin.Gold, Color.clear, Color.clear);
                return;
            }

            var text = count > 9 ? "9+" : count.ToString();
            var pill = UiSkin.Snap(new Rect(target.xMax - 18f, target.y - 6f, text.Length > 1 ? 32f : 24f, 24f));
            skin.Pill(pill, fill, UiSkin.Gold);
            GUI.Label(pill, text, BadgeCount(skin));
        }
    }
}
