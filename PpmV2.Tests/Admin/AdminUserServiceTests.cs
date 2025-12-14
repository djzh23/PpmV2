using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PpmV2.Application.Admin.Interfaces;
using PpmV2.Domain.Users;
using PpmV2.Infrastructure.Admin.Services;
using PpmV2.Infrastructure.Identity;
using PpmV2.Infrastructure.Persistence;

namespace PpmV2.Tests.Admin;

public class AdminUserServiceTests
{
    private static (AppDbContext Db, UserManager<AppUser> UserManager, IAdminUserService Service) CreateSut()
    {
        // Unique DB per test run
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var db = new AppDbContext(options);

        // Identity store
        var store = new UserStore<AppUser, AppRole, AppDbContext, Guid>(db);

        // UserManager dependencies
        var identityOptions = Options.Create(new IdentityOptions());
        var passwordHasher = new PasswordHasher<AppUser>();
        var userValidators = new List<IUserValidator<AppUser>> { new UserValidator<AppUser>() };
        var passwordValidators = new List<IPasswordValidator<AppUser>> { new PasswordValidator<AppUser>() };
        var keyNormalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        var services = new ServiceCollection().BuildServiceProvider();
        var logger = NullLogger<UserManager<AppUser>>.Instance;

        var userManager = new UserManager<AppUser>(
            store,
            identityOptions,
            passwordHasher,
            userValidators,
            passwordValidators,
            keyNormalizer,
            errors,
            services,
            logger);

        var service = new AdminUserService(userManager);

        return (db, userManager, service);
    }

    private static async Task<AppUser> SeedUserAsync(
        UserManager<AppUser> userManager,
        AppDbContext db,
        string email,
        UserStatus status,
        UserRole role = UserRole.Honorarkraft
        )
    {
        var user = new AppUser
        {
            UserName = email,
            Email = email,
            Role = role,
            Status = status,
            IsActive = true,
            IsProfileCompleted = false
        };

        var result = await userManager.CreateAsync(user);
        Assert.True(result.Succeeded);

        return user;

    }

    [Fact]
    public async Task GetPendingUsersAsync_ReturnsOnlyPendingUsers()
    {
        var (db, userManager, service) = CreateSut();

        await SeedUserAsync(userManager, db, "p1@test.com", UserStatus.Pending);
        await SeedUserAsync(userManager, db, "a1@test.com", UserStatus.Approved);
        await SeedUserAsync(userManager, db, "r1@test.com", UserStatus.Rejected);
        await SeedUserAsync(userManager, db, "p2@test.com", UserStatus.Pending);

        var pending = await service.GetPendingUsersAsync();

        Assert.Equal(2, pending.Count);
        Assert.All(pending, u => Assert.Equal(UserStatus.Pending, u.Status));
        Assert.Contains(pending, u => u.Email == "p1@test.com");
        Assert.Contains(pending, u => u.Email == "p2@test.com");
    }

    [Fact]
    public async Task ApproveUserAsync_ReturnsSuccess_WhenUserIsAlreadyApproved()
    {
        var (db, userManager, service) = CreateSut();

        var user = await SeedUserAsync(userManager, db, "approved@test.com", UserStatus.Approved);

        var result = await service.ApproveUserAsync(user.Id);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ApproveUserAsync_UpdatesStatusToApproved()
    {
        var (db, userManager, service) = CreateSut();

        var user = await SeedUserAsync(userManager, db, "pending@test.com", UserStatus.Pending);

        var result = await service.ApproveUserAsync(user.Id);

        Assert.True(result.Success);

        var updated = await userManager.FindByIdAsync(user.Id.ToString());
        Assert.NotNull(updated);
        Assert.Equal(UserStatus.Approved, updated!.Status);
    }

    [Fact]
    public async Task RejectUserAsync_UpdatesStatusToRejected()
    {
        var (db, userManager, service) = CreateSut();

        var user = await SeedUserAsync(userManager, db, "pending2@test.com", UserStatus.Pending);

        var result = await service.RejectUserAsync(user.Id);

        Assert.True(result.Success);

        var updated = await userManager.FindByIdAsync(user.Id.ToString());
        Assert.NotNull(updated);
        Assert.Equal(UserStatus.Rejected, updated!.Status);
    }

    [Fact]
    public async Task ApproveUserAsync_ReturnsFail_WhenUserNotFound()
    {
        var (_, _, service) = CreateSut();

        var result = await service.ApproveUserAsync(Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }
}
