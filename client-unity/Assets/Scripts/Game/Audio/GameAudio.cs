using System.Collections.Generic;
using FishingIdle.Game.Bootstrap;
using UnityEngine;

namespace FishingIdle.Game.Audio
{
    /// <summary>
    /// Plays the game's sounds: a short effect for each notice (catch, rare catch, record, level up,
    /// coins…) and the ambience of the map: long recordings in a shuffled playlist
    /// (<see cref="AmbiencePlaylist"/>) made from real nature sounds (A-137). The sounds themselves are
    /// files (see <see cref="SoundBank"/>); the volume and on/off switches are in Opções.
    /// </summary>
    public sealed class GameAudio : MonoBehaviour
    {
        private const float MinGapSeconds = 0.12f;
        private const float AmbientFadeSeconds = 3f;

        private GameRoot _root;
        private AudioSource _effects;
        private AmbiencePlaylist _playlist;
        private float _ambientLevel;
        private SoundCue _pending;
        private readonly Dictionary<SoundCue, float> _lastPlayed = new Dictionary<SoundCue, float>();

        private void Start()
        {
            _root = GetComponent<GameRoot>();
            _effects = gameObject.AddComponent<AudioSource>();
            _effects.playOnAwake = false;
            _playlist = new AmbiencePlaylist(gameObject);

            if (_root != null)
            {
                _root.Toasts.Pushed += Play;
                _root.MapChanged += OnMapChanged;
                OnMapChanged(_root.Player?.MapId);
            }
        }

        private void OnDestroy()
        {
            if (_root != null)
            {
                _root.Toasts.Pushed -= Play;
                _root.MapChanged -= OnMapChanged;
            }
        }

        private void OnMapChanged(string mapId)
        {
            _playlist?.SetScene(Scene.SceneTheme.For(mapId).ArtFolder);
        }

        private void Update()
        {
            // The ambience fades in on start and when switched on, and fades out when switched off.
            var wanted = GameSettings.SoundOn && GameSettings.AmbientOn ? 1f : 0f;
            _ambientLevel = Mathf.MoveTowards(_ambientLevel, wanted, Time.unscaledDeltaTime / AmbientFadeSeconds);
            _playlist.Update(_ambientLevel);
        }

        /// <summary>
        /// Asks for the effect of a notice. Notices that arrive together (a catch that also levels you
        /// up) play only the most important sound, once.
        /// </summary>
        public void Play(SoundCue cue)
        {
            if (Priority(cue) > Priority(_pending))
            {
                _pending = cue;
            }
        }

        private void LateUpdate()
        {
            var cue = _pending;
            _pending = SoundCue.None;
            if (!GameSettings.SoundOn || cue == SoundCue.None)
            {
                return;
            }

            if (_lastPlayed.TryGetValue(cue, out var last) && Time.unscaledTime - last < MinGapSeconds)
            {
                return;
            }

            var clip = SoundBank.For(cue);
            if (clip == null)
            {
                return;
            }

            _lastPlayed[cue] = Time.unscaledTime;
            _effects.PlayOneShot(clip, GameSettings.Volume);
        }

        private static int Priority(SoundCue cue)
        {
            switch (cue)
            {
                case SoundCue.Record: return 8;
                case SoundCue.LevelUp: return 7;
                case SoundCue.RareCatch: return 6;
                case SoundCue.Important: return 5;
                case SoundCue.Coins: return 4;
                case SoundCue.Catch: return 3;
                case SoundCue.Warning: return 2;
                case SoundCue.Click: return 1;
                default: return 0;
            }
        }
    }
}
