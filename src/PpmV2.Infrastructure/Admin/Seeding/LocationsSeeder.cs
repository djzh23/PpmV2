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

        var now = timeProvider.GetUtcNow().UtcDateTime;

        dbContext.Locations.AddRange(DemoCatalog.Places.Select(place => new Location
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

        logger.LogInformation("LocationsSeeder finished. Seeded {Count} locations.", DemoCatalog.Places.Length);
    }
}
