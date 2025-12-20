using PpmV2.Domain.Shifts;

namespace PpmV2.Application.Shifts.Smokes;

public sealed class ShiftParticipantDetailsSmokeDto
{
    public Guid UserId { get; set; }
    public ShiftRole Role { get; set; }
}
