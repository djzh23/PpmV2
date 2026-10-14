namespace PpmV2.Api.Common;

/// <summary>
/// Validates the JWT signing key at startup so a missing, weak, or accidentally
/// deployed development key fails fast with a clear error instead of issuing
/// insecure tokens silently.
/// </summary>
public static class JwtKeyGuard
{
    public const string KnownDevKey = "PpmV2_Dev_SecretKey_ThisMustBeAtLeast32Chars!";
    private const int MinimumKeyLength = 32;

    public static void Validate(string? key, bool isProduction)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("JWT Key is missing (Jwt:Key).");

        if (key.Length < MinimumKeyLength)
            throw new InvalidOperationException($"JWT Key must be at least {MinimumKeyLength} characters.");

        if (isProduction && key == KnownDevKey)
            throw new InvalidOperationException("JWT Key must not be the known development key in Production.");
    }
}
