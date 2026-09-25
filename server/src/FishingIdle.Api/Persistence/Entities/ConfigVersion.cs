namespace FishingIdle.Api.Persistence.Entities;

/// <summary>
/// One immutable snapshot of the whole <c>/config</c> balance set.
/// </summary>
/// <remarks>
/// Every outcome with economic or competitive value records which config version produced it,
/// so a catch, a battle or a sale can always be traced back to the balance that was live
/// (GDD section 43). Rows are append-only: a balance change creates a new version.
/// </remarks>
public sealed class ConfigVersion
{
    public long Id { get; set; }

    /// <summary>Monotonic, human-quotable version number, e.g. 1, 2, 3.</summary>
    public int VersionNumber { get; set; }

    /// <summary>SHA-256 over the canonicalised config payload, so an identical set is recognisable.</summary>
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>The full config set as stored JSON.</summary>
    public string PayloadJson { get; set; } = "{}";

    /// <summary>Whether this is the version the server currently resolves game rules against.</summary>
    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>Admin account that published this version; null for versions seeded from the repository.</summary>
    public Guid? CreatedByAdminId { get; set; }

    public string? Note { get; set; }
}
