using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PpmV2.Infrastructure.Identity;
using PpmV2.Infrastructure.Persistence;

namespace PpmV2.Infrastructure.Admin.Seeding;

/// <summary>
/// One-shot replacement of the old demo shifts, participants, assignments and
/// locations. Accounts stay. The admin account is never renamed.
/// Does nothing unless Seeding:ReplaceDemoContent:Enabled is true.
/// </summary>
public static class DemoContentReplacer
{
    public static async Task<bool> TryReplaceAsync(
        UserManager<AppUser> userManager,
        AppDbContext dbContext,
        IConfiguration configuration,
        TimeProvider timeProvider,
        ILogger logger)
    {
        if (!IsEnabled(configuration, "Seeding:ReplaceDemoContent:Enabled"))
        {
            logger.LogInformation("ReplaceDemoContent is off. Existing rows stay as they are.");
            return false;
        }

        if (!dbContext.Database.IsRelational())
        {
            logger.LogWarning("ReplaceDemoContent needs a relational database. Nothing was deleted.");
            return false;
        }

        if (!SeedFlagsAllowRefill(configuration))
        {
            logger.LogWarning("ReplaceDemoContent skipped because a demo seed flag is off. Nothing was deleted.");
            return false;
        }

        if (await IsAlreadyReplacedAsync(dbContext))
        {
            logger.LogInformation("ReplaceDemoContent already applied. Nothing was deleted.");
            return false;
        }

        if (!await HasLegacyDemoMarkerAsync(dbContext))
        {
            logger.LogInformation("ReplaceDemoContent found no previous demo rows. Nothing was deleted.");
            return false;
        }

        var strangerCount = await CountStrangersAsync(dbContext, configuration);
        if (strangerCount > 0)
        {
            logger.LogWarning(
                "ReplaceDemoContent aborted. {Count} accounts are outside the admin and demo set. Nothing was deleted.",
                strangerCount);
            return false;
        }

        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync();

            await RenameDemoUsersAsync(userManager, dbContext, timeProvider, logger);

            await dbContext.EinsatzParticipants.ExecuteDeleteAsync();
            await dbContext.Einsaetze.ExecuteDeleteAsync();
            await dbContext.UserLocationAssignments.ExecuteDeleteAsync();
            await dbContext.Locations.ExecuteDeleteAsync();

            await DemoUsersSeeder.SeedAsync(userManager, dbContext, configuration, timeProvider, logger);
            await LocationsSeeder.SeedAsync(dbContext, configuration, logger, timeProvider);
            await ShiftsSeeder.SeedAsync(dbContext, userManager, configuration, timeProvider, logger);
            await UserLocationAssignmentSeeder.SeedAsync(dbContext, userManager, configuration, timeProvider, logger);

            var markerExists = await dbContext.Einsaetze.AnyAsync(s => s.Id == DemoCatalog.MarkerShiftId);
            var placeExists = await dbContext.Locations.AnyAsync(l => l.Name == DemoCatalog.Places[0].Name);
            if (!markerExists || !placeExists)
                throw new InvalidOperationException("The new demo rows were not inserted. The replacement was rolled back.");

            await transaction.CommitAsync();
            logger.LogInformation("ReplaceDemoContent finished. Demo accounts were renamed in place and the operational rows were replaced.");
            return true;
        }
        catch (Exception ex)
        {
            dbContext.ChangeTracker.Clear();
            logger.LogError(ex, "ReplaceDemoContent failed. The transaction was rolled back and the previous rows stay in place.");
            return false;
        }
    }

    private static bool SeedFlagsAllowRefill(IConfiguration configuration)
    {
        return IsEnabled(configuration, "Seeding:DemoUsers:Enabled")
            && IsEnabled(configuration, "Seeding:Locations:Enabled")
            && IsEnabled(configuration, "Seeding:Shifts:Enabled")
            && IsEnabled(configuration, "Seeding:UserLocationAssignments:Enabled");
    }

    private static async Task<bool> IsAlreadyReplacedAsync(AppDbContext dbContext)
    {
        var markerExists = await dbContext.Einsaetze.AnyAsync(s => s.Id == DemoCatalog.MarkerShiftId);
        if (!markerExists)
            return false;

        return !await HasLegacyDemoMarkerAsync(dbContext);
    }

    private static async Task<bool> HasLegacyDemoMarkerAsync(AppDbContext dbContext)
    {
        if (await dbContext.Einsaetze.AnyAsync(s => s.Id == DemoCatalog.LegacyShiftId))
            return true;

        if (await dbContext.Locations.AnyAsync(l => l.Name == DemoCatalog.LegacyLocationName))
            return true;

        var legacyEmails = DemoCatalog.People.Select(p => p.LegacyEmail).ToArray();
        return await dbContext.Users.AnyAsync(u => u.Email != null && legacyEmails.Contains(u.Email));
    }

    private static async Task<int> CountStrangersAsync(AppDbContext dbContext, IConfiguration configuration)
    {
        var adminEmail = configuration["AdminSeed:Email"];
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(adminEmail))
            allowed.Add(adminEmail);

        foreach (var person in DemoCatalog.People)
        {
            allowed.Add(person.Email);
            allowed.Add(person.LegacyEmail);
        }

        var emails = await dbContext.Users.Select(u => u.Email).ToListAsync();
        return emails.Count(email => string.IsNullOrWhiteSpace(email) || !allowed.Contains(email));
    }

    private static async Task RenameDemoUsersAsync(
        UserManager<AppUser> userManager,
        AppDbContext dbContext,
        TimeProvider timeProvider,
        ILogger logger)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var person in DemoCatalog.People)
        {
            var user = await userManager.FindByEmailAsync(person.LegacyEmail);
            if (user is null)
                continue;

            var emailResult = await userManager.SetEmailAsync(user, person.Email);
            if (!emailResult.Succeeded)
                throw new InvalidOperationException($"Could not rename demo account {person.LegacyEmail}: {string.Join(", ", emailResult.Errors.Select(e => e.Description))}");

            var nameResult = await userManager.SetUserNameAsync(user, person.Email);
            if (!nameResult.Succeeded)
                throw new InvalidOperationException($"Could not rename demo login {person.LegacyEmail}: {string.Join(", ", nameResult.Errors.Select(e => e.Description))}");

            var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(p => p.IdentityUserId == user.Id);
            if (profile is null)
                continue;

            profile.Email = person.Email;
            profile.Firstname = person.Firstname;
            profile.Lastname = person.Lastname;
            profile.UpdatedAt = now;
            logger.LogInformation("Renamed demo account {LegacyEmail} to {Email}.", person.LegacyEmail, person.Email);
        }

        await dbContext.SaveChangesAsync();
    }

    private static bool IsEnabled(IConfiguration configuration, string key)
        => bool.TryParse(configuration[key], out var enabled) && enabled;
}
