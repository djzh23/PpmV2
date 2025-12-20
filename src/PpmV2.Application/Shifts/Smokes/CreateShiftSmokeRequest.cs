
using PpmV2.Domain.Shifts;

namespace PpmV2.Application.Shifts.Smokes;

public sealed class CreateShiftSmokeRequest
{
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public string? Notes { get; set; }

    public DateTime StartAtUtc { get; set; }
    public DateTime? EndAtUtc { get; set; }

    public Guid LocationId { get; set; }

    // Participants werden beim Create direkt gesetzt
    public List<ShiftParticipantSmokeDto> Participants { get; set; } = new();

    // optional: default Draft
    public ShiftStatus? Status { get; set; }
}
