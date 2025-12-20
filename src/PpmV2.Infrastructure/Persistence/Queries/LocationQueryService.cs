using Microsoft.EntityFrameworkCore;
using PpmV2.Application.Locations.DTOs;
using PpmV2.Application.Locations.Interfaces;

namespace PpmV2.Infrastructure.Persistence.Queries;

public sealed class LocationQueryService : ILocationQueryService
{
    private readonly AppDbContext _db;

    public LocationQueryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<LocationListItemDto>> GetActiveAsync()
    {
        return await _db.Locations
            .AsNoTracking()
            .Where(l => l.IsActive)
            .OrderBy(l => l.District)
            .ThenBy(l => l.Name)
            .Select(l => new LocationListItemDto(
                l.Id,
                l.Name,
                l.District
            ))
            .ToListAsync();
    }
}