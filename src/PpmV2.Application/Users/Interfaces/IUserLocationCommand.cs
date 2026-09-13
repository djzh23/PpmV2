using PpmV2.Application.Common.Results;

namespace PpmV2.Application.Users.Interfaces;

public interface IUserLocationCommand
{
    Task<ServiceResult> UpdateLocationsAsync(Guid userId, IReadOnlyList<Guid> locationIds, CancellationToken ct = default);
}
