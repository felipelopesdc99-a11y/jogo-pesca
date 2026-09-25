namespace FishingIdle.Api.Health;

public static class HealthStatus
{
    public const string Healthy = "healthy";
    public const string Degraded = "degraded";
    public const string Unhealthy = "unhealthy";
}

public sealed record DependencyHealth(
    string Name,
    string Status,
    double? LatencyMs,
    string? Detail);

public sealed record HealthReport(
    string Status,
    string Service,
    string Version,
    string Environment,
    DateTimeOffset ServerTimeUtc,
    IReadOnlyList<DependencyHealth> Dependencies);

public sealed record LivenessReport(
    string Status,
    string Service,
    string Version,
    DateTimeOffset ServerTimeUtc);
