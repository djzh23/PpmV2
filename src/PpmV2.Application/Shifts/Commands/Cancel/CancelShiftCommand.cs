using PpmV2.Application.Common.Results;
using PpmV2.Application.Shifts.Interfaces;
using PpmV2.Domain.Shifts;

namespace PpmV2.Application.Shifts.Commands.Cancel;

public sealed record CancelShiftCommand(Guid ShiftId);

/// <summary>
/// Coordinator cancels a shift (any status except Completed).
/// </summary>
public sealed class CancelShiftHandler
{
    private readonly IShiftWorkflowRepository _repo;

    public CancelShiftHandler(IShiftWorkflowRepository repo) => _repo = repo;

    public async Task<ServiceResult> Handle(CancelShiftCommand cmd, CancellationToken ct)
    {
        var shift = await _repo.GetWithParticipantsAsync(cmd.ShiftId, ct);

        if (shift is null)
            return ServiceResult.NotFound("Shift not found.");

        if (shift.Status == ShiftStatus.Completed)
            return ServiceResult.Fail("Completed shifts cannot be cancelled.");

        shift.Status = ShiftStatus.Cancelled;
        await _repo.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }
}
