using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PpmV2.Application.Admin.DTOs;
using PpmV2.Application.Admin.Interfaces;
using PpmV2.Application.Common.Results;
using PpmV2.Domain.Users;
using PpmV2.Infrastructure.Identity;

namespace PpmV2.Infrastructure.Admin.Services;

public class AdminUserService : IAdminUserService
{
    private readonly UserManager<AppUser> _userManager;

    public AdminUserService(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    public Task<List<UserAdminListDto>> GetPendingUsersAsync() => GetUsersByStatusAsync(UserStatus.Pending);

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
}
