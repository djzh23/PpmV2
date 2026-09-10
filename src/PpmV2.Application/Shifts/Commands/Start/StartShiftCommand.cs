using PpmV2.Application.Common.Results;
using PpmV2.Application.Shifts.Interfaces;
using PpmV2.Domain.Shifts;

namespace PpmV2.Application.Shifts.Commands.Start;

public sealed record StartShiftCommand(Guid ShiftId);

/// <summary>
/// Coordinator starts a planned shift: Planned → Active.
/// </summary>
public sealed class StartShiftHandler
{
    private readonly IShiftWorkflowRepository _repo;

    public StartShiftHandler(IShiftWorkflowRepository repo) => _repo = repo;

    public async Task<ServiceResult> Handle(StartShiftCommand cmd, CancellationToken ct)
    {
        var shift = await _repo.GetWithParticipantsAsync(cmd.ShiftId, ct);

        if (shift is null)
            return ServiceResult.Fail("Shift not found.");

        if (shift.Status != ShiftStatus.Planned)
            return ServiceResult.Fail($"Only Planned shifts can be started. Current status: {shift.Status}.");

        shift.Status = ShiftStatus.Active;
        await _repo.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }
}
