using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PpmV2.Infrastructure.Persistence;

namespace PpmV2.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory for integration tests.
///
/// Database strategy: uses the docker-compose PostgreSQL instance (port 5433)
/// with a dedicated test database (ppmv2_test), isolated from the development
/// database (ppmv2). Testcontainers was evaluated but rejected because
/// Windows Application Control policies on this machine block Docker.DotNet DLLs.
///
/// Prerequisites: docker-compose up -d db (postgres service) must be running.
/// The connection string can be overridden via the PPMV2_TEST_CONNECTION_STRING
/// environment variable for CI environments.
///
/// Data isolation: tests use random GUIDs in emails and names, making them
/// idempotent. The admin account is seeded idempotently by AdminSeeder.
/// The test database accumulates data across runs without conflict.
/// </summary>
public sealed class PpmV2WebApplicationFactory : WebApplicationFactory<Program>
{
    // Default: docker-compose postgres on port 5433, dedicated test database.
    // Override via environment variable in CI (e.g. GitHub Actions postgres service).
    private static readonly string TestConnectionString =
        Environment.GetEnvironmentVariable("PPMV2_TEST_CONNECTION_STRING")
        ?? "Host=localhost;Port=5433;Database=ppmv2_test;Username=ppmv2;Password=ppmv2_password";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Enable AdminSeeder so tests can log in as admin to approve users.
        // All other seeders are disabled — tests create their own data.
        builder.UseSetting("AdminSeed:Enabled", "true");
        builder.UseSetting("AdminSeed:Email", "admin@test.local");
        builder.UseSetting("AdminSeed:Password", "Pass123$");
        builder.UseSetting("Seeding:DemoUsers:Enabled", "false");
        builder.UseSetting("Seeding:Locations:Enabled", "false");
        builder.UseSetting("Seeding:Shifts:Enabled", "false");

        builder.ConfigureServices(services =>
        {
            // Replace the production DbContext registration with the test database.
            // Only the connection string changes — all EF configurations, migrations,
            // and DI registrations remain identical to production.
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(TestConnectionString));
        });
    }
}
