using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FishingIdle.Api.Persistence;

/// <summary>
/// Lets <c>dotnet ef</c> build the context without starting the web host.
/// </summary>
/// <remarks>
/// Adding or scripting a migration never touches a live database, so a placeholder
/// connection string is enough. Applying a migration uses the real connection string from
/// <c>FISHINGIDLE_ConnectionStrings__Postgres</c>; see ops/scripts/migrate.sh.
/// </remarks>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FishingIdleDbContext>
{
    private const string PlaceholderConnectionString =
        "Host=localhost;Port=5432;Database=fishing_idle;Username=fishing_idle;Password=design_time_placeholder";

    public FishingIdleDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("FISHINGIDLE_ConnectionStrings__Postgres")
            ?? PlaceholderConnectionString;

        var options = new DbContextOptionsBuilder<FishingIdleDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new FishingIdleDbContext(options);
    }
}
