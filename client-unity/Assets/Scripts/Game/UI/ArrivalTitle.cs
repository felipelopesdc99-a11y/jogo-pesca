using System.Linq;
using FishingIdle.Game.Bootstrap;
using FishingIdle.Game.Visual;
using FishingIdle.Texts;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The "new chapter" title card shown when the boat arrives on a map (addendum A-085): the
    /// chapter number, the map's name and its one-line feeling, fading in over the scene like the
    /// title of a film, then fading out. Calm on purpose: no box, no rays, no input blocked.
    /// </summary>
    public sealed class ArrivalTitle
    {
        private const float FadeIn = 1.2f;
        private const float Hold = 3.6f;
        private const float FadeOut = 1.6f;

        private string _chapter;
        private string _name;
        private string _line;
        private float _startedAt = -100f;

        public void Show(string chapter, string name, string line)
        {
            _chapter = chapter;
            _name = name;
            _line = line;
            _startedAt = Time.unscaledTime;
        }

        public void Draw(UiSkin skin, float screenWidth, float screenHeight)
        {
            var t = Time.unscaledTime - _startedAt;
            if (string.IsNullOrEmpty(_name) || t > FadeIn + Hold + FadeOut)
            {
                return;
            }

            var alpha = t < FadeIn ? Mathf.SmoothStep(0f, 1f, t / FadeIn) : t < FadeIn + Hold ? 1f : 1f - Mathf.SmoothStep(0f, 1f, (t - FadeIn - Hold) / FadeOut);
            var rise = (1f - Mathf.Clamp01(t / FadeIn)) * 12f;
            var centreY = screenHeight * 0.3f + rise;

            // A soft dark band behind the words, so they read over any sky.
            var band = new Rect(screenWidth / 2f - 460f, centreY - 70f, 920f, 150f);
            GUI.DrawTexture(band, skin.Glow.normal.background, ScaleMode.StretchToFill, true, 0, new Color(0.02f, 0.05f, 0.1f, 0.45f * alpha), 0, 0);

            var previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            var gold = VisualTheme.Current.Reward;
            var chapterStyle = new GUIStyle(skin.SmallGold) { alignment = TextAnchor.MiddleCenter, fontSize = 15 };
            var nameStyle = new GUIStyle(skin.Display) { alignment = TextAnchor.MiddleCenter, fontSize = 52 };
            var lineStyle = new GUIStyle(skin.DisplaySub) { alignment = TextAnchor.MiddleCenter, fontSize = 18 };
            lineStyle.normal.textColor = UiSkin.Muted;

            if (!string.IsNullOrEmpty(_chapter))
            {
                UiSkin.ShadowLabel(new Rect(0, centreY - 58f, screenWidth, 22f), _chapter, chapterStyle);
                var lw = 60f;
                GUI.DrawTexture(new Rect(screenWidth / 2f - 150f - lw, centreY - 47f, lw, 1.5f), skin.White, ScaleMode.StretchToFill, true, 0, new Color(gold.r, gold.g, gold.b, 0.7f * alpha), 0, 0);
                GUI.DrawTexture(new Rect(screenWidth / 2f + 150f, centreY - 47f, lw, 1.5f), skin.White, ScaleMode.StretchToFill, true, 0, new Color(gold.r, gold.g, gold.b, 0.7f * alpha), 0, 0);
            }

            UiSkin.ShadowLabel(new Rect(0, centreY - 34f, screenWidth, 64f), _name, nameStyle);
            if (!string.IsNullOrEmpty(_line))
            {
                UiSkin.ShadowLabel(new Rect(0, centreY + 32f, screenWidth, 28f), _line, lineStyle);
            }

            GUI.color = previous;
        }

        /// <summary>The chapter of a map: its place in the journey, by the Fisher level that unlocks it.</summary>
        public static int ChapterOf(GameRoot root, string mapId)
        {
            var maps = root?.Game?.Session.Config.Maps.Maps;
            if (maps == null)
            {
                return 0;
            }

            var ordered = maps.OrderBy(m => m.UnlockFisherLevel).ToList();
            return ordered.FindIndex(m => m.Id == mapId) + 1;
        }
    }
}
