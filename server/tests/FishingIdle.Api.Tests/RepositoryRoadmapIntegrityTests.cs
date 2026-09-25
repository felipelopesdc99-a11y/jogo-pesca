using FishingIdle.Api.Configuration;
using FishingIdle.Api.Roadmap;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace FishingIdle.Api.Tests;

/// <summary>
/// Guards the real <c>docs/roadmap.json</c>. If a status is mistyped or a dependency is renamed,
/// this fails in CI rather than quietly misreporting progress on the owner's panel.
/// </summary>
public sealed class RepositoryRoadmapIntegrityTests
{
    private static RoadmapLoadResult LoadRepositoryRoadmap()
    {
        var locator = new RepositoryRootLocator(
            Options.Create(new RepositoryOptions()),
            NullLogger<RepositoryRootLocator>.Instance);

        return new RoadmapService(locator, NullLogger<RoadmapService>.Instance).Load();
    }

    [Fact]
    public void The_repository_roadmap_is_valid()
    {
        var result = LoadRepositoryRoadmap();

        Assert.True(result.Succeeded, result.Error);
    }

    [Fact]
    public void The_repository_roadmap_covers_milestones_0_through_12()
    {
        var result = LoadRepositoryRoadmap();
        Assert.True(result.Succeeded, result.Error);

        var ids = result.Response!.Roadmap.Milestones.Select(m => m.Id).ToList();

        Assert.Equal(Enumerable.Range(0, 13).Select(i => $"M{i}"), ids);
    }

    [Fact]
    public void Every_task_carries_a_subsystem_and_a_description()
    {
        var result = LoadRepositoryRoadmap();
        Assert.True(result.Succeeded, result.Error);

        var incomplete = result.Response!.Roadmap.Milestones
            .SelectMany(m => m.Tasks)
            .Where(t => string.IsNullOrWhiteSpace(t.Subsystem) || string.IsNullOrWhiteSpace(t.Description))
            .Select(t => t.Id)
            .ToList();

        Assert.Empty(incomplete);
    }

    [Fact]
    public void Every_done_task_explains_what_was_delivered()
    {
        var result = LoadRepositoryRoadmap();
        Assert.True(result.Succeeded, result.Error);

        var undocumented = result.Response!.Roadmap.Milestones
            .SelectMany(m => m.Tasks)
            .Where(t => t.Status == RoadmapStatus.Done && string.IsNullOrWhiteSpace(t.CompletionNotes))
            .Select(t => t.Id)
            .ToList();

        Assert.Empty(undocumented);
    }

    [Fact]
    public void Every_task_needing_a_decision_is_listed_in_owner_decisions()
    {
        var result = LoadRepositoryRoadmap();
        Assert.True(result.Succeeded, result.Error);

        var document = result.Response!.Roadmap;
        var decisionTaskIds = (document.OwnerDecisions ?? [])
            .Select(d => d.TaskId)
            .Where(id => id is not null)
            .ToHashSet(StringComparer.Ordinal);

        var unlisted = document.Milestones
            .SelectMany(m => m.Tasks)
            .Where(t => t.Status == RoadmapStatus.NeedsOwnerDecision && !decisionTaskIds.Contains(t.Id))
            .Select(t => t.Id)
            .ToList();

        Assert.Empty(unlisted);
    }
}
