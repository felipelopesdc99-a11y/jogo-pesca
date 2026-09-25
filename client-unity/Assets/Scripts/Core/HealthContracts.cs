using System;
using System.Collections.Generic;

namespace FishingIdle.Client.Core
{
    /// <summary>
    /// Client-side mirrors of the server's health payloads.
    /// </summary>
    /// <remarks>
    /// Field names are snake_case to match what the API puts on the wire. These are
    /// presentation contracts only — nothing here is trusted as a game rule.
    /// </remarks>
    [Serializable]
    public sealed class DependencyHealthDto
    {
        public string name;
        public string status;
        public double? latency_ms;
        public string detail;
    }

    [Serializable]
    public sealed class HealthReportDto
    {
        public string status;
        public string service;
        public string version;
        public string environment;
        public DateTimeOffset server_time_utc;
        public List<DependencyHealthDto> dependencies;
    }
}
