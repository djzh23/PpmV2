using PpmV2.Application.Common.Exceptions;
using PpmV2.Application.Shifts.Interfaces;
using PpmV2.Domain.Shifts;

namespace PpmV2.Application.Shifts.Commands.Approve;

public sealed record ApproveShiftCommand(Guid ShiftId);

/// <summary>
/// Coordinator approves a shift: Draft or PendingApproval → Planned.
/// </summary>
public sealed class ApproveShiftHandler
{
    private readonly IShiftWorkflowRepository _repo;

    public ApproveShiftHandler(IShiftWorkflowRepository repo) => _repo = repo;

    public async Task Handle(ApproveShiftCommand cmd, CancellationToken ct)
    {
        var shift = await _repo.GetWithParticipantsAsync(cmd.ShiftId, ct);

        if (shift is null)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["shiftId"] = ["Shift not found."]
            });

        if (shift.Status is not (ShiftStatus.Draft or ShiftStatus.PendingApproval))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["status"] = [$"Only Draft or PendingApproval shifts can be approved. Current status: {shift.Status}."]
            });

        shift.Status = ShiftStatus.Planned;

        // Confirm all still-invited participants when Coordinator approves directly.
        foreach (var p in shift.Participants.Where(p => p.ConfirmationStatus == ParticipantConfirmationStatus.Invited))
        {
            p.ConfirmationStatus = ParticipantConfirmationStatus.Accepted;
            p.RespondedAt = null;
        }

        await _repo.SaveChangesAsync(ct);
    }
}
