using System.Collections;
using FishingIdle.Client.Core;
using UnityEngine;

namespace FishingIdle.Client.Diagnostics
{
    /// <summary>
    /// Polls <c>/health</c> and keeps a <see cref="ConnectionState"/> up to date.
    /// </summary>
    /// <remarks>
    /// This exists so the owner can confirm the client reaches the authoritative backend. It is a
    /// diagnostic, not gameplay: nothing in the game may branch on this state to decide an outcome.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ServerHealthProbe : MonoBehaviour
    {
        private BackendSettings _settings;
        private ApiClient _client;

        public ConnectionState State { get; } = new ConnectionState();

        /// <summary>Raised after every probe so a UI can refresh without polling this object.</summary>
        public event System.Action<ConnectionState> StateChanged;

        private void Awake()
        {
            _settings = BackendSettings.LoadOrDefault();
            _client = new ApiClient(_settings);
        }

        private void OnEnable()
        {
            StartCoroutine(PollLoop());
        }

        public string BaseUrl => _client?.BaseUrl ?? "(not configured)";

        private IEnumerator PollLoop()
        {
            while (enabled)
            {
                yield return ProbeOnce();
                yield return new WaitForSecondsRealtime(_settings.HealthPollSeconds);
            }
        }

        private IEnumerator ProbeOnce()
        {
            yield return _client.Get<HealthReportDto>("/health", result =>
            {
                if (result.Succeeded)
                {
                    State.ApplySuccess(result.Value, result.LatencySeconds);
                    Debug.Log($"[FishingIdle] Health: {State.Status} — {State.Message} " +
                              $"(server {State.ServerVersion}, {result.LatencySeconds * 1000f:0} ms)");
                }
                else
                {
                    State.ApplyFailure(result.Error, result.LatencySeconds);
                    Debug.LogWarning($"[FishingIdle] Health probe failed: {result.Error}");
                }

                StateChanged?.Invoke(State);
            });
        }
    }
}
