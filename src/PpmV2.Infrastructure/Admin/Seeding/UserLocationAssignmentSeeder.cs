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

        var placeNames = DemoCatalog.Places.Select(p => p.Name).ToArray();
        var locations = await dbContext.Locations
            .Where(l => placeNames.Contains(l.Name))
            .ToDictionaryAsync(l => l.Name, l => l.Id);

        if (locations.Count != DemoCatalog.Places.Length)
        {
            logger.LogWarning("UserLocationAssignmentSeeder skipped: the fictional locations are not all present.");
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var assignments = new List<UserLocationAssignment>();

        foreach (var plan in DemoCatalog.Assignments)
        {
            var user = await DemoUsersSeeder.FindPersonAsync(userManager, plan.Email);
            if (user is null)
            {
                logger.LogWarning("UserLocationAssignmentSeeder skipped: demo user {Email} was not found.", plan.Email);
                return;
            }

            foreach (var locationName in plan.LocationNames)
            {
                assignments.Add(new UserLocationAssignment
                {
                    UserId = user.Id,
                    LocationId = locations[locationName],
                    AssignedAt = now
                });
            }
        }

        dbContext.UserLocationAssignments.AddRange(assignments);
        await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "UserLocationAssignmentSeeder finished. Assigned {Count} location links.",
            assignments.Count);
    }
}
