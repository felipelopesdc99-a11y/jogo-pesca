using FishingIdle.Api.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FishingIdle.Api.Persistence;

/// <summary>
/// The authoritative database context. Schema is managed exclusively through EF Core migrations.
/// </summary>
/// <remarks>
/// Milestone 0 establishes the two tables the config pipeline and the audit trail need
/// (<c>config_versions</c>, <c>admin_audit_log</c>). Gameplay entities arrive with the
/// milestone that owns them; see docs/roadmap.json.
/// </remarks>
public sealed class FishingIdleDbContext : DbContext
{
    public FishingIdleDbContext(DbContextOptions<FishingIdleDbContext> options)
        : base(options)
    {
    }

    public DbSet<ConfigVersion> ConfigVersions => Set<ConfigVersion>();

    public DbSet<AdminAuditLogEntry> AdminAuditLog => Set<AdminAuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConfigVersion>(entity =>
        {
            entity.ToTable("config_versions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.VersionNumber).IsRequired();
            entity.Property(x => x.ContentHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.Note).HasMaxLength(500);
            entity.HasIndex(x => x.VersionNumber).IsUnique();

            // At most one active config version can exist at a time.
            entity.HasIndex(x => x.IsActive)
                .IsUnique()
                .HasFilter("is_active");
        });

        modelBuilder.Entity<AdminAuditLogEntry>(entity =>
        {
            entity.ToTable("admin_audit_log");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Action).HasMaxLength(100).IsRequired();
            entity.Property(x => x.TargetType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.TargetId).HasMaxLength(100);
            entity.Property(x => x.DetailJson).HasColumnType("jsonb").IsRequired();
            entity.HasIndex(x => x.OccurredAtUtc);
            entity.HasIndex(x => new { x.TargetType, x.TargetId });
        });

        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplySnakeCaseNames();
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Every timestamp is stored as UTC. The server owns time; the client is never trusted for it.
        configurationBuilder.Properties<DateTimeOffset>().HaveColumnType("timestamptz");
        base.ConfigureConventions(configurationBuilder);
    }
}
