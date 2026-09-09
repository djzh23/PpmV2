using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PpmV2.Domain.Users;
using PpmV2.Infrastructure.Identity;
using PpmV2.Infrastructure.Persistence;

namespace PpmV2.Infrastructure.Admin.Seeding;

public static class UserLocationAssignmentSeeder
{
    public static async Task SeedAsync(
        AppDbContext dbContext,
        UserManager<AppUser> userManager,
        IConfiguration configuration,
        TimeProvider timeProvider,
        ILogger logger)
    {
        var enabled = bool.Parse(configuration["Seeding:UserLocationAssignments:Enabled"] ?? "false");
        if (!enabled)
        {
            logger.LogInformation("UserLocationAssignmentSeeder disabled.");
            return;
        }

        if (await dbContext.UserLocationAssignments.AnyAsync())
        {
            logger.LogInformation("UserLocationAssignments already exist. Skipping.");
            return;
        }

        var locations = await dbContext.Locations.OrderBy(l => l.Name).ToListAsync();
        if (locations.Count == 0)
        {
            logger.LogWarning("UserLocationAssignmentSeeder skipped: no locations found.");
            return;
        }

        var fest1 = await userManager.FindByEmailAsync("fest1@test.com");
        var fest2 = await userManager.FindByEmailAsync("fest2@test.com");
        var koord1 = await userManager.FindByEmailAsync("koord1@test.com");

        if (fest1 is null || fest2 is null)
        {
            logger.LogWarning("UserLocationAssignmentSeeder skipped: demo Festmitarbeiter not found.");
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        // fest1 is assigned to the first 6 locations
        var fest1Assignments = locations.Take(6).Select(l => new UserLocationAssignment
        {
            UserId = fest1.Id,
            LocationId = l.Id,
            AssignedAt = now
        });

        // fest2 is assigned to a different set (locations 3-9, overlapping with fest1 in some)
        var fest2Assignments = locations.Skip(2).Take(6).Select(l => new UserLocationAssignment
        {
            UserId = fest2.Id,
            LocationId = l.Id,
            AssignedAt = now
        });

        // koord1 gets no assignments — Coordinators see all locations implicitly

        dbContext.UserLocationAssignments.AddRange(fest1Assignments);
        dbContext.UserLocationAssignments.AddRange(fest2Assignments);
        await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "UserLocationAssignmentSeeder finished. fest1: 6 locations, fest2: 6 locations.");
    }
}
