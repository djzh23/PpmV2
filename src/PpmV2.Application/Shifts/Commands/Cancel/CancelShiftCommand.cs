using PpmV2.Application.Common.Results;
using PpmV2.Application.Shifts.Interfaces;
using PpmV2.Domain.Shifts;

namespace PpmV2.Application.Shifts.Commands.Cancel;

/// <param name="ShiftId">The shift to cancel.</param>
/// <param name="RequesterId">
/// Optional: the user requesting the cancellation.
/// When provided and the caller is not a Coordinator/Admin, the handler checks
/// whether the requester is the shift leader and only allows Draft/PendingApproval.
/// When null the caller is assumed to be a privileged user (Coordinator/Admin).
/// </param>
public sealed record CancelShiftCommand(Guid ShiftId, Guid? RequesterId = null);

/// <summary>
/// Cancels a shift. Coordinator/Admin may cancel any non-completed shift.
/// The shift leader may cancel shifts in Draft or PendingApproval.
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

        if (cmd.RequesterId.HasValue)
        {
            // Non-privileged path: verify the requester is the leader
            var isLeader = shift.Participants.Any(p => p.UserId == cmd.RequesterId.Value && p.Role == ShiftRole.Leader);
            if (!isLeader)
                return ServiceResult.Fail("Only the shift leader or a coordinator can cancel this shift.");

            if (shift.Status != ShiftStatus.Draft && shift.Status != ShiftStatus.PendingApproval)
                return ServiceResult.Fail("Leaders may only cancel shifts in Draft or PendingApproval status.");
        }

        shift.Status = ShiftStatus.Cancelled;
        await _repo.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }
}
