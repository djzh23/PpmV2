using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PpmV2.Application.Admin.Interfaces;
using PpmV2.Application.Auth.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PpmV2.Infrastructure.Auth;

/// <summary>
/// Generates JSON Web Tokens (JWT) for authenticated users.
/// </summary>
/// <remarks>
/// This service is part of the Infrastructure layer and is responsible
/// solely for token creation, not for authentication or authorization logic.
///
/// The token contains domain-relevant claims (user id, role, status)
/// that are later used by the API for authorization decisions.
/// </remarks>
public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// Creates a signed JWT token based on the provided user claims.
    /// </summary>
    /// <remarks>
    /// Security notes:
    /// - Token settings (issuer, audience, key) are read from configuration.
    /// - A symmetric signing key (HMAC SHA-256) is used.
    /// - Token lifetime is intentionally limited.
    /// </remarks>
    public string GenerateToken(JwtUserClaims claims)
    {
        var jwtSection = _config.GetSection("Jwt");
        var issuer = jwtSection["Issuer"]!;
        var audience = jwtSection["Audience"]!;
        var key = jwtSection["Key"]!;

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        // Claims embedded into the token.
        // These claims are later evaluated by authorization policies and guards.
        var tokenClaims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, claims.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, claims.Email),
            new(ClaimTypes.Role, claims.Role.ToString()),
            new("status", claims.Status.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: tokenClaims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}