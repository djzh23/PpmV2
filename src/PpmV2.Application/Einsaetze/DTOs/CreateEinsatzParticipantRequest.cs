using PpmV2.Domain.Einsaetze;
using System.Runtime.ConstrainedExecution;

namespace PpmV2.Application.Einsaetze.DTOs;

//kommt vom Client

//ist Input

//ist nicht vertrauenswürdig

//kann sich ändern (Validierungen, neue Felder, UI-Anforderungen)

//gehört zum API-Use-Case „Create Einsatz“

//➡️ Zweck: „Was schickt der Client?“
public sealed class CreateEinsatzParticipantRequest
{
    public Guid UserId { get; set; }
    public EinsatzRole Role { get; set; }
}

