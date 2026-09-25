using FishingIdle.Client.Diagnostics;
using UnityEngine;

namespace FishingIdle.Client.Core
{
    /// <summary>
    /// Installs the persistent client services when the game starts.
    /// </summary>
    /// <remarks>
    /// Bootstrapping happens in code rather than from a scene asset so pressing Play in any scene
    /// brings up the health probe and the diagnostic overlay. The first designed scene — Map 1,
    /// Lago Sereno — is task M2-T01.
    /// </remarks>
    public static class ClientBootstrap
    {
        private const string RootObjectName = "[FishingIdle]";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            var root = new GameObject(RootObjectName);
            Object.DontDestroyOnLoad(root);

            root.AddComponent<ServerHealthProbe>();

            // The overlay is a development aid and is compiled out of release player builds.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            root.AddComponent<DiagnosticOverlay>();
#endif

            var settings = BackendSettings.LoadOrDefault();
            Debug.Log($"[FishingIdle] Client bootstrapped. Backend: {settings.BaseUrl}");
        }
    }
}
