using Microsoft.EntityFrameworkCore;
using PpmV2.Application.Shifts.DTOs;
using PpmV2.Application.Shifts.Interfaces;
using PpmV2.Domain.Shifts;
using PpmV2.Domain.Users;

namespace PpmV2.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core persistence implementation for Shifts (Einsaetze).
/// Implements write-port, details query, list query, and workflow repository.
/// Note: Legacy naming ("Einsaetze") retained for DB schema compatibility.
/// </summary>
public sealed class ShiftRepository : IShiftRepository, IShiftDetailsQuery, IShiftListQuery, IShiftWorkflowRepository
{
    private readonly AppDbContext _db;

    public ShiftRepository(AppDbContext db) => _db = db;

    // ---------- Write-Port (Create) ----------

    public Task<bool> LocationExistsAsync(Guid locationId, CancellationToken ct) =>
        _db.Locations.AsNoTracking().AnyAsync(l => l.Id == locationId, ct);

    public Task<int> CountExistingUsersAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct) =>
        _db.Users.AsNoTracking().CountAsync(u => userIds.Contains(u.Id), ct);

    public Task AddAsync(Shift einsatz, IReadOnlyCollection<ShiftParticipant> participants, CancellationToken ct)
    {
        _db.Einsaetze.Add(einsatz);
        _db.EinsatzParticipants.AddRange(participants);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct) =>
        _db.SaveChangesAsync(ct);

    // ---------- Workflow Repository ----------

    public async Task<Shift?> GetWithParticipantsAsync(Guid shiftId, CancellationToken ct)
    {
        return await _db.Einsaetze
            .Include(e => e.Participants)
            .FirstOrDefaultAsync(e => e.Id == shiftId, ct);
    }

    // ---------- Read-Port (Details Query) ----------

    public async Task<ShiftDetailsDto?> GetByIdAsync(Guid einsatzId, CancellationToken ct)
    {
        var einsatz = await _db.Einsaetze
            .AsNoTracking()
            .Where(e => e.Id == einsatzId)
            .Select(e => new
            {
                e.Id,
                e.Title,
                e.Description,
                e.StartAtUtc,
                e.EndAtUtc,
                e.Status,
                e.LocationId
            })
            .FirstOrDefaultAsync(ct);

        if (einsatz is null) return null;

        var location = await _db.Locations
            .AsNoTracking()
            .Where(l => l.Id == einsatz.LocationId)
            .Select(l => new ShiftLocationDto
            {
                Id = l.Id,
                Name = l.Name,
                District = l.District,
                Address = l.Address
            })
            .FirstAsync(ct);

        var participants = await _db.EinsatzParticipants
            .AsNoTracking()
            .Where(p => p.ShiftId == einsatz.Id)
            .Join(_db.UserProfiles, p => p.UserId, up => up.IdentityUserId,
                (p, up) => new ShiftParticipantDto
                {
                    UserId = p.UserId,
                    Firstname = up.Firstname,
                    Lastname = up.Lastname,
                    Role = p.Role,
                    ConfirmationStatus = p.ConfirmationStatus
                })
            .ToListAsync(ct);

        var missing = new List<string>();

        if (participants.Count(p => p.Role == ShiftRole.Leader) != 1)
            missing.Add("leader");

        var userIds = participants.Select(p => p.UserId).Distinct().ToList();
        var festCount = await _db.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .CountAsync(u => u.Role == UserRole.Festmitarbeiter, ct);

        if (festCount < 1) missing.Add("festmitarbeiter");

        return new ShiftDetailsDto
        {
            Id = einsatz.Id,
            Title = einsatz.Title,
            Description = einsatz.Description,
            StartAtUtc = einsatz.StartAtUtc,
            EndAtUtc = einsatz.EndAtUtc,
            Status = einsatz.Status,
            Location = location,
            Participants = participants,
            Readiness = missing.Count == 0 ? "ready" : "not_ready",
            MissingRequirements = missing
        };
    }

    // ---------- Read-Port (List Query) ----------

    public async Task<IReadOnlyList<ShiftSummaryDto>> GetAllAsync(ShiftStatus? status, Guid? participantId, CancellationToken ct)
    {
        var query = _db.Einsaetze.AsNoTracking();

        if (status.HasValue)
            query = query.Where(e => e.Status == status.Value);

        // Festmitarbeiter/Honorarkraft: only shifts they are assigned to
        if (participantId.HasValue)
            query = query.Where(e => e.Participants.Any(p => p.UserId == participantId.Value));

        var shifts = await query
            .OrderBy(e => e.StartAtUtc)
            .Select(e => new
            {
                e.Id,
                e.Title,
                e.Status,
                e.StartAtUtc,
                e.EndAtUtc,
                e.LocationId
            })
            .ToListAsync(ct);

        if (shifts.Count == 0)
            return [];

        var locationIds = shifts.Select(e => e.LocationId).Distinct().ToList();
        var locations = await _db.Locations
            .AsNoTracking()
            .Where(l => locationIds.Contains(l.Id))
            .Select(l => new ShiftLocationDto { Id = l.Id, Name = l.Name, District = l.District, Address = l.Address })
            .ToDictionaryAsync(l => l.Id, ct);

        var shiftIds = shifts.Select(e => e.Id).ToList();
        var participantCounts = await _db.EinsatzParticipants
            .AsNoTracking()
            .Where(p => shiftIds.Contains(p.ShiftId))
            .GroupBy(p => p.ShiftId)
            .Select(g => new { ShiftId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.ShiftId, g => g.Count, ct);

        return shifts.Select(e => new ShiftSummaryDto(
            e.Id,
            e.Title,
            e.Status,
            e.StartAtUtc,
            e.EndAtUtc,
            locations[e.LocationId],
            participantCounts.GetValueOrDefault(e.Id, 0)
        )).ToList();
    }
}
