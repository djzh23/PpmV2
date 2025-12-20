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
    public async Task<UserProfile?> GetByIdAsync(Guid id)
    {
        return await _dbContext.UserProfiles
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    /// <summary>Returns a profile by the associated Identity user id (1:1 relationship).</summary>
    public async Task<UserProfile?> GetByIdentityUserIdAsync(Guid identityUserId)
    {
        return await _dbContext.UserProfiles
            .FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId);
    }

    public async Task<UserProfile?> GetByEmailAsync(string email)
    {
        return await _dbContext.UserProfiles
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task AddAsync(UserProfile profile)
    {
        await _dbContext.UserProfiles.AddAsync(profile);
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }


}
