namespace FishingIdle.Api.Configuration;

/// <summary>
/// Where the server finds the repository-backed development artefacts
/// (<c>version.json</c>, <c>docs/roadmap.json</c>, <c>config/*.json</c>).
/// </summary>
/// <remarks>
/// These files are the single source of truth for project status and balance data.
/// The server reads them; it never writes them from a game request.
/// </remarks>
public sealed class RepositoryOptions
{
    public const string SectionName = "Repository";

    /// <summary>
    /// Absolute path to the repository root. When empty the root is discovered by
    /// walking up from the running assembly looking for <see cref="RootMarkerFile"/>,
    /// which is what happens during local <c>dotnet run</c> and when running tests.
    /// In Docker the repository is mounted read-only and this is set explicitly.
    /// </summary>
    public string RootPath { get; set; } = string.Empty;

    /// <summary>File whose presence identifies the repository root.</summary>
    public string RootMarkerFile { get; set; } = "version.json";

    public string RoadmapRelativePath { get; set; } = Path.Combine("docs", "roadmap.json");

    public string ConfigRelativePath { get; set; } = "config";
}
