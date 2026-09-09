using PpmV2.Application.Locations.DTOs;
using PpmV2.Application.Users.DTOs;

namespace PpmV2.Application.Users.Interfaces;

public interface IUserLocationQuery
{
    /// <summary>Returns locations assigned to a specific Festmitarbeiter.</summary>
    Task<IReadOnlyList<LocationListItemDto>> GetAssignedLocationsAsync(Guid userId, CancellationToken ct);

    /// <summary>
    /// Returns staff (Festmitarbeiter) assigned to a location who have no conflicting shift on the given date.
    /// </summary>
    Task<IReadOnlyList<StaffMemberDto>> GetAvailableStaffAsync(Guid locationId, DateOnly date, CancellationToken ct);
}
