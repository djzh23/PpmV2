using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PpmV2.Domain.Locations;
using PpmV2.Infrastructure.Persistence;

namespace PpmV2.Infrastructure.Admin.Seeding;

public static class LocationsSeeder
{
    public static async Task SeedAsync(
        AppDbContext dbContext,
        IConfiguration configuration,
        ILogger logger,
        TimeProvider timeProvider)
    {
        var enabled = bool.Parse(configuration["Seeding:Locations:Enabled"] ?? "false");
        if (!enabled)
        {
            logger.LogInformation("LocationsSeeder disabled (Seeding:Locations:Enabled=false).");
            return;
        }

        if (await dbContext.Locations.AnyAsync())
        {
            logger.LogInformation("Locations already exist. Skipping LocationsSeeder.");
            return;
        }

        await InsertMissingAsync(dbContext, timeProvider, logger);
    }

    internal static async Task InsertMissingAsync(AppDbContext dbContext, TimeProvider timeProvider, ILogger logger)
    {
        var existingNames = await dbContext.Locations.Select(l => l.Name).ToListAsync();
        var missing = DemoCatalog.Places
            .Where(place => !existingNames.Contains(place.Name, StringComparer.Ordinal))
            .ToArray();
        if (missing.Length == 0)
            return;

        var now = timeProvider.GetUtcNow().UtcDateTime;

        dbContext.Locations.AddRange(missing.Select(place => new Location
        {
            Name = place.Name,
            District = place.District,
            Address = place.Address,
            IsActive = true,
            Notes = null,
            ContactPerson = null,
            CreatedAt = now,
            UpdatedAt = now
        }));

        await dbContext.SaveChangesAsync();

        logger.LogInformation("LocationsSeeder inserted {Count} missing locations.", missing.Length);
    }
}
