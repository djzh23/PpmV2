using PpmV2.Domain.Shifts;

namespace PpmV2.Application.Shifts.Interfaces;

public interface IShiftRepository
{
    Task<bool> LocationExistsAsync(Guid locationId, CancellationToken ct);
    Task<int> CountExistingUsersAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct);

    Task AddAsync(
        Shift einsatz,
        IReadOnlyCollection<ShiftParticipant> participants,
        CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
