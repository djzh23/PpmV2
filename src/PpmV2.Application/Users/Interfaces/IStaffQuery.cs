using PpmV2.Application.Users.DTOs;

namespace PpmV2.Application.Users.Interfaces;

public interface IStaffQuery
{
    Task<IReadOnlyList<StaffMemberDto>> GetAllStaffAsync(CancellationToken ct = default);
}
