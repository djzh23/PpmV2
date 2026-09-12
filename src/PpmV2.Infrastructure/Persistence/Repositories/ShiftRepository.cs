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
        // Query 1: shift + location in one JOIN — avoids a second round-trip just for location data.
        // The Shift entity has no Location navigation property, so we use an explicit JOIN projection.
        // We only select the columns the DTO actually needs, not the full Location entity.
        var header = await (
            from e in _db.Einsaetze.AsNoTracking()
            where e.Id == einsatzId
            join l in _db.Locations.AsNoTracking() on e.LocationId equals l.Id
            select new
            {
                e.Id,
                e.Title,
                e.Description,
                e.StartAtUtc,
                e.EndAtUtc,
                e.Status,
                Location = new ShiftLocationDto
                {
                    Id = l.Id,
                    Name = l.Name,
                    District = l.District,
                    Address = l.Address
                }
            }
        ).FirstOrDefaultAsync(ct);

        if (header is null) return null;

        // Query 2: participants + user display names + system role in one 2-way JOIN.
        // Joining Users here replaces the previous 4th query that counted Festmitarbeiter separately.
        // The system role (UserRole) is only needed in-memory for readiness computation —
        // it is not exposed on ShiftParticipantDto to avoid leaking internal role info to clients.
        var participantRows = await (
            from p in _db.EinsatzParticipants.AsNoTracking()
            where p.ShiftId == einsatzId
            join up in _db.UserProfiles.AsNoTracking() on p.UserId equals up.IdentityUserId
            join u in _db.Users.AsNoTracking() on p.UserId equals u.Id
            select new
            {
                p.UserId,
                p.Role,
                p.ConfirmationStatus,
                up.Firstname,
                up.Lastname,
                SystemRole = u.Role
            }
        ).ToListAsync(ct);

        // Readiness computed in-memory — no additional DB query needed.
        // We have all required data: shift roles from p.Role, system roles from SystemRole.
        var missing = new List<string>();

        if (participantRows.Count(p => p.Role == ShiftRole.Leader) != 1)
            missing.Add("leader");

        if (!participantRows.Any(p => p.SystemRole == UserRole.Festmitarbeiter))
            missing.Add("festmitarbeiter");

        var participants = participantRows
            .Select(p => new ShiftParticipantDto
            {
                UserId = p.UserId,
                Firstname = p.Firstname,
                Lastname = p.Lastname,
                Role = p.Role,
                ConfirmationStatus = p.ConfirmationStatus
            })
            .ToList();

        return new ShiftDetailsDto
        {
            Id = header.Id,
            Title = header.Title,
            Description = header.Description,
            StartAtUtc = header.StartAtUtc,
            EndAtUtc = header.EndAtUtc,
            Status = header.Status,
            Location = header.Location,
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
            locations.GetValueOrDefault(e.LocationId),
            participantCounts.GetValueOrDefault(e.Id, 0)
        )).ToList();
    }
}
