using Microsoft.EntityFrameworkCore;
using PpmV2.Domain.Users;
using PpmV2.Domain.Users.Abstractions;

namespace PpmV2.Infrastructure.Persistence.Users;

public class UserProfileRepository : IUserProfileRepository
{
    private readonly AppDbContext _dbContext;

    public UserProfileRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserProfile?> GetByIdAsync(Guid id)
    {
        return await _dbContext.UserProfiles
            .FirstOrDefaultAsync(u => u.Id == id);
    }

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
