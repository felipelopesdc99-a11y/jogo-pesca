using Newtonsoft.Json.Linq;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>
/// Guards docs/roadmap.json, which both Dev Panels display as the project's status. Mirrors the
/// server's RepositoryRoadmapIntegrityTests so the check also runs where .NET 10 is not installed.
/// </summary>
public sealed class RoadmapIntegrityTests
{
    private static readonly string[] Statuses = { "TODO", "IN_PROGRESS", "DONE", "BLOCKED", "NEEDS_OWNER_DECISION" };
    private static readonly string[] Subsystems = { "repo", "docs", "config", "ops", "server", "client", "web" };

    private static JObject Roadmap => JObject.Parse(File.ReadAllText(Path.Combine(TestSupport.RepositoryRoot, "docs", "roadmap.json")));

    private static List<JToken> Tasks(JObject roadmap) => roadmap["milestones"]!.SelectMany(m => m["tasks"]!).ToList();

    [Fact]
    public void Every_task_is_well_formed()
    {
        var roadmap = Roadmap;
        var tasks = Tasks(roadmap);
        var ids = tasks.Select(t => (string)t["id"]!).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.All(tasks, t =>
        {
            Assert.Contains((string)t["status"]!, Statuses);
            Assert.Contains((string)t["subsystem"]!, Subsystems);
            Assert.False(string.IsNullOrWhiteSpace((string)t["description"]));
            Assert.All(t["dependencies"]!.Select(d => (string)d!), d => Assert.Contains(d, ids));
            Assert.Matches("^M[0-9]+-T[0-9]{2}$", (string)t["id"]!);
        });
    }

    [Fact]
    public void Milestones_run_from_0_to_12_and_the_current_one_exists()
    {
        var roadmap = Roadmap;
        var milestoneIds = roadmap["milestones"]!.Select(m => (string)m["id"]!).ToList();

        Assert.Equal(Enumerable.Range(0, 13).Select(i => "M" + i), milestoneIds);
        Assert.Contains((string)roadmap["current_milestone"]!, milestoneIds);
    }

    [Fact]
    public void Every_done_task_says_what_was_delivered()
    {
        var undocumented = Tasks(Roadmap)
            .Where(t => (string)t["status"]! == "DONE" && string.IsNullOrWhiteSpace((string?)t["completion_notes"]))
            .Select(t => (string)t["id"]!);

        Assert.Empty(undocumented);
    }

    [Fact]
    public void Every_task_waiting_on_the_owner_has_an_open_question()
    {
        var roadmap = Roadmap;
        var asked = roadmap["owner_decisions"]!
            .Where(d => (string)d["status"]! == "OPEN")
            .Select(d => (string?)d["task_id"])
            .ToHashSet();

        var unasked = Tasks(roadmap)
            .Where(t => (string)t["status"]! == "NEEDS_OWNER_DECISION" && !asked.Contains((string)t["id"]!))
            .Select(t => (string)t["id"]!);

        Assert.Empty(unasked);
    }
}
