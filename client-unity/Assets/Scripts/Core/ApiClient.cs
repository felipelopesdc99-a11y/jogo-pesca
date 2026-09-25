using System;
using System.Collections;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace FishingIdle.Client.Core
{
    /// <summary>The outcome of one API call.</summary>
    /// <typeparam name="T">Deserialised response type.</typeparam>
    public sealed class ApiResult<T>
    {
        private ApiResult(bool succeeded, T value, string error, long statusCode, float latencySeconds)
        {
            Succeeded = succeeded;
            Value = value;
            Error = error;
            StatusCode = statusCode;
            LatencySeconds = latencySeconds;
        }

        public bool Succeeded { get; }

        public T Value { get; }

        public string Error { get; }

        public long StatusCode { get; }

        public float LatencySeconds { get; }

        public static ApiResult<T> Ok(T value, long statusCode, float latencySeconds)
            => new ApiResult<T>(true, value, null, statusCode, latencySeconds);

        public static ApiResult<T> Fail(string error, long statusCode, float latencySeconds)
            => new ApiResult<T>(false, default, error, statusCode, latencySeconds);
    }

    /// <summary>
    /// Minimal HTTP access to the authoritative backend.
    /// </summary>
    /// <remarks>
    /// The client sends intent and reads authoritative results. It never computes a result with
    /// economic, progression or competitive value, and never tells the server what happened.
    /// Milestone 0 only needs GET; authenticated requests and idempotency keys arrive with
    /// Milestone 1 (M1-T01, M1-T09).
    /// </remarks>
    public sealed class ApiClient
    {
        private readonly BackendSettings _settings;

        public ApiClient(BackendSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public string BaseUrl => _settings.BaseUrl;

        /// <summary>
        /// Issues a GET and hands the parsed response to <paramref name="onComplete"/>.
        /// </summary>
        /// <param name="route">Route starting with a slash, e.g. <c>/health</c>.</param>
        public IEnumerator Get<T>(string route, Action<ApiResult<T>> onComplete)
        {
            var url = _settings.BaseUrl + route;
            var startedAt = Time.realtimeSinceStartup;

            using (var request = UnityWebRequest.Get(url))
            {
                request.timeout = _settings.RequestTimeoutSeconds;
                request.SetRequestHeader("Accept", "application/json");

                yield return request.SendWebRequest();

                var latency = Time.realtimeSinceStartup - startedAt;

                if (request.result != UnityWebRequest.Result.Success)
                {
                    onComplete?.Invoke(ApiResult<T>.Fail(
                        DescribeTransportFailure(request, url),
                        request.responseCode,
                        latency));
                    yield break;
                }

                T parsed;
                try
                {
                    parsed = JsonConvert.DeserializeObject<T>(request.downloadHandler.text);
                }
                catch (JsonException exception)
                {
                    onComplete?.Invoke(ApiResult<T>.Fail(
                        $"A resposta de {route} não pôde ser interpretada: {exception.Message}",
                        request.responseCode,
                        latency));
                    yield break;
                }

                if (parsed == null)
                {
                    onComplete?.Invoke(ApiResult<T>.Fail(
                        $"A resposta de {route} veio vazia.",
                        request.responseCode,
                        latency));
                    yield break;
                }

                onComplete?.Invoke(ApiResult<T>.Ok(parsed, request.responseCode, latency));
            }
        }

        /// <summary>
        /// Turns a transport failure into a message that names the likely cause, so the owner can
        /// tell "nothing is listening" from "the server answered an error".
        /// </summary>
        private static string DescribeTransportFailure(UnityWebRequest request, string url)
        {
            switch (request.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                    return $"Não foi possível alcançar {url}. O servidor está rodando? (ops/scripts/dev-up.sh)";
                case UnityWebRequest.Result.ProtocolError:
                    return $"{url} respondeu HTTP {request.responseCode}.";
                case UnityWebRequest.Result.DataProcessingError:
                    return $"A resposta de {url} não pôde ser processada: {request.error}";
                default:
                    return $"A requisição para {url} falhou: {request.error}";
            }
        }
    }
}
