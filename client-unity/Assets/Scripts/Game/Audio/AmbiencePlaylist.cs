using System;
using System.Collections.Generic;
using FishingIdle.Game.Bootstrap;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FishingIdle.Game.Audio
{
    /// <summary>The shape of Resources/Sons/ambiente.json (read with JsonUtility).</summary>
    [Serializable]
    public sealed class AmbienceFile
    {
        public float crossfade = 8f;
        public float gain = 0.6f;
        public AmbienceScene[] scenes;
    }

    [Serializable]
    public sealed class AmbienceScene
    {
        public string scene;
        public string[] tracks;
    }

    /// <summary>
    /// The ambience of a map as a playlist of long recordings (addendum A-081): each one plays to its
    /// end and hands over to the next with a crossfade, in a shuffled order that never repeats the
    /// track that just played, so what the player hears only comes back after several minutes.
    /// </summary>
    /// <remarks>
    /// Two AudioSources take turns. The tracks are files in Resources/Sons/Ambiente listed in
    /// ambiente.json; a map with no file has no ambience.
    /// </remarks>
    public sealed class AmbiencePlaylist
    {
        private const string ConfigPath = "Sons/ambiente";
        private const string Folder = "Sons/Ambiente/";

        private readonly AudioSource[] _sources = new AudioSource[2];
        private readonly float[] _fade = new float[2];
        private readonly float[] _fadeTarget = new float[2];
        private readonly AmbienceFile _file;
        private List<AudioClip> _tracks = new List<AudioClip>();
        private string _scene;
        private int _current = -1;
        private AudioClip _last;

        public AmbiencePlaylist(GameObject owner)
        {
            for (var i = 0; i < 2; i++)
            {
                var s = owner.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.loop = false;
                s.volume = 0f;
                _sources[i] = s;
            }

            _file = Load();
        }

        /// <summary>True when the current map has at least one recording.</summary>
        public bool HasTracks => _tracks.Count > 0;

        private float Crossfade => _file != null ? Mathf.Clamp(_file.crossfade, 0.5f, 30f) : 8f;

        private float Gain => _file != null ? Mathf.Clamp01(_file.gain) : 0.6f;

        /// <summary>Switches to the recordings of a map (SceneTheme.ArtFolder); the change crossfades.</summary>
        public void SetScene(string scene)
        {
            if (scene == _scene)
            {
                return;
            }

            _scene = scene;
            _tracks = new List<AudioClip>();
            if (_file?.scenes != null)
            {
                foreach (var s in _file.scenes)
                {
                    if (s == null || s.scene != scene || s.tracks == null)
                    {
                        continue;
                    }

                    foreach (var name in s.tracks)
                    {
                        var clip = string.IsNullOrEmpty(name) ? null : Resources.Load<AudioClip>(Folder + name);
                        if (clip != null)
                        {
                            _tracks.Add(clip);
                        }
                    }
                }
            }

            _last = null;
            if (_current >= 0)
            {
                // Fade out whatever is playing; the new map's first track starts on the next update.
                _fadeTarget[_current] = 0f;
                _current = -1;
            }
        }

        /// <summary>Called every frame with the ambience level (0 when switched off, fading).</summary>
        public void Update(float level)
        {
            var dt = Time.unscaledDeltaTime;
            if (HasTracks && level > 0f)
            {
                var playing = _current >= 0 ? _sources[_current] : null;
                var ending = playing == null || !playing.isPlaying
                    || (playing.clip != null && playing.time >= playing.clip.length - Crossfade);
                if (ending)
                {
                    StartNext();
                }
            }

            for (var i = 0; i < 2; i++)
            {
                _fade[i] = Mathf.MoveTowards(_fade[i], _fadeTarget[i], dt / Crossfade);
                var s = _sources[i];
                s.volume = GameSettings.Volume * Gain * level * _fade[i];
                if (_fade[i] <= 0f && _fadeTarget[i] <= 0f && s.isPlaying)
                {
                    s.Stop();
                }
            }
        }

        private void StartNext()
        {
            var clip = Pick();
            if (clip == null)
            {
                return;
            }

            if (_current >= 0)
            {
                _fadeTarget[_current] = 0f;
            }

            _current = _current == 0 ? 1 : 0;
            var s = _sources[_current];
            s.Stop();
            s.clip = clip;
            s.time = 0f;
            s.Play();
            _fade[_current] = 0f;
            _fadeTarget[_current] = 1f;
            _last = clip;
        }

        private AudioClip Pick()
        {
            if (_tracks.Count == 0)
            {
                return null;
            }

            if (_tracks.Count == 1)
            {
                return _tracks[0];
            }

            AudioClip pick;
            do
            {
                pick = _tracks[Random.Range(0, _tracks.Count)];
            }
            while (pick == _last);

            return pick;
        }

        private static AmbienceFile Load()
        {
            var asset = Resources.Load<TextAsset>(ConfigPath);
            if (asset == null)
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<AmbienceFile>(asset.text);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[FishingIdle] Ambience list could not be read; the map stays without ambience. " + e.Message);
                return null;
            }
        }
    }
}
