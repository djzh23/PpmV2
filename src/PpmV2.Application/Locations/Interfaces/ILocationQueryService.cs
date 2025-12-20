using PpmV2.Application.Locations.DTOs;

namespace PpmV2.Application.Locations.Interfaces;

public interface ILocationQueryService
{
    Task<IReadOnlyList<LocationListItemDto>> GetActiveAsync();


}
