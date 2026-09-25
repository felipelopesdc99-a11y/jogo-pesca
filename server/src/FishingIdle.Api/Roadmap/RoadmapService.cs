using System.Text.Json;
using FishingIdle.Api.Configuration;

namespace FishingIdle.Api.Roadmap;

/// <summary>Outcome of a roadmap load: either a document, or the reason it could not be read.</summary>
public sealed record RoadmapLoadResult(
    RoadmapResponse? Response,
    string? Error)
{
    public bool Succeeded => Response is not null;

    public static RoadmapLoadResult Ok(RoadmapResponse response) => new(response, null);

    public static RoadmapLoadResult Fail(string error) => new(null, error);
}

/// <summary>
/// Reads <c>docs/roadmap.json</c> and derives the dashboard summary.
/// </summary>
/// <remarks>
/// The file on disk is the source of truth. The result is cached only until the file's
/// last-write timestamp changes, so editing the roadmap as part of a code change is
/// reflected without restarting the server.
/// </remarks>
public sealed class RoadmapService
{
    private const int DashboardListSize = 5;

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly RepositoryRootLocator _locator;
    private readonly ILogger<RoadmapService> _logger;
    private readonly Lock _gate = new();

    private DateTime _cachedWriteTimeUtc;
    private long _cachedLength = -1;
    private RoadmapLoadResult? _cached;

    public RoadmapService(RepositoryRootLocator locator, ILogger<RoadmapService> logger)
    {
        _locator = locator;
        _logger = logger;
    }

    public RoadmapLoadResult Load()
    {
        var path = _locator.RoadmapPath;
        if (path is null)
        {
            return RoadmapLoadResult.Fail(
                "A raiz do repositório não pôde ser localizada, então docs/roadmap.json não pode ser lido. "
                + "Defina Repository:RootPath (variável FISHINGIDLE_Repository__RootPath).");
        }

        var file = new FileInfo(path);
        if (!file.Exists)
        {
            return RoadmapLoadResult.Fail($"Arquivo do roadmap não encontrado em {path}.");
        }

        lock (_gate)
        {
            if (_cached is not null
                && _cachedWriteTimeUtc == file.LastWriteTimeUtc
                && _cachedLength == file.Length)
            {
                return _cached;
            }

            var result = ReadAndBuild(file);
            _cached = result;
            _cachedWriteTimeUtc = file.LastWriteTimeUtc;
            _cachedLength = file.Length;
            return result;
        }
    }

