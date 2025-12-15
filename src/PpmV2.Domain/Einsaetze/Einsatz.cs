namespace PpmV2.Domain.Einsaetze;

public class Einsatz
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime? EndAtUtc { get; set; }
    public Guid LocationId { get; set; }
    public EinsatzStatus Status { get; set; }
    public ICollection<EinsatzParticipant> Participants { get; set; } = new List<EinsatzParticipant>();
}
