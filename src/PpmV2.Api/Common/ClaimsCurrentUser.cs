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
    public Guid UserId { get; }

    private readonly ClaimsPrincipal _principal;

    public ClaimsCurrentUser(IHttpContextAccessor accessor)
    {
        var user = accessor.HttpContext?.User
            ?? throw new InvalidOperationException("No active HTTP context.");

        var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? throw new InvalidOperationException("UserId claim not found.");

        UserId = Guid.Parse(sub);
        _principal = user;
    }

    public bool IsInRole(string role) => _principal.IsInRole(role);
}
