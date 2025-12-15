namespace PpmV2.Domain.Einsaetze;

public class EinsatzParticipant
{
    public Guid EinsatzId { get; set; }
    public Guid UserId { get; set; }
    public EinsatzRole Role { get; set; }
    
    // (Composite Key: EinsatzId + UserId)

}
