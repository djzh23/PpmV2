using Microsoft.EntityFrameworkCore;
using PpmV2.Application.Users.DTOs;
using PpmV2.Application.Users.Interfaces;
using PpmV2.Domain.Users;

namespace PpmV2.Infrastructure.Persistence.Queries;

public sealed class StaffQueryService : IStaffQuery
{
    private readonly AppDbContext _db;

    public StaffQueryService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<StaffMemberDto>> GetAllStaffAsync(CancellationToken ct = default)
    {
        var staffRoles = new[] { UserRole.Festmitarbeiter, UserRole.Honorarkraft };

        var staff = await _db.Users
            .AsNoTracking()
            .Where(u => staffRoles.Contains(u.Role) && u.Status == UserStatus.Approved)
            .Join(_db.UserProfiles, u => u.Id, p => p.IdentityUserId,
                (u, p) => new { u.Id, p.Firstname, p.Lastname, Role = u.Role.ToString() })
            .OrderBy(x => x.Lastname)
            .ThenBy(x => x.Firstname)
            .ToListAsync(ct);

        if (staff.Count == 0)
            return [];

        var staffIds = staff.Select(s => s.Id).ToList();

        var locationsByUser = await _db.UserLocationAssignments
            .AsNoTracking()
            .Where(a => staffIds.Contains(a.UserId))
            .Join(_db.Locations, a => a.LocationId, l => l.Id,
                (a, l) => new { a.UserId, l.Id, l.Name, l.District })
            .OrderBy(x => x.District)
            .ThenBy(x => x.Name)
            .ToListAsync(ct);

        var locationLookup = locationsByUser
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<StaffLocationDto>)g
                    .Select(x => new StaffLocationDto(x.Id, x.Name, x.District))
                    .ToList()
            );

        return staff
            .Select(s => new StaffMemberDto(
                s.Id.ToString(),
                s.Firstname,
                s.Lastname,
                s.Role,
                locationLookup.TryGetValue(s.Id, out var locs) ? locs : []
            ))
            .ToList();
    }
}
