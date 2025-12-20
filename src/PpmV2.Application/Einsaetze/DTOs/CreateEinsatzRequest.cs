namespace PpmV2.Application.Einsaetze.DTOs;

public sealed class CreateEinsatzRequest
{
    public string Title { get; set; } = default!;
    public string? Description { get; set; }

    public DateTime StartAtUtc { get; set; }
    public DateTime? EndAtUtc { get; set; }

    public Guid LocationId { get; set; }

    public List<CreateEinsatzParticipantRequest> Participants { get; set; } = new();
}