using UnityEngine;

namespace FishingIdle.Game.Bootstrap
{
    /// <summary>
    /// Player preferences that are pure presentation (sound). Kept in PlayerPrefs on this PC; never part
    /// of the save, because they change nothing in the rules.
    /// </summary>
    public static class GameSettings
    {
        private const string SoundKey = "fishingidle.sound";
        private const string AmbientKey = "fishingidle.ambient";
        private const string VolumeKey = "fishingidle.volume";
        private const string ZoomKey = "fishingidle.zoom";
        private static float? _zoom;

        public static bool SoundOn
        {
            get => PlayerPrefs.GetInt(SoundKey, 1) == 1;
            set => PlayerPrefs.SetInt(SoundKey, value ? 1 : 0);
        }

        public static bool AmbientOn
        {
            get => PlayerPrefs.GetInt(AmbientKey, 1) == 1;
            set => PlayerPrefs.SetInt(AmbientKey, value ? 1 : 0);
        }

        /// <summary>0 to 1.</summary>
        public static float Volume
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 0.6f));
            set => PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value));
        }

        /// <summary>How close the camera is to the fisherman: 0 = the whole scene, 1 = the closest (A-102).</summary>
        public static float Zoom
        {
            get => _zoom ?? (_zoom = Mathf.Clamp01(PlayerPrefs.GetFloat(ZoomKey, 0f))).Value;
            set
            {
                var v = Mathf.Clamp01(value);
                if (_zoom.HasValue && Mathf.Approximately(_zoom.Value, v))
                {
                    return;
                }

                _zoom = v;
                PlayerPrefs.SetFloat(ZoomKey, v);
            }
        }
    }
}
