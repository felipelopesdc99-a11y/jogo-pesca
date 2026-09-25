using System.Text.Json;
using System.Text.Json.Serialization;
using FishingIdle.Api.Configuration;
using FishingIdle.Api.Endpoints;
using FishingIdle.Api.GameConfig;
using FishingIdle.Api.Health;
using FishingIdle.Api.Persistence;
using FishingIdle.Api.Roadmap;
using FishingIdle.Api.Versioning;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Environment variables use the FISHINGIDLE_ prefix, e.g.
// FISHINGIDLE_ConnectionStrings__Postgres, FISHINGIDLE_Repository__RootPath.
builder.Configuration.AddEnvironmentVariables(prefix: "FISHINGIDLE_");

// ---------------------------------------------------------------- logging
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
    options.UseUtcTimestamp = true;
});

// ---------------------------------------------------------------- options
builder.Services
    .AddOptions<RepositoryOptions>()
    .Bind(builder.Configuration.GetSection(RepositoryOptions.SectionName))
    .ValidateOnStart();

builder.Services
    .AddOptions<DevConsoleOptions>()
    .Bind(builder.Configuration.GetSection(DevConsoleOptions.SectionName))
    .ValidateOnStart();

// ---------------------------------------------------------------- services
builder.Services.AddSingleton<RepositoryRootLocator>();
builder.Services.AddSingleton<RoadmapService>();
builder.Services.AddSingleton<BuildVersionService>();
builder.Services.AddSingleton<GameConfigFileService>();
builder.Services.AddScoped<HealthService>();

builder.Services.AddDbContext<FishingIdleDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Postgres")
        ?? throw new InvalidOperationException(
            "Nenhuma string de conexão com o PostgreSQL foi configurada. Defina ConnectionStrings:Postgres "
            + "(variável de ambiente FISHINGIDLE_ConnectionStrings__Postgres), ou copie .env.example para .env "
            + "e suba o ambiente com ops/scripts/dev-up.sh.");

    options.UseNpgsql(connectionString);
});

builder.Services.AddProblemDetails();

// JSON on the wire is snake_case, matching the repository's JSON files and the Dev Console's
// TypeScript contracts, so no field has two spellings across the project.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.DictionaryKeyPolicy = null;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
    options.SerializerOptions.WriteIndented = builder.Environment.IsDevelopment();
});

// The Dev Console renders server-side, but its browser-side refresh calls the API directly,
// so the configured console origins are allowed. No wildcard origin is ever used.
var devConsoleOrigins = builder.Configuration
    .GetSection(DevConsoleOptions.SectionName)
    .Get<DevConsoleOptions>()?.AllowedOrigins ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(DevConsoleOptions.CorsPolicyName, policy => policy
        .WithOrigins(devConsoleOrigins)
        .AllowAnyHeader()
        .WithMethods("GET"));
});

var app = builder.Build();

// ---------------------------------------------------------------- pipeline
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors(DevConsoleOptions.CorsPolicyName);

app.MapHealthEndpoints();
app.MapDevEndpoints();

app.MapGet("/", (BuildVersionService version) => Results.Ok(new
{
    service = HealthService.ServiceName,
    version = version.ServerVersion,
    docs = new[] { "/health", "/health/live", "/api/dev/roadmap", "/api/dev/version", "/api/dev/config" },
})).ExcludeFromDescription();

// Migrations are applied deliberately, never as a side effect of a deployment.
// ops/scripts/migrate.sh runs `dotnet ef database update`; see docs/API.md.
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var locator = scope.ServiceProvider.GetRequiredService<RepositoryRootLocator>();
    var buildVersion = scope.ServiceProvider.GetRequiredService<BuildVersionService>();

    logger.LogInformation(
        "{Service} {Version} starting in {Environment}. Repository root: {Root}.",
        HealthService.ServiceName,
        buildVersion.ServerVersion,
        app.Environment.EnvironmentName,
        locator.Root ?? "(not resolved)");
}

app.Run();

/// <summary>Marker type so the integration tests can host this application.</summary>
public partial class Program;