    private RoadmapLoadResult ReadAndBuild(FileInfo file)
    {
        RoadmapDocument? document;
        try
        {
            using var stream = file.OpenRead();
            document = JsonSerializer.Deserialize<RoadmapDocument>(stream, ReadOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Roadmap file {Path} is not valid JSON for the expected shape.", file.FullName);
            return RoadmapLoadResult.Fail($"O arquivo do roadmap não é válido: {ex.Message}");
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Roadmap file {Path} could not be read.", file.FullName);
            return RoadmapLoadResult.Fail($"O arquivo do roadmap não pôde ser lido: {ex.Message}");
        }

        if (document is null || document.Milestones.Count == 0)
        {
            return RoadmapLoadResult.Fail("O arquivo do roadmap foi lido, mas está vazio.");
        }

        if (Validate(document) is { } validationError)
        {
            _logger.LogError("Roadmap file {Path} failed validation: {Error}", file.FullName, validationError);
            return RoadmapLoadResult.Fail(validationError);
        }

        var summary = BuildSummary(document);
        _logger.LogInformation(
            "Roadmap loaded from {Path}: {Done}/{Total} tasks done ({Percent:0.0}%), current milestone {Milestone}.",
            file.FullName, summary.DoneTasks, summary.TotalTasks, summary.CompletionPercent, summary.CurrentMilestone);

        return RoadmapLoadResult.Ok(new RoadmapResponse(
            summary,
            document,
            file.FullName,
            DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// Rejects a roadmap the console would otherwise render as if it were true:
    /// an unknown status, a duplicate task id, or a dependency pointing nowhere.
    /// </summary>
    private static string? Validate(RoadmapDocument document)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);

        foreach (var milestone in document.Milestones)
        {
            foreach (var task in milestone.Tasks)
            {
                if (!RoadmapStatus.All.Contains(task.Status))
                {
                    return $"A tarefa {task.Id} tem o status desconhecido '{task.Status}'. "
                        + $"Valores permitidos: {string.Join(", ", RoadmapStatus.All)}.";
                }

                if (!ids.Add(task.Id))
                {
                    return $"O identificador de tarefa {task.Id} aparece mais de uma vez.";
                }
            }
        }

        foreach (var milestone in document.Milestones)
        {
            foreach (var task in milestone.Tasks)
            {
                foreach (var dependency in task.Dependencies)
                {
                    if (!ids.Contains(dependency))
                    {
                        return $"A tarefa {task.Id} depende da tarefa inexistente {dependency}.";
                    }
                }
            }
        }

        if (document.Milestones.All(m => m.Id != document.CurrentMilestone))
        {
            return $"current_milestone '{document.CurrentMilestone}' não corresponde a nenhum milestone.";
        }

        return null;
    }

    private static RoadmapSummary BuildSummary(RoadmapDocument document)
    {
        var allTasks = document.Milestones
            .SelectMany(m => m.Tasks.Select(t => new RoadmapTaskRef(m.Id, m.Title, t)))
            .ToList();

        var doneIds = allTasks
            .Where(x => x.Task.Status == RoadmapStatus.Done)
            .Select(x => x.Task.Id)
            .ToHashSet(StringComparer.Ordinal);

        var total = allTasks.Count;
        var done = doneIds.Count;

        var milestoneOrder = document.Milestones
            .Select((m, index) => (m.Id, index))
            .ToDictionary(x => x.Id, x => x.index, StringComparer.Ordinal);

        var nextUp = allTasks
            .Where(x => x.Task.Status == RoadmapStatus.Todo)
            // A task whose dependencies are all done can be started now; show those first.
            .OrderBy(x => x.Task.Dependencies.All(doneIds.Contains) ? 0 : 1)
            .ThenBy(x => milestoneOrder[x.MilestoneId])
            .ThenBy(x => x.Task.Id, StringComparer.Ordinal)
            .Take(DashboardListSize)
            .ToList();

        return new RoadmapSummary(
            Project: document.Project,
            TargetVersion: document.TargetVersion,
            CurrentMilestone: document.CurrentMilestone,
            CurrentMilestoneTitle: document.Milestones
                .FirstOrDefault(m => m.Id == document.CurrentMilestone)?.Title,
            TotalTasks: total,
            DoneTasks: done,
            CompletionPercent: total == 0 ? 0 : Math.Round(done * 100.0 / total, 1),
            StatusCounts: CountByStatus(allTasks.Select(x => x.Task)),
            Milestones: document.Milestones.Select(BuildMilestoneProgress).ToList(),
            InProgress: allTasks
                .Where(x => x.Task.Status == RoadmapStatus.InProgress)
                .ThenOrderForDisplay(milestoneOrder)
                .ToList(),
            RecentlyCompleted: allTasks
                .Where(x => x.Task.Status == RoadmapStatus.Done)
                .OrderByDescending(x => x.Task.UpdatedAt ?? string.Empty, StringComparer.Ordinal)
                .ThenByDescending(x => x.Task.Id, StringComparer.Ordinal)
                .Take(DashboardListSize)
                .ToList(),
            NeedsAttention: allTasks
                .Where(x => RoadmapStatus.IsAttentionNeeded(x.Task.Status))
                .ThenOrderForDisplay(milestoneOrder)
                .ToList(),
            NextUp: nextUp,
            OpenOwnerDecisions: (document.OwnerDecisions ?? [])
                .Where(d => !string.Equals(d.Status, "RESOLVED", StringComparison.OrdinalIgnoreCase))
                .ToList());
    }

    private static MilestoneProgress BuildMilestoneProgress(RoadmapMilestone milestone)
    {
        var total = milestone.Tasks.Count;
        var done = milestone.Tasks.Count(t => t.Status == RoadmapStatus.Done);

        return new MilestoneProgress(
            milestone.Id,
            milestone.Title,
            total,
            done,
            total == 0 ? 0 : Math.Round(done * 100.0 / total, 1),
            CountByStatus(milestone.Tasks));
    }

    /// <summary>Counts every allowed status, including the ones at zero, so the UI can render a stable legend.</summary>
    private static IReadOnlyDictionary<string, int> CountByStatus(IEnumerable<RoadmapTask> tasks)
    {
        var counts = RoadmapStatus.All.ToDictionary(status => status, _ => 0, StringComparer.Ordinal);

        foreach (var task in tasks)
        {
            if (counts.ContainsKey(task.Status))
            {
                counts[task.Status]++;
            }
        }

        return counts;
    }
}

internal static class RoadmapOrderingExtensions
{
    /// <summary>Roadmap order: milestone as authored, then task id.</summary>
    internal static IOrderedEnumerable<RoadmapTaskRef> ThenOrderForDisplay(
        this IEnumerable<RoadmapTaskRef> source,
        IReadOnlyDictionary<string, int> milestoneOrder)
        => source
            .OrderBy(x => milestoneOrder.TryGetValue(x.MilestoneId, out var index) ? index : int.MaxValue)
            .ThenBy(x => x.Task.Id, StringComparer.Ordinal);
}
