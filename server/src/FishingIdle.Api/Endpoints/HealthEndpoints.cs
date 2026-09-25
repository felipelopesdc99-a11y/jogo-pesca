using FishingIdle.Api.Health;

namespace FishingIdle.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        // Full health: reports the API plus every dependency it needs to serve gameplay.
        // Always 200 so a monitoring client can read the body and tell which dependency is down;
        // the overall "status" field carries the verdict.
        app.MapGet("/health", async (HealthService health, CancellationToken cancellationToken)
                => Results.Ok(await health.GetHealthAsync(cancellationToken)))
            .WithName("Health")
            .WithSummary("API status with a per-dependency breakdown.");

        // Liveness: deliberately touches nothing. Answering here while /health reports a degraded
        // database is the signal "server up, database down", which the Unity diagnostic surface shows.
        app.MapGet("/health/live", (HealthService health) => Results.Ok(health.GetLiveness()))
            .WithName("Liveness")
            .WithSummary("Dependency-free liveness signal.");

        return app;
    }
}
