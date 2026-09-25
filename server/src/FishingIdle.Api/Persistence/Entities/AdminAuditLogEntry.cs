namespace FishingIdle.Api.Persistence.Entities;

/// <summary>
/// Immutable audit record for an administrative action.
/// </summary>
/// <remarks>
/// Required by GDD section 41: important economy, market and admin operations leave an
/// immutable trail. Rows are never updated or deleted by application code.
/// </remarks>
public sealed class AdminAuditLogEntry
{
    public long Id { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }

    /// <summary>Admin account responsible, or null for an automated/system action.</summary>
    public Guid? ActorAdminId { get; set; }

    /// <summary>Stable action key, e.g. <c>config.publish</c>.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Entity kind the action targeted, e.g. <c>ConfigVersion</c>.</summary>
    public string TargetType { get; set; } = string.Empty;

    public string? TargetId { get; set; }

    /// <summary>Structured detail. Must never contain secrets or tokens.</summary>
    public string DetailJson { get; set; } = "{}";
}
