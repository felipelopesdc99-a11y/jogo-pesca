using System;
using FishingIdle.Game.Visual;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The animated boss of the "Boss Mundial" window (M24-T56, addendum A-178): the abyss behind the stage, the idle pose
    /// alternating with the breathing pose, a slow sway in the water, a few bubbles rising, and on each Cardume attack the
    /// attack pose with a short lunge, a red flash and a water burst on its body.
    /// </summary>
    /// <remarks>
    /// Presentation only: it decides nothing. The attack moment comes from <see cref="WorldBossWindow"/> (the service's attack
    /// count going up); the clock is Time.unscaledTime; the bubbles' spread is a fixed golden-ratio pattern (no draw).
    /// The three poses differ a little in framing, so each pose's opaque area (alpha bounds) is measured once when loaded
    /// and every pose is drawn with that area at the same width and centred on the same point. Without the pose files the
    /// window keeps its previous drawing (<see cref="DrawBoss"/> returns false); nothing is allocated per frame.
    /// </remarks>
    internal sealed class WorldBossScene
    {
        private const string IdleArt = "Boss/boss_pose_parado", BreathArt = "Boss/boss_pose_respira", AttackArt = "Boss/boss_pose_ataque";
        private const string AbyssArt = "Boss/boss_fundo_abismo";

        // Timings, in seconds (presentation only).
        private const float BreathCycle = 2.4f, SwayCycle = 3.6f;
        private const float AttackSeconds = 0.6f, ImpactSeconds = 0.5f, FlashSeconds = 0.22f, ShakeSeconds = 0.15f;

        // Sizes, in pixels or degrees.
        private const float SwayPixels = 4f, LungePixels = 16f, ShakePixels = 2.5f;
        private const int BubbleCount = 4;

        private static readonly Color ClosedTint = new Color(0.5f, 0.56f, 0.66f, 1f);
        private static readonly Color FlashTint = new Color(1f, 0.58f, 0.52f, 1f);

        private struct Pose
        {
            public Texture2D Texture;
            public Rect Bounds; // the opaque area, 0..1, top-left origin (GUI)
        }

        private readonly Pose[] _poses = new Pose[3]; // idle, breath, attack
        private readonly float[] _bubbleX = new float[BubbleCount], _bubbleSpeed = new float[BubbleCount],
            _bubblePhase = new float[BubbleCount], _bubbleSize = new float[BubbleCount];
        private bool _loaded;
        private float _minAspect = 1f;
        private float _attackAt = -100f;

        /// <summary>Starts the attack pose, flash and burst now (the window calls it when the attack count goes up).</summary>
        public void Attack() => _attackAt = Time.unscaledTime;

        /// <summary>Forgets a running attack (the window was reopened).</summary>
        public void Reset() => _attackAt = -100f;

        /// <summary>
        /// The abyss behind the whole stage at a whole scale, cropped at the edges (the floor kept in sight), a little
        /// darker when the event is closed.
        /// </summary>
        public static bool DrawBackground(UiSkin skin, Rect rect, bool open)
        {
            var abyss = ArtAssets.Texture(AbyssArt);
            if (abyss == null)
            {
                return false;
            }

            ArcArt.Cover(rect, abyss, Color.white, 0.5f, 0.7f);
            var veil = UiSkin.Night;
            ArcArt.Fill(skin, rect, new Color(veil.r, veil.g, veil.b, open ? 0.18f : 0.45f), 0f);
            return true;
        }

        /// <summary>
        /// Draws the boss inside <paramref name="body"/>. During the event it reacts to attacks; outside it, it only breathes,
        /// darker. Returns false when the pose files are missing, so the window keeps its previous drawing.
        /// </summary>
        /// <remarks>
        /// In pixel art (Art Bible 3.2, 4.9, 4.10, 5) the boss is drawn at the largest whole scale that fits (3× in the big
        /// window), never turned: the poses alternate instead of cross-fading, the sway and the lunge move it in whole art
        /// pixels, the hit is a one-pixel red outline flash, and the water burst and the bubbles are drawn by the code.
        /// </remarks>
        public bool DrawBoss(UiSkin skin, Rect body, bool open)
        {
            Load();
            if (_poses[0].Texture == null)
            {
                return false;
            }

            var now = Time.unscaledTime;
            var since = now - _attackAt;
            var attacking = open && since >= 0f && since < AttackSeconds;
            var pixel = ArtAssets.IsPixel(_poses[0].Texture);

            // The size of one art pixel on the canvas: a whole number of screen pixels (pixel art) or a free scale.
            var unit = PixelDraw.ScreenScale();
            var tex0 = _poses[0].Texture;
            float px;
            if (pixel)
            {
                var k = Mathf.Max(1, PixelDraw.WholeScale(new Rect(0f, 0f, body.width * 0.96f, body.height * 0.96f), tex0, unit));
                px = k / unit;
            }
            else
            {
                var width0 = Mathf.Min(body.width, body.height * _minAspect) * 0.96f;
                px = width0 / Mathf.Max(1f, _poses[0].Bounds.width * tex0.width);
            }

            var width = _poses[0].Bounds.width * tex0.width * px;
            var sway = Mathf.Sin(now * 2f * Mathf.PI / SwayCycle) * SwayPixels;
            var lunge = attacking ? -Mathf.Sin(since / AttackSeconds * Mathf.PI) * LungePixels : 0f;
            var shake = open && since >= 0f && since < ShakeSeconds ? Mathf.Sin(since * 90f) * ShakePixels : 0f;
            var anchor = new Vector2(body.center.x + lunge + shake, body.center.y + sway);
            if (pixel)
            {
                // Moves in whole art pixels.
                anchor = new Vector2(Mathf.Round(anchor.x / px) * px, Mathf.Round(anchor.y / px) * px);
            }

            DrawBubbles(body, width, now, open, false);

            // Breathing: the idle pose, then the breathing one for the middle of every BreathCycle seconds.
            var p = Mathf.Repeat(now, BreathCycle) / BreathCycle;
            var breathing = _poses[1].Texture != null && p > 0.4f && p < 0.75f;
            var pose = attacking && _poses[2].Texture != null ? _poses[2] : breathing ? _poses[1] : _poses[0];

            var flash = open && since >= 0f && since < FlashSeconds ? 1f - since / FlashSeconds : 0f;
            var tint = open ? Color.Lerp(Color.white, FlashTint, flash) : ClosedTint;
            var drawn = DrawPose(pose, anchor, px, tint, pixel);
            if (flash > 0f)
            {
                // The hit: a one-pixel red ring around the body, short (Art Bible 4.10).
                skin.PixelGlow(new Rect(drawn.x + drawn.width * 0.2f, drawn.y + drawn.height * 0.25f, drawn.width * 0.6f, drawn.height * 0.5f), UiSkin.Danger, flash);
            }

            if (open && since >= 0f && since < ImpactSeconds)
            {
                DrawImpact(skin, anchor, width, since / ImpactSeconds);
            }

            DrawBubbles(body, width, now, open, true);
            return true;
        }

        /// <summary>One pose with its opaque area centred on <paramref name="anchor"/>, <paramref name="px"/> canvas units per art pixel.</summary>
        private static Rect DrawPose(Pose pose, Vector2 anchor, float px, Color tint, bool pixel)
        {
            var tw = (float)pose.Texture.width;
            var th = (float)pose.Texture.height;
            var full = new Vector2(tw * px, th * px);
            var x = anchor.x - pose.Bounds.center.x * full.x;
            var y = anchor.y - pose.Bounds.center.y * full.y;
            if (pixel)
            {
                // The corner on a whole art pixel of the screen, so the three poses line up.
                var unit = PixelDraw.ScreenScale();
                x = Mathf.Round(x * unit) / unit;
                y = Mathf.Round(y * unit) / unit;
            }

            var rect = new Rect(x, y, full.x, full.y);
            GUI.DrawTexture(rect, pose.Texture, ScaleMode.StretchToFill, true, 0, tint, 0, 0);
            return rect;
        }

        /// <summary>
        /// The water burst on the boss's body (a little behind the head), drawn by the code: eight pixel drops thrown
        /// outwards from a light centre, in whole art pixels, fading out.
        /// </summary>
        private static void DrawImpact(UiSkin skin, Vector2 anchor, float width, float u)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            const int p = UiSkin.ArcPixel;
            var ease = 1f - (1f - u) * (1f - u);
            var reach = width * 0.16f * Mathf.Lerp(0.3f, 1f, ease);
            var alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 1f, u));
            var center = new Vector2(anchor.x - width * 0.14f, anchor.y + width * 0.02f);
            var foam = new Color(0.92f, 0.98f, 1f, alpha);
            var water = new Color(0.50f, 0.85f, 0.95f, alpha);
            for (var i = 0; i < 8; i++)
            {
                var angle = i * Mathf.PI / 4f + 0.2f;
                var d = reach * (i % 2 == 0 ? 1f : 0.7f);
                var drop = p * (i % 2 == 0 ? 3 : 2);
                var x = Mathf.Round((center.x + Mathf.Cos(angle) * d) / p) * p;
                var y = Mathf.Round((center.y + Mathf.Sin(angle) * d) / p) * p;
                GUI.DrawTexture(new Rect(x - drop / 2f, y - drop / 2f, drop, drop), skin.White, ScaleMode.StretchToFill, true, 0, i % 2 == 0 ? foam : water, 0, 0);
            }

            if (u < 0.35f)
            {
                var core = p * 4;
                GUI.DrawTexture(UiSkin.Snap(new Rect(center.x - core / 2f, center.y - core / 2f, core, core)), skin.White, ScaleMode.StretchToFill, true, 0, foam, 0, 0);
            }
        }

        /// <summary>A few bubble groups rising slowly around the boss, faint; one passes in front of it (drawn by the code).</summary>
        private void DrawBubbles(Rect body, float width, float now, bool open, bool front)
        {
            var travel = body.height + 40f;
            for (var i = 0; i < BubbleCount; i++)
            {
                if ((i == 0) != front)
                {
                    continue;
                }

                var size = _bubbleSize[i];
                var rise = Mathf.Repeat(now * _bubbleSpeed[i] + _bubblePhase[i], travel);
                var y = body.yMax - rise;
                var x = body.center.x + _bubbleX[i] * width * 0.5f + Mathf.Sin(now * 0.9f + i * 1.7f) * 6f;
                var k = rise / travel;
                var alpha = (open ? 0.55f : 0.35f) * Mathf.Clamp01(k / 0.15f) * Mathf.Clamp01((1f - k) / 0.25f);
                if (alpha <= 0.001f || y - size < body.y - 20f)
                {
                    continue;
                }

                // A group of three: one big, two small around it.
                var color = new Color(0.85f, 0.97f, 1f, alpha);
                ArcArt.Bubble(new Vector2(x, y - size * 0.5f), true, color);
                ArcArt.Bubble(new Vector2(x - size * 0.3f, y - size * 0.1f), false, color);
                ArcArt.Bubble(new Vector2(x + size * 0.25f, y - size * 0.9f), false, color);
            }
        }

        private void Load()
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;
            var paths = new[] { IdleArt, BreathArt, AttackArt };
            _minAspect = float.MaxValue;
            for (var i = 0; i < paths.Length; i++)
            {
                var tex = ArtAssets.Texture(paths[i]);
                _poses[i] = new Pose { Texture = tex, Bounds = tex == null ? new Rect(0f, 0f, 1f, 1f) : AlphaBounds(tex) };
                if (tex != null)
                {
                    var aspect = _poses[i].Bounds.width * tex.width / Mathf.Max(1f, _poses[i].Bounds.height * tex.height);
                    _minAspect = Mathf.Min(_minAspect, aspect);
                }
            }

            if (_minAspect == float.MaxValue)
            {
                _minAspect = 1f;
            }

            for (var i = 0; i < BubbleCount; i++)
            {
                // Spread by the golden ratio: varied, but the same every time (decoration, no draw needed).
                _bubbleX[i] = Mathf.Repeat(0.23f + i * 0.618f, 1f) * 1.9f - 0.95f;
                _bubbleSpeed[i] = 14f + Mathf.Repeat(0.71f + i * 0.382f, 1f) * 12f;
                _bubblePhase[i] = Mathf.Repeat(0.37f + i * 0.754f, 1f) * 400f;
                _bubbleSize[i] = 34f + Mathf.Repeat(0.11f + i * 0.447f, 1f) * 24f;
            }
        }

        /// <summary>
        /// The opaque area of a texture (alpha above ~10%), as 0..1 with the GUI's top-left origin. The art is imported
        /// unreadable, so a small copy is drawn into a render texture and read back, once per pose. The whole picture if
        /// anything fails.
        /// </summary>
        private static Rect AlphaBounds(Texture2D tex)
        {
            const int W = 160;
            var h = Mathf.Max(1, Mathf.RoundToInt(W * (float)tex.height / tex.width));
            var previous = RenderTexture.active;
            RenderTexture rt = null;
            Texture2D read = null;
            try
            {
                rt = RenderTexture.GetTemporary(W, h, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(tex, rt);
                RenderTexture.active = rt;
                read = new Texture2D(W, h, TextureFormat.RGBA32, false);
                read.ReadPixels(new Rect(0, 0, W, h), 0, 0, false);
                read.Apply(false);
                var pixels = read.GetPixels32();
                int minX = W, maxX = -1, minY = h, maxY = -1;
                for (var y = 0; y < h; y++)
                {
                    for (var x = 0; x < W; x++)
                    {
                        if (pixels[y * W + x].a > 24)
                        {
                            if (x < minX) minX = x;
                            if (x > maxX) maxX = x;
                            if (y < minY) minY = y;
                            if (y > maxY) maxY = y;
                        }
                    }
                }

                if (maxX < minX || maxY < minY)
                {
                    return new Rect(0f, 0f, 1f, 1f);
                }

                // ReadPixels rows start at the bottom; the GUI's start at the top.
                return new Rect((float)minX / W, 1f - (float)(maxY + 1) / h, (float)(maxX - minX + 1) / W, (float)(maxY - minY + 1) / h);
            }
            catch (Exception)
            {
                return new Rect(0f, 0f, 1f, 1f);
            }
            finally
            {
                RenderTexture.active = previous;
                if (rt != null)
                {
                    RenderTexture.ReleaseTemporary(rt);
                }

                if (read != null)
                {
                    UnityEngine.Object.Destroy(read);
                }
            }
        }
    }
}
