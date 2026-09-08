using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PpmV2.Application.Auth.DTOs;
using PpmV2.Domain.Users;
using PpmV2.Infrastructure.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PpmV2.Tests.Auth;

public class JwtTokenServiceTests
{
    private const string TestKey = "test-secret-key-that-is-at-least-32-characters-long";
    private const string TestIssuer = "test-issuer";
    private const string TestAudience = "test-audience";

    private static JwtTokenService CreateSut(TimeProvider? timeProvider = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = TestKey,
                ["Jwt:Issuer"] = TestIssuer,
                ["Jwt:Audience"] = TestAudience,
            })
            .Build();

        return new JwtTokenService(config, timeProvider ?? TimeProvider.System);
    }

    private static JwtUserClaims SampleClaims(
        Guid? userId = null,
        string email = "user@example.com",
        UserRole role = UserRole.Coordinator,
        UserStatus status = UserStatus.Approved)
        => new(userId ?? Guid.NewGuid(), email, role, status);

    [Fact]
    public void GenerateToken_ReturnsNonEmptyString()
    {
        var sut = CreateSut();
        var token = sut.GenerateToken(SampleClaims());
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void GenerateToken_ProducesValidJwtStructure()
    {
        var sut = CreateSut();
        var token = sut.GenerateToken(SampleClaims());

        // A JWT always has three base64-encoded segments separated by dots.
        Assert.Equal(3, token.Split('.').Length);
    }

    [Fact]
    public void GenerateToken_ContainsCorrectSubAndEmailClaims()
    {
        var userId = Guid.NewGuid();
        var sut = CreateSut();
        var token = sut.GenerateToken(SampleClaims(userId: userId, email: "test@example.com"));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(userId.ToString(), jwt.Subject);
        Assert.Equal("test@example.com", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
    }

    [Fact]
    public void GenerateToken_ContainsCorrectRoleAndStatusClaims()
    {
        var sut = CreateSut();
        var token = sut.GenerateToken(SampleClaims(role: UserRole.Festmitarbeiter, status: UserStatus.Approved));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(UserRole.Festmitarbeiter.ToString(), jwt.Claims.First(c => c.Type == ClaimTypes.Role).Value);
        Assert.Equal(UserStatus.Approved.ToString(), jwt.Claims.First(c => c.Type == "status").Value);
    }

    [Fact]
    public void GenerateToken_HasCorrectIssuerAndAudience()
    {
        var sut = CreateSut();
        var token = sut.GenerateToken(SampleClaims());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(TestIssuer, jwt.Issuer);
        Assert.Contains(TestAudience, jwt.Audiences);
    }

    [Fact]
    public void GenerateToken_ExpiresInApproximatelyTwoHours()
    {
        var before = DateTimeOffset.UtcNow;
        var sut = CreateSut();
        var token = sut.GenerateToken(SampleClaims());
        var after = DateTimeOffset.UtcNow;

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.True(jwt.ValidTo >= before.AddHours(2).AddMinutes(-1).UtcDateTime);
        Assert.True(jwt.ValidTo <= after.AddHours(2).AddMinutes(1).UtcDateTime);
    }

    [Fact]
    public void GenerateToken_PassesSignatureValidation()
    {
        var sut = CreateSut();
        var token = sut.GenerateToken(SampleClaims());

        var handler = new JwtSecurityTokenHandler();
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = TestIssuer,
            ValidateAudience = true,
            ValidAudience = TestAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        var principal = handler.ValidateToken(token, validationParams, out _);
        Assert.NotNull(principal);
    }
}
