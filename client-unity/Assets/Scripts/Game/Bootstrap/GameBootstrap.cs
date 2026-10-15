using UnityEngine;

namespace FishingIdle.Game.Bootstrap
{
    /// <summary>
    /// Makes pressing Play start the game from any scene, including an empty one.
    /// </summary>
    /// <remarks>
    /// The whole fishing scene is built from code (docs/DECISOES.md, TD-016), so a scene file only
    /// needs to exist for builds. If a scene already contains a GameRoot, nothing is added.
    /// </remarks>
    public static class GameBootstrap
    {
        private const string RootObjectName = "[Fishing Idle]";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureGameRoot()
        {
            if (Object.FindAnyObjectByType<GameRoot>() != null)
            {
                return;
            }

            var root = new GameObject(RootObjectName);
            Object.DontDestroyOnLoad(root);
            root.AddComponent<GameRoot>();
        }
    }
}
