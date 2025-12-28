using PpmV2.Application.Auth.DTOs;

namespace PpmV2.Application.Admin.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(JwtUserClaims claims);
}
