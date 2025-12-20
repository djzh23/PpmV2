namespace PpmV2.Application.Locations.DTOs;

public sealed record LocationListItemDto(
    Guid Id,
    string Name,
    string District
);
