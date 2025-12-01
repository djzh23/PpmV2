using Microsoft.AspNetCore.Identity;
using Moq;
using PpmV2.Application.Auth.DTOs;
using PpmV2.Domain.Users;
using PpmV2.Domain.Users.Abstractions;
using PpmV2.Infrastructure.Auth;
using PpmV2.Infrastructure.Identity;

namespace PpmV2.Tests.Auth;

public class AuthServiceTests
{
    // Fields for mocks and service used in all tests
    private readonly Mock<UserManager<AppUser>> _userManagerMock;
    private readonly Mock<IUserProfileRepository> _userProfileRepoMock;
    private readonly AuthService _authService;

    // Constructor for common setup (runs before each test)
    public AuthServiceTests()
    {
        _userManagerMock = CreateMockUserManager();
        _userProfileRepoMock = new Mock<IUserProfileRepository>();
        _authService = new AuthService(_userManagerMock.Object, _userProfileRepoMock.Object);
    }

    // Helper method for creating the UserManager mock (encapsulates the complexity)
    private Mock<UserManager<AppUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<AppUser>>();
        return new Mock<UserManager<AppUser>>(
            store.Object,
            null, // IOptions<IdentityOptions>
            new PasswordHasher<AppUser>(),
            Array.Empty<IUserValidator<AppUser>>(),
            Array.Empty<IPasswordValidator<AppUser>>(),
            null, null, null, null
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
            .Setup(r => r.AddAsync(It.IsAny<UserProfile>()))
            .Returns(Task.CompletedTask);

        _userProfileRepoMock
            .Setup(r => r.SaveChangesAsync())
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
        _userProfileRepoMock.Verify(r => r.AddAsync(It.IsAny<UserProfile>()), Times.Once);
        _userProfileRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);

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
        _userProfileRepoMock.Verify(r => r.AddAsync(It.IsAny<UserProfile>()), Times.Never);
        _userProfileRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

}