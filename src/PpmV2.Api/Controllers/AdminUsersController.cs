using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PpmV2.Domain.Users;
using PpmV2.Infrastructure.Identity;

namespace PpmV2.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = "AdminOnly")]
public class AdminUsersController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;

    public AdminUsersController(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingUsers()
    {
        var pending = _userManager.Users
            .Where(u => u.Status == UserStatus.Pending)
            .Select(u => new { u.Id, u.Email })
            .ToList();

        return Ok(pending);
    }

    [HttpPut("approve/{id:guid}")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        user.Status = UserStatus.Approved;
        await _userManager.UpdateAsync(user);

        return Ok(new { message = "Approved" });
    }
}
