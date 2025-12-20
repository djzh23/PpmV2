using PpmV2.Domain.Assignments;
using PpmV2.Domain.Einsaetze;

namespace PpmV2.Application.Einsaetze.Interfaces;

public interface IEinsatzRepository
{
    Task<bool> LocationExistsAsync(Guid locationId, CancellationToken ct);
    Task<int> CountExistingUsersAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct);

    Task AddAsync(
        Einsatz einsatz,
        IReadOnlyCollection<EinsatzParticipant> participants,
        CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
