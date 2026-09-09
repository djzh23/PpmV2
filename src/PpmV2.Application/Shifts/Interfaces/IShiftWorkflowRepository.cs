using PpmV2.Domain.Shifts;

namespace PpmV2.Application.Shifts.Interfaces;

/// <summary>
/// Write-side repository for shift workflow state transitions.
/// </summary>
public interface IShiftWorkflowRepository
{
    Task<Shift?> GetWithParticipantsAsync(Guid shiftId, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
