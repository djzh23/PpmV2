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

        await InsertMissingLinksAsync(dbContext, userManager, timeProvider, logger, strict: false);
    }

    internal static async Task InsertMissingLinksAsync(
        AppDbContext dbContext,
        UserManager<AppUser> userManager,
        TimeProvider timeProvider,
        ILogger logger,
        bool strict)
    {
        var placeNames = DemoCatalog.Places.Select(p => p.Name).ToArray();
        var locations = await dbContext.Locations
            .Where(l => placeNames.Contains(l.Name))
            .ToDictionaryAsync(l => l.Name, l => l.Id);

        if (locations.Count != DemoCatalog.Places.Length)
            throw new InvalidOperationException("The fictional locations are not all present, so the new assignments were not inserted.");

        var existing = await dbContext.UserLocationAssignments
            .Select(a => new { a.UserId, a.LocationId })
            .ToListAsync();
        var existingPairs = existing.Select(a => (a.UserId, a.LocationId)).ToHashSet();

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var assignments = new List<UserLocationAssignment>();

        foreach (var plan in DemoCatalog.Assignments)
        {
            var user = await DemoUsersSeeder.FindPersonAsync(userManager, plan.Email);
            if (user is null)
            {
                if (strict)
                    throw new InvalidOperationException($"Demo user {plan.Email} was not found, so the new assignments were not inserted.");

                logger.LogWarning("UserLocationAssignmentSeeder skipped: demo user {Email} was not found.", plan.Email);
                return;
            }

            foreach (var locationName in plan.LocationNames)
            {
                var pair = (user.Id, locations[locationName]);
                if (!existingPairs.Add(pair))
                    continue;

                assignments.Add(new UserLocationAssignment
                {
                    UserId = pair.Item1,
                    LocationId = pair.Item2,
                    AssignedAt = now
                });
            }
        }

        if (assignments.Count == 0)
        {
            logger.LogInformation("Fictional location links are already present.");
            return;
        }

        dbContext.UserLocationAssignments.AddRange(assignments);
        await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "UserLocationAssignmentSeeder inserted {Count} location links.",
            assignments.Count);
    }
}
