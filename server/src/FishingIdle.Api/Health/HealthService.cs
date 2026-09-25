using System.Diagnostics;
using FishingIdle.Api.Persistence;
using FishingIdle.Api.Versioning;
using Microsoft.EntityFrameworkCore;

namespace FishingIdle.Api.Health;

/// <summary>
/// Builds the health report served at <c>/health</c>.
/// </summary>
/// <remarks>
/// The report distinguishes "the API process is up" from "the API can reach its database",
/// because the Unity client's diagnostic surface needs to tell the owner which one is broken.
/// </remarks>
public sealed class HealthService
{
    public const string ServiceName = "fishing-idle-api";

    private static readonly TimeSpan DatabaseProbeTimeout = TimeSpan.FromSeconds(3);

    private readonly FishingIdleDbContext _dbContext;
    private readonly BuildVersionService _versionService;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<HealthService> _logger;

    public HealthService(
        FishingIdleDbContext dbContext,
        BuildVersionService versionService,
        IHostEnvironment environment,
        ILogger<HealthService> logger)
    {
        _dbContext = dbContext;
        _versionService = versionService;
        _environment = environment;
        _logger = logger;
    }

    public LivenessReport GetLiveness() => new(
        HealthStatus.Healthy,
        ServiceName,
        _versionService.ServerVersion,
        DateTimeOffset.UtcNow);

    public async Task<HealthReport> GetHealthAsync(CancellationToken cancellationToken)
    {
        var database = await ProbeDatabaseAsync(cancellationToken);
        var dependencies = new[] { database };

        var status = dependencies.Any(d => d.Status == HealthStatus.Unhealthy)
            ? HealthStatus.Degraded
            : HealthStatus.Healthy;

        return new HealthReport(
            status,
            ServiceName,
            _versionService.ServerVersion,
            _environment.EnvironmentName,
            DateTimeOffset.UtcNow,
            dependencies);
    }

    private async Task<DependencyHealth> ProbeDatabaseAsync(CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(DatabaseProbeTimeout);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(timeout.Token);
            stopwatch.Stop();

            return canConnect
                ? new DependencyHealth("database", HealthStatus.Healthy, Round(stopwatch), null)
                : new DependencyHealth("database", HealthStatus.Unhealthy, Round(stopwatch),
                    "PostgreSQL refused the connection. Is the database container running?");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            return new DependencyHealth("database", HealthStatus.Unhealthy, Round(stopwatch),
                $"PostgreSQL did not answer within {DatabaseProbeTimeout.TotalSeconds:0}s.");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Database health probe failed.");

            // The message is safe to surface: it names the failure, never the credentials.
            return new DependencyHealth("database", HealthStatus.Unhealthy, Round(stopwatch), ex.GetType().Name);
        }
    }

    private static double Round(Stopwatch stopwatch) => Math.Round(stopwatch.Elapsed.TotalMilliseconds, 1);
}
