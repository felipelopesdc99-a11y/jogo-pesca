using System.Collections.Generic;
using FishingIdle.Game.Bootstrap;
using FishingIdle.Game.UI;
using UnityEngine;

namespace FishingIdle.Game.Audio
{
    /// <summary>
    /// Placeholder audio (START HERE M10): short sounds synthesised in code for each kind of notice,
    /// and a soft water ambience. No audio files to import; the real sounds replace these later by
    /// swapping the clips built in <see cref="SoundBank"/>.
    /// </summary>
    public sealed class GameAudio : MonoBehaviour
    {
        private const float MinGapSeconds = 0.12f;

        private GameRoot _root;
        private AudioSource _effects;
        private AudioSource _ambient;
        private readonly Dictionary<ToastKind, float> _lastPlayed = new Dictionary<ToastKind, float>();

        private void Start()
        {
            _root = GetComponent<GameRoot>();
            _effects = gameObject.AddComponent<AudioSource>();
            _effects.playOnAwake = false;
            _ambient = gameObject.AddComponent<AudioSource>();
            _ambient.playOnAwake = false;
            _ambient.loop = true;
            _ambient.clip = SoundBank.WaterAmbience();

            if (_root != null)
            {
                _root.Toasts.Pushed += OnToast;
            }
        }

        private void OnDestroy()
        {
            if (_root != null)
            {
                _root.Toasts.Pushed -= OnToast;
            }
        }

        private void Update()
        {
            var ambientWanted = GameSettings.SoundOn && GameSettings.AmbientOn;
            _ambient.volume = GameSettings.Volume * 0.35f;
            if (ambientWanted && !_ambient.isPlaying)
            {
                _ambient.Play();
            }
            else if (!ambientWanted && _ambient.isPlaying)
            {
                _ambient.Stop();
            }
        }

        private void OnToast(ToastKind kind)
        {
            if (!GameSettings.SoundOn)
            {
                return;
            }

            // A burst of notices plays one sound per kind, not a pile-up.
            if (_lastPlayed.TryGetValue(kind, out var last) && Time.unscaledTime - last < MinGapSeconds)
            {
                return;
            }

            _lastPlayed[kind] = Time.unscaledTime;
            _effects.PlayOneShot(SoundBank.For(kind), GameSettings.Volume);
        }
    }
}
