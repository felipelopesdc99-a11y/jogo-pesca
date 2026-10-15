using System.IO;
using UnityEngine;

namespace FishingIdle.Game.Bootstrap
{
    /// <summary>Where the game finds its balance files and keeps the save.</summary>
    public static class GamePaths
    {
        /// <summary>
        /// The balance files. In the editor they are read straight from the repository's /config
        /// folder, so an edit (by hand or through the Dev Panel) is what the next Play uses. In a
        /// built game they are a copy placed in StreamingAssets by the build step.
        /// </summary>
        public static string ConfigDirectory
        {
            get
            {
#if UNITY_EDITOR
                return RepositoryConfigDirectory;
#else
                return Path.Combine(Application.streamingAssetsPath, "config");
#endif
            }
        }

        /// <summary>/config at the repository root (one level above the Unity project).</summary>
        public static string RepositoryConfigDirectory => Path.GetFullPath(Path.Combine(RepositoryRoot, "config"));

        /// <summary>The repository root: the folder that contains client-unity.</summary>
        public static string RepositoryRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));

        /// <summary>The save folder, inside the per-user data folder Unity picks for this game.</summary>
        public static string SaveDirectory => Path.Combine(Application.persistentDataPath, "save");
    }
}
