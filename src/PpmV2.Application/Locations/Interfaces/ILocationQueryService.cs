using PpmV2.Application.Locations.DTOs;

namespace PpmV2.Application.Locations.Interfaces;

public interface ILocationQueryService
{
    Task<IReadOnlyList<LocationListItemDto>> GetActiveAsync(CancellationToken ct = default);
    Task<LocationDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
