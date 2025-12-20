using PpmV2.Domain.Einsaetze;

namespace PpmV2.Application.Einsaetze.DTOs;

//geht an den Client zurück

//ist Output

//ist kontrolliert

//kann zusätzliche Infos bekommen(Name, Rolle-Label, Flags)

//gehört zum Read-Model(Query)

//➡️ Zweck: „Was zeigen wir dem Client?“
public sealed class EinsatzParticipantDto
{
    public Guid UserId { get; set; }
    public EinsatzRole Role { get; set; }
}
