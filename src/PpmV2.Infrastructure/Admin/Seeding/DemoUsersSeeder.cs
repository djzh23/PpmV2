using Microsoft.AspNetCore.Identity;
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
        ILogger logger)
    {
        var enabled = bool.Parse(configuration["Seeding:DemoUsers:Enabled"] ?? "false");

        if (!enabled)
        {
            logger.LogInformation("DemoUsersSeeder disabled (Seeding:DemoUsers:Enabled=false).");
            return;
        }

        var password = configuration["Seeding:DemoUsers:DefaultPassword"] ?? "Pass123$";


        // 3 Coordinators, 3 Festmitarbeiter, 3 Honorarkraft
        var demoUsers = new (string Email, string Firstname, string Lastname, UserRole Role)[]
        {
            ("koord1@test.com", "Koordinator", "One",   UserRole.Coordinator),
            ("koord2@test.com", "Koordinator", "Two",   UserRole.Coordinator),
            ("koord3@test.com", "Koordinator", "Three", UserRole.Coordinator),

            ("fest1@test.com",  "Fest", "One",   UserRole.Festmitarbeiter),
            ("fest2@test.com",  "Fest", "Two",   UserRole.Festmitarbeiter),
            ("fest3@test.com",  "Fest", "Three", UserRole.Festmitarbeiter),

            ("hon1@test.com",   "Honorar", "One",   UserRole.Honorarkraft),
            ("hon2@test.com",   "Honorar", "Two",   UserRole.Honorarkraft),
            ("hon3@test.com",   "Honorar", "Three", UserRole.Honorarkraft),
        };

        var createdCount = 0;
        var updatedCount = 0;

        foreach (var u in demoUsers)
        {
            var existing = await userManager.FindByEmailAsync(u.Email);

            if (existing is null)
            {
                var user = new AppUser
                {
                    UserName = u.Email,
                    Email = u.Email,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    logger.LogWarning(
                        "Failed creating demo user {Email}. Errors: {Errors}",
                        u.Email,
                        string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    continue;
                }

                existing = user;
                createdCount++;
                logger.LogInformation("Created demo user: {Email}", u.Email);
            }
            else
            {
                // optional: ensure confirmed
                if (!existing.EmailConfirmed)
                {
                    existing.EmailConfirmed = true;
                    var updateResult = await userManager.UpdateAsync(existing);
                    if (!updateResult.Succeeded)
                    {
                        logger.LogWarning(
                            "Failed updating demo user {Email}. Errors: {Errors}",
                            u.Email,
                            string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                    }
                    else
                    {
                        updatedCount++;
                    }
                }
            }

            // Ensure role
            var roleName = u.Role.ToString();
            if (!await userManager.IsInRoleAsync(existing, roleName))
            {
                var addRole = await userManager.AddToRoleAsync(existing, roleName);
                if (!addRole.Succeeded)
                {
                    logger.LogWarning(
                        "Failed assigning role {Role} to {Email}. Errors: {Errors}",
                        roleName,
                        u.Email,
                        string.Join(", ", addRole.Errors.Select(e => e.Description)));
                }
                else
                {
                    logger.LogInformation("Assigned role {Role} to {Email}", roleName, u.Email);
                }
            }
        }

        logger.LogInformation("DemoUsersSeeder finished. Created: {Created}, Updated: {Updated}", createdCount, updatedCount);
    }
}
