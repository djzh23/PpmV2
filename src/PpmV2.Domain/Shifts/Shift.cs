namespace PpmV2.Domain.Shifts;

public class Shift
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime? EndAtUtc { get; set; }
    public Guid LocationId { get; set; }
    public ShiftStatus Status { get; set; }
    public ICollection<ShiftParticipant> Participants { get; set; } = new List<ShiftParticipant>();
}
