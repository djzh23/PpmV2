namespace PpmV2.Application.Einsaetze.Interfaces;

public interface ICurrentUser
{
    Guid UserId { get; }
    bool IsInRole(string role); // optional
}
