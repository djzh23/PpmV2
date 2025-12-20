namespace PpmV2.Application.Shifts.Interfaces;

public interface ICurrentUser
{
    Guid UserId { get; }
    bool IsInRole(string role); // optional
}
