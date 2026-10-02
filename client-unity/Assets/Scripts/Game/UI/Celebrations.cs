using System.Collections.Generic;
using FishingIdle.Game.Visual;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// Level 3 of the visual intensity system (Art Bible, sections 15–16): a short banner near the
    /// top of the screen for the moments that deserve it — rare catch, Excepcional size, personal
    /// record, new species, level up, auction won. Gold and light rays, 2–4 seconds, then the game
    /// goes on. It never blocks input and never stacks: moments that arrive together wait their turn.
    /// </summary>
    public sealed class Celebrations
    {
        private const int MaxQueued = 3;

        private sealed class Item
        {
            public string Title;
            public string Subtitle;
            public Color Color;
            public Texture2D Art;
            public bool Reveal;
            public float StartedAt = -1f;
        }

        private readonly Queue<Item> _queue = new Queue<Item>();
        private Item _current;

        public bool Active => _current != null || _queue.Count > 0;

        /// <summary>
        /// Queues a celebration. <paramref name="art"/> is shown on the left (e.g. the fish);
        /// <paramref name="reveal"/> starts it as a silhouette and colours it in (new species).
        /// </summary>
        public void Show(string title, string subtitle, Color color, Texture2D art = null, bool reveal = false)
        {
            if (_queue.Count >= MaxQueued)
            {
                return;
            }

            _queue.Enqueue(new Item { Title = title, Subtitle = subtitle, Color = color, Art = art, Reveal = reveal });
        }

        public void Draw(UiSkin skin, float screenWidth)
        {
            var theme = VisualTheme.Current;
            var duration = theme.CelebrationSeconds;
            if (_current != null && Time.unscaledTime - _current.StartedAt > duration)
            {
                _current = null;
            }

            if (_current == null)
            {
                if (_queue.Count == 0)
                {
                    return;
                }

                _current = _queue.Dequeue();
                _current.StartedAt = Time.unscaledTime;
            }

            var item = _current;
            var age = Time.unscaledTime - item.StartedAt;
            var alpha = Mathf.Min(Mathf.Clamp01(age / 0.2f), Mathf.Clamp01((duration - age) / 0.45f));
            // Pop: a little overshoot, then settle.
            var pop = age < 0.35f ? Mathf.Lerp(0.82f, 1.04f, Ease(age / 0.35f)) : Mathf.Lerp(1.04f, 1f, Mathf.Clamp01((age - 0.35f) / 0.25f));

            var hasArt = item.Art != null;
            var titleWidth = skin.Display.CalcSize(new GUIContent(item.Title)).x;
            var width = Mathf.Clamp(titleWidth + (hasArt ? 230f : 90f), 520f, 900f);
            var rect = new Rect(screenWidth / 2f - width / 2f, 96f, width, 124f);

            var previousMatrix = GUI.matrix;
            var previousColor = GUI.color;
            GUIUtility.ScaleAroundPivot(new Vector2(pop, pop), rect.center);
            GUI.color = new Color(1f, 1f, 1f, alpha);

            // Soft rays turning slowly behind the banner.
            var raysSize = rect.height * 3.2f;
            var raysCentre = new Vector2(rect.xMax - rect.height * 0.55f, rect.center.y);
            var beforeRotation = GUI.matrix;
            GUIUtility.RotateAroundPivot(age * 14f, raysCentre);
            GUI.DrawTexture(new Rect(raysCentre.x - raysSize / 2f, raysCentre.y - raysSize / 2f, raysSize, raysSize), skin.Rays, ScaleMode.StretchToFill, true, 0,
                new Color(item.Color.r, item.Color.g, item.Color.b, 0.55f * theme.GlowIntensity), 0, 0);
            GUI.matrix = beforeRotation;

            skin.DrawGlow(rect, item.Color, 0.55f);
            skin.DrawShadow(rect);
            GUI.Box(rect, GUIContent.none, skin.PanelSolid);
            skin.DrawOutline(rect, item.Color);

            var textX = rect.x + 30f;
            if (hasArt)
            {
                var art = new Rect(rect.x + 18f, rect.y + 14f, 170f, rect.height - 28f);
                var bob = Mathf.Sin(age * 5f) * 2f;
                if (item.Reveal)
                {
                    // The silhouette colours in, like the Encyclopedia filling a page.
                    var k = Mathf.Clamp01((age - 0.35f) / 0.8f);
                    GUI.DrawTexture(new Rect(art.x, art.y + bob, art.width, art.height), item.Art, ScaleMode.ScaleToFit, true, 0, new Color(0.02f, 0.05f, 0.09f, alpha * (1f - k)), 0, 0);
                    GUI.DrawTexture(new Rect(art.x, art.y + bob, art.width, art.height), item.Art, ScaleMode.ScaleToFit, true, 0, new Color(1f, 1f, 1f, alpha * k), 0, 0);
                }
                else
                {
                    GUI.DrawTexture(new Rect(art.x, art.y + bob, art.width, art.height), item.Art, ScaleMode.ScaleToFit, true);
                }

                textX = art.xMax + 18f;
            }

            var previousContent = GUI.contentColor;
            GUI.contentColor = Color.Lerp(item.Color, Color.white, 0.12f);
            GUI.Label(new Rect(textX, rect.y + 16f, rect.xMax - textX - 20f, 58f), item.Title, skin.Display);
            GUI.contentColor = previousContent;
            if (!string.IsNullOrEmpty(item.Subtitle))
            {
                GUI.Label(new Rect(textX + 2f, rect.y + 74f, rect.xMax - textX - 20f, 30f), item.Subtitle, skin.DisplaySub);
            }

            GUI.color = previousColor;
            GUI.matrix = previousMatrix;
        }

        private static float Ease(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - (1f - t) * (1f - t);
        }
    }
}
