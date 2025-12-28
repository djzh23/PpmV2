using PpmV2.Domain.Shifts;

namespace PpmV2.Application.Shifts.DTOs;

//kommt vom Client

//ist Input

//ist nicht vertrauenswürdig

//kann sich ändern (Validierungen, neue Felder, UI-Anforderungen)

//gehört zum API-Use-Case „Create Einsatz“

//➡️ Zweck: „Was schickt der Client?“
public sealed class CreateShiftParticipantRequest
{
    public Guid UserId { get; set; }
    public ShiftRole Role { get; set; }
}

