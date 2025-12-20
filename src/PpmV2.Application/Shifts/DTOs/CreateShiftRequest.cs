namespace PpmV2.Application.Shifts.DTOs;

/// <summary>
/// Client request model for creating a Shift.
/// </summary>
/// <remarks>
/// This is an API-facing input DTO (untrusted input).
/// It should be mapped to CreateShiftCommand after validation/normalization.
/// </remarks>
public sealed class CreateShiftRequest
{
    public string Title { get; set; } = default!;
    public string? Description { get; set; }

    public DateTime StartAtUtc { get; set; }
    public DateTime? EndAtUtc { get; set; }

    public Guid LocationId { get; set; }

    public List<CreateShiftParticipantRequest> Participants { get; set; } = new();
}