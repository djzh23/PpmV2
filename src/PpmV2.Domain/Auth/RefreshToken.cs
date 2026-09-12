namespace PpmV2.Domain.Auth;

/// <summary>
/// Represents a single-use refresh token tied to a user session.
/// </summary>
/// <remarks>
/// Tokens are stored as plain random strings for portfolio simplicity.
/// In production, store a SHA-256 hash and compare against the hash to limit
/// exposure if the refresh_tokens table is ever compromised.
/// </remarks>
public sealed class RefreshToken
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid UserId { get; init; }

    /// <summary>Cryptographically random token value (base64url, 32 bytes).</summary>
    public string Token { get; init; } = string.Empty;

    public DateTime ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }

    /// <summary>Set when the token is consumed (rotated) or explicitly revoked on logout.</summary>
    public DateTime? RevokedAt { get; set; }

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive(DateTime utcNow) => !IsRevoked && !IsExpired(utcNow);
}
