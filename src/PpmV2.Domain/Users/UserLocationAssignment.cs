namespace PpmV2.Domain.Users;

/// <summary>
/// Defines which locations a Festmitarbeiter is regularly assigned to (their work profile).
/// Coordinators have access to all locations implicitly and are not listed here.
/// </summary>
public class UserLocationAssignment
{
    public Guid UserId { get; set; }
    public Guid LocationId { get; set; }
    public DateTime AssignedAt { get; set; }
}
