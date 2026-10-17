using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PpmV2.Domain.Users;
using PpmV2.Infrastructure.Identity;
using PpmV2.Infrastructure.Persistence;

namespace PpmV2.Infrastructure.Admin.Seeding;

public static class DemoUsersSeeder
{
    public static async Task SeedAsync(
        UserManager<AppUser> userManager,
        AppDbContext dbContext,
        IConfiguration configuration,
        TimeProvider timeProvider,
        ILogger logger)
    {
        var enabled = bool.Parse(configuration["Seeding:DemoUsers:Enabled"] ?? "false");

        if (!enabled)
        {
            logger.LogInformation("DemoUsersSeeder disabled (Seeding:DemoUsers:Enabled=false).");
            return;
        }

        var password = configuration["Seeding:DemoUsers:DefaultPassword"] ?? "Pass123$";
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var createdCount = 0;
        var updatedCount = 0;

        foreach (var person in DemoCatalog.People)
        {
            var existing = await FindPersonAsync(userManager, person.Email);

            if (existing is null)
            {
                var user = new AppUser
                {
                    UserName = person.Email,
                    Email = person.Email,
                    EmailConfirmed = true,
                    Status = UserStatus.Approved,
                    Role = person.Role,
                    IsActive = true,
                    IsProfileCompleted = true
                };

                var createResult = await userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    logger.LogWarning(
                        "Failed creating demo user {Email}. Errors: {Errors}",
                        person.Email,
                        string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    continue;
                }

                existing = user;
                createdCount++;
                logger.LogInformation("Created demo user: {Email}", person.Email);
            }
            else
            {
                var changed = false;

                if (!existing.EmailConfirmed) { existing.EmailConfirmed = true; changed = true; }
                if (existing.Status != UserStatus.Approved) { existing.Status = UserStatus.Approved; changed = true; }
                if (existing.Role != person.Role) { existing.Role = person.Role; changed = true; }
                if (!existing.IsActive) { existing.IsActive = true; changed = true; }
                if (!existing.IsProfileCompleted) { existing.IsProfileCompleted = true; changed = true; }

                if (changed)
                {
                    var updateResult = await userManager.UpdateAsync(existing);
                    if (!updateResult.Succeeded)
                    {
                        logger.LogWarning(
                            "Failed updating demo user {Email}. Errors: {Errors}",
                            existing.Email,
                            string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                    }
                    else
                    {
                        updatedCount++;
                    }
                }
            }

            var roleName = person.Role.ToString();
            if (!await userManager.IsInRoleAsync(existing, roleName))
            {
                var addRole = await userManager.AddToRoleAsync(existing, roleName);
                if (!addRole.Succeeded)
                {
                    logger.LogWarning(
                        "Failed assigning role {Role} to {Email}. Errors: {Errors}",
                        roleName,
                        existing.Email,
                        string.Join(", ", addRole.Errors.Select(e => e.Description)));
                }
            }

            var profileExists = await dbContext.UserProfiles
                .AnyAsync(p => p.IdentityUserId == existing.Id);

            if (!profileExists)
            {
                dbContext.UserProfiles.Add(new UserProfile
                {
                    IdentityUserId = existing.Id,
                    Email = existing.Email ?? person.Email,
                    Firstname = person.Firstname,
                    Lastname = person.Lastname,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                });

                logger.LogInformation("Created UserProfile for demo user: {Email}", existing.Email);
            }
        }

        await dbContext.SaveChangesAsync();

        logger.LogInformation("DemoUsersSeeder finished. Created: {Created}, Updated: {Updated}", createdCount, updatedCount);
    }

    /// <summary>
    /// Finds the current address first, then the previous demo address.
    /// Never creates a second account for the same person.
    /// </summary>
    internal static async Task<AppUser?> FindPersonAsync(UserManager<AppUser> userManager, string email)
    {
        var person = DemoCatalog.People.FirstOrDefault(p =>
            p.Email.Equals(email, StringComparison.OrdinalIgnoreCase)
            || p.LegacyEmail.Equals(email, StringComparison.OrdinalIgnoreCase));

        if (person is null)
            return await userManager.FindByEmailAsync(email);

        return await userManager.FindByEmailAsync(person.Email)
            ?? await userManager.FindByEmailAsync(person.LegacyEmail);
    }
}
