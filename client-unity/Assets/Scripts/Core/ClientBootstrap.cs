using FishingIdle.Client.Diagnostics;
using UnityEngine;

namespace FishingIdle.Client.Core
{
    /// <summary>
    /// Installs the persistent client services when the game starts.
    /// </summary>
    /// <remarks>
    /// Dormant since the local-MVP pivot (docs/DECISOES.md, TD-015): the game no longer needs a
    /// remote server, so nothing calls this automatically any more. The code is kept as the starting
    /// point for the future Remote...Service implementations, which will need the same HTTP access
    /// and connection diagnostics. Call <see cref="Install"/> by hand to bring the probe back.
    /// </remarks>
    public static class ClientBootstrap
    {
        private const string RootObjectName = "[FishingIdle.Remote]";

        public static void Install()
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
