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
            new() { Name = "Wohnanlage Elbpark", District = "Altona", Address = "Elbchaussee 212", IsActive = true, Notes = null },
            new() { Name = "Sozialunterkunft Nordlicht", District = "Eimsbüttel", Address = "Lutterothstraße 98", IsActive = true, Notes = null },
            new() { Name = "Wohnzentrum Süderelbe", District = "Harburg", Address = "Rehrstieg 45", IsActive = true, Notes = null },
            new() { Name = "Unterkunft Am Stadtdeich", District = "Rothenburgsort", Address = "Billwerder Neuer Deich 23", IsActive = true, Notes = null },
            new() { Name = "Wohnhaus Sonnenhof", District = "Wandsbek", Address = "Friedrich-Ebert-Damm 112", IsActive = true, Notes = null },
            new() { Name = "Erstaufnahme Elbbrücken", District = "Hammerbrook", Address = "Spaldingstraße 160", IsActive = true, Notes = "Zentrale Erstaufnahme" },
            new() { Name = "Gemeinschaftsunterkunft Moorfleet", District = "Moorfleet", Address = "Moorfleeter Hauptdeich 75", IsActive = true, Notes = null },
            new() { Name = "UPH Alsterblick", District = "Winterhude", Address = "Barmbeker Straße 87", IsActive = true, Notes = "Unbegleitete Minderjährige" },
            new() { Name = "Wohnanlage Grünau", District = "Bergedorf", Address = "Lohbrügger Landstraße 210", IsActive = true, Notes = null },
            new() { Name = "Unterkunft Am Gleisfeld", District = "Billstedt", Address = "Manshardtstraße 64", IsActive = true, Notes = null },
            new() { Name = "Wohnunterkunft Hafenrand", District = "Veddel", Address = "Peutestraße 42", IsActive = true, Notes = null },
            new() { Name = "Wohnprojekt Regenbogenhof", District = "Wilhelmsburg", Address = "Krieterstraße 18", IsActive = true, Notes = null }
        );


        await dbContext.SaveChangesAsync();

        logger.LogInformation("LocationsSeeder finished. Seeded {Count} locations.", 12);
    }
}
