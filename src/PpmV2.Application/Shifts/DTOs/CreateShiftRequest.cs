namespace PpmV2.Application.Shifts.DTOs;

public sealed class CreateShiftRequest
{
    public string Title { get; set; } = default!;
    public string? Description { get; set; }

    public DateTime StartAtUtc { get; set; }
    public DateTime? EndAtUtc { get; set; }

    public Guid LocationId { get; set; }

    public List<CreateShiftParticipantRequest> Participants { get; set; } = new();
}