using System.Collections.Generic;
using FishingIdle.Game.UI;
using UnityEngine;

namespace FishingIdle.Game.Audio
{
    /// <summary>Synthesised placeholder clips, built once and cached.</summary>
    public static class SoundBank
    {
        private const int Rate = 44100;
        private static readonly Dictionary<ToastKind, AudioClip> Effects = new Dictionary<ToastKind, AudioClip>();
        private static AudioClip _water;

        public static AudioClip For(ToastKind kind)
        {
            // "clip == null" also catches a clip Unity destroyed when play mode ended.
            if (!Effects.TryGetValue(kind, out var clip) || clip == null)
            {
                clip = Build(kind);
                Effects[kind] = clip;
            }

            return clip;
        }

        private static AudioClip Build(ToastKind kind)
        {
            switch (kind)
            {
                case ToastKind.Catch: return Notes("Captura", new[] { 880f, 1318.5f }, 0.09f, 0.35f);
                case ToastKind.Important: return Notes("Captura importante", new[] { 784f, 988f, 1175f, 1568f }, 0.11f, 0.4f, shimmer: true);
                case ToastKind.Coins: return Notes("Moedas", new[] { 1975.5f, 2637f }, 0.07f, 0.25f, bright: true);
                case ToastKind.LevelUp: return Notes("Subiu de nível", new[] { 523.3f, 659.3f, 784f, 1046.5f }, 0.12f, 0.4f, shimmer: true);
                case ToastKind.Warning: return Notes("Aviso", new[] { 392f, 330f }, 0.11f, 0.3f);
                default: return Notes("Clique", new[] { 1046.5f }, 0.06f, 0.2f);
            }
        }

        /// <summary>A short sequence of soft plucked notes.</summary>
        private static AudioClip Notes(string name, float[] frequencies, float noteSeconds, float gain, bool shimmer = false, bool bright = false)
        {
            var tail = 0.25f;
            var length = (int)(Rate * (noteSeconds * frequencies.Length + tail));
            var data = new float[length];
            for (var n = 0; n < frequencies.Length; n++)
            {
                var start = (int)(Rate * noteSeconds * n);
                var f = frequencies[n];
                for (var i = start; i < length; i++)
                {
                    var t = (i - start) / (float)Rate;
                    var envelope = Mathf.Min(1f, t / 0.004f) * Mathf.Exp(-t * (bright ? 18f : 9f));
                    var tone = Mathf.Sin(2f * Mathf.PI * f * t) + 0.3f * Mathf.Sin(4f * Mathf.PI * f * t);
                    if (shimmer)
                    {
                        tone += 0.15f * Mathf.Sin(2f * Mathf.PI * f * 3.01f * t);
                    }

                    data[i] += tone * envelope * gain;
                }
            }

            for (var i = 0; i < length; i++)
            {
                data[i] = Mathf.Clamp(data[i], -1f, 1f);
            }

            var clip = AudioClip.Create(name, length, 1, Rate, false);
            clip.hideFlags = HideFlags.DontSave;
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Six seconds of gentle water: filtered noise with slow swells, looping without a seam.</summary>
        public static AudioClip WaterAmbience()
        {
            if (_water != null && _water)
            {
                return _water;
            }

            const float seconds = 6f;
            var length = (int)(Rate * seconds);
            var data = new float[length];
            var random = new System.Random(20260929);
            float low = 0f, lower = 0f;
            for (var i = 0; i < length; i++)
            {
                var white = (float)(random.NextDouble() * 2.0 - 1.0);
                low += (white - low) * 0.02f;
                lower += (low - lower) * 0.05f;
                var t = i / (float)Rate;
                var swell = 0.55f + 0.45f * Mathf.Sin(2f * Mathf.PI * t / seconds) * Mathf.Sin(2f * Mathf.PI * t * 2f / seconds);
                data[i] = lower * 6f * swell;
            }

            // Cross-fade the ends so the loop point is inaudible.
            var fade = Rate / 2;
            for (var i = 0; i < fade; i++)
            {
                var k = i / (float)fade;
                data[i] = data[i] * k + data[length - fade + i] * (1f - k);
            }

            _water = AudioClip.Create("Água", length - fade, 1, Rate, false);
            _water.hideFlags = HideFlags.DontSave;
            var trimmed = new float[length - fade];
            System.Array.Copy(data, trimmed, trimmed.Length);
            _water.SetData(trimmed, 0);
            return _water;
        }
    }
}
