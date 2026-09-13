using PpmV2.Application.Common.Results;
using PpmV2.Application.Shifts.Interfaces;
using PpmV2.Domain.Shifts;

namespace PpmV2.Application.Shifts.Commands.AddParticipant;

public sealed record AddParticipantCommand(Guid ShiftId, Guid RequesterId, Guid UserId, ShiftRole Role);

public sealed class AddParticipantHandler
{
    private readonly IShiftWorkflowRepository _repo;

    public AddParticipantHandler(IShiftWorkflowRepository repo) => _repo = repo;

    public async Task<ServiceResult> Handle(AddParticipantCommand cmd, CancellationToken ct)
    {
        var shift = await _repo.GetWithParticipantsAsync(cmd.ShiftId, ct);

        if (shift is null)
            return ServiceResult.NotFound("Shift not found.");

        if (shift.Status != ShiftStatus.Draft && shift.Status != ShiftStatus.PendingApproval)
            return ServiceResult.Fail("Participants can only be added to shifts in Draft or PendingApproval status.");

        var isLeader = shift.Participants.Any(p => p.UserId == cmd.RequesterId && p.Role == ShiftRole.Leader);
        if (!isLeader)
            return ServiceResult.Fail("Only the shift leader can add participants.");

        var alreadyParticipant = shift.Participants.Any(p => p.UserId == cmd.UserId);
        if (alreadyParticipant)
            return ServiceResult.Fail("User is already a participant of this shift.");

        shift.Participants.Add(new ShiftParticipant
        {
            ShiftId = cmd.ShiftId,
            UserId = cmd.UserId,
            Role = cmd.Role,
            ConfirmationStatus = ParticipantConfirmationStatus.Invited,
        });

        await _repo.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }
}
