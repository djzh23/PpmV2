using PpmV2.Domain.Einsaetze;

namespace PpmV2.Application.Einsaetze.Commands.Creation;

public sealed record CreateEinsatzParticipantDto(Guid UserId, EinsatzRole Role);