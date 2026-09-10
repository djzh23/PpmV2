using PpmV2.Application.Common.Results;
using PpmV2.Application.Shifts.Interfaces;
using PpmV2.Domain.Shifts;

namespace PpmV2.Application.Shifts.Commands.Propose;

public sealed record ProposeShiftTeamCommand(Guid ShiftId, Guid ProposerId);

/// <summary>
/// Transitions a Draft shift to PendingApproval, setting all non-proposer participants to Invited.
/// Called by a Festmitarbeiter who is already assigned as Leader on the shift.
/// </summary>
public sealed class ProposeShiftTeamHandler
{
    private readonly IShiftWorkflowRepository _repo;

    public ProposeShiftTeamHandler(IShiftWorkflowRepository repo) => _repo = repo;

    public async Task<ServiceResult> Handle(ProposeShiftTeamCommand cmd, CancellationToken ct)
    {
        var shift = await _repo.GetWithParticipantsAsync(cmd.ShiftId, ct);

        if (shift is null)
            return ServiceResult.Fail("Shift not found.");

        if (shift.Status != ShiftStatus.Draft)
            return ServiceResult.Fail($"Only Draft shifts can be proposed. Current status: {shift.Status}.");

        shift.Status = ShiftStatus.PendingApproval;

        foreach (var p in shift.Participants)
        {
            if (p.UserId == cmd.ProposerId)
                continue;

            p.ConfirmationStatus = ParticipantConfirmationStatus.Invited;
            p.RespondedAt = null;
        }

        await _repo.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }
}
