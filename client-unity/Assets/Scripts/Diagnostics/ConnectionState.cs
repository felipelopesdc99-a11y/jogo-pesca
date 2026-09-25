using System;
using System.Collections.Generic;
using System.Text;
using FishingIdle.Client.Core;

namespace FishingIdle.Client.Diagnostics
{
    /// <summary>How the client currently sees the backend.</summary>
    public enum ConnectionStatus
    {
        /// <summary>No probe has completed yet.</summary>
        Unknown,

        /// <summary>The server answered and every dependency it needs is healthy.</summary>
        Healthy,

        /// <summary>The server answered but a dependency it needs is not healthy.</summary>
        Degraded,

        /// <summary>The server could not be reached at all.</summary>
        Unreachable,
    }

    /// <summary>
    /// The latest health reading, shaped for display.
    /// </summary>
    /// <remarks>
    /// This is a snapshot of what the server said, never a source of truth about gameplay.
    /// </remarks>
    public sealed class ConnectionState
    {
        public ConnectionStatus Status { get; private set; } = ConnectionStatus.Unknown;

        public string ServerVersion { get; private set; }

        public string Environment { get; private set; }

        public float LatencySeconds { get; private set; }

        public DateTimeOffset? LastProbeAtUtc { get; private set; }

        public string Message { get; private set; } = "Waiting for the first health probe…";

        public IReadOnlyList<DependencyHealthDto> Dependencies { get; private set; }
            = Array.Empty<DependencyHealthDto>();

        public void ApplySuccess(HealthReportDto report, float latencySeconds)
        {
            Status = string.Equals(report.status, "healthy", StringComparison.OrdinalIgnoreCase)
                ? ConnectionStatus.Healthy
                : ConnectionStatus.Degraded;

            ServerVersion = report.version;
            Environment = report.environment;
            LatencySeconds = latencySeconds;
            LastProbeAtUtc = DateTimeOffset.UtcNow;
            Dependencies = report.dependencies ?? new List<DependencyHealthDto>();
            Message = Status == ConnectionStatus.Healthy
                ? "Server and database healthy."
                : BuildDegradedMessage(Dependencies);
        }

        public void ApplyFailure(string error, float latencySeconds)
        {
            Status = ConnectionStatus.Unreachable;
            ServerVersion = null;
            Environment = null;
            LatencySeconds = latencySeconds;
            LastProbeAtUtc = DateTimeOffset.UtcNow;
            Dependencies = Array.Empty<DependencyHealthDto>();
            Message = error;
        }

        /// <summary>Names which dependency is unhealthy, so "degraded" is actionable.</summary>
        private static string BuildDegradedMessage(IReadOnlyList<DependencyHealthDto> dependencies)
        {
            var builder = new StringBuilder("Server is up but a dependency is not: ");
            var first = true;

            foreach (var dependency in dependencies)
            {
                if (string.Equals(dependency.status, "healthy", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!first)
                {
                    builder.Append("; ");
                }

                builder.Append(dependency.name);
                if (!string.IsNullOrWhiteSpace(dependency.detail))
                {
                    builder.Append(" — ").Append(dependency.detail);
                }

                first = false;
            }

            return first ? "Server reported a degraded status." : builder.ToString();
        }
    }
}
