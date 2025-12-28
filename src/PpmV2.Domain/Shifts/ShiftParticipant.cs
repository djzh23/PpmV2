namespace PpmV2.Domain.Shifts;

public class ShiftParticipant
{
    public Guid ShiftId { get; set; }
    public Guid UserId { get; set; }
    public ShiftRole Role { get; set; }
    
    // (Composite Key: ShiftId + UserId)

}
