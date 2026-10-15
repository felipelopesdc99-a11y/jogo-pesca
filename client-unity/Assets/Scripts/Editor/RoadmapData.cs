using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using FishingIdle.GameService.Config;
using Newtonsoft.Json;

namespace FishingIdle.Editor
{
    // Read-only models of docs/roadmap.json and version.json, plus the summary the Dev Panel shows.
    // The roadmap file is the source of truth for progress (CLAUDE.md, section 5); this panel only
    // reads it. Status and subsystem values are technical keys, translated at display time.

    public sealed class RoadmapDocument
    {
        public string Project { get; set; }
        public string TargetVersion { get; set; }
        public string UpdatedAt { get; set; }
        public string CurrentMilestone { get; set; }
        public List<OwnerDecision> OwnerDecisions { get; set; } = new List<OwnerDecision>();
        public List<RoadmapMilestone> Milestones { get; set; } = new List<RoadmapMilestone>();
    }

    public sealed class OwnerDecision
    {
        public string Id { get; set; }
        public string TaskId { get; set; }
        public string Title { get; set; }
        public string Detail { get; set; }
        public string Status { get; set; }
    }

    public sealed class RoadmapMilestone
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Goal { get; set; }
        public List<string> CompletionCriteria { get; set; } = new List<string>();
        public List<RoadmapTask> Tasks { get; set; } = new List<RoadmapTask>();

        public int DoneCount => Tasks.Count(t => t.Status == "DONE");
    }

    public sealed class RoadmapTask
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Subsystem { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public List<string> Dependencies { get; set; } = new List<string>();
        public string CompletionNotes { get; set; }
        public string UpdatedAt { get; set; }
    }

    public sealed class VersionDocument
    {
        public string TargetVersion { get; set; }
        public string CurrentMilestone { get; set; }
        public Dictionary<string, string> Components { get; set; } = new Dictionary<string, string>();
        public VersionBuild Build { get; set; }
    }

    public sealed class VersionBuild
    {
        public int Number { get; set; }
        public string Channel { get; set; }
        public string Date { get; set; }
    }

    /// <summary>What the Visão geral tab shows, computed from the roadmap.</summary>
    public sealed class RoadmapSummary
    {
        public int Total;
        public int Done;
        public double Ratio => Total == 0 ? 0 : (double)Done / Total;
        public RoadmapMilestone Current;
        public List<RoadmapTask> InProgress = new List<RoadmapTask>();
        public RoadmapTask Next;
        public List<RoadmapTask> Blocked = new List<RoadmapTask>();
        public List<OwnerDecision> OpenDecisions = new List<OwnerDecision>();
        public List<RoadmapTask> RecentlyDone = new List<RoadmapTask>();

        public static RoadmapSummary Build(RoadmapDocument roadmap)
        {
            var tasks = roadmap.Milestones.SelectMany(m => m.Tasks).ToList();
            var done = new HashSet<string>(tasks.Where(t => t.Status == "DONE").Select(t => t.Id));

            return new RoadmapSummary
            {
                Total = tasks.Count,
                Done = done.Count,
                Current = roadmap.Milestones.FirstOrDefault(m => m.Id == roadmap.CurrentMilestone),
                InProgress = tasks.Where(t => t.Status == "IN_PROGRESS").ToList(),
                // Next step: the first task, in roadmap order, that is ready to start.
                Next = tasks.FirstOrDefault(t => t.Status == "TODO" && t.Dependencies.All(done.Contains)),
                Blocked = tasks.Where(t => t.Status == "BLOCKED" || t.Status == "NEEDS_OWNER_DECISION").ToList(),
                OpenDecisions = roadmap.OwnerDecisions.Where(d => d.Status == "OPEN").ToList(),
                RecentlyDone = tasks.Where(t => t.Status == "DONE")
                    .OrderByDescending(t => t.UpdatedAt, StringComparer.Ordinal)
                    .Take(6)
                    .ToList(),
            };
        }
    }

    public static class RepositoryFiles
    {
        public static string RoadmapPath => Path.Combine(FishingIdle.Game.Bootstrap.GamePaths.RepositoryRoot, "docs", "roadmap.json");

        public static string VersionPath => Path.Combine(FishingIdle.Game.Bootstrap.GamePaths.RepositoryRoot, "version.json");

        public static T Read<T>(string path, out string error) where T : class
        {
            error = null;
            try
            {
                return JsonConvert.DeserializeObject<T>(File.ReadAllText(path), JsonSettings.Default);
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return null;
            }
        }

        /// <summary>ISO-8601 timestamp from the roadmap → "28/09/2026 21:55" in local time.</summary>
        public static string FormatTimestamp(string iso)
        {
            if (string.IsNullOrEmpty(iso))
            {
                return "—";
            }

            return DateTimeOffset.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
                ? FishingIdle.Texts.Format.DateTime(parsed.LocalDateTime)
                : iso;
        }
    }
}
