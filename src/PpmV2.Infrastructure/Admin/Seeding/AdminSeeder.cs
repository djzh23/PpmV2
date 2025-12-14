using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PpmV2.Domain.Users;
using PpmV2.Infrastructure.Identity;

namespace PpmV2.Infrastructure.Admin.Seeding;

public static class AdminSeeder
{
    public static async Task SeedAsync(
        UserManager<AppUser> userManager,
        IConfiguration configuration,
        ILogger logger)
    {
        var section = configuration.GetSection("AdminSeed");

        var enabled = bool.TryParse(section["Enabled"], out var isEnabled) && isEnabled;
        if (!enabled)
        {
            logger.LogInformation("AdminSeeder: disabled.");
            return;
        }

        var email = section["Email"];
        var password = section["Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("AdminSeeder: Email or Password missing. Skipping seed.");
            return;
        }

        var existing = await userManager.FindByEmailAsync(email);

        if (existing == null)
        {
            var admin = new AppUser
            {
                UserName = email,
                Email = email,
                IsActive = true,
                Status = UserStatus.Approved,
                Role = UserRole.Admin,
                IsProfileCompleted = true
            };

            var createResult = await userManager.CreateAsync(admin, password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                logger.LogError("AdminSeeder: failed to create admin user. Errors: {Errors}", errors);
                return;
            }

            logger.LogInformation("AdminSeeder: created admin user {Email}", email);
            return;
        }

        var changed = false;

        if (existing.Role != UserRole.Admin)
        {
            existing.Role = UserRole.Admin;
            changed = true;
        }

        if (existing.Status != UserStatus.Approved)
        {
            existing.Status = UserStatus.Approved;
            changed = true;
        }

        if (!existing.IsActive)
        {
            existing.IsActive = true;
            changed = true;
        }

        if (!changed)
        {
            logger.LogInformation("AdminSeeder: admin user already OK {Email}", email);
            return;
        }

        var updateResult = await userManager.UpdateAsync(existing);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
            logger.LogError("AdminSeeder: failed to update admin user. Errors: {Errors}", errors);
            return;
        }

        logger.LogInformation("AdminSeeder: updated admin user {Email}", email);
    }
}
