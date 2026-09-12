using Microsoft.AspNetCore.Identity;
using Moq;
using PpmV2.Application.Admin.Interfaces;
using PpmV2.Application.Auth.DTOs;
using PpmV2.Application.Auth.Interfaces;
using PpmV2.Application.Users.Interfaces;
using PpmV2.Domain.Auth;
using PpmV2.Domain.Users;
using PpmV2.Infrastructure.Auth;
using PpmV2.Infrastructure.Identity;

namespace PpmV2.Tests.Auth;

public class AuthServiceTests
{
    private readonly Mock<UserManager<AppUser>> _userManagerMock;
    private readonly Mock<IUserProfileRepository> _userProfileRepoMock;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepoMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userManagerMock = CreateMockUserManager();
        _userProfileRepoMock = new Mock<IUserProfileRepository>();
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();
        _refreshTokenRepoMock = new Mock<IRefreshTokenRepository>();

        _refreshTokenRepoMock
            .Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _refreshTokenRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _authService = new AuthService(
            _userManagerMock.Object,
            _userProfileRepoMock.Object,
            _jwtTokenServiceMock.Object,
            _refreshTokenRepoMock.Object,
            TimeProvider.System);
    }


    // Helper method for creating the UserManager mock (encapsulates the complexity)
    private Mock<UserManager<AppUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<AppUser>>();
        return new Mock<UserManager<AppUser>>(
            store.Object,
            null!, // IOptions<IdentityOptions>
            new PasswordHasher<AppUser>(),
            Array.Empty<IUserValidator<AppUser>>(),
            Array.Empty<IPasswordValidator<AppUser>>(),
            null!, null!, null!, null! // Null-forgiving Operator verwenden
        );
    }

    [Fact]
    public async Task RegisterAsync_Should_Create_User_And_Profile_When_Email_Is_New()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "Pass123$",
            Firstname = "Test",
            Lastname = "User"
        };

        _userManagerMock
            .Setup(m => m.FindByEmailAsync(request.Email))
            .ReturnsAsync((AppUser)null!); // Email does not exist yet

        AppUser? createdUser = null;

        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<AppUser>(), request.Password))
            .Callback<AppUser, string>((user, _) =>
            {
                user.Id = Guid.NewGuid(); // simulate real DB ID generation
                createdUser = user;
            })
            .ReturnsAsync(IdentityResult.Success);

        _userProfileRepoMock
            .Setup(r => r.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _userProfileRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert: AuthResult correctness
        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(request.Email, result.Email);
        Assert.NotNull(result.UserId);
        Assert.NotEqual(Guid.Empty, result.UserId.Value);

        // Assert: UserManager has been used properly
        _userManagerMock.Verify(
            m => m.CreateAsync(It.IsAny<AppUser>(), request.Password),
            Times.Once
        );

        // Assert: Profile repository has been used properly
        _userProfileRepoMock.Verify(r => r.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Once);
        _userProfileRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Assert: captured AppUser correctness
        Assert.NotNull(createdUser);
        Assert.Equal(request.Email, createdUser!.Email);
        Assert.Equal(UserStatus.Pending, createdUser.Status);
        Assert.Equal(UserRole.Honorarkraft, createdUser.Role);
        Assert.Equal(createdUser.Id, result.UserId);
    }


    [Theory]
    [InlineData("existing@example.com")]
    [InlineData("another.user@test.com")]
    public async Task RegisterAsync_Should_Return_Fail_When_Email_Already_Exists(string existingEmail)
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = existingEmail,
            Password = "Pass123$",
            Firstname = "Test",
            Lastname = "User"
        };

        _userManagerMock
            .Setup(m => m.FindByEmailAsync(existingEmail))
            .ReturnsAsync(new AppUser { Email = existingEmail });

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert: kein Exception mehr
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("already exists", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);

        _userManagerMock.Verify(m => m.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>()), Times.Never);
        _userProfileRepoMock.Verify(r => r.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Never);
        _userProfileRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_Should_Return_New_Token_Pair_And_Revoke_Old_Token()
    {
        var userId = Guid.NewGuid();
        var oldTokenValue = "old-refresh-token";
        var utcNow = DateTime.UtcNow;

        var storedToken = new RefreshToken
        {
            UserId = userId,
            Token = oldTokenValue,
            CreatedAt = utcNow.AddHours(-1),
            ExpiresAt = utcNow.AddDays(6)
        };

        var user = new AppUser { Id = userId, Email = "user@test.com", Status = UserStatus.Approved };

        _refreshTokenRepoMock
            .Setup(r => r.GetByTokenAsync(oldTokenValue, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _jwtTokenServiceMock
            .Setup(j => j.GenerateToken(It.IsAny<JwtUserClaims>()))
            .Returns("new-access-token");

        var result = await _authService.RefreshAsync(new RefreshRequest(oldTokenValue));

        Assert.True(result.Success);
        Assert.Equal("new-access-token", result.Token);
        Assert.NotNull(result.RefreshToken);
        Assert.NotEqual(oldTokenValue, result.RefreshToken);
        Assert.NotNull(storedToken.RevokedAt);
    }

    [Fact]
    public async Task RefreshAsync_Should_Fail_When_Token_Is_Revoked()
    {
        var revokedToken = new RefreshToken
        {
            UserId = Guid.NewGuid(),
            Token = "revoked",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            ExpiresAt = DateTime.UtcNow.AddDays(6),
            RevokedAt = DateTime.UtcNow.AddHours(-1)
        };

        _refreshTokenRepoMock
            .Setup(r => r.GetByTokenAsync("revoked", It.IsAny<CancellationToken>()))
            .ReturnsAsync(revokedToken);

        var result = await _authService.RefreshAsync(new RefreshRequest("revoked"));

        Assert.False(result.Success);
        Assert.Equal(AuthErrorCode.InvalidCredentials, result.ErrorCode);
    }

    [Fact]
    public async Task RefreshAsync_Should_Fail_When_Token_Is_Expired()
    {
        var expiredToken = new RefreshToken
        {
            UserId = Guid.NewGuid(),
            Token = "expired",
            CreatedAt = DateTime.UtcNow.AddDays(-8),
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };

        _refreshTokenRepoMock
            .Setup(r => r.GetByTokenAsync("expired", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredToken);

        var result = await _authService.RefreshAsync(new RefreshRequest("expired"));

        Assert.False(result.Success);
        Assert.Equal(AuthErrorCode.InvalidCredentials, result.ErrorCode);
    }

    [Fact]
    public async Task RefreshAsync_Should_Fail_When_Token_Does_Not_Exist()
    {
        _refreshTokenRepoMock
            .Setup(r => r.GetByTokenAsync("unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var result = await _authService.RefreshAsync(new RefreshRequest("unknown"));

        Assert.False(result.Success);
        Assert.Equal(AuthErrorCode.InvalidCredentials, result.ErrorCode);
    }

    [Fact]
    public async Task LogoutAsync_Should_Revoke_Token()
    {
        var activeToken = new RefreshToken
        {
            UserId = Guid.NewGuid(),
            Token = "active",
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            ExpiresAt = DateTime.UtcNow.AddDays(6)
        };

        _refreshTokenRepoMock
            .Setup(r => r.GetByTokenAsync("active", It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeToken);

        var result = await _authService.LogoutAsync("active");

        Assert.True(result.Success);
        Assert.NotNull(activeToken.RevokedAt);
        _refreshTokenRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_Should_Succeed_When_Token_Already_Revoked()
    {
        var alreadyRevoked = new RefreshToken
        {
            UserId = Guid.NewGuid(),
            Token = "already-revoked",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            ExpiresAt = DateTime.UtcNow.AddDays(6),
            RevokedAt = DateTime.UtcNow.AddHours(-2)
        };

        _refreshTokenRepoMock
            .Setup(r => r.GetByTokenAsync("already-revoked", It.IsAny<CancellationToken>()))
            .ReturnsAsync(alreadyRevoked);

        var result = await _authService.LogoutAsync("already-revoked");

        Assert.True(result.Success);
        // SaveChanges should NOT be called -- idempotent, no mutation needed.
        _refreshTokenRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

}