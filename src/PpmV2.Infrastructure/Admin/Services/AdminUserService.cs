using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PpmV2.Application.Admin.DTOs;
using PpmV2.Application.Admin.Interfaces;
using PpmV2.Application.Common.Results;
using PpmV2.Domain.Users;
using PpmV2.Infrastructure.Identity;

namespace PpmV2.Infrastructure.Admin.Services;

/// <summary>
/// Admin-only user management service.
/// </summary>
/// <remarks>
/// This service is part of the Infrastructure layer and builds on ASP.NET Core Identity.
/// It encapsulates administrative operations such as:
/// - listing users by approval status
/// - approving/rejecting users
/// - assigning roles under strict rules
///
/// Failures are returned using ServiceResult to represent expected business outcomes
/// without throwing exceptions.
/// </remarks>
public class AdminUserService : IAdminUserService
{
    private readonly UserManager<AppUser> _userManager;

    public AdminUserService(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    /// <summary>Returns users whose accounts are pending approval.</summary>
    public Task<List<UserAdminListDto>> GetPendingUsersAsync() => GetUsersByStatusAsync(UserStatus.Pending);

    /// <summary>
    /// Approves a user account (Pending -> Approved).
    /// </summary>
    /// <remarks>
    /// If the user is already approved, the operation is idempotent and returns success.
    /// </remarks>
    public async Task<ServiceResult> ApproveUserAsync(Guid userId) {
        
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if(user == null)
        {
            return ServiceResult.Fail("User not found.");
        }

        if(user.Status == UserStatus.Approved)
        {
            return ServiceResult.Ok();
        }
        
        user.Status = UserStatus.Approved;
        
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return ServiceResult.Fail($"Failed to approve user: {errors}");
        }

        return ServiceResult.Ok();
    }

    /// <summary>
    /// Rejects a user account (Pending -> Rejected).
    /// </summary>
    /// <remarks>
    /// If the user is already rejected, the operation is idempotent and returns success.
    /// </remarks>
    public async Task<ServiceResult> RejectUserAsync(Guid userId) { 
    
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if(user == null)
        {
            return ServiceResult.Fail("User not found.");
        }
        if(user.Status == UserStatus.Rejected)
        {
            return ServiceResult.Ok();
        }
        user.Status = UserStatus.Rejected;
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return ServiceResult.Fail($"Failed to reject user: {errors}");
        }

        return ServiceResult.Ok();
    }

    public Task<List<UserAdminListDto>> GetApprovedUsersAsync() => GetUsersByStatusAsync(UserStatus.Approved);

    public Task<List<UserAdminListDto>> GetRejectedUsersAsync() => GetUsersByStatusAsync(UserStatus.Rejected);

    private async Task<List<UserAdminListDto>> GetUsersByStatusAsync(UserStatus status)
    {
        return await _userManager.Users
            .Include(u => u.Profile)
            .Where(u => u.Status == status)
            .Select(u => new UserAdminListDto(
                u.Id,
                u.Email ?? string.Empty,
                u.Profile != null ? u.Profile.Firstname : string.Empty,
                u.Profile != null ? u.Profile.Lastname : string.Empty,
                u.Status,
                u.Role
            ))
            .ToListAsync();
    }

    /// <summary>
    /// Assigns a role to an approved user under strict constraints.
    /// </summary>
    /// <remarks>
    /// Rules:
    /// - role changes are allowed only for approved users
    /// - Admin role cannot be assigned via this API endpoint
    /// - Unknown/invalid roles are rejected
    /// </remarks>
    public async Task<ServiceResult> SetUserRoleAsync(Guid userId, UserRole role)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return ServiceResult.Fail("User not found");

        if (user.Status != UserStatus.Approved)
            return ServiceResult.Fail("Role can only be changed for approved users");

        if (role == UserRole.Admin)
            return ServiceResult.Fail("Admin role cannot be assigned via API");

        if (!Enum.IsDefined(typeof(UserRole), role) || role == UserRole.Unknown)
            return ServiceResult.Fail("Invalid role");

        user.Role = role;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return ServiceResult.Fail(errors);
        }

        return ServiceResult.Ok();
    }

}
