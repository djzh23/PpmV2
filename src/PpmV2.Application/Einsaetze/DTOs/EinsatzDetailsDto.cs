using PpmV2.Domain.Einsaetze;

namespace PpmV2.Application.Einsaetze.DTOs;
public class EinsatzDetailsDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime? EndAtUtc { get; set; }
    public EinsatzStatus Status { get; set; }

    public EinsatzLocationDto Location { get; set; } = default!;
    public List<EinsatzParticipantDto> Participants { get; set; } = new();

    public string Readiness { get; set; } = default!;
    public List<string> MissingRequirements { get; set; } = new();

}
