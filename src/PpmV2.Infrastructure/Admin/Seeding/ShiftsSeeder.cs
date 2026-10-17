using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PpmV2.Domain.Shifts;
using PpmV2.Infrastructure.Identity;
using PpmV2.Infrastructure.Persistence;

namespace PpmV2.Infrastructure.Admin.Seeding;

public static class ShiftsSeeder
{
    public static async Task SeedAsync(
        AppDbContext dbContext,
        UserManager<AppUser> userManager,
        IConfiguration configuration,
        TimeProvider timeProvider,
        ILogger logger)
    {
        var enabled = bool.Parse(configuration["Seeding:Shifts:Enabled"] ?? "false");
        if (!enabled)
        {
            logger.LogInformation("ShiftsSeeder disabled (Seeding:Shifts:Enabled=false).");
            return;
        }

        if (await dbContext.Einsaetze.AnyAsync(e => e.Id == DemoCatalog.MarkerShiftId || e.Id == DemoCatalog.LegacyShiftId))
        {
            logger.LogInformation("Demo shifts already seeded. Skipping.");
            return;
        }

        var placeNames = DemoCatalog.Places.Select(p => p.Name).ToArray();
        var locations = await dbContext.Locations
            .Where(l => placeNames.Contains(l.Name))
            .ToDictionaryAsync(l => l.Name, l => l.Id);

        if (locations.Count != DemoCatalog.Places.Length)
        {
            logger.LogWarning("ShiftsSeeder skipped: the fictional locations are not all present.");
            return;
        }

        var emails = DemoCatalog.Shifts.SelectMany(s => s.Members).Select(m => m.Email).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var users = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var email in emails)
        {
            var user = await DemoUsersSeeder.FindPersonAsync(userManager, email);
            if (user is null)
            {
                logger.LogWarning("ShiftsSeeder skipped: demo user {Email} was not found.", email);
                return;
            }

            users[email] = user.Id;
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var shifts = DemoCatalog.Shifts.Select(plan => BuildShift(plan, utcNow, locations, users)).ToList();

        dbContext.Einsaetze.AddRange(shifts);
        await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "ShiftsSeeder finished. Seeded {Count} shifts.",
            shifts.Count);
    }

    private static Shift BuildShift(
        DemoShiftPlan plan,
        DateTime utcNow,
        IReadOnlyDictionary<string, Guid> locations,
        IReadOnlyDictionary<string, Guid> users)
    {
        var start = plan.AroundNow
            ? utcNow.AddHours(-2)
            : DemoClock.AtBerlin(utcNow, plan.StartDayOffset, plan.StartHour);
        var end = plan.AroundNow
            ? utcNow.AddHours(6)
            : DemoClock.AtBerlin(utcNow, plan.EndDayOffset, plan.EndHour);

        return new Shift
        {
            Id = plan.Id,
            Title = plan.Title,
            Description = plan.Description,
            StartAtUtc = start,
            EndAtUtc = end,
            LocationId = locations[plan.LocationName],
            Status = plan.Status,
            Participants = plan.Members.Select(member => new ShiftParticipant
            {
                UserId = users[member.Email],
                Role = member.Role,
                ConfirmationStatus = member.Status,
                RespondedAt = member.Status == ParticipantConfirmationStatus.Invited ? null : utcNow
            }).ToList()
        };
    }
}
