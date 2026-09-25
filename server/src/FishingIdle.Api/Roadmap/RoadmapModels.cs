using System.Text.Json.Serialization;

namespace FishingIdle.Api.Roadmap;

/// <summary>The five statuses a roadmap task may carry. Any other value is a data error.</summary>
public static class RoadmapStatus
{
    public const string Todo = "TODO";
    public const string InProgress = "IN_PROGRESS";
    public const string Done = "DONE";
    public const string Blocked = "BLOCKED";
    public const string NeedsOwnerDecision = "NEEDS_OWNER_DECISION";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Todo, InProgress, Done, Blocked, NeedsOwnerDecision,
    };

    /// <summary>Statuses that mean "the owner is waiting on something", shown together on the dashboard.</summary>
    public static bool IsAttentionNeeded(string status)
        => status is Blocked or NeedsOwnerDecision;
}

public sealed record RoadmapTask(
    string Id,
    string Title,
    string Subsystem,
    string Description,
    string Status,
    IReadOnlyList<string> Dependencies,
    string? CompletionNotes,
    string? UpdatedAt);

public sealed record RoadmapMilestone(
    string Id,
    string Title,
    string Goal,
    IReadOnlyList<string>? CompletionCriteria,
    IReadOnlyList<RoadmapTask> Tasks);

public sealed record RoadmapOwnerDecision(
    string Id,
    string? TaskId,
    string Title,
    string Detail,
    string? RaisedAt,
    string Status,
    string? Resolution);

public sealed record RoadmapDocument(
    string Project,
    string TargetVersion,
    string? UpdatedAt,
    string CurrentMilestone,
    IReadOnlyList<string>? Statuses,
    string? Notes,
    IReadOnlyList<RoadmapOwnerDecision>? OwnerDecisions,
    IReadOnlyList<RoadmapMilestone> Milestones);

/// <summary>A task paired with the milestone it belongs to, for flat dashboard lists.</summary>
public sealed record RoadmapTaskRef(
    string MilestoneId,
    string MilestoneTitle,
    RoadmapTask Task);

/// <summary>Per-milestone task counts and completion.</summary>
public sealed record MilestoneProgress(
    string MilestoneId,
    string MilestoneTitle,
    int TotalTasks,
    int DoneTasks,
    double CompletionPercent,
    IReadOnlyDictionary<string, int> StatusCounts);

/// <summary>
/// Everything the Dashboard needs, computed server-side so every consumer of the roadmap
/// agrees on what "42% complete" means.
/// </summary>
public sealed record RoadmapSummary(
    string Project,
    string TargetVersion,
    string CurrentMilestone,
    string? CurrentMilestoneTitle,
    int TotalTasks,
    int DoneTasks,
    double CompletionPercent,
    IReadOnlyDictionary<string, int> StatusCounts,
    IReadOnlyList<MilestoneProgress> Milestones,
    IReadOnlyList<RoadmapTaskRef> InProgress,
    IReadOnlyList<RoadmapTaskRef> RecentlyCompleted,
    IReadOnlyList<RoadmapTaskRef> NeedsAttention,
    IReadOnlyList<RoadmapTaskRef> NextUp,
    IReadOnlyList<RoadmapOwnerDecision> OpenOwnerDecisions);

/// <summary>The payload returned by <c>GET /api/dev/roadmap</c>.</summary>
public sealed record RoadmapResponse(
    RoadmapSummary Summary,
    RoadmapDocument Roadmap,
    [property: JsonPropertyName("source_path")] string? SourcePath,
    [property: JsonPropertyName("loaded_at_utc")] DateTimeOffset LoadedAtUtc);
