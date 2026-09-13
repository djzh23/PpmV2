namespace PpmV2.Application.Users.DTOs;

public sealed record StaffMemberDto(
    string UserId,
    string Firstname,
    string Lastname,
    string Role,
    IReadOnlyList<StaffLocationDto> Locations
);

public sealed record StaffLocationDto(Guid Id, string Name, string District);
