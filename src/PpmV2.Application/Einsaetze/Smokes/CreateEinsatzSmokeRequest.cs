using PpmV2.Domain.Einsaetze;
using System;
using System.Collections.Generic;
using System.Text;

namespace PpmV2.Application.Einsaetze.Smokes;

public sealed class CreateEinsatzSmokeRequest
{
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public string? Notes { get; set; }

    public DateTime StartAtUtc { get; set; }
    public DateTime? EndAtUtc { get; set; }

    public Guid LocationId { get; set; }

    // Participants werden beim Create direkt gesetzt
    public List<EinsatzParticipantSmokeDto> Participants { get; set; } = new();

    // optional: default Draft
    public EinsatzStatus? Status { get; set; }
}
