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
    }
}
