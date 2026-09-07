using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PpmV2.Api.Common;
using PpmV2.Application.Admin.DTOs;
using PpmV2.Application.Admin.Interfaces;
using PpmV2.Application.Common.Errors;
using PpmV2.Application.Common.Results;
using PpmV2.Domain.Users;

namespace PpmV2.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = "AdminOnly")]
/// <summary>
/// Admin-only endpoints to manage user approval and role assignment.
/// </summary>
/// <remarks>
/// This controller is protected by the "AdminOnly" authorization policy.
/// It delegates all business logic to the application service (IAdminUserService).
/// </remarks>
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
            return ApiProblem.From(result.ToAppError(), HttpContext);

        return Ok(new { message = "User approved successfully." });
    }

    [HttpPut("reject/{id:guid}")]
    public async Task<IActionResult> Reject(Guid id)
    {
        var result = await _adminUserService.RejectUserAsync(id);

        if (!result.Success)
            return ApiProblem.From(result.ToAppError(), HttpContext);

        return Ok(new { message = "User rejected successfully." });
    }

    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> SetUserRole(
        Guid id,
        [FromBody] SetUserRoleRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Role))
            return ApiProblem.From(new AppError("VALIDATION_ERROR", "Role is required.", 400), HttpContext);

        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
            return ApiProblem.From(new AppError("VALIDATION_ERROR", $"Invalid role value: '{request.Role}'.", 400), HttpContext);

        var result = await _adminUserService.SetUserRoleAsync(id, role);

        if (!result.Success)
            return ApiProblem.From(result.ToAppError(), HttpContext);

        return Ok(new { message = "User role updated successfully." });
    }
}
