using PpmV2.Application.Common.Results;
using PpmV2.Application.Shifts.Interfaces;
using PpmV2.Domain.Shifts;

namespace PpmV2.Application.Shifts.Commands.Start;

/// <param name="ShiftId">The shift to start.</param>
/// <param name="RequesterId">The authenticated caller, used to verify they are this shift's Leader.</param>
public sealed record StartShiftCommand(Guid ShiftId, Guid RequesterId);

/// <summary>
/// Starts a planned shift: Planned → Active. Only the shift's Leader participant may do this.
/// </summary>
public sealed class StartShiftHandler
{
    private readonly IShiftWorkflowRepository _repo;

    public StartShiftHandler(IShiftWorkflowRepository repo) => _repo = repo;

    public async Task<ServiceResult> Handle(StartShiftCommand cmd, CancellationToken ct)
    {
        var shift = await _repo.GetWithParticipantsAsync(cmd.ShiftId, ct);

        if (shift is null)
            return ServiceResult.NotFound("Shift not found.");

        var isLeader = shift.Participants.Any(p => p.UserId == cmd.RequesterId && p.Role == ShiftRole.Leader);
        if (!isLeader)
            return ServiceResult.Forbidden("Only this shift's leader can start it.");

        if (shift.Status != ShiftStatus.Planned)
            return ServiceResult.Fail($"Only Planned shifts can be started. Current status: {shift.Status}.");

        shift.Status = ShiftStatus.Active;
        await _repo.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }
}
