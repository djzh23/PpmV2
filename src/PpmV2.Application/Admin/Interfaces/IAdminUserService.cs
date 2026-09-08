using PpmV2.Application.Admin.DTOs;
using PpmV2.Application.Common.Results;
using PpmV2.Domain.Users;

namespace PpmV2.Application.Admin.Interfaces;

public interface IAdminUserService
{
    Task<List<UserAdminListDto>> GetPendingUsersAsync(CancellationToken ct = default);
    Task<List<UserAdminListDto>> GetApprovedUsersAsync(CancellationToken ct = default);
    Task<List<UserAdminListDto>> GetRejectedUsersAsync(CancellationToken ct = default);
    Task<ServiceResult> ApproveUserAsync(Guid userId, CancellationToken ct = default);
    Task<ServiceResult> RejectUserAsync(Guid userId, CancellationToken ct = default);
    Task<ServiceResult> SetUserRoleAsync(Guid userId, UserRole role, CancellationToken ct = default);
}
