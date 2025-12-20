using PpmV2.Domain.Shifts;

namespace PpmV2.Application.Shifts.DTOs;
public class ShiftDetailsDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime? EndAtUtc { get; set; }
    public ShiftStatus Status { get; set; }

    public ShiftLocationDto Location { get; set; } = default!;
    public List<ShiftParticipantDto> Participants { get; set; } = new();

    public string Readiness { get; set; } = default!;
    public List<string> MissingRequirements { get; set; } = new();

}
