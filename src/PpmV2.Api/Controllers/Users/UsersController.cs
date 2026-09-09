using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PpmV2.Application.Users.DTOs;
using PpmV2.Application.Users.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PpmV2.Api.Controllers.Users;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserProfileRepository _profileRepository;

    public UsersController(IUserProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    /// <summary>
    /// Returns the profile of the currently authenticated user.
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<MyProfileDto>> GetMe(CancellationToken ct)
    {
        var userIdValue = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                       ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdValue, out var identityUserId))
            return Unauthorized();

        var profile = await _profileRepository.GetByIdentityUserIdAsync(identityUserId, ct);
        if (profile is null)
            return NotFound();

        var role   = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        var status = User.FindFirst("status")?.Value ?? string.Empty;

        return Ok(new MyProfileDto(
            profile.Id,
            profile.Firstname,
            profile.Lastname,
            profile.Email,
            role,
            status
        ));
    }
}
