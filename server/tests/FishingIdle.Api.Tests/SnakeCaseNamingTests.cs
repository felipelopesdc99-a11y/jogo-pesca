using FishingIdle.Api.Persistence;
using Xunit;

namespace FishingIdle.Api.Tests;

public sealed class SnakeCaseNamingTests
{
    [Theory]
    [InlineData("Id", "id")]
    [InlineData("VersionNumber", "version_number")]
    [InlineData("IsActive", "is_active")]
    [InlineData("CreatedByAdminId", "created_by_admin_id")]
    [InlineData("PayloadJson", "payload_json")]
    [InlineData("OccurredAtUtc", "occurred_at_utc")]
    [InlineData("PK_ConfigVersions", "pk_config_versions")]
    [InlineData("IX_AdminAuditLog_TargetType_TargetId", "ix_admin_audit_log_target_type_target_id")]
    [InlineData("already_snake", "already_snake")]
    public void Converts_names_the_way_the_migration_expects(string input, string expected)
        => Assert.Equal(expected, SnakeCaseNaming.ToSnakeCase(input));
}
