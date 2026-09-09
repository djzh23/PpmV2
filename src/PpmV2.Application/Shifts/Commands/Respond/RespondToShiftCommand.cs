using PpmV2.Application.Common.Exceptions;
using PpmV2.Application.Shifts.Interfaces;
using PpmV2.Domain.Shifts;

namespace PpmV2.Application.Shifts.Commands.Respond;

public sealed record RespondToShiftCommand(Guid ShiftId, Guid UserId, ParticipantConfirmationStatus Response, DateTime RespondedAt);

/// <summary>
/// A participant accepts or declines an invitation to a shift.
/// Only valid when the shift is in PendingApproval and the participant is Invited.
/// </summary>
public sealed class RespondToShiftHandler
{
    private readonly IShiftWorkflowRepository _repo;

    public RespondToShiftHandler(IShiftWorkflowRepository repo) => _repo = repo;

    public async Task Handle(RespondToShiftCommand cmd, CancellationToken ct)
    {
        if (cmd.Response == ParticipantConfirmationStatus.Invited)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["response"] = ["Response must be Accepted or Declined."]
            });

        var shift = await _repo.GetWithParticipantsAsync(cmd.ShiftId, ct);

        if (shift is null)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["shiftId"] = ["Shift not found."]
            });

        if (shift.Status != ShiftStatus.PendingApproval)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["status"] = ["Can only respond to shifts in PendingApproval status."]
            });

        var participant = shift.Participants.FirstOrDefault(p => p.UserId == cmd.UserId);
        if (participant is null)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["userId"] = ["You are not a participant of this shift."]
            });

        if (participant.ConfirmationStatus != ParticipantConfirmationStatus.Invited)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["confirmationStatus"] = ["You have already responded to this shift."]
            });

        participant.ConfirmationStatus = cmd.Response;
        participant.RespondedAt = cmd.RespondedAt;

        await _repo.SaveChangesAsync(ct);
    }
}
