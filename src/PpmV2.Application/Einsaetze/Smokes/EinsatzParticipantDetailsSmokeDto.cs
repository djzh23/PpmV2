using PpmV2.Domain.Einsaetze;

namespace PpmV2.Application.Einsaetze.Smokes;

public sealed class EinsatzParticipantDetailsSmokeDto
{
    public Guid UserId { get; set; }
    public EinsatzRole Role { get; set; }
}
