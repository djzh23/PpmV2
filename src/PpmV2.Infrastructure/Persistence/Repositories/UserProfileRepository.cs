using Microsoft.EntityFrameworkCore;
using PpmV2.Application.Users.Interfaces;
using PpmV2.Domain.Users;

namespace PpmV2.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core repository for UserProfile entities.
/// </summary>
/// <remarks>
/// UserProfile stores application-specific user data and complements ASP.NET Identity.
/// This repository encapsulates all persistence operations for profiles and keeps EF Core concerns
/// out of the application layer.
/// </remarks>
public class UserProfileRepository : IUserProfileRepository
{
    private readonly AppDbContext _dbContext;

    public UserProfileRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>Returns a profile by its domain identifier.</summary>
    public async Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbContext.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    /// <summary>Returns a profile by the associated Identity user id (1:1 relationship).</summary>
    public async Task<UserProfile?> GetByIdentityUserIdAsync(Guid identityUserId, CancellationToken ct = default)
    {
        return await _dbContext.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId, ct);
    }

    public async Task<UserProfile?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        return await _dbContext.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, ct);
    }

    public async Task AddAsync(UserProfile profile, CancellationToken ct = default)
    {
        await _dbContext.UserProfiles.AddAsync(profile, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _dbContext.SaveChangesAsync(ct);
    }


}
