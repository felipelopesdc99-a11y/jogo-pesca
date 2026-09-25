using FishingIdle.Api.Configuration;
using Microsoft.Extensions.Options;

namespace FishingIdle.Api.Configuration;

/// <summary>
/// Resolves the repository root once and exposes the paths of the development artefacts.
/// </summary>
public sealed class RepositoryRootLocator
{
    private const int MaxWalkUpLevels = 12;

    private readonly RepositoryOptions _options;
    private readonly ILogger<RepositoryRootLocator> _logger;
    private readonly Lazy<string?> _root;

    public RepositoryRootLocator(IOptions<RepositoryOptions> options, ILogger<RepositoryRootLocator> logger)
    {
        _options = options.Value;
        _logger = logger;
        _root = new Lazy<string?>(Resolve, isThreadSafe: true);
    }

    /// <summary>The repository root, or <c>null</c> when it could not be located.</summary>
    public string? Root => _root.Value;

    public string? RoadmapPath => Combine(_options.RoadmapRelativePath);

    public string? ConfigDirectory => Combine(_options.ConfigRelativePath);

    public string? VersionFilePath => Combine(_options.RootMarkerFile);

    private string? Combine(string relativePath)
        => Root is null ? null : Path.GetFullPath(Path.Combine(Root, relativePath));

    private string? Resolve()
    {
        if (!string.IsNullOrWhiteSpace(_options.RootPath))
        {
            var configured = Path.GetFullPath(_options.RootPath);
            if (File.Exists(Path.Combine(configured, _options.RootMarkerFile)))
            {
                _logger.LogInformation("Repository root resolved from configuration: {Root}", configured);
                return configured;
            }

            // A configured-but-wrong path is a deployment mistake, not something to paper over.
            _logger.LogError(
                "Configured repository root {Root} does not contain the marker file {Marker}. "
                + "Development endpoints that read repository files will report unavailable.",
                configured, _options.RootMarkerFile);
            return null;
        }

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        for (var level = 0; level < MaxWalkUpLevels && directory is not null; level++)
        {
            if (File.Exists(Path.Combine(directory.FullName, _options.RootMarkerFile)))
            {
                _logger.LogInformation("Repository root discovered by walk-up: {Root}", directory.FullName);
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        _logger.LogWarning(
            "Repository root could not be discovered by walking up from {Start} looking for {Marker}. "
            + "Set Repository:RootPath explicitly.",
            AppContext.BaseDirectory, _options.RootMarkerFile);
        return null;
    }
}
