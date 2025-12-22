using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using PpmV2.Domain.Users;
using PpmV2.Infrastructure.Identity;

namespace PpmV2.Infrastructure.Admin.Seeding;

public static class RolesSeeder
{
    public static async Task SeedAsync(RoleManager<AppRole> roleManager, ILogger logger)
    {
        var roleNames = Enum.GetNames<UserRole>(); // Admin, Coordinator, Festmitarbeiter, Honorarkraft

        foreach (var roleName in roleNames)
        {
            if (await roleManager.RoleExistsAsync(roleName))
                continue;

            var result = await roleManager.CreateAsync(new AppRole { Name = roleName });

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create role '{roleName}': {errors}");
            }

            logger.LogInformation("RolesSeeder: created role {Role}", roleName);
        }
    }
}
