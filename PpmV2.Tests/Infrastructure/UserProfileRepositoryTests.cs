using Microsoft.EntityFrameworkCore;
using PpmV2.Domain.Users;
using PpmV2.Infrastructure.Persistence;
using PpmV2.Infrastructure.Persistence.Repositories;

namespace PpmV2.Tests.Infrastructure;

public class UserProfileRepositoryTests
{
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static UserProfile NewProfile(string email, Guid? identityUserId = null) => new()
    {
        IdentityUserId = identityUserId ?? Guid.NewGuid(),
        Email = email,
        Firstname = "Test",
        Lastname = "User",
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task AddAsync_And_SaveChangesAsync_PersistProfile()
    {
        using var db = CreateDb();
        var repo = new UserProfileRepository(db);
        var profile = NewProfile("persist@test.com");

        await repo.AddAsync(profile);
        await repo.SaveChangesAsync();

        Assert.NotEqual(Guid.Empty, profile.Id);
        Assert.Equal(1, await db.UserProfiles.CountAsync());
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsProfile_WhenExists()
    {
        using var db = CreateDb();
        var repo = new UserProfileRepository(db);
        var profile = NewProfile("byid@test.com");
        await repo.AddAsync(profile);
        await repo.SaveChangesAsync();

        var result = await repo.GetByIdAsync(profile.Id);

        Assert.NotNull(result);
        Assert.Equal("byid@test.com", result!.Email);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        using var db = CreateDb();
        var repo = new UserProfileRepository(db);

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdentityUserIdAsync_ReturnsProfile_WhenExists()
    {
        using var db = CreateDb();
        var repo = new UserProfileRepository(db);
        var identityId = Guid.NewGuid();
        var profile = NewProfile("byidentity@test.com", identityId);
        await repo.AddAsync(profile);
        await repo.SaveChangesAsync();

        var result = await repo.GetByIdentityUserIdAsync(identityId);

        Assert.NotNull(result);
        Assert.Equal(identityId, result!.IdentityUserId);
    }

    [Fact]
    public async Task GetByIdentityUserIdAsync_ReturnsNull_WhenNotFound()
    {
        using var db = CreateDb();
        var repo = new UserProfileRepository(db);

        var result = await repo.GetByIdentityUserIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsProfile_WhenExists()
    {
        using var db = CreateDb();
        var repo = new UserProfileRepository(db);
        var profile = NewProfile("byemail@test.com");
        await repo.AddAsync(profile);
        await repo.SaveChangesAsync();

        var result = await repo.GetByEmailAsync("byemail@test.com");

        Assert.NotNull(result);
        Assert.Equal("byemail@test.com", result!.Email);
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsNull_WhenEmailNotFound()
    {
        using var db = CreateDb();
        var repo = new UserProfileRepository(db);

        var result = await repo.GetByEmailAsync("nobody@test.com");

        Assert.Null(result);
    }
}
