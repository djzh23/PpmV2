using Microsoft.EntityFrameworkCore;
using PpmV2.Domain.Locations;
using PpmV2.Infrastructure.Identity;
using PpmV2.Infrastructure.Persistence;
using PpmV2.Infrastructure.Persistence.Repositories;

namespace PpmV2.Tests.Infrastructure;

public class ShiftRepositoryTests
{
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Location NewLocation() => new()
    {
        Name = "Test Location",
        District = "Berlin",
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    private static AppUser NewUser() => new()
    {
        Id = Guid.NewGuid(),
        UserName = $"{Guid.NewGuid()}@test.com",
        Email = $"{Guid.NewGuid()}@test.com"
    };

    // ── LocationExistsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task LocationExistsAsync_ReturnsTrue_WhenLocationExists()
    {
        using var db = CreateDb();
        var repo = new ShiftRepository(db);
        var location = NewLocation();
        db.Locations.Add(location);
        await db.SaveChangesAsync();

        var result = await repo.LocationExistsAsync(location.Id, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task LocationExistsAsync_ReturnsFalse_WhenLocationDoesNotExist()
    {
        using var db = CreateDb();
        var repo = new ShiftRepository(db);

        var result = await repo.LocationExistsAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result);
    }

    // ── CountExistingUsersAsync ──────────────────────────────────────────────

    [Fact]
    public async Task CountExistingUsersAsync_ReturnsCorrectCount_WhenSomeIdsExist()
    {
        using var db = CreateDb();
        var repo = new ShiftRepository(db);
        var user1 = NewUser();
        var user2 = NewUser();
        db.Users.AddRange(user1, user2);
        await db.SaveChangesAsync();

        var ids = new List<Guid> { user1.Id, user2.Id, Guid.NewGuid() };
        var count = await repo.CountExistingUsersAsync(ids, CancellationToken.None);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task CountExistingUsersAsync_ReturnsAllCount_WhenAllIdsExist()
    {
        using var db = CreateDb();
        var repo = new ShiftRepository(db);
        var user1 = NewUser();
        var user2 = NewUser();
        db.Users.AddRange(user1, user2);
        await db.SaveChangesAsync();

        var count = await repo.CountExistingUsersAsync([user1.Id, user2.Id], CancellationToken.None);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task CountExistingUsersAsync_ReturnsZero_WhenNoIdsMatch()
    {
        using var db = CreateDb();
        var repo = new ShiftRepository(db);

        var count = await repo.CountExistingUsersAsync([Guid.NewGuid(), Guid.NewGuid()], CancellationToken.None);

        Assert.Equal(0, count);
    }
}
