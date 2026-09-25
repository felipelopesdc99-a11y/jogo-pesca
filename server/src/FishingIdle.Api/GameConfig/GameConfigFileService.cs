using System.Text.Json;
using System.Text.Json.Nodes;
using FishingIdle.Api.Configuration;

namespace FishingIdle.Api.GameConfig;

public sealed record GameConfigFile(
    string Name,
    string? BalanceStatus,
    int? SchemaVersion,
    string? Description,
    long SizeBytes,
    DateTimeOffset LastModifiedUtc);

public sealed record GameConfigListing(
    IReadOnlyList<GameConfigFile> Files,
    string? Directory,
    bool Editable,
    string EditingNote);

/// <summary>
/// Read-only access to the <c>/config</c> balance files for the Development Console.
/// </summary>
/// <remarks>
/// Milestone 0 only reads. Validated editing, config versioning and the admin audit trail
/// arrive in Milestone 1 (tasks M1-T05 to M1-T08); nothing here writes to disk.
/// </remarks>
public sealed class GameConfigFileService
{
    private const string EditingNote =
        "Read-only in Milestone 0. Validated editing, config versioning and the admin audit trail arrive in Milestone 1.";

    private readonly RepositoryRootLocator _locator;
    private readonly ILogger<GameConfigFileService> _logger;

    public GameConfigFileService(RepositoryRootLocator locator, ILogger<GameConfigFileService> logger)
    {
        _locator = locator;
        _logger = logger;
    }

    public GameConfigListing List()
    {
        var directory = _locator.ConfigDirectory;
        if (directory is null || !Directory.Exists(directory))
        {
            return new GameConfigListing([], directory, false, EditingNote);
        }

        var files = new List<GameConfigFile>();
        foreach (var path in Directory.EnumerateFiles(directory, "*.json").OrderBy(p => p, StringComparer.Ordinal))
        {
            var info = new FileInfo(path);
            var root = TryParse(path);

            files.Add(new GameConfigFile(
                Name: info.Name,
                BalanceStatus: root?["balance_status"]?.GetValue<string>(),
                SchemaVersion: root?["config_schema_version"]?.GetValue<int>(),
                Description: root?["description"]?.GetValue<string>(),
                SizeBytes: info.Length,
                LastModifiedUtc: info.LastWriteTimeUtc));
        }

        return new GameConfigListing(files, directory, false, EditingNote);
    }

    /// <summary>Returns the raw contents of one config file, or <c>null</c> when it does not exist.</summary>
    public JsonNode? Read(string fileName)
    {
        var directory = _locator.ConfigDirectory;
        if (directory is null)
        {
            return null;
        }

        // Only a bare file name is accepted, so a crafted name cannot escape /config.
        if (fileName != Path.GetFileName(fileName)
            || !fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var path = Path.Combine(directory, fileName);
        return File.Exists(path) ? TryParse(path) : null;
    }

    private JsonObject? TryParse(string path)
    {
        try
        {
            return JsonNode.Parse(File.ReadAllText(path)) as JsonObject;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Config file {Path} is not valid JSON.", path);
            return null;
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Config file {Path} could not be read.", path);
            return null;
        }
    }
}
