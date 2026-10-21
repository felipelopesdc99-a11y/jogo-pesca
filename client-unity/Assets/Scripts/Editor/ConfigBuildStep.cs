using System.IO;
using FishingIdle.Game.Bootstrap;
using FishingIdle.GameService.Config;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using T = FishingIdle.Texts.GameTexts.ProjectSetup;

namespace FishingIdle.Editor
{
    /// <summary>
    /// Copies /config into the build (StreamingAssets/config), after checking it is valid.
    /// </summary>
    /// <remarks>
    /// In the editor the game reads /config directly; a built game cannot see the repository, so it
    /// reads this copy. The copy is regenerated on every build and is not versioned (.gitignore),
    /// so /config stays the single source of truth. An invalid balance stops the build.
    /// </remarks>
    public sealed class ConfigBuildStep : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var source = GamePaths.RepositoryConfigDirectory;
            var check = GameConfigLoader.LoadFromDirectory(source);
            if (!check.Succeeded)
            {
                throw new BuildFailedException(T.BuildConfigInvalid + "\n" + string.Join("\n", check.Errors));
            }

            var target = Path.Combine("Assets", "StreamingAssets", "config");
            Directory.CreateDirectory(target);
            foreach (var file in Directory.GetFiles(source, "*.json"))
            {
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
            }

            AssetDatabase.Refresh();
        }
    }
}
