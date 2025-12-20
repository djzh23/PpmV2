namespace PpmV2.Application.Shifts.Commands.Creation;

public sealed record CreateShiftCommand(
    string Title,
    string? Description,
    DateTime StartAtUtc,
    DateTime? EndAtUtc,
    Guid LocationId,
    IReadOnlyList<CreateShiftParticipantDto> Participants
);
