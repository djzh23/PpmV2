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
    private static readonly Guid NachtschichtId = new("11111111-0001-0000-0000-000000000000");
    private static readonly Guid TagdienstId    = new("11111111-0002-0000-0000-000000000000");
    private static readonly Guid WochenendId    = new("11111111-0003-0000-0000-000000000000");
    private static readonly Guid AbendId        = new("11111111-0004-0000-0000-000000000000");
    private static readonly Guid FruehId        = new("11111111-0005-0000-0000-000000000000");
    private static readonly Guid AktivId        = new("11111111-0006-0000-0000-000000000000");

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

        if (await dbContext.Einsaetze.AnyAsync(e => e.Id == NachtschichtId))
        {
            logger.LogInformation("Demo shifts already seeded. Skipping.");
            return;
        }

        var locations = await dbContext.Locations.ToListAsync();
        if (locations.Count == 0)
        {
            logger.LogWarning("ShiftsSeeder skipped: no locations found.");
            return;
        }

        // Resolve demo user IDs by email
        var koord1 = await userManager.FindByEmailAsync("koord1@test.com");
        var koord2 = await userManager.FindByEmailAsync("koord2@test.com");
        var fest1  = await userManager.FindByEmailAsync("fest1@test.com");
        var fest2  = await userManager.FindByEmailAsync("fest2@test.com");
        var hon1   = await userManager.FindByEmailAsync("hon1@test.com");
        var hon2   = await userManager.FindByEmailAsync("hon2@test.com");

        if (koord1 is null || fest1 is null)
        {
            logger.LogWarning("ShiftsSeeder skipped: demo users not found. Run DemoUsersSeeder first.");
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        var shifts = new List<Shift>
        {
            // Draft shifts — future, not yet published
            new()
            {
                Id = NachtschichtId,
                Title = "Nachtschicht Elbpark",
                Description = "Reguläre Nachtschicht mit Einlasskontrolle.",
                StartAtUtc = now.AddDays(3).Date.AddHours(22),
                EndAtUtc   = now.AddDays(4).Date.AddHours(6),
                LocationId = locations[0].Id,
                Status     = ShiftStatus.Draft,
                Participants = fest1 is not null && hon1 is not null ? new List<ShiftParticipant>
                {
                    new() { UserId = fest1.Id, Role = ShiftRole.Leader },
                    new() { UserId = hon1.Id,  Role = ShiftRole.Member }
                } : new List<ShiftParticipant>()
            },
            new()
            {
                Id = TagdienstId,
                Title = "Tagdienst Nordlicht",
                Description = "Unterstützung bei der Essensausgabe und Betreuung.",
                StartAtUtc = now.AddDays(5).Date.AddHours(8),
                EndAtUtc   = now.AddDays(5).Date.AddHours(16),
                LocationId = locations[1].Id,
                Status     = ShiftStatus.Draft,
                Participants = fest2 is not null ? new List<ShiftParticipant>
                {
                    new() { UserId = fest2.Id, Role = ShiftRole.Leader }
                } : new List<ShiftParticipant>()
            },
            new()
            {
                Id = WochenendId,
                Title = "Wochenendschicht Süderelbe",
                Description = null,
                StartAtUtc = now.AddDays(8).Date.AddHours(10),
                EndAtUtc   = now.AddDays(8).Date.AddHours(18),
                LocationId = locations[2].Id,
                Status     = ShiftStatus.Draft,
                Participants = new List<ShiftParticipant>()
            },

            // Planned shifts — published, participants assigned
            new()
            {
                Id = AbendId,
                Title = "Abendbetreuung Stadtdeich",
                Description = "Abendliche Betreuungsrunde und Sicherheitskontrolle.",
                StartAtUtc = now.AddDays(1).Date.AddHours(18),
                EndAtUtc   = now.AddDays(1).Date.AddHours(23),
                LocationId = locations[3].Id,
                Status     = ShiftStatus.Planned,
                Participants = koord2 is not null && hon1 is not null && hon2 is not null
                    ? new List<ShiftParticipant>
                    {
                        new() { UserId = koord2.Id, Role = ShiftRole.Leader },
                        new() { UserId = hon1.Id,   Role = ShiftRole.Member },
                        new() { UserId = hon2.Id,   Role = ShiftRole.Support }
                    }
                    : new List<ShiftParticipant>()
            },
            new()
            {
                Id = FruehId,
                Title = "Frühdienst Sonnenhof",
                Description = "Frühschicht mit Frühstücksausgabe.",
                StartAtUtc = now.AddDays(2).Date.AddHours(6),
                EndAtUtc   = now.AddDays(2).Date.AddHours(14),
                LocationId = locations[4].Id,
                Status     = ShiftStatus.Planned,
                Participants = koord1 is not null && fest1 is not null && fest2 is not null
                    ? new List<ShiftParticipant>
                    {
                        new() { UserId = koord1.Id, Role = ShiftRole.Leader },
                        new() { UserId = fest1.Id,  Role = ShiftRole.Member },
                        new() { UserId = fest2.Id,  Role = ShiftRole.Member }
                    }
                    : new List<ShiftParticipant>()
            },

            // Active shift — currently running
            new()
            {
                Id = AktivId,
                Title = "Laufende Schicht Elbbrücken",
                Description = "Aktive Schicht — Einlasskontrolle und Betreuung.",
                StartAtUtc = now.AddHours(-2),
                EndAtUtc   = now.AddHours(4),
                LocationId = locations[5].Id,
                Status     = ShiftStatus.Active,
                Participants = koord2 is not null && hon1 is not null
                    ? new List<ShiftParticipant>
                    {
                        new() { UserId = koord2.Id, Role = ShiftRole.Leader },
                        new() { UserId = hon1.Id,   Role = ShiftRole.Member }
                    }
                    : new List<ShiftParticipant>()
            }
        };

        dbContext.Einsaetze.AddRange(shifts);
        await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "ShiftsSeeder finished. Seeded {Count} shifts ({Draft} Draft, {Planned} Planned, {Active} Active).",
            shifts.Count,
            shifts.Count(s => s.Status == ShiftStatus.Draft),
            shifts.Count(s => s.Status == ShiftStatus.Planned),
            shifts.Count(s => s.Status == ShiftStatus.Active));
    }
}
