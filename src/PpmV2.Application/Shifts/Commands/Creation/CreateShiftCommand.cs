namespace PpmV2.Application.Shifts.Commands.Creation;

/// <summary>
/// Command representing the "Create Shift" use case.
/// </summary>
/// <remarks>
/// This command is the application-layer representation of the create intent.
/// API request DTOs are mapped to this command before execution.
/// </remarks>
public sealed record CreateShiftCommand(
    string Title,
    string? Description,
    DateTime StartAtUtc,
    DateTime? EndAtUtc,
    Guid LocationId,
    IReadOnlyList<CreateShiftParticipantDto> Participants
);
