using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using FishingIdle.Api.Configuration;

namespace FishingIdle.Api.Versioning;

/// <summary>
/// Serves <c>version.json</c> — the single source of truth for client, server and web versions —
/// enriched with the runtime facts only the running process knows.
/// </summary>
public sealed class BuildVersionService
{
    /// <summary>
    /// Taken from the OS process, not from a static initialiser, so uptime stays correct even though
    /// this service is only constructed on the first request that needs it.
    /// </summary>
    private static readonly DateTimeOffset ProcessStartedUtc =
        new(Process.GetCurrentProcess().StartTime.ToUniversalTime(), TimeSpan.Zero);

    private readonly RepositoryRootLocator _locator;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<BuildVersionService> _logger;
    private readonly Lock _gate = new();

    private DateTime _cachedWriteTimeUtc;
    private JsonObject? _cachedFile;

    public BuildVersionService(
        RepositoryRootLocator locator,
        IHostEnvironment environment,
        ILogger<BuildVersionService> logger)
    {
        _locator = locator;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>Short version string used in health responses and logs.</summary>
    public string ServerVersion
        => ReadFile()?["components"]?["server"]?.GetValue<string>() ?? "unknown";

    public JsonObject GetBuildInfo()
    {
        var payload = new JsonObject
        {
            ["environment"] = _environment.EnvironmentName,
            ["server_time_utc"] = DateTimeOffset.UtcNow.ToString("O"),
            ["process_started_utc"] = ProcessStartedUtc.ToString("O"),
            ["uptime_seconds"] = Math.Round((DateTimeOffset.UtcNow - ProcessStartedUtc).TotalSeconds, 1),
            ["dotnet_version"] = Environment.Version.ToString(),
        };

        var file = ReadFile();
        if (file is null)
        {
            payload["version_file_available"] = false;
            payload["version_file_error"] =
                "version.json could not be read. Set Repository:RootPath (FISHINGIDLE_Repository__RootPath).";
            return payload;
        }

        payload["version_file_available"] = true;
        payload["version_file_path"] = _locator.VersionFilePath;

        // The file's own keys are surfaced verbatim so the Dev Console never needs a second
        // mapping layer that could drift from the file.
        foreach (var property in file)
        {
            payload[property.Key] = property.Value?.DeepClone();
        }

        return payload;
    }

    private JsonObject? ReadFile()
    {
        var path = _locator.VersionFilePath;
        if (path is null || !File.Exists(path))
        {
            return null;
        }

        var writeTimeUtc = File.GetLastWriteTimeUtc(path);

        lock (_gate)
        {
            if (_cachedFile is not null && _cachedWriteTimeUtc == writeTimeUtc)
            {
                return _cachedFile;
            }

            try
            {
                var parsed = JsonNode.Parse(File.ReadAllText(path)) as JsonObject;
                _cachedFile = parsed;
                _cachedWriteTimeUtc = writeTimeUtc;
                return parsed;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "version.json at {Path} is not valid JSON.", path);
                return null;
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "version.json at {Path} could not be read.", path);
                return null;
            }
        }
    }
}
