using PpmV2.Domain.Einsaetze;

namespace PpmV2.Application.Einsaetze.Smokes;

public sealed class EinsatzDetailsSmokeResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime? EndAtUtc { get; set; }
    public EinsatzStatus Status { get; set; }

    public LocationSmokeDto Location { get; set; } = default!;
    public List<EinsatzParticipantDetailsSmokeDto> Participants { get; set; } = new();

    // NEW:
    public string Readiness { get; set; } = default!;
    public List<string> MissingRequirements { get; set; } = new();
}
