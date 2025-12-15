using PpmV2.Application.Admin.DTOs;
using PpmV2.Application.Common.Results;
using PpmV2.Domain.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace PpmV2.Application.Admin.Interfaces;

public interface IAdminUserService
{
    Task<List<UserAdminListDto>> GetPendingUsersAsync();
    Task<List<UserAdminListDto>> GetApprovedUsersAsync();
    Task<List<UserAdminListDto>> GetRejectedUsersAsync();
    Task<ServiceResult> ApproveUserAsync(Guid userId);
    Task<ServiceResult> RejectUserAsync(Guid userId);

    Task<ServiceResult> SetUserRoleAsync(Guid userId, UserRole role);

}
