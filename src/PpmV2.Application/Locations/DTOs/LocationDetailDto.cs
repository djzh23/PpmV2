namespace PpmV2.Application.Locations.DTOs;

public sealed record LocationDetailDto(
    Guid Id,
    string Name,
    string District,
    string? Address,
    string? Description,
    string? PhotoUrl,
    string? ContactPerson,
    int? Capacity,
    string? Notes,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
