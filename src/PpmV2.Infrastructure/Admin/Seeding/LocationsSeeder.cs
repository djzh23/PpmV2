using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PpmV2.Infrastructure.Persistence;

namespace PpmV2.Infrastructure.Admin.Seeding;

public static class LocationsSeeder
{
    public static async Task SeedAsync(
        AppDbContext dbContext,
        IConfiguration configuration,
        ILogger logger)
    {
        var enabled = bool.Parse(configuration["Seeding:Locations:Enabled"] ?? "false");
        if (!enabled)
        {
            logger.LogInformation("LocationsSeeder disabled (Seeding:Locations:Enabled=false).");
            return;
        }

        // If you prefer partial seeding, we can seed-by-name.
        // For now: seed only when table is empty.
        if (await dbContext.Locations.AnyAsync())
        {
            logger.LogInformation("Locations already exist. Skipping LocationsSeeder.");
            return;
        }

        dbContext.Locations.AddRange(
            new() { Name = "Wohnunterkunft Neuenfelde", District = "Wilhelmsburg", Address = "Karl-Arnold-Ring", IsActive = true, Notes = null },
            new() { Name = "Wohnunterkunft Curslack I", District = "Bergedorf", Address = "Curslacker Heerweg", IsActive = true, Notes = null },
            new() { Name = "Wohnunterkunft Hamm", District = "Hamm", Address = "Friesenstraße 14", IsActive = true, Notes = null },
            new() { Name = "Wohnunterkunft Kirchwerder", District = "Kirchwerder", Address = "Am Süderbrack 9", IsActive = true, Notes = null },
            new() { Name = "Wohnunterkunft Harburg", District = "Harburg", Address = "Am Röhricht 18", IsActive = true, Notes = null },
            new() { Name = "Erstunterkunft Harburg", District = "Harburg", Address = "Neuländer Platz 1", IsActive = true, Notes = "Große Unterkunft" },
            new() { Name = "Wohnunterkunft Billbrook", District = "Billbrook", Address = "Benzelstraße 5", IsActive = true, Notes = null },
            new() { Name = "UPH HafenCity", District = "Kirchwerder", Address = "Haberföken 2", IsActive = true, Notes = "Unbegleitete minderjährige" },
            new() { Name = "Wohnunterkunft Curslack II", District = "Bergedorf", Address = "Curslacker Heerweg", IsActive = true, Notes = null },
            new() { Name = "Wohnunterkunft Brookkehre", District = "Bergedorf", Address = "Brookkehre 7", IsActive = true, Notes = null },
            new() { Name = "Wohnunterkunft Gleisdreieck", District = "Bergedorf", Address = "Am Gleisdreieck", IsActive = true, Notes = null },
            new() { Name = "Wohnunterkunft Vogelhüttendeich", District = "Wilhelmsburg", Address = "Vogelhüttendeich", IsActive = true, Notes = null }
        );

        await dbContext.SaveChangesAsync();

        logger.LogInformation("LocationsSeeder finished. Seeded {Count} locations.", 12);
    }
}
