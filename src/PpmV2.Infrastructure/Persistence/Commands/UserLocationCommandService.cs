using Microsoft.EntityFrameworkCore;
using PpmV2.Application.Common.Results;
using PpmV2.Application.Users.Interfaces;
using PpmV2.Domain.Users;

namespace PpmV2.Infrastructure.Persistence.Commands;

public sealed class UserLocationCommandService : IUserLocationCommand
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _time;

    public UserLocationCommandService(AppDbContext db, TimeProvider time)
    {
        _db = db;
        _time = time;
    }

    public async Task<ServiceResult> UpdateLocationsAsync(Guid userId, IReadOnlyList<Guid> locationIds, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
            return ServiceResult.NotFound("User not found.");

        if (user.Role != UserRole.Festmitarbeiter && user.Role != UserRole.Honorarkraft)
            return ServiceResult.Fail("Location assignments are only supported for Festmitarbeiter and Honorarkraft.");

        var existing = await _db.UserLocationAssignments
            .Where(a => a.UserId == userId)
            .ToListAsync(ct);

        _db.UserLocationAssignments.RemoveRange(existing);

        if (locationIds.Count > 0)
        {
            var validLocationIds = await _db.Locations
                .AsNoTracking()
                .Where(l => locationIds.Contains(l.Id))
                .Select(l => l.Id)
                .ToListAsync(ct);

            var now = _time.GetUtcNow().UtcDateTime;
            var assignments = validLocationIds.Select(locationId => new UserLocationAssignment
            {
                UserId = userId,
                LocationId = locationId,
                AssignedAt = now,
            });

            _db.UserLocationAssignments.AddRange(assignments);
        }

        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }
}
