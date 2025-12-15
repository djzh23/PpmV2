using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PpmV2.Application.Admin.Interfaces;
using PpmV2.Domain.Users;
using PpmV2.Infrastructure.Identity;

namespace PpmV2.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = "AdminOnly")]
public class AdminUsersController : ControllerBase
{

    private readonly IAdminUserService _adminUserService;

    public AdminUsersController(IAdminUserService adminUserService)
    {
        _adminUserService = adminUserService;
    }


    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingUsers()
    {
        var pendingUsers = await _adminUserService.GetPendingUsersAsync();
        return Ok(pendingUsers);
    }

    [HttpGet("approved")]
    public async Task<IActionResult> GetApprovedUsers()
    {
        var approvedUsers = await _adminUserService.GetApprovedUsersAsync();
        return Ok(approvedUsers);
    }

    [HttpGet("rejected")]
    public async Task<IActionResult> GetRejectedUsers()
    {
        var rejectedUsers = await _adminUserService.GetRejectedUsersAsync();
        return Ok(rejectedUsers);
    }

    [HttpPut("approve/{id:guid}")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var result = await _adminUserService.ApproveUserAsync(id);

        if (!result.Success)
        {
            // string.Equals(result.ErrorMessage, "User not found.", StringComparison.OrdinalIgnoreCase)
            if (result.ErrorMessage == "User not found")
                return NotFound(new { message = result.ErrorMessage });
            
            return BadRequest(new { message = result.ErrorMessage });
        }
        return Ok(new { message = "User approved successfully." });
    }

    [HttpPut("reject/{id:guid}")]
    public async Task<IActionResult> Reject(Guid id)
    {
        var result = await _adminUserService.RejectUserAsync(id);

        if(!result.Success)
        {
            if (result.ErrorMessage == "User not found")
                return NotFound(new { message = result.ErrorMessage });
            
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new { message = "User rejected successfully." });
    }
}
