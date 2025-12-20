using PpmV2.Domain.Einsaetze;

namespace PpmV2.Application.Einsaetze.Smokes;

public sealed class EinsatzParticipantSmokeDto
{
    public Guid UserId { get; set; }
    public EinsatzRole Role { get; set; }
}