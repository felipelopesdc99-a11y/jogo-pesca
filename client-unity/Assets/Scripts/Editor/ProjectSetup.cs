using System.IO;
using System.Linq;
using FishingIdle.Game.Bootstrap;
using FishingIdle.Texts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using T = FishingIdle.Texts.GameTexts.ProjectSetup;

namespace FishingIdle.Editor
{
    /// <summary>
    /// First-open setup, so the owner only has to open the project and press Play.
    /// </summary>
    /// <remarks>
    /// Runs every time scripts load and only acts on what is missing: it creates the main scene
    /// (Assets/Scenes/Principal.unity) and registers it for builds, opens it when the editor is
    /// showing an empty untitled scene, and sets the product/company names the first time (they
    /// decide where the save folder lives). It never overwrites something the owner changed.
    /// </remarks>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        public const string MainScenePath = "Assets/Scenes/Principal.unity";

        static ProjectSetup()
        {
            EditorApplication.delayCall += Run;
        }

        [MenuItem(T.OpenSceneMenu, priority = 20)]
        public static void OpenMainScene()
        {
            EnsureMainScene();
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(MainScenePath);
            }
        }

        [MenuItem(T.OpenSaveFolderMenu, priority = 21)]
        public static void OpenSaveFolder()
        {
            Directory.CreateDirectory(GamePaths.SaveDirectory);
            EditorUtility.RevealInFinder(GamePaths.SaveDirectory);
        }

        private static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            ApplyFirstTimePlayerSettings();
            EnsureMainScene();

            // Replace an empty, unsaved "Untitled" scene with the main scene.
            var active = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(active.path) && !active.isDirty && File.Exists(MainScenePath))
            {
                EditorSceneManager.OpenScene(MainScenePath);
            }
        }

        private static void EnsureMainScene()
        {
            if (!File.Exists(MainScenePath))
            {
                var active = SceneManager.GetActiveScene();
                if (active.isDirty)
                {
                    // Never throw away unsaved work; try again on the next script reload.
                    return;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(MainScenePath));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, MainScenePath);
                Debug.Log("[FishingIdle] " + T.SceneCreated + " " + MainScenePath);
            }

            var scenes = EditorBuildSettings.scenes;
            if (scenes.All(s => s.path != MainScenePath))
            {
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MainScenePath, true) }.Concat(scenes).ToArray();
            }
        }

        private static void ApplyFirstTimePlayerSettings()
        {
            if (PlayerSettings.companyName != "DefaultCompany")
            {
                return;
            }

            PlayerSettings.companyName = T.CompanyName;
            PlayerSettings.productName = GameTexts.GameTitle;
            PlayerSettings.runInBackground = true;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.resizableWindow = true;

            var version = RepositoryFiles.Read<VersionDocument>(RepositoryFiles.VersionPath, out _);
            if (version?.Components != null && version.Components.TryGetValue("client", out var client))
            {
                PlayerSettings.bundleVersion = client;
            }

            Debug.Log("[FishingIdle] " + T.PlayerSettingsApplied);
        }
    }
}
