using FishingIdle.Game.Visual;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The shared look of a full window (Art Bible, section 13): the scene dimmed but still visible,
    /// a floating panel with a soft shadow, an icon tile, title and subtitle, and "✕ Fechar".
    /// </summary>
    public static class WindowFrame
    {
        /// <summary>Draws the frame and returns the content area. <paramref name="closed"/> is true when Fechar was clicked.</summary>
        public static Rect Draw(UiSkin skin, float screenWidth, float screenHeight, string title, string subtitle, out bool closed, float maxWidth = 1320f, float maxHeight = 840f, string icon = null)
        {
            var panel = Panel(skin, screenWidth, screenHeight, maxWidth, maxHeight);
            closed = Header(skin, panel, title, subtitle, icon);
            return new Rect(panel.x + 28, panel.y + 96, panel.width - 56, panel.height - 120);
        }

        /// <summary>Dims the scene and draws the floating panel; returns its rect.</summary>
        public static Rect Panel(UiSkin skin, float screenWidth, float screenHeight, float maxWidth, float maxHeight)
        {
            GUI.DrawTexture(new Rect(0, 0, screenWidth, screenHeight), skin.Overlay);
            var width = Mathf.Min(maxWidth, screenWidth - 80f);
            var height = Mathf.Min(maxHeight, screenHeight - 120f);
            var panel = new Rect((screenWidth - width) / 2f, (screenHeight - height) / 2f + 20f, width, height);
            skin.FloatingPanel(panel);
            return panel;
        }

        /// <summary>Icon tile, title, subtitle and the close button. Returns true when Fechar was clicked.</summary>
        public static bool Header(UiSkin skin, Rect panel, string title, string subtitle, string icon)
        {
            var x = panel.x + 28;
            if (icon != null)
            {
                skin.IconBadge(new Rect(x, panel.y + 20, 58, 58), icon, UiSkin.Accent);
                x += 74;
            }

            GUI.Label(new Rect(x, panel.y + 18, panel.width - (x - panel.x) - 200, 36), title, skin.Title);
            if (!string.IsNullOrEmpty(subtitle))
            {
                GUI.Label(new Rect(x + 1, panel.y + 56, panel.width - (x - panel.x) - 200, 34), subtitle, skin.SmallMuted);
            }

            return skin.IconButton(new Rect(panel.xMax - 156, panel.y + 22, 128, 42), Icons.Close, GameTexts.Box.Close, skin.Button);
        }

        /// <summary>A centred confirmation box over everything. Returns the box rect.</summary>
        public static Rect Dialog(UiSkin skin, float screenWidth, float screenHeight, float height, float width = 640f)
        {
            var rect = new Rect(screenWidth / 2f - width / 2f, screenHeight / 2f - height / 2f, width, height);
            GUI.DrawTexture(new Rect(0, 0, screenWidth, screenHeight), skin.Overlay);
            skin.DrawShadow(rect);
            GUI.Box(rect, GUIContent.none, skin.PanelSolid);
            return rect;
        }
    }
}
