using PpmV2.Domain.Shifts;

namespace PpmV2.Application.Shifts.Smokes;

public sealed class ShiftDetailsSmokeResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime? EndAtUtc { get; set; }
    public ShiftStatus Status { get; set; }

    public LocationSmokeDto Location { get; set; } = default!;
    public List<ShiftParticipantDetailsSmokeDto> Participants { get; set; } = new();

    // NEW:
    public string Readiness { get; set; } = default!;
    public List<string> MissingRequirements { get; set; } = new();
}
