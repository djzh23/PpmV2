using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using PpmV2.Application.Shifts.Interfaces;

namespace PpmV2.Api.Common;

/// <summary>
/// Reads the current user's identity from the active HTTP request's JWT claims.
/// Registered as Scoped — one instance per request.
/// </summary>
public sealed class ClaimsCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public ClaimsCurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    // Lazy property: resolved on first access, never in the constructor.
    // Reason: ASP.NET Core constructs the controller (and resolves DI) before [Authorize]
    // filters run. Throwing in the constructor would produce 500 instead of 401 for
    // unauthenticated requests. [Authorize] ensures UserId is only read on authenticated calls.
    public Guid UserId
    {
        get
        {
            var user = _accessor.HttpContext?.User;
            var sub = user?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                   ?? user?.FindFirstValue(ClaimTypes.NameIdentifier);
            return sub is not null ? Guid.Parse(sub) : Guid.Empty;
        }
    }

    public bool IsInRole(string role) =>
        _accessor.HttpContext?.User.IsInRole(role) ?? false;
}
