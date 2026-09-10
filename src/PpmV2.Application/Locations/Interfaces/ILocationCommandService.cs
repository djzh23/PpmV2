using PpmV2.Application.Common.Results;
using PpmV2.Application.Locations.DTOs;

namespace PpmV2.Application.Locations.Interfaces;

public interface ILocationCommandService
{
    Task<ServiceResult<LocationDetailDto>> CreateAsync(CreateLocationRequest request, CancellationToken ct = default);
    Task<ServiceResult<LocationDetailDto>> UpdateAsync(Guid id, UpdateLocationRequest request, CancellationToken ct = default);
    Task<ServiceResult> DeactivateAsync(Guid id, CancellationToken ct = default);
}
