using PpmV2.Domain.Shifts;

namespace PpmV2.Application.Shifts.Commands.Creation;

/// <summary>
/// Internal participant DTO used by the CreateShift command.
/// </summary>
/// <remarks>
/// This type represents validated/normalized data used inside the application use case.
/// It is distinct from API request models to keep external contracts decoupled from internal processing.
public sealed record CreateShiftParticipantDto(Guid UserId, ShiftRole Role);