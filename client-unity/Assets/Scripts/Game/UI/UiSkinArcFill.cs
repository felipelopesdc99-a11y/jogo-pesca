using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// Fills and outlines in the ARC skin for the windows' own drawings (M25-T15, addendum A-188, TD-063): what used to be
    /// a smoothed rounded rect (GUI.DrawTexture with a corner radius) becomes a pixel shape — a pill when the radius was
    /// about half the height, a stepped chamfer otherwise, a plain square under 12 px (a pixel dot). Lines are one art
    /// pixel (2 px of the 1080 canvas), whatever width the old outline had.
    /// </summary>
    /// <remarks>
    /// Same signature as the old <c>Fill(rect, colour, radius)</c> helpers of the windows, so a window switches by
    /// changing its helper; the shapes come from the masks of <see cref="Shape"/> (made once, no allocation per frame).
    /// </remarks>
    public sealed partial class UiSkin
    {
        /// <summary>A rect smaller than this (either side) is drawn square: a pixel dot or a thin line has no corners.</summary>
        private const float ArcMinShape = 12f;

        /// <summary>
        /// A filled pixel shape in place of a rounded rect of <paramref name="radius"/>: a pill when the radius reaches
        /// half the shorter side, a chamfer of one or two art pixels otherwise, a plain rect when the radius is 0.
        /// </summary>
        public void ArcFill(Rect rect, Color color, float radius)
        {
            if (Event.current.type != EventType.Repaint || color.a <= 0f || rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            if (radius <= 0.01f || Mathf.Min(rect.width, rect.height) < ArcMinShape)
            {
                GUI.DrawTexture(Snap(rect), White, ScaleMode.StretchToFill, true, 0, color, 0, 0);
                return;
            }

            ArcShape(rect, radius, out var r, out var round);
            Shape(rect, r, round, color, 0, Color.clear, Color.clear, Color.clear);
        }

        /// <summary>A one-art-pixel outline in place of a rounded border of <paramref name="radius"/> (see <see cref="ArcFill"/>).</summary>
        public void ArcLine(Rect rect, Color color, float radius)
        {
            if (Event.current.type != EventType.Repaint || color.a <= 0f || rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            if (Mathf.Min(rect.width, rect.height) < ArcMinShape)
            {
                // Too small for a ring: a frame of four one-pixel strips.
                rect = Snap(rect);
                GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, ArcPixel), White, ScaleMode.StretchToFill, true, 0, color, 0, 0);
                GUI.DrawTexture(new Rect(rect.x, rect.yMax - ArcPixel, rect.width, ArcPixel), White, ScaleMode.StretchToFill, true, 0, color, 0, 0);
                GUI.DrawTexture(new Rect(rect.x, rect.y, ArcPixel, rect.height), White, ScaleMode.StretchToFill, true, 0, color, 0, 0);
                GUI.DrawTexture(new Rect(rect.xMax - ArcPixel, rect.y, ArcPixel, rect.height), White, ScaleMode.StretchToFill, true, 0, color, 0, 0);
                return;
            }

            ArcShape(rect, radius, out var r, out var round);
            Shape(rect, r, round, Color.clear, 1, color, Color.clear, Color.clear);
        }

        /// <summary>A filled pixel shape with a one-pixel line (both in place of a rounded rect and its border).</summary>
        public void ArcFillLine(Rect rect, Color fill, Color line, float radius)
        {
            if (Event.current.type != EventType.Repaint || rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            if (Mathf.Min(rect.width, rect.height) < ArcMinShape)
            {
                ArcFill(rect, fill, 0f);
                ArcLine(rect, line, 0f);
                return;
            }

            ArcShape(rect, radius, out var r, out var round);
            Shape(rect, r, round, fill, line.a > 0f ? 1 : 0, line, Color.clear, Color.clear);
        }

        /// <summary>The pixel shape that stands for a rounded rect: pill (round) or chamfer, and its radius in art pixels.</summary>
        private static void ArcShape(Rect rect, float radius, out int r, out bool round)
        {
            var shortSide = Mathf.Min(rect.width, rect.height);
            if (radius >= shortSide / 2f - 1f)
            {
                round = true;
                r = PillRadius(shortSide);
                return;
            }

            round = false;
            r = radius <= 0.01f ? 0 : radius >= 10f ? 2 : 1;
        }
    }
}
