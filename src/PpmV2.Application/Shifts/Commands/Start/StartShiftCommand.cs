using PpmV2.Application.Common.Exceptions;
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

    public async Task Handle(StartShiftCommand cmd, CancellationToken ct)
    {
        var shift = await _repo.GetWithParticipantsAsync(cmd.ShiftId, ct);

        if (shift is null)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["shiftId"] = ["Shift not found."]
            });

        if (shift.Status != ShiftStatus.Planned)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["status"] = [$"Only Planned shifts can be started. Current status: {shift.Status}."]
            });

        shift.Status = ShiftStatus.Active;
        await _repo.SaveChangesAsync(ct);
    }
}
