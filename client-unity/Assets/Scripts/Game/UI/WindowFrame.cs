using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>The shared look of a full window: dimmed scene, panel, title and a close button.</summary>
    public static class WindowFrame
    {
        /// <summary>Draws the frame and returns the content area. <paramref name="closed"/> is true when Fechar was clicked.</summary>
        public static Rect Draw(UiSkin skin, float screenWidth, float screenHeight, string title, string subtitle, out bool closed, float maxWidth = 1320f, float maxHeight = 840f)
        {
            GUI.DrawTexture(new Rect(0, 0, screenWidth, screenHeight), skin.Overlay);
            var width = Mathf.Min(maxWidth, screenWidth - 80f);
            var height = Mathf.Min(maxHeight, screenHeight - 120f);
            var panel = new Rect((screenWidth - width) / 2f, (screenHeight - height) / 2f + 20f, width, height);
            GUI.Box(panel, GUIContent.none, skin.Panel);

            GUI.Label(new Rect(panel.x + 28, panel.y + 20, panel.width - 200, 32), title, skin.Title);
            if (!string.IsNullOrEmpty(subtitle))
            {
                GUI.Label(new Rect(panel.x + 30, panel.y + 56, panel.width - 200, 22), subtitle, skin.SmallMuted);
            }

            closed = GUI.Button(new Rect(panel.xMax - 148, panel.y + 20, 120, 38), GameTexts.Box.Close, skin.Button);
            return new Rect(panel.x + 28, panel.y + 96, panel.width - 56, panel.height - 120);
        }

        /// <summary>A centred confirmation box over everything. Returns the box rect.</summary>
        public static Rect Dialog(UiSkin skin, float screenWidth, float screenHeight, float height, float width = 640f)
        {
            var rect = new Rect(screenWidth / 2f - width / 2f, screenHeight / 2f - height / 2f, width, height);
            GUI.DrawTexture(new Rect(0, 0, screenWidth, screenHeight), skin.Overlay);
            GUI.Box(rect, GUIContent.none, skin.CardImportant);
            return rect;
        }
    }
}
