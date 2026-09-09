namespace PpmV2.Application.Users.DTOs;

public record MyProfileDto(
    Guid Id,
    string Firstname,
    string Lastname,
    string Email,
    string Role,
    string Status
);
