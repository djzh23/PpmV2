using PpmV2.Application.Common.Exceptions;
using PpmV2.Application.Shifts.Interfaces;
using PpmV2.Domain.Shifts;

namespace PpmV2.Application.Shifts.Commands.Complete;

public sealed record CompleteShiftCommand(Guid ShiftId);

/// <summary>
/// Coordinator completes an active shift: Active → Completed.
/// </summary>
public sealed class CompleteShiftHandler
{
    private readonly IShiftWorkflowRepository _repo;

    public CompleteShiftHandler(IShiftWorkflowRepository repo) => _repo = repo;

    public async Task Handle(CompleteShiftCommand cmd, CancellationToken ct)
    {
        var shift = await _repo.GetWithParticipantsAsync(cmd.ShiftId, ct);

        if (shift is null)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["shiftId"] = ["Shift not found."]
            });

        if (shift.Status != ShiftStatus.Active)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["status"] = [$"Only Active shifts can be completed. Current status: {shift.Status}."]
            });

        shift.Status = ShiftStatus.Completed;
        await _repo.SaveChangesAsync(ct);
    }
}
