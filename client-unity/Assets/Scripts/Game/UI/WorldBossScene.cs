using System;
using FishingIdle.Game.Visual;
using UnityEngine;

namespace FishingIdle.Game.UI
{
    /// <summary>
    /// The animated boss of the "Boss Mundial" window (M24-T56, addendum A-178): the abyss behind the stage, the idle pose
    /// cross-fading into the breathing pose, a slow sway in the water, a few bubbles rising, and on each Cardume attack the
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
        private const string AbyssArt = "Boss/boss_fundo_abismo", ImpactArt = "Boss/boss_impacto", BubblesArt = "Boss/boss_bolhas";

        // Timings, in seconds (presentation only).
        private const float BreathCycle = 2.4f, SwayCycle = 3.6f, TiltCycle = 5.2f;
        private const float AttackSeconds = 0.6f, ImpactSeconds = 0.5f, FlashSeconds = 0.22f, ShakeSeconds = 0.15f;

        // Sizes, in pixels or degrees.
        private const float SwayPixels = 4f, TiltDegrees = 1.6f, LungePixels = 16f, ShakePixels = 2.5f;
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

        /// <summary>The abyss behind the whole stage, cropped to fit, a little darker when the event is closed.</summary>
        public static bool DrawBackground(Rect rect, bool open)
        {
            var abyss = ArtAssets.Texture(AbyssArt);
            if (abyss == null)
            {
                return false;
            }

            GUI.DrawTexture(rect, abyss, ScaleMode.ScaleAndCrop, false, 0, Color.white, 0, 10f);
            var veil = UiSkin.Night;
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(veil.r, veil.g, veil.b, open ? 0.18f : 0.45f), 0, 10f);
            return true;
        }

        /// <summary>
        /// Draws the boss inside <paramref name="body"/>. During the event it reacts to attacks; outside it, it only breathes,
        /// darker. Returns false when the pose files are missing, so the window keeps its previous drawing.
        /// </summary>
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

            // One width for every pose (the tallest pose still fits the height), centred on the same point.
            var width = Mathf.Min(body.width, body.height * _minAspect) * 0.96f;
            var sway = Mathf.Sin(now * 2f * Mathf.PI / SwayCycle) * SwayPixels;
            var tilt = Mathf.Sin(now * 2f * Mathf.PI / TiltCycle + 0.7f) * TiltDegrees;
            var lunge = attacking ? -Mathf.Sin(since / AttackSeconds * Mathf.PI) * LungePixels : 0f;
            var shake = open && since >= 0f && since < ShakeSeconds ? Mathf.Sin(since * 90f) * ShakePixels : 0f;
            var anchor = new Vector2(body.center.x + lunge + shake, body.center.y + sway);

            DrawBubbles(body, width, now, open, false);

            // Breathing: holds the idle pose, cross-fades into the breathing one and back, every BreathCycle seconds.
            var p = Mathf.Repeat(now, BreathCycle) / BreathCycle;
            var triangle = p < 0.5f ? p * 2f : 2f - p * 2f;
            var breath = _poses[1].Texture != null ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 0.65f, triangle)) : 0f;

            // Attack: fades in fast, holds, fades out at the end.
            var attack = 0f;
            if (attacking && _poses[2].Texture != null)
            {
                attack = Mathf.Min(Mathf.Clamp01(since / 0.06f), Mathf.Clamp01((AttackSeconds - since) / 0.14f));
            }

            var flash = open && since >= 0f && since < FlashSeconds ? 1f - since / FlashSeconds : 0f;
            var tint = open ? Color.Lerp(Color.white, FlashTint, flash) : ClosedTint;
            if (flash > 0f)
            {
                var glow = new Rect(anchor.x - width * 0.35f, anchor.y - width * 0.22f, width * 0.7f, width * 0.44f);
                skin.DrawGlow(glow, UiSkin.Danger, 0.55f * flash);
            }

            var matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(tilt, anchor);
            var idle = 1f - attack;
            var idleAlpha = Mathf.Min(1f, 2f * (1f - breath)) * idle;
            var breathAlpha = Mathf.Min(1f, 2f * breath) * idle;
            DrawPose(_poses[0], anchor, width, tint, idleAlpha);
            DrawPose(_poses[1], anchor, width, tint, breathAlpha);
            DrawPose(_poses[2], anchor, width, tint, attack);
            GUI.matrix = matrix;

            if (open && since >= 0f && since < ImpactSeconds)
            {
                DrawImpact(anchor, width, since / ImpactSeconds);
            }

            DrawBubbles(body, width, now, open, true);
            return true;
        }

        private static void DrawPose(Pose pose, Vector2 anchor, float width, Color tint, float alpha)
        {
            if (pose.Texture == null || alpha <= 0.001f)
            {
                return;
            }

            var tw = (float)pose.Texture.width;
            var th = (float)pose.Texture.height;
            var scale = width / Mathf.Max(1f, pose.Bounds.width * tw);
            var full = new Vector2(tw * scale, th * scale);
            var x = anchor.x - pose.Bounds.center.x * full.x;
            var y = anchor.y - pose.Bounds.center.y * full.y;
            GUI.DrawTexture(new Rect(x, y, full.x, full.y), pose.Texture, ScaleMode.StretchToFill, true, 0, new Color(tint.r, tint.g, tint.b, tint.a * alpha), 0, 0);
        }

        /// <summary>The water burst on the boss's body (a little behind the head): grows 0,6→1,1 and fades out.</summary>
        private static void DrawImpact(Vector2 anchor, float width, float u)
        {
            var impact = ArtAssets.Texture(ImpactArt);
            if (impact == null)
            {
                return;
            }

            var ease = 1f - (1f - u) * (1f - u);
            var size = width * 0.42f * Mathf.Lerp(0.6f, 1.1f, ease);
            var alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 1f, u));
            var center = new Vector2(anchor.x - width * 0.14f, anchor.y + width * 0.02f);
            GUI.DrawTexture(new Rect(center.x - size / 2f, center.y - size / 2f, size, size), impact, ScaleMode.ScaleToFit, true, 0, new Color(1f, 1f, 1f, alpha), 0, 0);
        }

        /// <summary>A few bubble groups rising slowly around the boss, faint; one passes in front of it.</summary>
        private void DrawBubbles(Rect body, float width, float now, bool open, bool front)
        {
            var bubbles = ArtAssets.Texture(BubblesArt);
            if (bubbles == null)
            {
                return;
            }

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
                var alpha = (open ? 0.32f : 0.2f) * Mathf.Clamp01(k / 0.15f) * Mathf.Clamp01((1f - k) / 0.25f);
                if (alpha <= 0.001f || y - size < body.y - 20f)
                {
                    continue;
                }

                GUI.DrawTexture(new Rect(x - size / 2f, y - size, size, size), bubbles, ScaleMode.ScaleToFit, true, 0, new Color(1f, 1f, 1f, alpha), 0, 0);
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
