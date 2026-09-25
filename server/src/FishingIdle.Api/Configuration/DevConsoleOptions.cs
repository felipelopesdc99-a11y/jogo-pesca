namespace FishingIdle.Api.Configuration;

/// <summary>Settings for the private Development Console's access to the API.</summary>
public sealed class DevConsoleOptions
{
    public const string SectionName = "DevConsole";

    public const string CorsPolicyName = "dev-console";

    /// <summary>
    /// Exact origins allowed to call the API from a browser. Empty means no browser origin is
    /// allowed, which is the safe default; a wildcard origin is never used.
    /// </summary>
    public string[] AllowedOrigins { get; set; } = [];
}
