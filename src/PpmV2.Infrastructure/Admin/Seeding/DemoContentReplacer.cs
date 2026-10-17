using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PpmV2.Infrastructure.Identity;
using PpmV2.Infrastructure.Persistence;

namespace PpmV2.Infrastructure.Admin.Seeding;

/// <summary>
/// Two separate switches for the same database.
/// AddFictionalDemo inserts the new catalog and renames the nine demo accounts.
/// RemoveLegacyDemo deletes only the previous demo rows, and only after the new catalog is present.
/// Both stay off unless configured. Accounts outside the admin and demo set block either step.
/// </summary>
public static class DemoContentReplacer
{
    public static async Task TryAddFictionalDemoAsync(
        UserManager<AppUser> userManager,
        AppDbContext dbContext,
        IConfiguration configuration,
        TimeProvider timeProvider,
        ILogger logger)
    {
        if (!IsEnabled(configuration, "Seeding:AddFictionalDemo:Enabled"))
        {
            logger.LogInformation("AddFictionalDemo is off. Existing rows stay as they are.");
            return;
        }

        if (!dbContext.Database.IsRelational())
        {
            logger.LogWarning("AddFictionalDemo needs a relational database. Nothing was changed.");
            return;
        }

        if (!await HasLegacyDemoMarkerAsync(dbContext))
        {
            logger.LogInformation("AddFictionalDemo found no previous demo rows. Nothing was changed.");
            return;
        }

        if (await IsCatalogAlreadyPresentAsync(dbContext))
        {
            logger.LogInformation("AddFictionalDemo already applied. Old rows were left in place.");
            return;
        }

        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync();

            await RemoveNamedProfilesAsync(userManager, dbContext, configuration, logger);

            var strangerCount = await CountStrangersAsync(dbContext, configuration);
            if (strangerCount > 0)
                throw new InvalidOperationException($"AddFictionalDemo aborted. {strangerCount} accounts are outside the admin and demo set. Nothing was changed.");

            await RenameDemoUsersAsync(userManager, dbContext, timeProvider, logger);
            await LocationsSeeder.InsertMissingAsync(dbContext, timeProvider, logger);
            await ShiftsSeeder.InsertCatalogIfMissingAsync(dbContext, userManager, timeProvider, logger);
            await UserLocationAssignmentSeeder.InsertMissingLinksAsync(dbContext, userManager, timeProvider, logger, strict: true);

            var markerExists = await dbContext.Einsaetze.AnyAsync(s => s.Id == DemoCatalog.MarkerShiftId);
            var placeExists = await dbContext.Locations.AnyAsync(l => l.Name == DemoCatalog.Places[0].Name);
            if (!markerExists || !placeExists)
                throw new InvalidOperationException("The new demo rows were not inserted. The transaction was rolled back.");

            await transaction.CommitAsync();
            logger.LogInformation("AddFictionalDemo finished. The new catalog is present and the previous rows are still there.");
        }
        catch (Exception ex)
        {
            dbContext.ChangeTracker.Clear();
            logger.LogError(ex, "AddFictionalDemo failed. The transaction was rolled back and the previous rows stay in place.");
        }
    }

    public static async Task TryRemoveLegacyDemoAsync(
        AppDbContext dbContext,
        IConfiguration configuration,
        ILogger logger)
    {
        if (!IsEnabled(configuration, "Seeding:RemoveLegacyDemo:Enabled"))
        {
            logger.LogInformation("RemoveLegacyDemo is off. Previous demo rows stay in place.");
            return;
        }

        if (IsEnabled(configuration, "Seeding:AddFictionalDemo:Enabled"))
        {
            logger.LogWarning("RemoveLegacyDemo stays off while AddFictionalDemo is on. Nothing was deleted.");
            return;
        }

        if (!dbContext.Database.IsRelational())
        {
            logger.LogWarning("RemoveLegacyDemo needs a relational database. Nothing was deleted.");
            return;
        }

        if (!await dbContext.Einsaetze.AnyAsync(s => s.Id == DemoCatalog.MarkerShiftId))
        {
            logger.LogWarning("RemoveLegacyDemo aborted because the new catalog is not present. Nothing was deleted.");
            return;
        }

        var strangerCount = await CountStrangersAsync(dbContext, configuration);
        if (strangerCount > 0)
        {
            logger.LogWarning(
                "RemoveLegacyDemo aborted. {Count} accounts are outside the admin and demo set. Nothing was deleted.",
                strangerCount);
            return;
        }

        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync();

            var legacyLocationIds = await dbContext.Locations
                .Where(l => DemoCatalog.LegacyPlaceNames.Contains(l.Name))
                .Select(l => l.Id)
                .ToListAsync();

            var legacyShiftIds = await dbContext.Einsaetze
                .Where(s => DemoCatalog.LegacyShiftIds.Contains(s.Id) || legacyLocationIds.Contains(s.LocationId))
                .Select(s => s.Id)
                .ToListAsync();

            var removedParticipants = await dbContext.EinsatzParticipants
                .Where(p => legacyShiftIds.Contains(p.ShiftId))
                .ExecuteDeleteAsync();
            var removedShifts = await dbContext.Einsaetze
                .Where(s => legacyShiftIds.Contains(s.Id))
                .ExecuteDeleteAsync();
            var removedAssignments = await dbContext.UserLocationAssignments
                .Where(a => legacyLocationIds.Contains(a.LocationId))
                .ExecuteDeleteAsync();
            var removedLocations = await dbContext.Locations
                .Where(l => legacyLocationIds.Contains(l.Id))
                .ExecuteDeleteAsync();

            await transaction.CommitAsync();
            logger.LogInformation(
                "RemoveLegacyDemo finished. Removed {Shifts} shifts, {Participants} participants, {Assignments} assignments and {Locations} locations.",
                removedShifts,
                removedParticipants,
                removedAssignments,
                removedLocations);
        }
        catch (Exception ex)
        {
            dbContext.ChangeTracker.Clear();
            logger.LogError(ex, "RemoveLegacyDemo failed. The transaction was rolled back and the previous rows stay in place.");
        }
    }

    private static async Task RemoveNamedProfilesAsync(
        UserManager<AppUser> userManager,
        AppDbContext dbContext,
        IConfiguration configuration,
        ILogger logger)
    {
        var names = configuration.GetSection("Seeding:AddFictionalDemo:RemoveProfileNames").Get<string[]>() ?? [];
        foreach (var fullName in names)
        {
            var splitAt = fullName.LastIndexOf(' ');
            if (splitAt <= 0 || splitAt == fullName.Length - 1)
                throw new InvalidOperationException($"The profile name '{fullName}' needs a first name and a last name.");

            var firstname = fullName[..splitAt].Trim();
            var lastname = fullName[(splitAt + 1)..].Trim();
            var matches = await dbContext.UserProfiles
                .AsNoTracking()
                .Where(p => p.Firstname.ToLower() == firstname.ToLower() && p.Lastname.ToLower() == lastname.ToLower())
                .Select(p => new { p.IdentityUserId, p.Email })
                .ToListAsync();

            if (matches.Count == 0)
            {
                logger.LogInformation("No account named {Name} was found.", fullName);
                continue;
            }

            if (matches.Count != 1)
                throw new InvalidOperationException($"More than one account is named {fullName}. Nothing was changed.");

            var match = matches[0];
            if (IsProtectedAccount(match.Email, configuration))
                throw new InvalidOperationException($"The account named {fullName} is an admin or demo account. Nothing was changed.");

            var user = await userManager.FindByIdAsync(match.IdentityUserId.ToString());
            if (user is null)
                throw new InvalidOperationException($"The account named {fullName} has a profile but no login. Nothing was changed.");

            await dbContext.EinsatzParticipants.Where(p => p.UserId == user.Id).ExecuteDeleteAsync();
            await dbContext.UserLocationAssignments.Where(a => a.UserId == user.Id).ExecuteDeleteAsync();
            await dbContext.RefreshTokens.Where(t => t.UserId == user.Id).ExecuteDeleteAsync();

            var deleteResult = await userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
                throw new InvalidOperationException($"Could not remove the account named {fullName}: {string.Join(", ", deleteResult.Errors.Select(e => e.Description))}");

            logger.LogInformation("Removed extra account {Name}.", fullName);
        }
    }

    private static bool IsProtectedAccount(string? email, IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var adminEmail = configuration["AdminSeed:Email"];
        if (!string.IsNullOrWhiteSpace(adminEmail) && email.Equals(adminEmail, StringComparison.OrdinalIgnoreCase))
            return true;

        return DemoCatalog.People.Any(person =>
            email.Equals(person.Email, StringComparison.OrdinalIgnoreCase)
            || email.Equals(person.LegacyEmail, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<bool> IsCatalogAlreadyPresentAsync(AppDbContext dbContext)
    {
        var markerExists = await dbContext.Einsaetze.AnyAsync(s => s.Id == DemoCatalog.MarkerShiftId);
        if (!markerExists)
            return false;

        var placeNames = DemoCatalog.Places.Select(p => p.Name).ToArray();
        var placeCount = await dbContext.Locations.CountAsync(l => placeNames.Contains(l.Name));
        if (placeCount != DemoCatalog.Places.Length)
            return false;

        var legacyEmails = DemoCatalog.People.Select(p => p.LegacyEmail).ToArray();
        var legacyUserExists = await dbContext.Users.AnyAsync(u => u.Email != null && legacyEmails.Contains(u.Email));
        return !legacyUserExists;
    }

    private static async Task<bool> HasLegacyDemoMarkerAsync(AppDbContext dbContext)
    {
        if (await dbContext.Einsaetze.AnyAsync(s => DemoCatalog.LegacyShiftIds.Contains(s.Id)))
            return true;

        if (await dbContext.Locations.AnyAsync(l => DemoCatalog.LegacyPlaceNames.Contains(l.Name)))
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
