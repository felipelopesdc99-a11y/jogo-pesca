using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace FishingIdle.Api.Tests;

public sealed class HealthEndpointTests : IClassFixture<UnreachableDatabaseApiFactory>
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };

    private readonly UnreachableDatabaseApiFactory _factory;

    public HealthEndpointTests(UnreachableDatabaseApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Liveness_answers_without_touching_the_database()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/live");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("healthy", body.GetProperty("status").GetString());
        Assert.Equal("fishing-idle-api", body.GetProperty("service").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("server_time_utc").GetString()));
    }

    [Fact]
    public async Task Health_reports_degraded_and_names_the_database_when_it_is_unreachable()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);

        // 200 with a status field, so a client can read *which* dependency failed.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("degraded", body.GetProperty("status").GetString());

        var database = body.GetProperty("dependencies")
            .EnumerateArray()
            .Single(d => d.GetProperty("name").GetString() == "database");

        Assert.Equal("unhealthy", database.GetProperty("status").GetString());
        Assert.False(string.IsNullOrWhiteSpace(database.GetProperty("detail").GetString()));
    }

    [Fact]
    public async Task Health_uses_snake_case_on_the_wire()
    {
        using var client = _factory.CreateClient();

        var raw = await client.GetStringAsync("/health");

        Assert.Contains("server_time_utc", raw, StringComparison.Ordinal);
        Assert.DoesNotContain("serverTimeUtc", raw, StringComparison.Ordinal);
    }
}
