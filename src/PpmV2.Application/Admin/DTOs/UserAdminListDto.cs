using PpmV2.Domain.Users;

namespace PpmV2.Application.Admin.DTOs;

public record UserAdminListDto(
    Guid Id,
    string Email,
    string Firstname,
    string Lastname,
    UserStatus Status,
    UserRole Role
);
