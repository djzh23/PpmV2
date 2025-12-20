using PpmV2.Domain.Shifts;

namespace PpmV2.Application.Shifts.Interfaces;

/// <summary>
/// Write-side persistence port for Shift aggregates.
/// </summary>
/// <remarks>
/// The application layer depends on this abstraction.
/// Infrastructure provides the EF Core implementation.
/// 
/// This interface currently focuses on the create flow (Draft creation + participants).
/// Additional write operations (publish/complete/member changes) can be added as the feature evolves.
/// </remarks>
public interface IShiftRepository
{
    /// <summary>Checks whether the referenced Location exists.</summary>
    Task<bool> LocationExistsAsync(Guid locationId, CancellationToken ct);

    /// <summary>
    /// Counts how many of the given user ids exist in the Identity store.
    /// Used for defensive validation before persistence.
    /// </summary>
    Task<int> CountExistingUsersAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct);

    /// <summary>
    /// Adds the shift aggregate and its participants to the current unit of work.
    /// Call SaveChangesAsync to persist.
    /// </summary>
    Task AddAsync(
        Shift einsatz,
        IReadOnlyCollection<ShiftParticipant> participants,
        CancellationToken ct);

    /// <summary>Persists all pending changes of the current unit of work.</summary>
    Task SaveChangesAsync(CancellationToken ct);
}
