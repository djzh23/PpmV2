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

    public async Task<List<PendingUserDto>> GetPendingUsersAsync() {

        var pendingUsers = await _userManager.Users
                                .Include(u => u.Profile)
                                .Where(u => u.Status == UserStatus.Pending)
                                .ToListAsync();
        
        return pendingUsers.Select(user => new PendingUserDto(
            Id: user.Id,
            Email: user.Email ?? string.Empty,
            Firstname: user.Profile?.Firstname ?? string.Empty, 
            Lastname: user.Profile?.Lastname ?? string.Empty,  
            Status: user.Status
            )).ToList();
    
    }
    
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
}
