using PpmV2.Api.Common;

namespace PpmV2.Tests.Api.Common;

public class JwtKeyGuardTests
{
    [Fact]
    public void Validate_ThrowsInvalidOperationException_WhenKeyIsMissing()
    {
        Assert.Throws<InvalidOperationException>(() => JwtKeyGuard.Validate(null, isProduction: false));
    }

    [Fact]
    public void Validate_ThrowsInvalidOperationException_WhenKeyIsTooShort()
    {
        Assert.Throws<InvalidOperationException>(() => JwtKeyGuard.Validate("TooShortKey", isProduction: false));
    }

    [Fact]
    public void Validate_ThrowsInvalidOperationException_WhenDevKeyIsUsedInProduction()
    {
        Assert.Throws<InvalidOperationException>(() => JwtKeyGuard.Validate(JwtKeyGuard.KnownDevKey, isProduction: true));
    }

    [Fact]
    public void Validate_DoesNotThrow_WhenDevKeyIsUsedInDevelopment()
    {
        var exception = Record.Exception(() => JwtKeyGuard.Validate(JwtKeyGuard.KnownDevKey, isProduction: false));

        Assert.Null(exception);
    }
}
