namespace PpmV2.Application.Shifts.Interfaces;

/// <summary>
/// Abstraction for accessing information about the currently authenticated user.
/// </summary>
/// <remarks>
/// The application layer depends on this interface to avoid direct coupling to ASP.NET Core.
/// The Infrastructure/API layer provides the concrete implementation based on HttpContext claims.
/// </remarks>
public interface ICurrentUser
{
    Guid UserId { get; }

    // Returns whether the current user is in the given role.
    bool IsInRole(string role);
}
