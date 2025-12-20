using PpmV2.Domain.Shifts;

namespace PpmV2.Application.Einsaetze.Commands.Creation;

public sealed record CreateShiftParticipantDto(Guid UserId, ShiftRole Role);