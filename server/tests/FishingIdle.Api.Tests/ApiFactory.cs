using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FishingIdle.Api.Tests;

/// <summary>
/// Hosts the API in-process for endpoint tests.
/// </summary>
/// <remarks>
/// The connection string deliberately points at a port nothing listens on, so the database
/// dependency is reliably unreachable. That makes the "server up, database down" path — the exact
/// case the Unity diagnostic surface must distinguish — testable without a database.
/// </remarks>
public sealed class UnreachableDatabaseApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Port 1 is privileged and unused, so the connection fails fast rather than hanging.</summary>
    private const string UnreachableConnectionString =
        "Host=127.0.0.1;Port=1;Database=fishing_idle;Username=test;Password=test;Timeout=1;Command Timeout=1";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = UnreachableConnectionString,
                ["DevConsole:AllowedOrigins:0"] = "http://localhost:3000",
            });
        });

        builder.ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));
    }
}
