using PpmV2.Domain.Shifts;

namespace PpmV2.Application.Shifts.DTOs;

/// <summary>
/// Read model for displaying shift details (UI/API output).
/// </summary>
/// <remarks>
/// This DTO is optimized for read scenarios and may contain computed fields.
/// In particular, Readiness and MissingRequirements indicate whether business prerequisites
/// for certain actions (e.g. publish) are satisfied.
/// </remarks>
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

    /// <summary>
    /// Computed readiness indicator (e.g. "ready" / "not_ready") used by the UI flow.
    /// </summary>
    public string Readiness { get; set; } = default!;

    /// <summary>
    /// List of missing prerequisites (keys) when the shift is not ready.
    /// Example keys: "leader", "festmitarbeiter".
    /// </summary>
    public List<string> MissingRequirements { get; set; } = new();

}
