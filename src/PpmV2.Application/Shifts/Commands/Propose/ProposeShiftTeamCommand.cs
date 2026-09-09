using PpmV2.Application.Common.Exceptions;
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

    public async Task Handle(ProposeShiftTeamCommand cmd, CancellationToken ct)
    {
        var shift = await _repo.GetWithParticipantsAsync(cmd.ShiftId, ct);

        if (shift is null)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["shiftId"] = ["Shift not found."]
            });

        if (shift.Status != ShiftStatus.Draft)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["status"] = [$"Only Draft shifts can be proposed. Current status: {shift.Status}."]
            });

        shift.Status = ShiftStatus.PendingApproval;

        foreach (var p in shift.Participants)
        {
            if (p.UserId == cmd.ProposerId)
                continue;

            p.ConfirmationStatus = ParticipantConfirmationStatus.Invited;
            p.RespondedAt = null;
        }

        await _repo.SaveChangesAsync(ct);
    }
}
