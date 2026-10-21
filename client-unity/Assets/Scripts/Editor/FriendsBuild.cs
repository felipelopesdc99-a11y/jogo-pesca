using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using FishingIdle.Game.Bootstrap;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using T = FishingIdle.Texts.GameTexts.FriendsBuild;

namespace FishingIdle.Editor
{
    /// <summary>
    /// One click to make a Windows version for friends to test (TD-029): builds the main scene to
    /// /dist/FishingIdle_&lt;version&gt;, adds a LEIA-ME, zips it and opens the folder. /dist is not versioned.
    /// </summary>
    public static class FriendsBuild
    {
        [MenuItem(T.Menu, priority = 40)]
        public static void BuildForFriends()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
            {
                EditorUtility.DisplayDialog(T.Title, T.NoWindowsSupport, T.Ok);
                return;
            }

            var version = ClientVersion();
            var dist = Path.Combine(GamePaths.RepositoryRoot, "dist");
            var folderName = "FishingIdle_" + version;
            var folder = Path.Combine(dist, folderName);
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }

            Directory.CreateDirectory(folder);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.MainScenePath },
                locationPathName = Path.Combine(folder, "FishingIdle.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                EditorUtility.DisplayDialog(T.Title, T.Failed, T.Ok);
                return;
            }

            // Unity's debug symbols folder is not needed by players.
            foreach (var junk in Directory.GetDirectories(folder, "*_BurstDebugInformation_DoNotShip"))
            {
                Directory.Delete(junk, true);
            }

            File.WriteAllText(Path.Combine(folder, "LEIA-ME.txt"), T.Readme(version), new UTF8Encoding(true));

            var zipPath = Path.Combine(dist, folderName + ".zip");
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            Zip(folder, zipPath, folderName);
            var sizeMb = new FileInfo(zipPath).Length / (1024.0 * 1024.0);
            Debug.Log("[FishingIdle] Friends build ready: " + zipPath + " (" + sizeMb.ToString("0.0") + " MB).");
            EditorUtility.RevealInFinder(zipPath);
            EditorUtility.DisplayDialog(T.Title, T.Done(folderName + ".zip", Texts.Format.Decimal(sizeMb, 1)), T.Ok);
        }

        /// <summary>The client version in /version.json ("0.2.0-m17.5"), or "dev" when it cannot be read.</summary>
        private static string ClientVersion()
        {
            try
            {
                var json = File.ReadAllText(Path.Combine(GamePaths.RepositoryRoot, "version.json"));
                var match = Regex.Match(json, "\"client\"\\s*:\\s*\"([^\"]+)\"");
                return match.Success ? match.Groups[1].Value : "dev";
            }
            catch (Exception)
            {
                return "dev";
            }
        }

        private static void Zip(string folder, string zipPath, string rootName)
        {
            using (var zip = new ZipArchive(File.Create(zipPath), ZipArchiveMode.Create))
            {
                foreach (var file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
                {
                    var relative = file.Substring(folder.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    var entry = zip.CreateEntry(rootName + "/" + relative.Replace('\\', '/'), System.IO.Compression.CompressionLevel.Optimal);
                    using (var input = File.OpenRead(file))
                    using (var output = entry.Open())
                    {
                        input.CopyTo(output);
                    }
                }
            }
        }
    }
}
