using PpmV2.Domain.Shifts;

namespace PpmV2.Application.Shifts.DTOs;

//geht an den Client zurück

//ist Output

//ist kontrolliert

//kann zusätzliche Infos bekommen(Name, Rolle-Label, Flags)

//gehört zum Read-Model(Query)

//➡️ Zweck: „Was zeigen wir dem Client?“
public sealed class ShiftParticipantDto
{
    public Guid UserId { get; set; }
    public ShiftRole Role { get; set; }
}
