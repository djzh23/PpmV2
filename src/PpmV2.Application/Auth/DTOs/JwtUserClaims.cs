using PpmV2.Domain.Users;

namespace PpmV2.Application.Auth.DTOs;

public sealed record JwtUserClaims(
    Guid UserId,
    string Email,
    UserRole Role,
    UserStatus Status
);
