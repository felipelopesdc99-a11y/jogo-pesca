using System;
using FishingIdle.Client.Core;
using UnityEngine;

namespace FishingIdle.Client.Diagnostics
{
    /// <summary>
    /// Development-only overlay showing whether the client is talking to the authoritative backend.
    /// </summary>
    /// <remarks>
    /// Compiled out of release player builds. Toggle with F1; it starts visible in the editor so the
    /// connection state is the first thing visible when pressing Play. It is deliberately plain: the
    /// game's own UI is a separate, designed surface (GDD section 44) and this must never be mistaken
    /// for it.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class DiagnosticOverlay : MonoBehaviour
    {
        private const KeyCode ToggleKey = KeyCode.F1;
        private const int PanelWidth = 470;

        private static readonly Color HealthyColor = new Color(0.25f, 0.75f, 0.35f);
        private static readonly Color DegradedColor = new Color(0.85f, 0.68f, 0.18f);
        private static readonly Color UnreachableColor = new Color(0.90f, 0.32f, 0.29f);
        private static readonly Color UnknownColor = new Color(0.55f, 0.58f, 0.62f);

        private ServerHealthProbe _probe;
        private bool _visible = true;
        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private Texture2D _panelTexture;

        private void Awake()
        {
            _probe = GetComponent<ServerHealthProbe>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(ToggleKey))
            {
                _visible = !_visible;
            }
        }

        private void OnDestroy()
        {
            if (_panelTexture != null)
            {
                Destroy(_panelTexture);
            }
        }

        private void OnGUI()
        {
            if (!_visible || _probe == null)
            {
                return;
            }

            EnsureStyles();

            var state = _probe.State;
            var area = new Rect(12, 12, PanelWidth, 0);

            GUILayout.BeginArea(area, _panelStyle);
            GUILayout.BeginVertical();

            var previous = GUI.contentColor;
            GUI.contentColor = ColorFor(state.Status);
            GUILayout.Label($"Backend: {Describe(state.Status)}", _titleStyle);
            GUI.contentColor = previous;

            GUILayout.Label(state.Message, _bodyStyle);
            GUILayout.Space(4);
            GUILayout.Label($"Endpoint   {_probe.BaseUrl}", _bodyStyle);

            if (!string.IsNullOrEmpty(state.ServerVersion))
            {
                GUILayout.Label(
                    $"Server     {state.ServerVersion}  ({state.Environment})",
                    _bodyStyle);
            }

            if (state.LastProbeAtUtc.HasValue)
            {
                GUILayout.Label(
                    $"Last probe {state.LastProbeAtUtc.Value:HH:mm:ss} UTC  ·  " +
                    $"{state.LatencySeconds * 1000f:0} ms",
                    _bodyStyle);
            }

            foreach (var dependency in state.Dependencies)
            {
                var latency = dependency.latency_ms.HasValue
                    ? $"{dependency.latency_ms.Value:0} ms"
                    : "—";
                GUILayout.Label($"  {dependency.name}: {dependency.status}  ({latency})", _bodyStyle);
            }

            GUILayout.Space(4);
            GUILayout.Label($"F1 hides this panel  ·  development builds only", _bodyStyle);

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private static string Describe(ConnectionStatus status)
        {
            switch (status)
            {
                case ConnectionStatus.Healthy: return "connected";
                case ConnectionStatus.Degraded: return "degraded";
                case ConnectionStatus.Unreachable: return "unreachable";
                default: return "checking…";
            }
        }

        private static Color ColorFor(ConnectionStatus status)
        {
            switch (status)
            {
                case ConnectionStatus.Healthy: return HealthyColor;
                case ConnectionStatus.Degraded: return DegradedColor;
                case ConnectionStatus.Unreachable: return UnreachableColor;
                default: return UnknownColor;
            }
        }

        private void EnsureStyles()
        {
            if (_panelStyle != null)
            {
                return;
            }

            _panelTexture = new Texture2D(1, 1);
            _panelTexture.SetPixel(0, 0, new Color(0.04f, 0.06f, 0.09f, 0.90f));
            _panelTexture.Apply();

            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 10, 10),
                alignment = TextAnchor.UpperLeft,
            };
            _panelStyle.normal.background = _panelTexture;

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
            };

            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
            };
            _bodyStyle.normal.textColor = new Color(0.85f, 0.88f, 0.92f);
        }
    }
}
