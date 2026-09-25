using FishingIdle.Api.GameConfig;
using FishingIdle.Api.Roadmap;
using FishingIdle.Api.Versioning;

namespace FishingIdle.Api.Endpoints;

/// <summary>
/// Endpoints that back the private Development Console.
/// </summary>
/// <remarks>
/// These are development/admin surfaces, not gameplay. They are read-only in Milestone 0.
/// Admin authentication and role separation arrive with Milestone 1 (M1-T01, M1-T08);
/// until then the API is bound to localhost in development and must not be exposed publicly.
/// </remarks>
public static class DevEndpoints
{
    public static IEndpointRouteBuilder MapDevEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dev").WithTags("dev-console");

        group.MapGet("/roadmap", (RoadmapService roadmap) =>
            {
                var result = roadmap.Load();

                return result.Succeeded
                    ? Results.Ok(result.Response)
                    : Results.Problem(
                        title: "Roadmap indisponível",
                        detail: result.Error,
                        statusCode: StatusCodes.Status503ServiceUnavailable);
            })
            .WithName("DevRoadmap")
            .WithSummary("Roadmap state plus the computed dashboard summary, read from docs/roadmap.json.");

        group.MapGet("/version", (BuildVersionService version) => Results.Ok(version.GetBuildInfo()))
            .WithName("DevVersion")
            .WithSummary("Client, server and web versions from version.json plus runtime build facts.");

        group.MapGet("/config", (GameConfigFileService config) => Results.Ok(config.List()))
            .WithName("DevConfigListing")
            .WithSummary("The /config balance files available, with balance status and schema version.");

        group.MapGet("/config/{fileName}", (string fileName, GameConfigFileService config) =>
            {
                var contents = config.Read(fileName);

                return contents is null
                    ? Results.Problem(
                        title: "Arquivo de configuração não encontrado",
                        detail: $"Não existe um arquivo de configuração legível chamado '{fileName}' em /config.",
                        statusCode: StatusCodes.Status404NotFound)
                    : Results.Ok(contents);
            })
            .WithName("DevConfigFile")
            .WithSummary("Raw contents of one /config balance file.");

        return app;
    }
}
