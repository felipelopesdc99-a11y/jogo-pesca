using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace FishingIdle.Api.Tests;

public sealed class DevEndpointTests : IClassFixture<UnreachableDatabaseApiFactory>
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };

    private readonly UnreachableDatabaseApiFactory _factory;

    public DevEndpointTests(UnreachableDatabaseApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Roadmap_endpoint_serves_the_repository_roadmap_with_a_computed_summary()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/dev/roadmap");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        var summary = body.GetProperty("summary");
        var milestones = body.GetProperty("roadmap").GetProperty("milestones");

        // Milestones 0 through 12 inclusive.
        Assert.Equal(13, milestones.GetArrayLength());

        var totalTasks = summary.GetProperty("total_tasks").GetInt32();
        var doneTasks = summary.GetProperty("done_tasks").GetInt32();
        var countedTasks = milestones.EnumerateArray().Sum(m => m.GetProperty("tasks").GetArrayLength());

        Assert.Equal(countedTasks, totalTasks);
        Assert.InRange(doneTasks, 0, totalTasks);

        var expectedPercent = Math.Round(doneTasks * 100.0 / totalTasks, 1);
        Assert.Equal(expectedPercent, summary.GetProperty("completion_percent").GetDouble(), precision: 3);

        // The dashboard lists are capped at five so the panel stays readable.
        Assert.True(summary.GetProperty("next_up").GetArrayLength() <= 5);
        Assert.True(summary.GetProperty("recently_completed").GetArrayLength() <= 5);
    }

    [Fact]
    public async Task Roadmap_summary_status_counts_cover_every_allowed_status()
    {
        using var client = _factory.CreateClient();

        var body = await client.GetFromJsonAsync<JsonElement>("/api/dev/roadmap", Json);
        var counts = body.GetProperty("summary").GetProperty("status_counts");

        foreach (var status in new[] { "TODO", "IN_PROGRESS", "DONE", "BLOCKED", "NEEDS_OWNER_DECISION" })
        {
            Assert.True(counts.TryGetProperty(status, out _), $"status_counts is missing {status}");
        }
    }

    [Fact]
    public async Task Version_endpoint_serves_every_component_version()
    {
        using var client = _factory.CreateClient();

        var body = await client.GetFromJsonAsync<JsonElement>("/api/dev/version", Json);

        Assert.True(body.GetProperty("version_file_available").GetBoolean());

        var components = body.GetProperty("components");
        foreach (var component in new[] { "client", "server", "web" })
        {
            Assert.False(string.IsNullOrWhiteSpace(components.GetProperty(component).GetString()));
        }

        Assert.Equal("Testing", body.GetProperty("environment").GetString());
    }

    [Fact]
    public async Task Config_listing_exposes_the_balance_files()
    {
        using var client = _factory.CreateClient();

        var body = await client.GetFromJsonAsync<JsonElement>("/api/dev/config", Json);
        var names = body.GetProperty("files")
            .EnumerateArray()
            .Select(f => f.GetProperty("name").GetString())
            .ToList();

        foreach (var expected in new[]
                 {
                     "arena.json", "economy.json", "expeditions.json", "fish_catalog.json",
                     "maps.json", "progression.json", "rods.json",
                 })
        {
            Assert.Contains(expected, names);
        }

        // Editing is a Milestone 1 deliverable; the console must not offer it yet.
        Assert.False(body.GetProperty("editable").GetBoolean());
    }

    [Fact]
    public async Task Config_file_endpoint_serves_a_known_file()
    {
        using var client = _factory.CreateClient();

        var body = await client.GetFromJsonAsync<JsonElement>("/api/dev/config/fish_catalog.json", Json);

        Assert.Equal(20, body.GetProperty("species").GetArrayLength());
    }

    [Theory]
    [InlineData("../version.json")]
    [InlineData("..%2Fversion.json")]
    [InlineData("nope.json")]
    [InlineData("appsettings.json")]
    public async Task Config_file_endpoint_refuses_anything_outside_the_config_directory(string fileName)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/dev/config/{fileName}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
