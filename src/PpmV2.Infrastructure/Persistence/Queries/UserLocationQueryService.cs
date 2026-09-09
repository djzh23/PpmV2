using Microsoft.EntityFrameworkCore;
using PpmV2.Application.Locations.DTOs;
using PpmV2.Application.Users.DTOs;
using PpmV2.Application.Users.Interfaces;
using PpmV2.Domain.Users;

namespace PpmV2.Infrastructure.Persistence.Queries;

public sealed class UserLocationQueryService : IUserLocationQuery
{
    private readonly AppDbContext _db;

    public UserLocationQueryService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<LocationListItemDto>> GetAssignedLocationsAsync(Guid userId, CancellationToken ct)
    {
        return await _db.UserLocationAssignments
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .Join(_db.Locations, a => a.LocationId, l => l.Id, (a, l) => l)
            .Where(l => l.IsActive)
            .OrderBy(l => l.District).ThenBy(l => l.Name)
            .Select(l => new LocationListItemDto(l.Id, l.Name, l.District))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<StaffMemberDto>> GetAvailableStaffAsync(Guid locationId, DateOnly date, CancellationToken ct)
    {
        // Staff assigned to this location
        var assignedUserIds = await _db.UserLocationAssignments
            .AsNoTracking()
            .Where(a => a.LocationId == locationId)
            .Select(a => a.UserId)
            .ToListAsync(ct);

        if (assignedUserIds.Count == 0)
            return [];

        // Users with a conflicting shift on this date
        var dayStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEnd   = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var busyUserIds = await _db.EinsatzParticipants
            .AsNoTracking()
            .Where(p => assignedUserIds.Contains(p.UserId))
            .Where(p => p.ConfirmationStatus != Domain.Shifts.ParticipantConfirmationStatus.Declined)
            .Join(_db.Einsaetze, p => p.ShiftId, e => e.Id, (p, e) => new { p.UserId, e.StartAtUtc, e.EndAtUtc })
            .Where(x => x.StartAtUtc < dayEnd && (x.EndAtUtc == null || x.EndAtUtc > dayStart))
            .Select(x => x.UserId)
            .Distinct()
            .ToListAsync(ct);

        var availableIds = assignedUserIds.Except(busyUserIds).ToList();

        return await _db.Users
            .AsNoTracking()
            .Where(u => availableIds.Contains(u.Id) && u.Role == UserRole.Festmitarbeiter)
            .Join(_db.UserProfiles, u => u.Id, p => p.IdentityUserId,
                (u, p) => new StaffMemberDto(u.Id, p.Firstname, p.Lastname, u.Role.ToString()))
            .ToListAsync(ct);
    }
}
