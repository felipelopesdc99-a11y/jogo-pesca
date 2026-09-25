using System.Text.Json;
using FishingIdle.Api.Configuration;
using FishingIdle.Api.Roadmap;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace FishingIdle.Api.Tests;

/// <summary>
/// Validation tests for the roadmap loader. A roadmap the console would render as if it were
/// true, when it is not, is worse than no roadmap at all — so bad data must be rejected loudly.
/// </summary>
public sealed class RoadmapServiceTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("fishing-idle-roadmap-").FullName;

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private RoadmapService BuildService(string roadmapJson)
    {
        File.WriteAllText(Path.Combine(_root, "version.json"), """{"components":{"server":"test"}}""");
        Directory.CreateDirectory(Path.Combine(_root, "docs"));
        File.WriteAllText(Path.Combine(_root, "docs", "roadmap.json"), roadmapJson);

        var options = Options.Create(new RepositoryOptions { RootPath = _root });
        var locator = new RepositoryRootLocator(options, NullLogger<RepositoryRootLocator>.Instance);

        return new RoadmapService(locator, NullLogger<RoadmapService>.Instance);
    }

    private static string Roadmap(string tasksJson, string currentMilestone = "M0") => $$"""
        {
          "project": "Fishing Idle",
          "target_version": "0.1.0",
          "current_milestone": "{{currentMilestone}}",
          "milestones": [
            { "id": "M0", "title": "Foundation", "goal": "g", "tasks": [ {{tasksJson}} ] }
          ]
        }
        """;

    private static string Task(string id, string status, string[]? dependencies = null, string? updatedAt = null)
    {
        var deps = JsonSerializer.Serialize(dependencies ?? []);
        return $$"""
            {
              "id": "{{id}}", "title": "T {{id}}", "subsystem": "server", "description": "d",
              "status": "{{status}}", "dependencies": {{deps}}, "completion_notes": "",
              "updated_at": "{{updatedAt ?? "2026-09-25T00:00:00Z"}}"
            }
            """;
    }

    [Fact]
    public void Computes_completion_from_done_tasks()
    {
        var service = BuildService(Roadmap(string.Join(",",
            Task("M0-T01", "DONE"),
            Task("M0-T02", "DONE"),
            Task("M0-T03", "IN_PROGRESS"),
            Task("M0-T04", "TODO"))));

        var result = service.Load();

        Assert.True(result.Succeeded, result.Error);
        var summary = result.Response!.Summary;
        Assert.Equal(4, summary.TotalTasks);
        Assert.Equal(2, summary.DoneTasks);
        Assert.Equal(50.0, summary.CompletionPercent);
        Assert.Equal("M0-T03", Assert.Single(summary.InProgress).Task.Id);
    }

    [Fact]
    public void Next_up_prefers_tasks_whose_dependencies_are_all_done()
    {
        var service = BuildService(Roadmap(string.Join(",",
            Task("M0-T01", "DONE"),
            // Depends on a task that is still TODO, so it is not startable yet.
            Task("M0-T02", "TODO", ["M0-T04"]),
            // Depends only on a DONE task, so it is ready now.
            Task("M0-T03", "TODO", ["M0-T01"]),
            Task("M0-T04", "TODO", ["M0-T02"]))));

        var result = service.Load();

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal("M0-T03", result.Response!.Summary.NextUp.First().Task.Id);
    }

    [Fact]
    public void Recently_completed_is_ordered_by_most_recent_update()
    {
        var service = BuildService(Roadmap(string.Join(",",
            Task("M0-T01", "DONE", updatedAt: "2026-01-01T00:00:00Z"),
            Task("M0-T02", "DONE", updatedAt: "2026-03-01T00:00:00Z"),
            Task("M0-T03", "DONE", updatedAt: "2026-02-01T00:00:00Z"))));

        var result = service.Load();

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal(
            ["M0-T02", "M0-T03", "M0-T01"],
            result.Response!.Summary.RecentlyCompleted.Select(x => x.Task.Id));
    }

    [Fact]
    public void Blocked_and_owner_decision_tasks_surface_as_needing_attention()
    {
        var service = BuildService(Roadmap(string.Join(",",
            Task("M0-T01", "BLOCKED"),
            Task("M0-T02", "NEEDS_OWNER_DECISION"),
            Task("M0-T03", "TODO"))));

        var result = service.Load();

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal(
            ["M0-T01", "M0-T02"],
            result.Response!.Summary.NeedsAttention.Select(x => x.Task.Id));
    }

    [Fact]
    public void Rejects_an_unknown_status()
    {
        var service = BuildService(Roadmap(Task("M0-T01", "ALMOST_DONE")));

        var result = service.Load();

        Assert.False(result.Succeeded);
        Assert.Contains("unknown status", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rejects_a_duplicate_task_id()
    {
        var service = BuildService(Roadmap(string.Join(",",
            Task("M0-T01", "DONE"),
            Task("M0-T01", "TODO"))));

        var result = service.Load();

        Assert.False(result.Succeeded);
        Assert.Contains("more than once", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rejects_a_dependency_that_points_nowhere()
    {
        var service = BuildService(Roadmap(Task("M0-T01", "TODO", ["M9-T99"])));

        var result = service.Load();

        Assert.False(result.Succeeded);
        Assert.Contains("unknown task", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rejects_a_current_milestone_that_does_not_exist()
    {
        var service = BuildService(Roadmap(Task("M0-T01", "TODO"), currentMilestone: "M7"));

        var result = service.Load();

        Assert.False(result.Succeeded);
        Assert.Contains("current_milestone", result.Error!, StringComparison.Ordinal);
    }

    [Fact]
    public void Reports_a_missing_roadmap_file_instead_of_pretending_it_is_empty()
    {
        File.WriteAllText(Path.Combine(_root, "version.json"), "{}");
        var options = Options.Create(new RepositoryOptions { RootPath = _root });
        var locator = new RepositoryRootLocator(options, NullLogger<RepositoryRootLocator>.Instance);
        var service = new RoadmapService(locator, NullLogger<RoadmapService>.Instance);

        var result = service.Load();

        Assert.False(result.Succeeded);
        Assert.Contains("not found", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Picks_up_an_edit_to_the_roadmap_file_without_a_restart()
    {
        var service = BuildService(Roadmap(Task("M0-T01", "TODO")));
        Assert.Equal(0, service.Load().Response!.Summary.DoneTasks);

        var path = Path.Combine(_root, "docs", "roadmap.json");
        File.WriteAllText(path, Roadmap(Task("M0-T01", "DONE")));
        // The cache keys on last-write time and length; nudge the timestamp so the change is unambiguous.
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddSeconds(1));

        Assert.Equal(1, service.Load().Response!.Summary.DoneTasks);
    }
}
