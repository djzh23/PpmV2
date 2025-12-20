using Microsoft.EntityFrameworkCore;
using PpmV2.Application.Locations.DTOs;
using PpmV2.Application.Locations.Interfaces;

namespace PpmV2.Infrastructure.Persistence.Queries;

/// <summary>
/// Read-only query service for Locations.
/// </summary>
/// <remarks>
/// This service represents the CQRS read side for Location data.
/// It is EF Core dependent and therefore located in the Infrastructure/Persistence layer.
///
/// The service returns lightweight DTOs optimized for list and selection scenarios
/// (e.g. dropdowns, filters), not full domain entities.
/// </remarks>
public sealed class LocationQueryService : ILocationQueryService
{
    private readonly AppDbContext _db;

    public LocationQueryService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns all active locations ordered by district and name.
    /// </summary>
    public async Task<IReadOnlyList<LocationListItemDto>> GetActiveAsync()
    {
        return await _db.Locations
            .AsNoTracking() // Read-only query: no change tracking required.
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