using PpmV2.Application.Common.Results;
using PpmV2.Application.Shifts.Interfaces;
using PpmV2.Domain.Shifts;

namespace PpmV2.Application.Shifts.Commands.Complete;

/// <param name="ShiftId">The shift to complete.</param>
/// <param name="RequesterId">The authenticated caller, used to verify they are this shift's Leader.</param>
public sealed record CompleteShiftCommand(Guid ShiftId, Guid RequesterId);

/// <summary>
/// Completes an active shift: Active → Completed. Only the shift's Leader participant may do this.
/// </summary>
public sealed class CompleteShiftHandler
{
    private readonly IShiftWorkflowRepository _repo;

    public CompleteShiftHandler(IShiftWorkflowRepository repo) => _repo = repo;

    public async Task<ServiceResult> Handle(CompleteShiftCommand cmd, CancellationToken ct)
    {
        var shift = await _repo.GetWithParticipantsAsync(cmd.ShiftId, ct);

        if (shift is null)
            return ServiceResult.NotFound("Shift not found.");

        var isLeader = shift.Participants.Any(p => p.UserId == cmd.RequesterId && p.Role == ShiftRole.Leader);
        if (!isLeader)
            return ServiceResult.Forbidden("Only this shift's leader can complete it.");

        if (shift.Status != ShiftStatus.Active)
            return ServiceResult.Fail($"Only Active shifts can be completed. Current status: {shift.Status}.");

        shift.Status = ShiftStatus.Completed;
        await _repo.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }
}
