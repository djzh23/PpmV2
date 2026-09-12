using Microsoft.EntityFrameworkCore;
using PpmV2.Application.Common.Results;
using PpmV2.Application.Locations.DTOs;
using PpmV2.Application.Locations.Interfaces;
using PpmV2.Domain.Locations;

namespace PpmV2.Infrastructure.Persistence.Commands;

public sealed class LocationCommandService : ILocationCommandService
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _time;

    public LocationCommandService(AppDbContext db, TimeProvider time)
    {
        _db = db;
        _time = time;
    }

    public async Task<ServiceResult<LocationDetailDto>> CreateAsync(CreateLocationRequest request, CancellationToken ct = default)
    {
        var duplicate = await _db.Locations
            .AsNoTracking()
            .AnyAsync(l => l.Name == request.Name && l.District == request.District, ct);

        if (duplicate)
            return ServiceResult<LocationDetailDto>.Fail(
                $"A location named '{request.Name}' already exists in '{request.District}'.");

        var now = _time.GetUtcNow().UtcDateTime;

        var location = new Location
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            District = request.District,
            Address = request.Address,
            Description = request.Description,
            PhotoUrl = request.PhotoUrl,
            ContactPerson = request.ContactPerson,
            Capacity = request.Capacity,
            Notes = request.Notes,
            IsActive = true,
            CreatedAt = now
        };

        _db.Locations.Add(location);
        await _db.SaveChangesAsync(ct);

        return ServiceResult<LocationDetailDto>.Ok(ToDetailDto(location));
    }

    public async Task<ServiceResult<LocationDetailDto>> UpdateAsync(Guid id, UpdateLocationRequest request, CancellationToken ct = default)
    {
        var location = await _db.Locations.FindAsync([id], ct);

        if (location is null)
            return ServiceResult<LocationDetailDto>.NotFound("Location not found.");

        var duplicate = await _db.Locations
            .AsNoTracking()
            .AnyAsync(l => l.Name == request.Name && l.District == request.District && l.Id != id, ct);

        if (duplicate)
            return ServiceResult<LocationDetailDto>.Fail(
                $"A location named '{request.Name}' already exists in '{request.District}'.");

        location.Name = request.Name;
        location.District = request.District;
        location.Address = request.Address;
        location.Description = request.Description;
        location.PhotoUrl = request.PhotoUrl;
        location.ContactPerson = request.ContactPerson;
        location.Capacity = request.Capacity;
        location.Notes = request.Notes;
        location.IsActive = request.IsActive;
        location.UpdatedAt = _time.GetUtcNow().UtcDateTime;

        await _db.SaveChangesAsync(ct);

        return ServiceResult<LocationDetailDto>.Ok(ToDetailDto(location));
    }

    public async Task<ServiceResult> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var location = await _db.Locations.FindAsync([id], ct);

        if (location is null)
            return ServiceResult.NotFound("Location not found.");

        location.IsActive = false;
        location.UpdatedAt = _time.GetUtcNow().UtcDateTime;

        await _db.SaveChangesAsync(ct);

        return ServiceResult.Ok();
    }

    private static LocationDetailDto ToDetailDto(Location l) => new(
        l.Id,
        l.Name,
        l.District,
        l.Address,
        l.Description,
        l.PhotoUrl,
        l.ContactPerson,
        l.Capacity,
        l.Notes,
        l.IsActive,
        l.CreatedAt,
        l.UpdatedAt
    );
}
