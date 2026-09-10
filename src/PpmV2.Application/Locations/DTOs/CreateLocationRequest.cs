namespace PpmV2.Application.Locations.DTOs;

public sealed record CreateLocationRequest(
    string Name,
    string District,
    string? Address,
    string? Description,
    string? PhotoUrl,
    string? ContactPerson,
    int? Capacity,
    string? Notes
);
