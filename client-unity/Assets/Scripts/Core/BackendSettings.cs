using UnityEngine;

namespace FishingIdle.Client.Core
{
    /// <summary>
    /// Where the client finds the authoritative backend.
    /// </summary>
    /// <remarks>
    /// The endpoint lives in an asset rather than in code so switching between a local server and a
    /// hosted one never requires a code change. Nothing here is a game rule: the server owns every
    /// rule, and the client only needs to know which server to ask.
    /// </remarks>
    [CreateAssetMenu(
        fileName = "BackendSettings",
        menuName = "Fishing Idle/Backend Settings",
        order = 0)]
    public sealed class BackendSettings : ScriptableObject
    {
        [Header("Connection")]
        [Tooltip("Base URL of the Fishing Idle backend, with no trailing slash.")]
        [SerializeField]
        private string baseUrl = "http://localhost:5080";

        [Tooltip("Seconds to wait for a response before treating the request as failed.")]
        [SerializeField]
        [Range(1, 30)]
        private int requestTimeoutSeconds = 5;

        [Header("Health probe")]
        [Tooltip("Seconds between health probes while the client is running.")]
        [SerializeField]
        [Range(2, 120)]
        private int healthPollSeconds = 10;

        public string BaseUrl => string.IsNullOrWhiteSpace(baseUrl)
            ? "http://localhost:5080"
            : baseUrl.TrimEnd('/');

        public int RequestTimeoutSeconds => requestTimeoutSeconds;

        public int HealthPollSeconds => healthPollSeconds;

        /// <summary>Environment variable that overrides the configured base URL.</summary>
        public const string BaseUrlEnvironmentVariable = "FISHING_IDLE_API_BASE_URL";

        /// <summary>
        /// Resolves the settings to use, in order: the environment variable, then the
        /// <c>Assets/Resources/BackendSettings.asset</c> file, then built-in defaults.
        /// </summary>
        /// <remarks>
        /// The defaults match the local Docker Compose stack, so a fresh checkout runs without any
        /// asset having been created. The environment variable exists so a build can be pointed at a
        /// different server without reopening the editor.
        /// </remarks>
        public static BackendSettings LoadOrDefault()
        {
            var asset = Resources.Load<BackendSettings>("BackendSettings");
            var settings = asset != null ? Instantiate(asset) : CreateInstance<BackendSettings>();
            settings.name = asset != null ? "BackendSettings (asset)" : "BackendSettings (defaults)";

            var overrideUrl = System.Environment.GetEnvironmentVariable(BaseUrlEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(overrideUrl))
            {
                settings.baseUrl = overrideUrl;
                settings.name += $" + {BaseUrlEnvironmentVariable}";
            }

            return settings;
        }
    }
}
