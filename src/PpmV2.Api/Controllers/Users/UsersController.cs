using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PpmV2.Application.Shifts.Interfaces;
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
    private readonly IUserLocationQuery _locationQuery;
    private readonly ICurrentUser _currentUser;
    private readonly IStaffQuery _staffQuery;

    public UsersController(
        IUserProfileRepository profileRepository,
        IUserLocationQuery locationQuery,
        ICurrentUser currentUser,
        IStaffQuery staffQuery)
    {
        _profileRepository = profileRepository;
        _locationQuery = locationQuery;
        _currentUser = currentUser;
        _staffQuery = staffQuery;
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

    /// <summary>
    /// Returns the locations a Festmitarbeiter is assigned to (their work profile).
    /// </summary>
    [HttpGet("me/locations")]
    public async Task<IActionResult> GetMyLocations(CancellationToken ct)
    {
        var locations = await _locationQuery.GetAssignedLocationsAsync(_currentUser.UserId, ct);
        return Ok(locations);
    }

    /// <summary>
    /// Returns all approved staff members (Festmitarbeiter and Honorarkraft) with their location assignments.
    /// </summary>
    [HttpGet("staff")]
    [Authorize(Policy = "ShiftManage")]
    public async Task<IActionResult> GetStaff(CancellationToken ct)
    {
        var result = await _staffQuery.GetAllStaffAsync(ct);
        return Ok(result);
    }
}
