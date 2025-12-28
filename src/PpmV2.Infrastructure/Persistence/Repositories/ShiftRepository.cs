using Microsoft.EntityFrameworkCore;
using PpmV2.Application.Shifts.DTOs;
using PpmV2.Application.Shifts.Interfaces;
using PpmV2.Domain.Shifts;
using PpmV2.Domain.Users;

namespace PpmV2.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core persistence implementation for Shifts(Einsaetze).
/// </summary>
/// <remarks>
/// This class currently implements both:
/// - the write-side repository (IShiftRepository), and
/// - the read-side details query (IShiftDetailsQuery).
///
/// For v1 this keeps the persistence code in one place. If the read model becomes more complex,
/// the query can be extracted into a dedicated query service.
/// 
/// Note: Some members still use legacy naming ("Einsaetze") for compatibility with the existing schema.
/// </remarks>
public sealed class ShiftRepository : IShiftRepository, IShiftDetailsQuery
{
    private readonly AppDbContext _db;

    public ShiftRepository(AppDbContext db) => _db = db;

    // ---------- Write-Port (Create) ----------

    /// <summary>Checks if a referenced Location exists.</summary>
    public Task<bool> LocationExistsAsync(Guid locationId, CancellationToken ct) =>
        _db.Locations.AnyAsync(l => l.Id == locationId, ct);

    /// <summary>
    /// Counts how many of the given user ids exist in the Identity user store.
    /// Used to validate participant inputs before creating the shift.
    /// </summary>
    public Task<int> CountExistingUsersAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct) =>
        _db.Users.CountAsync(u => userIds.Contains(u.Id), ct);

    /// <summary>
    /// Adds a new Shift aggregate and its participants to the unit of work.
    /// Call SaveChangesAsync to persist.
    /// </summary>
    public Task AddAsync(Shift einsatz, IReadOnlyCollection<ShiftParticipant> participants, CancellationToken ct)
    {
        _db.Einsaetze.Add(einsatz);
        _db.EinsatzParticipants.AddRange(participants);
        return Task.CompletedTask;
    }

    /// <summary>Persists all pending changes to the database.</summary>
    public Task SaveChangesAsync(CancellationToken ct) =>
        _db.SaveChangesAsync(ct);

    // ---------- Read-Port (Details Query) ----------

    public async Task<ShiftDetailsDto?> GetByIdAsync(Guid einsatzId, CancellationToken ct)
    {
        // 1) Einsatz + Location (Projection)
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

        // 2) Participants
        var participants = await _db.EinsatzParticipants
            .AsNoTracking()
            .Where(p => p.ShiftId == einsatz.Id)
            .Select(p => new ShiftParticipantDto
            {
                UserId = p.UserId,
                Role = p.Role
            })
            .ToListAsync(ct);

        // 3) Readiness berechnen (Read-Model)
        var missing = new List<string>();

        var leaderCount = participants.Count(p => p.Role == ShiftRole.Leader);
        if (leaderCount != 1) missing.Add("leader");

        var userIds = participants.Select(p => p.UserId).Distinct().ToList();

        // Annahme: AppUser hat Property Role : UserRole
        var festCount = await _db.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .CountAsync(u => u.Role == UserRole.Festmitarbeiter, ct);

        if (festCount < 1) missing.Add("festmitarbeiter");

        var readiness = missing.Count == 0 ? "ready" : "not_ready";

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
            Readiness = readiness,
            MissingRequirements = missing
        };
    }
}
